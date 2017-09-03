// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const int MON_THREAD_GROWTH = 5;
const int MON_ARRAY_GROWTH = 40;
const int MON_MAX_MONITORS_PER_THREAD = 63;
const int MON_THREAD_INIT_RETRIES = 1000;
const int MON_THREAD_INIT_RETRY_PERIOD_IN_MS = 10;
const int MON_THREAD_NETWORK_FAIL_RETRY_IN_MS = 1000*60; // if we know we failed to connect, retry every minute
const int MON_THREAD_NETWORK_SUCCESSFUL_RETRY_IN_MS = 1000*60*20; // if we're just checking for remote servers dieing, check much less frequently
const int MON_THREAD_WAIT_REMOVE_DEVICE = 5000;
const LPCWSTR MONUTIL_WINDOW_CLASS = L"MonUtilClass";

enum MON_MESSAGE
{
    MON_MESSAGE_ADD = WM_APP + 1,
    MON_MESSAGE_REMOVE,
    MON_MESSAGE_REMOVED, // Sent by waiter thread back to coordinator thread to indicate a remove occurred
    MON_MESSAGE_NETWORK_WAIT_FAILED, // Sent by waiter thread back to coordinator thread to indicate a network wait failed. Coordinator thread will periodically trigger retries (via MON_MESSAGE_NETWORK_STATUS_UPDATE messages).
    MON_MESSAGE_NETWORK_WAIT_SUCCEEDED, // Sent by waiter thread back to coordinator thread to indicate a previously failing network wait is now succeeding. Coordinator thread will stop triggering retries if no other failing waits exist.
    MON_MESSAGE_NETWORK_STATUS_UPDATE, // Some change to network connectivity occurred (a network connection was connected or disconnected for example)
    MON_MESSAGE_NETWORK_RETRY_SUCCESSFUL_NETWORK_WAITS, // Coordinator thread is telling waiters to retry any successful network waits.
        // Annoyingly, this is necessary to catch the rare case that the remote server goes offline unexpectedly, such as by
        // network cable unplugged or power loss - in this case there is no local network status change, and the wait will just never fire.
        // So we very occasionally retry all successful network waits. When this occurs, we notify for changes, even though there may not have been any.
        // This is because we have no way to detect if the old wait had failed (and changes were lost) due to the remote server going offline during that time or not.
        // If we do this often, it can cause a lot of wasted work (which could be expensive for battery life), so the default is to do it very rarely (every 20 minutes).
    MON_MESSAGE_NETWORK_RETRY_FAILED_NETWORK_WAITS, // Coordinator thread is telling waiters to retry any failed network waits
    MON_MESSAGE_DRIVE_STATUS_UPDATE, // Some change to local drive has occurred (new drive created or plugged in, or removed)
    MON_MESSAGE_DRIVE_QUERY_REMOVE, // User wants to unplug a drive, which MonUtil will always allow
    MON_MESSAGE_STOP
};

enum MON_TYPE
{
    MON_NONE = 0,
    MON_DIRECTORY = 1,
    MON_REGKEY = 2
};

struct MON_REQUEST
{
    MON_TYPE type;
    DWORD dwMaxSilencePeriodInMs;

    // Handle to the main window for RegisterDeviceNotification() (same handle as owned by coordinator thread)
    HWND hwnd;
    // and handle to the notification (specific to this request)
    HDEVNOTIFY hNotify;

    BOOL fRecursive;
    void *pvContext;

    HRESULT hrStatus;

    LPWSTR sczOriginalPathRequest;
    BOOL fNetwork; // This reflects either a UNC or mounted drive original request
    DWORD dwPathHierarchyIndex;
    LPWSTR *rgsczPathHierarchy;
    DWORD cPathHierarchy;

    // If the notify fires, fPendingFire gets set to TRUE, and we wait to see if other writes are occurring, and only after the configured silence period do we notify of changes
    // after notification, we set fPendingFire back to FALSE
    BOOL fPendingFire;
    BOOL fSkipDeltaAdd;
    DWORD dwSilencePeriodInMs;

    union
    {
        struct
        {
        } directory;
        struct
        {
            HKEY hkRoot;
            HKEY hkSubKey;
            REG_KEY_BITNESS kbKeyBitness; // Only used to pass on 32-bit, 64-bit, or default parameter
        } regkey;
    };
};

struct MON_ADD_MESSAGE
{
    MON_REQUEST request;
    HANDLE handle;
};

struct MON_REMOVE_MESSAGE
{
    MON_TYPE type;
    BOOL fRecursive;

    union
    {
        struct
        {
            LPWSTR sczDirectory;
        } directory;
        struct
        {
            HKEY hkRoot;
            LPWSTR sczSubKey;
            REG_KEY_BITNESS kbKeyBitness;
        } regkey;
    };
};

struct MON_WAITER_CONTEXT
{
    DWORD dwCoordinatorThreadId;

    HANDLE hWaiterThread;
    DWORD dwWaiterThreadId;
    BOOL fWaiterThreadMessageQueueInitialized;

    // Callbacks
    PFN_MONGENERAL vpfMonGeneral;
    PFN_MONDIRECTORY vpfMonDirectory;
    PFN_MONREGKEY vpfMonRegKey;

    // Context for callbacks
    LPVOID pvContext;

    // HANDLEs are in their own array for easy use with WaitForMultipleObjects()
    // After initialization, the very first handle is just to wake the listener thread to have it re-wait on a new list
    // Because this array is read by both coordinator thread and waiter thread, to avoid locking between both threads, it must start at the maximum size
    HANDLE *rgHandles;
    DWORD cHandles;

    // Requested things to monitor
    MON_REQUEST *rgRequests;
    DWORD cRequests;

    // Number of pending notifications
    DWORD cRequestsPending;

    // Number of requests in a failed state (couldn't initiate wait)
    DWORD cRequestsFailing;
};

// Info stored about each waiter by the coordinator
struct MON_WAITER_INFO
{
    DWORD cMonitorCount;

    MON_WAITER_CONTEXT *pWaiterContext;
};

// This struct is used when Thread A wants to send a task to another thread B (and get notified when the task finishes)
// You typically declare this struct in a manner that a pointer to it is valid as long as a thread that could respond is still running
// (even long after sender is no longer waiting, in case thread has huge message queue)
// and you must send 2 parameters in the message:
// 1) a pointer to this struct (which is always valid)
// 2) the original value of dwIteration
// The receiver of the message can compare the current value of dwSendIteration in the struct with what was sent in the message
// If values are different, we're too late and thread A is no longer waiting on this response
// otherwise, set dwResponseIteration to the same value, and call ::SetEvent() on hWait
// Thread A will then wakeup, and must verify that dwResponseIteration == dwSendIteration to ensure it isn't an earlier out-of-date reply
// replying to a newer wait
// pvContext is used to send a misc parameter related to processing data
struct MON_INTERNAL_TEMPORARY_WAIT
{
    // Should be incremented each time sender sends a pointer to this struct, so each request has a different iteration
    DWORD dwSendIteration;
    DWORD dwReceiveIteration;
    HANDLE hWait;
    void *pvContext;
};

struct MON_STRUCT
{
    HANDLE hCoordinatorThread;
    DWORD dwCoordinatorThreadId;
    BOOL fCoordinatorThreadMessageQueueInitialized;

    // Invisible window for receiving network status & drive added/removal messages
    HWND hwnd;
    // Used by window procedure for sending request and waiting for response from waiter threads
    // such as in event of a request to remove a device
    MON_INTERNAL_TEMPORARY_WAIT internalWait;

    // Callbacks
    PFN_MONGENERAL vpfMonGeneral;
    PFN_MONDRIVESTATUS vpfMonDriveStatus;
    PFN_MONDIRECTORY vpfMonDirectory;
    PFN_MONREGKEY vpfMonRegKey;

    // Context for callbacks
    LPVOID pvContext;

    // Waiter thread array
    MON_WAITER_INFO *rgWaiterThreads;
    DWORD cWaiterThreads;
};

const int MON_HANDLE_BYTES = sizeof(MON_STRUCT);

static DWORD WINAPI CoordinatorThread(
    __in_bcount(sizeof(MON_STRUCT)) LPVOID pvContext
    );
// Initiates (or if *pHandle is non-null, continues) wait on the directory or subkey
// if the directory or subkey doesn't exist, instead calls it on the first existing parent directory or subkey
// writes to pRequest->dwPathHierarchyIndex with the array index that was waited on
static HRESULT InitiateWait(
    __inout MON_REQUEST *pRequest,
    __inout HANDLE *pHandle
    );
static DWORD WINAPI WaiterThread(
    __in_bcount(sizeof(MON_WAITER_CONTEXT)) LPVOID pvContext
    );
static void Notify(
    __in HRESULT hr,
    __in MON_WAITER_CONTEXT *pWaiterContext,
    __in MON_REQUEST *pRequest
    );
static void MonRequestDestroy(
    __in MON_REQUEST *pRequest
    );
static void MonAddMessageDestroy(
    __in MON_ADD_MESSAGE *pMessage
    );
static void MonRemoveMessageDestroy(
    __in MON_REMOVE_MESSAGE *pMessage
    );
static BOOL GetRecursiveFlag(
    __in MON_REQUEST *pRequest,
    __in DWORD dwIndex
    );
static HRESULT FindRequestIndex(
    __in MON_WAITER_CONTEXT *pWaiterContext,
    __in MON_REMOVE_MESSAGE *pMessage,
    __out DWORD *pdwIndex
    );
static HRESULT RemoveRequest(
    __inout MON_WAITER_CONTEXT *pWaiterContext,
    __in DWORD dwRequestIndex
    );
static REGSAM GetRegKeyBitness(
    __in MON_REQUEST *pRequest
    );
static HRESULT DuplicateRemoveMessage(
    __in MON_REMOVE_MESSAGE *pMessage,
    __out MON_REMOVE_MESSAGE **ppMessage
    );
static LRESULT CALLBACK MonWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );
static HRESULT CreateMonWindow(
    __in MON_STRUCT *pm,
    __out HWND *pHwnd
    );
// if *phMonitor is non-NULL, closes the old wait before re-starting the new wait
static HRESULT WaitForNetworkChanges(
    __inout HANDLE *phMonitor,
    __in MON_STRUCT *pm
    );
static HRESULT UpdateWaitStatus(
    __in HRESULT hrNewStatus,
    __inout MON_WAITER_CONTEXT *pWaiterContext,
    __in DWORD dwRequestIndex,
    __out DWORD *pdwNewRequestIndex
    );

