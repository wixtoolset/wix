// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static DWORD WINAPI BAEngineMessagePumpThreadProc(
    __in LPVOID lpThreadParameter
);
static void CALLBACK FreeQueueItem(
    __in void* pvValue,
    __in void* /*pvContext*/
);


extern "C" HRESULT BAEngineCreateContext(
    __in BURN_ENGINE_STATE *pEngineState,
    __inout BAENGINE_CONTEXT** ppContext
)
{
    HRESULT hr = S_OK;
    BAENGINE_CONTEXT* pContext = NULL;

    pContext = static_cast<BAENGINE_CONTEXT*>(MemAlloc(sizeof(BAENGINE_CONTEXT), TRUE));
    ExitOnNull(pContext, hr, E_OUTOFMEMORY, "Failed to allocate bootstrapper application engine context.");

    ::InitializeCriticalSection(&pContext->csQueue);

    pContext->hQueueSemaphore = ::CreateSemaphoreW(NULL, 0, LONG_MAX, NULL);
    ExitOnNullWithLastError(pContext->hQueueSemaphore, hr, "Failed to create semaphore for queue.");

    hr = QueCreate(&pContext->hQueue);
    ExitOnFailure(hr, "Failed to create queue for bootstrapper engine.");

    pContext->pEngineState = pEngineState;

    *ppContext = pContext;
    pContext = NULL;

LExit:
    if (pContext)
    {
        BAEngineFreeContext(pContext);
        pContext = NULL;
    }

    return hr;
}

extern "C" void BAEngineFreeContext(
    __in BAENGINE_CONTEXT* pContext
)
{
    PipeRpcUninitiailize(&pContext->hRpcPipe);
    ReleaseQueue(pContext->hQueue, FreeQueueItem, pContext);
    ReleaseHandle(pContext->hQueueSemaphore);
    ::DeleteCriticalSection(&pContext->csQueue);
}

extern "C" void DAPI BAEngineFreeAction(
    __in BAENGINE_ACTION * pAction
)
{
    switch (pAction->dwMessage)
    {
    case WM_BURN_LAUNCH_APPROVED_EXE:
        ApprovedExesUninitializeLaunch(&pAction->launchApprovedExe);
        break;
    }

    MemFree(pAction);
}

extern "C" HRESULT BAEngineStartListening(
    __in BAENGINE_CONTEXT *pContext,
    __in HANDLE hBAEnginePipe
)
{
    HRESULT hr = S_OK;

    if (PipeRpcInitialized(&pContext->hRpcPipe))
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Bootstrapper application engine already listening on a pipe.");
    }

    PipeRpcInitialize(&pContext->hRpcPipe, hBAEnginePipe, TRUE);

    pContext->hThread = ::CreateThread(NULL, 0, BAEngineMessagePumpThreadProc, pContext, 0, NULL);
    ExitOnNullWithLastError(pContext->hThread, hr, "Failed to create bootstrapper application engine thread.");

LExit:
    return hr;
}

extern "C" HRESULT BAEngineStopListening(
    __in BAENGINE_CONTEXT * pContext
)
{
    HRESULT hr = S_OK;

    // If the pipe was open, this should cause the bootstrapper application engine pipe thread to stop pumping messages and exit.
    if (PipeRpcInitialized(&pContext->hRpcPipe))
    {
        PipeWriteDisconnect(pContext->hRpcPipe.hPipe);

        PipeRpcUninitiailize(&pContext->hRpcPipe);
    }

    if (pContext->hThread)
    {
        hr = AppWaitForSingleObject(pContext->hThread, INFINITE);

        ReleaseHandle(pContext->hThread); // always release the thread, no matter if we were able to wait for it to join or not.

        ExitOnFailure(hr, "Failed to wait for bootstrapper application engine pipe thread.");
    }

LExit:
    return hr;
}

static void CALLBACK FreeQueueItem(
    __in void* pvValue,
    __in void* /*pvContext*/
)
{
    BAENGINE_ACTION* pAction = reinterpret_cast<BAENGINE_ACTION*>(pvValue);

    LogId(REPORT_WARNING, MSG_IGNORE_OPERATION_AFTER_QUIT, LoggingBurnMessageToString(pAction->dwMessage));

    BAEngineFreeAction(pAction);
    MemFree(pAction);
}

static HRESULT BAEngineGetPackageCount(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_GETPACKAGECOUNT_ARGS args = { };
    BAENGINE_GETPACKAGECOUNT_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetPackageCount args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetPackageCount results.");

    // Execute.
    ExternalEngineGetPackageCount(pContext->pEngineState, &results.cPackages);

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineGetPackageCount struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.cPackages);
    ExitOnFailure(hr, "Failed to write length of value of BAEngineGetPackageCount struct.");

