// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


const DWORD BURN_TIMEOUT = 5 * 60 * 1000; // TODO: is 5 minutes good?

typedef enum _BURN_ELEVATION_MESSAGE_TYPE
{
    BURN_ELEVATION_MESSAGE_TYPE_UNKNOWN,
    BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE,
    BURN_ELEVATION_MESSAGE_TYPE_APPLY_UNINITIALIZE,
    BURN_ELEVATION_MESSAGE_TYPE_SESSION_BEGIN,
    BURN_ELEVATION_MESSAGE_TYPE_SESSION_END,
    BURN_ELEVATION_MESSAGE_TYPE_SAVE_STATE,
    BURN_ELEVATION_MESSAGE_TYPE_CACHE_PREPARE_PACKAGE,
    BURN_ELEVATION_MESSAGE_TYPE_CACHE_COMPLETE_PAYLOAD,
    BURN_ELEVATION_MESSAGE_TYPE_CACHE_VERIFY_PAYLOAD,
    BURN_ELEVATION_MESSAGE_TYPE_CACHE_CLEANUP,
    BURN_ELEVATION_MESSAGE_TYPE_PROCESS_DEPENDENT_REGISTRATION,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_RELATED_BUNDLE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_BUNDLE_PACKAGE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_EXE_PACKAGE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_PACKAGE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSP_PACKAGE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSU_PACKAGE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PACKAGE_PROVIDER,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PACKAGE_DEPENDENCY,
    BURN_ELEVATION_MESSAGE_TYPE_LAUNCH_EMBEDDED_CHILD,
    BURN_ELEVATION_MESSAGE_TYPE_CLEAN_PACKAGE,
    BURN_ELEVATION_MESSAGE_TYPE_LAUNCH_APPROVED_EXE,
    BURN_ELEVATION_MESSAGE_TYPE_BEGIN_MSI_TRANSACTION,
    BURN_ELEVATION_MESSAGE_TYPE_COMMIT_MSI_TRANSACTION,
    BURN_ELEVATION_MESSAGE_TYPE_ROLLBACK_MSI_TRANSACTION,
    BURN_ELEVATION_MESSAGE_TYPE_UNINSTALL_MSI_COMPATIBLE_PACKAGE,
    BURN_ELEVATION_MESSAGE_TYPE_CLEAN_COMPATIBLE_PACKAGE,

    BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_PAUSE_AU_BEGIN,
    BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_PAUSE_AU_COMPLETE,
    BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_SYSTEM_RESTORE_POINT_BEGIN,
    BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_SYSTEM_RESTORE_POINT_COMPLETE,
    BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_BEGIN,
    BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_COMPLETE,
    BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_SUCCESS,
    BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_FAILURE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROGRESS,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROCESS_CANCEL,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROCESS_STARTED,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROCESS_COMPLETED,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ERROR,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_MESSAGE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_FILES_IN_USE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_NETFX_FILES_IN_USE,
    BURN_ELEVATION_MESSAGE_TYPE_LAUNCH_APPROVED_EXE_PROCESSID,
    BURN_ELEVATION_MESSAGE_TYPE_PROGRESS_ROUTINE,
    BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ACTION_COMPLETE,
} BURN_ELEVATION_MESSAGE_TYPE;


// struct

typedef struct _BURN_ELEVATION_APPLY_INITIALIZE_MESSAGE_CONTEXT
{
    BURN_USER_EXPERIENCE* pBA;
    BOOL fPauseCompleteNeeded;
    BOOL fSrpCompleteNeeded;
} BURN_ELEVATION_APPLY_INITIALIZE_MESSAGE_CONTEXT;

typedef struct _BURN_ELEVATION_CACHE_MESSAGE_CONTEXT
{
    PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler;
    LPPROGRESS_ROUTINE pfnProgress;
    LPVOID pvContext;
} BURN_ELEVATION_CACHE_MESSAGE_CONTEXT;

typedef struct _BURN_ELEVATION_GENERIC_MESSAGE_CONTEXT
{
    PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler;
    LPVOID pvContext;
    BOOTSTRAPPER_APPLY_RESTART restart;
} BURN_ELEVATION_GENERIC_MESSAGE_CONTEXT;

typedef struct _BURN_ELEVATION_MSI_MESSAGE_CONTEXT
{
    PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler;
    LPVOID pvContext;
    BOOTSTRAPPER_APPLY_RESTART restart;
} BURN_ELEVATION_MSI_MESSAGE_CONTEXT;

typedef struct _BURN_ELEVATION_LAUNCH_APPROVED_EXE_MESSAGE_CONTEXT
{
    DWORD dwProcessId;
} BURN_ELEVATION_LAUNCH_APPROVED_EXE_MESSAGE_CONTEXT;

typedef struct _BURN_ELEVATION_CHILD_MESSAGE_CONTEXT
{
    HANDLE hPipe;
    HANDLE* phLock;
    BOOL* pfDisabledAutomaticUpdates;
    BOOL* pfApplying;
    BURN_APPROVED_EXES* pApprovedExes;
    BURN_CACHE* pCache;
    BURN_CONTAINERS* pContainers;
    BURN_PACKAGES* pPackages;
    BURN_PAYLOADS* pPayloads;
    BURN_VARIABLES* pVariables;
    BURN_REGISTRATION* pRegistration;
    BURN_USER_EXPERIENCE* pUserExperience;
} BURN_ELEVATION_CHILD_MESSAGE_CONTEXT;


// internal function declarations

/*******************************************************************
 LaunchElevatedProcess - Called from the per-user process to create
                          the per-machine process and set up the
                          communication pipe.

*******************************************************************/
static HRESULT LaunchElevatedProcess(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    );
static DWORD WINAPI ElevatedChildCacheThreadProc(
    __in LPVOID lpThreadParameter
    );
static HRESULT WaitForElevatedChildCacheThread(
    __in HANDLE hCacheThread,
    __in DWORD dwExpectedExitCode
    );
