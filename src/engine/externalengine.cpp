// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static HRESULT CopyStringToExternal(
    __in_z LPWSTR wzValue,
    __in_z_opt LPWSTR wzBuffer,
    __inout DWORD* pcchBuffer
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
    __inout DWORD* pcchValue
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
    __inout DWORD* pcchValue
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
    __inout DWORD* pcchOut
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
    __inout DWORD* pcchOut
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

    if (BURN_MODE_EMBEDDED != pEngineState->mode)
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

    hr = PipeSendMessage(pEngineState->embeddedConnection.hPipe, BURN_EMBEDDED_MESSAGE_TYPE_ERROR, pbData, cbData, NULL, NULL, &dwResult);
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

    if (BURN_MODE_EMBEDDED != pEngineState->mode)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_STATE);
        ExitOnRootFailure(hr, "BA requested to send embedded progress message when not in embedded mode.");
    }

    hr = BuffWriteNumber(&pbData, &cbData, dwProgressPercentage);
    ExitOnFailure(hr, "Failed to write progress percentage to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, dwOverallProgressPercentage);
    ExitOnFailure(hr, "Failed to write overall progress percentage to message buffer.");

    hr = PipeSendMessage(pEngineState->embeddedConnection.hPipe, BURN_EMBEDDED_MESSAGE_TYPE_PROGRESS, pbData, cbData, NULL, NULL, &dwResult);
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
    __in_opt const BYTE* rgbHash,
    __in const DWORD cbHash
    )
{
    HRESULT hr = S_OK;
    LPCWSTR sczId = NULL;
    LPWSTR sczLocalSource = NULL;
    LPWSTR sczCommandline = NULL;
    UUID guid = { };
    WCHAR wzGuid[39];
    RPC_STATUS rs = RPC_S_OK;

    ::EnterCriticalSection(&pEngineState->csActive);

    if ((!wzLocalSource || !*wzLocalSource) && (!wzDownloadSource || !*wzDownloadSource))
    {
        UpdateUninitialize(&pEngineState->update);
    }
    else if (BOOTSTRAPPER_UPDATE_HASH_TYPE_NONE == hashType && (0 != cbHash || rgbHash))
    {
        hr = E_INVALIDARG;
    }
    else if (BOOTSTRAPPER_UPDATE_HASH_TYPE_SHA1 == hashType && (SHA1_HASH_LEN != cbHash || !rgbHash))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        UpdateUninitialize(&pEngineState->update);

        if (!wzLocalSource || !*wzLocalSource)
        {
            hr = StrAllocFormatted(&sczLocalSource, L"update\\%ls", pEngineState->registration.sczExecutableName);
            ExitOnFailure(hr, "Failed to default local update source");
        }

        hr = CoreRecreateCommandLine(&sczCommandline, BOOTSTRAPPER_ACTION_INSTALL, pEngineState->command.display, pEngineState->command.restart, BOOTSTRAPPER_RELATION_NONE, FALSE, pEngineState->registration.sczActiveParent, pEngineState->registration.sczAncestors, NULL, pEngineState->command.wzCommandLine);
        ExitOnFailure(hr, "Failed to recreate command-line for update bundle.");

        // Per-user bundles would fail to use the downloaded update bundle, as the existing install would already be cached 
        // at the registration id's location.  Here I am generating a random guid, but in the future it would be nice if the
        // feed would provide the ID of the update.
        if (!pEngineState->registration.fPerMachine)
        {
            rs = ::UuidCreate(&guid);
            hr = HRESULT_FROM_RPC(rs);
            ExitOnFailure(hr, "Failed to create bundle update guid.");

            if (!::StringFromGUID2(guid, wzGuid, countof(wzGuid)))
            {
                hr = E_OUTOFMEMORY;
                ExitOnRootFailure(hr, "Failed to convert bundle update guid into string.");
            }

            sczId = wzGuid;
        }
        else
        {
            sczId = pEngineState->registration.sczId;
        }

        hr = PseudoBundleInitialize(FILEMAKEVERSION(rmj, rmm, rup, rpr), &pEngineState->update.package, FALSE, sczId, BOOTSTRAPPER_RELATION_UPDATE, BOOTSTRAPPER_PACKAGE_STATE_ABSENT, pEngineState->registration.sczExecutableName, sczLocalSource ? sczLocalSource : wzLocalSource, wzDownloadSource, qwSize, TRUE, sczCommandline, NULL, NULL, NULL, rgbHash, cbHash);
        ExitOnFailure(hr, "Failed to set update bundle.");

        pEngineState->update.fUpdateAvailable = TRUE;
    }

LExit:
    ::LeaveCriticalSection(&pEngineState->csActive);

    ReleaseStr(sczCommandline);
    ReleaseStr(sczLocalSource);

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

    ::EnterCriticalSection(&pEngineState->csActive);
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

        if (BURN_PAYLOAD_PACKAGING_EMBEDDED == pPayload->packaging)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_OPERATION);
            ExitOnFailure(hr, "BA denied while trying to set source on embedded payload: %ls", wzPayloadId);
        }

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
    ::LeaveCriticalSection(&pEngineState->csActive);

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

    ::EnterCriticalSection(&pEngineState->csActive);
    hr = UserExperienceEnsureEngineInactive(&pEngineState->userExperience);
    ExitOnFailure(hr, "Engine is active, cannot change engine state.");

    if (wzPayloadId && *wzPayloadId)
    {
        hr = PayloadFindById(&pEngineState->payloads, wzPayloadId, &pPayload);
        ExitOnFailure(hr, "BA requested unknown payload with id: %ls", wzPayloadId);

        if (BURN_PAYLOAD_PACKAGING_EMBEDDED == pPayload->packaging)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_OPERATION);
            ExitOnFailure(hr, "BA denied while trying to set download URL on embedded payload: %ls", wzPayloadId);
        }

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
    ::LeaveCriticalSection(&pEngineState->csActive);

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
    __in const DWORD dwThreadId,
    __in_opt const HWND hwndParent
    )
{
    HRESULT hr = S_OK;

    if (!::PostThreadMessageW(dwThreadId, WM_BURN_DETECT, 0, reinterpret_cast<LPARAM>(hwndParent)))
    {
        ExitWithLastError(hr, "Failed to post detect message.");
    }

LExit:
    return hr;
}