LExit:
    return hr;
}

static HRESULT BAEngineGetVariableNumeric(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_GETVARIABLENUMERIC_ARGS args = { };
    BAENGINE_GETVARIABLENUMERIC_RESULTS results = { };
    LPWSTR sczVariable = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetVariableNumeric args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVariable);
    ExitOnFailure(hr, "Failed to read variable name of BAEngineGetVariableNumeric args.");

    args.wzVariable = sczVariable;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetVariableNumeric results.");

    // Execute.
    hr = ExternalEngineGetVariableNumeric(pContext->pEngineState, args.wzVariable, &results.llValue);

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineGetVariableNumeric struct.");

    hr = BuffWriteNumber64ToBuffer(pBuffer, (DWORD64)results.llValue);
    ExitOnFailure(hr, "Failed to write length of value of BAEngineGetVariableNumeric struct.");

LExit:
    ReleaseStr(sczVariable);
    return hr;
}

static HRESULT BAEngineGetVariableString(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_GETVARIABLESTRING_ARGS args = { };
    BAENGINE_GETVARIABLESTRING_RESULTS results = { };
    LPWSTR sczVariable = NULL;
    LPWSTR sczValue = NULL;
    DWORD cchValue = 0;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetVariableString args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVariable);
    ExitOnFailure(hr, "Failed to read variable name of BAEngineGetVariableString args.");

    args.wzVariable = sczVariable;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetVariableString results.");

    hr = BuffReaderReadNumber(pReaderResults, &cchValue);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetVariableString results.");

    results.cchValue = cchValue;

    // Execute.
    hr = VariableGetString(&pContext->pEngineState->variables, args.wzVariable, &sczValue);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get string variable: %ls", sczVariable);

    results.cchValue = lstrlenW(sczValue);
    results.wzValue = sczValue;

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineGetVariableString struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.cchValue);
    ExitOnFailure(hr, "Failed to write length of value of BAEngineGetVariableString struct.");

    hr = BuffWriteStringToBuffer(pBuffer, results.wzValue);
    ExitOnFailure(hr, "Failed to write value of BAEngineGetVariableString struct.");

LExit:
    ReleaseStr(sczValue);
    ReleaseStr(sczVariable);

    return hr;
}

static HRESULT BAEngineGetVariableVersion(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_GETVARIABLEVERSION_ARGS args = { };
    BAENGINE_GETVARIABLEVERSION_RESULTS results = { };
    LPWSTR sczVariable = NULL;
    VERUTIL_VERSION* pVersion = NULL;
    DWORD cchValue = 0;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetVariableVersion args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVariable);
    ExitOnFailure(hr, "Failed to read variable name of BAEngineGetVariableVersion args.");

    args.wzVariable = sczVariable;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetVariableVersion results.");

    hr = BuffReaderReadNumber(pReaderResults, &cchValue);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetVariableVersion results.");

    results.cchValue = cchValue;

    // Execute.
    hr = VariableGetVersion(&pContext->pEngineState->variables, args.wzVariable, &pVersion);
    ExitOnFailure(hr, "Failed to get version variable: %ls", sczVariable);

    results.cchValue = lstrlenW(pVersion->sczVersion);
    results.wzValue = pVersion->sczVersion;

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineGetVariableVersion struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.cchValue);
    ExitOnFailure(hr, "Failed to write length of value of BAEngineGetVariableVersion struct.");

    hr = BuffWriteStringToBuffer(pBuffer, results.wzValue);
    ExitOnFailure(hr, "Failed to write value of BAEngineGetVariableVersion struct.");

LExit:
    ReleaseVerutilVersion(pVersion);
    ReleaseStr(sczVariable);

    return hr;
}

static HRESULT BAEngineGetRelatedBundleVariable(
    __in BAENGINE_CONTEXT* /* pContext */,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_GETRELATEDBUNDLEVARIABLE_ARGS args = { };
    BAENGINE_GETRELATEDBUNDLEVARIABLE_RESULTS results = { };
    LPWSTR sczBundleCode = NULL;
    LPWSTR sczVariable = NULL;
    LPWSTR sczValue = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetRelatedBundleVariable args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleCode);
    ExitOnFailure(hr, "Failed to read bundle code of BAEngineGetRelatedBundleVariable args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVariable);
    ExitOnFailure(hr, "Failed to read variable name of BAEngineGetRelatedBundleVariable args.");

    args.wzBundleCode = sczBundleCode;
    args.wzVariable = sczVariable;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetRelatedBundleVariable results.");

    hr = BuffReaderReadNumber(pReaderResults, &results.cchValue); // ignored, overwritten below.
    ExitOnFailure(hr, "Failed to read API version of BAEngineGetRelatedBundleVariable results.");

    // Execute.
    hr = BundleGetBundleVariable(args.wzBundleCode, args.wzVariable, &sczValue);
    ExitOnFailure(hr, "Failed to get related bundle variable: %ls", sczVariable);

    results.cchValue = lstrlenW(sczValue);
    results.wzValue = sczValue;

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineGetRelatedBundleVariable struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.cchValue);
    ExitOnFailure(hr, "Failed to write length of value of BAEngineGetRelatedBundleVariable struct.");

    hr = BuffWriteStringToBuffer(pBuffer, results.wzValue);
    ExitOnFailure(hr, "Failed to write value of BAEngineGetRelatedBundleVariable struct.");

