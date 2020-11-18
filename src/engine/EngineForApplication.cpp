// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static HRESULT BAEngineGetPackageCount(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_GETPACKAGECOUNT_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_GETPACKAGECOUNT_RESULTS, pResults);

    ExternalEngineGetPackageCount(pContext->pEngineState, &pResults->cPackages);

LExit:
    return hr;
}

static HRESULT BAEngineGetVariableNumeric(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_GETVARIABLENUMERIC_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_GETVARIABLENUMERIC_RESULTS, pResults);

    hr = ExternalEngineGetVariableNumeric(pContext->pEngineState, pArgs->wzVariable, &pResults->llValue);

LExit:
    return hr;
}

static HRESULT BAEngineGetVariableString(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_GETVARIABLESTRING_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_GETVARIABLESTRING_RESULTS, pResults);

    hr = ExternalEngineGetVariableString(pContext->pEngineState, pArgs->wzVariable, pResults->wzValue, &pResults->cchValue);

LExit:
    return hr;
}

static HRESULT BAEngineGetVariableVersion(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_GETVARIABLEVERSION_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_GETVARIABLEVERSION_RESULTS, pResults);

    hr = ExternalEngineGetVariableVersion(pContext->pEngineState, pArgs->wzVariable, pResults->wzValue, &pResults->cchValue);

LExit:
    return hr;
}

static HRESULT BAEngineFormatString(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_FORMATSTRING_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_FORMATSTRING_RESULTS, pResults);

    hr = ExternalEngineFormatString(pContext->pEngineState, pArgs->wzIn, pResults->wzOut, &pResults->cchOut);

LExit:
    return hr;
}

static HRESULT BAEngineEscapeString(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* /*pContext*/,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_ESCAPESTRING_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_ESCAPESTRING_RESULTS, pResults);

    hr = ExternalEngineEscapeString(pArgs->wzIn, pResults->wzOut, &pResults->cchOut);

LExit:
    return hr;
}

static HRESULT BAEngineEvaluateCondition(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_EVALUATECONDITION_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_EVALUATECONDITION_RESULTS, pResults);

    hr = ExternalEngineEvaluateCondition(pContext->pEngineState, pArgs->wzCondition, &pResults->f);

LExit:
    return hr;
}

static HRESULT BAEngineLog(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* /*pContext*/,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    REPORT_LEVEL rl = REPORT_NONE;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_LOG_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_LOG_RESULTS, pResults);

    switch (pArgs->level)
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

    hr = ExternalEngineLog(rl, pArgs->wzMessage);
    ExitOnFailure(hr, "Failed to log BA message.");

LExit:
    return hr;
}

static HRESULT BAEngineSendEmbeddedError(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_SENDEMBEDDEDERROR_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_SENDEMBEDDEDERROR_RESULTS, pResults);

    hr = ExternalEngineSendEmbeddedError(pContext->pEngineState, pArgs->dwErrorCode, pArgs->wzMessage, pArgs->dwUIHint, &pResults->nResult);

LExit:
    return hr;
}

static HRESULT BAEngineSendEmbeddedProgress(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_SENDEMBEDDEDPROGRESS_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_SENDEMBEDDEDPROGRESS_RESULTS, pResults);

    hr = ExternalEngineSendEmbeddedProgress(pContext->pEngineState, pArgs->dwProgressPercentage, pArgs->dwOverallProgressPercentage, &pResults->nResult);

LExit:
    return hr;
}

static HRESULT BAEngineSetUpdate(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_SETUPDATE_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_SETUPDATE_RESULTS, pResults);

    hr = ExternalEngineSetUpdate(pContext->pEngineState, pArgs->wzLocalSource, pArgs->wzDownloadSource, pArgs->qwSize, pArgs->hashType, pArgs->rgbHash, pArgs->cbHash);

LExit:
    return hr;
}

static HRESULT BAEngineSetLocalSource(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_SETLOCALSOURCE_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_SETLOCALSOURCE_RESULTS, pResults);

    hr = ExternalEngineSetLocalSource(pContext->pEngineState, pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->wzPath);

LExit:
    return hr;
}

static HRESULT BAEngineSetDownloadSource(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_SETDOWNLOADSOURCE_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_SETDOWNLOADSOURCE_RESULTS, pResults);

    hr = ExternalEngineSetDownloadSource(pContext->pEngineState, pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->wzUrl, pArgs->wzUser, pArgs->wzPassword);

LExit:
    return hr;
}

static HRESULT BAEngineSetVariableNumeric(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_SETVARIABLENUMERIC_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_SETVARIABLENUMERIC_RESULTS, pResults);

    hr = ExternalEngineSetVariableNumeric(pContext->pEngineState, pArgs->wzVariable, pArgs->llValue);