HRESULT ExternalEnginePlan(
    __in const DWORD dwThreadId,
    __in const BOOTSTRAPPER_ACTION action
    )
{
    HRESULT hr = S_OK;

    if (!::PostThreadMessageW(dwThreadId, WM_BURN_PLAN, 0, action))
    {
        ExitWithLastError(hr, "Failed to post plan message.");
    }

LExit:
    return hr;
}

HRESULT ExternalEngineElevate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const DWORD dwThreadId,
    __in_opt const HWND hwndParent
    )
{
    HRESULT hr = S_OK;

    if (INVALID_HANDLE_VALUE != pEngineState->companionConnection.hPipe)
    {
        hr = HRESULT_FROM_WIN32(ERROR_ALREADY_INITIALIZED);
    }
    else if (!::PostThreadMessageW(dwThreadId, WM_BURN_ELEVATE, 0, reinterpret_cast<LPARAM>(hwndParent)))
    {
        ExitWithLastError(hr, "Failed to post elevate message.");
    }

LExit:
    return hr;
}

HRESULT ExternalEngineApply(
    __in const DWORD dwThreadId,
    __in_opt const HWND hwndParent
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(hwndParent, hr, E_INVALIDARG, "BA passed NULL hwndParent to Apply.");
    if (!::IsWindow(hwndParent))
    {
        ExitOnFailure(hr = E_INVALIDARG, "BA passed invalid hwndParent to Apply.");
    }

    if (!::PostThreadMessageW(dwThreadId, WM_BURN_APPLY, 0, reinterpret_cast<LPARAM>(hwndParent)))
    {
        ExitWithLastError(hr, "Failed to post apply message.");
    }

LExit:
    return hr;
}

HRESULT ExternalEngineQuit(
    __in const DWORD dwThreadId,
    __in const DWORD dwExitCode
    )
{
    HRESULT hr = S_OK;

    if (!::PostThreadMessageW(dwThreadId, WM_BURN_QUIT, static_cast<WPARAM>(dwExitCode), 0))
    {
        ExitWithLastError(hr, "Failed to post shutdown message.");
    }

LExit:
    return hr;
}

HRESULT ExternalEngineLaunchApprovedExe(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const DWORD dwThreadId,
    __in_opt const HWND hwndParent,
    __in_z LPCWSTR wzApprovedExeForElevationId,
    __in_z_opt LPCWSTR wzArguments,
    __in const DWORD dwWaitForInputIdleTimeout
    )
{
    HRESULT hr = S_OK;
    BURN_APPROVED_EXE* pApprovedExe = NULL;
    BOOL fLeaveCriticalSection = FALSE;
    BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe = NULL;

    pLaunchApprovedExe = (BURN_LAUNCH_APPROVED_EXE*)MemAlloc(sizeof(BURN_LAUNCH_APPROVED_EXE), TRUE);
    ExitOnNull(pLaunchApprovedExe, hr, E_OUTOFMEMORY, "Failed to alloc BURN_LAUNCH_APPROVED_EXE");

    ::EnterCriticalSection(&pEngineState->csActive);
    fLeaveCriticalSection = TRUE;
    hr = UserExperienceEnsureEngineInactive(&pEngineState->userExperience);
    ExitOnFailure(hr, "Engine is active, cannot change engine state.");

    if (!wzApprovedExeForElevationId || !*wzApprovedExeForElevationId)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    hr = ApprovedExesFindById(&pEngineState->approvedExes, wzApprovedExeForElevationId, &pApprovedExe);
    ExitOnFailure(hr, "BA requested unknown approved exe with id: %ls", wzApprovedExeForElevationId);

    ::LeaveCriticalSection(&pEngineState->csActive);
    fLeaveCriticalSection = FALSE;

    hr = StrAllocString(&pLaunchApprovedExe->sczId, wzApprovedExeForElevationId, NULL);
    ExitOnFailure(hr, "Failed to copy the id.");

    if (wzArguments)
    {
        hr = StrAllocString(&pLaunchApprovedExe->sczArguments, wzArguments, NULL);
        ExitOnFailure(hr, "Failed to copy the arguments.");
    }

    pLaunchApprovedExe->dwWaitForInputIdleTimeout = dwWaitForInputIdleTimeout;

    pLaunchApprovedExe->hwndParent = hwndParent;

    if (!::PostThreadMessageW(dwThreadId, WM_BURN_LAUNCH_APPROVED_EXE, 0, reinterpret_cast<LPARAM>(pLaunchApprovedExe)))
    {
        ExitWithLastError(hr, "Failed to post launch approved exe message.");
    }

LExit:
    if (fLeaveCriticalSection)
    {
        ::LeaveCriticalSection(&pEngineState->csActive);
    }

    if (FAILED(hr))
    {
        ApprovedExesUninitializeLaunch(pLaunchApprovedExe);
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
    __inout DWORD* pcchBuffer
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
        hr = ::StringCchLengthW(wzValue, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(pcchBuffer));
        if (SUCCEEDED(hr))
        {
            hr = E_MOREDATA;
            *pcchBuffer += 1; // null terminator.
        }
    }

    return hr;
}