extern "C" HRESULT DAPI MonCreate(
    __out_bcount(MON_HANDLE_BYTES) MON_HANDLE *pHandle,
    __in PFN_MONGENERAL vpfMonGeneral,
    __in_opt PFN_MONDRIVESTATUS vpfMonDriveStatus,
    __in_opt PFN_MONDIRECTORY vpfMonDirectory,
    __in_opt PFN_MONREGKEY vpfMonRegKey,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    DWORD dwRetries = MON_THREAD_INIT_RETRIES;

    ExitOnNull(pHandle, hr, E_INVALIDARG, "Pointer to handle not specified while creating monitor");

    // Allocate the struct
    *pHandle = static_cast<MON_HANDLE>(MemAlloc(sizeof(MON_STRUCT), TRUE));
    ExitOnNull(*pHandle, hr, E_OUTOFMEMORY, "Failed to allocate monitor object");

    MON_STRUCT *pm = static_cast<MON_STRUCT *>(*pHandle);

    pm->vpfMonGeneral = vpfMonGeneral;
    pm->vpfMonDriveStatus = vpfMonDriveStatus;
    pm->vpfMonDirectory = vpfMonDirectory;
    pm->vpfMonRegKey = vpfMonRegKey;
    pm->pvContext = pvContext;

    pm->hCoordinatorThread = ::CreateThread(NULL, 0, CoordinatorThread, pm, 0, &pm->dwCoordinatorThreadId);
    if (!pm->hCoordinatorThread)
    {
        ExitWithLastError(hr, "Failed to create waiter thread.");
    }

    // Ensure the created thread initializes its message queue. It does this first thing, so if it doesn't within 10 seconds, there must be a huge problem.
    while (!pm->fCoordinatorThreadMessageQueueInitialized && 0 < dwRetries)
    {
        ::Sleep(MON_THREAD_INIT_RETRY_PERIOD_IN_MS);
        --dwRetries;
    }

    if (0 == dwRetries)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Waiter thread apparently never initialized its message queue.");
    }

LExit:
    return hr;
}

extern "C" HRESULT DAPI MonAddDirectory(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle,
    __in_z LPCWSTR wzDirectory,
    __in BOOL fRecursive,
    __in DWORD dwSilencePeriodInMs,
    __in_opt LPVOID pvDirectoryContext
    )
{
    HRESULT hr = S_OK;
    MON_STRUCT *pm = static_cast<MON_STRUCT *>(handle);
    LPWSTR sczDirectory = NULL;
    LPWSTR sczOriginalPathRequest = NULL;
    MON_ADD_MESSAGE *pMessage = NULL;

    hr = StrAllocString(&sczOriginalPathRequest, wzDirectory, 0);
    ExitOnFailure(hr, "Failed to convert directory string to UNC path");

    hr = PathBackslashTerminate(&sczOriginalPathRequest);
    ExitOnFailure(hr, "Failed to ensure directory ends in backslash");

    pMessage = reinterpret_cast<MON_ADD_MESSAGE *>(MemAlloc(sizeof(MON_ADD_MESSAGE), TRUE));
    ExitOnNull(pMessage, hr, E_OUTOFMEMORY, "Failed to allocate memory for message");

    if (sczOriginalPathRequest[0] == L'\\' && sczOriginalPathRequest[1] == L'\\')
    {
        pMessage->request.fNetwork = TRUE;
    }
    else
    {
        hr = UncConvertFromMountedDrive(&sczDirectory, sczOriginalPathRequest);
        if (SUCCEEDED(hr))
        {
            pMessage->request.fNetwork = TRUE;
        }
    }

    if (NULL == sczDirectory)
    {
        // Likely not a mounted drive - just copy the request then
        hr = S_OK;

        hr = StrAllocString(&sczDirectory, sczOriginalPathRequest, 0);
        ExitOnFailure(hr, "Failed to copy original path request: %ls", sczOriginalPathRequest);
    }

    pMessage->handle = INVALID_HANDLE_VALUE;
    pMessage->request.type = MON_DIRECTORY;
    pMessage->request.fRecursive = fRecursive;
    pMessage->request.dwMaxSilencePeriodInMs = dwSilencePeriodInMs;
    pMessage->request.hwnd = pm->hwnd;
    pMessage->request.pvContext = pvDirectoryContext;
    pMessage->request.sczOriginalPathRequest = sczOriginalPathRequest;
    sczOriginalPathRequest = NULL;

    hr = PathGetHierarchyArray(sczDirectory, &pMessage->request.rgsczPathHierarchy, reinterpret_cast<LPUINT>(&pMessage->request.cPathHierarchy));
    ExitOnFailure(hr, "Failed to get hierarchy array for path %ls", sczDirectory);

    if (0 < pMessage->request.cPathHierarchy)
    {
        pMessage->request.hrStatus = InitiateWait(&pMessage->request, &pMessage->handle);
        if (!::PostThreadMessageW(pm->dwCoordinatorThreadId, MON_MESSAGE_ADD, reinterpret_cast<WPARAM>(pMessage), 0))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to add directory wait for path %ls", sczDirectory);
        }
        pMessage = NULL;
    }

LExit:
    ReleaseStr(sczDirectory);
    ReleaseStr(sczOriginalPathRequest);
    MonAddMessageDestroy(pMessage);

    return hr;
}

extern "C" HRESULT DAPI MonAddRegKey(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle,
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in REG_KEY_BITNESS kbKeyBitness,
    __in BOOL fRecursive,
    __in DWORD dwSilencePeriodInMs,
    __in_opt LPVOID pvRegKeyContext
    )
{
    HRESULT hr = S_OK;
    MON_STRUCT *pm = static_cast<MON_STRUCT *>(handle);
    LPWSTR sczSubKey = NULL;
    MON_ADD_MESSAGE *pMessage = NULL;

    hr = StrAllocString(&sczSubKey, wzSubKey, 0);
    ExitOnFailure(hr, "Failed to copy subkey string");

    hr = PathBackslashTerminate(&sczSubKey);
    ExitOnFailure(hr, "Failed to ensure subkey path ends in backslash");

    pMessage = reinterpret_cast<MON_ADD_MESSAGE *>(MemAlloc(sizeof(MON_ADD_MESSAGE), TRUE));
    ExitOnNull(pMessage, hr, E_OUTOFMEMORY, "Failed to allocate memory for message");

    pMessage->handle = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(pMessage->handle, hr, "Failed to create anonymous event for regkey monitor");

    pMessage->request.type = MON_REGKEY;
    pMessage->request.regkey.hkRoot = hkRoot;
    pMessage->request.regkey.kbKeyBitness = kbKeyBitness;
    pMessage->request.fRecursive = fRecursive;
    pMessage->request.dwMaxSilencePeriodInMs = dwSilencePeriodInMs,
    pMessage->request.hwnd = pm->hwnd;
    pMessage->request.pvContext = pvRegKeyContext;

    hr = PathGetHierarchyArray(sczSubKey, &pMessage->request.rgsczPathHierarchy, reinterpret_cast<LPUINT>(&pMessage->request.cPathHierarchy));
    ExitOnFailure(hr, "Failed to get hierarchy array for subkey %ls", sczSubKey);

    if (0 < pMessage->request.cPathHierarchy)
    {
        pMessage->request.hrStatus = InitiateWait(&pMessage->request, &pMessage->handle);
        ExitOnFailure(hr, "Failed to initiate wait");

        if (!::PostThreadMessageW(pm->dwCoordinatorThreadId, MON_MESSAGE_ADD, reinterpret_cast<WPARAM>(pMessage), 0))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to add directory wait for regkey %ls", sczSubKey);
        }
        pMessage = NULL;
    }

LExit:
    ReleaseStr(sczSubKey);
    MonAddMessageDestroy(pMessage);

    return hr;
}

extern "C" HRESULT DAPI MonRemoveDirectory(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle,
    __in_z LPCWSTR wzDirectory,
    __in BOOL fRecursive
    )
{
    HRESULT hr = S_OK;
    MON_STRUCT *pm = static_cast<MON_STRUCT *>(handle);
    LPWSTR sczDirectory = NULL;
    MON_REMOVE_MESSAGE *pMessage = NULL;

    hr = StrAllocString(&sczDirectory, wzDirectory, 0);
    ExitOnFailure(hr, "Failed to copy directory string");

    hr = PathBackslashTerminate(&sczDirectory);
    ExitOnFailure(hr, "Failed to ensure directory ends in backslash");

    pMessage = reinterpret_cast<MON_REMOVE_MESSAGE *>(MemAlloc(sizeof(MON_REMOVE_MESSAGE), TRUE));
    ExitOnNull(pMessage, hr, E_OUTOFMEMORY, "Failed to allocate memory for message");

    pMessage->type = MON_DIRECTORY;
    pMessage->fRecursive = fRecursive;

    hr = StrAllocString(&pMessage->directory.sczDirectory, sczDirectory, 0);
    ExitOnFailure(hr, "Failed to allocate copy of directory string");

    if (!::PostThreadMessageW(pm->dwCoordinatorThreadId, MON_MESSAGE_REMOVE, reinterpret_cast<WPARAM>(pMessage), 0))
    {
        ExitWithLastError(hr, "Failed to send message to worker thread to add directory wait for path %ls", sczDirectory);
    }
    pMessage = NULL;

LExit:
    MonRemoveMessageDestroy(pMessage);

    return hr;
}

extern "C" HRESULT DAPI MonRemoveRegKey(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle,
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in REG_KEY_BITNESS kbKeyBitness,
    __in BOOL fRecursive
    )
{
    HRESULT hr = S_OK;
    MON_STRUCT *pm = static_cast<MON_STRUCT *>(handle);
    LPWSTR sczSubKey = NULL;
    MON_REMOVE_MESSAGE *pMessage = NULL;

    hr = StrAllocString(&sczSubKey, wzSubKey, 0);
    ExitOnFailure(hr, "Failed to copy subkey string");

    hr = PathBackslashTerminate(&sczSubKey);
    ExitOnFailure(hr, "Failed to ensure subkey path ends in backslash");

    pMessage = reinterpret_cast<MON_REMOVE_MESSAGE *>(MemAlloc(sizeof(MON_REMOVE_MESSAGE), TRUE));
    ExitOnNull(pMessage, hr, E_OUTOFMEMORY, "Failed to allocate memory for message");

    pMessage->type = MON_REGKEY;
    pMessage->regkey.hkRoot = hkRoot;
    pMessage->regkey.kbKeyBitness = kbKeyBitness;
    pMessage->fRecursive = fRecursive;

    hr = StrAllocString(&pMessage->regkey.sczSubKey, sczSubKey, 0);
    ExitOnFailure(hr, "Failed to allocate copy of directory string");

    if (!::PostThreadMessageW(pm->dwCoordinatorThreadId, MON_MESSAGE_REMOVE, reinterpret_cast<WPARAM>(pMessage), 0))
    {
        ExitWithLastError(hr, "Failed to send message to worker thread to add directory wait for path %ls", sczSubKey);
    }
    pMessage = NULL;

LExit:
    ReleaseStr(sczSubKey);
    MonRemoveMessageDestroy(pMessage);

    return hr;
}

