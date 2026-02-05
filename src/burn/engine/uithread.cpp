// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define BURN_UITHREAD_CLASS_WINDOW L"WixBurnMessageWindow"


// structs

struct UITHREAD_CONTEXT
{
    HANDLE hInitializedEvent;
    HINSTANCE hInstance;
    BURN_ENGINE_STATE* pEngineState;
};

struct UITHREAD_INFO
{
    BOOL fElevatedEngine;
    BURN_ENGINE_STATE* pEngineState;
};


// internal function declarations

static DWORD WINAPI ThreadProc(
    __in LPVOID pvContext
    );

static LRESULT CALLBACK WndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );


// function definitions

HRESULT UiCreateMessageWindow(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    HANDLE rgWaitHandles[2] = { };
    UITHREAD_CONTEXT context = { };

    // Try to make this process the first one to receive WM_QUERYENDSESSION.
    // When blocking shutdown during Apply, this prevents other applications from being closed even though the restart will be blocked.
    // When initiating a restart, this makes it reasonable to assume WM_QUERYENDSESSION will be received quickly because otherwise other applications could delay indefinitely.
    ::SetProcessShutdownParameters(0x3FF, 0);

    // Create event to signal after the UI thread / window is initialized.
    rgWaitHandles[0] = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(rgWaitHandles[0], hr, "Failed to create initialization event.");

    // Pass necessary information to create the window.
    context.hInitializedEvent = rgWaitHandles[0];
    context.hInstance = hInstance;
    context.pEngineState = pEngineState;

    // Create our separate UI thread.
    rgWaitHandles[1] = ::CreateThread(NULL, 0, ThreadProc, &context, 0, NULL);
    ExitOnNullWithLastError(rgWaitHandles[1], hr, "Failed to create the UI thread.");

    // Wait for either the thread to be initialized or the window to exit / fail prematurely.
    ::WaitForMultipleObjects(countof(rgWaitHandles), rgWaitHandles, FALSE, INFINITE);

    pEngineState->hMessageWindowThread = rgWaitHandles[1];
    rgWaitHandles[1] = NULL;

LExit:
    ReleaseHandle(rgWaitHandles[1]);
    ReleaseHandle(rgWaitHandles[0]);

    return hr;
}

void UiCloseMessageWindow(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    if (::IsWindow(pEngineState->hMessageWindow))
    {
        ::PostMessageW(pEngineState->hMessageWindow, WM_CLOSE, 0, 0);
    }
}


// internal function definitions

static DWORD WINAPI ThreadProc(
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    UITHREAD_CONTEXT* pContext = static_cast<UITHREAD_CONTEXT*>(pvContext);
    UITHREAD_INFO info = { };

    WNDCLASSW wc = { };
    BOOL fRegistered = FALSE;
    HWND hWnd = NULL;

    BOOL fRet = FALSE;
    MSG msg = { };

    BURN_ENGINE_STATE* pEngineState = pContext->pEngineState;
    BOOL fElevatedEngine = BURN_MODE_ELEVATED == pContext->pEngineState->internalCommand.mode;

    wc.lpfnWndProc = WndProc;
    wc.hInstance = pContext->hInstance;
    wc.lpszClassName = BURN_UITHREAD_CLASS_WINDOW;

    if (!::RegisterClassW(&wc))
    {
        ExitWithLastError(hr, "Failed to register window.");
    }

    fRegistered = TRUE;

    info.fElevatedEngine = fElevatedEngine;
    info.pEngineState = pEngineState;

    // Create the window to handle reboots without activating it.
    hWnd = ::CreateWindowExW(WS_EX_NOACTIVATE, wc.lpszClassName, BURN_UITHREAD_CLASS_WINDOW, WS_POPUP, 0, 0, 0, 0, HWND_DESKTOP, NULL, pContext->hInstance, &info);
    ExitOnNullWithLastError(hWnd, hr, "Failed to create Burn UI thread window.");

    ::ShowWindow(hWnd, SW_SHOWNA);

    // Persist the window handle and let the caller know we've initialized.
    pEngineState->hMessageWindow = hWnd;
    ::SetEvent(pContext->hInitializedEvent);

    // Pump messages until the window is closed.
    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected return value from message pump.");
        }
        else if (!::IsDialogMessageW(msg.hwnd, &msg))
        {
            ::TranslateMessage(&msg);
            ::DispatchMessageW(&msg);
        }
    }