LExit:
    ReleaseStr(sczValue);
    ReleaseStr(sczVariable);
    ReleaseStr(sczBundleCode);

    return hr;
}

static HRESULT BAEngineFormatString(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_FORMATSTRING_ARGS args = { };
    BAENGINE_FORMATSTRING_RESULTS results = { };
    LPWSTR sczIn = NULL;
    LPWSTR sczOut = NULL;
    SIZE_T cchOut = 0;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineFormatString args.");

    hr = BuffReaderReadString(pReaderArgs, &sczIn);
    ExitOnFailure(hr, "Failed to read string to format of BAEngineFormatString args.");

    args.wzIn = sczIn;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineFormatString results.");

    hr = BuffReaderReadNumber(pReaderResults, &results.cchOut); // ignored, overwritten below.
    ExitOnFailure(hr, "Failed to read allowed length of formatted string of BAEngineFormatString results.");

    // Execute.
    hr = VariableFormatString(&pContext->pEngineState->variables, args.wzIn, &sczOut, &cchOut);
    ExitOnFailure(hr, "Failed to format string");

    results.cchOut = (cchOut > DWORD_MAX) ? DWORD_MAX : static_cast<DWORD>(cchOut);
    results.wzOut = sczOut;

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineFormatString struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.cchOut);
    ExitOnFailure(hr, "Failed to write length of formatted string of BAEngineFormatString struct.");

    hr = BuffWriteStringToBuffer(pBuffer, results.wzOut);
    ExitOnFailure(hr, "Failed to write formatted string of BAEngineFormatString struct.");

LExit:
    ReleaseStr(sczOut);
    ReleaseStr(sczIn);

    return hr;
}

static HRESULT BAEngineEscapeString(
    __in BAENGINE_CONTEXT* /* pContext */,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_ESCAPESTRING_ARGS args = { };
    BAENGINE_ESCAPESTRING_RESULTS results = { };
    LPWSTR sczIn = NULL;
    LPWSTR sczOut = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineEscapeString args.");

    hr = BuffReaderReadString(pReaderArgs, &sczIn);
    ExitOnFailure(hr, "Failed to read string to escape of BAEngineEscapeString args.");

    args.wzIn = sczIn;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineEscapeString results.");

    hr = BuffReaderReadNumber(pReaderResults, &results.cchOut); // ignored, overwritten below.
    ExitOnFailure(hr, "Failed to read allowed length of escaped string of BAEngineEscapeString results.");

    // Execute.
    hr = VariableEscapeString(args.wzIn, &sczOut);
    ExitOnFailure(hr, "Failed to format string");

    results.cchOut = lstrlenW(sczOut);
    results.wzOut = sczOut;

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineEscapeString struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.cchOut);
    ExitOnFailure(hr, "Failed to write length of formatted string of BAEngineEscapeString struct.");

    hr = BuffWriteStringToBuffer(pBuffer, results.wzOut);
    ExitOnFailure(hr, "Failed to write formatted string of BAEngineEscapeString struct.");

LExit:
    ReleaseStr(sczOut);
    ReleaseStr(sczIn);

    return hr;
}

static HRESULT BAEngineEvaluateCondition(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_EVALUATECONDITION_ARGS args = { };
    BAENGINE_EVALUATECONDITION_RESULTS results = { };
    LPWSTR sczCondition = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineEvaluateCondition args.");

    hr = BuffReaderReadString(pReaderArgs, &sczCondition);
    ExitOnFailure(hr, "Failed to read condition of BAEngineEvaluateCondition args.");

    args.wzCondition = sczCondition;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineEvaluateCondition results.");

    // Execute.
    hr = ConditionEvaluate(&pContext->pEngineState->variables, args.wzCondition, &results.f);
    ExitOnFailure(hr, "Failed to evalute condition.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineEvaluateCondition struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.f);
    ExitOnFailure(hr, "Failed to result of BAEngineEvaluateCondition struct.");

LExit:
    ReleaseStr(sczCondition);

    return hr;
}

static HRESULT BAEngineLog(
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_LOG_ARGS args = { };
    BAENGINE_LOG_RESULTS results = { };
    LPWSTR sczMessage = NULL;
    REPORT_LEVEL rl = REPORT_NONE;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineLog args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.level));
    ExitOnFailure(hr, "Failed to read API version of BAEngineLog args.");

    hr = BuffReaderReadString(pReaderArgs, &sczMessage);
    ExitOnFailure(hr, "Failed to read variable name of BAEngineLog args.");

    switch (args.level)
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

    args.wzMessage = sczMessage;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineLog results.");

    // Execute.
    hr = ExternalEngineLog(rl, args.wzMessage);
    ExitOnFailure(hr, "Failed to log BA message.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineLog struct.");