extern "C" void DAPI MonDestroy(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    MON_STRUCT *pm = static_cast<MON_STRUCT *>(handle);

    if (!::PostThreadMessageW(pm->dwCoordinatorThreadId, MON_MESSAGE_STOP, 0, 0))
    {
        er = ::GetLastError();
        if (ERROR_INVALID_THREAD_ID == er)
        {
            // It already halted, or doesn't exist for some other reason, so let's just ignore it and clean up
            er = ERROR_SUCCESS;
        }
        ExitOnWin32Error(er, hr, "Failed to send message to background thread to halt");
    }

    if (pm->hCoordinatorThread)
    {
        ::WaitForSingleObject(pm->hCoordinatorThread, INFINITE);
        ::CloseHandle(pm->hCoordinatorThread);
    }

LExit:
    return;
}

static void MonRequestDestroy(
    __in MON_REQUEST *pRequest
    )
{
    if (NULL != pRequest)
    {
        if (MON_REGKEY == pRequest->type)
        {
            ReleaseRegKey(pRequest->regkey.hkSubKey);
        }
        else if (MON_DIRECTORY == pRequest->type && pRequest->hNotify)
        {
            UnregisterDeviceNotification(pRequest->hNotify);
            pRequest->hNotify = NULL;
        }
        ReleaseStr(pRequest->sczOriginalPathRequest);
        ReleaseStrArray(pRequest->rgsczPathHierarchy, pRequest->cPathHierarchy);
    }
}

static void MonAddMessageDestroy(
    __in MON_ADD_MESSAGE *pMessage
    )
{
    if (NULL != pMessage)
    {
        MonRequestDestroy(&pMessage->request);
        if (MON_DIRECTORY == pMessage->request.type && INVALID_HANDLE_VALUE != pMessage->handle)
        {
            ::FindCloseChangeNotification(pMessage->handle);
        }
        else if (MON_REGKEY == pMessage->request.type)
        {
            ReleaseHandle(pMessage->handle);
        }

        ReleaseMem(pMessage);
    }
}

static void MonRemoveMessageDestroy(
    __in MON_REMOVE_MESSAGE *pMessage
    )
{
    if (NULL != pMessage)
    {
        switch (pMessage->type)
        {
        case MON_DIRECTORY:
            ReleaseStr(pMessage->directory.sczDirectory);
            break;
        case MON_REGKEY:
            ReleaseStr(pMessage->regkey.sczSubKey);
            break;
        default:
            Assert(false);
        }

        ReleaseMem(pMessage);
    }
}

static DWORD WINAPI CoordinatorThread(
    __in_bcount(sizeof(MON_STRUCT)) LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    MSG msg = { };
    DWORD dwThreadIndex = DWORD_MAX;
    DWORD dwRetries;
    DWORD dwFailingNetworkWaits = 0;
    MON_WAITER_CONTEXT *pWaiterContext = NULL;
    MON_REMOVE_MESSAGE *pRemoveMessage = NULL;
    MON_REMOVE_MESSAGE *pTempRemoveMessage = NULL;
    MON_STRUCT *pm = reinterpret_cast<MON_STRUCT*>(pvContext);
    WSADATA wsaData = { };
    HANDLE hMonitor = NULL;
    BOOL fRet = FALSE;
    UINT_PTR uTimerSuccessfulNetworkRetry = 0;
    UINT_PTR uTimerFailedNetworkRetry = 0;

    // Ensure the thread has a message queue
    ::PeekMessage(&msg, NULL, WM_USER, WM_USER, PM_NOREMOVE);
    pm->fCoordinatorThreadMessageQueueInitialized = TRUE;

    hr = CreateMonWindow(pm, &pm->hwnd);
    ExitOnFailure(hr, "Failed to create window for status update thread");

    ::WSAStartup(MAKEWORD(2, 2), &wsaData);

    hr = WaitForNetworkChanges(&hMonitor, pm);
    ExitOnFailure(hr, "Failed to wait for network changes");

    uTimerSuccessfulNetworkRetry = ::SetTimer(NULL, 1, MON_THREAD_NETWORK_SUCCESSFUL_RETRY_IN_MS, NULL);
    if (0 == uTimerSuccessfulNetworkRetry)
    {
        ExitWithLastError(hr, "Failed to set timer for network successful retry");
    }

    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnRootFailure(hr, "Unexpected return value from message pump.");
        }
        else
        {
            switch (msg.message)
            {
            case MON_MESSAGE_ADD:
                dwThreadIndex = DWORD_MAX;
                for (DWORD i = 0; i < pm->cWaiterThreads; ++i)
                {
                    if (pm->rgWaiterThreads[i].cMonitorCount < MON_MAX_MONITORS_PER_THREAD)
                    {
                        dwThreadIndex = i;
                        break;
                    }
                }

                if (dwThreadIndex < pm->cWaiterThreads)
                {
                    pWaiterContext = pm->rgWaiterThreads[dwThreadIndex].pWaiterContext;
                }
                else
                {
                    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pm->rgWaiterThreads), pm->cWaiterThreads + 1, sizeof(MON_WAITER_INFO), MON_THREAD_GROWTH);
                    ExitOnFailure(hr, "Failed to grow waiter thread array size");
                    ++pm->cWaiterThreads;

                    dwThreadIndex = pm->cWaiterThreads - 1;
                    pm->rgWaiterThreads[dwThreadIndex].pWaiterContext = reinterpret_cast<MON_WAITER_CONTEXT*>(MemAlloc(sizeof(MON_WAITER_CONTEXT), TRUE));
                    ExitOnNull(pm->rgWaiterThreads[dwThreadIndex].pWaiterContext, hr, E_OUTOFMEMORY, "Failed to allocate waiter context struct");
                    pWaiterContext = pm->rgWaiterThreads[dwThreadIndex].pWaiterContext;
                    pWaiterContext->dwCoordinatorThreadId = ::GetCurrentThreadId();
                    pWaiterContext->vpfMonGeneral = pm->vpfMonGeneral;
                    pWaiterContext->vpfMonDirectory = pm->vpfMonDirectory;
                    pWaiterContext->vpfMonRegKey = pm->vpfMonRegKey;
                    pWaiterContext->pvContext = pm->pvContext;

                    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pWaiterContext->rgHandles), MON_MAX_MONITORS_PER_THREAD + 1, sizeof(HANDLE), 0);
                    ExitOnFailure(hr, "Failed to allocate first handle");
                    pWaiterContext->cHandles = 1;

                    pWaiterContext->rgHandles[0] = ::CreateEventW(NULL, FALSE, FALSE, NULL);
                    ExitOnNullWithLastError(pWaiterContext->rgHandles[0], hr, "Failed to create general event");

                    pWaiterContext->hWaiterThread = ::CreateThread(NULL, 0, WaiterThread, pWaiterContext, 0, &pWaiterContext->dwWaiterThreadId);
                    if (!pWaiterContext->hWaiterThread)
                    {
                        ExitWithLastError(hr, "Failed to create waiter thread.");
                    }

                    dwRetries = MON_THREAD_INIT_RETRIES;
                    while (!pWaiterContext->fWaiterThreadMessageQueueInitialized && 0 < dwRetries)
                    {
                        ::Sleep(MON_THREAD_INIT_RETRY_PERIOD_IN_MS);
                        --dwRetries;
                    }

                    if (0 == dwRetries)
                    {
                        hr = E_UNEXPECTED;
                        ExitOnFailure(hr, "Waiter thread apparently never initialized its message queue.");
                    }
                }

                ++pm->rgWaiterThreads[dwThreadIndex].cMonitorCount;
                if (!::PostThreadMessageW(pWaiterContext->dwWaiterThreadId, MON_MESSAGE_ADD, msg.wParam, 0))
                {
                    ExitWithLastError(hr, "Failed to send message to waiter thread to add monitor");
                }

                if (!::SetEvent(pWaiterContext->rgHandles[0]))
                {
                    ExitWithLastError(hr, "Failed to set event to notify waiter thread of incoming message");
                }
                break;

            case MON_MESSAGE_REMOVE:
                // Send remove to all waiter threads. They'll ignore it if they don't have that monitor.
                // If they do have that monitor, they'll remove it from their list, and tell coordinator they have another
                // empty slot via MON_MESSAGE_REMOVED message
                for (DWORD i = 0; i < pm->cWaiterThreads; ++i)
                {
                    pWaiterContext = pm->rgWaiterThreads[i].pWaiterContext;
                    pRemoveMessage = reinterpret_cast<MON_REMOVE_MESSAGE *>(msg.wParam);

                    hr = DuplicateRemoveMessage(pRemoveMessage, &pTempRemoveMessage);
                    ExitOnFailure(hr, "Failed to duplicate remove message");

                    if (!::PostThreadMessageW(pWaiterContext->dwWaiterThreadId, MON_MESSAGE_REMOVE, reinterpret_cast<WPARAM>(pTempRemoveMessage), msg.lParam))
                    {
                        ExitWithLastError(hr, "Failed to send message to waiter thread to add monitor");
                    }
                    pTempRemoveMessage = NULL;

                    if (!::SetEvent(pWaiterContext->rgHandles[0]))
                    {
                        ExitWithLastError(hr, "Failed to set event to notify waiter thread of incoming remove message");
                    }
                }
                MonRemoveMessageDestroy(pRemoveMessage);
                pRemoveMessage = NULL;
                break;

            case MON_MESSAGE_REMOVED:
                for (DWORD i = 0; i < pm->cWaiterThreads; ++i)
                {
                    if (pm->rgWaiterThreads[i].pWaiterContext->dwWaiterThreadId == static_cast<DWORD>(msg.wParam))
                    {
                        Assert(pm->rgWaiterThreads[i].cMonitorCount > 0);
                        --pm->rgWaiterThreads[i].cMonitorCount;
                        if (0 == pm->rgWaiterThreads[i].cMonitorCount)
                        {
                            if (!::PostThreadMessageW(pm->rgWaiterThreads[i].pWaiterContext->dwWaiterThreadId, MON_MESSAGE_STOP, msg.wParam, msg.lParam))
                            {
                                ExitWithLastError(hr, "Failed to send message to waiter thread to stop");
                            }
                            MemRemoveFromArray(reinterpret_cast<LPVOID>(pm->rgWaiterThreads), i, 1, pm->cWaiterThreads, sizeof(MON_WAITER_INFO), TRUE);
                            --pm->cWaiterThreads;
                            --i; // reprocess this index in the for loop, which will now contain the item after the one we removed
                        }
                    }
                }
                break;

            case MON_MESSAGE_NETWORK_WAIT_FAILED:
                if (0 == dwFailingNetworkWaits)
                {
                    uTimerFailedNetworkRetry = ::SetTimer(NULL, uTimerSuccessfulNetworkRetry + 1, MON_THREAD_NETWORK_FAIL_RETRY_IN_MS, NULL);
                    if (0 == uTimerFailedNetworkRetry)
                    {
                        ExitWithLastError(hr, "Failed to set timer for network fail retry");
                    }
                }
                ++dwFailingNetworkWaits;
                break;

            case MON_MESSAGE_NETWORK_WAIT_SUCCEEDED:
                --dwFailingNetworkWaits;
                if (0 == dwFailingNetworkWaits)
                {
                    if (!::KillTimer(NULL, uTimerFailedNetworkRetry))
                    {
                        ExitWithLastError(hr, "Failed to kill timer for network fail retry");
                    }
                    uTimerFailedNetworkRetry = 0;
                }
                break;

            case MON_MESSAGE_NETWORK_STATUS_UPDATE:
                hr = WaitForNetworkChanges(&hMonitor, pm);
                ExitOnFailure(hr, "Failed to re-wait for network changes");

                // Propagate any network status update messages to all waiter threads
                for (DWORD i = 0; i < pm->cWaiterThreads; ++i)
                {
                    pWaiterContext = pm->rgWaiterThreads[i].pWaiterContext;

                    if (!::PostThreadMessageW(pWaiterContext->dwWaiterThreadId, MON_MESSAGE_NETWORK_STATUS_UPDATE, 0, 0))
                    {
                        ExitWithLastError(hr, "Failed to send message to waiter thread to notify of network status update");
                    }

                    if (!::SetEvent(pWaiterContext->rgHandles[0]))
                    {
                        ExitWithLastError(hr, "Failed to set event to notify waiter thread of incoming network status update message");
                    }
                }
                break;

            case WM_TIMER:
                // Timer means some network wait is failing, and we need to retry every so often in case a remote server goes back up
                for (DWORD i = 0; i < pm->cWaiterThreads; ++i)
                {
                    pWaiterContext = pm->rgWaiterThreads[i].pWaiterContext;

                    if (!::PostThreadMessageW(pWaiterContext->dwWaiterThreadId, msg.wParam == uTimerFailedNetworkRetry ? MON_MESSAGE_NETWORK_RETRY_FAILED_NETWORK_WAITS : MON_MESSAGE_NETWORK_RETRY_SUCCESSFUL_NETWORK_WAITS, 0, 0))
                    {
                        ExitWithLastError(hr, "Failed to send message to waiter thread to notify of network status update");
                    }

                    if (!::SetEvent(pWaiterContext->rgHandles[0]))
                    {
                        ExitWithLastError(hr, "Failed to set event to notify waiter thread of incoming network status update message");
                    }
                }
                break;

            case MON_MESSAGE_DRIVE_STATUS_UPDATE:
                // If user requested to be notified of drive status updates, notify!
                if (pm->vpfMonDriveStatus)
                {
                    pm->vpfMonDriveStatus(static_cast<WCHAR>(msg.wParam), static_cast<BOOL>(msg.lParam), pm->pvContext);
                }

                // Propagate any drive status update messages to all waiter threads
                for (DWORD i = 0; i < pm->cWaiterThreads; ++i)
                {
                    pWaiterContext = pm->rgWaiterThreads[i].pWaiterContext;

                    if (!::PostThreadMessageW(pWaiterContext->dwWaiterThreadId, MON_MESSAGE_DRIVE_STATUS_UPDATE, msg.wParam, msg.lParam))
                    {
                        ExitWithLastError(hr, "Failed to send message to waiter thread to notify of drive status update");
                    }

                    if (!::SetEvent(pWaiterContext->rgHandles[0]))
                    {
                        ExitWithLastError(hr, "Failed to set event to notify waiter thread of incoming drive status update message");
                    }
                }
                break;

            case MON_MESSAGE_STOP:
                ExitFunction1(hr = static_cast<HRESULT>(msg.wParam));

            default:
                // This thread owns a window, so this handles all the other random messages we get
                ::TranslateMessage(&msg);
                ::DispatchMessageW(&msg);
                break;
            }
        }
    }

