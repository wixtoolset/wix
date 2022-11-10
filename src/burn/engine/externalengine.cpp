// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static HRESULT CopyStringToExternal(
    __in_z LPWSTR wzValue,
    __in_z_opt LPWSTR wzBuffer,
    __inout SIZE_T* pcchBuffer
    );
static HRESULT ProcessUnknownEmbeddedMessages(
    __in BURN_PIPE_MESSAGE* /*pMsg*/,
    __in_opt LPVOID /*pvContext*/,
    __out DWORD* pdwResult
    );
static HRESULT EnqueueAction(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __inout BOOTSTRAPPER_ENGINE_ACTION** ppAction
    );

// function definitions

void ExternalEngineGetPackageCount(
    __in BURN_ENGINE_STATE* pEngineState,
    __out DWORD* pcPackages
    )
{
    *pcPackages = pEngineState->packages.cPackages;
}

HRESULT ExternalEngineGetVariableNumeric(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;

    if (wzVariable && *wzVariable)
    {
        hr = VariableGetNumeric(&pEngineState->variables, wzVariable, pllValue);
    }
    else
    {
        *pllValue = 0;
        hr = E_INVALIDARG;
    }

    return hr;
}

HRESULT ExternalEngineGetVariableString(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout SIZE_T* pcchValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    if (wzVariable && *wzVariable)
    {
        hr = VariableGetString(&pEngineState->variables, wzVariable, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = CopyStringToExternal(sczValue, wzValue, pcchValue);
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    StrSecureZeroFreeString(sczValue);

    return hr;
}

HRESULT ExternalEngineGetVariableVersion(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout SIZE_T* pcchValue
    )
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pVersion = NULL;

    if (wzVariable && *wzVariable)
    {
        hr = VariableGetVersion(&pEngineState->variables, wzVariable, &pVersion);
        if (SUCCEEDED(hr))
        {
            hr = CopyStringToExternal(pVersion->sczVersion, wzValue, pcchValue);
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    ReleaseVerutilVersion(pVersion);

    return hr;
}

HRESULT ExternalEngineFormatString(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzIn,
    __out_ecount_opt(*pcchOut) LPWSTR wzOut,
    __inout SIZE_T* pcchOut
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    if (wzIn && *wzIn)
    {
        hr = VariableFormatString(&pEngineState->variables, wzIn, &sczValue, NULL);
        if (SUCCEEDED(hr))
        {
            hr = CopyStringToExternal(sczValue, wzOut, pcchOut);
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    StrSecureZeroFreeString(sczValue);

    return hr;
}

HRESULT ExternalEngineEscapeString(
    __in_z LPCWSTR wzIn,
    __out_ecount_opt(*pcchOut) LPWSTR wzOut,
    __inout SIZE_T* pcchOut
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    if (wzIn && *wzIn)
    {
        hr = VariableEscapeString(wzIn, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = CopyStringToExternal(sczValue, wzOut, pcchOut);
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    StrSecureZeroFreeString(sczValue);

    return hr;
}

HRESULT ExternalEngineEvaluateCondition(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;

    if (wzCondition && *wzCondition)
    {
        hr = ConditionEvaluate(&pEngineState->variables, wzCondition, pf);
    }
    else
    {
        *pf = FALSE;
        hr = E_INVALIDARG;
    }

    return hr;
}

HRESULT ExternalEngineLog(
    __in REPORT_LEVEL rl,
    __in_z LPCWSTR wzMessage
    )
{
    HRESULT hr = S_OK;

    hr = LogStringLine(rl, "%ls", wzMessage);

    return hr;
}

HRESULT ExternalEngineSendEmbeddedError(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const DWORD dwErrorCode,
    __in_z LPCWSTR wzMessage,
    __in const DWORD dwUIHint,
    __out int* pnResult
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = *pnResult = 0;

    if (BURN_MODE_EMBEDDED != pEngineState->internalCommand.mode)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_STATE);
        ExitOnRootFailure(hr, "BA requested to send embedded message when not in embedded mode.");
    }

    hr = BuffWriteNumber(&pbData, &cbData, dwErrorCode);
    ExitOnFailure(hr, "Failed to write error code to message buffer.");

    hr = BuffWriteString(&pbData, &cbData, wzMessage ? wzMessage : L"");
    ExitOnFailure(hr, "Failed to write message string to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, dwUIHint);
    ExitOnFailure(hr, "Failed to write UI hint to message buffer.");

    hr = PipeSendMessage(pEngineState->embeddedConnection.hPipe, BURN_EMBEDDED_MESSAGE_TYPE_ERROR, pbData, cbData, ProcessUnknownEmbeddedMessages, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send embedded message over pipe.");

    *pnResult = static_cast<int>(dwResult);

LExit:
    ReleaseBuffer(pbData);

    return hr;
}

HRESULT ExternalEngineSendEmbeddedProgress(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const DWORD dwProgressPercentage,
    __in const DWORD dwOverallProgressPercentage,
    __out int* pnResult
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = *pnResult = 0;

    if (BURN_MODE_EMBEDDED != pEngineState->internalCommand.mode)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_STATE);
        ExitOnRootFailure(hr, "BA requested to send embedded progress message when not in embedded mode.");
    }

    hr = BuffWriteNumber(&pbData, &cbData, dwProgressPercentage);
    ExitOnFailure(hr, "Failed to write progress percentage to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, dwOverallProgressPercentage);
    ExitOnFailure(hr, "Failed to write overall progress percentage to message buffer.");

    hr = PipeSendMessage(pEngineState->embeddedConnection.hPipe, BURN_EMBEDDED_MESSAGE_TYPE_PROGRESS, pbData, cbData, ProcessUnknownEmbeddedMessages, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send embedded progress message over pipe.");

    *pnResult = static_cast<int>(dwResult);

LExit:
    ReleaseBuffer(pbData);

    return hr;
}

HRESULT ExternalEngineSetUpdate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z_opt LPCWSTR wzLocalSource,
    __in_z_opt LPCWSTR wzDownloadSource,
    __in const DWORD64 qwSize,
    __in const BOOTSTRAPPER_UPDATE_HASH_TYPE hashType,
    __in_opt LPCWSTR wzHash
    )
{
    HRESULT hr = S_OK;
    BOOL fLeaveCriticalSection = FALSE;
    LPWSTR sczFilePath = NULL;
    LPWSTR sczCommandline = NULL;
    LPWSTR sczPreviousId = NULL;
    LPCWSTR wzNewId = NULL;
    UUID guid = { };
    WCHAR wzGuid[39];
    RPC_STATUS rs = RPC_S_OK;
    BOOL fRemove = (!wzLocalSource || !*wzLocalSource) && (!wzDownloadSource || !*wzDownloadSource);

    UserExperienceOnSetUpdateBegin(&pEngineState->userExperience);

    ::EnterCriticalSection(&pEngineState->userExperience.csEngineActive);
    fLeaveCriticalSection = TRUE;
    hr = UserExperienceEnsureEngineInactive(&pEngineState->userExperience);
    ExitOnFailure(hr, "Engine is active, cannot change engine state.");

    if (!fRemove)
    {
        if (BOOTSTRAPPER_UPDATE_HASH_TYPE_NONE == hashType && wzHash && *wzHash)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
        else if (BOOTSTRAPPER_UPDATE_HASH_TYPE_SHA512 == hashType && (!wzHash || !*wzHash || SHA512_HASH_LEN * 2 != lstrlenW(wzHash)))
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
    }

    sczPreviousId = pEngineState->update.package.sczId;
    pEngineState->update.package.sczId = NULL;
    UpdateUninitialize(&pEngineState->update);

    if (fRemove)
    {
        ExitFunction();
    }

    hr = CoreCreateUpdateBundleCommandLine(&sczCommandline, &pEngineState->internalCommand, &pEngineState->command);
    ExitOnFailure(hr, "Failed to create command-line for update bundle.");

    // Bundles would fail to use the downloaded update bundle, as the running bundle would be one of the search paths.
    // Here I am generating a random guid, but in the future it would be nice if the feed would provide the ID of the update.
    rs = ::UuidCreate(&guid);
    hr = HRESULT_FROM_RPC(rs);
    ExitOnFailure(hr, "Failed to create bundle update guid.");

    if (!::StringFromGUID2(guid, wzGuid, countof(wzGuid)))
    {
        hr = E_OUTOFMEMORY;
        ExitOnRootFailure(hr, "Failed to convert bundle update guid into string.");
    }

    hr = StrAllocFormatted(&sczFilePath, L"%ls\\%ls", wzGuid, pEngineState->registration.sczExecutableName);
    ExitOnFailure(hr, "Failed to build bundle update file path.");

    if (!wzLocalSource || !*wzLocalSource)
    {
        wzLocalSource = sczFilePath;
    }

    hr = PseudoBundleInitializeUpdateBundle(&pEngineState->update.package, wzGuid, pEngineState->registration.sczId, sczFilePath, wzLocalSource, wzDownloadSource, qwSize, sczCommandline, wzHash);
    ExitOnFailure(hr, "Failed to set update bundle.");

    pEngineState->update.fUpdateAvailable = TRUE;
    wzNewId = wzGuid;

LExit:
    if (fLeaveCriticalSection)
    {
        ::LeaveCriticalSection(&pEngineState->userExperience.csEngineActive);
    }

    UserExperienceOnSetUpdateComplete(&pEngineState->userExperience, hr, sczPreviousId, wzNewId);

    ReleaseStr(sczPreviousId);
    ReleaseStr(sczCommandline);
    ReleaseStr(sczFilePath);

    return hr;
}

HRESULT ExternalEngineSetLocalSource(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER* pContainer = NULL;
    BURN_PAYLOAD* pPayload = NULL;

    ::EnterCriticalSection(&pEngineState->userExperience.csEngineActive);
    hr = UserExperienceEnsureEngineInactive(&pEngineState->userExperience);
    ExitOnFailure(hr, "Engine is active, cannot change engine state.");

    if (!wzPath || !*wzPath)
    {
        hr = E_INVALIDARG;
    }
    else if (wzPayloadId && *wzPayloadId)
    {
        hr = PayloadFindById(&pEngineState->payloads, wzPayloadId, &pPayload);
        ExitOnFailure(hr, "BA requested unknown payload with id: %ls", wzPayloadId);

        hr = StrAllocString(&pPayload->sczSourcePath, wzPath, 0);
        ExitOnFailure(hr, "Failed to set source path for payload.");
    }
    else if (wzPackageOrContainerId && *wzPackageOrContainerId)
    {
        hr = ContainerFindById(&pEngineState->containers, wzPackageOrContainerId, &pContainer);
        ExitOnFailure(hr, "BA requested unknown container with id: %ls", wzPackageOrContainerId);

        hr = StrAllocString(&pContainer->sczSourcePath, wzPath, 0);
        ExitOnFailure(hr, "Failed to set source path for container.");
    }
    else
    {
        hr = E_INVALIDARG;
    }

LExit:
    ::LeaveCriticalSection(&pEngineState->userExperience.csEngineActive);

    return hr;
}

HRESULT ExternalEngineSetDownloadSource(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z_opt LPCWSTR wzUrl,
    __in_z_opt LPCWSTR wzUser,
    __in_z_opt LPCWSTR wzPassword
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER* pContainer = NULL;
    BURN_PAYLOAD* pPayload = NULL;
    DOWNLOAD_SOURCE* pDownloadSource = NULL;

    ::EnterCriticalSection(&pEngineState->userExperience.csEngineActive);
    hr = UserExperienceEnsureEngineInactive(&pEngineState->userExperience);
    ExitOnFailure(hr, "Engine is active, cannot change engine state.");

    if (wzPayloadId && *wzPayloadId)
    {
        hr = PayloadFindById(&pEngineState->payloads, wzPayloadId, &pPayload);
        ExitOnFailure(hr, "BA requested unknown payload with id: %ls", wzPayloadId);

        pDownloadSource = &pPayload->downloadSource;
    }
    else if (wzPackageOrContainerId && *wzPackageOrContainerId)
    {
        hr = ContainerFindById(&pEngineState->containers, wzPackageOrContainerId, &pContainer);
        ExitOnFailure(hr, "BA requested unknown container with id: %ls", wzPackageOrContainerId);

        pDownloadSource = &pContainer->downloadSource;
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "BA did not provide container or payload id.");
    }

    if (wzUrl && *wzUrl)
    {
        hr = StrAllocString(&pDownloadSource->sczUrl, wzUrl, 0);
        ExitOnFailure(hr, "Failed to set download URL.");

        if (wzUser && *wzUser)
        {
            hr = StrAllocString(&pDownloadSource->sczUser, wzUser, 0);
            ExitOnFailure(hr, "Failed to set download user.");

            if (wzPassword && *wzPassword)
            {
                hr = StrAllocString(&pDownloadSource->sczPassword, wzPassword, 0);
                ExitOnFailure(hr, "Failed to set download password.");
            }
            else // no password.
            {
                ReleaseNullStr(pDownloadSource->sczPassword);
            }
        }
        else // no user means no password either.
        {
            ReleaseNullStr(pDownloadSource->sczUser);
            ReleaseNullStr(pDownloadSource->sczPassword);
        }
    }
    else // no URL provided means clear out the whole download source.
    {
        ReleaseNullStr(pDownloadSource->sczUrl);
        ReleaseNullStr(pDownloadSource->sczUser);
        ReleaseNullStr(pDownloadSource->sczPassword);
    }

LExit:
    ::LeaveCriticalSection(&pEngineState->userExperience.csEngineActive);

    return hr;
}

HRESULT ExternalEngineSetVariableNumeric(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __in const LONGLONG llValue
    )
{
    HRESULT hr = S_OK;

    if (wzVariable && *wzVariable)
    {
        hr = VariableSetNumeric(&pEngineState->variables, wzVariable, llValue, FALSE);
        ExitOnFailure(hr, "Failed to set numeric variable.");
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "SetVariableNumeric did not provide variable name.");
    }

LExit:
    return hr;
}

HRESULT ExternalEngineSetVariableString(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in const BOOL fFormatted
    )
{
    HRESULT hr = S_OK;

    if (wzVariable && *wzVariable)
    {
        hr = VariableSetString(&pEngineState->variables, wzVariable, wzValue, FALSE, fFormatted);
        ExitOnFailure(hr, "Failed to set string variable.");
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "SetVariableString did not provide variable name.");
    }

LExit:
    return hr;
}

HRESULT ExternalEngineSetVariableVersion(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    )
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pVersion = NULL;

    if (wzVariable && *wzVariable)
    {
        if (wzValue)
        {
            hr = VerParseVersion(wzValue, 0, FALSE, &pVersion);
            ExitOnFailure(hr, "Failed to parse new version value.");
        }

        hr = VariableSetVersion(&pEngineState->variables, wzVariable, pVersion, FALSE);
        ExitOnFailure(hr, "Failed to set version variable.");
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "SetVariableVersion did not provide variable name.");
    }

LExit:
    ReleaseVerutilVersion(pVersion);

    return hr;
}

void ExternalEngineCloseSplashScreen(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    // If the splash screen is still around, close it.
    if (::IsWindow(pEngineState->command.hwndSplashScreen))
    {
        ::PostMessageW(pEngineState->command.hwndSplashScreen, WM_CLOSE, 0, 0);
    }
}

HRESULT ExternalEngineCompareVersions(
    __in_z LPCWSTR wzVersion1,
    __in_z LPCWSTR wzVersion2,
    __out int* pnResult
    )
{
    HRESULT hr = S_OK;

    hr = VerCompareStringVersions(wzVersion1, wzVersion2, FALSE, pnResult);

    return hr;
}

HRESULT ExternalEngineDetect(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in_opt const HWND hwndParent
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ENGINE_ACTION* pAction = NULL;

    pAction = (BOOTSTRAPPER_ENGINE_ACTION*)MemAlloc(sizeof(BOOTSTRAPPER_ENGINE_ACTION), TRUE);
    ExitOnNull(pAction, hr, E_OUTOFMEMORY, "Failed to alloc BOOTSTRAPPER_ENGINE_ACTION");

    pAction->dwMessage = WM_BURN_DETECT;
    pAction->detect.hwndParent = hwndParent;

    hr = EnqueueAction(pEngineContext, &pAction);
    ExitOnFailure(hr, "Failed to enqueue detect action.");

LExit:
    ReleaseMem(pAction);

    return hr;
}

HRESULT ExternalEnginePlan(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in const BOOTSTRAPPER_ACTION action
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ENGINE_ACTION* pAction = NULL;

    if (BOOTSTRAPPER_ACTION_LAYOUT > action || BOOTSTRAPPER_ACTION_UPDATE_REPLACE_EMBEDDED < action)
    {
        ExitOnRootFailure(hr = E_INVALIDARG, "BA passed invalid action to Plan: %u.", action);
    }

    pAction = (BOOTSTRAPPER_ENGINE_ACTION*)MemAlloc(sizeof(BOOTSTRAPPER_ENGINE_ACTION), TRUE);
    ExitOnNull(pAction, hr, E_OUTOFMEMORY, "Failed to alloc BOOTSTRAPPER_ENGINE_ACTION");

    pAction->dwMessage = WM_BURN_PLAN;
    pAction->plan.action = action;

    hr = EnqueueAction(pEngineContext, &pAction);
    ExitOnFailure(hr, "Failed to enqueue plan action.");

LExit:
    ReleaseMem(pAction);

    return hr;
}

HRESULT ExternalEngineElevate(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in_opt const HWND hwndParent
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ENGINE_ACTION* pAction = NULL;

    if (INVALID_HANDLE_VALUE != pEngineContext->pEngineState->companionConnection.hPipe)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_ALREADY_INITIALIZED));
    }

    pAction = (BOOTSTRAPPER_ENGINE_ACTION*)MemAlloc(sizeof(BOOTSTRAPPER_ENGINE_ACTION), TRUE);
    ExitOnNull(pAction, hr, E_OUTOFMEMORY, "Failed to alloc BOOTSTRAPPER_ENGINE_ACTION");

    pAction->dwMessage = WM_BURN_ELEVATE;
    pAction->elevate.hwndParent = hwndParent;

    hr = EnqueueAction(pEngineContext, &pAction);
    ExitOnFailure(hr, "Failed to enqueue elevate action.");