static HRESULT ProcessApplyInitializeMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT ProcessBurnCacheMessages(
    __in PIPE_MESSAGE* pMsg,
    __in LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT ProcessGenericExecuteMessages(
    __in PIPE_MESSAGE* pMsg,
    __in LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT ProcessMsiPackageMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT ProcessLaunchApprovedExeMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT ProcessProgressRoutineMessage(
    __in PIPE_MESSAGE* pMsg,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT ProcessElevatedChildMessage(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT ProcessElevatedChildCacheMessage(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT ProcessExecuteActionCompleteMessage(
    __in BYTE* pbData,
    __in DWORD cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart,
    __out DWORD* pdwResult
    );
static HRESULT OnApplyInitialize(
    __in HANDLE hPipe,
    __in BURN_VARIABLES* pVariables,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PACKAGES* pPackages,
    __in HANDLE* phLock,
    __in BOOL* pfDisabledWindowsUpdate,
    __in BOOL* pfApplying,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT ElevatedProcessDetect(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PACKAGES* pPackages
    );
static HRESULT OnApplyUninitialize(
    __in HANDLE* phLock,
    __in BOOL* pfApplying,
    __in BOOL* pfDisabledAutomaticUpdates
    );
static HRESULT OnSessionBegin(
    __in BURN_CACHE* pCache,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnSessionEnd(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnSaveState(
    __in BURN_REGISTRATION* pRegistration,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnCachePreparePackage(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnCacheCompletePayload(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOADS* pPayloads,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnCacheVerifyPayload(
    __in HANDLE hPipe,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOADS* pPayloads,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static void OnCacheCleanup(
    __in BURN_CACHE* pCache
    );
static HRESULT OnProcessDependentRegistration(
    __in const BURN_REGISTRATION* pRegistration,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnExecuteRelatedBundle(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT OnExecuteBundlePackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT OnExecuteExePackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT OnExecuteMsiPackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT OnExecuteMspPackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT OnExecuteMsuPackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT OnUninstallMsiCompatiblePackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT OnExecutePackageProviderAction(
    __in BURN_PACKAGES* pPackages,
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnExecutePackageDependencyAction(
    __in BURN_PACKAGES* pPackages,
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT CALLBACK BurnCacheMessageHandler(
    __in BURN_CACHE_MESSAGE* pMessage,
    __in LPVOID pvContext
    );
static DWORD CALLBACK ElevatedProgressRoutine(
    __in LARGE_INTEGER TotalFileSize,
    __in LARGE_INTEGER TotalBytesTransferred,
    __in LARGE_INTEGER StreamSize,
    __in LARGE_INTEGER StreamBytesTransferred,
    __in DWORD dwStreamNumber,
    __in DWORD dwCallbackReason,
    __in HANDLE hSourceFile,
    __in HANDLE hDestinationFile,
    __in_opt LPVOID lpData
    );
static int GenericExecuteMessageHandler(
    __in GENERIC_EXECUTE_MESSAGE* pMessage,
    __in LPVOID pvContext
    );
static int MsiExecuteMessageHandler(
    __in WIU_MSI_EXECUTE_MESSAGE* pMessage,
    __in_opt LPVOID pvContext
    );
static HRESULT OnCleanCompatiblePackage(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnCleanPackage(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnLaunchApprovedExe(
    __in HANDLE hPipe,
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnMsiBeginTransaction(
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData
    );
static HRESULT OnMsiCommitTransaction(
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART *pRestart
    );
static HRESULT OnMsiRollbackTransaction(
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART *pRestart
    );
static HRESULT ElevatedOnPauseAUBegin(
    __in HANDLE hPipe
    );
static HRESULT ElevatedOnPauseAUComplete(
    __in HANDLE hPipe,
    __in HRESULT hrStatus
    );
static HRESULT ElevatedOnSystemRestorePointBegin(
    __in HANDLE hPipe
    );
static HRESULT ElevatedOnSystemRestorePointComplete(
    __in HANDLE hPipe,
    __in HRESULT hrStatus
    );
static HRESULT ElevatedOnExecuteActionComplete(
    __in HANDLE hPipe,
    __in BOOTSTRAPPER_APPLY_RESTART restart
    );


// function definitions

extern "C" HRESULT ElevationElevate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in WM_BURN reason,
    __in_opt HWND hwndParent
    )
{
    Assert(BURN_MODE_ELEVATED != pEngineState->internalCommand.mode);
    Assert(!pEngineState->companionConnection.sczName);
    Assert(!pEngineState->companionConnection.sczSecret);
    Assert(!pEngineState->companionConnection.hProcess);
    Assert(!pEngineState->companionConnection.dwProcessId);
    Assert(INVALID_HANDLE_VALUE == pEngineState->companionConnection.hPipe);
    Assert(INVALID_HANDLE_VALUE == pEngineState->companionConnection.hCachePipe);
    Assert(INVALID_HANDLE_VALUE == pEngineState->companionConnection.hLoggingPipe);

    HRESULT hr = S_OK;
    int nResult = IDOK;

    hr = BACallbackOnElevateBegin(&pEngineState->userExperience);
    ExitOnRootFailure(hr, "BA aborted elevation requirement.");

    hr = BurnPipeCreateNameAndSecret(&pEngineState->companionConnection.sczName, &pEngineState->companionConnection.sczSecret);
    ExitOnFailure(hr, "Failed to create pipe name and client token.");

    hr = BurnPipeCreatePipes(&pEngineState->companionConnection, TRUE);
    ExitOnFailure(hr, "Failed to create pipe and cache pipe.");

    LogId(REPORT_STANDARD, MSG_LAUNCH_ELEVATED_ENGINE_STARTING);

    do
    {
        nResult = IDOK;

        // Create the elevated process and if successful, wait for it to connect.
        hr = LaunchElevatedProcess(pEngineState, hwndParent);
        if (SUCCEEDED(hr))
        {
            LogId(REPORT_STANDARD, MSG_LAUNCH_ELEVATED_ENGINE_SUCCESS);

            hr = BurnPipeWaitForChildConnect(&pEngineState->companionConnection);
            if (HRESULT_FROM_WIN32(ERROR_NO_DATA) == hr)
            {
                hr = E_SUSPECTED_AV_INTERFERENCE;
            }
            ExitOnFailure(hr, "Failed to connect to elevated child process.");

            LogId(REPORT_STANDARD, MSG_CONNECT_TO_ELEVATED_ENGINE_SUCCESS);
        }
        else if (HRESULT_FROM_WIN32(ERROR_CANCELLED) == hr)
        {
            // The user clicked "Cancel" on the elevation prompt or the elevation prompt timed out, provide the notification with the option to retry.
            if (WM_BURN_APPLY == reason)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
            }

            nResult = BootstrapperApplicationSendError(&pEngineState->userExperience, BOOTSTRAPPER_ERROR_TYPE_ELEVATE, NULL, hr, NULL, MB_ICONERROR | MB_RETRYCANCEL, IDNOACTION);
        }
    } while (IDRETRY == nResult);
    ExitOnFailure(hr, "Failed to elevate.");

LExit:
    if (FAILED(hr))
    {
        BurnPipeConnectionUninitialize(&pEngineState->companionConnection);
    }

    BACallbackOnElevateComplete(&pEngineState->userExperience, hr);

    return hr;
}

extern "C" HRESULT ElevationApplyInitialize(
    __in HANDLE hPipe,
    __in BURN_USER_EXPERIENCE* pBA,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;
    BURN_ELEVATION_APPLY_INITIALIZE_MESSAGE_CONTEXT context = { };

    context.pBA = pBA;

    // serialize message data
    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pPlan->action);
    ExitOnFailure(hr, "Failed to write action to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pPlan->pInternalCommand->automaticUpdates);
    ExitOnFailure(hr, "Failed to write update action to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)!pPlan->pInternalCommand->fDisableSystemRestore);
    ExitOnFailure(hr, "Failed to write system restore point action to message buffer.");

    hr = VariableSerialize(pVariables, FALSE, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to write variables.");

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE, pbData, cbData, ProcessApplyInitializeMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send message to per-machine process.");

    hr = (HRESULT)dwResult;

    // Best effort to keep the sequence of BA events sane.
    if (context.fPauseCompleteNeeded)
    {
        BACallbackOnPauseAUComplete(pBA, hr);
    }
    if (context.fSrpCompleteNeeded)
    {
        BACallbackOnSystemRestorePointComplete(pBA, hr);
    }

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationApplyUninitialize(
    __in HANDLE hPipe
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_APPLY_UNINITIALIZE, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationSessionBegin -

*******************************************************************/
extern "C" HRESULT ElevationSessionBegin(
    __in HANDLE hPipe,
    __in_z LPCWSTR wzEngineWorkingPath,
    __in_z LPCWSTR wzResumeCommandLine,
    __in BOOL fDisableResume,
    __in BURN_VARIABLES* pVariables,
    __in DWORD dwRegistrationOperations,
    __in BOOL fDetectedForeignProviderKeyBundleCode,
    __in DWORD64 qwEstimatedSize,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, wzEngineWorkingPath);
    ExitOnFailure(hr, "Failed to write engine working path to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, wzResumeCommandLine);
    ExitOnFailure(hr, "Failed to write resume command line to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, fDisableResume);
    ExitOnFailure(hr, "Failed to write resume flag.");

    hr = BuffWriteNumber(&pbData, &cbData, dwRegistrationOperations);
    ExitOnFailure(hr, "Failed to write registration operations to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)fDetectedForeignProviderKeyBundleCode);
    ExitOnFailure(hr, "Failed to write dependency registration action to message buffer.");

    hr = BuffWriteNumber64(&pbData, &cbData, qwEstimatedSize);
    ExitOnFailure(hr, "Failed to write estimated size to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)registrationType);
    ExitOnFailure(hr, "Failed to write registration type to message buffer.");

    hr = VariableSerialize(pVariables, FALSE, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to write variables.");

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_SESSION_BEGIN, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationSessionEnd -

*******************************************************************/
extern "C" HRESULT ElevationSessionEnd(
    __in HANDLE hPipe,
    __in BURN_RESUME_MODE resumeMode,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __in BOOL fDetectedForeignProviderKeyBundleCode,
    __in DWORD64 qwEstimatedSize,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)resumeMode);
    ExitOnFailure(hr, "Failed to write resume mode to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)restart);
    ExitOnFailure(hr, "Failed to write restart enum to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)fDetectedForeignProviderKeyBundleCode);
    ExitOnFailure(hr, "Failed to write dependency registration action to message buffer.");

    hr = BuffWriteNumber64(&pbData, &cbData, qwEstimatedSize);
    ExitOnFailure(hr, "Failed to write estimated size to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)registrationType);
    ExitOnFailure(hr, "Failed to write registration type to message buffer.");

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_SESSION_END, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationSaveState -

*******************************************************************/
HRESULT ElevationSaveState(
    __in HANDLE hPipe,
    __in_bcount(cbBuffer) BYTE* pbBuffer,
    __in SIZE_T cbBuffer
    )
{
    HRESULT hr = S_OK;
    DWORD dwResult = 0;

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_SAVE_STATE, pbBuffer, cbBuffer, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    return hr;
}

extern "C" HRESULT ElevationCachePreparePackage(
    __in HANDLE hPipe,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // Serialize message data.
    hr = BuffWriteString(&pbData, &cbData, pPackage ? pPackage->sczId : NULL);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    // Send message.
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_CACHE_PREPARE_PACKAGE, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_CACHE_PREPARE_PACKAGE message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationCacheCompletePayload -

*******************************************************************/
extern "C" HRESULT ElevationCacheCompletePayload(
    __in HANDLE hPipe,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzUnverifiedPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;
    BURN_ELEVATION_CACHE_MESSAGE_CONTEXT context = { };

    context.pfnCacheMessageHandler = pfnCacheMessageHandler;
    context.pfnProgress = pfnProgress;
    context.pvContext = pContext;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pPayload->sczKey);
    ExitOnFailure(hr, "Failed to write payload id to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, wzUnverifiedPath);
    ExitOnFailure(hr, "Failed to write unverified path to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)fMove);
    ExitOnFailure(hr, "Failed to write move flag to message buffer.");

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_CACHE_COMPLETE_PAYLOAD, pbData, cbData, ProcessBurnCacheMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_CACHE_COMPLETE_PAYLOAD message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationCacheVerifyPayload(
    __in HANDLE hPipe,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PAYLOAD* pPayload,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;
    BURN_ELEVATION_CACHE_MESSAGE_CONTEXT context = { };

    context.pfnCacheMessageHandler = pfnCacheMessageHandler;
    context.pfnProgress = pfnProgress;
    context.pvContext = pContext;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pPayload->sczKey);
    ExitOnFailure(hr, "Failed to write payload id to message buffer.");

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_CACHE_VERIFY_PAYLOAD, pbData, cbData, ProcessBurnCacheMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_CACHE_VERIFY_PAYLOAD message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationCacheCleanup -

*******************************************************************/
extern "C" HRESULT ElevationCacheCleanup(
    __in HANDLE hPipe
    )
{
    HRESULT hr = S_OK;
    DWORD dwResult = 0;

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_CACHE_CLEANUP, NULL, 0, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_CACHE_CLEANUP message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    return hr;
}

extern "C" HRESULT ElevationProcessDependentRegistration(
    __in HANDLE hPipe,
    __in const BURN_DEPENDENT_REGISTRATION_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteNumber(&pbData, &cbData, pAction->type);
    ExitOnFailure(hr, "Failed to write action type to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pAction->sczBundleCode);
    ExitOnFailure(hr, "Failed to write bundle code to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pAction->sczDependentProviderKey);
    ExitOnFailure(hr, "Failed to write dependent provider key to message buffer.");

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_PROCESS_DEPENDENT_REGISTRATION, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_PROCESS_DEPENDENT_REGISTRATION message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationExecuteRelatedBundle -

*******************************************************************/
extern "C" HRESULT ElevationExecuteRelatedBundle(
    __in HANDLE hPipe,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    BURN_ELEVATION_GENERIC_MESSAGE_CONTEXT context = { };
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->relatedBundle.pRelatedBundle->package.sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->relatedBundle.action);
    ExitOnFailure(hr, "Failed to write action to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->relatedBundle.pRelatedBundle->planRelationType);
    ExitOnFailure(hr, "Failed to write planRelationType to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, fRollback);
    ExitOnFailure(hr, "Failed to write rollback.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->relatedBundle.sczIgnoreDependencies);
    ExitOnFailure(hr, "Failed to write the list of dependencies to ignore to the message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->relatedBundle.sczAncestors);
    ExitOnFailure(hr, "Failed to write the list of ancestors to the message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->relatedBundle.sczEngineWorkingDirectory);
    ExitOnFailure(hr, "Failed to write the custom working directory to the message buffer.");

    hr = VariableSerialize(pVariables, FALSE, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to write variables.");

    // send message
    context.pfnGenericMessageHandler = pfnGenericMessageHandler;
    context.pvContext = pvContext;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_RELATED_BUNDLE, pbData, cbData, ProcessGenericExecuteMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_RELATED_BUNDLE message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);
    *pRestart = context.restart;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationExecuteBundlePackage -

*******************************************************************/
extern "C" HRESULT ElevationExecuteBundlePackage(
    __in HANDLE hPipe,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    BURN_ELEVATION_GENERIC_MESSAGE_CONTEXT context = { };
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->bundlePackage.pPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->bundlePackage.action);
    ExitOnFailure(hr, "Failed to write action to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, fRollback);
    ExitOnFailure(hr, "Failed to write rollback.");

    hr = BuffWriteNumber(&pbData, &cbData, SUCCEEDED(pExecuteAction->bundlePackage.pPackage->hrCacheResult));
    ExitOnFailure(hr, "Failed to write fCacheAvailable.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->bundlePackage.sczParent);
    ExitOnFailure(hr, "Failed to write the parent to the message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->bundlePackage.sczIgnoreDependencies);
    ExitOnFailure(hr, "Failed to write the list of dependencies to ignore to the message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->bundlePackage.sczAncestors);
    ExitOnFailure(hr, "Failed to write the list of ancestors to the message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->bundlePackage.sczEngineWorkingDirectory);
    ExitOnFailure(hr, "Failed to write the custom working directory to the message buffer.");

    hr = VariableSerialize(pVariables, FALSE, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to write variables.");

    // send message
    context.pfnGenericMessageHandler = pfnGenericMessageHandler;
    context.pvContext = pvContext;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_BUNDLE_PACKAGE, pbData, cbData, ProcessGenericExecuteMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_BUNDLE_PACKAGE message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);
    *pRestart = context.restart;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationExecuteExePackage -

*******************************************************************/
extern "C" HRESULT ElevationExecuteExePackage(
    __in HANDLE hPipe,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    BURN_ELEVATION_GENERIC_MESSAGE_CONTEXT context = { };
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->exePackage.pPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->exePackage.action);
    ExitOnFailure(hr, "Failed to write action to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, fRollback);
    ExitOnFailure(hr, "Failed to write rollback.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->exePackage.sczAncestors);
    ExitOnFailure(hr, "Failed to write the list of ancestors to the message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->exePackage.sczEngineWorkingDirectory);
    ExitOnFailure(hr, "Failed to write the custom working directory to the message buffer.");

    hr = VariableSerialize(pVariables, FALSE, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to write variables.");

    // send message
    context.pfnGenericMessageHandler = pfnGenericMessageHandler;
    context.pvContext = pvContext;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_EXE_PACKAGE, pbData, cbData, ProcessGenericExecuteMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_EXE_PACKAGE message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);
    *pRestart = context.restart;

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationMsiBeginTransaction(
    __in HANDLE hPipe,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = ERROR_SUCCESS;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pRollbackBoundary->sczId);
    ExitOnFailure(hr, "Failed to write transaction name to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pRollbackBoundary->sczLogPath);
    ExitOnFailure(hr, "Failed to write transaction log path to message buffer.");

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_BEGIN_MSI_TRANSACTION, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_BEGIN_MSI_TRANSACTION message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationMsiCommitTransaction(
    __in HANDLE hPipe,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    BURN_ELEVATION_MSI_MESSAGE_CONTEXT context = { };
    DWORD dwResult = ERROR_SUCCESS;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pRollbackBoundary->sczId);
    ExitOnFailure(hr, "Failed to write transaction name to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pRollbackBoundary->sczLogPath);
    ExitOnFailure(hr, "Failed to write transaction log path to message buffer.");

    // send message
    context.pfnMessageHandler = pfnMessageHandler;
    context.pvContext = pvContext;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_COMMIT_MSI_TRANSACTION, pbData, cbData, ProcessMsiPackageMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_COMMIT_MSI_TRANSACTION message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);
    *pRestart = context.restart;

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationMsiRollbackTransaction(
    __in HANDLE hPipe,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    BURN_ELEVATION_MSI_MESSAGE_CONTEXT context = { };
    DWORD dwResult = ERROR_SUCCESS;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pRollbackBoundary->sczId);
    ExitOnFailure(hr, "Failed to write transaction name to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pRollbackBoundary->sczLogPath);
    ExitOnFailure(hr, "Failed to write transaction log path to message buffer.");

    // send message
    context.pfnMessageHandler = pfnMessageHandler;
    context.pvContext = pvContext;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_ROLLBACK_MSI_TRANSACTION, pbData, cbData, ProcessMsiPackageMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_ROLLBACK_MSI_TRANSACTION message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);
    *pRestart = context.restart;

LExit:
    ReleaseMem(pbData);

    return hr;
}



/*******************************************************************
 ElevationExecuteMsiPackage -

*******************************************************************/
extern "C" HRESULT ElevationExecuteMsiPackage(
    __in HANDLE hPipe,
    __in_opt HWND hwndParent,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    BURN_ELEVATION_MSI_MESSAGE_CONTEXT context = { };
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)fRollback);
    ExitOnFailure(hr, "Failed to write rollback flag to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->msiPackage.pPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWritePointer(&pbData, &cbData, (DWORD_PTR)hwndParent);
    ExitOnFailure(hr, "Failed to write parent hwnd to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->msiPackage.sczLogPath);
    ExitOnFailure(hr, "Failed to write package log to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->msiPackage.actionMsiProperty);
    ExitOnFailure(hr, "Failed to write actionMsiProperty to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->msiPackage.uiLevel);
    ExitOnFailure(hr, "Failed to write UI level to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->msiPackage.fDisableExternalUiHandler);
    ExitOnFailure(hr, "Failed to write fDisableExternalUiHandler to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->msiPackage.fileVersioning);
    ExitOnFailure(hr, "Failed to write fileVersioning to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->msiPackage.action);
    ExitOnFailure(hr, "Failed to write action to message buffer.");

    // Feature actions.
    for (DWORD i = 0; i < pExecuteAction->msiPackage.pPackage->Msi.cFeatures; ++i)
    {
        hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->msiPackage.rgFeatures[i]);
        ExitOnFailure(hr, "Failed to write feature action to message buffer.");
    }

    // Slipstream patches actions.
    for (DWORD i = 0; i < pExecuteAction->msiPackage.pPackage->Msi.cSlipstreamMspPackages; ++i)
    {
        BURN_SLIPSTREAM_MSP* pSlipstreamMsp = pExecuteAction->msiPackage.pPackage->Msi.rgSlipstreamMsps + i;
        BOOTSTRAPPER_ACTION_STATE* pAction = fRollback ? &pSlipstreamMsp->rollback : &pSlipstreamMsp->execute;
        hr = BuffWriteNumber(&pbData, &cbData, (DWORD)*pAction);
        ExitOnFailure(hr, "Failed to write slipstream patch action to message buffer.");
    }

    hr = VariableSerialize(pVariables, FALSE, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to write variables.");


    // send message
    context.pfnMessageHandler = pfnMessageHandler;
    context.pvContext = pvContext;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_PACKAGE, pbData, cbData, ProcessMsiPackageMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_PACKAGE message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);
    *pRestart = context.restart;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationExecuteMspPackage -

*******************************************************************/
extern "C" HRESULT ElevationExecuteMspPackage(
    __in HANDLE hPipe,
    __in_opt HWND hwndParent,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    BURN_ELEVATION_MSI_MESSAGE_CONTEXT context = { };
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->mspTarget.pPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWritePointer(&pbData, &cbData, (DWORD_PTR)hwndParent);
    ExitOnFailure(hr, "Failed to write parent hwnd to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->mspTarget.sczTargetProductCode);
    ExitOnFailure(hr, "Failed to write target product code to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->mspTarget.sczLogPath);
    ExitOnFailure(hr, "Failed to write package log to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->mspTarget.actionMsiProperty);
    ExitOnFailure(hr, "Failed to write actionMsiProperty to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->mspTarget.uiLevel);
    ExitOnFailure(hr, "Failed to write UI level to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->mspTarget.fDisableExternalUiHandler);
    ExitOnFailure(hr, "Failed to write fDisableExternalUiHandler to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->mspTarget.fileVersioning);
    ExitOnFailure(hr, "Failed to write fileVersioning to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->mspTarget.action);
    ExitOnFailure(hr, "Failed to write action to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, pExecuteAction->mspTarget.cOrderedPatches);
    ExitOnFailure(hr, "Failed to write count of ordered patches to message buffer.");

    for (DWORD i = 0; i < pExecuteAction->mspTarget.cOrderedPatches; ++i)
    {
        hr = BuffWriteString(&pbData, &cbData, pExecuteAction->mspTarget.rgOrderedPatches[i].pPackage->sczId);
        ExitOnFailure(hr, "Failed to write ordered patch id to message buffer.");
    }

    hr = VariableSerialize(pVariables, FALSE, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to write variables.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)fRollback);
    ExitOnFailure(hr, "Failed to write rollback flag to message buffer.");

    // send message
    context.pfnMessageHandler = pfnMessageHandler;
    context.pvContext = pvContext;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSP_PACKAGE, pbData, cbData, ProcessMsiPackageMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSP_PACKAGE message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);
    *pRestart = context.restart;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationExecuteMsuPackage -

*******************************************************************/
extern "C" HRESULT ElevationExecuteMsuPackage(
    __in HANDLE hPipe,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BOOL fRollback,
    __in BOOL fStopWusaService,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    BURN_ELEVATION_GENERIC_MESSAGE_CONTEXT context = { };
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->msuPackage.pPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->msuPackage.sczLogPath);
    ExitOnFailure(hr, "Failed to write package log to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pExecuteAction->msuPackage.action);
    ExitOnFailure(hr, "Failed to write action to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, fRollback);
    ExitOnFailure(hr, "Failed to write rollback.");

    hr = BuffWriteNumber(&pbData, &cbData, fStopWusaService);
    ExitOnFailure(hr, "Failed to write StopWusaService.");

    // send message
    context.pfnGenericMessageHandler = pfnGenericMessageHandler;
    context.pvContext = pvContext;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSU_PACKAGE, pbData, cbData, ProcessGenericExecuteMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSU_PACKAGE message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);
    *pRestart = context.restart;

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationUninstallMsiCompatiblePackage(
    __in HANDLE hPipe,
    __in_opt HWND hwndParent,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    BURN_ELEVATION_MSI_MESSAGE_CONTEXT context = { };
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)fRollback);
    ExitOnFailure(hr, "Failed to write rollback flag to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->uninstallMsiCompatiblePackage.pParentPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->uninstallMsiCompatiblePackage.pParentPackage->compatiblePackage.compatibleEntry.sczId);
    ExitOnFailure(hr, "Failed to write compatible package id to message buffer.");

    hr = BuffWritePointer(&pbData, &cbData, (DWORD_PTR)hwndParent);
    ExitOnFailure(hr, "Failed to write parent hwnd to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->uninstallMsiCompatiblePackage.sczLogPath);
    ExitOnFailure(hr, "Failed to write package log to message buffer.");

    hr = VariableSerialize(pVariables, FALSE, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to write variables.");


    // send message
    context.pfnMessageHandler = pfnMessageHandler;
    context.pvContext = pvContext;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_UNINSTALL_MSI_COMPATIBLE_PACKAGE, pbData, cbData, ProcessMsiPackageMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_UNINSTALL_MSI_COMPATIBLE_PACKAGE message to per-machine process.");

    hr = static_cast<HRESULT>(dwResult);
    *pRestart = context.restart;

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationExecutePackageProviderAction(
    __in HANDLE hPipe,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BOOL fRollback
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // Serialize the message data.
    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->packageProvider.pPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)fRollback);
    ExitOnFailure(hr, "Failed to write rollback flag to message buffer.");

    // Provider actions.
    for (DWORD i = 0; i < pExecuteAction->packageProvider.pPackage->cDependencyProviders; ++i)
    {
        BURN_DEPENDENCY_PROVIDER* pProvider = pExecuteAction->packageProvider.pPackage->rgDependencyProviders + i;
        BURN_DEPENDENCY_ACTION* pAction = fRollback ? &pProvider->providerRollback : &pProvider->providerExecute;
        hr = BuffWriteNumber(&pbData, &cbData, (DWORD)*pAction);
        ExitOnFailure(hr, "Failed to write provider action to message buffer.");
    }

    // Send the message.
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PACKAGE_PROVIDER, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PACKAGE_PROVIDER message to per-machine process.");

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationExecutePackageDependencyAction(
    __in HANDLE hPipe,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BOOL fRollback
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // Serialize the message data.
    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->packageDependency.pPackage->sczId);
    ExitOnFailure(hr, "Failed to write package id to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, (DWORD)fRollback);
    ExitOnFailure(hr, "Failed to write rollback flag to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pExecuteAction->packageDependency.sczBundleProviderKey);
    ExitOnFailure(hr, "Failed to write bundle dependency key to message buffer.");

    // Dependent actions.
    for (DWORD i = 0; i < pExecuteAction->packageProvider.pPackage->cDependencyProviders; ++i)
    {
        BURN_DEPENDENCY_PROVIDER* pProvider = pExecuteAction->packageProvider.pPackage->rgDependencyProviders + i;
        BURN_DEPENDENCY_ACTION* pAction = fRollback ? &pProvider->dependentRollback : &pProvider->dependentExecute;
        hr = BuffWriteNumber(&pbData, &cbData, (DWORD)*pAction);
        ExitOnFailure(hr, "Failed to write dependent action to message buffer.");
    }

    // Send the message.
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PACKAGE_DEPENDENCY, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PACKAGE_DEPENDENCY message to per-machine process.");

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationCleanCompatiblePackage(
    __in HANDLE hPipe,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pPackage->sczId);
    ExitOnFailure(hr, "Failed to write clean package id to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pPackage->compatiblePackage.compatibleEntry.sczId);
    ExitOnFailure(hr, "Failed to write compatible package id to message buffer.");

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_CLEAN_COMPATIBLE_PACKAGE, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_CLEAN_COMPATIBLE_PACKAGE message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationCleanPackage -

*******************************************************************/
extern "C" HRESULT ElevationCleanPackage(
    __in HANDLE hPipe,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // serialize message data
    hr = BuffWriteString(&pbData, &cbData, pPackage->sczId);
    ExitOnFailure(hr, "Failed to write clean package id to message buffer.");

    // send message
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_CLEAN_PACKAGE, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_CLEAN_PACKAGE message to per-machine process.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

extern "C" HRESULT ElevationLaunchApprovedExe(
    __in HANDLE hPipe,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe,
    __out DWORD* pdwProcessId
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;
    BURN_ELEVATION_LAUNCH_APPROVED_EXE_MESSAGE_CONTEXT context = { };

    // Serialize message data.
    hr = BuffWriteString(&pbData, &cbData, pLaunchApprovedExe->sczId);
    ExitOnFailure(hr, "Failed to write approved exe id to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, pLaunchApprovedExe->sczArguments);
    ExitOnFailure(hr, "Failed to write approved exe arguments to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, pLaunchApprovedExe->dwWaitForInputIdleTimeout);
    ExitOnFailure(hr, "Failed to write approved exe WaitForInputIdle timeout to message buffer.");

    // Send the message.
    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_LAUNCH_APPROVED_EXE, pbData, cbData, ProcessLaunchApprovedExeMessages, &context, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_LAUNCH_APPROVED_EXE message to per-machine process.");

    hr = (HRESULT)dwResult;
    *pdwProcessId = context.dwProcessId;

LExit:
    ReleaseMem(pbData);

    return hr;
}

/*******************************************************************
 ElevationChildPumpMessages -

*******************************************************************/
extern "C" HRESULT ElevationChildPumpMessages(
    __in HANDLE hPipe,
    __in HANDLE hCachePipe,
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in BURN_CACHE* pCache,
    __in BURN_CONTAINERS* pContainers,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOADS* pPayloads,
    __in BURN_VARIABLES* pVariables,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __out HANDLE* phLock,
    __out DWORD* pdwChildExitCode,
    __out BOOL* pfRestart,
    __out BOOL* pfApplying
    )
{
    HRESULT hr = S_OK;
    BURN_ELEVATION_CHILD_MESSAGE_CONTEXT cacheContext = { };
    BURN_ELEVATION_CHILD_MESSAGE_CONTEXT context = { };
    HANDLE hCacheThread = NULL;
    BURN_PIPE_RESULT result = { };
    BOOL fDisabledAutomaticUpdates = FALSE;

    cacheContext.hPipe = hCachePipe;
    cacheContext.pCache = pCache;
    cacheContext.pContainers = pContainers;
    cacheContext.pPackages = pPackages;
    cacheContext.pPayloads = pPayloads;
    cacheContext.pVariables = pVariables;
    cacheContext.pRegistration = pRegistration;
    cacheContext.pUserExperience = pUserExperience;

    context.hPipe = hPipe;
    context.phLock = phLock;
    context.pfDisabledAutomaticUpdates = &fDisabledAutomaticUpdates;
    context.pfApplying = pfApplying;
    context.pApprovedExes = pApprovedExes;
    context.pCache = pCache;
    context.pContainers = pContainers;
    context.pPackages = pPackages;
    context.pPayloads = pPayloads;
    context.pVariables = pVariables;
    context.pRegistration = pRegistration;
    context.pUserExperience = pUserExperience;

    hCacheThread = ::CreateThread(NULL, 0, ElevatedChildCacheThreadProc, &cacheContext, 0, NULL);
    ExitOnNullWithLastError(hCacheThread, hr, "Failed to create elevated cache thread.");

    hr = BurnPipePumpMessages(hPipe, ProcessElevatedChildMessage, &context, &result);
    ExitOnFailure(hr, "Failed to pump messages in child process.");

    // Wait for the cache thread and verify it gets the right result but don't fail if things
    // don't work out.
    WaitForElevatedChildCacheThread(hCacheThread, result.dwResult);

    *pdwChildExitCode = result.dwResult;
    *pfRestart = result.fRestart;

LExit:
    ReleaseHandle(hCacheThread);

    if (fDisabledAutomaticUpdates)
    {
        ElevationChildResumeAutomaticUpdates();
    }

    return hr;
}

extern "C" HRESULT ElevationChildResumeAutomaticUpdates()
{
    HRESULT hr = S_OK;

    LogId(REPORT_STANDARD, MSG_RESUME_AU_STARTING);

    hr = WuaResumeAutomaticUpdates();
    ExitOnFailure(hr, "Failed to resume automatic updates after pausing them, continuing...");

    LogId(REPORT_STANDARD, MSG_RESUME_AU_SUCCEEDED);

LExit:
    return hr;
}

// internal function definitions

static HRESULT LaunchElevatedProcess(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    )
{
    HRESULT hr = S_OK;
    DWORD dwCurrentProcessId = ::GetCurrentProcessId();
    LPWSTR sczParameters = NULL;
    HANDLE hProcess = NULL;
    BURN_PIPE_CONNECTION* pConnection = &pEngineState->companionConnection;

    hr = StrAllocFormatted(&sczParameters, L"-q -%ls %ls %ls %u", BURN_COMMANDLINE_SWITCH_ELEVATED, pConnection->sczName, pConnection->sczSecret, dwCurrentProcessId);
    ExitOnFailure(hr, "Failed to allocate parameters for elevated process.");

    if (BURN_LOGGING_ATTRIBUTE_EXTRADEBUG & pEngineState->internalCommand.dwLoggingAttributes)
    {
        hr = StrAllocConcatFormatted(&sczParameters, L" -%ls=%ls", BURN_COMMANDLINE_SWITCH_LOG_MODE, L"x");
        ExitOnFailure(hr, "Failed to set log mode in elevated process command-line.");
    }

    // Since ShellExecuteEx doesn't support passing inherited handles, don't bother with CoreAppendFileHandleSelfToCommandLine.
    // We could fallback to using ::DuplicateHandle to inject the file handle later if necessary.
    hr = ShelExec(pEngineState->sczBundleEngineWorkingPath, sczParameters, L"runas", NULL, SW_SHOWNA, hwndParent, &hProcess);
    ExitOnFailure(hr, "Failed to launch elevated child process: %ls", pEngineState->sczBundleEngineWorkingPath);

    pConnection->dwProcessId = ::GetProcessId(hProcess);
    pConnection->hProcess = hProcess;
    hProcess = NULL;

LExit:
    ReleaseHandle(hProcess);
    ReleaseStr(sczParameters);

    return hr;
}

static DWORD WINAPI ElevatedChildCacheThreadProc(
    __in LPVOID lpThreadParameter
    )
{
    HRESULT hr = S_OK;
    BURN_ELEVATION_CHILD_MESSAGE_CONTEXT* pContext = reinterpret_cast<BURN_ELEVATION_CHILD_MESSAGE_CONTEXT*>(lpThreadParameter);
    BOOL fComInitialized = FALSE;
    BURN_PIPE_RESULT result = { };

    // initialize COM
    hr = ::CoInitializeEx(NULL, COINIT_MULTITHREADED);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    hr = BurnPipePumpMessages(pContext->hPipe, ProcessElevatedChildCacheMessage, pContext, &result);
    ExitOnFailure(hr, "Failed to pump messages in child process.");

    hr = (HRESULT)result.dwResult;

LExit:
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return (DWORD)hr;
}

static HRESULT WaitForElevatedChildCacheThread(
    __in HANDLE hCacheThread,
    __in DWORD dwExpectedExitCode
    )
{
    UNREFERENCED_PARAMETER(dwExpectedExitCode);

    HRESULT hr = S_OK;
    DWORD dwExitCode = ERROR_SUCCESS;

    hr = ThrdWaitForCompletion(hCacheThread, BURN_TIMEOUT, &dwExitCode);
    ExitOnFailure(hr, "Failed to wait for cache thread to complete.");

    AssertSz(dwExitCode == dwExpectedExitCode, "Cache thread should have exited with the expected exit code.");

LExit:
    return hr;
}

static HRESULT ProcessApplyInitializeMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    BURN_ELEVATION_APPLY_INITIALIZE_MESSAGE_CONTEXT* pContext = static_cast<BURN_ELEVATION_APPLY_INITIALIZE_MESSAGE_CONTEXT*>(pvContext);
    BYTE* pbData = (BYTE*)pMsg->pvData;
    SIZE_T iData = 0;
    HRESULT hrStatus = S_OK;
    HRESULT hrBA = S_OK;

    // Process the message.
    switch (pMsg->dwMessageType)
    {
    case BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_PAUSE_AU_BEGIN:
        pContext->fPauseCompleteNeeded = TRUE;
        hrBA = BACallbackOnPauseAUBegin(pContext->pBA);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_PAUSE_AU_COMPLETE:
        // read hrStatus
        hr = BuffReadNumber(pbData, pMsg->cbData, &iData, reinterpret_cast<DWORD*>(&hrStatus));
        ExitOnFailure(hr, "Failed to read pause AU hrStatus.");

        pContext->fPauseCompleteNeeded = FALSE;
        hrBA = BACallbackOnPauseAUComplete(pContext->pBA, hrStatus);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_SYSTEM_RESTORE_POINT_BEGIN:
        if (pContext->fPauseCompleteNeeded)
        {
            pContext->fPauseCompleteNeeded = FALSE;
            hrBA = BACallbackOnPauseAUComplete(pContext->pBA, E_INVALIDSTATE);
        }

        pContext->fSrpCompleteNeeded = TRUE;
        hrBA = BACallbackOnSystemRestorePointBegin(pContext->pBA);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_SYSTEM_RESTORE_POINT_COMPLETE:
        // read hrStatus
        hr = BuffReadNumber(pbData, pMsg->cbData, &iData, reinterpret_cast<DWORD*>(&hrStatus));
        ExitOnFailure(hr, "Failed to read system restore point hrStatus.");

        pContext->fSrpCompleteNeeded = FALSE;
        hrBA = BACallbackOnSystemRestorePointComplete(pContext->pBA, hrStatus);
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid apply initialize message.");
        break;
    }

    *pdwResult = static_cast<DWORD>(hrBA);

LExit:
    return hr;
}

static HRESULT ProcessBurnCacheMessages(
    __in PIPE_MESSAGE* pMsg,
    __in LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    BURN_ELEVATION_CACHE_MESSAGE_CONTEXT* pContext = static_cast<BURN_ELEVATION_CACHE_MESSAGE_CONTEXT*>(pvContext);
    BURN_CACHE_MESSAGE message = { };
    BOOL fProgressRoutine = FALSE;

    // Process the message.
    switch (pMsg->dwMessageType)
    {
    case BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_BEGIN:
        // read message parameters
        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, reinterpret_cast<DWORD*>(&message.begin.cacheStep));
        ExitOnFailure(hr, "Failed to read begin cache step.");

        message.type = BURN_CACHE_MESSAGE_BEGIN;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_COMPLETE:
        // read message parameters
        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, reinterpret_cast<DWORD*>(&message.complete.hrStatus));
        ExitOnFailure(hr, "Failed to read complete hresult.");

        message.type = BURN_CACHE_MESSAGE_COMPLETE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_SUCCESS:
        // read message parameters
        hr = BuffReadNumber64((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &message.success.qwFileSize);
        ExitOnFailure(hr, "Failed to read success file size.");

        message.type = BURN_CACHE_MESSAGE_SUCCESS;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_FAILURE:
        // read message parameters
        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, reinterpret_cast<DWORD*>(&message.failure.cacheStep));
        ExitOnFailure(hr, "Failed to read failure cache step.");

        message.type = BURN_CACHE_MESSAGE_FAILURE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_PROGRESS_ROUTINE:
        fProgressRoutine = TRUE;
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid burn cache message.");
        break;
    }

    if (fProgressRoutine)
    {
        hr = ProcessProgressRoutineMessage(pMsg, pContext->pfnProgress, pContext->pvContext, pdwResult);
    }
    else
    {
        hr = pContext->pfnCacheMessageHandler(&message, pContext->pvContext);
        *pdwResult = static_cast<DWORD>(hr);
    }

LExit:
    return hr;
}

static HRESULT ProcessGenericExecuteMessages(
    __in PIPE_MESSAGE* pMsg,
    __in LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    BURN_ELEVATION_GENERIC_MESSAGE_CONTEXT* pContext = static_cast<BURN_ELEVATION_GENERIC_MESSAGE_CONTEXT*>(pvContext);
    LPWSTR sczMessage = NULL;
    DWORD cFiles = 0;
    LPWSTR* rgwzFiles = NULL;
    GENERIC_EXECUTE_MESSAGE message = { };

    if (BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ACTION_COMPLETE == pMsg->dwMessageType)
    {
        hr = ProcessExecuteActionCompleteMessage((BYTE*)pMsg->pvData, pMsg->cbData, &pContext->restart, pdwResult);
        ExitOnFailure(hr, "Failed to process BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ACTION_COMPLETE message.");

        ExitFunction();
    }

    hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &message.dwUIHint);
    ExitOnFailure(hr, "Failed to allowed results.");

    // Process the message.
    switch (pMsg->dwMessageType)
    {
    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROGRESS:
        message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;

        // read message parameters
        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &message.progress.dwPercentage);
        ExitOnFailure(hr, "Failed to read progress.");
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROCESS_CANCEL:
        message.type = GENERIC_EXECUTE_MESSAGE_PROCESS_CANCEL;

        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &message.processCancel.dwProcessId);
        ExitOnFailure(hr, "Failed to read processId.");
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROCESS_STARTED:
        message.type = GENERIC_EXECUTE_MESSAGE_PROCESS_STARTED;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROCESS_COMPLETED:
        message.type = GENERIC_EXECUTE_MESSAGE_PROCESS_COMPLETED;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ERROR:
        message.type = GENERIC_EXECUTE_MESSAGE_ERROR;

        // read message parameters
        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &message.error.dwErrorCode);
        ExitOnFailure(hr, "Failed to read error code.");

        hr = BuffReadString((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &sczMessage);
        ExitOnFailure(hr, "Failed to read error message.");

        message.error.wzMessage = sczMessage;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_NETFX_FILES_IN_USE:
        message.type = GENERIC_EXECUTE_MESSAGE_NETFX_FILES_IN_USE;

        // read message parameters
        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &cFiles);
        ExitOnFailure(hr, "Failed to read file count.");

        rgwzFiles = (LPWSTR*)MemAlloc(sizeof(LPWSTR*) * cFiles, TRUE);
        ExitOnNull(rgwzFiles, hr, E_OUTOFMEMORY, "Failed to allocate buffer for files in use.");

        for (DWORD i = 0; i < cFiles; ++i)
        {
            hr = BuffReadString((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &rgwzFiles[i]);
            ExitOnFailure(hr, "Failed to read file name: %u", i);
        }

        message.filesInUse.cFiles = cFiles;
        message.filesInUse.rgwzFiles = (LPCWSTR*)rgwzFiles;
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid package message.");
        break;
    }

    // send message
    *pdwResult =  (DWORD)pContext->pfnGenericMessageHandler(&message, pContext->pvContext);

LExit:
    ReleaseStr(sczMessage);

    if (rgwzFiles)
    {
        for (DWORD i = 0; i < cFiles; ++i)
        {
            ReleaseStr(rgwzFiles[i]);
        }
        MemFree(rgwzFiles);
    }
    return hr;
}

static HRESULT ProcessMsiPackageMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    WIU_MSI_EXECUTE_MESSAGE message = { };
    DWORD cMsiData = 0;
    LPWSTR* rgwzMsiData = NULL;
    BURN_ELEVATION_MSI_MESSAGE_CONTEXT* pContext = static_cast<BURN_ELEVATION_MSI_MESSAGE_CONTEXT*>(pvContext);
    LPWSTR sczMessage = NULL;
    BOOL fRestartManager = FALSE;

    if (BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ACTION_COMPLETE == pMsg->dwMessageType)
    {
        hr = ProcessExecuteActionCompleteMessage((BYTE*)pMsg->pvData, pMsg->cbData, &pContext->restart, pdwResult);
        ExitOnFailure(hr, "Failed to process BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ACTION_COMPLETE message.");

        ExitFunction();
    }

    // Read MSI extended message data.
    hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &cMsiData);
    ExitOnFailure(hr, "Failed to read MSI data count.");

    if (cMsiData)
    {
        rgwzMsiData = (LPWSTR*)MemAlloc(sizeof(LPWSTR*) * cMsiData, TRUE);
        ExitOnNull(rgwzMsiData, hr, E_OUTOFMEMORY, "Failed to allocate buffer to read MSI data.");

        for (DWORD i = 0; i < cMsiData; ++i)
        {
            hr = BuffReadString((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &rgwzMsiData[i]);
            ExitOnFailure(hr, "Failed to read MSI data: %u", i);
        }

        message.cData = cMsiData;
        message.rgwzData = (LPCWSTR*)rgwzMsiData;
    }

    hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &message.dwUIHint);
    ExitOnFailure(hr, "Failed to read UI flags.");

    // Process the rest of the message.
    switch (pMsg->dwMessageType)
    {
    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROGRESS:
        // read message parameters
        message.type = WIU_MSI_EXECUTE_MESSAGE_PROGRESS;

        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &message.progress.dwPercentage);
        ExitOnFailure(hr, "Failed to read progress.");
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ERROR:
        // read message parameters
        message.type = WIU_MSI_EXECUTE_MESSAGE_ERROR;

        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &message.error.dwErrorCode);
        ExitOnFailure(hr, "Failed to read error code.");

        hr = BuffReadString((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &sczMessage);
        ExitOnFailure(hr, "Failed to read MSI execute error message.");
        message.error.wzMessage = sczMessage;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_MESSAGE:
        // read message parameters
        message.type = WIU_MSI_EXECUTE_MESSAGE_MSI_MESSAGE;

        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, (DWORD*)&message.msiMessage.mt);
        ExitOnFailure(hr, "Failed to read MSI execute message type.");

        hr = BuffReadString((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &sczMessage);
        ExitOnFailure(hr, "Failed to read MSI execute message.");
        message.msiMessage.wzMessage = sczMessage;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_FILES_IN_USE:
        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, (DWORD*)&fRestartManager);
        ExitOnFailure(hr, "Failed to read fRestartManager.");

        message.type = fRestartManager ? WIU_MSI_EXECUTE_MESSAGE_MSI_RM_FILES_IN_USE : WIU_MSI_EXECUTE_MESSAGE_MSI_FILES_IN_USE;
        message.msiFilesInUse.cFiles = cMsiData;
        message.msiFilesInUse.rgwzFiles = (LPCWSTR*)rgwzMsiData;
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid package message.");
        break;
    }

    // send message
    *pdwResult = (DWORD)pContext->pfnMessageHandler(&message, pContext->pvContext);

LExit:
    ReleaseStr(sczMessage);

    if (rgwzMsiData)
    {
        for (DWORD i = 0; i < cMsiData; ++i)
        {
            ReleaseStr(rgwzMsiData[i]);
        }

        MemFree(rgwzMsiData);
    }

    return hr;
}

static HRESULT ProcessLaunchApprovedExeMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    BURN_ELEVATION_LAUNCH_APPROVED_EXE_MESSAGE_CONTEXT* pContext = static_cast<BURN_ELEVATION_LAUNCH_APPROVED_EXE_MESSAGE_CONTEXT*>(pvContext);
    DWORD dwProcessId = 0;

    // Process the message.
    switch (pMsg->dwMessageType)
    {
    case BURN_ELEVATION_MESSAGE_TYPE_LAUNCH_APPROVED_EXE_PROCESSID:
        // read message parameters
        hr = BuffReadNumber((BYTE*)pMsg->pvData, pMsg->cbData, &iData, &dwProcessId);
        ExitOnFailure(hr, "Failed to read approved exe process id.");
        pContext->dwProcessId = dwProcessId;
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid launch approved exe message.");
        break;
    }

    *pdwResult = static_cast<DWORD>(hr);

LExit:
    return hr;
}

static HRESULT ProcessProgressRoutineMessage(
    __in PIPE_MESSAGE* pMsg,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LARGE_INTEGER liTotalFileSize = { };
    LARGE_INTEGER liTotalBytesTransferred = { };
    LARGE_INTEGER liStreamSize = { };
    LARGE_INTEGER liStreamBytesTransferred = { };
    DWORD dwStreamNumber = 0;
    DWORD dwCallbackReason = CALLBACK_CHUNK_FINISHED;
    HANDLE hSourceFile = INVALID_HANDLE_VALUE;
    HANDLE hDestinationFile = INVALID_HANDLE_VALUE;

    hr = BuffReadNumber64((BYTE*)pMsg->pvData, pMsg->cbData, &iData, reinterpret_cast<DWORD64*>(&liTotalFileSize.QuadPart));
    ExitOnFailure(hr, "Failed to read total file size for progress.");

    hr = BuffReadNumber64((BYTE*)pMsg->pvData, pMsg->cbData, &iData, reinterpret_cast<DWORD64*>(&liTotalBytesTransferred.QuadPart));
    ExitOnFailure(hr, "Failed to read total bytes transferred for progress.");

    *pdwResult = pfnProgress(liTotalFileSize, liTotalBytesTransferred, liStreamSize, liStreamBytesTransferred, dwStreamNumber, dwCallbackReason, hSourceFile, hDestinationFile, pvContext);

LExit:
    return hr;
}

static HRESULT ProcessElevatedChildMessage(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    BURN_ELEVATION_CHILD_MESSAGE_CONTEXT* pContext = static_cast<BURN_ELEVATION_CHILD_MESSAGE_CONTEXT*>(pvContext);
    HRESULT hrResult = S_OK;
    BOOTSTRAPPER_APPLY_RESTART restart = BOOTSTRAPPER_APPLY_RESTART_NONE;
    BOOL fSendRestart = FALSE;

    switch (pMsg->dwMessageType)
    {
    case BURN_ELEVATION_MESSAGE_TYPE_BEGIN_MSI_TRANSACTION:
        hrResult = OnMsiBeginTransaction(pContext->pPackages, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_COMMIT_MSI_TRANSACTION:
        hrResult = OnMsiCommitTransaction(pContext->pPackages, (BYTE*)pMsg->pvData, pMsg->cbData, &restart);
        fSendRestart = TRUE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_ROLLBACK_MSI_TRANSACTION:
        hrResult = OnMsiRollbackTransaction(pContext->pPackages, (BYTE*)pMsg->pvData, pMsg->cbData, &restart);
        fSendRestart = TRUE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE:
        hrResult = OnApplyInitialize(pContext->hPipe, pContext->pVariables, pContext->pRegistration, pContext->pPackages, pContext->phLock, pContext->pfDisabledAutomaticUpdates, pContext->pfApplying, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_APPLY_UNINITIALIZE:
        hrResult = OnApplyUninitialize(pContext->phLock, pContext->pfApplying, pContext->pfDisabledAutomaticUpdates);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_SESSION_BEGIN:
        hrResult = OnSessionBegin(pContext->pCache, pContext->pRegistration, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_SESSION_END:
        hrResult = OnSessionEnd(pContext->pCache, pContext->pPackages, pContext->pRegistration, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_SAVE_STATE:
        hrResult = OnSaveState(pContext->pRegistration, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_PROCESS_DEPENDENT_REGISTRATION:
        hrResult = OnProcessDependentRegistration(pContext->pRegistration, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_RELATED_BUNDLE:
        hrResult = OnExecuteRelatedBundle(pContext->hPipe, pContext->pCache, &pContext->pRegistration->relatedBundles, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData, &restart);
        fSendRestart = TRUE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_BUNDLE_PACKAGE:
        hrResult = OnExecuteBundlePackage(pContext->hPipe, pContext->pCache, pContext->pPackages, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData, &restart);
        fSendRestart = TRUE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_EXE_PACKAGE:
        hrResult = OnExecuteExePackage(pContext->hPipe, pContext->pCache, pContext->pPackages, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData, &restart);
        fSendRestart = TRUE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_PACKAGE:
        hrResult = OnExecuteMsiPackage(pContext->hPipe, pContext->pCache, pContext->pPackages, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData, &restart);
        fSendRestart = TRUE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSP_PACKAGE:
        hrResult = OnExecuteMspPackage(pContext->hPipe, pContext->pCache, pContext->pPackages, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData, &restart);
        fSendRestart = TRUE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSU_PACKAGE:
        hrResult = OnExecuteMsuPackage(pContext->hPipe, pContext->pCache, pContext->pPackages, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData, &restart);
        fSendRestart = TRUE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PACKAGE_PROVIDER:
        hrResult = OnExecutePackageProviderAction(pContext->pPackages, &pContext->pRegistration->relatedBundles, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PACKAGE_DEPENDENCY:
        hrResult = OnExecutePackageDependencyAction(pContext->pPackages, &pContext->pRegistration->relatedBundles, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_CLEAN_PACKAGE:
        hrResult = OnCleanPackage(pContext->pCache, pContext->pPackages, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_LAUNCH_APPROVED_EXE:
        hrResult = OnLaunchApprovedExe(pContext->hPipe, pContext->pApprovedExes, pContext->pCache, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_UNINSTALL_MSI_COMPATIBLE_PACKAGE:
        hrResult = OnUninstallMsiCompatiblePackage(pContext->hPipe, pContext->pCache, pContext->pPackages, pContext->pVariables, (BYTE*)pMsg->pvData, pMsg->cbData, &restart);
        fSendRestart = TRUE;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_CLEAN_COMPATIBLE_PACKAGE:
        hrResult = OnCleanCompatiblePackage(pContext->pCache, pContext->pPackages, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    default:
        ExitWithRootFailure(hr, E_INVALIDARG, "Unexpected elevated message sent to child process, msg: %u", pMsg->dwMessageType);
    }

    if (fSendRestart)
    {
        hr = ElevatedOnExecuteActionComplete(pContext->hPipe, restart);
        ExitOnFailure(hr, "ElevatedOnExecuteActionComplete failed.");
    }

    *pdwResult = (DWORD)hrResult;

LExit:
    return hr;
}

static HRESULT ProcessElevatedChildCacheMessage(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    BURN_ELEVATION_CHILD_MESSAGE_CONTEXT* pContext = static_cast<BURN_ELEVATION_CHILD_MESSAGE_CONTEXT*>(pvContext);
    HRESULT hrResult = S_OK;

    switch (pMsg->dwMessageType)
    {
    case BURN_ELEVATION_MESSAGE_TYPE_CACHE_PREPARE_PACKAGE:
        hrResult = OnCachePreparePackage(pContext->pCache, pContext->pPackages, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_CACHE_COMPLETE_PAYLOAD:
        hrResult = OnCacheCompletePayload(pContext->hPipe, pContext->pCache, pContext->pPackages, pContext->pPayloads, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_CACHE_VERIFY_PAYLOAD:
        hrResult = OnCacheVerifyPayload(pContext->hPipe, pContext->pPackages, pContext->pPayloads, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_CACHE_CLEANUP:
        OnCacheCleanup(pContext->pCache);
        hrResult = S_OK;
        break;

    case BURN_ELEVATION_MESSAGE_TYPE_CLEAN_PACKAGE:
        hrResult = OnCleanPackage(pContext->pCache, pContext->pPackages, (BYTE*)pMsg->pvData, pMsg->cbData);
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Unexpected elevated cache message sent to child process, msg: %u", pMsg->dwMessageType);
    }

    *pdwResult = (DWORD)hrResult;

LExit:
    return hr;
}

static HRESULT ProcessExecuteActionCompleteMessage(
    __in BYTE* pbData,
    __in DWORD cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;

    hr = BuffReadNumber(pbData, cbData, &iData, reinterpret_cast<DWORD*>(pRestart));
    ExitOnFailure(hr, "Failed to read execute action restart result.");

    *pdwResult = (DWORD)hr;

LExit:
    return hr;
}

static HRESULT OnApplyInitialize(
    __in HANDLE hPipe,
    __in BURN_VARIABLES* pVariables,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PACKAGES* pPackages,
    __in HANDLE* phLock,
    __in BOOL* pfDisabledWindowsUpdate,
    __in BOOL* pfApplying,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    DWORD dwAction = 0;
    DWORD dwAUAction = 0;
    DWORD dwTakeSystemRestorePoint = 0;
    LPWSTR sczBundleName = NULL;
    HRESULT hrStatus = S_OK;

    // Deserialize message data.
    hr = BuffReadNumber(pbData, cbData, &iData, &dwAction);
    ExitOnFailure(hr, "Failed to read action.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwAUAction);
    ExitOnFailure(hr, "Failed to read update action.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwTakeSystemRestorePoint);
    ExitOnFailure(hr, "Failed to read system restore point action.");

    hr = VariableDeserialize(pVariables, FALSE, pbData, cbData, &iData);
    ExitOnFailure(hr, "Failed to read variables.");

    // Initialize.
    hr = ApplyLock(TRUE, phLock);
    ExitOnFailure(hr, "Failed to acquire lock due to setup in other session.");

    *pfApplying = TRUE;

    // Detect.
    hr = ElevatedProcessDetect(pRegistration, pVariables, pPackages);
    ExitOnFailure(hr, "Failed to run detection in elevated process.");

    // Attempt to pause AU with best effort.
    if (BURN_AU_PAUSE_ACTION_IFELEVATED == dwAUAction || BURN_AU_PAUSE_ACTION_IFELEVATED_NORESUME == dwAUAction)
    {
        hr = ElevatedOnPauseAUBegin(hPipe);
        ExitOnFailure(hr, "ElevatedOnPauseAUBegin failed.");

        LogId(REPORT_STANDARD, MSG_PAUSE_AU_STARTING);

        hrStatus = hr = WuaPauseAutomaticUpdates();
        if (FAILED(hr))
        {
            LogId(REPORT_STANDARD, MSG_FAILED_PAUSE_AU, hr);
            hr = S_OK;
        }
        else
        {
            LogId(REPORT_STANDARD, MSG_PAUSE_AU_SUCCEEDED);
            if (BURN_AU_PAUSE_ACTION_IFELEVATED == dwAUAction)
            {
                *pfDisabledWindowsUpdate = TRUE;
            }
        }

        hr = ElevatedOnPauseAUComplete(hPipe, hrStatus);
        ExitOnFailure(hr, "ElevatedOnPauseAUComplete failed.");
    }

    if (dwTakeSystemRestorePoint)
    {
        hr = VariableGetString(pVariables, BURN_BUNDLE_NAME, &sczBundleName);
        if (FAILED(hr))
        {
            hr = S_OK;
            ExitFunction();
        }

        hr = ElevatedOnSystemRestorePointBegin(hPipe);
        ExitOnFailure(hr, "ElevatedOnSystemRestorePointBegin failed.");

        LogId(REPORT_STANDARD, MSG_SYSTEM_RESTORE_POINT_STARTING);

        SRP_ACTION restoreAction = SRP_ACTION_UNKNOWN;
        switch (static_cast<BOOTSTRAPPER_ACTION>(dwAction))
        {
        case BOOTSTRAPPER_ACTION_INSTALL:
            restoreAction = SRP_ACTION_INSTALL;
            break;
        case BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL: __fallthrough;
        case BOOTSTRAPPER_ACTION_UNINSTALL:
            restoreAction = SRP_ACTION_UNINSTALL;
            break;
        default:
            restoreAction = SRP_ACTION_MODIFY;
            break;
        }

        hrStatus = hr = SrpCreateRestorePoint(sczBundleName, restoreAction);
        if (SUCCEEDED(hr))
        {
            LogId(REPORT_STANDARD, MSG_SYSTEM_RESTORE_POINT_SUCCEEDED);
        }
        else if (E_NOTIMPL == hr)
        {
            LogId(REPORT_STANDARD, MSG_SYSTEM_RESTORE_POINT_DISABLED);
            hr = S_OK;
        }
        else
        {
            LogId(REPORT_STANDARD, MSG_SYSTEM_RESTORE_POINT_FAILED, hr);
            hr = S_OK;
        }

        hr = ElevatedOnSystemRestorePointComplete(hPipe, hrStatus);
        ExitOnFailure(hr, "ElevatedOnSystemRestorePointComplete failed.");
    }

LExit:
    ReleaseStr(sczBundleName);
    return hr;
}

static HRESULT ElevatedProcessDetect(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PACKAGES* pPackages
    )
{
    HRESULT hr = S_OK;

    DetectReset(pRegistration, pPackages);

    hr = RegistrationSetDynamicVariables(pRegistration, pVariables);
    ExitOnFailure(hr, "Failed to reset the dynamic registration variables during elevated detect.");

    hr = RelatedBundlesInitializeForScope(TRUE, pRegistration, &pRegistration->relatedBundles);
    ExitOnFailure(hr, "Failed to initialize per-machine related bundles.");

    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        BURN_PACKAGE* pPackage = pPackages->rgPackages + i;

        hr = DependencyDetectCompatibleEntry(pPackage, pRegistration);
        ExitOnFailure(hr, "Failed to detect per-machine compatible entry for package: %ls", pPackage->sczId);

        switch (pPackage->type)
        {
        case BURN_PACKAGE_TYPE_MSI:
            hr = MsiEngineDetectCompatiblePackage(pPackage);
            ExitOnFailure(hr, "Failed to detect per-machine compatible package for package: %ls", pPackage->sczId);

            break;
        }
    }

LExit:
    return hr;
}

static HRESULT OnApplyUninitialize(
    __in HANDLE* phLock,
    __in BOOL* pfApplying,
    __in BOOL* pfDisabledAutomaticUpdates
    )
{
    Assert(phLock);

    // TODO: end system restore point.

    *pfApplying = FALSE;

    if (*pfDisabledAutomaticUpdates)
    {
        *pfDisabledAutomaticUpdates = FALSE;

        ElevationChildResumeAutomaticUpdates();
    }

    if (*phLock)
    {
        ::ReleaseMutex(*phLock);
        ::CloseHandle(*phLock);
        *phLock = NULL;
    }

    return S_OK;
}

static HRESULT OnSessionBegin(
    __in BURN_CACHE* pCache,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczEngineWorkingPath = NULL;
    DWORD dwRegistrationOperations = 0;
    DWORD64 qwEstimatedSize = 0;
    DWORD dwRegistrationType = 0;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczEngineWorkingPath);
    ExitOnFailure(hr, "Failed to read engine working path.");

    hr = BuffReadString(pbData, cbData, &iData, &pRegistration->sczResumeCommandLine);
    ExitOnFailure(hr, "Failed to read resume command line.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&pRegistration->fDisableResume);
    ExitOnFailure(hr, "Failed to read resume flag.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwRegistrationOperations);
    ExitOnFailure(hr, "Failed to read registration operations.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&pRegistration->fDetectedForeignProviderKeyBundleCode);
    ExitOnFailure(hr, "Failed to read dependency registration action.");

    hr = BuffReadNumber64(pbData, cbData, &iData, &qwEstimatedSize);
    ExitOnFailure(hr, "Failed to read estimated size.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwRegistrationType);
    ExitOnFailure(hr, "Failed to read dependency registration action.");

    hr = VariableDeserialize(pVariables, FALSE, pbData, cbData, &iData);
    ExitOnFailure(hr, "Failed to read variables.");

    // Begin session in per-machine process.
    hr = RegistrationSessionBegin(sczEngineWorkingPath, pRegistration, pCache, pVariables, dwRegistrationOperations, qwEstimatedSize, (BOOTSTRAPPER_REGISTRATION_TYPE)dwRegistrationType);
    ExitOnFailure(hr, "Failed to begin registration session.");

LExit:
    ReleaseStr(sczEngineWorkingPath);

    return hr;
}

static HRESULT OnSessionEnd(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    DWORD dwResumeMode = 0;
    DWORD dwRestart = 0;
    DWORD64 qwEstimatedSize = 0;
    DWORD dwRegistrationType = 0;

    // Deserialize message data.
    hr = BuffReadNumber(pbData, cbData, &iData, &dwResumeMode);
    ExitOnFailure(hr, "Failed to read resume mode enum.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwRestart);
    ExitOnFailure(hr, "Failed to read restart enum.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&pRegistration->fDetectedForeignProviderKeyBundleCode);
    ExitOnFailure(hr, "Failed to read dependency registration action.");

    hr = BuffReadNumber64(pbData, cbData, &iData, &qwEstimatedSize);
    ExitOnFailure(hr, "Failed to read estimated size.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwRegistrationType);
    ExitOnFailure(hr, "Failed to read dependency registration action.");

    // suspend session in per-machine process
    hr = RegistrationSessionEnd(pRegistration, pCache, pVariables, pPackages, (BURN_RESUME_MODE)dwResumeMode, (BOOTSTRAPPER_APPLY_RESTART)dwRestart, qwEstimatedSize, (BOOTSTRAPPER_REGISTRATION_TYPE)dwRegistrationType);
    ExitOnFailure(hr, "Failed to suspend registration session.");

LExit:
    return hr;
}

static HRESULT OnSaveState(
    __in BURN_REGISTRATION* pRegistration,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;

    // save state in per-machine process
    hr = RegistrationSaveState(pRegistration, pbData, cbData);
    ExitOnFailure(hr, "Failed to save state.");

LExit:
    return hr;
}

static HRESULT OnCachePreparePackage(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR scz = NULL;
    BURN_PACKAGE* pPackage = NULL;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &scz);
    ExitOnFailure(hr, "Failed to read package id.");

    if (scz && *scz)
    {
        hr = PackageFindById(pPackages, scz, &pPackage);
        ExitOnFailure(hr, "Failed to find package: %ls", scz);
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid data passed to cache prepare package.");
    }

    hr = CachePreparePackage(pCache, pPackage);
    ExitOnFailure(hr, "Failed to prepare cache package.");

LExit:
    ReleaseStr(scz);

    return hr;
}

static HRESULT OnCacheCompletePayload(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOADS* pPayloads,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR scz = NULL;
    BURN_PACKAGE* pPackage = NULL;
    BURN_PAYLOAD* pPayload = NULL;
    LPWSTR sczUnverifiedPath = NULL;
    BOOL fMove = FALSE;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &scz);
    ExitOnFailure(hr, "Failed to read package id.");

    if (scz && *scz)
    {
        hr = PackageFindById(pPackages, scz, &pPackage);
        ExitOnFailure(hr, "Failed to find package: %ls", scz);
    }

    hr = BuffReadString(pbData, cbData, &iData, &scz);
    ExitOnFailure(hr, "Failed to read payload id.");

    if (scz && *scz)
    {
        hr = PayloadFindById(pPayloads, scz, &pPayload);
        ExitOnFailure(hr, "Failed to find payload: %ls", scz);
    }

    hr = BuffReadString(pbData, cbData, &iData, &sczUnverifiedPath);
    ExitOnFailure(hr, "Failed to read unverified path.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&fMove);
    ExitOnFailure(hr, "Failed to read move flag.");

    if (pPackage && pPayload) // complete payload.
    {
        hr = CacheCompletePayload(pCache, pPackage->fPerMachine, pPayload, pPackage->sczCacheId, sczUnverifiedPath, fMove, BurnCacheMessageHandler, ElevatedProgressRoutine, hPipe);
        ExitOnFailure(hr, "Failed to cache payload: %ls", pPayload->sczKey);
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid data passed to cache complete payload.");
    }

LExit:
    ReleaseStr(sczUnverifiedPath);
    ReleaseStr(scz);

    return hr;
}

static HRESULT OnCacheVerifyPayload(
    __in HANDLE hPipe,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOADS* pPayloads,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR scz = NULL;
    BURN_PACKAGE* pPackage = NULL;
    BURN_PAYLOAD* pPayload = NULL;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &scz);
    ExitOnFailure(hr, "Failed to read package id.");

    if (scz && *scz)
    {
        hr = PackageFindById(pPackages, scz, &pPackage);
        ExitOnFailure(hr, "Failed to find package: %ls", scz);
    }

    hr = BuffReadString(pbData, cbData, &iData, &scz);
    ExitOnFailure(hr, "Failed to read payload id.");

    if (scz && *scz)
    {
        hr = PayloadFindById(pPayloads, scz, &pPayload);
        ExitOnFailure(hr, "Failed to find payload: %ls", scz);
    }

    if (pPackage && pPayload)
    {
        if (!pPackage->sczCacheFolder)
        {
            hr = E_INVALIDSTATE;
            ExitOnRootFailure(hr, "Cache verify payload called without starting its package.");
        }

        hr = CacheVerifyPayload(pPayload, pPackage->sczCacheFolder, BurnCacheMessageHandler, ElevatedProgressRoutine, hPipe);
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid data passed to cache verify payload.");
    }
    // Nothing should be logged on failure.

LExit:
    ReleaseStr(scz);

    return hr;
}

static void OnCacheCleanup(
    __in BURN_CACHE* pCache
    )
{
    CacheCleanup(TRUE, pCache);
}

static HRESULT OnProcessDependentRegistration(
    __in const BURN_REGISTRATION* pRegistration,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    BURN_DEPENDENT_REGISTRATION_ACTION action = { };

    // Deserialize message data.
    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&action.type);
    ExitOnFailure(hr, "Failed to read action type.");

    hr = BuffReadString(pbData, cbData, &iData, &action.sczBundleCode);
    ExitOnFailure(hr, "Failed to read bundle code.");

    hr = BuffReadString(pbData, cbData, &iData, &action.sczDependentProviderKey);
    ExitOnFailure(hr, "Failed to read dependent provider key.");

    // Execute the registration action.
    hr = DependencyProcessDependentRegistration(pRegistration, &action);
    ExitOnFailure(hr, "Failed to execute dependent registration action for provider key: %ls", action.sczDependentProviderKey);

LExit:
    // TODO: do the right thing here.
    //DependencyUninitializeRegistrationAction(&action);
    ReleaseStr(action.sczDependentProviderKey);
    ReleaseStr(action.sczBundleCode)

    return hr;
}

static HRESULT OnExecuteRelatedBundle(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackage = NULL;
    DWORD dwPlanRelationType = 0;
    DWORD dwRollback = 0;
    BURN_EXECUTE_ACTION executeAction = { };
    LPWSTR sczIgnoreDependencies = NULL;
    LPWSTR sczAncestors = NULL;
    LPWSTR sczEngineWorkingDirectory = NULL;
    *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;

    executeAction.type = BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
    ExitOnFailure(hr, "Failed to read related bundle code.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.relatedBundle.action);
    ExitOnFailure(hr, "Failed to read action.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwPlanRelationType);
    ExitOnFailure(hr, "Failed to read planRelationType.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwRollback);
    ExitOnFailure(hr, "Failed to read rollback.");

    hr = BuffReadString(pbData, cbData, &iData, &sczIgnoreDependencies);
    ExitOnFailure(hr, "Failed to read the list of dependencies to ignore.");

    hr = BuffReadString(pbData, cbData, &iData, &sczAncestors);
    ExitOnFailure(hr, "Failed to read the list of ancestors.");

    hr = BuffReadString(pbData, cbData, &iData, &sczEngineWorkingDirectory);
    ExitOnFailure(hr, "Failed to read the custom working directory.");

    hr = VariableDeserialize(pVariables, FALSE, pbData, cbData, &iData);
    ExitOnFailure(hr, "Failed to read variables.");

    hr = RelatedBundleFindById(pRelatedBundles, sczPackage, &executeAction.relatedBundle.pRelatedBundle);
    ExitOnFailure(hr, "Failed to find related bundle: %ls", sczPackage);

    executeAction.relatedBundle.pRelatedBundle->planRelationType = (BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE)dwPlanRelationType;

    // Pass the list of dependencies to ignore, if any, to the related bundle.
    if (sczIgnoreDependencies && *sczIgnoreDependencies)
    {
        hr = StrAllocString(&executeAction.relatedBundle.sczIgnoreDependencies, sczIgnoreDependencies, 0);
        ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
    }

    // Pass the list of ancestors, if any, to the related bundle.
    if (sczAncestors && *sczAncestors)
    {
        hr = StrAllocString(&executeAction.relatedBundle.sczAncestors, sczAncestors, 0);
        ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
    }

    if (sczEngineWorkingDirectory && *sczEngineWorkingDirectory)
    {
        hr = StrAllocString(&executeAction.relatedBundle.sczEngineWorkingDirectory, sczEngineWorkingDirectory, 0);
        ExitOnFailure(hr, "Failed to allocate the custom working directory.");
    }

    // Execute related bundle.
    hr = BundlePackageEngineExecuteRelatedBundle(&executeAction, pCache, pVariables, static_cast<BOOL>(dwRollback), GenericExecuteMessageHandler, hPipe, pRestart);
    ExitOnFailure(hr, "Failed to execute related bundle.");

LExit:
    ReleaseStr(sczEngineWorkingDirectory);
    ReleaseStr(sczAncestors);
    ReleaseStr(sczIgnoreDependencies);
    ReleaseStr(sczPackage);
    PlanUninitializeExecuteAction(&executeAction);

    return hr;
}

static HRESULT OnExecuteBundlePackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackage = NULL;
    BOOL fRollback = FALSE;
    BOOL fCacheAvailable = FALSE;
    BURN_EXECUTE_ACTION executeAction = { };
    LPWSTR sczIgnoreDependencies = NULL;
    LPWSTR sczAncestors = NULL;
    LPWSTR sczEngineWorkingDirectory = NULL;
    *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;

    executeAction.type = BURN_EXECUTE_ACTION_TYPE_BUNDLE_PACKAGE;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
    ExitOnFailure(hr, "Failed to read BUNDLE package id.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.bundlePackage.action);
    ExitOnFailure(hr, "Failed to read action.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&fRollback);
    ExitOnFailure(hr, "Failed to read rollback.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&fCacheAvailable);
    ExitOnFailure(hr, "Failed to read fCacheAvailable.");

    hr = BuffReadString(pbData, cbData, &iData, &executeAction.bundlePackage.sczParent);
    ExitOnFailure(hr, "Failed to read the parent.");

    hr = BuffReadString(pbData, cbData, &iData, &sczIgnoreDependencies);
    ExitOnFailure(hr, "Failed to read the list of dependencies to ignore.");

    hr = BuffReadString(pbData, cbData, &iData, &sczAncestors);
    ExitOnFailure(hr, "Failed to read the list of ancestors.");

    hr = BuffReadString(pbData, cbData, &iData, &sczEngineWorkingDirectory);
    ExitOnFailure(hr, "Failed to read the custom working directory.");

    hr = VariableDeserialize(pVariables, FALSE, pbData, cbData, &iData);
    ExitOnFailure(hr, "Failed to read variables.");

    hr = PackageFindById(pPackages, sczPackage, &executeAction.bundlePackage.pPackage);
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackage);

    if (BURN_PACKAGE_TYPE_BUNDLE != executeAction.bundlePackage.pPackage->type)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Package is not a BUNDLE package: %ls", sczPackage);
    }

    // Pass the list of dependencies to ignore, if any, to the related bundle.
    if (sczIgnoreDependencies && *sczIgnoreDependencies)
    {
        hr = StrAllocString(&executeAction.bundlePackage.sczIgnoreDependencies, sczIgnoreDependencies, 0);
        ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
    }

    // Pass the list of ancestors, if any, to the related bundle.
    if (sczAncestors && *sczAncestors)
    {
        hr = StrAllocString(&executeAction.bundlePackage.sczAncestors, sczAncestors, 0);
        ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
    }

    if (sczEngineWorkingDirectory && *sczEngineWorkingDirectory)
    {
        hr = StrAllocString(&executeAction.bundlePackage.sczEngineWorkingDirectory, sczEngineWorkingDirectory, 0);
        ExitOnFailure(hr, "Failed to allocate the custom working directory.");
    }

    // Execute BUNDLE package.
    hr = BundlePackageEngineExecutePackage(&executeAction, pCache, pVariables, fRollback, fCacheAvailable, GenericExecuteMessageHandler, hPipe, pRestart);
    ExitOnFailure(hr, "Failed to execute BUNDLE package.");

LExit:
    ReleaseStr(sczEngineWorkingDirectory);
    ReleaseStr(sczAncestors);
    ReleaseStr(sczIgnoreDependencies);
    ReleaseStr(sczPackage);
    PlanUninitializeExecuteAction(&executeAction);

    return hr;
}

static HRESULT OnExecuteExePackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackage = NULL;
    DWORD dwRollback = 0;
    BURN_EXECUTE_ACTION executeAction = { };
    LPWSTR sczAncestors = NULL;
    LPWSTR sczEngineWorkingDirectory = NULL;
    *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;

    executeAction.type = BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
    ExitOnFailure(hr, "Failed to read EXE package id.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.exePackage.action);
    ExitOnFailure(hr, "Failed to read action.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwRollback);
    ExitOnFailure(hr, "Failed to read rollback.");

    hr = BuffReadString(pbData, cbData, &iData, &sczAncestors);
    ExitOnFailure(hr, "Failed to read the list of ancestors.");

    hr = BuffReadString(pbData, cbData, &iData, &sczEngineWorkingDirectory);
    ExitOnFailure(hr, "Failed to read the custom working directory.");

    hr = VariableDeserialize(pVariables, FALSE, pbData, cbData, &iData);
    ExitOnFailure(hr, "Failed to read variables.");

    hr = PackageFindById(pPackages, sczPackage, &executeAction.exePackage.pPackage);
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackage);

    if (BURN_PACKAGE_TYPE_EXE != executeAction.exePackage.pPackage->type)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Package is not an EXE package: %ls", sczPackage);
    }

    // Pass the list of ancestors, if any, to the related bundle.
    if (sczAncestors && *sczAncestors)
    {
        hr = StrAllocString(&executeAction.exePackage.sczAncestors, sczAncestors, 0);
        ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
    }

    if (sczEngineWorkingDirectory && *sczEngineWorkingDirectory)
    {
        hr = StrAllocString(&executeAction.exePackage.sczEngineWorkingDirectory, sczEngineWorkingDirectory, 0);
        ExitOnFailure(hr, "Failed to allocate the custom working directory.");
    }

    // Execute EXE package.
    hr = ExeEngineExecutePackage(&executeAction, pCache, pVariables, static_cast<BOOL>(dwRollback), GenericExecuteMessageHandler, hPipe, pRestart);
    ExitOnFailure(hr, "Failed to execute EXE package.");

LExit:
    ReleaseStr(sczEngineWorkingDirectory);
    ReleaseStr(sczAncestors);
    ReleaseStr(sczPackage);
    PlanUninitializeExecuteAction(&executeAction);

    return hr;
}

static HRESULT OnExecuteMsiPackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackage = NULL;
    HWND hwndParent = NULL;
    BOOL fRollback = FALSE;
    BURN_EXECUTE_ACTION executeAction = { };
    *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;

    executeAction.type = BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE;

    // Deserialize message data.
    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&fRollback);
    ExitOnFailure(hr, "Failed to read rollback flag.");

    hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
    ExitOnFailure(hr, "Failed to read MSI package id.");

    hr = PackageFindById(pPackages, sczPackage, &executeAction.msiPackage.pPackage);
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackage);

    hr = BuffReadPointer(pbData, cbData, &iData, (DWORD_PTR*)&hwndParent);
    ExitOnFailure(hr, "Failed to read parent hwnd.");

    hr = BuffReadString(pbData, cbData, &iData, &executeAction.msiPackage.sczLogPath);
    ExitOnFailure(hr, "Failed to read package log.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.msiPackage.actionMsiProperty);
    ExitOnFailure(hr, "Failed to read actionMsiProperty.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.msiPackage.uiLevel);
    ExitOnFailure(hr, "Failed to read UI level.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.msiPackage.fDisableExternalUiHandler);
    ExitOnFailure(hr, "Failed to read fDisableExternalUiHandler.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.msiPackage.fileVersioning);
    ExitOnFailure(hr, "Failed to read fileVersioning.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.msiPackage.action);
    ExitOnFailure(hr, "Failed to read action.");

    // Read feature actions.
    if (executeAction.msiPackage.pPackage->Msi.cFeatures)
    {
        executeAction.msiPackage.rgFeatures = (BOOTSTRAPPER_FEATURE_ACTION*)MemAlloc(executeAction.msiPackage.pPackage->Msi.cFeatures * sizeof(BOOTSTRAPPER_FEATURE_ACTION), TRUE);
        ExitOnNull(executeAction.msiPackage.rgFeatures, hr, E_OUTOFMEMORY, "Failed to allocate memory for feature actions.");

        for (DWORD i = 0; i < executeAction.msiPackage.pPackage->Msi.cFeatures; ++i)
        {
            hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.msiPackage.rgFeatures[i]);
            ExitOnFailure(hr, "Failed to read feature action.");
        }
    }

    // Read slipstream patches actions.
    if (executeAction.msiPackage.pPackage->Msi.cSlipstreamMspPackages)
    {
        for (DWORD i = 0; i < executeAction.msiPackage.pPackage->Msi.cSlipstreamMspPackages; ++i)
        {
            BURN_SLIPSTREAM_MSP* pSlipstreamMsp = executeAction.msiPackage.pPackage->Msi.rgSlipstreamMsps + i;
            BOOTSTRAPPER_ACTION_STATE* pAction = fRollback ? &pSlipstreamMsp->rollback : &pSlipstreamMsp->execute;
            hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)pAction);
            ExitOnFailure(hr, "Failed to read slipstream action.");
        }
    }

    hr = VariableDeserialize(pVariables, FALSE, pbData, cbData, &iData);
    ExitOnFailure(hr, "Failed to read variables.");

    if (BURN_PACKAGE_TYPE_MSI != executeAction.msiPackage.pPackage->type)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Package is not an MSI package: %ls", sczPackage);
    }

    // Execute MSI package.
    hr = MsiEngineExecutePackage(hwndParent, &executeAction, pCache, pVariables, fRollback, MsiExecuteMessageHandler, hPipe, pRestart);
    ExitOnFailure(hr, "Failed to execute MSI package.");

LExit:
    ReleaseStr(sczPackage);
    PlanUninitializeExecuteAction(&executeAction);

    return hr;
}

static HRESULT OnExecuteMspPackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackage = NULL;
    HWND hwndParent = NULL;
    BOOL fRollback = FALSE;
    BURN_EXECUTE_ACTION executeAction = { };
    *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;

    executeAction.type = BURN_EXECUTE_ACTION_TYPE_MSP_TARGET;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
    ExitOnFailure(hr, "Failed to read MSP package id.");

    hr = PackageFindById(pPackages, sczPackage, &executeAction.mspTarget.pPackage);
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackage);

    hr = BuffReadPointer(pbData, cbData, &iData, (DWORD_PTR*)&hwndParent);
    ExitOnFailure(hr, "Failed to read parent hwnd.");

    executeAction.mspTarget.fPerMachineTarget = TRUE; // we're in the elevated process, clearly we're targeting a per-machine product.

    hr = BuffReadString(pbData, cbData, &iData, &executeAction.mspTarget.sczTargetProductCode);
    ExitOnFailure(hr, "Failed to read target product code.");

    hr = BuffReadString(pbData, cbData, &iData, &executeAction.mspTarget.sczLogPath);
    ExitOnFailure(hr, "Failed to read package log.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.mspTarget.actionMsiProperty);
    ExitOnFailure(hr, "Failed to read actionMsiProperty.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.mspTarget.uiLevel);
    ExitOnFailure(hr, "Failed to read UI level.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.mspTarget.fDisableExternalUiHandler);
    ExitOnFailure(hr, "Failed to read fDisableExternalUiHandler.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.mspTarget.fileVersioning);
    ExitOnFailure(hr, "Failed to read fileVersioning.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.mspTarget.action);
    ExitOnFailure(hr, "Failed to read action.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&executeAction.mspTarget.cOrderedPatches);
    ExitOnFailure(hr, "Failed to read count of ordered patches.");

    if (executeAction.mspTarget.cOrderedPatches)
    {
        executeAction.mspTarget.rgOrderedPatches = (BURN_ORDERED_PATCHES*)MemAlloc(executeAction.mspTarget.cOrderedPatches * sizeof(BURN_ORDERED_PATCHES), TRUE);
        ExitOnNull(executeAction.mspTarget.rgOrderedPatches, hr, E_OUTOFMEMORY, "Failed to allocate memory for ordered patches.");

        for (DWORD i = 0; i < executeAction.mspTarget.cOrderedPatches; ++i)
        {
            hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
            ExitOnFailure(hr, "Failed to read ordered patch package id.");

            hr = PackageFindById(pPackages, sczPackage, &executeAction.mspTarget.rgOrderedPatches[i].pPackage);
            ExitOnFailure(hr, "Failed to find ordered patch package: %ls", sczPackage);
        }
    }

    hr = VariableDeserialize(pVariables, FALSE, pbData, cbData, &iData);
    ExitOnFailure(hr, "Failed to read variables.");

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&fRollback);
    ExitOnFailure(hr, "Failed to read rollback flag.");

    if (BURN_PACKAGE_TYPE_MSP != executeAction.mspTarget.pPackage->type)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Package is not an MSP package: %ls", sczPackage);
    }

    // Execute MSP package.
    hr = MspEngineExecutePackage(hwndParent, &executeAction, pCache, pVariables, fRollback, MsiExecuteMessageHandler, hPipe, pRestart);
    ExitOnFailure(hr, "Failed to execute MSP package.");

LExit:
    ReleaseStr(sczPackage);
    PlanUninitializeExecuteAction(&executeAction);

    return hr;
}

static HRESULT OnExecuteMsuPackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackage = NULL;
    DWORD dwRollback = 0;
    DWORD dwStopWusaService = 0;
    BURN_EXECUTE_ACTION executeAction = { };
    *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;

    executeAction.type = BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
    ExitOnFailure(hr, "Failed to read MSU package id.");

    hr = BuffReadString(pbData, cbData, &iData, &executeAction.msuPackage.sczLogPath);
    ExitOnFailure(hr, "Failed to read package log.");

    hr = BuffReadNumber(pbData, cbData, &iData, reinterpret_cast<DWORD*>(&executeAction.msuPackage.action));
    ExitOnFailure(hr, "Failed to read action.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwRollback);
    ExitOnFailure(hr, "Failed to read rollback.");

    hr = BuffReadNumber(pbData, cbData, &iData, &dwStopWusaService);
    ExitOnFailure(hr, "Failed to read StopWusaService.");

    hr = PackageFindById(pPackages, sczPackage, &executeAction.msuPackage.pPackage);
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackage);

    if (BURN_PACKAGE_TYPE_MSU != executeAction.msuPackage.pPackage->type)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Package is not an MSU package: %ls", sczPackage);
    }

    // execute MSU package
    hr = MsuEngineExecutePackage(&executeAction, pCache, pVariables, static_cast<BOOL>(dwRollback), static_cast<BOOL>(dwStopWusaService), GenericExecuteMessageHandler, hPipe, pRestart);
    ExitOnFailure(hr, "Failed to execute MSU package.");

LExit:
    ReleaseStr(sczPackage);
    PlanUninitializeExecuteAction(&executeAction);

    return hr;
}

static HRESULT OnUninstallMsiCompatiblePackage(
    __in HANDLE hPipe,
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackageId = NULL;
    LPWSTR sczCompatiblePackageId = NULL;
    HWND hwndParent = NULL;
    BOOL fRollback = FALSE;
    BURN_EXECUTE_ACTION executeAction = { };
    BURN_PACKAGE* pPackage = NULL;
    BURN_COMPATIBLE_PACKAGE* pCompatiblePackage = NULL;
    *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;

    executeAction.type = BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE;

    // Deserialize message data.
    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&fRollback);
    ExitOnFailure(hr, "Failed to read rollback flag.");

    hr = BuffReadString(pbData, cbData, &iData, &sczPackageId);
    ExitOnFailure(hr, "Failed to read MSI package id.");

    hr = BuffReadString(pbData, cbData, &iData, &sczCompatiblePackageId);
    ExitOnFailure(hr, "Failed to read MSI compatible package id.");

    hr = BuffReadPointer(pbData, cbData, &iData, (DWORD_PTR*)&hwndParent);
    ExitOnFailure(hr, "Failed to read parent hwnd.");

    hr = BuffReadString(pbData, cbData, &iData, &executeAction.uninstallMsiCompatiblePackage.sczLogPath);
    ExitOnFailure(hr, "Failed to read package log.");

    hr = VariableDeserialize(pVariables, FALSE, pbData, cbData, &iData);
    ExitOnFailure(hr, "Failed to read variables.");

    hr = PackageFindById(pPackages, sczPackageId, &pPackage);
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackageId);

    executeAction.uninstallMsiCompatiblePackage.pParentPackage = pPackage;
    pCompatiblePackage = &pPackage->compatiblePackage;

    if (!pCompatiblePackage->fDetected || BURN_PACKAGE_TYPE_MSI != pCompatiblePackage->type || !pCompatiblePackage->compatibleEntry.sczId)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Package '%ls' has no compatible MSI package", sczPackageId);
    }

    if (!sczCompatiblePackageId || !*sczCompatiblePackageId ||
        CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pCompatiblePackage->compatibleEntry.sczId, -1, sczCompatiblePackageId, -1))
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Package '%ls' has no compatible package with id: %ls", sczPackageId, sczCompatiblePackageId);
    }

    // Uninstall MSI compatible package.
    hr = MsiEngineUninstallCompatiblePackage(hwndParent, &executeAction, pCache, pVariables, fRollback, MsiExecuteMessageHandler, hPipe, pRestart);
    ExitOnFailure(hr, "Failed to execute compatible MSI package.");

LExit:
    ReleaseStr(sczPackageId);
    ReleaseStr(sczCompatiblePackageId);
    PlanUninitializeExecuteAction(&executeAction);

    return hr;
}

static HRESULT OnExecutePackageProviderAction(
    __in BURN_PACKAGES* pPackages,
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackage = NULL;
    BOOL fRollback = FALSE;
    BURN_EXECUTE_ACTION executeAction = { };

    executeAction.type = BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER;

    // Deserialize the message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
    ExitOnFailure(hr, "Failed to read package id from message buffer.");

    // Find the package again.
    hr = PackageFindById(pPackages, sczPackage, &executeAction.packageProvider.pPackage);
    if (E_NOTFOUND == hr)
    {
        hr = PackageFindRelatedById(pRelatedBundles, sczPackage, &executeAction.packageProvider.pPackage);
    }
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackage);

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&fRollback);
    ExitOnFailure(hr, "Failed to read rollback flag.");

    // Read provider actions.
    for (DWORD i = 0; i < executeAction.packageProvider.pPackage->cDependencyProviders; ++i)
    {
        BURN_DEPENDENCY_PROVIDER* pProvider = executeAction.packageProvider.pPackage->rgDependencyProviders + i;
        BURN_DEPENDENCY_ACTION* pAction = fRollback ? &pProvider->providerRollback : &pProvider->providerExecute;
        hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)pAction);
        ExitOnFailure(hr, "Failed to read provider action.");
    }

    if (!executeAction.packageProvider.pPackage->fPerMachine)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "ExecutePackageProviderAction called for per-user package.");
    }

    // Execute the package provider action.
    hr = DependencyExecutePackageProviderAction(&executeAction, fRollback);
    ExitOnFailure(hr, "Failed to execute package provider action.");

LExit:
    ReleaseStr(sczPackage);
    PlanUninitializeExecuteAction(&executeAction);

    return hr;
}

static HRESULT OnExecutePackageDependencyAction(
    __in BURN_PACKAGES* pPackages,
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackage = NULL;
    BOOL fRollback = FALSE;
    BURN_EXECUTE_ACTION executeAction = { };

    executeAction.type = BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY;

    // Deserialize the message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
    ExitOnFailure(hr, "Failed to read package id from message buffer.");

    // Find the package again.
    hr = PackageFindById(pPackages, sczPackage, &executeAction.packageDependency.pPackage);
    if (E_NOTFOUND == hr)
    {
        hr = PackageFindRelatedById(pRelatedBundles, sczPackage, &executeAction.packageDependency.pPackage);
    }
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackage);

    hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)&fRollback);
    ExitOnFailure(hr, "Failed to read rollback flag.");

    hr = BuffReadString(pbData, cbData, &iData, &executeAction.packageDependency.sczBundleProviderKey);
    ExitOnFailure(hr, "Failed to read bundle dependency key from message buffer.");

    // Read dependent actions.
    for (DWORD i = 0; i < executeAction.packageProvider.pPackage->cDependencyProviders; ++i)
    {
        BURN_DEPENDENCY_PROVIDER* pProvider = executeAction.packageProvider.pPackage->rgDependencyProviders + i;
        BURN_DEPENDENCY_ACTION* pAction = fRollback ? &pProvider->dependentRollback : &pProvider->dependentExecute;
        hr = BuffReadNumber(pbData, cbData, &iData, (DWORD*)pAction);
        ExitOnFailure(hr, "Failed to read dependent action.");
    }

    // Execute the package dependency action.
    hr = DependencyExecutePackageDependencyAction(TRUE, &executeAction, fRollback);
    ExitOnFailure(hr, "Failed to execute package dependency action.");

LExit:
    ReleaseStr(sczPackage);
    PlanUninitializeExecuteAction(&executeAction);

    return hr;
}

static HRESULT CALLBACK BurnCacheMessageHandler(
    __in BURN_CACHE_MESSAGE* pMessage,
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    DWORD dwResult = 0;
    HANDLE hPipe = (HANDLE)pvContext;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwMessage = 0;

    switch (pMessage->type)
    {
    case BURN_CACHE_MESSAGE_BEGIN:
        // serialize message data
        hr = BuffWriteNumber(&pbData, &cbData, pMessage->begin.cacheStep);
        ExitOnFailure(hr, "Failed to write cache step to message buffer.");

        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_BEGIN;
        break;

    case BURN_CACHE_MESSAGE_COMPLETE:
        // serialize message data
        hr = BuffWriteNumber(&pbData, &cbData, pMessage->complete.hrStatus);
        ExitOnFailure(hr, "Failed to write error code to message buffer.");

        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_COMPLETE;
        break;

    case BURN_CACHE_MESSAGE_SUCCESS:
        hr = BuffWriteNumber64(&pbData, &cbData, pMessage->success.qwFileSize);
        ExitOnFailure(hr, "Failed to write file size to message buffer.");

        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_SUCCESS;
        break;

    case BURN_CACHE_MESSAGE_FAILURE:
        // serialize message data
        hr = BuffWriteNumber(&pbData, &cbData, pMessage->failure.cacheStep);
        ExitOnFailure(hr, "Failed to write cache step to message buffer.");

        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_BURN_CACHE_FAILURE;
        break;
    }

    // send message
    hr = BurnPipeSendMessage(hPipe, dwMessage, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send burn cache message to per-user process.");

    hr = dwResult;

LExit:
    ReleaseMem(pbData);

    return hr;
}

static DWORD CALLBACK ElevatedProgressRoutine(
    __in LARGE_INTEGER TotalFileSize,
    __in LARGE_INTEGER TotalBytesTransferred,
    __in LARGE_INTEGER /*StreamSize*/,
    __in LARGE_INTEGER /*StreamBytesTransferred*/,
    __in DWORD /*dwStreamNumber*/,
    __in DWORD /*dwCallbackReason*/,
    __in HANDLE /*hSourceFile*/,
    __in HANDLE /*hDestinationFile*/,
    __in_opt LPVOID lpData
    )
{
    HRESULT hr = S_OK;
    DWORD dwResult = 0;
    HANDLE hPipe = (HANDLE)lpData;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwMessage = BURN_ELEVATION_MESSAGE_TYPE_PROGRESS_ROUTINE;

    hr = BuffWriteNumber64(&pbData, &cbData, TotalFileSize.QuadPart);
    ExitOnFailure(hr, "Failed to write total file size progress to message buffer.");

    hr = BuffWriteNumber64(&pbData, &cbData, TotalBytesTransferred.QuadPart);
    ExitOnFailure(hr, "Failed to write total bytes transferred progress to message buffer.");

    // send message
    hr = BurnPipeSendMessage(hPipe, dwMessage, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send progress routine message to per-user process.");

LExit:
    ReleaseMem(pbData);

    return dwResult;
}

static int GenericExecuteMessageHandler(
    __in GENERIC_EXECUTE_MESSAGE* pMessage,
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    int nResult = IDOK;
    HANDLE hPipe = (HANDLE)pvContext;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwMessage = 0;

    hr = BuffWriteNumber(&pbData, &cbData, pMessage->dwUIHint);
    ExitOnFailure(hr, "Failed to write UI flags.");

    switch(pMessage->type)
    {
    case GENERIC_EXECUTE_MESSAGE_PROGRESS:
        // serialize message data
        hr = BuffWriteNumber(&pbData, &cbData, pMessage->progress.dwPercentage);
        ExitOnFailure(hr, "Failed to write progress percentage to message buffer.");

        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROGRESS;
        break;

    case GENERIC_EXECUTE_MESSAGE_PROCESS_CANCEL:
        hr = BuffWriteNumber(&pbData, &cbData, pMessage->processCancel.dwProcessId);
        ExitOnFailure(hr, "Failed to write progress percentage to message buffer.");

        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROCESS_CANCEL;
        break;

    case GENERIC_EXECUTE_MESSAGE_PROCESS_STARTED:
        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROCESS_STARTED;
        break;

    case GENERIC_EXECUTE_MESSAGE_PROCESS_COMPLETED:
        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROCESS_COMPLETED;
        break;

    case GENERIC_EXECUTE_MESSAGE_ERROR:
        // serialize message data
        hr = BuffWriteNumber(&pbData, &cbData, pMessage->error.dwErrorCode);
        ExitOnFailure(hr, "Failed to write error code to message buffer.");

        hr = BuffWriteString(&pbData, &cbData, pMessage->error.wzMessage);
        ExitOnFailure(hr, "Failed to write message to message buffer.");

        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ERROR;
        break;

    case GENERIC_EXECUTE_MESSAGE_NETFX_FILES_IN_USE:
        hr = BuffWriteNumber(&pbData, &cbData, pMessage->filesInUse.cFiles);
        ExitOnFailure(hr, "Failed to count of files in use to message buffer.");

        for (DWORD i = 0; i < pMessage->filesInUse.cFiles; ++i)
        {
            hr = BuffWriteString(&pbData, &cbData, pMessage->filesInUse.rgwzFiles[i]);
            ExitOnFailure(hr, "Failed to write file in use to message buffer.");
        }

        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_NETFX_FILES_IN_USE;
        break;
    }

    // send message
    hr = BurnPipeSendMessage(hPipe, dwMessage, pbData, cbData, NULL, NULL, reinterpret_cast<DWORD*>(&nResult));
    ExitOnFailure(hr, "Failed to send message to per-user process.");

LExit:
    ReleaseMem(pbData);

    return nResult;
}

static int MsiExecuteMessageHandler(
    __in WIU_MSI_EXECUTE_MESSAGE* pMessage,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    int nResult = IDOK;
    HANDLE hPipe = (HANDLE)pvContext;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwMessage = 0;
    BOOL fRestartManager = FALSE;

    // Always send any extra data via the struct first.
    hr = BuffWriteNumber(&pbData, &cbData, pMessage->cData);
    ExitOnFailure(hr, "Failed to write MSI data count to message buffer.");

    for (DWORD i = 0; i < pMessage->cData; ++i)
    {
        hr = BuffWriteString(&pbData, &cbData, pMessage->rgwzData[i]);
        ExitOnFailure(hr, "Failed to write MSI data to message buffer.");
    }

    hr = BuffWriteNumber(&pbData, &cbData, pMessage->dwUIHint);
    ExitOnFailure(hr, "Failed to write UI flags.");

    switch (pMessage->type)
    {
    case WIU_MSI_EXECUTE_MESSAGE_PROGRESS:
        // serialize message data
        hr = BuffWriteNumber(&pbData, &cbData, pMessage->progress.dwPercentage);
        ExitOnFailure(hr, "Failed to write progress percentage to message buffer.");

        // set message id
        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_PROGRESS;
        break;

    case WIU_MSI_EXECUTE_MESSAGE_ERROR:
        // serialize message data
        hr = BuffWriteNumber(&pbData, &cbData, pMessage->error.dwErrorCode);
        ExitOnFailure(hr, "Failed to write error code to message buffer.");

        hr = BuffWriteString(&pbData, &cbData, pMessage->error.wzMessage);
        ExitOnFailure(hr, "Failed to write message to message buffer.");

        // set message id
        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ERROR;
        break;

    case WIU_MSI_EXECUTE_MESSAGE_MSI_MESSAGE:
        // serialize message data
        hr = BuffWriteNumber(&pbData, &cbData, (DWORD)pMessage->msiMessage.mt);
        ExitOnFailure(hr, "Failed to write MSI message type to message buffer.");

        hr = BuffWriteString(&pbData, &cbData, pMessage->msiMessage.wzMessage);
        ExitOnFailure(hr, "Failed to write message to message buffer.");

        // set message id
        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_MESSAGE;
        break;

    case WIU_MSI_EXECUTE_MESSAGE_MSI_RM_FILES_IN_USE:
        fRestartManager = TRUE;
        __fallthrough;
    case WIU_MSI_EXECUTE_MESSAGE_MSI_FILES_IN_USE:
        hr = BuffWriteNumber(&pbData, &cbData, (DWORD)fRestartManager);
        ExitOnFailure(hr, "Failed to write fRestartManager to message buffer.");

        // set message id
        dwMessage = BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_MSI_FILES_IN_USE;
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Invalid MSI execute message type: %d", pMessage->type);
    }

    // send message
    hr = BurnPipeSendMessage(hPipe, dwMessage, pbData, cbData, NULL, NULL, (DWORD*)&nResult);
    ExitOnFailure(hr, "Failed to send msi message to per-user process.");

LExit:
    ReleaseMem(pbData);

    return nResult;
}

static HRESULT OnCleanCompatiblePackage(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackageId = NULL;
    LPWSTR sczCompatiblePackageId = NULL;
    BURN_PACKAGE* pPackage = NULL;
    BURN_COMPATIBLE_PACKAGE* pCompatiblePackage = NULL;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id.");

    hr = BuffReadString(pbData, cbData, &iData, &sczCompatiblePackageId);
    ExitOnFailure(hr, "Failed to read compatible package id.");

    hr = PackageFindById(pPackages, sczPackageId, &pPackage);
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackageId);

    pCompatiblePackage = &pPackage->compatiblePackage;

    if (!pCompatiblePackage->fDetected || !pCompatiblePackage->compatibleEntry.sczId || !pCompatiblePackage->sczCacheId || !*pCompatiblePackage->sczCacheId)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Package '%ls' has no compatible package to clean.", sczPackageId);
    }

    if (!sczCompatiblePackageId || !*sczCompatiblePackageId ||
        CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pCompatiblePackage->compatibleEntry.sczId, -1, sczCompatiblePackageId, -1))
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Package '%ls' has no compatible package with id: %ls", sczPackageId, sczCompatiblePackageId);
    }

    // Remove the package from the cache.
    hr = CacheRemovePackage(pCache, TRUE, pCompatiblePackage->compatibleEntry.sczId, pCompatiblePackage->sczCacheId);
    ExitOnFailure(hr, "Failed to remove from cache compatible package: %ls", pCompatiblePackage->compatibleEntry.sczId);

LExit:
    ReleaseStr(sczPackageId);
    ReleaseStr(sczCompatiblePackageId);
    return hr;
}

static HRESULT OnCleanPackage(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczPackage = NULL;
    BURN_PACKAGE* pPackage = NULL;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczPackage);
    ExitOnFailure(hr, "Failed to read package id.");

    hr = PackageFindById(pPackages, sczPackage, &pPackage);
    ExitOnFailure(hr, "Failed to find package: %ls", sczPackage);

    // Remove the package from the cache.
    hr = CacheRemovePackage(pCache, TRUE, pPackage->sczId, pPackage->sczCacheId);
    ExitOnFailure(hr, "Failed to remove from cache package: %ls", pPackage->sczId);

LExit:
    ReleaseStr(sczPackage);
    return hr;
}

static HRESULT OnLaunchApprovedExe(
    __in HANDLE hPipe,
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    BURN_LAUNCH_APPROVED_EXE launchApprovedExe = { };
    BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe = &launchApprovedExe;
    BURN_APPROVED_EXE* pApprovedExe = NULL;
    HKEY hKey = NULL;
    DWORD dwProcessId = 0;
    BYTE* pbSendData = NULL;
    SIZE_T cbSendData = 0;
    DWORD dwResult = 0;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &pLaunchApprovedExe->sczId);
    ExitOnFailure(hr, "Failed to read approved exe id.");

    hr = BuffReadString(pbData, cbData, &iData, &pLaunchApprovedExe->sczArguments);
    ExitOnFailure(hr, "Failed to read approved exe arguments.");

    hr = BuffReadNumber(pbData, cbData, &iData, &pLaunchApprovedExe->dwWaitForInputIdleTimeout);
    ExitOnFailure(hr, "Failed to read approved exe WaitForInputIdle timeout.");

    hr = ApprovedExesFindById(pApprovedExes, pLaunchApprovedExe->sczId, &pApprovedExe);
    ExitOnFailure(hr, "The per-user process requested unknown approved exe with id: %ls", pLaunchApprovedExe->sczId);

    LogId(REPORT_STANDARD, MSG_LAUNCH_APPROVED_EXE_SEARCH, pApprovedExe->sczKey, pApprovedExe->sczValueName ? pApprovedExe->sczValueName : L"", pApprovedExe->fWin64 ? L"yes" : L"no");

    hr = RegOpenEx(HKEY_LOCAL_MACHINE, pApprovedExe->sczKey, KEY_QUERY_VALUE, pApprovedExe->fWin64 ? REG_KEY_64BIT : REG_KEY_32BIT, &hKey);
    ExitOnFailure(hr, "Failed to open the registry key for the approved exe path.");

    hr = RegReadString(hKey, pApprovedExe->sczValueName, &pLaunchApprovedExe->sczExecutablePath);
    ExitOnFailure(hr, "Failed to read the value for the approved exe path.");

    hr = ApprovedExesVerifySecureLocation(pCache, pVariables, pLaunchApprovedExe->sczExecutablePath);
    ExitOnFailure(hr, "Failed to verify the executable path is in a secure location: %ls", pLaunchApprovedExe->sczExecutablePath);
    if (S_FALSE == hr)
    {
        LogStringLine(REPORT_STANDARD, "The executable path is not in a secure location: %ls", pLaunchApprovedExe->sczExecutablePath);
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED));
    }

    hr = ApprovedExesLaunch(pVariables, pLaunchApprovedExe, &dwProcessId);
    ExitOnFailure(hr, "Failed to launch approved exe: %ls", pLaunchApprovedExe->sczExecutablePath);

    //send process id over pipe
    hr = BuffWriteNumber(&pbSendData, &cbSendData, dwProcessId);
    ExitOnFailure(hr, "Failed to write the approved exe process id to message buffer.");

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_LAUNCH_APPROVED_EXE_PROCESSID, pbSendData, cbSendData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_LAUNCH_APPROVED_EXE_PROCESSID message to per-user process.");

LExit:
    ReleaseMem(pbSendData);
    ApprovedExesUninitializeLaunch(pLaunchApprovedExe);
    return hr;
}

static HRESULT OnMsiBeginTransaction(
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczId = NULL;
    LPWSTR sczLogPath = NULL;
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = NULL;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczId);
    ExitOnFailure(hr, "Failed to read rollback boundary id.");

    hr = BuffReadString(pbData, cbData, &iData, &sczLogPath);
    ExitOnFailure(hr, "Failed to read transaction log path.");

    hr = PackageFindRollbackBoundaryById(pPackages, sczId, &pRollbackBoundary);
    ExitOnFailure(hr, "Failed to find rollback boundary: %ls", sczId);

    if (sczLogPath && *sczLogPath)
    {
        pRollbackBoundary->sczLogPath = sczLogPath;
    }

    hr = MsiEngineBeginTransaction(pRollbackBoundary);

LExit:
    ReleaseStr(sczId);
    ReleaseStr(sczLogPath);

    if (pRollbackBoundary)
    {
        pRollbackBoundary->sczLogPath = NULL;
    }

    return hr;
}

static HRESULT OnMsiCommitTransaction(
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART *pRestart
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczId = NULL;
    LPWSTR sczLogPath = NULL;
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = NULL;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczId);
    ExitOnFailure(hr, "Failed to read rollback boundary id.");

    hr = BuffReadString(pbData, cbData, &iData, &sczLogPath);
    ExitOnFailure(hr, "Failed to read transaction log path.");

    hr = PackageFindRollbackBoundaryById(pPackages, sczId, &pRollbackBoundary);
    ExitOnFailure(hr, "Failed to find rollback boundary: %ls", sczId);

    if (sczLogPath && *sczLogPath)
    {
        pRollbackBoundary->sczLogPath = sczLogPath;
    }

    hr = MsiEngineCommitTransaction(pRollbackBoundary, pRestart);

LExit:
    ReleaseStr(sczId);
    ReleaseStr(sczLogPath);

    if (pRollbackBoundary)
    {
        pRollbackBoundary->sczLogPath = NULL;
    }

    return hr;
}

static HRESULT OnMsiRollbackTransaction(
    __in BURN_PACKAGES* pPackages,
    __in BYTE* pbData,
    __in SIZE_T cbData,
    __out BOOTSTRAPPER_APPLY_RESTART *pRestart
    )
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    LPWSTR sczId = NULL;
    LPWSTR sczLogPath = NULL;
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = NULL;

    // Deserialize message data.
    hr = BuffReadString(pbData, cbData, &iData, &sczId);
    ExitOnFailure(hr, "Failed to read rollback boundary id.");

    hr = BuffReadString(pbData, cbData, &iData, &sczLogPath);
    ExitOnFailure(hr, "Failed to read transaction log path.");

    hr = PackageFindRollbackBoundaryById(pPackages, sczId, &pRollbackBoundary);
    ExitOnFailure(hr, "Failed to find rollback boundary: %ls", sczId);

    if (sczLogPath && *sczLogPath)
    {
        pRollbackBoundary->sczLogPath = sczLogPath;
    }

    hr = MsiEngineRollbackTransaction(pRollbackBoundary, pRestart);

LExit:
    ReleaseStr(sczId);
    ReleaseStr(sczLogPath);

    if (pRollbackBoundary)
    {
        pRollbackBoundary->sczLogPath = NULL;
    }

    return hr;
}

static HRESULT ElevatedOnPauseAUBegin(
    __in HANDLE hPipe
    )
{
    HRESULT hr = S_OK;
    DWORD dwResult = 0;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_PAUSE_AU_BEGIN, NULL, 0, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_PAUSE_AU_BEGIN message to per-user process.");

LExit:
    return hr;
}

static HRESULT ElevatedOnPauseAUComplete(
    __in HANDLE hPipe,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BYTE* pbSendData = NULL;
    SIZE_T cbSendData = 0;
    DWORD dwResult = 0;

    hr = BuffWriteNumber(&pbSendData, &cbSendData, hrStatus);
    ExitOnFailure(hr, "Failed to write the pause au status to message buffer.");

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_PAUSE_AU_COMPLETE, pbSendData, cbSendData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_PAUSE_AU_COMPLETE message to per-user process.");

LExit:
    ReleaseMem(pbSendData);

    return hr;
}

static HRESULT ElevatedOnSystemRestorePointBegin(
    __in HANDLE hPipe
    )
{
    HRESULT hr = S_OK;
    DWORD dwResult = 0;

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_SYSTEM_RESTORE_POINT_BEGIN, NULL, 0, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_SYSTEM_RESTORE_POINT_BEGIN message to per-user process.");

LExit:
    return hr;
}

static HRESULT ElevatedOnSystemRestorePointComplete(
    __in HANDLE hPipe,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BYTE* pbSendData = NULL;
    SIZE_T cbSendData = 0;
    DWORD dwResult = 0;

    hr = BuffWriteNumber(&pbSendData, &cbSendData, hrStatus);
    ExitOnFailure(hr, "Failed to write the system restore point status to message buffer.");

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_SYSTEM_RESTORE_POINT_COMPLETE, pbSendData, cbSendData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_APPLY_INITIALIZE_SYSTEM_RESTORE_POINT_COMPLETE message to per-user process.");

LExit:
    ReleaseMem(pbSendData);

    return hr;
}

static HRESULT ElevatedOnExecuteActionComplete(
    __in HANDLE hPipe,
    __in BOOTSTRAPPER_APPLY_RESTART restart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbSendData = NULL;
    SIZE_T cbSendData = 0;
    DWORD dwResult = 0;

    hr = BuffWriteNumber(&pbSendData, &cbSendData, restart);
    ExitOnFailure(hr, "Failed to write the restart type to message buffer.");

    hr = BurnPipeSendMessage(hPipe, BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ACTION_COMPLETE, pbSendData, cbSendData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send BURN_ELEVATION_MESSAGE_TYPE_EXECUTE_ACTION_COMPLETE message to per-user process.");

LExit:
    ReleaseMem(pbSendData);

    return hr;
}