LExit:
    if (uTimerFailedNetworkRetry)
    {
        fRet = ::KillTimer(NULL, uTimerFailedNetworkRetry);
    }
    if (uTimerSuccessfulNetworkRetry)
    {
        fRet = ::KillTimer(NULL, uTimerSuccessfulNetworkRetry);
    }

    if (pm->hwnd)
    {
        ::CloseWindow(pm->hwnd);
    }

    // Tell all waiter threads to shutdown
    for (DWORD i = 0; i < pm->cWaiterThreads; ++i)
    {
        pWaiterContext = pm->rgWaiterThreads[i].pWaiterContext;
        if (NULL != pWaiterContext->rgHandles[0])
        {
            if (!::PostThreadMessageW(pWaiterContext->dwWaiterThreadId, MON_MESSAGE_STOP, msg.wParam, msg.lParam))
            {
                TraceError(HRESULT_FROM_WIN32(::GetLastError()), "Failed to send message to waiter thread to stop");
            }

            if (!::SetEvent(pWaiterContext->rgHandles[0]))
            {
                TraceError(HRESULT_FROM_WIN32(::GetLastError()), "Failed to set event to notify waiter thread of incoming message");
            }
        }
    }

    if (hMonitor != NULL)
    {
        ::WSALookupServiceEnd(hMonitor);
    }

    // Now confirm they're actually shut down before returning
    for (DWORD i = 0; i < pm->cWaiterThreads; ++i)
    {
        pWaiterContext = pm->rgWaiterThreads[i].pWaiterContext;
        if (NULL != pWaiterContext->hWaiterThread)
        {
            ::WaitForSingleObject(pWaiterContext->hWaiterThread, INFINITE);
            ::CloseHandle(pWaiterContext->hWaiterThread);
        }

        // Waiter thread can't release these, because coordinator thread uses it to try communicating with waiter thread
        ReleaseHandle(pWaiterContext->rgHandles[0]);
        ReleaseMem(pWaiterContext->rgHandles);

        ReleaseMem(pWaiterContext);
    }

    if (FAILED(hr))
    {
        // If coordinator thread fails, notify general callback of an error
        Assert(pm->vpfMonGeneral);
        pm->vpfMonGeneral(hr, pm->pvContext);
    }
    MonRemoveMessageDestroy(pRemoveMessage);
    MonRemoveMessageDestroy(pTempRemoveMessage);

    ::WSACleanup();

    return hr;
}

static HRESULT InitiateWait(
    __inout MON_REQUEST *pRequest,
    __inout HANDLE *pHandle
    )
{
    HRESULT hr = S_OK;
    HRESULT hrTemp = S_OK;
    DEV_BROADCAST_HANDLE dev = { };
    BOOL fRedo = FALSE;
    BOOL fHandleFound;
    DWORD er = ERROR_SUCCESS;
    DWORD dwIndex = 0;
    HKEY hk = NULL;
    HANDLE hTemp = INVALID_HANDLE_VALUE;

    if (pRequest->hNotify)
    {
        UnregisterDeviceNotification(pRequest->hNotify);
        pRequest->hNotify = NULL;
    }

    do
    {
        fRedo = FALSE;
        fHandleFound = FALSE;

        for (DWORD i = 0; i < pRequest->cPathHierarchy && !fHandleFound; ++i)
        {
            dwIndex = pRequest->cPathHierarchy - i - 1;
            switch (pRequest->type)
            {
            case MON_DIRECTORY:
                if (INVALID_HANDLE_VALUE != *pHandle)
                {
                    ::FindCloseChangeNotification(*pHandle);
                    *pHandle = INVALID_HANDLE_VALUE;
                }

                *pHandle = ::FindFirstChangeNotificationW(pRequest->rgsczPathHierarchy[dwIndex], GetRecursiveFlag(pRequest, dwIndex), FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_SECURITY);
                if (INVALID_HANDLE_VALUE == *pHandle)
                {
                    hr = HRESULT_FROM_WIN32(::GetLastError());
                    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr || E_ACCESSDENIED == hr)
                    {
                        continue;
                    }
                    ExitOnWin32Error(er, hr, "Failed to wait on path %ls", pRequest->rgsczPathHierarchy[dwIndex]);
                }
                else
                {
                    fHandleFound = TRUE;
                    hr = S_OK;
                }
                break;
            case MON_REGKEY:
                ReleaseRegKey(pRequest->regkey.hkSubKey);
                hr = RegOpen(pRequest->regkey.hkRoot, pRequest->rgsczPathHierarchy[dwIndex], KEY_NOTIFY | GetRegKeyBitness(pRequest), &pRequest->regkey.hkSubKey);
                if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
                {
                    continue;
                }
                ExitOnFailure(hr, "Failed to open regkey %ls", pRequest->rgsczPathHierarchy[dwIndex]);

                er = ::RegNotifyChangeKeyValue(pRequest->regkey.hkSubKey, GetRecursiveFlag(pRequest, dwIndex), REG_NOTIFY_CHANGE_NAME | REG_NOTIFY_CHANGE_LAST_SET | REG_NOTIFY_CHANGE_SECURITY, *pHandle, TRUE);
                ReleaseRegKey(hk);
                hr = HRESULT_FROM_WIN32(er);
                if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr || HRESULT_FROM_WIN32(ERROR_KEY_DELETED) == hr)
                {
                    continue;
                }
                else
                {
                    ExitOnWin32Error(er, hr, "Failed to wait on subkey %ls", pRequest->rgsczPathHierarchy[dwIndex]);

                    fHandleFound = TRUE;
                }

                break;
            default:
                return E_INVALIDARG;
            }
        }

        pRequest->dwPathHierarchyIndex = dwIndex;

        // If we're monitoring a parent instead of the real path because the real path didn't exist, double-check the child hasn't been created since.
        // If it has, restart the whole loop
        if (dwIndex < pRequest->cPathHierarchy - 1)
        {
            switch (pRequest->type)
            {
            case MON_DIRECTORY:
                hTemp = ::FindFirstChangeNotificationW(pRequest->rgsczPathHierarchy[dwIndex + 1], GetRecursiveFlag(pRequest, dwIndex + 1), FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_SECURITY);
                if (INVALID_HANDLE_VALUE != hTemp)
                {
                    ::FindCloseChangeNotification(hTemp);
                    fRedo = TRUE;
                }
                break;
            case MON_REGKEY:
                hrTemp = RegOpen(pRequest->regkey.hkRoot, pRequest->rgsczPathHierarchy[dwIndex + 1], KEY_NOTIFY | GetRegKeyBitness(pRequest), &hk);
                ReleaseRegKey(hk);
                fRedo = SUCCEEDED(hrTemp);
                break;
            default:
                Assert(false);
            }
        }
    } while (fRedo);

    ExitOnFailure(hr, "Didn't get a successful wait after looping through all available options %ls", pRequest->rgsczPathHierarchy[pRequest->cPathHierarchy - 1]);

    if (MON_DIRECTORY == pRequest->type)
    {
        dev.dbch_size = sizeof(dev);
        dev.dbch_devicetype = DBT_DEVTYP_HANDLE;
        dev.dbch_handle = *pHandle;
        // Ignore failure on this - some drives by design don't support it (like network paths), and the worst that can happen is a
        // removable device will be left in use so user cannot gracefully remove
        pRequest->hNotify = RegisterDeviceNotification(pRequest->hwnd, &dev, DEVICE_NOTIFY_WINDOW_HANDLE);
    }