LExit:
    ReleaseStr(sczMessage);
    return hr;
}

static HRESULT BAEngineSendEmbeddedError(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_SENDEMBEDDEDERROR_ARGS args = { };
    BAENGINE_SENDEMBEDDEDERROR_RESULTS results = { };
    LPWSTR sczMessage = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSendEmbeddedError args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwErrorCode);
    ExitOnFailure(hr, "Failed to read error code of BAEngineSendEmbeddedError args.");

    hr = BuffReaderReadString(pReaderArgs, &sczMessage);
    ExitOnFailure(hr, "Failed to read condition of BAEngineSendEmbeddedError args.");

    args.wzMessage = sczMessage;

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwUIHint);
    ExitOnFailure(hr, "Failed to read UI hint of BAEngineSendEmbeddedError args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSendEmbeddedError results.");

    // Execute.
    hr = ExternalEngineSendEmbeddedError(pContext->pEngineState, args.dwErrorCode, args.wzMessage, args.dwUIHint, &results.nResult);
    ExitOnFailure(hr, "Failed to send embedded error.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineSendEmbeddedError struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.nResult);
    ExitOnFailure(hr, "Failed to result of BAEngineSendEmbeddedError struct.");

LExit:
    ReleaseStr(sczMessage);
    return hr;
}

