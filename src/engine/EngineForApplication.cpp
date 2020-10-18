// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static HRESULT CopyStringToBA(
    __in LPWSTR wzValue,
    __in LPWSTR wzBuffer,
    __inout DWORD* pcchBuffer
    );

static HRESULT BAEngineGetPackageCount(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_GETPACKAGECOUNT_ARGS* /*pArgs*/,
    __in BAENGINE_GETPACKAGECOUNT_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    DWORD* pcPackages = &pResults->cPackages;

    *pcPackages = pContext->pEngineState->packages.cPackages;

    return hr;
}

static HRESULT BAEngineGetVariableNumeric(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_GETVARIABLENUMERIC_ARGS* pArgs,
    __in BAENGINE_GETVARIABLENUMERIC_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzVariable = pArgs->wzVariable;
    LONGLONG* pllValue = &pResults->llValue;

    if (wzVariable && *wzVariable)
    {
        hr = VariableGetNumeric(&pContext->pEngineState->variables, wzVariable, pllValue);
    }
    else
    {
        hr = E_INVALIDARG;
    }

    return hr;
}

static HRESULT BAEngineGetVariableString(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_GETVARIABLESTRING_ARGS* pArgs,
    __in BAENGINE_GETVARIABLESTRING_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    LPCWSTR wzVariable = pArgs->wzVariable;
    LPWSTR wzValue = pResults->wzValue;
    DWORD* pcchValue = &pResults->cchValue;

    if (wzVariable && *wzVariable)
    {
        hr = VariableGetString(&pContext->pEngineState->variables, wzVariable, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = CopyStringToBA(sczValue, wzValue, pcchValue);
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    StrSecureZeroFreeString(sczValue);
    return hr;
}

static HRESULT BAEngineGetVariableVersion(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_GETVARIABLEVERSION_ARGS* pArgs,
    __in BAENGINE_GETVARIABLEVERSION_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pVersion = NULL;
    LPCWSTR wzVariable = pArgs->wzVariable;
    LPWSTR wzValue = pResults->wzValue;
    DWORD* pcchValue = &pResults->cchValue;

    if (wzVariable && *wzVariable)
    {
        hr = VariableGetVersion(&pContext->pEngineState->variables, wzVariable, &pVersion);
        if (SUCCEEDED(hr))
        {
            hr = CopyStringToBA(pVersion->sczVersion, wzValue, pcchValue);
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    ReleaseVerutilVersion(pVersion);

    return hr;
}

static HRESULT BAEngineFormatString(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_FORMATSTRING_ARGS* pArgs,
    __in BAENGINE_FORMATSTRING_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    LPCWSTR wzIn = pArgs->wzIn;
    LPWSTR wzOut = pResults->wzOut;
    DWORD* pcchOut = &pResults->cchOut;

    if (wzIn && *wzIn)
    {
        hr = VariableFormatString(&pContext->pEngineState->variables, wzIn, &sczValue, NULL);
        if (SUCCEEDED(hr))
        {
            hr = CopyStringToBA(sczValue, wzOut, pcchOut);
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    StrSecureZeroFreeString(sczValue);
    return hr;
}

static HRESULT BAEngineEscapeString(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* /*pContext*/,
    __in BAENGINE_ESCAPESTRING_ARGS* pArgs,
    __in BAENGINE_ESCAPESTRING_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    LPCWSTR wzIn = pArgs->wzIn;
    LPWSTR wzOut = pResults->wzOut;
    DWORD* pcchOut = &pResults->cchOut;

    if (wzIn && *wzIn)
    {
        hr = VariableEscapeString(wzIn, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = CopyStringToBA(sczValue, wzOut, pcchOut);
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    StrSecureZeroFreeString(sczValue);
    return hr;
}

static HRESULT BAEngineEvaluateCondition(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_EVALUATECONDITION_ARGS* pArgs,
    __in BAENGINE_EVALUATECONDITION_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzCondition = pArgs->wzCondition;
    BOOL* pf = &pResults->f;

    if (wzCondition && *wzCondition)
    {
        hr = ConditionEvaluate(&pContext->pEngineState->variables, wzCondition, pf);
    }
    else
    {
        hr = E_INVALIDARG;
    }

    return hr;
}

static HRESULT BAEngineLog(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* /*pContext*/,
    __in BAENGINE_LOG_ARGS* pArgs,
    __in BAENGINE_LOG_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;
    REPORT_LEVEL rl = REPORT_NONE;
    BOOTSTRAPPER_LOG_LEVEL level = pArgs->level;
    LPCWSTR wzMessage = pArgs->wzMessage;

    switch (level)
    {
    case BOOTSTRAPPER_LOG_LEVEL_STANDARD:
        rl = REPORT_STANDARD;
        break;

    case BOOTSTRAPPER_LOG_LEVEL_VERBOSE:
        rl = REPORT_VERBOSE;
        break;

    case BOOTSTRAPPER_LOG_LEVEL_DEBUG:
        rl = REPORT_DEBUG;
        break;

    case BOOTSTRAPPER_LOG_LEVEL_ERROR:
        rl = REPORT_ERROR;
        break;

    default:
        ExitFunction1(hr = E_INVALIDARG);
    }

    hr = LogStringLine(rl, "%ls", wzMessage);
    ExitOnFailure(hr, "Failed to log BA message.");

LExit:
    return hr;
}

static HRESULT BAEngineSendEmbeddedError(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_SENDEMBEDDEDERROR_ARGS* pArgs,
    __in BAENGINE_SENDEMBEDDEDERROR_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    DWORD cbData = 0;
    DWORD dwResult = 0;
    DWORD dwErrorCode = pArgs->dwErrorCode;
    LPCWSTR wzMessage = pArgs->wzMessage;
    DWORD dwUIHint = pArgs->dwUIHint;
    int* pnResult = &pResults->nResult;

    if (BURN_MODE_EMBEDDED != pContext->pEngineState->mode)
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

    hr = PipeSendMessage(pContext->pEngineState->embeddedConnection.hPipe, BURN_EMBEDDED_MESSAGE_TYPE_ERROR, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send embedded message over pipe.");

    *pnResult = static_cast<int>(dwResult);

LExit:
    ReleaseBuffer(pbData);
    return hr;
}

static HRESULT BAEngineSendEmbeddedProgress(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_SENDEMBEDDEDPROGRESS_ARGS* pArgs,
    __in BAENGINE_SENDEMBEDDEDPROGRESS_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    DWORD cbData = 0;
    DWORD dwResult = 0;
    DWORD dwProgressPercentage = pArgs->dwProgressPercentage;
    DWORD dwOverallProgressPercentage = pArgs->dwOverallProgressPercentage;
    int* pnResult = &pResults->nResult;

    if (BURN_MODE_EMBEDDED != pContext->pEngineState->mode)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_STATE);
        ExitOnRootFailure(hr, "BA requested to send embedded progress message when not in embedded mode.");
    }

    hr = BuffWriteNumber(&pbData, &cbData, dwProgressPercentage);
    ExitOnFailure(hr, "Failed to write progress percentage to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, dwOverallProgressPercentage);
    ExitOnFailure(hr, "Failed to write overall progress percentage to message buffer.");

    hr = PipeSendMessage(pContext->pEngineState->embeddedConnection.hPipe, BURN_EMBEDDED_MESSAGE_TYPE_PROGRESS, pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send embedded progress message over pipe.");

    *pnResult = static_cast<int>(dwResult);

LExit:
    ReleaseBuffer(pbData);
    return hr;
}

static HRESULT BAEngineSetUpdate(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_SETUPDATE_ARGS* pArgs,
    __in BAENGINE_SETUPDATE_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;
    LPCWSTR sczId = NULL;
    LPWSTR sczLocalSource = NULL;
    LPWSTR sczCommandline = NULL;
    UUID guid = { };
    WCHAR wzGuid[39];
    RPC_STATUS rs = RPC_S_OK;
    LPCWSTR wzLocalSource = pArgs->wzLocalSource;
    LPCWSTR wzDownloadSource = pArgs->wzDownloadSource;
    DWORD64 qwSize = pArgs->qwSize;
    BOOTSTRAPPER_UPDATE_HASH_TYPE hashType = pArgs->hashType;
    BYTE* rgbHash = pArgs->rgbHash;
    DWORD cbHash = pArgs->cbHash;

    ::EnterCriticalSection(&pContext->pEngineState->csActive);

    if ((!wzLocalSource || !*wzLocalSource) && (!wzDownloadSource || !*wzDownloadSource))
    {
        UpdateUninitialize(&pContext->pEngineState->update);
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
        UpdateUninitialize(&pContext->pEngineState->update);

        if (!wzLocalSource || !*wzLocalSource)
        {
            hr = StrAllocFormatted(&sczLocalSource, L"update\\%ls", pContext->pEngineState->registration.sczExecutableName);
            ExitOnFailure(hr, "Failed to default local update source");
        }

        hr = CoreRecreateCommandLine(&sczCommandline, BOOTSTRAPPER_ACTION_INSTALL, pContext->pEngineState->command.display, pContext->pEngineState->command.restart, BOOTSTRAPPER_RELATION_NONE, FALSE, pContext->pEngineState->registration.sczActiveParent, pContext->pEngineState->registration.sczAncestors, NULL, pContext->pEngineState->command.wzCommandLine);
        ExitOnFailure(hr, "Failed to recreate command-line for update bundle.");

        // Per-user bundles would fail to use the downloaded update bundle, as the existing install would already be cached 
        // at the registration id's location.  Here I am generating a random guid, but in the future it would be nice if the
        // feed would provide the ID of the update.
        if (!pContext->pEngineState->registration.fPerMachine)
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
            sczId = pContext->pEngineState->registration.sczId;
        }

        hr = PseudoBundleInitialize(FILEMAKEVERSION(rmj, rmm, rup, 0), &pContext->pEngineState->update.package, FALSE, sczId, BOOTSTRAPPER_RELATION_UPDATE, BOOTSTRAPPER_PACKAGE_STATE_ABSENT, pContext->pEngineState->registration.sczExecutableName, sczLocalSource ? sczLocalSource : wzLocalSource, wzDownloadSource, qwSize, TRUE, sczCommandline, NULL, NULL, NULL, rgbHash, cbHash);
        ExitOnFailure(hr, "Failed to set update bundle.");

        pContext->pEngineState->update.fUpdateAvailable = TRUE;
    }

LExit:
    ::LeaveCriticalSection(&pContext->pEngineState->csActive);

    ReleaseStr(sczCommandline);
    ReleaseStr(sczLocalSource);
    return hr;
}

static HRESULT BAEngineSetLocalSource(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_SETLOCALSOURCE_ARGS* pArgs,
    __in BAENGINE_SETLOCALSOURCE_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER* pContainer = NULL;
    BURN_PAYLOAD* pPayload = NULL;
    LPCWSTR wzPackageOrContainerId = pArgs->wzPackageOrContainerId;
    LPCWSTR wzPayloadId = pArgs->wzPayloadId;
    LPCWSTR wzPath = pArgs->wzPath;

    ::EnterCriticalSection(&pContext->pEngineState->csActive);
    hr = UserExperienceEnsureEngineInactive(&pContext->pEngineState->userExperience);
    ExitOnFailure(hr, "Engine is active, cannot change engine state.");

    if (!wzPath || !*wzPath)
    {
        hr = E_INVALIDARG;
    }
    else if (wzPayloadId && * wzPayloadId)
    {
        hr = PayloadFindById(&pContext->pEngineState->payloads, wzPayloadId, &pPayload);
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
        hr = ContainerFindById(&pContext->pEngineState->containers, wzPackageOrContainerId, &pContainer);
        ExitOnFailure(hr, "BA requested unknown container with id: %ls", wzPackageOrContainerId);

        hr = StrAllocString(&pContainer->sczSourcePath, wzPath, 0);
        ExitOnFailure(hr, "Failed to set source path for container.");
    }
    else
    {
        hr = E_INVALIDARG;
    }

LExit:
    ::LeaveCriticalSection(&pContext->pEngineState->csActive);
    return hr;
}

static HRESULT BAEngineSetDownloadSource(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_SETDOWNLOADSOURCE_ARGS* pArgs,
    __in BAENGINE_SETDOWNLOADSOURCE_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER* pContainer = NULL;
    BURN_PAYLOAD* pPayload = NULL;
    DOWNLOAD_SOURCE* pDownloadSource = NULL;
    LPCWSTR wzPackageOrContainerId = pArgs->wzPackageOrContainerId;
    LPCWSTR wzPayloadId = pArgs->wzPayloadId;
    LPCWSTR wzUrl = pArgs->wzUrl;
    LPCWSTR wzUser = pArgs->wzUser;
    LPCWSTR wzPassword = pArgs->wzPassword;

    ::EnterCriticalSection(&pContext->pEngineState->csActive);
    hr = UserExperienceEnsureEngineInactive(&pContext->pEngineState->userExperience);
    ExitOnFailure(hr, "Engine is active, cannot change engine state.");

    if (wzPayloadId && *wzPayloadId)
    {
        hr = PayloadFindById(&pContext->pEngineState->payloads, wzPayloadId, &pPayload);
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
        hr = ContainerFindById(&pContext->pEngineState->containers, wzPackageOrContainerId, &pContainer);
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
    ::LeaveCriticalSection(&pContext->pEngineState->csActive);
    return hr;
}

static HRESULT BAEngineSetVariableNumeric(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_SETVARIABLENUMERIC_ARGS* pArgs,
    __in BAENGINE_SETVARIABLENUMERIC_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzVariable = pArgs->wzVariable;
    LONGLONG llValue = pArgs->llValue;

    if (wzVariable && *wzVariable)
    {
        hr = VariableSetNumeric(&pContext->pEngineState->variables, wzVariable, llValue, FALSE);
        ExitOnFailure(hr, "Failed to set numeric variable.");
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "BA did not provide variable name.");
    }

LExit:
    return hr;
}

static HRESULT BAEngineSetVariableString(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_SETVARIABLESTRING_ARGS* pArgs,
    __in BAENGINE_SETVARIABLESTRING_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzVariable = pArgs->wzVariable;
    LPCWSTR wzValue = pArgs->wzValue;

    if (wzVariable && *wzVariable)
    {
        hr = VariableSetString(&pContext->pEngineState->variables, wzVariable, wzValue, FALSE, pArgs->fFormatted);
        ExitOnFailure(hr, "Failed to set string variable.");
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "BA did not provide variable name.");
    }

LExit:
    return hr;
}

static HRESULT BAEngineSetVariableVersion(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_SETVARIABLEVERSION_ARGS* pArgs,
    __in BAENGINE_SETVARIABLEVERSION_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzVariable = pArgs->wzVariable;
    LPCWSTR wzValue = pArgs->wzValue;
    VERUTIL_VERSION* pVersion = NULL;

    if (wzVariable && *wzVariable)
    {
        hr = VerParseVersion(wzValue, 0, FALSE, &pVersion);
        ExitOnFailure(hr, "Failed to parse new version value.");

        hr = VariableSetVersion(&pContext->pEngineState->variables, wzVariable, pVersion, FALSE);
        ExitOnFailure(hr, "Failed to set version variable.");
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "BA did not provide variable name.");
    }

LExit:
    ReleaseVerutilVersion(pVersion);

    return hr;
}

static HRESULT BAEngineCloseSplashScreen(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_CLOSESPLASHSCREEN_ARGS* /*pArgs*/,
    __in BAENGINE_CLOSESPLASHSCREEN_RESULTS* /*pResults*/
    )
{
    // If the splash screen is still around, close it.
    if (::IsWindow(pContext->pEngineState->command.hwndSplashScreen))
    {
        ::PostMessageW(pContext->pEngineState->command.hwndSplashScreen, WM_CLOSE, 0, 0);
    }

    return S_OK;
}

static HRESULT BAEngineCompareVersions(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* /*pContext*/,
    __in const BAENGINE_COMPAREVERSIONS_ARGS* pArgs,
    __in BAENGINE_COMPAREVERSIONS_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzVersion1 = pArgs->wzVersion1;
    LPCWSTR wzVersion2 = pArgs->wzVersion2;
    int* pnResult = &pResults->nResult;

    hr = VerCompareStringVersions(wzVersion1, wzVersion2, FALSE, pnResult);

    return hr;
}

static HRESULT BAEngineDetect(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_DETECT_ARGS* pArgs,
    __in BAENGINE_DETECT_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;

    if (!::PostThreadMessageW(pContext->dwThreadId, WM_BURN_DETECT, 0, reinterpret_cast<LPARAM>(pArgs->hwndParent)))
    {
        ExitWithLastError(hr, "Failed to post detect message.");
    }

LExit:
    return hr;
}

static HRESULT BAEnginePlan(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_PLAN_ARGS* pArgs,
    __in BAENGINE_PLAN_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ACTION action = pArgs->action;

    if (!::PostThreadMessageW(pContext->dwThreadId, WM_BURN_PLAN, 0, action))
    {
        ExitWithLastError(hr, "Failed to post plan message.");
    }

LExit:
    return hr;
}

static HRESULT BAEngineElevate(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_ELEVATE_ARGS* pArgs,
    __in BAENGINE_ELEVATE_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;

    if (INVALID_HANDLE_VALUE != pContext->pEngineState->companionConnection.hPipe)
    {
        hr = HRESULT_FROM_WIN32(ERROR_ALREADY_INITIALIZED);
    }
    else if (!::PostThreadMessageW(pContext->dwThreadId, WM_BURN_ELEVATE, 0, reinterpret_cast<LPARAM>(pArgs->hwndParent)))
    {
        ExitWithLastError(hr, "Failed to post elevate message.");
    }

LExit:
    return hr;
}

static HRESULT BAEngineApply(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_APPLY_ARGS* pArgs,
    __in BAENGINE_APPLY_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(pArgs->hwndParent, hr, E_INVALIDARG, "BA passed NULL hwndParent to Apply.");
    if (!::IsWindow(pArgs->hwndParent))
    {
        ExitOnFailure(hr = E_INVALIDARG, "BA passed invalid hwndParent to Apply.");
    }

    if (!::PostThreadMessageW(pContext->dwThreadId, WM_BURN_APPLY, 0, reinterpret_cast<LPARAM>(pArgs->hwndParent)))
    {
        ExitWithLastError(hr, "Failed to post apply message.");
    }

LExit:
    return hr;
}

static HRESULT BAEngineQuit(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_QUIT_ARGS* pArgs,
    __in BAENGINE_QUIT_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;

    if (!::PostThreadMessageW(pContext->dwThreadId, WM_BURN_QUIT, static_cast<WPARAM>(pArgs->dwExitCode), 0))
    {
        ExitWithLastError(hr, "Failed to post shutdown message.");
    }

LExit:
    return hr;
}

static HRESULT BAEngineLaunchApprovedExe(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const BAENGINE_LAUNCHAPPROVEDEXE_ARGS* pArgs,
    __in BAENGINE_LAUNCHAPPROVEDEXE_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;
    BURN_APPROVED_EXE* pApprovedExe = NULL;
    BOOL fLeaveCriticalSection = FALSE;
    BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe = (BURN_LAUNCH_APPROVED_EXE*)MemAlloc(sizeof(BURN_LAUNCH_APPROVED_EXE), TRUE);
    HWND hwndParent = pArgs->hwndParent;
    LPCWSTR wzApprovedExeForElevationId = pArgs->wzApprovedExeForElevationId;
    LPCWSTR wzArguments = pArgs->wzArguments;
    DWORD dwWaitForInputIdleTimeout = pArgs->dwWaitForInputIdleTimeout;

    ::EnterCriticalSection(&pContext->pEngineState->csActive);
    fLeaveCriticalSection = TRUE;
    hr = UserExperienceEnsureEngineInactive(&pContext->pEngineState->userExperience);
    ExitOnFailure(hr, "Engine is active, cannot change engine state.");

    if (!wzApprovedExeForElevationId || !*wzApprovedExeForElevationId)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    hr = ApprovedExesFindById(&pContext->pEngineState->approvedExes, wzApprovedExeForElevationId, &pApprovedExe);
    ExitOnFailure(hr, "BA requested unknown approved exe with id: %ls", wzApprovedExeForElevationId);

    ::LeaveCriticalSection(&pContext->pEngineState->csActive);
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

    if (!::PostThreadMessageW(pContext->dwThreadId, WM_BURN_LAUNCH_APPROVED_EXE, 0, reinterpret_cast<LPARAM>(pLaunchApprovedExe)))
    {
        ExitWithLastError(hr, "Failed to post launch approved exe message.");
    }

LExit:
    if (fLeaveCriticalSection)
    {
        ::LeaveCriticalSection(&pContext->pEngineState->csActive);
    }

    if (FAILED(hr))
    {
        ApprovedExesUninitializeLaunch(pLaunchApprovedExe);
    }

    return hr;
}

HRESULT WINAPI EngineForApplicationProc(
    __in BOOTSTRAPPER_ENGINE_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ENGINE_CONTEXT* pContext = reinterpret_cast<BOOTSTRAPPER_ENGINE_CONTEXT*>(pvContext);

    if (!pContext || !pvArgs || !pvResults)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    switch (message)
    {
    case BOOTSTRAPPER_ENGINE_MESSAGE_GETPACKAGECOUNT:
        hr = BAEngineGetPackageCount(pContext, reinterpret_cast<BAENGINE_GETPACKAGECOUNT_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_GETPACKAGECOUNT_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLENUMERIC:
        hr = BAEngineGetVariableNumeric(pContext, reinterpret_cast<BAENGINE_GETVARIABLENUMERIC_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_GETVARIABLENUMERIC_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLESTRING:
        hr = BAEngineGetVariableString(pContext, reinterpret_cast<BAENGINE_GETVARIABLESTRING_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_GETVARIABLESTRING_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLEVERSION:
        hr = BAEngineGetVariableVersion(pContext, reinterpret_cast<BAENGINE_GETVARIABLEVERSION_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_GETVARIABLEVERSION_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_FORMATSTRING:
        hr = BAEngineFormatString(pContext, reinterpret_cast<BAENGINE_FORMATSTRING_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_FORMATSTRING_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_ESCAPESTRING:
        hr = BAEngineEscapeString(pContext, reinterpret_cast<BAENGINE_ESCAPESTRING_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_ESCAPESTRING_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_EVALUATECONDITION:
        hr = BAEngineEvaluateCondition(pContext, reinterpret_cast<BAENGINE_EVALUATECONDITION_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_EVALUATECONDITION_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_LOG:
        hr = BAEngineLog(pContext, reinterpret_cast<BAENGINE_LOG_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_LOG_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDERROR:
        hr = BAEngineSendEmbeddedError(pContext, reinterpret_cast<BAENGINE_SENDEMBEDDEDERROR_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_SENDEMBEDDEDERROR_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDPROGRESS:
        hr = BAEngineSendEmbeddedProgress(pContext, reinterpret_cast<BAENGINE_SENDEMBEDDEDPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_SENDEMBEDDEDPROGRESS_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETUPDATE:
        hr = BAEngineSetUpdate(pContext, reinterpret_cast<BAENGINE_SETUPDATE_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_SETUPDATE_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETLOCALSOURCE:
        hr = BAEngineSetLocalSource(pContext, reinterpret_cast<BAENGINE_SETLOCALSOURCE_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_SETLOCALSOURCE_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETDOWNLOADSOURCE:
        hr = BAEngineSetDownloadSource(pContext, reinterpret_cast<BAENGINE_SETDOWNLOADSOURCE_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_SETDOWNLOADSOURCE_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLENUMERIC:
        hr = BAEngineSetVariableNumeric(pContext, reinterpret_cast<BAENGINE_SETVARIABLENUMERIC_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_SETVARIABLENUMERIC_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLESTRING:
        hr = BAEngineSetVariableString(pContext, reinterpret_cast<BAENGINE_SETVARIABLESTRING_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_SETVARIABLESTRING_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLEVERSION:
        hr = BAEngineSetVariableVersion(pContext, reinterpret_cast<BAENGINE_SETVARIABLEVERSION_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_SETVARIABLEVERSION_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_CLOSESPLASHSCREEN:
        hr = BAEngineCloseSplashScreen(pContext, reinterpret_cast<BAENGINE_CLOSESPLASHSCREEN_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_CLOSESPLASHSCREEN_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_DETECT:
        hr = BAEngineDetect(pContext, reinterpret_cast<BAENGINE_DETECT_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_DETECT_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_PLAN:
        hr = BAEnginePlan(pContext, reinterpret_cast<BAENGINE_PLAN_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_PLAN_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_ELEVATE:
        hr = BAEngineElevate(pContext, reinterpret_cast<BAENGINE_ELEVATE_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_ELEVATE_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_APPLY:
        hr = BAEngineApply(pContext, reinterpret_cast<BAENGINE_APPLY_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_APPLY_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_QUIT:
        hr = BAEngineQuit(pContext, reinterpret_cast<BAENGINE_QUIT_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_QUIT_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_LAUNCHAPPROVEDEXE:
        hr = BAEngineLaunchApprovedExe(pContext, reinterpret_cast<BAENGINE_LAUNCHAPPROVEDEXE_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_LAUNCHAPPROVEDEXE_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_COMPAREVERSIONS:
        hr = BAEngineCompareVersions(pContext, reinterpret_cast<BAENGINE_COMPAREVERSIONS_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_COMPAREVERSIONS_RESULTS*>(pvResults));
        break;
    default:
        hr = E_NOTIMPL;
        break;
    }

LExit:
    return hr;
}

static HRESULT CopyStringToBA(
    __in LPWSTR wzValue,
    __in LPWSTR wzBuffer,
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