LExit:
    ReleaseRegKey(hk);

    return hr;
}

static DWORD WINAPI WaiterThread(
    __in_bcount(sizeof(MON_WAITER_CONTEXT)) LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    HRESULT hrTemp = S_OK;
    DWORD dwRet = 0;
    BOOL fAgain = FALSE;
    BOOL fContinue = TRUE;
    BOOL fNotify = FALSE;
    BOOL fRet = FALSE;
    MSG msg = { };
    MON_ADD_MESSAGE *pAddMessage = NULL;
    MON_REMOVE_MESSAGE *pRemoveMessage = NULL;
    MON_WAITER_CONTEXT *pWaiterContext = reinterpret_cast<MON_WAITER_CONTEXT *>(pvContext);
    DWORD dwRequestIndex;
    DWORD dwNewRequestIndex;
    // If we have one or more requests pending notification, this is the period we intend to wait for multiple objects (shortest amount of time to next potential notify)
    DWORD dwWait = 0;
    DWORD uCurrentTime = 0;
    DWORD uLastTimeInMs = ::GetTickCount();
    DWORD uDeltaInMs = 0;
    DWORD cRequestsPendingBeforeLoop = 0;
    LPWSTR sczDirectory = NULL;
    bool rgfProcessedIndex[MON_MAX_MONITORS_PER_THREAD + 1] = { };
    MON_INTERNAL_TEMPORARY_WAIT * pInternalWait = NULL;

    // Ensure the thread has a message queue
    ::PeekMessage(&msg, NULL, WM_USER, WM_USER, PM_NOREMOVE);
    pWaiterContext->fWaiterThreadMessageQueueInitialized = TRUE;

    do
    {
        dwRet = ::WaitForMultipleObjects(pWaiterContext->cHandles - pWaiterContext->cRequestsFailing, pWaiterContext->rgHandles, FALSE, pWaiterContext->cRequestsPending > 0 ? dwWait : INFINITE);

        uCurrentTime = ::GetTickCount();
        uDeltaInMs = uCurrentTime - uLastTimeInMs;
        uLastTimeInMs = uCurrentTime;

        if (WAIT_OBJECT_0 == dwRet)
        {
            do
            {
                fRet = ::PeekMessage(&msg, reinterpret_cast<HWND>(-1), 0, 0, PM_REMOVE);
                fAgain = fRet;
                if (fRet)
                {
                    switch (msg.message)
                    {
                        case MON_MESSAGE_ADD:
                            pAddMessage = reinterpret_cast<MON_ADD_MESSAGE *>(msg.wParam);

                            // Don't just blindly put it at the end of the array - it must be before any failing requests
                            // for WaitForMultipleObjects() to succeed
                            dwNewRequestIndex = pWaiterContext->cRequests - pWaiterContext->cRequestsFailing;
                            if (FAILED(pAddMessage->request.hrStatus))
                            {
                                ++pWaiterContext->cRequestsFailing;
                            }

                            hr = MemInsertIntoArray(reinterpret_cast<void **>(&pWaiterContext->rgHandles), dwNewRequestIndex + 1, 1, pWaiterContext->cHandles, sizeof(HANDLE), MON_ARRAY_GROWTH);
                            ExitOnFailure(hr, "Failed to insert additional handle");
                            ++pWaiterContext->cHandles;

                            // Ugh - directory types start with INVALID_HANDLE_VALUE instead of NULL
                            if (MON_DIRECTORY == pAddMessage->request.type)
                            {
                                pWaiterContext->rgHandles[dwNewRequestIndex + 1] = INVALID_HANDLE_VALUE;
                            }

                            hr = MemInsertIntoArray(reinterpret_cast<void **>(&pWaiterContext->rgRequests), dwNewRequestIndex, 1, pWaiterContext->cRequests, sizeof(MON_REQUEST), MON_ARRAY_GROWTH);
                            ExitOnFailure(hr, "Failed to insert additional request struct");
                            ++pWaiterContext->cRequests;

                            pWaiterContext->rgRequests[dwNewRequestIndex] = pAddMessage->request;
                            pWaiterContext->rgHandles[dwNewRequestIndex + 1] = pAddMessage->handle;

                            ReleaseNullMem(pAddMessage);
                            break;

                        case MON_MESSAGE_REMOVE:
                            pRemoveMessage = reinterpret_cast<MON_REMOVE_MESSAGE *>(msg.wParam);

                            // Find the request to remove
                            hr = FindRequestIndex(pWaiterContext, pRemoveMessage, &dwRequestIndex);
                            if (E_NOTFOUND == hr)
                            {
                                // Coordinator sends removes blindly to all waiter threads, so maybe this one wasn't intended for us
                                hr = S_OK;
                            }
                            else
                            {
                                ExitOnFailure(hr, "Failed to find request index for remove message");

                                hr = RemoveRequest(pWaiterContext, dwRequestIndex);
                                ExitOnFailure(hr, "Failed to remove request after request from coordinator thread.");
                            }

                            MonRemoveMessageDestroy(pRemoveMessage);
                            pRemoveMessage = NULL;
                            break;

                        case MON_MESSAGE_NETWORK_RETRY_FAILED_NETWORK_WAITS:
                            if (::PeekMessage(&msg, NULL, MON_MESSAGE_NETWORK_RETRY_FAILED_NETWORK_WAITS, MON_MESSAGE_NETWORK_RETRY_FAILED_NETWORK_WAITS, PM_NOREMOVE))
                            {
                                // If there is another a pending retry failed wait message, skip this one
                                continue;
                            }

                            ZeroMemory(rgfProcessedIndex, sizeof(rgfProcessedIndex));
                            for (DWORD i = 0; i < pWaiterContext->cRequests; ++i)
                            { 
                                if (rgfProcessedIndex[i])
                                {
                                    // if we already processed this item due to UpdateWaitStatus swapping array indices, then skip it
                                    continue;
                                }

                                if (MON_DIRECTORY == pWaiterContext->rgRequests[i].type && pWaiterContext->rgRequests[i].fNetwork && FAILED(pWaiterContext->rgRequests[i].hrStatus))
                                {
                                    // This is not a failure, just record this in the request's status
                                    hrTemp = InitiateWait(pWaiterContext->rgRequests + i, pWaiterContext->rgHandles + i + 1);

                                    hr = UpdateWaitStatus(hrTemp, pWaiterContext, i, &dwNewRequestIndex);
                                    ExitOnFailure(hr, "Failed to update wait status");
                                    hrTemp = S_OK;

                                    if (dwNewRequestIndex != i)
                                    {
                                        // If this request was moved to the end of the list, reprocess this index and mark the new index for skipping
                                        rgfProcessedIndex[dwNewRequestIndex] = true;
                                        --i;
                                    }
                                }
                            }
                        break;

                        case MON_MESSAGE_NETWORK_RETRY_SUCCESSFUL_NETWORK_WAITS:
                            if (::PeekMessage(&msg, NULL, MON_MESSAGE_NETWORK_RETRY_SUCCESSFUL_NETWORK_WAITS, MON_MESSAGE_NETWORK_RETRY_SUCCESSFUL_NETWORK_WAITS, PM_NOREMOVE))
                            {
                                // If there is another a pending retry successful wait message, skip this one
                                continue;
                            }

                            ZeroMemory(rgfProcessedIndex, sizeof(rgfProcessedIndex));
                            for (DWORD i = 0; i < pWaiterContext->cRequests; ++i)
                            { 
                                if (rgfProcessedIndex[i])
                                {
                                    // if we already processed this item due to UpdateWaitStatus swapping array indices, then skip it
                                    continue;
                                }

                                if (MON_DIRECTORY == pWaiterContext->rgRequests[i].type && pWaiterContext->rgRequests[i].fNetwork && SUCCEEDED(pWaiterContext->rgRequests[i].hrStatus))
                                {
                                    // This is not a failure, just record this in the request's status
                                    hrTemp = InitiateWait(pWaiterContext->rgRequests + i, pWaiterContext->rgHandles + i + 1);

                                    hr = UpdateWaitStatus(hrTemp, pWaiterContext, i, &dwNewRequestIndex);
                                    ExitOnFailure(hr, "Failed to update wait status");
                                    hrTemp = S_OK;

                                    if (dwNewRequestIndex != i)
                                    {
                                        // If this request was moved to the end of the list, reprocess this index and mark the new index for skipping
                                        rgfProcessedIndex[dwNewRequestIndex] = true;
                                        --i;
                                    }
                                }
                            }
                            break;

                        case MON_MESSAGE_NETWORK_STATUS_UPDATE:
                            if (::PeekMessage(&msg, NULL, MON_MESSAGE_NETWORK_STATUS_UPDATE, MON_MESSAGE_NETWORK_STATUS_UPDATE, PM_NOREMOVE))
                            {
                                // If there is another a pending network status update message, skip this one
                                continue;
                            }

                            ZeroMemory(rgfProcessedIndex, sizeof(rgfProcessedIndex));
                            for (DWORD i = 0; i < pWaiterContext->cRequests; ++i)
                            { 
                                if (rgfProcessedIndex[i])
                                {
                                    // if we already processed this item due to UpdateWaitStatus swapping array indices, then skip it
                                    continue;
                                }

                                if (MON_DIRECTORY == pWaiterContext->rgRequests[i].type && pWaiterContext->rgRequests[i].fNetwork)
                                {
                                    // Failures here get recorded in the request's status
                                    hrTemp = InitiateWait(pWaiterContext->rgRequests + i, pWaiterContext->rgHandles + i + 1);

                                    hr = UpdateWaitStatus(hrTemp, pWaiterContext, i, &dwNewRequestIndex);
                                    ExitOnFailure(hr, "Failed to update wait status");
                                    hrTemp = S_OK;

                                    if (dwNewRequestIndex != i)
                                    {
                                        // If this request was moved to the end of the list, reprocess this index and mark the new index for skipping
                                        rgfProcessedIndex[dwNewRequestIndex] = true;
                                        --i;
                                    }
                                }
                            }
                            break;

                        case MON_MESSAGE_DRIVE_STATUS_UPDATE:
                            ZeroMemory(rgfProcessedIndex, sizeof(rgfProcessedIndex));
                            for (DWORD i = 0; i < pWaiterContext->cRequests; ++i)
                            { 
                                if (rgfProcessedIndex[i])
                                {
                                    // if we already processed this item due to UpdateWaitStatus swapping array indices, then skip it
                                    continue;
                                }

                                if (MON_DIRECTORY == pWaiterContext->rgRequests[i].type && pWaiterContext->rgRequests[i].sczOriginalPathRequest[0] == static_cast<WCHAR>(msg.wParam))
                                {
                                    // Failures here get recorded in the request's status
                                    if (static_cast<BOOL>(msg.lParam))
                                    {
                                        hrTemp = InitiateWait(pWaiterContext->rgRequests + i, pWaiterContext->rgHandles + i + 1);
                                    }
                                    else
                                    {
                                        // If the message says the drive is disconnected, don't even try to wait, just mark it as gone
                                        hrTemp = E_PATHNOTFOUND;
                                    }

                                    hr = UpdateWaitStatus(hrTemp, pWaiterContext, i, &dwNewRequestIndex);
                                    ExitOnFailure(hr, "Failed to update wait status");
                                    hrTemp = S_OK;

                                    if (dwNewRequestIndex != i)
                                    {
                                        // If this request was moved to the end of the list, reprocess this index and mark the new index for skipping
                                        rgfProcessedIndex[dwNewRequestIndex] = true;
                                        --i;
                                    }
                                }
                            }
                            break;

                        case MON_MESSAGE_DRIVE_QUERY_REMOVE:
                            pInternalWait = reinterpret_cast<MON_INTERNAL_TEMPORARY_WAIT *>(msg.wParam);
                            // Only do any work if message is not yet out of date
                            // While it could become out of date while doing this processing, sending thread will check response to guard against this
                            if (pInternalWait->dwSendIteration == static_cast<DWORD>(msg.lParam))
                            {
                                for (DWORD i = 0; i < pWaiterContext->cRequests; ++i)
                                { 
                                    if (MON_DIRECTORY == pWaiterContext->rgRequests[i].type && pWaiterContext->rgHandles[i + 1] == reinterpret_cast<HANDLE>(pInternalWait->pvContext))
                                    {
                                        // Release handles ASAP so the remove request will succeed
                                        if (pWaiterContext->rgRequests[i].hNotify)
                                        {
                                            UnregisterDeviceNotification(pWaiterContext->rgRequests[i].hNotify);
                                            pWaiterContext->rgRequests[i].hNotify = NULL;
                                        }
                                        ::FindCloseChangeNotification(pWaiterContext->rgHandles[i + 1]);
                                        pWaiterContext->rgHandles[i + 1] = INVALID_HANDLE_VALUE;

                                        // Reply to unblock our reply to the remove request
                                        pInternalWait->dwReceiveIteration = static_cast<DWORD>(msg.lParam);
                                        if (!::SetEvent(pInternalWait->hWait))
                                        {
                                            TraceError(HRESULT_FROM_WIN32(::GetLastError()), "Failed to set event to notify coordinator thread that removable device handle was released, this could be due to wndproc no longer waiting for waiter thread's response");
                                        }

                                        // Drive is disconnecting, don't even try to wait, just mark it as gone
                                        hrTemp = E_PATHNOTFOUND;

                                        hr = UpdateWaitStatus(hrTemp, pWaiterContext, i, &dwNewRequestIndex);
                                        ExitOnFailure(hr, "Failed to update wait status");
                                        hrTemp = S_OK;
                                        break;
                                    }
                                }
                            }
                            break;

                        case MON_MESSAGE_STOP:
                            // Stop requested, so abort the whole thread
                            Trace(REPORT_DEBUG, "Waiter thread was told to stop");
                            fAgain = FALSE;
                            fContinue = FALSE;
                            ExitFunction1(hr = static_cast<HRESULT>(msg.wParam));

                        default:
                            Assert(false);
                            break;
                    }
                }
            } while (fAgain);
        }
        else if (dwRet > WAIT_OBJECT_0 && dwRet - WAIT_OBJECT_0 < pWaiterContext->cHandles)
        {
            // OK a handle fired - only notify if it's the actual target, and not just some parent waiting for the target child to exist
            dwRequestIndex = dwRet - WAIT_OBJECT_0 - 1;
            fNotify = (pWaiterContext->rgRequests[dwRequestIndex].dwPathHierarchyIndex == pWaiterContext->rgRequests[dwRequestIndex].cPathHierarchy - 1);

            // Initiate re-waits before we notify callback, to ensure we don't miss a single update
            hrTemp = InitiateWait(pWaiterContext->rgRequests + dwRequestIndex, pWaiterContext->rgHandles + dwRequestIndex + 1);
            hr = UpdateWaitStatus(hrTemp, pWaiterContext, dwRequestIndex, &dwRequestIndex);
            ExitOnFailure(hr, "Failed to update wait status");
            hrTemp = S_OK;

            // If there were no errors and we were already waiting on the right target, or if we weren't yet but are able to now, it's a successful notify
            if (SUCCEEDED(pWaiterContext->rgRequests[dwRequestIndex].hrStatus) && (fNotify || (pWaiterContext->rgRequests[dwRequestIndex].dwPathHierarchyIndex == pWaiterContext->rgRequests[dwRequestIndex].cPathHierarchy - 1)))
            {
                Trace(REPORT_DEBUG, "Changes detected, waiting for silence period index %u", dwRequestIndex);

                if (0 < pWaiterContext->rgRequests[dwRequestIndex].dwMaxSilencePeriodInMs)
                {
                    pWaiterContext->rgRequests[dwRequestIndex].dwSilencePeriodInMs = 0;
                    pWaiterContext->rgRequests[dwRequestIndex].fSkipDeltaAdd = TRUE;

                    if (!pWaiterContext->rgRequests[dwRequestIndex].fPendingFire)
                    {
                        pWaiterContext->rgRequests[dwRequestIndex].fPendingFire = TRUE;
                        ++pWaiterContext->cRequestsPending;
                    }
                }
                else
                {
                    // If no silence period, notify immediately
                    Notify(S_OK, pWaiterContext, pWaiterContext->rgRequests + dwRequestIndex);
                }
            }
        }
        else if (WAIT_TIMEOUT != dwRet)
        {
            ExitWithLastError(hr, "Failed to wait for multiple objects with return code %u", dwRet);
        }

        // OK, now that we've checked all triggered handles (resetting silence period timers appropriately), check for any pending notifications that we can finally fire
        // And set dwWait appropriately so we awaken at the right time to fire the next pending notification (in case no further writes occur during that time)
        if (0 < pWaiterContext->cRequestsPending)
        {
            // Start at max value and find the lowest wait we can below that
            dwWait = DWORD_MAX;
            cRequestsPendingBeforeLoop = pWaiterContext->cRequestsPending;

            for (DWORD i = 0; i < pWaiterContext->cRequests; ++i)
            {
                if (pWaiterContext->rgRequests[i].fPendingFire)
                {
                    if (0 == cRequestsPendingBeforeLoop)
                    {
                        Assert(FALSE);
                        hr = HRESULT_FROM_WIN32(ERROR_EA_LIST_INCONSISTENT);
                        ExitOnFailure(hr, "Phantom pending fires were found!");
                    }
                    --cRequestsPendingBeforeLoop;

                    dwRequestIndex = i;

                    if (pWaiterContext->rgRequests[dwRequestIndex].fSkipDeltaAdd)
                    {
                        pWaiterContext->rgRequests[dwRequestIndex].fSkipDeltaAdd = FALSE;
                    }
                    else
                    {
                        pWaiterContext->rgRequests[dwRequestIndex].dwSilencePeriodInMs += uDeltaInMs;
                    }

                    // silence period has elapsed without further notifications, so reset pending-related variables, and finally fire a notify!
                    if (pWaiterContext->rgRequests[dwRequestIndex].dwSilencePeriodInMs >= pWaiterContext->rgRequests[dwRequestIndex].dwMaxSilencePeriodInMs)
                    {
                        Trace(REPORT_DEBUG, "Silence period surpassed, notifying %u ms late", pWaiterContext->rgRequests[dwRequestIndex].dwSilencePeriodInMs - pWaiterContext->rgRequests[dwRequestIndex].dwMaxSilencePeriodInMs);
                        Notify(S_OK, pWaiterContext, pWaiterContext->rgRequests + dwRequestIndex);
                    }
                    else
                    {
                        // set dwWait to the shortest interval period so that if no changes occur, WaitForMultipleObjects
                        // wakes the thread back up when it's time to fire the next pending notification
                        if (dwWait > pWaiterContext->rgRequests[dwRequestIndex].dwMaxSilencePeriodInMs - pWaiterContext->rgRequests[dwRequestIndex].dwSilencePeriodInMs)
                        {
                            dwWait = pWaiterContext->rgRequests[dwRequestIndex].dwMaxSilencePeriodInMs - pWaiterContext->rgRequests[dwRequestIndex].dwSilencePeriodInMs;
                        }
                    }
                }
            }

            // Some post-loop list validation for sanity checking
            if (0 < cRequestsPendingBeforeLoop)
            {
                Assert(FALSE);
                hr = HRESULT_FROM_WIN32(PEERDIST_ERROR_MISSING_DATA);
                ExitOnFailure(hr, "Missing %u pending fires! Total pending fires: %u, wait: %u", cRequestsPendingBeforeLoop, pWaiterContext->cRequestsPending, dwWait);
            }
            if (0 < pWaiterContext->cRequestsPending && DWORD_MAX == dwWait)
            {
                Assert(FALSE);
                hr = HRESULT_FROM_WIN32(ERROR_CANT_WAIT);
                ExitOnFailure(hr, "Pending fires exist, but wait was infinite", cRequestsPendingBeforeLoop);
            }
        }
    } while (fContinue);

    // Don't bother firing pending notifications. We were told to stop monitoring, so client doesn't care.

LExit:
    ReleaseStr(sczDirectory);
    MonAddMessageDestroy(pAddMessage);
    MonRemoveMessageDestroy(pRemoveMessage);

    for (DWORD i = 0; i < pWaiterContext->cRequests; ++i)
    {
        MonRequestDestroy(pWaiterContext->rgRequests + i);

        switch (pWaiterContext->rgRequests[i].type)
        {
        case MON_DIRECTORY:
            if (INVALID_HANDLE_VALUE != pWaiterContext->rgHandles[i + 1])
            {
                ::FindCloseChangeNotification(pWaiterContext->rgHandles[i + 1]);
            }
            break;
        case MON_REGKEY:
            ReleaseHandle(pWaiterContext->rgHandles[i + 1]);
            break;
        default:
            Assert(false);
        }
    }

    if (FAILED(hr))
    {
        // If waiter thread fails, notify general callback of an error
        Assert(pWaiterContext->vpfMonGeneral);
        pWaiterContext->vpfMonGeneral(hr, pWaiterContext->pvContext);

        // And tell coordinator to shut all other waiters down
        if (!::PostThreadMessageW(pWaiterContext->dwCoordinatorThreadId, MON_MESSAGE_STOP, 0, 0))
        {
            TraceError(HRESULT_FROM_WIN32(::GetLastError()), "Failed to send message to coordinator thread to stop (due to general failure).");
        }
    }

    return hr;
}