static HRESULT BAEngineSendEmbeddedProgress(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_SENDEMBEDDEDPROGRESS_ARGS args = { };
    BAENGINE_SENDEMBEDDEDPROGRESS_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSendEmbeddedProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwProgressPercentage);
    ExitOnFailure(hr, "Failed to read progress of BAEngineSendEmbeddedProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwOverallProgressPercentage);
    ExitOnFailure(hr, "Failed to read overall progress of BAEngineSendEmbeddedProgress args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSendEmbeddedProgress results.");

    // Execute.
    hr = ExternalEngineSendEmbeddedProgress(pContext->pEngineState, args.dwProgressPercentage, args.dwOverallProgressPercentage, &results.nResult);
    ExitOnFailure(hr, "Failed to send embedded error.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineSendEmbeddedProgress struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.nResult);
    ExitOnFailure(hr, "Failed to result of BAEngineSendEmbeddedProgress struct.");

LExit:
    return hr;
}

static HRESULT BAEngineSetUpdate(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_SETUPDATE_ARGS args = { };
    BAENGINE_SETUPDATE_RESULTS results = { };
    LPWSTR sczLocalSource = NULL;
    LPWSTR sczDownloadSource = NULL;
    LPWSTR sczHash = NULL;
    LPWSTR sczUpdatePackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetUpdate args.");

    hr = BuffReaderReadString(pReaderArgs, &sczLocalSource);
    ExitOnFailure(hr, "Failed to read local source of BAEngineSetUpdate args.");

    args.wzLocalSource = sczLocalSource;

    hr = BuffReaderReadString(pReaderArgs, &sczDownloadSource);
    ExitOnFailure(hr, "Failed to read download source of BAEngineSetUpdate args.");

    args.wzDownloadSource = sczDownloadSource;

    hr = BuffReaderReadNumber64(pReaderArgs, &args.qwSize);
    ExitOnFailure(hr, "Failed to read update size of BAEngineSetUpdate args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hashType));
    ExitOnFailure(hr, "Failed to read hash type of BAEngineSetUpdate args.");

    hr = BuffReaderReadString(pReaderArgs, &sczHash);
    ExitOnFailure(hr, "Failed to read hash of BAEngineSetUpdate args.");

    args.wzHash = sczHash;

    hr = BuffReaderReadString(pReaderArgs, &sczUpdatePackageId);
    ExitOnFailure(hr, "Failed to read update package id of BAEngineSetUpdate args.");

    args.wzUpdatePackageId = sczUpdatePackageId;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetUpdate results.");

    // Execute.
    hr = ExternalEngineSetUpdate(pContext->pEngineState, args.wzLocalSource, args.wzDownloadSource, args.qwSize, args.hashType, args.wzHash, args.wzUpdatePackageId);
    ExitOnFailure(hr, "Failed to set update.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineSetUpdate struct.");

LExit:
    ReleaseStr(sczUpdatePackageId);
    ReleaseStr(sczHash);
    ReleaseStr(sczDownloadSource);
    ReleaseStr(sczLocalSource);
    return hr;
}

static HRESULT BAEngineSetLocalSource(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_SETLOCALSOURCE_ARGS args = { };
    BAENGINE_SETLOCALSOURCE_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;
    LPWSTR sczPath = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetLocalSource args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of BAEngineSetLocalSource args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of BAEngineSetLocalSource args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadString(pReaderArgs, &sczPath);
    ExitOnFailure(hr, "Failed to read path of BAEngineSetLocalSource args.");

    args.wzPath = sczPath;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetLocalSource results.");

    // Execute.
    hr = ExternalEngineSetLocalSource(pContext->pEngineState, args.wzPackageOrContainerId, args.wzPayloadId, args.wzPath);
    ExitOnFailure(hr, "Failed to set local source.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineSetLocalSource struct.");

LExit:
    ReleaseStr(sczPath);
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT BAEngineSetDownloadSource(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_SETDOWNLOADSOURCE_ARGS args = { };
    BAENGINE_SETDOWNLOADSOURCE_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;
    LPWSTR sczUrl = NULL;
    LPWSTR sczUser = NULL;
    LPWSTR sczPassword = NULL;
    LPWSTR sczAuthorizationHeader = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetDownloadSource args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of BAEngineSetDownloadSource args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of BAEngineSetDownloadSource args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadString(pReaderArgs, &sczUrl);
    ExitOnFailure(hr, "Failed to read url of BAEngineSetDownloadSource args.");

    args.wzUrl = sczUrl;

    hr = BuffReaderReadString(pReaderArgs, &sczUser);
    ExitOnFailure(hr, "Failed to read user of BAEngineSetDownloadSource args.");

    args.wzUser = sczUser;

    hr = BuffReaderReadString(pReaderArgs, &sczPassword);
    ExitOnFailure(hr, "Failed to read password of BAEngineSetDownloadSource args.");

    args.wzPassword = sczPassword;

    hr = BuffReaderReadString(pReaderArgs, &sczAuthorizationHeader);
    ExitOnFailure(hr, "Failed to read authorization header of BAEngineSetDownloadSource args.");

    args.wzAuthorizationHeader = sczAuthorizationHeader;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetDownloadSource results.");

    // Execute.
    hr = ExternalEngineSetDownloadSource(pContext->pEngineState, args.wzPackageOrContainerId, args.wzPayloadId, args.wzUrl, args.wzUser, args.wzPassword, args.wzAuthorizationHeader);
    ExitOnFailure(hr, "Failed to set download source.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineSetDownloadSource struct.");

LExit:
    ReleaseStr(sczAuthorizationHeader);
    ReleaseStr(sczPassword);
    ReleaseStr(sczUser);
    ReleaseStr(sczUrl);
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}


static HRESULT BAEngineSetVariableNumeric(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_SETVARIABLENUMERIC_ARGS args = { };
    BAENGINE_SETVARIABLENUMERIC_RESULTS results = { };
    LPWSTR sczVariable = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetVariableNumeric args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVariable);
    ExitOnFailure(hr, "Failed to read variable of BAEngineSetVariableNumeric args.");

    args.wzVariable = sczVariable;

    hr = BuffReaderReadNumber64(pReaderArgs, reinterpret_cast<DWORD64*>(&args.llValue));
    ExitOnFailure(hr, "Failed to read formatted flag of BAEngineSetVariableNumeric results.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetVariableNumeric results.");

    // Execute.
    hr = ExternalEngineSetVariableNumeric(pContext->pEngineState, args.wzVariable, args.llValue);
    ExitOnFailure(hr, "Failed to set numeric variable.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineSetVariableNumeric struct.");

LExit:
    ReleaseStr(sczVariable);
    return hr;
}

static HRESULT BAEngineSetVariableString(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_SETVARIABLESTRING_ARGS args = { };
    BAENGINE_SETVARIABLESTRING_RESULTS results = { };
    LPWSTR sczVariable = NULL;
    LPWSTR sczValue = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetVariableString args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVariable);
    ExitOnFailure(hr, "Failed to read variable of BAEngineSetVariableString args.");

    args.wzVariable = sczVariable;

    hr = BuffReaderReadString(pReaderArgs, &sczValue);
    ExitOnFailure(hr, "Failed to read value of BAEngineSetVariableString args.");

    args.wzValue = sczValue;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fFormatted));
    ExitOnFailure(hr, "Failed to read formatted flag of BAEngineSetVariableString results.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetVariableString results.");

    // Execute.
    hr = ExternalEngineSetVariableString(pContext->pEngineState, args.wzVariable, args.wzValue, args.fFormatted);
    ExitOnFailure(hr, "Failed to set string variable.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineSetVariableString struct.");