LExit:
    return hr;
}

static HRESULT BAEngineSetVariableString(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_SETVARIABLESTRING_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_SETVARIABLESTRING_RESULTS, pResults);

    hr = ExternalEngineSetVariableString(pContext->pEngineState, pArgs->wzVariable, pArgs->wzValue, pArgs->fFormatted);

LExit:
    return hr;
}

static HRESULT BAEngineSetVariableVersion(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_SETVARIABLEVERSION_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_SETVARIABLEVERSION_RESULTS, pResults);

    hr = ExternalEngineSetVariableVersion(pContext->pEngineState, pArgs->wzVariable, pArgs->wzValue);

LExit:
    return hr;
}

static HRESULT BAEngineCloseSplashScreen(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_CLOSESPLASHSCREEN_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_CLOSESPLASHSCREEN_RESULTS, pResults);

    ExternalEngineCloseSplashScreen(pContext->pEngineState);

LExit:
    return hr;
}

static HRESULT BAEngineCompareVersions(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* /*pContext*/,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_COMPAREVERSIONS_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_COMPAREVERSIONS_RESULTS, pResults);

    hr = ExternalEngineCompareVersions(pArgs->wzVersion1, pArgs->wzVersion2, &pResults->nResult);

LExit:
    return hr;
}

static HRESULT BAEngineDetect(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_DETECT_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_DETECT_RESULTS, pResults);

    hr = ExternalEngineDetect(pContext->dwThreadId, pArgs->hwndParent);

LExit:
    return hr;
}

static HRESULT BAEnginePlan(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_PLAN_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_PLAN_RESULTS, pResults);

    hr = ExternalEnginePlan(pContext->dwThreadId, pArgs->action);

LExit:
    return hr;
}

static HRESULT BAEngineElevate(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_ELEVATE_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_ELEVATE_RESULTS, pResults);

    hr = ExternalEngineElevate(pContext->pEngineState, pContext->dwThreadId, pArgs->hwndParent);

LExit:
    return hr;
}

static HRESULT BAEngineApply(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_APPLY_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_APPLY_RESULTS, pResults);

    hr = ExternalEngineApply(pContext->dwThreadId, pArgs->hwndParent);

LExit:
    return hr;
}

static HRESULT BAEngineQuit(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_QUIT_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_QUIT_RESULTS, pResults);

    hr = ExternalEngineQuit(pContext->dwThreadId, pArgs->dwExitCode);

LExit:
    return hr;
}

static HRESULT BAEngineLaunchApprovedExe(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BAENGINE_LAUNCHAPPROVEDEXE_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BAENGINE_LAUNCHAPPROVEDEXE_RESULTS, pResults);

    hr = ExternalEngineLaunchApprovedExe(pContext->pEngineState, pContext->dwThreadId, pArgs->hwndParent, pArgs->wzApprovedExeForElevationId, pArgs->wzArguments, pArgs->dwWaitForInputIdleTimeout);

LExit:
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
        hr = BAEngineGetPackageCount(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLENUMERIC:
        hr = BAEngineGetVariableNumeric(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLESTRING:
        hr = BAEngineGetVariableString(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLEVERSION:
        hr = BAEngineGetVariableVersion(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_FORMATSTRING:
        hr = BAEngineFormatString(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_ESCAPESTRING:
        hr = BAEngineEscapeString(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_EVALUATECONDITION:
        hr = BAEngineEvaluateCondition(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_LOG:
        hr = BAEngineLog(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDERROR:
        hr = BAEngineSendEmbeddedError(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDPROGRESS:
        hr = BAEngineSendEmbeddedProgress(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETUPDATE:
        hr = BAEngineSetUpdate(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETLOCALSOURCE:
        hr = BAEngineSetLocalSource(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETDOWNLOADSOURCE:
        hr = BAEngineSetDownloadSource(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLENUMERIC:
        hr = BAEngineSetVariableNumeric(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLESTRING:
        hr = BAEngineSetVariableString(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLEVERSION:
        hr = BAEngineSetVariableVersion(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_CLOSESPLASHSCREEN:
        hr = BAEngineCloseSplashScreen(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_DETECT:
        hr = BAEngineDetect(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_PLAN:
        hr = BAEnginePlan(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_ELEVATE:
        hr = BAEngineElevate(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_APPLY:
        hr = BAEngineApply(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_QUIT:
        hr = BAEngineQuit(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_LAUNCHAPPROVEDEXE:
        hr = BAEngineLaunchApprovedExe(pContext, pvArgs, pvResults);
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_COMPAREVERSIONS:
        hr = BAEngineCompareVersions(pContext, pvArgs, pvResults);
        break;
    default:
        hr = E_NOTIMPL;
        break;
    }

LExit:
    return hr;
}