static void Notify(
    __in HRESULT hr,
    __in MON_WAITER_CONTEXT *pWaiterContext,
    __in MON_REQUEST *pRequest
    )
{
    if (pRequest->fPendingFire)
    {
        --pWaiterContext->cRequestsPending;
    }

    pRequest->fPendingFire = FALSE;
    pRequest->fSkipDeltaAdd = FALSE;
    pRequest->dwSilencePeriodInMs = 0;

    switch (pRequest->type)
    {
    case MON_DIRECTORY:
        Assert(pWaiterContext->vpfMonDirectory);
        pWaiterContext->vpfMonDirectory(hr, pRequest->sczOriginalPathRequest, pRequest->fRecursive, pWaiterContext->pvContext, pRequest->pvContext);
        break;
    case MON_REGKEY:
        Assert(pWaiterContext->vpfMonRegKey);
        pWaiterContext->vpfMonRegKey(hr, pRequest->regkey.hkRoot, pRequest->rgsczPathHierarchy[pRequest->cPathHierarchy - 1], pRequest->regkey.kbKeyBitness, pRequest->fRecursive, pWaiterContext->pvContext, pRequest->pvContext);
        break;
    default:
        Assert(false);
    }
}

static BOOL GetRecursiveFlag(
    __in MON_REQUEST *pRequest,
    __in DWORD dwIndex
    )
{
    if (pRequest->cPathHierarchy - 1 == dwIndex)
    {
        return pRequest->fRecursive;
    }
    else
    {
        return FALSE;
    }
}