LExit:
    ReleaseMem(pAction);

    return hr;
}

HRESULT ExternalEngineApply(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in_opt const HWND hwndParent
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ENGINE_ACTION* pAction = NULL;

    ExitOnNull(hwndParent, hr, E_INVALIDARG, "BA passed NULL hwndParent to Apply.");
    if (!::IsWindow(hwndParent))
    {
        ExitOnRootFailure(hr = E_INVALIDARG, "BA passed invalid hwndParent to Apply.");
    }

    pAction = (BOOTSTRAPPER_ENGINE_ACTION*)MemAlloc(sizeof(BOOTSTRAPPER_ENGINE_ACTION), TRUE);
    ExitOnNull(pAction, hr, E_OUTOFMEMORY, "Failed to alloc BOOTSTRAPPER_ENGINE_ACTION");

    pAction->dwMessage = WM_BURN_APPLY;
    pAction->apply.hwndParent = hwndParent;

    hr = EnqueueAction(pEngineContext, &pAction);
    ExitOnFailure(hr, "Failed to enqueue apply action.");

LExit:
    ReleaseMem(pAction);

    return hr;
}

HRESULT ExternalEngineQuit(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in const DWORD dwExitCode
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ENGINE_ACTION* pAction = NULL;

    pAction = (BOOTSTRAPPER_ENGINE_ACTION*)MemAlloc(sizeof(BOOTSTRAPPER_ENGINE_ACTION), TRUE);
    ExitOnNull(pAction, hr, E_OUTOFMEMORY, "Failed to alloc BOOTSTRAPPER_ENGINE_ACTION");

    pAction->dwMessage = WM_BURN_QUIT;
    pAction->quit.dwExitCode = dwExitCode;

    hr = EnqueueAction(pEngineContext, &pAction);
    ExitOnFailure(hr, "Failed to enqueue shutdown action.");

LExit:
    ReleaseMem(pAction);

    return hr;
}