LExit:
    ReleaseStr(sczValue);
    ReleaseStr(sczVariable);
    return hr;
}

static HRESULT BAEngineSetVariableVersion(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_SETVARIABLEVERSION_ARGS args = { };
    BAENGINE_SETVARIABLEVERSION_RESULTS results = { };
    LPWSTR sczVariable = NULL;
    LPWSTR sczValue = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetVariableVersion args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVariable);
    ExitOnFailure(hr, "Failed to read variable of BAEngineSetVariableVersion args.");

    args.wzVariable = sczVariable;

    hr = BuffReaderReadString(pReaderArgs, &sczValue);
    ExitOnFailure(hr, "Failed to read value of BAEngineSetVariableVersion args.");

    args.wzValue = sczValue;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetVariableVersion results.");

    // Execute.
    hr = ExternalEngineSetVariableVersion(pContext->pEngineState, args.wzVariable, args.wzValue);
    ExitOnFailure(hr, "Failed to set variable version.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineSetVariableVersion struct.");

LExit:
    ReleaseStr(sczValue);
    ReleaseStr(sczVariable);
    return hr;
}

static HRESULT BAEngineCloseSplashScreen(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_CLOSESPLASHSCREEN_ARGS args = { };
    BAENGINE_CLOSESPLASHSCREEN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineCloseSplashScreen args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineCloseSplashScreen results.");

    // Execute.
    ExternalEngineCloseSplashScreen(pContext->pEngineState);

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineCloseSplashScreen struct.");

LExit:
    return hr;
}

static HRESULT BAEngineCompareVersions(
    __in BAENGINE_CONTEXT* /* pContext */,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_COMPAREVERSIONS_ARGS args = { };
    BAENGINE_COMPAREVERSIONS_RESULTS results = { };
    LPWSTR sczVersion1 = NULL;
    LPWSTR sczVersion2 = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineCompareVersions args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVersion1);
    ExitOnFailure(hr, "Failed to read first input of BAEngineCompareVersions args.");

    args.wzVersion1 = sczVersion1;

    hr = BuffReaderReadString(pReaderArgs, &sczVersion2);
    ExitOnFailure(hr, "Failed to read second input of BAEngineCompareVersions args.");

    args.wzVersion2 = sczVersion2;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineCompareVersions results.");

    // Execute.
    hr = ExternalEngineCompareVersions(args.wzVersion1, args.wzVersion2, &results.nResult);
    ExitOnFailure(hr, "Failed to compare versions.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineCompareVersions struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.nResult);
    ExitOnFailure(hr, "Failed to result of BAEngineCompareVersions struct.");

LExit:
    ReleaseStr(sczVersion2);
    ReleaseStr(sczVersion1);

    return hr;
}

static HRESULT BAEngineDetect(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_DETECT_ARGS args = { };
    BAENGINE_DETECT_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineDetect args.");

    hr = BuffReaderReadNumber64(pReaderArgs, &args.hwndParent);
    ExitOnFailure(hr, "Failed to read parent window of BAEngineDetect args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineDetect results.");

    // Execute.
    hr = ExternalEngineDetect(pContext, reinterpret_cast<HWND>(args.hwndParent));
    ExitOnFailure(hr, "Failed to detect in the engine.");

    // Pack result.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineDetect struct.");

LExit:
    return hr;
}

static HRESULT BAEnginePlan(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_PLAN_ARGS args = { };
    BAENGINE_PLAN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEnginePlan args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.action));
    ExitOnFailure(hr, "Failed to read plan action of BAEnginePlan args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.plannedScope));
    ExitOnFailure(hr, "Failed to read plan scope of BAEnginePlan args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEnginePlan results.");

    // Execute.
    hr = ExternalEnginePlan(pContext, args.action, args.plannedScope);
    ExitOnFailure(hr, "Failed to plan in the engine.");

    // Pack result.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEnginePlan struct.");

LExit:
    return hr;
}

static HRESULT BAEngineElevate(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_ELEVATE_ARGS args = { };
    BAENGINE_ELEVATE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineElevate args.");

    hr = BuffReaderReadNumber64(pReaderArgs, &args.hwndParent);
    ExitOnFailure(hr, "Failed to read parent window of BAEngineElevate args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineElevate results.");

    // Execute.
    hr = ExternalEngineElevate(pContext, reinterpret_cast<HWND>(args.hwndParent));
    ExitOnFailure(hr, "Failed to detect in the engine.");

    // Pack result.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineElevate struct.");

LExit:
    return hr;
}