static HRESULT FindRequestIndex(
    __in MON_WAITER_CONTEXT *pWaiterContext,
    __in MON_REMOVE_MESSAGE *pMessage,
    __out DWORD *pdwIndex
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pWaiterContext->cRequests; ++i)
    {
        if (pWaiterContext->rgRequests[i].type == pMessage->type)
        {
            switch (pWaiterContext->rgRequests[i].type)
            {
            case MON_DIRECTORY:
                if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pWaiterContext->rgRequests[i].rgsczPathHierarchy[pWaiterContext->rgRequests[i].cPathHierarchy - 1], -1, pMessage->directory.sczDirectory, -1) && pWaiterContext->rgRequests[i].fRecursive == pMessage->fRecursive)
                {
                    *pdwIndex = i;
                    ExitFunction1(hr = S_OK);
                }
                break;
            case MON_REGKEY:
                if (reinterpret_cast<DWORD_PTR>(pMessage->regkey.hkRoot) == reinterpret_cast<DWORD_PTR>(pWaiterContext->rgRequests[i].regkey.hkRoot) && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pWaiterContext->rgRequests[i].rgsczPathHierarchy[pWaiterContext->rgRequests[i].cPathHierarchy - 1], -1, pMessage->regkey.sczSubKey, -1) && pWaiterContext->rgRequests[i].fRecursive == pMessage->fRecursive && pWaiterContext->rgRequests[i].regkey.kbKeyBitness == pMessage->regkey.kbKeyBitness)
                {
                    *pdwIndex = i;
                    ExitFunction1(hr = S_OK);
                }
                break;
            default:
                Assert(false);
            }
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

static HRESULT RemoveRequest(
    __inout MON_WAITER_CONTEXT *pWaiterContext,
    __in DWORD dwRequestIndex
    )
{
    HRESULT hr = S_OK;

    MonRequestDestroy(pWaiterContext->rgRequests + dwRequestIndex);

    switch (pWaiterContext->rgRequests[dwRequestIndex].type)
    {
    case MON_DIRECTORY:
        if (pWaiterContext->rgHandles[dwRequestIndex + 1] != INVALID_HANDLE_VALUE)
        {
            ::FindCloseChangeNotification(pWaiterContext->rgHandles[dwRequestIndex + 1]);
        }
        break;
    case MON_REGKEY:
        ReleaseHandle(pWaiterContext->rgHandles[dwRequestIndex + 1]);
        break;
    default:
        Assert(false);
    }

    if (pWaiterContext->rgRequests[dwRequestIndex].fPendingFire)
    {
        --pWaiterContext->cRequestsPending;
    }

    if (FAILED(pWaiterContext->rgRequests[dwRequestIndex].hrStatus))
    {
        --pWaiterContext->cRequestsFailing;
    }

    MemRemoveFromArray(reinterpret_cast<void *>(pWaiterContext->rgHandles), dwRequestIndex + 1, 1, pWaiterContext->cHandles, sizeof(HANDLE), TRUE);
    --pWaiterContext->cHandles;
    MemRemoveFromArray(reinterpret_cast<void *>(pWaiterContext->rgRequests), dwRequestIndex, 1, pWaiterContext->cRequests, sizeof(MON_REQUEST), TRUE);
    --pWaiterContext->cRequests;

    // Notify coordinator thread that a wait was removed
    if (!::PostThreadMessageW(pWaiterContext->dwCoordinatorThreadId, MON_MESSAGE_REMOVED, static_cast<WPARAM>(::GetCurrentThreadId()), 0))
    {
        ExitWithLastError(hr, "Failed to send message to coordinator thread to confirm directory was removed.");
    }

LExit:
    return hr;
}

static REGSAM GetRegKeyBitness(
    __in MON_REQUEST *pRequest
    )
{
    if (REG_KEY_32BIT == pRequest->regkey.kbKeyBitness)
    {
        return KEY_WOW64_32KEY;
    }
    else if (REG_KEY_64BIT == pRequest->regkey.kbKeyBitness)
    {
        return KEY_WOW64_64KEY;
    }
    else
    {
        return 0;
    }
}

static HRESULT DuplicateRemoveMessage(
    __in MON_REMOVE_MESSAGE *pMessage,
    __out MON_REMOVE_MESSAGE **ppMessage
    )
{
    HRESULT hr = S_OK;

    *ppMessage = reinterpret_cast<MON_REMOVE_MESSAGE *>(MemAlloc(sizeof(MON_REMOVE_MESSAGE), TRUE));
    ExitOnNull(*ppMessage, hr, E_OUTOFMEMORY, "Failed to allocate copy of remove message");

    (*ppMessage)->type = pMessage->type;
    (*ppMessage)->fRecursive = pMessage->fRecursive;

    switch (pMessage->type)
    {
    case MON_DIRECTORY:
        hr = StrAllocString(&(*ppMessage)->directory.sczDirectory, pMessage->directory.sczDirectory, 0);
        ExitOnFailure(hr, "Failed to copy directory");
        break;
    case MON_REGKEY:
        (*ppMessage)->regkey.hkRoot = pMessage->regkey.hkRoot;
        (*ppMessage)->regkey.kbKeyBitness = pMessage->regkey.kbKeyBitness;
        hr = StrAllocString(&(*ppMessage)->regkey.sczSubKey, pMessage->regkey.sczSubKey, 0);
        ExitOnFailure(hr, "Failed to copy subkey");
        break;
    default:
        Assert(false);
        break;
    }

LExit:
    return hr;
}

