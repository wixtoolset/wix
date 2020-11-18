// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static HRESULT BEEngineEscapeString(
    __in BURN_EXTENSION_ENGINE_CONTEXT* /*pContext*/,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_ESCAPESTRING_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_ESCAPESTRING_RESULTS, pResults);

    hr = ExternalEngineEscapeString(pArgs->wzIn, pResults->wzOut, &pResults->cchOut);

LExit:
    return hr;
}

static HRESULT BEEngineEvaluateCondition(
    __in BURN_EXTENSION_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_EVALUATECONDITION_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_EVALUATECONDITION_RESULTS, pResults);

    hr = ExternalEngineEvaluateCondition(pContext->pEngineState, pArgs->wzCondition, &pResults->f);

LExit:
    return hr;
}

static HRESULT BEEngineFormatString(
    __in BURN_EXTENSION_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_FORMATSTRING_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_FORMATSTRING_RESULTS, pResults);

    hr = ExternalEngineFormatString(pContext->pEngineState, pArgs->wzIn, pResults->wzOut, &pResults->cchOut);

LExit:
    return hr;
}

static HRESULT BEEngineGetVariableNumeric(
    __in BURN_EXTENSION_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_GETVARIABLENUMERIC_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_GETVARIABLENUMERIC_RESULTS, pResults);

    hr = ExternalEngineGetVariableNumeric(pContext->pEngineState, pArgs->wzVariable, &pResults->llValue);

LExit:
    return hr;
}

static HRESULT BEEngineGetVariableString(
    __in BURN_EXTENSION_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_GETVARIABLESTRING_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_GETVARIABLESTRING_RESULTS, pResults);

    hr = ExternalEngineGetVariableString(pContext->pEngineState, pArgs->wzVariable, pResults->wzValue, &pResults->cchValue);

LExit:
    return hr;
}

static HRESULT BEEngineGetVariableVersion(
    __in BURN_EXTENSION_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_GETVARIABLEVERSION_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_GETVARIABLEVERSION_RESULTS, pResults);

    hr = ExternalEngineGetVariableVersion(pContext->pEngineState, pArgs->wzVariable, pResults->wzValue, &pResults->cchValue);

LExit:
    return hr;
}

static HRESULT BEEngineLog(
    __in BURN_EXTENSION_ENGINE_CONTEXT* /*pContext*/,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    REPORT_LEVEL rl = REPORT_NONE;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_LOG_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_LOG_RESULTS, pResults);

    switch (pArgs->level)
    {
    case BUNDLE_EXTENSION_LOG_LEVEL_STANDARD:
        rl = REPORT_STANDARD;
        break;

    case BUNDLE_EXTENSION_LOG_LEVEL_VERBOSE:
        rl = REPORT_VERBOSE;
        break;

    case BUNDLE_EXTENSION_LOG_LEVEL_DEBUG:
        rl = REPORT_DEBUG;
        break;

    case BUNDLE_EXTENSION_LOG_LEVEL_ERROR:
        rl = REPORT_ERROR;
        break;

    default:
        ExitFunction1(hr = E_INVALIDARG);
    }

    hr = ExternalEngineLog(rl, pArgs->wzMessage);
    ExitOnFailure(hr, "Failed to log Bundle Extension message.");

LExit:
    return hr;
}

static HRESULT BEEngineSetVariableNumeric(
    __in BURN_EXTENSION_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_SETVARIABLENUMERIC_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_SETVARIABLENUMERIC_RESULTS, pResults);

    hr = ExternalEngineSetVariableNumeric(pContext->pEngineState, pArgs->wzVariable, pArgs->llValue);

LExit:
    return hr;
}

static HRESULT BEEngineSetVariableString(
    __in BURN_EXTENSION_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_SETVARIABLESTRING_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_SETVARIABLESTRING_RESULTS, pResults);

    hr = ExternalEngineSetVariableString(pContext->pEngineState, pArgs->wzVariable, pArgs->wzValue, pArgs->fFormatted);

LExit:
    return hr;
}

static HRESULT BEEngineSetVariableVersion(
    __in BURN_EXTENSION_ENGINE_CONTEXT* pContext,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_SETVARIABLEVERSION_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_SETVARIABLEVERSION_RESULTS, pResults);

    hr = ExternalEngineSetVariableVersion(pContext->pEngineState, pArgs->wzVariable, pArgs->wzValue);

LExit:
    return hr;
}

static HRESULT BEEngineCompareVersions(
    __in BURN_EXTENSION_ENGINE_CONTEXT* /*pContext*/,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;
    ValidateMessageArgs(hr, pvArgs, BUNDLE_EXTENSION_ENGINE_COMPAREVERSIONS_ARGS, pArgs);
    ValidateMessageResults(hr, pvResults, BUNDLE_EXTENSION_ENGINE_COMPAREVERSIONS_RESULTS, pResults);

    hr = ExternalEngineCompareVersions(pArgs->wzVersion1, pArgs->wzVersion2, &pResults->nResult);

LExit:
    return hr;
}

HRESULT WINAPI EngineForExtensionProc(
    __in BUNDLE_EXTENSION_ENGINE_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    BURN_EXTENSION_ENGINE_CONTEXT* pContext = reinterpret_cast<BURN_EXTENSION_ENGINE_CONTEXT*>(pvContext);

    if (!pContext || !pvArgs || !pvResults)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    switch (message)
    {
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_ESCAPESTRING:
        hr = BEEngineEscapeString(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_EVALUATECONDITION:
        hr = BEEngineEvaluateCondition(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_FORMATSTRING:
        hr = BEEngineFormatString(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_GETVARIABLENUMERIC:
        hr = BEEngineGetVariableNumeric(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_GETVARIABLESTRING:
        hr = BEEngineGetVariableString(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_GETVARIABLEVERSION:
        hr = BEEngineGetVariableVersion(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_LOG:
        hr = BEEngineLog(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_SETVARIABLENUMERIC:
        hr = BEEngineSetVariableNumeric(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_SETVARIABLESTRING:
        hr = BEEngineSetVariableString(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_SETVARIABLEVERSION:
        hr = BEEngineSetVariableVersion(pContext, pvArgs, pvResults);
        break;
    case BUNDLE_EXTENSION_ENGINE_MESSAGE_COMPAREVERSIONS:
        hr = BEEngineCompareVersions(pContext, pvArgs, pvResults);
        break;
    default:
        hr = E_NOTIMPL;
        break;
    }

LExit:
    return hr;
}