LExit:
    if (fRegistered)
    {
        ::UnregisterClassW(BURN_UITHREAD_CLASS_WINDOW, pContext->hInstance);
    }

    return hr;
}

static LRESULT CALLBACK WndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    switch (uMsg)
    {
    case WM_NCCREATE:
        {
        LPCREATESTRUCTW lpcs = reinterpret_cast<LPCREATESTRUCTW>(lParam);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(lpcs->lpCreateParams));
        break;
        }

    case WM_NCDESTROY:
        {
        LRESULT lRes = ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
        return lRes;
        }

    case WM_QUERYENDSESSION:
        {
        BOOL fCritical = ENDSESSION_CRITICAL & lParam;
        BOOL fAllowed = FALSE;

        UITHREAD_INFO* pInfo = reinterpret_cast<UITHREAD_INFO*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));
        if (!pInfo->pEngineState->plan.fApplying && // always block shutdown during apply.
            !fCritical)                             // always block critical shutdowns to receive the WM_ENDSESSION message.
        {
            fAllowed = TRUE;
        }

        CoreUpdateRestartState(pInfo->pEngineState, BURN_RESTART_STATE_INITIATING);
        pInfo->pEngineState->fCriticalShutdownInitiated |= fCritical;

        LogId(REPORT_STANDARD, MSG_SYSTEM_SHUTDOWN_REQUEST, LoggingBoolToString(fAllowed), LoggingBoolToString(pInfo->fElevatedEngine), LoggingBoolToString(fCritical), LoggingBoolToString(lParam & ENDSESSION_LOGOFF), LoggingBoolToString(lParam & ENDSESSION_CLOSEAPP));
        LogFlush();
        return fAllowed;
        }

    case WM_ENDSESSION:
        {
        UITHREAD_INFO* pInfo = reinterpret_cast<UITHREAD_INFO*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));
        BOOL fAllowed = 0 != wParam;

        LogId(REPORT_STANDARD, MSG_SYSTEM_SHUTDOWN_RESULT, LoggingBoolToString(fAllowed), LoggingBoolToString(pInfo->fElevatedEngine), LoggingBoolToString(lParam & ENDSESSION_CRITICAL), LoggingBoolToString(lParam & ENDSESSION_LOGOFF), LoggingBoolToString(lParam & ENDSESSION_CLOSEAPP));

        if (fAllowed)
        {
            // Windows will shutdown the process as soon as we return from this message.
            // https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ms700677(v=vs.85)

            // Give Apply approximately 20 seconds to complete.
            for (DWORD i = 0; i < 80; ++i)
            {
                if (!pInfo->pEngineState->plan.fApplying)
                {
                    break;
                }

                ::Sleep(250);
            }

            // If this is the per-machine process then close the logging pipe with the parent process.
            if (pInfo->fElevatedEngine)
            {
                CoreCloseElevatedLoggingThread(pInfo->pEngineState);
            }
            else
            {
                CoreWaitForUnelevatedLoggingThread(pInfo->pEngineState->hUnelevatedLoggingThread);
            }

            LogStringWorkRaw("=======================================\r\n");

            // Close the log to try to make sure everything is flushed to disk.
            LogClose(FALSE);
        }

        CoreUpdateRestartState(pInfo->pEngineState, fAllowed ? BURN_RESTART_STATE_INITIATED : BURN_RESTART_STATE_BLOCKED);

        return 0;
        }

    case WM_DESTROY:
        ::PostQuitMessage(0);
        return 0;
    }

    return ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
}