static LRESULT CALLBACK MonWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    HRESULT hr = S_OK;
    DEV_BROADCAST_HDR *pHdr = NULL;
    DEV_BROADCAST_HANDLE *pHandle = NULL;
    DEV_BROADCAST_VOLUME *pVolume = NULL;
    DWORD dwUnitMask = 0;
    DWORD er = ERROR_SUCCESS;
    WCHAR chDrive = L'\0';
    BOOL fArrival = FALSE;
    BOOL fReturnTrue = FALSE;
    CREATESTRUCT *pCreateStruct = NULL;
    MON_WAITER_CONTEXT *pWaiterContext = NULL;
    MON_STRUCT *pm = NULL;

    // keep track of the MON_STRUCT pointer that was passed in on init, associate it with the window
    if (WM_CREATE == uMsg)
    {
        pCreateStruct = reinterpret_cast<CREATESTRUCT *>(lParam);
        if (pCreateStruct)
        {
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pCreateStruct->lpCreateParams));
        }
    }
    else if (WM_NCDESTROY == uMsg)
    {
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
    }

    // Note this message ONLY comes in through WndProc, it isn't visible from the GetMessage loop.
    else if (WM_DEVICECHANGE == uMsg)
    {
        if (DBT_DEVICEARRIVAL == wParam || DBT_DEVICEREMOVECOMPLETE == wParam)
        {
            fArrival = DBT_DEVICEARRIVAL == wParam;

            pHdr = reinterpret_cast<DEV_BROADCAST_HDR*>(lParam);
            if (DBT_DEVTYP_VOLUME == pHdr->dbch_devicetype)
            {
                pVolume = reinterpret_cast<DEV_BROADCAST_VOLUME*>(lParam);
                dwUnitMask = pVolume->dbcv_unitmask;
                chDrive = L'a';
                while (0 < dwUnitMask)
                {
                    if (dwUnitMask & 0x1)
                    {
                        // This drive had a status update, so send it out to all threads
                        if (!::PostThreadMessageW(::GetCurrentThreadId(), MON_MESSAGE_DRIVE_STATUS_UPDATE, static_cast<WPARAM>(chDrive), static_cast<LPARAM>(fArrival)))
                        {
                            ExitWithLastError(hr, "Failed to send drive status update with drive %wc and arrival %ls", chDrive, fArrival ? L"TRUE" : L"FALSE");
                        }
                    }
                    dwUnitMask >>= 1;
                    ++chDrive;

                    if (chDrive == 'z')
                    {
                        hr = E_UNEXPECTED;
                        ExitOnFailure(hr, "UnitMask showed drives beyond z:. Remaining UnitMask at this point: %u", dwUnitMask);
                    }
                }
            }
        }
        // We can only process device query remove messages if we have a MON_STRUCT pointer
        else if (DBT_DEVICEQUERYREMOVE == wParam)
        {
            pm = reinterpret_cast<MON_STRUCT*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));
            if (!pm)
            {
                hr = E_POINTER;
                ExitOnFailure(hr, "DBT_DEVICEQUERYREMOVE message received with no MON_STRUCT pointer, so message was ignored");
            }

            fReturnTrue = TRUE;

            pHdr = reinterpret_cast<DEV_BROADCAST_HDR*>(lParam);
            if (DBT_DEVTYP_HANDLE == pHdr->dbch_devicetype)
            {
                // We must wait for the actual wait handle to be released by waiter thread before telling windows to proceed with device removal, otherwise it could fail
                // due to handles still being open, so use a MON_INTERNAL_TEMPORARY_WAIT struct to send and receive a reply from a waiter thread
                pm->internalWait.hWait = ::CreateEventW(NULL, TRUE, FALSE, NULL);
                ExitOnNullWithLastError(pm->internalWait.hWait, hr, "Failed to create anonymous event for waiter to notify wndproc device can be removed");

                pHandle = reinterpret_cast<DEV_BROADCAST_HANDLE*>(lParam);
                pm->internalWait.pvContext = pHandle->dbch_handle;
                pm->internalWait.dwReceiveIteration = pm->internalWait.dwSendIteration - 1;
                // This drive had a status update, so send it out to all threads
                for (DWORD i = 0; i < pm->cWaiterThreads; ++i)
                {
                    pWaiterContext = pm->rgWaiterThreads[i].pWaiterContext;

                    if (!::PostThreadMessageW(pWaiterContext->dwWaiterThreadId, MON_MESSAGE_DRIVE_QUERY_REMOVE, reinterpret_cast<WPARAM>(&pm->internalWait), static_cast<LPARAM>(pm->internalWait.dwSendIteration)))
                    {
                        ExitWithLastError(hr, "Failed to send message to waiter thread to notify of drive query remove");
                    }

                    if (!::SetEvent(pWaiterContext->rgHandles[0]))
                    {
                        ExitWithLastError(hr, "Failed to set event to notify waiter thread of incoming drive query remove message");
                    }
                }

                er = ::WaitForSingleObject(pm->internalWait.hWait, MON_THREAD_WAIT_REMOVE_DEVICE);
                // Make sure any waiter thread processing really old messages can immediately know that we're no longer waiting for a response
                if (WAIT_OBJECT_0 == er)
                {
                    // If the response ID matches what we sent, we actually got a valid reply!
                    if (pm->internalWait.dwReceiveIteration != pm->internalWait.dwSendIteration)
                    {
                        TraceError(HRESULT_FROM_WIN32(er), "Waiter thread received wrong ID reply");
                    }
                }
                else if (WAIT_TIMEOUT == er)
                {
                    TraceError(HRESULT_FROM_WIN32(er), "No response from any waiter thread for query remove message");
                }
                else
                {
                    ExitWithLastError(hr, "WaitForSingleObject failed with non-timeout reason while waiting for response from waiter thread");
                }
                ++pm->internalWait.dwSendIteration;
            }
        }
    }

LExit:
    if (pm)
    {
        ReleaseHandle(pm->internalWait.hWait);
    }

    if (fReturnTrue)
    {
        return TRUE;
    }
    else
    {
        return ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
    }
}

static HRESULT CreateMonWindow(
    __in MON_STRUCT *pm,
    __out HWND *pHwnd
    )
{
    HRESULT hr = S_OK;
    WNDCLASSW wc = { };

    wc.lpfnWndProc = MonWndProc;
    wc.hInstance = ::GetModuleHandleW(NULL);
    wc.lpszClassName = MONUTIL_WINDOW_CLASS;
    if (!::RegisterClassW(&wc))
    {
        if (ERROR_CLASS_ALREADY_EXISTS != ::GetLastError())
        {
            ExitWithLastError(hr, "Failed to register MonUtil window class.");
        }
    }

    *pHwnd = ::CreateWindowExW(0, wc.lpszClassName, L"", 0, CW_USEDEFAULT, CW_USEDEFAULT, 0, 0, HWND_DESKTOP, NULL, wc.hInstance, pm);
    ExitOnNullWithLastError(*pHwnd, hr, "Failed to create window.");

    // Rumor has it that drive arrival / removal events can be lost in the rare event that some other application higher up in z-order is hanging if we don't make our window topmost
    // SWP_NOACTIVATE is important so the currently active window doesn't lose focus
    SetWindowPos(*pHwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_DEFERERASE | SWP_NOACTIVATE);

LExit:
    return hr;
}

static HRESULT WaitForNetworkChanges(
    __inout HANDLE *phMonitor,
    __in MON_STRUCT *pm
    )
{
    HRESULT hr = S_OK;
    int nResult = 0;
    DWORD dwBytesReturned = 0;
    WSACOMPLETION wsaCompletion = { };
    WSAQUERYSET qsRestrictions = { };

    qsRestrictions.dwSize = sizeof(WSAQUERYSET);
    qsRestrictions.dwNameSpace = NS_NLA;

    if (NULL != *phMonitor)
    {
        ::WSALookupServiceEnd(*phMonitor);
        *phMonitor = NULL;
    }

    if (::WSALookupServiceBegin(&qsRestrictions, LUP_RETURN_ALL, phMonitor))
    {
        hr = HRESULT_FROM_WIN32(::WSAGetLastError());
        ExitOnFailure(hr, "WSALookupServiceBegin() failed");
    }

    wsaCompletion.Type = NSP_NOTIFY_HWND;
    wsaCompletion.Parameters.WindowMessage.hWnd = pm->hwnd;
    wsaCompletion.Parameters.WindowMessage.uMsg = MON_MESSAGE_NETWORK_STATUS_UPDATE;
    nResult = ::WSANSPIoctl(*phMonitor, SIO_NSP_NOTIFY_CHANGE, NULL, 0, NULL, 0, &dwBytesReturned, &wsaCompletion);
    if (SOCKET_ERROR != nResult || WSA_IO_PENDING != ::WSAGetLastError())
    {
        hr = HRESULT_FROM_WIN32(::WSAGetLastError());
        if (SUCCEEDED(hr))
        {
            hr = E_FAIL;
        }
        ExitOnFailure(hr, "WSANSPIoctl() failed with return code %i, wsa last error %u", nResult, ::WSAGetLastError());
    }

LExit:
    return hr;
}

static HRESULT UpdateWaitStatus(
    __in HRESULT hrNewStatus,
    __inout MON_WAITER_CONTEXT *pWaiterContext,
    __in DWORD dwRequestIndex,
    __out_opt DWORD *pdwNewRequestIndex
    )
{
    HRESULT hr = S_OK;
    DWORD dwNewRequestIndex;
    MON_REQUEST *pRequest = pWaiterContext->rgRequests + dwRequestIndex;

    if (NULL != pdwNewRequestIndex)
    {
        *pdwNewRequestIndex = dwRequestIndex;
    }

    if (SUCCEEDED(pRequest->hrStatus) || SUCCEEDED(hrNewStatus))
    {
        // If it's a network wait, notify as long as it's new status is successful because we *may* have lost some changes
        // before the wait was re-initiated. Otherwise, only notify if there was an interesting status change
        if (SUCCEEDED(pRequest->hrStatus) != SUCCEEDED(hrNewStatus) || (pRequest->fNetwork && SUCCEEDED(hrNewStatus)))
        {
            Notify(hrNewStatus, pWaiterContext, pRequest);
        }

        if (SUCCEEDED(pRequest->hrStatus) && FAILED(hrNewStatus))
        {
            // If it's a network wait, notify coordinator thread that a network wait is failing
            if (pRequest->fNetwork && !::PostThreadMessageW(pWaiterContext->dwCoordinatorThreadId, MON_MESSAGE_NETWORK_WAIT_FAILED, 0, 0))
            {
                ExitWithLastError(hr, "Failed to send message to coordinator thread to notify a network wait started to fail");
            }

            // Move the failing wait to the end of the list of waits and increment cRequestsFailing so WaitForMultipleObjects isn't passed an invalid handle
            ++pWaiterContext->cRequestsFailing;
            dwNewRequestIndex = pWaiterContext->cRequests - 1;
            MemArraySwapItems(reinterpret_cast<void *>(pWaiterContext->rgHandles), dwRequestIndex + 1, dwNewRequestIndex + 1, sizeof(*pWaiterContext->rgHandles));
            MemArraySwapItems(reinterpret_cast<void *>(pWaiterContext->rgRequests), dwRequestIndex, dwNewRequestIndex, sizeof(*pWaiterContext->rgRequests));
            // Reset pRequest to the newly swapped item
            pRequest = pWaiterContext->rgRequests + dwNewRequestIndex;
            if (NULL != pdwNewRequestIndex)
            {
                *pdwNewRequestIndex = dwNewRequestIndex;
            }
        }
        else if (FAILED(pRequest->hrStatus) && SUCCEEDED(hrNewStatus))
        {
            Assert(pWaiterContext->cRequestsFailing > 0);
            // If it's a network wait, notify coordinator thread that a network wait is succeeding again
            if (pRequest->fNetwork && !::PostThreadMessageW(pWaiterContext->dwCoordinatorThreadId, MON_MESSAGE_NETWORK_WAIT_SUCCEEDED, 0, 0))
            {
                ExitWithLastError(hr, "Failed to send message to coordinator thread to notify a network wait is succeeding again");
            }

            --pWaiterContext->cRequestsFailing;
            dwNewRequestIndex = 0;
            MemArraySwapItems(reinterpret_cast<void *>(pWaiterContext->rgHandles), dwRequestIndex + 1, dwNewRequestIndex + 1, sizeof(*pWaiterContext->rgHandles));
            MemArraySwapItems(reinterpret_cast<void *>(pWaiterContext->rgRequests), dwRequestIndex, dwNewRequestIndex, sizeof(*pWaiterContext->rgRequests));
            // Reset pRequest to the newly swapped item
            pRequest = pWaiterContext->rgRequests + dwNewRequestIndex;
            if (NULL != pdwNewRequestIndex)
            {
                *pdwNewRequestIndex = dwNewRequestIndex;
            }
        }
    }

    pRequest->hrStatus = hrNewStatus;

LExit:
    return hr;
}