static HRESULT BAEngineApply(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_APPLY_ARGS args = { };
    BAENGINE_APPLY_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineApply args.");

    hr = BuffReaderReadNumber64(pReaderArgs, &args.hwndParent);
    ExitOnFailure(hr, "Failed to read parent window of BAEngineApply args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineApply results.");

    // Execute.
    hr = ExternalEngineApply(pContext, reinterpret_cast<HWND>(args.hwndParent));
    ExitOnFailure(hr, "Failed to detect in the engine.");

    // Pack result.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineApply struct.");

LExit:
    return hr;
}

static HRESULT BAEngineQuit(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_QUIT_ARGS args = { };
    BAENGINE_QUIT_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineQuit args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwExitCode);
    ExitOnFailure(hr, "Failed to read API version of BAEngineQuit args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineQuit results.");

    // Execute.
    hr = ExternalEngineQuit(pContext, args.dwExitCode);
    ExitOnFailure(hr, "Failed to quit the engine.");

    // Pack result.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineQuit struct.");

LExit:
    return hr;
}

static HRESULT BAEngineLaunchApprovedExe(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_LAUNCHAPPROVEDEXE_ARGS args = { };
    BAENGINE_LAUNCHAPPROVEDEXE_RESULTS results = { };
    LPWSTR sczApprovedExeForElevationId = NULL;
    LPWSTR sczArguments = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineLaunchApprovedExe args.");

    hr = BuffReaderReadNumber64(pReaderArgs, &args.hwndParent);
    ExitOnFailure(hr, "Failed to read parent window of BAEngineLaunchApprovedExe args.");

    hr = BuffReaderReadString(pReaderArgs, &sczApprovedExeForElevationId);
    ExitOnFailure(hr, "Failed to read approved exe elevation id of BAEngineLaunchApprovedExe args.");

    args.wzApprovedExeForElevationId = sczApprovedExeForElevationId;

    hr = BuffReaderReadString(pReaderArgs, &sczArguments);
    ExitOnFailure(hr, "Failed to read arguments of BAEngineLaunchApprovedExe args.");

    args.wzArguments = sczArguments;

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwWaitForInputIdleTimeout);
    ExitOnFailure(hr, "Failed to read wait for idle input timeout of BAEngineLaunchApprovedExe args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineLaunchApprovedExe results.");

    // Execute.
    hr = ExternalEngineLaunchApprovedExe(pContext, reinterpret_cast<HWND>(args.hwndParent), args.wzApprovedExeForElevationId, args.wzArguments, args.dwWaitForInputIdleTimeout);
    ExitOnFailure(hr, "Failed to quit the engine.");

    // Pack result.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineLaunchApprovedExe struct.");

LExit:
    ReleaseStr(sczArguments);
    ReleaseStr(sczApprovedExeForElevationId);
    return hr;
}

static HRESULT BAEngineSetUpdateSource(
    __in BAENGINE_CONTEXT* pContext,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BAENGINE_SETUPDATESOURCE_ARGS args = { };
    BAENGINE_SETUPDATESOURCE_RESULTS results = { };
    LPWSTR sczUrl = NULL;
    LPWSTR sczAuthorizationHeader = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetUpdateSource args.");

    hr = BuffReaderReadString(pReaderArgs, &sczUrl);
    ExitOnFailure(hr, "Failed to read url of BAEngineSetUpdateSource args.");

    args.wzUrl = sczUrl;

    hr = BuffReaderReadString(pReaderArgs, &sczAuthorizationHeader);
    ExitOnFailure(hr, "Failed to read authorization header of BAEngineSetUpdateSource args.");

    args.wzAuthorizationHeader = sczAuthorizationHeader;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of BAEngineSetUpdateSource results.");

    // Execute.
    hr = ExternalEngineSetUpdateSource(pContext->pEngineState, args.wzUrl, args.wzAuthorizationHeader);
    ExitOnFailure(hr, "Failed to set update source in the engine.");

    // Pack result.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BAEngineSetUpdateSource struct.");

LExit:
    ReleaseStr(sczAuthorizationHeader);
    ReleaseStr(sczUrl);

    return hr;
}