HRESULT ExternalEngineLaunchApprovedExe(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in_opt const HWND hwndParent,
    __in_z LPCWSTR wzApprovedExeForElevationId,
    __in_z_opt LPCWSTR wzArguments,
    __in const DWORD dwWaitForInputIdleTimeout
    )
{
    HRESULT hr = S_OK;
    BURN_APPROVED_EXE* pApprovedExe = NULL;
    BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe = NULL;
    BOOTSTRAPPER_ENGINE_ACTION* pAction = NULL;

    if (!wzApprovedExeForElevationId || !*wzApprovedExeForElevationId)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    hr = ApprovedExesFindById(&pEngineContext->pEngineState->approvedExes, wzApprovedExeForElevationId, &pApprovedExe);
    ExitOnFailure(hr, "BA requested unknown approved exe with id: %ls", wzApprovedExeForElevationId);

    pAction = (BOOTSTRAPPER_ENGINE_ACTION*)MemAlloc(sizeof(BOOTSTRAPPER_ENGINE_ACTION), TRUE);
    ExitOnNull(pAction, hr, E_OUTOFMEMORY, "Failed to alloc BOOTSTRAPPER_ENGINE_ACTION");

    pAction->dwMessage = WM_BURN_LAUNCH_APPROVED_EXE;
    pLaunchApprovedExe = &pAction->launchApprovedExe;

    hr = StrAllocString(&pLaunchApprovedExe->sczId, wzApprovedExeForElevationId, NULL);
    ExitOnFailure(hr, "Failed to copy the id.");

    if (wzArguments)
    {
        hr = StrAllocString(&pLaunchApprovedExe->sczArguments, wzArguments, NULL);
        ExitOnFailure(hr, "Failed to copy the arguments.");
    }

    pLaunchApprovedExe->dwWaitForInputIdleTimeout = dwWaitForInputIdleTimeout;

    pLaunchApprovedExe->hwndParent = hwndParent;

    hr = EnqueueAction(pEngineContext, &pAction);
    ExitOnFailure(hr, "Failed to enqueue launch approved exe action.");

LExit:
    if (pAction)
    {
        CoreBootstrapperEngineActionUninitialize(pAction);
        MemFree(pAction);
    }

    return hr;
}