static HRESULT ParseArgsAndResults(
    __in_bcount(cbData) LPCBYTE pbData,
    __in SIZE_T cbData,
    __in BUFF_READER* pBufferArgs,
    __in BUFF_READER* pBufferResults
)
{
    HRESULT hr = S_OK;
    SIZE_T iData = 0;
    DWORD dw = 0;

    // Get the args reader size and point to the data just after the size.
    hr = BuffReadNumber(pbData, cbData, &iData, &dw);
    ExitOnFailure(hr, "Failed to parse size of args");

    pBufferArgs->pbData = pbData + iData;
    pBufferArgs->cbData = dw;
    pBufferArgs->iBuffer = 0;

    // Get the results reader size and point to the data just after the size.
    hr = ::SIZETAdd(iData, dw, &iData);
    ExitOnFailure(hr, "Failed to advance index beyond args");

    hr = BuffReadNumber(pbData, cbData, &iData, &dw);
    ExitOnFailure(hr, "Failed to parse size of results");

    pBufferResults->pbData = pbData + iData;
    pBufferResults->cbData = dw;
    pBufferResults->iBuffer = 0;

LExit:
    return hr;
}

HRESULT WINAPI EngineForApplicationProc(
    __in BAENGINE_CONTEXT* pContext,
    __in BOOTSTRAPPER_ENGINE_MESSAGE message,
    __in_bcount(cbData) LPCBYTE pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    BUFF_READER readerArgs = { };
    BUFF_READER readerResults = { };
    BUFF_BUFFER bufferResponse = { };

    hr = ParseArgsAndResults(pbData, cbData, &readerArgs, &readerResults);
    if (SUCCEEDED(hr))
    {
        switch (message)
        {
        case BOOTSTRAPPER_ENGINE_MESSAGE_GETPACKAGECOUNT:
            hr = BAEngineGetPackageCount(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLENUMERIC:
            hr = BAEngineGetVariableNumeric(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLESTRING:
            hr = BAEngineGetVariableString(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLEVERSION:
            hr = BAEngineGetVariableVersion(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_FORMATSTRING:
            hr = BAEngineFormatString(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_ESCAPESTRING:
            hr = BAEngineEscapeString(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_EVALUATECONDITION:
            hr = BAEngineEvaluateCondition(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_LOG:
            hr = BAEngineLog(&readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDERROR:
            hr = BAEngineSendEmbeddedError(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDPROGRESS:
            hr = BAEngineSendEmbeddedProgress(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_SETUPDATE:
            hr = BAEngineSetUpdate(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_SETLOCALSOURCE:
            hr = BAEngineSetLocalSource(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_SETDOWNLOADSOURCE:
            hr = BAEngineSetDownloadSource(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLENUMERIC:
            hr = BAEngineSetVariableNumeric(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLESTRING:
            hr = BAEngineSetVariableString(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLEVERSION:
            hr = BAEngineSetVariableVersion(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_CLOSESPLASHSCREEN:
            hr = BAEngineCloseSplashScreen(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_DETECT:
            hr = BAEngineDetect(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_PLAN:
            hr = BAEnginePlan(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_ELEVATE:
            hr = BAEngineElevate(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_APPLY:
            hr = BAEngineApply(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_QUIT:
            hr = BAEngineQuit(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_LAUNCHAPPROVEDEXE:
            hr = BAEngineLaunchApprovedExe(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_SETUPDATESOURCE:
            hr = BAEngineSetUpdateSource(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_COMPAREVERSIONS:
            hr = BAEngineCompareVersions(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        case BOOTSTRAPPER_ENGINE_MESSAGE_GETRELATEDBUNDLEVARIABLE:
            hr = BAEngineGetRelatedBundleVariable(pContext, &readerArgs, &readerResults, &bufferResponse);
            break;
        default:
            hr = E_NOTIMPL;
            break;
        }
    }

    hr = PipeRpcResponse(&pContext->hRpcPipe, message, hr, bufferResponse.pbData, bufferResponse.cbData);
    ExitOnFailure(hr, "Failed to send engine result to bootstrapper application.");

LExit:
    ReleaseBuffer(bufferResponse);
    return hr;
}

static DWORD WINAPI BAEngineMessagePumpThreadProc(
    __in LPVOID lpThreadParameter
)
{
    HRESULT hr = S_OK;
    BOOL fComInitialized = FALSE;
    BAENGINE_CONTEXT* pContext = reinterpret_cast<BAENGINE_CONTEXT*>(lpThreadParameter);
    PIPE_MESSAGE msg = { };

    // initialize COM
    hr = ::CoInitializeEx(NULL, COINIT_MULTITHREADED);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    // Pump messages from bootstrapper application for engine messages until the pipe is closed.
    while (S_OK == (hr = PipeRpcReadMessage(&pContext->hRpcPipe, &msg)))
    {
        EngineForApplicationProc(pContext, static_cast<BOOTSTRAPPER_ENGINE_MESSAGE>(msg.dwMessageType), reinterpret_cast<LPCBYTE>(msg.pvData), msg.cbData);

        ReleasePipeMessage(&msg);
    }
    ExitOnFailure(hr, "Failed to get message over bootstrapper application pipe");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleasePipeMessage(&msg);

    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return (DWORD)hr;
}