HRESULT ExternalEngineSetUpdateSource(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzUrl
    )
{
    HRESULT hr = S_OK;
    BOOL fLeaveCriticalSection = FALSE;

    ::EnterCriticalSection(&pEngineState->userExperience.csEngineActive);
    fLeaveCriticalSection = TRUE;
    hr = UserExperienceEnsureEngineInactive(&pEngineState->userExperience);
    ExitOnFailure(hr, "Engine is active, cannot change engine state.");

    if (wzUrl && *wzUrl)
    {
        hr = StrAllocString(&pEngineState->update.sczUpdateSource, wzUrl, 0);
        ExitOnFailure(hr, "Failed to set feed download URL.");
    }
    else // no URL provided means clear out the whole download source.
    {
        ReleaseNullStr(pEngineState->update.sczUpdateSource);
    }

LExit:
    if (fLeaveCriticalSection)
    {
        ::LeaveCriticalSection(&pEngineState->userExperience.csEngineActive);
    }

    return hr;
}

HRESULT ExternalEngineGetRelatedBundleVariable(
    __in BURN_ENGINE_STATE* /*pEngineState*/,
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzVariable,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout SIZE_T* pcchValue
    )
{
    HRESULT hr = S_OK;
    if (wzVariable && *wzVariable && pcchValue)
    {
        hr = BundleGetBundleVariableFixed(wzBundleId, wzVariable, wzValue, pcchValue);
    }
    else
    {
        hr = E_INVALIDARG;
    }
    return hr;
}

// TODO: callers need to provide the original size (at the time of first public release) of the struct instead of the current size.
HRESULT WINAPI ExternalEngineValidateMessageParameter(
    __in_opt const LPVOID pv,
    __in SIZE_T cbSizeOffset,
    __in DWORD dwMinimumSize
    )
{
    HRESULT hr = S_OK;

    if (!pv)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    DWORD cbSize = *(DWORD*)((BYTE*)pv + cbSizeOffset);
    if (dwMinimumSize < cbSize)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    return hr;
}

static HRESULT CopyStringToExternal(
    __in_z LPWSTR wzValue,
    __in_z_opt LPWSTR wzBuffer,
    __inout SIZE_T* pcchBuffer
    )
{
    HRESULT hr = S_OK;
    BOOL fTooSmall = !wzBuffer;

    if (!fTooSmall)
    {
        hr = ::StringCchCopyExW(wzBuffer, *pcchBuffer, wzValue, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
        if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
        {
            fTooSmall = TRUE;
        }
    }

    if (fTooSmall)
    {
        hr = ::StringCchLengthW(wzValue, STRSAFE_MAX_LENGTH, reinterpret_cast<size_t*>(pcchBuffer));
        if (SUCCEEDED(hr))
        {
            hr = E_MOREDATA;
            *pcchBuffer += 1; // null terminator.
        }
    }

    return hr;
}

static HRESULT ProcessUnknownEmbeddedMessages(
    __in BURN_PIPE_MESSAGE* /*pMsg*/,
    __in_opt LPVOID /*pvContext*/,
    __out DWORD* pdwResult
    )
{
    *pdwResult = (DWORD)E_NOTIMPL;

    return S_OK;
}

static HRESULT EnqueueAction(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __inout BOOTSTRAPPER_ENGINE_ACTION** ppAction
    )
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&pEngineContext->csQueue);

    if (pEngineContext->pEngineState->fQuit)
    {
        LogId(REPORT_WARNING, MSG_IGNORE_OPERATION_AFTER_QUIT, LoggingBurnMessageToString((*ppAction)->dwMessage));
        hr = E_INVALIDSTATE;
    }
    else
    {
        hr = QueEnqueue(pEngineContext->hQueue, *ppAction);
    }

    ::LeaveCriticalSection(&pEngineContext->csQueue);

    ExitOnFailure(hr, "Failed to enqueue action.");

    *ppAction = NULL;

    if (!::ReleaseSemaphore(pEngineContext->hQueueSemaphore, 1, NULL))
    {
        ExitWithLastError(hr, "Failed to signal queue semaphore.");
    }

LExit:
    return hr;
}
