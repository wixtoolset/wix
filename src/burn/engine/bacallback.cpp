// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// internal function declarations

static HRESULT FilterExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOL fRollback,
    __in BOOL fCancel,
    __in LPCWSTR sczEventName
    );
static HRESULT SendBAMessage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in BUFF_BUFFER* pBufferArgs,
    __in BUFF_BUFFER* pBufferResults,
    __in PIPE_RPC_RESULT* pResult
    );
static HRESULT SendBAMessageFromInactiveEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in BUFF_BUFFER* pBufferArgs,
    __in BUFF_BUFFER* pBufferResults,
    __in PIPE_RPC_RESULT* pResult
    );
static HRESULT CombineArgsAndResults(
    __in BUFF_BUFFER* pBufferArgs,
    __in BUFF_BUFFER* pBufferResults,
    __in BUFF_BUFFER* pBufferCombined
    );

// function definitions

EXTERN_C HRESULT BACallbackOnApplyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwPhaseCount
    )
{
    HRESULT hr = S_OK;
    BA_ONAPPLYBEGIN_ARGS args = { };
    BA_ONAPPLYBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.dwPhaseCount = dwPhaseCount;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnApplyBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwPhaseCount);
    ExitOnFailure(hr, "Failed to write phase count of OnApplyBegin args command.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnApplyBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnApplyBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnApplyBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnApplyBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnApplyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONAPPLYCOMPLETE_ARGS args = { };
    BA_ONAPPLYCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;
    args.restart = restart;
    args.recommendation = *pAction;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pAction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnApplyComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnApplyComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.restart);
    ExitOnFailure(hr, "Failed to write restart of OnApplyComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommended action of OnApplyComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnApplyComplete results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write default action of OnApplyComplete results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnApplyComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnApplyComplete result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnApplyComplete result.");

    *pAction = results.action;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnApplyDowngrade(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout HRESULT* phrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONAPPLYDOWNGRADE_ARGS args = { };
    BA_ONAPPLYDOWNGRADE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrRecommended = *phrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.hrStatus = *phrStatus;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnApplyDowngrade args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrRecommended);
    ExitOnFailure(hr, "Failed to write recommended status of OnApplyDowngrade args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnApplyDowngrade results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.hrStatus);
    ExitOnFailure(hr, "Failed to write default action of OnApplyDowngrade results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYDOWNGRADE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnApplyDowngrade failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnApplyDowngrade result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.hrStatus));
    ExitOnFailure(hr, "Failed to read action of OnApplyDowngrade result.");

    *phrStatus = results.hrStatus;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnBeginMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    )
{
    HRESULT hr = S_OK;
    BA_ONBEGINMSITRANSACTIONBEGIN_ARGS args = { };
    BA_ONBEGINMSITRANSACTIONBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzTransactionId = wzTransactionId;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnBeginMsiTransactionBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzTransactionId);
    ExitOnFailure(hr, "Failed to write recommended status of OnBeginMsiTransactionBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnBeginMsiTransactionBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnBeginMsiTransactionBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnBeginMsiTransactionBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read action of OnBeginMsiTransactionBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnBeginMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONBEGINMSITRANSACTIONCOMPLETE_ARGS args = { };
    BA_ONBEGINMSITRANSACTIONCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzTransactionId = wzTransactionId;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnBeginMsiTransactionComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzTransactionId);
    ExitOnFailure(hr, "Failed to write recommended status of OnBeginMsiTransactionComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnBeginMsiTransactionComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnBeginMsiTransactionComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnBeginMsiTransactionComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheAcquireBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z LPWSTR* pwzSource,
    __in_z LPWSTR* pwzDownloadUrl,
    __in_z_opt LPCWSTR wzPayloadContainerId,
    __out BOOTSTRAPPER_CACHE_OPERATION* pCacheOperation
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEACQUIREBEGIN_ARGS args = { };
    BA_ONCACHEACQUIREBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    *pCacheOperation = BOOTSTRAPPER_CACHE_OPERATION_NONE;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.wzSource = *pwzSource;
    args.wzDownloadUrl = *pwzDownloadUrl;
    args.wzPayloadContainerId = wzPayloadContainerId;
    args.recommendation = *pCacheOperation;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pCacheOperation;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheAcquireBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container of OnCacheAcquireBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheAcquireBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzSource);
    ExitOnFailure(hr, "Failed to write source of OnCacheAcquireBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzDownloadUrl);
    ExitOnFailure(hr, "Failed to write download url of OnCacheAcquireBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadContainerId);
    ExitOnFailure(hr, "Failed to write payload container id of OnCacheAcquireBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnCacheAcquireBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheAcquireBegin results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCacheAcquireBegin results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheAcquireBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheAcquireBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCacheAcquireBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCacheAcquireBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    else
    {
        // Verify the BA requested an action that is possible.
        if (BOOTSTRAPPER_CACHE_OPERATION_DOWNLOAD == results.action && *pwzDownloadUrl && **pwzDownloadUrl ||
            BOOTSTRAPPER_CACHE_OPERATION_EXTRACT == results.action && wzPayloadContainerId ||
            BOOTSTRAPPER_CACHE_OPERATION_COPY == results.action ||
            BOOTSTRAPPER_CACHE_OPERATION_NONE == results.action)
        {
            *pCacheOperation = results.action;
        }
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheAcquireComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus,
    __inout BOOL* pfRetry
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEACQUIRECOMPLETE_ARGS args = { };
    BA_ONCACHEACQUIRECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.hrStatus = hrStatus;
    args.recommendation = *pfRetry ? BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION_RETRY : BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION_NONE;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = args.recommendation;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheAcquireComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container of OnCacheAcquireComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheAcquireComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnCacheAcquireComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnCacheAcquireComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheAcquireComplete results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCacheAcquireComplete results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheAcquireComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheAcquireComplete result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCacheAcquireComplete result.");

    if (FAILED(hrStatus))
    {
        *pfRetry = BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION_RETRY == results.action;
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C BAAPI BACallbackOnCacheAcquireProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEACQUIREPROGRESS_ARGS args = { };
    BA_ONCACHEACQUIREPROGRESS_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.dw64Progress = dw64Progress;
    args.dw64Total = dw64Total;
    args.dwOverallPercentage = dwOverallPercentage;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheAcquireProgress args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container of OnCacheAcquireProgress args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheAcquireProgress args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64Progress);
    ExitOnFailure(hr, "Failed to write progress of OnCacheAcquireProgress args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64Total);
    ExitOnFailure(hr, "Failed to write total progress of OnCacheAcquireProgress args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to write overall percentage of OnCacheAcquireProgress args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheAcquireProgress results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREPROGRESS, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheAcquireProgress failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheAcquireProgress result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCacheAcquireProgress result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheAcquireResolving(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_ecount_z(cSearchPaths) LPWSTR* rgSearchPaths,
    __in DWORD cSearchPaths,
    __in BOOL fFoundLocal,
    __in DWORD* pdwChosenSearchPath,
    __in_z_opt LPWSTR* pwzDownloadUrl,
    __in_z_opt LPCWSTR wzPayloadContainerId,
    __inout BOOTSTRAPPER_CACHE_RESOLVE_OPERATION* pCacheOperation
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEACQUIRERESOLVING_ARGS args = { };
    BA_ONCACHEACQUIRERESOLVING_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.rgSearchPaths = const_cast<LPCWSTR*>(rgSearchPaths);
    args.cSearchPaths = cSearchPaths;
    args.fFoundLocal = fFoundLocal;
    args.dwRecommendedSearchPath = *pdwChosenSearchPath;
    args.wzDownloadUrl = *pwzDownloadUrl;
    args.recommendation = *pCacheOperation;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.dwChosenSearchPath = *pdwChosenSearchPath;
    results.action = *pCacheOperation;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheAcquireResolving args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container of OnCacheAcquireResolving args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheAcquireResolving args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.cSearchPaths);
    ExitOnFailure(hr, "Failed to write count of search paths of OnCacheAcquireResolving args.");

    for (DWORD i = 0; i < args.cSearchPaths; ++i)
    {
        hr = BuffWriteStringToBuffer(&bufferArgs, args.rgSearchPaths[i]);
        ExitOnFailure(hr, "Failed to write search path[%u] of OnCacheAcquireResolving args.", i);
    }

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fFoundLocal);
    ExitOnFailure(hr, "Failed to write found local of OnCacheAcquireResolving args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwRecommendedSearchPath);
    ExitOnFailure(hr, "Failed to write recommended search path of OnCacheAcquireResolving args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzDownloadUrl);
    ExitOnFailure(hr, "Failed to write download url of OnCacheAcquireResolving args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadContainerId);
    ExitOnFailure(hr, "Failed to write payload container id of OnCacheAcquireResolving args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnCacheAcquireResolving args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheAcquireResolving results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwChosenSearchPath);
    ExitOnFailure(hr, "Failed to write chose search path of OnCacheAcquireResolving results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCacheAcquireResolving results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRERESOLVING, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheAcquireResolving failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheAcquireResolving result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwChosenSearchPath);
    ExitOnFailure(hr, "Failed to read chosen search path of OnCacheAcquireResolving result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCacheAcquireResolving result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCacheAcquireResolving result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    else
    {
        // Verify the BA requested an action that is possible.
        if (BOOTSTRAPPER_CACHE_RESOLVE_DOWNLOAD == results.action && *pwzDownloadUrl && **pwzDownloadUrl ||
            BOOTSTRAPPER_CACHE_RESOLVE_CONTAINER == results.action && wzPayloadContainerId ||
            BOOTSTRAPPER_CACHE_RESOLVE_RETRY == results.action ||
            BOOTSTRAPPER_CACHE_RESOLVE_NONE == results.action)
        {
            *pCacheOperation = results.action;
        }
        else if (BOOTSTRAPPER_CACHE_RESOLVE_LOCAL == results.action && results.dwChosenSearchPath < cSearchPaths)
        {
            *pdwChosenSearchPath = results.dwChosenSearchPath;
            *pCacheOperation = results.action;
        }
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEBEGIN_ARGS args = { };
    BA_ONCACHEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCacheBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECOMPLETE_ARGS args = { };
    BA_ONCACHECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnCacheComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C BAAPI BACallbackOnCacheContainerOrPayloadVerifyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_ARGS args = { };
    BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheContainerOrPayloadVerifyBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container id of OnCacheContainerOrPayloadVerifyBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheContainerOrPayloadVerifyBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheContainerOrPayloadVerifyBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheContainerOrPayloadVerifyBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheContainerOrPayloadVerifyBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCacheContainerOrPayloadVerifyBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheContainerOrPayloadVerifyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_ARGS args = { };
    BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheContainerOrPayloadVerifyComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container id of OnCacheContainerOrPayloadVerifyComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheContainerOrPayloadVerifyComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnCacheContainerOrPayloadVerifyComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheContainerOrPayloadVerifyComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheContainerOrPayloadVerifyComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheContainerOrPayloadVerifyProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_ARGS args = { };
    BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.dw64Progress = dw64Progress;
    args.dw64Total = dw64Total;
    args.dwOverallPercentage = dwOverallPercentage;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheContainerOrPayloadVerifyProgress args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container id of OnCacheContainerOrPayloadVerifyProgress args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheContainerOrPayloadVerifyProgress args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64Progress);
    ExitOnFailure(hr, "Failed to write progress of OnCacheContainerOrPayloadVerifyProgress args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64Total);
    ExitOnFailure(hr, "Failed to write total progress of OnCacheContainerOrPayloadVerifyProgress args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to write overall percentage of OnCacheContainerOrPayloadVerifyProgress args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheContainerOrPayloadVerifyProgress results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheContainerOrPayloadVerifyProgress failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheContainerOrPayloadVerifyProgress result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCacheContainerOrPayloadVerifyProgress result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCachePackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cCachePayloads,
    __in DWORD64 dw64PackageCacheSize,
    __in BOOL fVital
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPACKAGEBEGIN_ARGS args = { };
    BA_ONCACHEPACKAGEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.cCachePayloads = cCachePayloads;
    args.dw64PackageCacheSize = dw64PackageCacheSize;
    args.fVital = fVital;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePackageBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnCachePackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.cCachePayloads);
    ExitOnFailure(hr, "Failed to write count of cached payloads of OnCachePackageBegin args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64PackageCacheSize);
    ExitOnFailure(hr, "Failed to write package cache size of OnCachePackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fVital);
    ExitOnFailure(hr, "Failed to write vital of OnCachePackageBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePackageBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCachePackageBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCachePackageBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCachePackageBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCachePackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPACKAGECOMPLETE_ARGS args = { };
    BA_ONCACHEPACKAGECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.recommendation = *pAction;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pAction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePackageComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnCachePackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnCachePackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnCachePackageComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePackageComplete results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCachePackageComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCachePackageComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCachePackageComplete result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read cancel of OnCachePackageComplete result.");

    if (FAILED(hrStatus))
    {
        *pAction = results.action;
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCachePackageNonVitalValidationFailure(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_ARGS args = { };
    BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.recommendation = *pAction;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pAction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePackageNonVitalValidationFailure args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnCachePackageNonVitalValidationFailure args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnCachePackageNonVitalValidationFailure args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnCachePackageNonVitalValidationFailure args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePackageNonVitalValidationFailure results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write API version of OnCachePackageNonVitalValidationFailure results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGENONVITALVALIDATIONFAILURE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCachePackageNonVitalValidationFailure failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCachePackageNonVitalValidationFailure result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read cancel of OnCachePackageNonVitalValidationFailure result.");

    switch (results.action)
    {
    case BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION_NONE: __fallthrough;
    case BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION_ACQUIRE:
        *pAction = results.action;
        break;
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCachePayloadExtractBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPAYLOADEXTRACTBEGIN_ARGS args = { };
    BA_ONCACHEPAYLOADEXTRACTBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzContainerId = wzContainerId;
    args.wzPayloadId = wzPayloadId;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePayloadExtractBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzContainerId);
    ExitOnFailure(hr, "Failed to write container id of OnCachePayloadExtractBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCachePayloadExtractBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePayloadExtractBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCachePayloadExtractBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCachePayloadExtractBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCachePayloadExtractBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCachePayloadExtractComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPAYLOADEXTRACTCOMPLETE_ARGS args = { };
    BA_ONCACHEPAYLOADEXTRACTCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzContainerId = wzContainerId;
    args.wzPayloadId = wzPayloadId;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePayloadExtractComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzContainerId);
    ExitOnFailure(hr, "Failed to write container id of OnCachePayloadExtractComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCachePayloadExtractComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnCachePayloadExtractComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePayloadExtractComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCachePayloadExtractComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCachePayloadExtractProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPAYLOADEXTRACTPROGRESS_ARGS args = { };
    BA_ONCACHEPAYLOADEXTRACTPROGRESS_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzContainerId = wzContainerId;
    args.wzPayloadId = wzPayloadId;
    args.dw64Progress = dw64Progress;
    args.dw64Total = dw64Total;
    args.dwOverallPercentage = dwOverallPercentage;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePayloadExtractProgress args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzContainerId);
    ExitOnFailure(hr, "Failed to write container id of OnCachePayloadExtractProgress args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCachePayloadExtractProgress args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64Progress);
    ExitOnFailure(hr, "Failed to write progress of OnCachePayloadExtractProgress args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64Total);
    ExitOnFailure(hr, "Failed to write total progress of OnCachePayloadExtractProgress args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to write overall percentage of OnCachePayloadExtractProgress args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCachePayloadExtractProgress results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTPROGRESS, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCachePayloadExtractProgress failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCachePayloadExtractProgress result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCachePayloadExtractProgress result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheVerifyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEVERIFYBEGIN_ARGS args = { };
    BA_ONCACHEVERIFYBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheVerifyBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container id of OnCacheVerifyBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheVerifyBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheVerifyBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheVerifyBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheVerifyBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCacheVerifyBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheVerifyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEVERIFYCOMPLETE_ARGS args = { };
    BA_ONCACHEVERIFYCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.hrStatus = hrStatus;
    args.recommendation = *pAction;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pAction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheVerifyComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container id of OnCacheVerifyComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheVerifyComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnCacheVerifyComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnCacheVerifyComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheVerifyComplete results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCacheVerifyComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheVerifyComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheVerifyComplete result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCacheVerifyComplete result.");

    if (FAILED(hrStatus))
    {
        *pAction = results.action;
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCacheVerifyProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage,
    __in BOOTSTRAPPER_CACHE_VERIFY_STEP verifyStep
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEVERIFYPROGRESS_ARGS args = { };
    BA_ONCACHEVERIFYPROGRESS_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.dw64Progress = dw64Progress;
    args.dw64Total = dw64Total;
    args.dwOverallPercentage = dwOverallPercentage;
    args.verifyStep = verifyStep;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheVerifyProgress args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
    ExitOnFailure(hr, "Failed to write package or container id of OnCacheVerifyProgress args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
    ExitOnFailure(hr, "Failed to write payload id of OnCacheVerifyProgress args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64Progress);
    ExitOnFailure(hr, "Failed to write progress of OnCacheVerifyProgress args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64Total);
    ExitOnFailure(hr, "Failed to write total progress of OnCacheVerifyProgress args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to write overall percentage of OnCacheVerifyProgress args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.verifyStep);
    ExitOnFailure(hr, "Failed to write verify step of OnCacheVerifyProgress args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCacheVerifyProgress results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYPROGRESS, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCacheVerifyProgress failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCacheVerifyProgress result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCacheVerifyProgress result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCommitMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    )
{
    HRESULT hr = S_OK;
    BA_ONCOMMITMSITRANSACTIONBEGIN_ARGS args = { };
    BA_ONCOMMITMSITRANSACTIONBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzTransactionId = wzTransactionId;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCommitMsiTransactionBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzTransactionId);
    ExitOnFailure(hr, "Failed to write transaction id of OnCommitMsiTransactionBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCommitMsiTransactionBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCommitMsiTransactionBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCommitMsiTransactionBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnCommitMsiTransactionBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCommitMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION* pAction
)
{
    HRESULT hr = S_OK;
    BA_ONCOMMITMSITRANSACTIONCOMPLETE_ARGS args = { };
    BA_ONCOMMITMSITRANSACTIONCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzTransactionId = wzTransactionId;
    args.hrStatus = hrStatus;
    args.restart = restart;
    args.recommendation = *pAction;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pAction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCommitMsiTransactionComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzTransactionId);
    ExitOnFailure(hr, "Failed to write transaction id of OnCommitMsiTransactionComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnCommitMsiTransactionComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.restart);
    ExitOnFailure(hr, "Failed to write restart of OnCommitMsiTransactionComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnCommitMsiTransactionComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCommitMsiTransactionComplete results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write API version of OnCommitMsiTransactionComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCommitMsiTransactionComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnCommitMsiTransactionComplete result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCommitMsiTransactionComplete result.");

    *pAction = results.action;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnCreate(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_COMMAND* pCommand
)
{
    HRESULT hr = S_OK;
    BA_ONCREATE_ARGS args = { };
    BA_ONCREATE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCreate args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, pCommand->cbSize);
    ExitOnFailure(hr, "Failed to write size of OnCreate args command.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, pCommand->action);
    ExitOnFailure(hr, "Failed to write action of OnCreate args command.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, pCommand->display);
    ExitOnFailure(hr, "Failed to write display of OnCreate args command.");

    hr = BuffWriteStringToBuffer(&bufferArgs, pCommand->wzCommandLine);
    ExitOnFailure(hr, "Failed to write command-line of OnCreate args command.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, pCommand->nCmdShow);
    ExitOnFailure(hr, "Failed to write show command of OnCreate args command.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, pCommand->resumeType);
    ExitOnFailure(hr, "Failed to write resume type of OnCreate args command.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, reinterpret_cast<DWORD64>(pCommand->hwndSplashScreen));
    ExitOnFailure(hr, "Failed to write splash screen handle of OnCreate args command.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, pCommand->relationType);
    ExitOnFailure(hr, "Failed to write relation type of OnCreate args command.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, pCommand->fPassthrough);
    ExitOnFailure(hr, "Failed to write passthrough of OnCreate args command.");

    hr = BuffWriteStringToBuffer(&bufferArgs, pCommand->wzLayoutDirectory);
    ExitOnFailure(hr, "Failed to write layout directory of OnCreate args command.");

    hr = BuffWriteStringToBuffer(&bufferArgs, pCommand->wzBootstrapperWorkingFolder);
    ExitOnFailure(hr, "Failed to write working folder of OnCreate args command.");

    hr = BuffWriteStringToBuffer(&bufferArgs, pCommand->wzBootstrapperApplicationDataPath);
    ExitOnFailure(hr, "Failed to write application data path of OnCreate args command.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnCreate results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCREATE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnCreate failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDestroy(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fReload
)
{
    HRESULT hr = S_OK;
    BA_ONDESTROY_ARGS args = { };
    BA_ONDESTROY_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.fReload = fReload;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDestroy args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fReload);
    ExitOnFailure(hr, "Failed to write reload of OnDestroy args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDestroy results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDESTROY, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDestroy failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fCached,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType,
    __in DWORD cPackages
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTBEGIN_ARGS args = { };
    BA_ONDETECTBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.registrationType = registrationType;
    args.cPackages = cPackages;
    args.fCached = fCached;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.registrationType);
    ExitOnFailure(hr, "Failed to write restart of OnDetectBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.cPackages);
    ExitOnFailure(hr, "Failed to write package count of OnDetectBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fCached);
    ExitOnFailure(hr, "Failed to write cached of OnDetectBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectCompatibleMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in VERUTIL_VERSION* pCompatiblePackageVersion
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTCOMPATIBLEMSIPACKAGE_ARGS args = { };
    BA_ONDETECTCOMPATIBLEMSIPACKAGE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzCompatiblePackageId = wzCompatiblePackageId;
    args.wzCompatiblePackageVersion = pCompatiblePackageVersion->sczVersion;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectCompatibleMsiPackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnDetectCompatibleMsiPackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzCompatiblePackageId);
    ExitOnFailure(hr, "Failed to write compatible package id of OnDetectCompatibleMsiPackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzCompatiblePackageVersion);
    ExitOnFailure(hr, "Failed to write compatible package version of OnDetectCompatibleMsiPackage args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectCompatibleMsiPackage results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectCompatibleMsiPackage failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectCompatibleMsiPackage result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectCompatibleMsiPackage result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOL fEligibleForCleanup
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTCOMPLETE_ARGS args = { };
    BA_ONDETECTCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;
    args.fEligibleForCleanup = fEligibleForCleanup;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnDetectComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fEligibleForCleanup);
    ExitOnFailure(hr, "Failed to write eligible for cleanup of OnDetectComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectComplete results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleCode,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOL fMissingFromCache
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS args = { };
    BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzBundleCode = wzBundleCode;
    args.relationType = relationType;
    args.wzBundleTag = wzBundleTag;
    args.fPerMachine = fPerMachine;
    args.wzVersion = pVersion->sczVersion;
    args.fMissingFromCache = fMissingFromCache;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectForwardCompatibleBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleCode);
    ExitOnFailure(hr, "Failed to write bundle code of OnDetectForwardCompatibleBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.relationType);
    ExitOnFailure(hr, "Failed to write relation type of OnDetectForwardCompatibleBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleTag);
    ExitOnFailure(hr, "Failed to write bundle tag of OnDetectForwardCompatibleBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fPerMachine);
    ExitOnFailure(hr, "Failed to write per-machine of OnDetectForwardCompatibleBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVersion);
    ExitOnFailure(hr, "Failed to write version of OnDetectForwardCompatibleBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fMissingFromCache);
    ExitOnFailure(hr, "Failed to write missing from cache of OnDetectForwardCompatibleBundle args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectForwardCompatibleBundle results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectForwardCompatibleBundle failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectForwardCompatibleBundle result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectForwardCompatibleBundle result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __in BOOTSTRAPPER_FEATURE_STATE state
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTMSIFEATURE_ARGS args = { };
    BA_ONDETECTMSIFEATURE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzFeatureId = wzFeatureId;
    args.state = state;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectMsiFeature args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnDetectMsiFeature args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzFeatureId);
    ExitOnFailure(hr, "Failed to write feature id of OnDetectMsiFeature args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.state);
    ExitOnFailure(hr, "Failed to write state of OnDetectMsiFeature args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectMsiFeature results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTMSIFEATURE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectMsiFeature failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectMsiFeature result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectMsiFeature result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPACKAGEBEGIN_ARGS args = { };
    BA_ONDETECTPACKAGEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectPackageBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnDetectPackageBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectPackageBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectPackageBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectPackageBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectPackageBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOL fCached
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPACKAGECOMPLETE_ARGS args = { };
    BA_ONDETECTPACKAGECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.state = state;
    args.fCached = fCached;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectPackageComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnDetectPackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnDetectPackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.state);
    ExitOnFailure(hr, "Failed to write state of OnDetectPackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fCached);
    ExitOnFailure(hr, "Failed to write cached of OnDetectPackageComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectPackageComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectPackageComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleCode,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOL fMissingFromCache
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTRELATEDBUNDLE_ARGS args = { };
    BA_ONDETECTRELATEDBUNDLE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzBundleCode = wzBundleCode;
    args.relationType = relationType;
    args.wzBundleTag = wzBundleTag;
    args.fPerMachine = fPerMachine;
    args.wzVersion = pVersion->sczVersion;
    args.fMissingFromCache = fMissingFromCache;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectRelatedBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleCode);
    ExitOnFailure(hr, "Failed to write bundle code of OnDetectRelatedBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.relationType);
    ExitOnFailure(hr, "Failed to write relation type of OnDetectRelatedBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleTag);
    ExitOnFailure(hr, "Failed to write bundle tag of OnDetectRelatedBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fPerMachine);
    ExitOnFailure(hr, "Failed to write per-machine of OnDetectRelatedBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVersion);
    ExitOnFailure(hr, "Failed to write version of OnDetectRelatedBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fMissingFromCache);
    ExitOnFailure(hr, "Failed to write cached of OnDetectRelatedBundle args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectRelatedBundle results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectRelatedBundle failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectRelatedBundle result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectRelatedBundle result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectRelatedBundlePackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzBundleCode,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTRELATEDBUNDLEPACKAGE_ARGS args = { };
    BA_ONDETECTRELATEDBUNDLEPACKAGE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzBundleCode = wzBundleCode;
    args.relationType = relationType;
    args.fPerMachine = fPerMachine;
    args.wzVersion = pVersion->sczVersion;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectRelatedBundlePackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnDetectRelatedBundlePackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleCode);
    ExitOnFailure(hr, "Failed to write bundle code of OnDetectRelatedBundlePackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.relationType);
    ExitOnFailure(hr, "Failed to write relation type of OnDetectRelatedBundlePackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fPerMachine);
    ExitOnFailure(hr, "Failed to write per-machine of OnDetectRelatedBundlePackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVersion);
    ExitOnFailure(hr, "Failed to write version of OnDetectRelatedBundlePackage args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectRelatedBundlePackage results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLEPACKAGE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectRelatedBundlePackage failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectRelatedBundlePackage result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectRelatedBundlePackage result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectRelatedMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzUpgradeCode,
    __in_z LPCWSTR wzProductCode,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTRELATEDMSIPACKAGE_ARGS args = { };
    BA_ONDETECTRELATEDMSIPACKAGE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzUpgradeCode = wzUpgradeCode;
    args.wzProductCode = wzProductCode;
    args.fPerMachine = fPerMachine;
    args.wzVersion = pVersion->sczVersion;
    args.operation = operation;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectRelatedMsiPackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnDetectRelatedMsiPackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzUpgradeCode);
    ExitOnFailure(hr, "Failed to write upgrade code of OnDetectRelatedMsiPackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzProductCode);
    ExitOnFailure(hr, "Failed to write product code of OnDetectRelatedMsiPackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fPerMachine);
    ExitOnFailure(hr, "Failed to write per-machine of OnDetectRelatedMsiPackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVersion);
    ExitOnFailure(hr, "Failed to write version of OnDetectRelatedMsiPackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.operation);
    ExitOnFailure(hr, "Failed to write operation OnDetectRelatedMsiPackage args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectRelatedMsiPackage results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDMSIPACKAGE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectRelatedMsiPackage failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectRelatedMsiPackage result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectRelatedMsiPackage result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectPatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __in BOOTSTRAPPER_PACKAGE_STATE patchState
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPATCHTARGET_ARGS args = { };
    BA_ONDETECTPATCHTARGET_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzProductCode = wzProductCode;
    args.patchState = patchState;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectPatchTarget args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnDetectPatchTarget args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzProductCode);
    ExitOnFailure(hr, "Failed to write product code of OnDetectPatchTarget args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.patchState);
    ExitOnFailure(hr, "Failed to write patch state OnDetectPatchTarget args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectPatchTarget results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPATCHTARGET, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectPatchTarget failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectPatchTarget result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectPatchTarget result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectUpdate(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzUpdateLocation,
    __in DWORD64 dw64Size,
    __in_z_opt LPCWSTR wzHash,
    __in BOOTSTRAPPER_UPDATE_HASH_TYPE hashAlgorithm,
    __in VERUTIL_VERSION* pVersion,
    __in_z_opt LPCWSTR wzTitle,
    __in_z_opt LPCWSTR wzSummary,
    __in_z_opt LPCWSTR wzContentType,
    __in_z_opt LPCWSTR wzContent,
    __inout BOOL* pfStopProcessingUpdates
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATE_ARGS args = { };
    BA_ONDETECTUPDATE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzUpdateLocation = wzUpdateLocation;
    args.dw64Size = dw64Size;
    args.wzHash = wzHash;
    args.hashAlgorithm = hashAlgorithm;
    args.wzVersion = pVersion->sczVersion;
    args.wzTitle = wzTitle;
    args.wzSummary = wzSummary;
    args.wzContentType = wzContentType;
    args.wzContent = wzContent;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.fStopProcessingUpdates = *pfStopProcessingUpdates;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectUpdate args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzUpdateLocation);
    ExitOnFailure(hr, "Failed to write update location of OnDetectUpdate args.");

    hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.dw64Size);
    ExitOnFailure(hr, "Failed to write update size OnDetectUpdate args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzHash);
    ExitOnFailure(hr, "Failed to write hash of OnDetectUpdate args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hashAlgorithm);
    ExitOnFailure(hr, "Failed to write hash algorithm OnDetectUpdate args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVersion);
    ExitOnFailure(hr, "Failed to write version of OnDetectUpdate args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzTitle);
    ExitOnFailure(hr, "Failed to write title of OnDetectUpdate args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzSummary);
    ExitOnFailure(hr, "Failed to write summary of OnDetectUpdate args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzContentType);
    ExitOnFailure(hr, "Failed to write content type of OnDetectUpdate args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzContent);
    ExitOnFailure(hr, "Failed to write content of OnDetectUpdate args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectUpdate results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.fStopProcessingUpdates);
    ExitOnFailure(hr, "Failed to write stop processing updates of OnDetectUpdate results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectUpdate failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectUpdate result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectUpdate result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fStopProcessingUpdates));
    ExitOnFailure(hr, "Failed to read stop processing updates of OnDetectUpdate result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pfStopProcessingUpdates = results.fStopProcessingUpdates;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectUpdateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzUpdateLocation,
    __inout BOOL* pfSkip
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATEBEGIN_ARGS args = { };
    BA_ONDETECTUPDATEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzUpdateLocation = wzUpdateLocation;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.fSkip = *pfSkip;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectUpdateBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzUpdateLocation);
    ExitOnFailure(hr, "Failed to write update location of OnDetectUpdateBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectUpdateBegin results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.fSkip);
    ExitOnFailure(hr, "Failed to write skip of OnDetectUpdateBegin results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectUpdateBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectUpdateBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectUpdateBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fSkip));
    ExitOnFailure(hr, "Failed to read cancel of OnDetectUpdateBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pfSkip = results.fSkip;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnDetectUpdateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __inout BOOL* pfIgnoreError
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATECOMPLETE_ARGS args = { };
    BA_ONDETECTUPDATECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.fIgnoreError = *pfIgnoreError;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectUpdateComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnDetectUpdateComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnDetectUpdateComplete results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.fIgnoreError);
    ExitOnFailure(hr, "Failed to write ignore error of OnDetectUpdateComplete results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnDetectUpdateComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnDetectUpdateComplete result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fIgnoreError));
    ExitOnFailure(hr, "Failed to read ignore error of OnDetectUpdateComplete result.");

    if (FAILED(hrStatus))
    {
        *pfIgnoreError = results.fIgnoreError;
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnElevateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONELEVATEBEGIN_ARGS args = { };
    BA_ONELEVATEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnElevateBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnElevateBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnElevateBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnElevateBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnElevateBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnElevateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONELEVATECOMPLETE_ARGS args = { };
    BA_ONELEVATECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnElevateComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnElevateComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnElevateComplete results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnElevateComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnError(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in DWORD dwCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD dwUIHint,
    __in DWORD cData,
    __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
    __inout int* pnResult
    )
{
    HRESULT hr = S_OK;
    BA_ONERROR_ARGS args = { };
    BA_ONERROR_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.errorType = errorType;
    args.wzPackageId = wzPackageId;
    args.dwCode = dwCode;
    args.wzError = wzError;
    args.dwUIHint = dwUIHint;
    args.cData = cData;
    args.rgwzData = rgwzData;
    args.nRecommendation = *pnResult;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.nResult = *pnResult;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnError args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.errorType);
    ExitOnFailure(hr, "Failed to write error type OnError args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnError args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwCode);
    ExitOnFailure(hr, "Failed to write code OnError args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzError);
    ExitOnFailure(hr, "Failed to write error of OnError args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwUIHint);
    ExitOnFailure(hr, "Failed to write UI hint OnError args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.cData);
    ExitOnFailure(hr, "Failed to write count of data of OnError args.");

    for (DWORD i = 0; i < args.cData; ++i)
    {
        hr = BuffWriteStringToBuffer(&bufferArgs, args.rgwzData[i]);
        ExitOnFailure(hr, "Failed to write data[%u] of OnError args.", i);
    }

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.nRecommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnError args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnError results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.nResult);
    ExitOnFailure(hr, "Failed to write result of OnError results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONERROR, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnError failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnError result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.nResult));
    ExitOnFailure(hr, "Failed to read result of OnError result.");

    *pnResult = results.nResult;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnExecuteBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cExecutingPackages
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEBEGIN_ARGS args = { };
    BA_ONEXECUTEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.cExecutingPackages = cExecutingPackages;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.cExecutingPackages);
    ExitOnFailure(hr, "Failed to write executing packages OnExecuteBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnExecuteBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnExecuteBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnExecuteBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnExecuteComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTECOMPLETE_ARGS args = { };
    BA_ONEXECUTECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status OnExecuteComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnExecuteComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnExecuteFilesInUse(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cFiles,
    __in_ecount_z_opt(cFiles) LPCWSTR* rgwzFiles,
    __in BOOTSTRAPPER_FILES_IN_USE_TYPE source,
    __inout int* pnResult
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEFILESINUSE_ARGS args = { };
    BA_ONEXECUTEFILESINUSE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.cFiles = cFiles;
    args.rgwzFiles = rgwzFiles;
    args.nRecommendation = *pnResult;
    args.source = source;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.nResult = *pnResult;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteFilesInUse args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnExecuteFilesInUse args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.cFiles);
    ExitOnFailure(hr, "Failed to write count of files of OnExecuteFilesInUse args.");

    for (DWORD i = 0; i < args.cFiles; ++i)
    {
        hr = BuffWriteStringToBuffer(&bufferArgs, args.rgwzFiles[i]);
        ExitOnFailure(hr, "Failed to write file[%u] of OnExecuteFilesInUse args.", i);
    }

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.nRecommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnExecuteFilesInUse args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.source);
    ExitOnFailure(hr, "Failed to write source of OnExecuteFilesInUse args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteFilesInUse results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.nResult);
    ExitOnFailure(hr, "Failed to write result of OnExecuteFilesInUse results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEFILESINUSE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnExecuteFilesInUse failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnExecuteFilesInUse result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.nResult));
    ExitOnFailure(hr, "Failed to read result of OnExecuteFilesInUse result.");

    *pnResult = results.nResult;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnExecuteMsiMessage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in INSTALLMESSAGE messageType,
    __in DWORD dwUIHint,
    __in_z LPCWSTR wzMessage,
    __in DWORD cData,
    __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
    __inout int* pnResult
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEMSIMESSAGE_ARGS args = { };
    BA_ONEXECUTEMSIMESSAGE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.messageType = messageType;
    args.dwUIHint = dwUIHint;
    args.wzMessage = wzMessage;
    args.cData = cData;
    args.rgwzData = rgwzData;
    args.nRecommendation = *pnResult;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.nResult = *pnResult;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteMsiMessage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnExecuteMsiMessage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.messageType);
    ExitOnFailure(hr, "Failed to write message type OnExecuteMsiMessage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwUIHint);
    ExitOnFailure(hr, "Failed to write UI hint OnExecuteMsiMessage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzMessage);
    ExitOnFailure(hr, "Failed to write message of OnExecuteMsiMessage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.cData);
    ExitOnFailure(hr, "Failed to write count of data of OnExecuteMsiMessage args.");

    for (DWORD i = 0; i < args.cData; ++i)
    {
        hr = BuffWriteStringToBuffer(&bufferArgs, args.rgwzData[i]);
        ExitOnFailure(hr, "Failed to write data[%u] of OnExecuteMsiMessage args.", i);
    }

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.nRecommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnExecuteMsiMessage args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteMsiMessage results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.nResult);
    ExitOnFailure(hr, "Failed to write result of OnExecuteMsiMessage results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEMSIMESSAGE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnExecuteMsiMessage failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnExecuteMsiMessage result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.nResult));
    ExitOnFailure(hr, "Failed to read cancel of OnExecuteMsiMessage result.");

    *pnResult = results.nResult;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnExecutePackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOL fExecute,
    __in BOOTSTRAPPER_ACTION_STATE action,
    __in INSTALLUILEVEL uiLevel,
    __in BOOL fDisableExternalUiHandler
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPACKAGEBEGIN_ARGS args = { };
    BA_ONEXECUTEPACKAGEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.fExecute = fExecute;
    args.action = action;
    args.uiLevel = uiLevel;
    args.fDisableExternalUiHandler = fDisableExternalUiHandler;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecutePackageBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnExecutePackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fExecute);
    ExitOnFailure(hr, "Failed to write execute OnExecutePackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.action);
    ExitOnFailure(hr, "Failed to write action OnExecutePackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.uiLevel);
    ExitOnFailure(hr, "Failed to write UI level of OnExecutePackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fDisableExternalUiHandler);
    ExitOnFailure(hr, "Failed to write disable external UI handler of OnExecutePackageBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecutePackageBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnExecutePackageBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnExecutePackageBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnExecutePackageBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnExecutePackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPACKAGECOMPLETE_ARGS args = { };
    BA_ONEXECUTEPACKAGECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.restart = restart;
    args.recommendation = *pAction;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pAction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecutePackageComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnExecutePackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnExecutePackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.restart);
    ExitOnFailure(hr, "Failed to write restart of OnExecutePackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnExecutePackageComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecutePackageComplete results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write action of OnExecutePackageComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnExecutePackageComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnExecutePackageComplete result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnExecutePackageComplete result.");

    *pAction = results.action;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnExecutePatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzTargetProductCode
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPATCHTARGET_ARGS args = { };
    BA_ONEXECUTEPATCHTARGET_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzTargetProductCode = wzTargetProductCode;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecutePatchTarget args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnExecutePatchTarget args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzTargetProductCode);
    ExitOnFailure(hr, "Failed to write target product code of OnExecutePatchTarget args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecutePatchTarget results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPATCHTARGET, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnExecutePatchTarget failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnExecutePatchTarget result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnExecutePatchTarget result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnExecuteProcessCancel(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD dwProcessId,
    __inout BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPROCESSCANCEL_ARGS args = { };
    BA_ONEXECUTEPROCESSCANCEL_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.dwProcessId = dwProcessId;
    args.recommendation = *pAction;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pAction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteProcessCancel args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnExecuteProcessCancel args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwProcessId);
    ExitOnFailure(hr, "Failed to write process id of OnExecuteProcessCancel args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommendation of OnExecuteProcessCancel args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteProcessCancel results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write action of OnExecuteProcessCancel results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROCESSCANCEL, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnExecuteProcessCancel failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnExecuteProcessCancel result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnExecuteProcessCancel result.");

    *pAction = results.action;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnExecuteProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD dwProgressPercentage,
    __in DWORD dwOverallPercentage,
    __out int* pnResult
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPROGRESS_ARGS args = { };
    BA_ONEXECUTEPROGRESS_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.dwProgressPercentage = dwProgressPercentage;
    args.dwOverallPercentage = dwOverallPercentage;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteProgress args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnExecuteProgress args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwProgressPercentage);
    ExitOnFailure(hr, "Failed to write progress of OnExecuteProgress args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to write overall progress of OnExecuteProgress args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnExecuteProgress results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROGRESS, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnExecuteProgress failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnExecuteProgress result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnExecuteProgress result.");

LExit:
    if (FAILED(hr))
    {
        *pnResult = IDERROR;
    }
    else if (results.fCancel)
    {
        *pnResult = IDCANCEL;
    }
    else
    {
        *pnResult = IDNOACTION;
    }

    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnLaunchApprovedExeBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONLAUNCHAPPROVEDEXEBEGIN_ARGS args = { };
    BA_ONLAUNCHAPPROVEDEXEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnLaunchApprovedExeBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnLaunchApprovedExeBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnLaunchApprovedExeBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnLaunchApprovedExeBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnLaunchApprovedExeBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnLaunchApprovedExeComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in DWORD dwProcessId
    )
{
    HRESULT hr = S_OK;
    BA_ONLAUNCHAPPROVEDEXECOMPLETE_ARGS args = { };
    BA_ONLAUNCHAPPROVEDEXECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;
    args.dwProcessId = dwProcessId;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnLaunchApprovedExeComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnLaunchApprovedExeComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwProcessId);
    ExitOnFailure(hr, "Failed to write process id of OnLaunchApprovedExeComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnLaunchApprovedExeComplete results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnLaunchApprovedExeComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPauseAUBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONPAUSEAUTOMATICUPDATESBEGIN_ARGS args = { };
    BA_ONPAUSEAUTOMATICUPDATESBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPauseAUBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPauseAUBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPauseAUBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPauseAUComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_ARGS args = { };
    BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPauseAUComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnPauseAUComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPauseAUComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPauseAUComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cPackages
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANBEGIN_ARGS args = { };
    BA_ONPLANBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.cPackages = cPackages;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.cPackages);
    ExitOnFailure(hr, "Failed to write count of packages of OnPlanBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanCompatibleMsiPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in VERUTIL_VERSION* pCompatiblePackageVersion,
    __inout BOOL* pfRequested
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_ARGS args = { };
    BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzCompatiblePackageId = wzCompatiblePackageId;
    args.wzCompatiblePackageVersion = pCompatiblePackageVersion->sczVersion;
    args.fRecommendedRemove = *pfRequested;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.fRequestRemove = *pfRequested;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanCompatibleMsiPackageBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnPlanCompatibleMsiPackageBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzCompatiblePackageId);
    ExitOnFailure(hr, "Failed to write compatible package id of OnPlanCompatibleMsiPackageBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzCompatiblePackageVersion);
    ExitOnFailure(hr, "Failed to write compatible package version of OnPlanCompatibleMsiPackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fRecommendedRemove);
    ExitOnFailure(hr, "Failed to write recommend remove of OnPlanCompatibleMsiPackageBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanCompatibleMsiPackageBegin results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.fRequestRemove);
    ExitOnFailure(hr, "Failed to write request remove of OnPlanCompatibleMsiPackageBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanCompatibleMsiPackageBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanCompatibleMsiPackageBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanCompatibleMsiPackageBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fRequestRemove));
    ExitOnFailure(hr, "Failed to read requested remove of OnPlanCompatibleMsiPackageBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pfRequested = results.fRequestRemove;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanCompatibleMsiPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in HRESULT hrStatus,
    __in BOOL fRequested
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_ARGS args = { };
    BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzCompatiblePackageId = wzCompatiblePackageId;
    args.hrStatus = hrStatus;
    args.fRequestedRemove = fRequested;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanCompatibleMsiPackageComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnPlanCompatibleMsiPackageComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzCompatiblePackageId);
    ExitOnFailure(hr, "Failed to write compatible package id of OnPlanCompatibleMsiPackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnPlanCompatibleMsiPackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fRequestedRemove);
    ExitOnFailure(hr, "Failed to write requested remove of OnPlanCompatibleMsiPackageComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanCompatibleMsiPackageComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanCompatibleMsiPackageComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __inout BOOTSTRAPPER_FEATURE_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANMSIFEATURE_ARGS args = { };
    BA_ONPLANMSIFEATURE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzFeatureId = wzFeatureId;
    args.recommendedState = *pRequestedState;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.requestedState = *pRequestedState;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanMsiFeature args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnPlanMsiFeature args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzFeatureId);
    ExitOnFailure(hr, "Failed to write feature id of OnPlanMsiFeature args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedState);
    ExitOnFailure(hr, "Failed to write recommended state of OnPlanMsiFeature args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanMsiFeature results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.requestedState);
    ExitOnFailure(hr, "Failed to write requested state of OnPlanMsiFeature results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIFEATURE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanMsiFeature failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanMsiFeature result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requested state of OnPlanMsiFeature result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanMsiFeature result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pRequestedState = results.requestedState;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPLETE_ARGS args = { };
    BA_ONPLANCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnPlanComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanComplete results.");

    // Callback.
    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleCode,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __inout BOOL* pfIgnoreBundle
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANFORWARDCOMPATIBLEBUNDLE_ARGS args = { };
    BA_ONPLANFORWARDCOMPATIBLEBUNDLE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzBundleCode = wzBundleCode;
    args.relationType = relationType;
    args.wzBundleTag = wzBundleTag;
    args.fPerMachine = fPerMachine;
    args.wzVersion = pVersion->sczVersion;
    args.fRecommendedIgnoreBundle = *pfIgnoreBundle;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.fIgnoreBundle = *pfIgnoreBundle;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanForwardCompatibleBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleCode);
    ExitOnFailure(hr, "Failed to write bundle code of OnPlanForwardCompatibleBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.relationType);
    ExitOnFailure(hr, "Failed to write relation type of OnPlanForwardCompatibleBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleTag);
    ExitOnFailure(hr, "Failed to write bundle tag of OnPlanForwardCompatibleBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fPerMachine);
    ExitOnFailure(hr, "Failed to write per-machine of OnPlanForwardCompatibleBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVersion);
    ExitOnFailure(hr, "Failed to write version of OnPlanForwardCompatibleBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fRecommendedIgnoreBundle);
    ExitOnFailure(hr, "Failed to write recommended ignore bundle of OnPlanForwardCompatibleBundle args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanForwardCompatibleBundle results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.fIgnoreBundle);
    ExitOnFailure(hr, "Failed to write ignore bundle of OnPlanForwardCompatibleBundle results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANFORWARDCOMPATIBLEBUNDLE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanForwardCompatibleBundle failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanForwardCompatibleBundle result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanForwardCompatibleBundle result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fIgnoreBundle));
    ExitOnFailure(hr, "Failed to read ignore bundle of OnPlanForwardCompatibleBundle result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pfIgnoreBundle = results.fIgnoreBundle;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOL fExecute,
    __in BOOTSTRAPPER_ACTION_STATE action,
    __inout BURN_MSI_PROPERTY* pActionMsiProperty,
    __inout INSTALLUILEVEL* pUiLevel,
    __inout BOOL* pfDisableExternalUiHandler,
    __inout BOOTSTRAPPER_MSI_FILE_VERSIONING* pFileVersioning
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANMSIPACKAGE_ARGS args = { };
    BA_ONPLANMSIPACKAGE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.fExecute = fExecute;
    args.action = action;
    args.recommendedFileVersioning = *pFileVersioning;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.actionMsiProperty = *pActionMsiProperty;
    results.uiLevel = *pUiLevel;
    results.fDisableExternalUiHandler = *pfDisableExternalUiHandler;
    results.fileVersioning = args.recommendedFileVersioning;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanMsiPackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnPlanMsiPackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fExecute);
    ExitOnFailure(hr, "Failed to write execute of OnPlanMsiPackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.action);
    ExitOnFailure(hr, "Failed to write action of OnPlanMsiPackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedFileVersioning);
    ExitOnFailure(hr, "Failed to write recommended file versioning of OnPlanMsiPackage args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanMsiPackage results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.actionMsiProperty);
    ExitOnFailure(hr, "Failed to write action msi property of OnPlanMsiPackage results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.uiLevel);
    ExitOnFailure(hr, "Failed to write UI level of OnPlanMsiPackage results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.fDisableExternalUiHandler);
    ExitOnFailure(hr, "Failed to write disable external UI handler of OnPlanMsiPackage results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.fileVersioning);
    ExitOnFailure(hr, "Failed to write file versioning of OnPlanMsiPackage results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIPACKAGE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanMsiPackage failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanMsiPackage result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanMsiPackage result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.actionMsiProperty));
    ExitOnFailure(hr, "Failed to read action MSI property of OnPlanMsiPackage result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.uiLevel));
    ExitOnFailure(hr, "Failed to read UI level of OnPlanMsiPackage result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fDisableExternalUiHandler));
    ExitOnFailure(hr, "Failed to read disable external UI handler of OnPlanMsiPackage result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fileVersioning));
    ExitOnFailure(hr, "Failed to read file versioning of OnPlanMsiPackage result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pActionMsiProperty = results.actionMsiProperty;
    *pUiLevel = results.uiLevel;
    *pfDisableExternalUiHandler = results.fDisableExternalUiHandler;
    *pFileVersioning = results.fileVersioning;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlannedCompatiblePackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in BOOL fRemove
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANNEDCOMPATIBLEPACKAGE_ARGS args = { };
    BA_ONPLANNEDCOMPATIBLEPACKAGE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzCompatiblePackageId = wzCompatiblePackageId;
    args.fRemove = fRemove;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlannedCompatiblePackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnPlannedCompatiblePackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzCompatiblePackageId);
    ExitOnFailure(hr, "Failed to write compatible package id of OnPlannedCompatiblePackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fRemove);
    ExitOnFailure(hr, "Failed to write remove of OnPlannedCompatiblePackage args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlannedCompatiblePackage results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDCOMPATIBLEPACKAGE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlannedCompatiblePackage failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlannedPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOTSTRAPPER_ACTION_STATE execute,
    __in BOOTSTRAPPER_ACTION_STATE rollback,
    __in BOOL fPlannedCache,
    __in BOOL fPlannedUncache
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANNEDPACKAGE_ARGS args = { };
    BA_ONPLANNEDPACKAGE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.execute = execute;
    args.rollback = rollback;
    args.fPlannedCache = fPlannedCache;
    args.fPlannedUncache = fPlannedUncache;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlannedPackage args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnPlannedPackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.execute);
    ExitOnFailure(hr, "Failed to write execute of OnPlannedPackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.rollback);
    ExitOnFailure(hr, "Failed to write rollback of OnPlannedPackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fPlannedCache);
    ExitOnFailure(hr, "Failed to write planned cache of OnPlannedPackage args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fPlannedUncache);
    ExitOnFailure(hr, "Failed to write planned uncache of OnPlannedPackage args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlannedPackage results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDPACKAGE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlannedPackage failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOL fCached,
    __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition,
    __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT repairCondition,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState,
    __inout BOOTSTRAPPER_CACHE_TYPE* pRequestedCacheType
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPACKAGEBEGIN_ARGS args = { };
    BA_ONPLANPACKAGEBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.state = state;
    args.fCached = fCached;
    args.installCondition = installCondition;
    args.repairCondition = repairCondition;
    args.recommendedState = *pRequestedState;
    args.recommendedCacheType = *pRequestedCacheType;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.requestedState = *pRequestedState;
    results.requestedCacheType = *pRequestedCacheType;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanPackageBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnPlanPackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.state);
    ExitOnFailure(hr, "Failed to write state of OnPlanPackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fCached);
    ExitOnFailure(hr, "Failed to write cached of OnPlanPackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.installCondition);
    ExitOnFailure(hr, "Failed to write install condition of OnPlanPackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.repairCondition);
    ExitOnFailure(hr, "Failed to write repair condition of OnPlanPackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedState);
    ExitOnFailure(hr, "Failed to write recommended state of OnPlanPackageBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedCacheType);
    ExitOnFailure(hr, "Failed to write recommended cache type of OnPlanPackageBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanPackageBegin results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.requestedState);
    ExitOnFailure(hr, "Failed to write requested state of OnPlanPackageBegin results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.requestedCacheType);
    ExitOnFailure(hr, "Failed to write requested cache type of OnPlanPackageBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanPackageBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanPackageBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanPackageBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requested state of OnPlanPackageBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.requestedCacheType));
    ExitOnFailure(hr, "Failed to read requested cache type of OnPlanPackageBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pRequestedState = results.requestedState;

    if (BOOTSTRAPPER_CACHE_TYPE_REMOVE <= results.requestedCacheType && BOOTSTRAPPER_CACHE_TYPE_FORCE >= results.requestedCacheType)
    {
        *pRequestedCacheType = results.requestedCacheType;
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_REQUEST_STATE requested
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPACKAGECOMPLETE_ARGS args = { };
    BA_ONPLANPACKAGECOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.requested = requested;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanPackageComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnPlanPackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnPlanPackageComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.requested);
    ExitOnFailure(hr, "Failed to write requested of OnPlanPackageComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanPackageComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGECOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanPackageComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleCode,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANRELATEDBUNDLE_ARGS args = { };
    BA_ONPLANRELATEDBUNDLE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzBundleCode = wzBundleCode;
    args.recommendedState = *pRequestedState;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.requestedState = *pRequestedState;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanRelatedBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleCode);
    ExitOnFailure(hr, "Failed to write bundle code of OnPlanRelatedBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedState);
    ExitOnFailure(hr, "Failed to write recommended state of OnPlanRelatedBundle args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanRelatedBundle results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.requestedState);
    ExitOnFailure(hr, "Failed to write requested state of OnPlanRelatedBundle results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanRelatedBundle failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanRelatedBundle result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanRelatedBundle result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requested state of OnPlanRelatedBundle result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pRequestedState = results.requestedState;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanRelatedBundleType(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleCode,
    __inout BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE* pRequestedType
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANRELATEDBUNDLETYPE_ARGS args = { };
    BA_ONPLANRELATEDBUNDLETYPE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzBundleCode = wzBundleCode;
    args.recommendedType = *pRequestedType;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.requestedType = *pRequestedType;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanRelatedBundleType args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleCode);
    ExitOnFailure(hr, "Failed to write bundle code of OnPlanRelatedBundleType args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedType);
    ExitOnFailure(hr, "Failed to write recommended type of OnPlanRelatedBundleType args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanRelatedBundleType results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.requestedType);
    ExitOnFailure(hr, "Failed to write requested type of OnPlanRelatedBundleType results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLETYPE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanRelatedBundleType failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanRelatedBundleType result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanRelatedBundleType result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.requestedType));
    ExitOnFailure(hr, "Failed to read requested type of OnPlanRelatedBundleType result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pRequestedType = results.requestedType;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanRestoreRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleCode,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANRESTORERELATEDBUNDLE_ARGS args = { };
    BA_ONPLANRESTORERELATEDBUNDLE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzBundleCode = wzBundleCode;
    args.recommendedState = *pRequestedState;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.requestedState = *pRequestedState;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanRestoreRelatedBundle args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleCode);
    ExitOnFailure(hr, "Failed to write bundle code of OnPlanRestoreRelatedBundle args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedState);
    ExitOnFailure(hr, "Failed to write recommended state of OnPlanRestoreRelatedBundle args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanRestoreRelatedBundle results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.requestedState);
    ExitOnFailure(hr, "Failed to write requested state of OnPlanRestoreRelatedBundle results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRESTORERELATEDBUNDLE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanRestoreRelatedBundle failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanRestoreRelatedBundle result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanRestoreRelatedBundle result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requested state of OnPlanRestoreRelatedBundle result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pRequestedState = results.requestedState;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanRollbackBoundary(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzRollbackBoundaryId,
    __inout BOOL* pfTransaction
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANROLLBACKBOUNDARY_ARGS args = { };
    BA_ONPLANROLLBACKBOUNDARY_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzRollbackBoundaryId = wzRollbackBoundaryId;
    args.fRecommendedTransaction = *pfTransaction;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.fTransaction = *pfTransaction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanRollbackBoundary args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzRollbackBoundaryId);
    ExitOnFailure(hr, "Failed to write rollback boundary id of OnPlanRollbackBoundary args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.fRecommendedTransaction);
    ExitOnFailure(hr, "Failed to write recommended transaction of OnPlanRollbackBoundary args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanRollbackBoundary results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.fTransaction);
    ExitOnFailure(hr, "Failed to write transaction of OnPlanRollbackBoundary results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANROLLBACKBOUNDARY, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanRollbackBoundary failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanRollbackBoundary result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fTransaction));
    ExitOnFailure(hr, "Failed to read transaction of OnPlanRollbackBoundary result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanRollbackBoundary result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pfTransaction = results.fTransaction;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnPlanPatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPATCHTARGET_ARGS args = { };
    BA_ONPLANPATCHTARGET_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzPackageId = wzPackageId;
    args.wzProductCode = wzProductCode;
    args.recommendedState = *pRequestedState;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.requestedState = *pRequestedState;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanPatchTarget args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageId);
    ExitOnFailure(hr, "Failed to write package id of OnPlanPatchTarget args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzProductCode);
    ExitOnFailure(hr, "Failed to write product code of OnPlanPatchTarget args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedState);
    ExitOnFailure(hr, "Failed to write recommended state of OnPlanPatchTarget args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnPlanPatchTarget results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.requestedState);
    ExitOnFailure(hr, "Failed to write requested state of OnPlanPatchTarget results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPATCHTARGET, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnPlanPatchTarget failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnPlanPatchTarget result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnPlanPatchTarget result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requrested state of OnPlanPatchTarget result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

    *pRequestedState = results.requestedState;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwProgressPercentage,
    __in DWORD dwOverallPercentage
    )
{
    HRESULT hr = S_OK;
    BA_ONPROGRESS_ARGS args = { };
    BA_ONPROGRESS_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.dwProgressPercentage = dwProgressPercentage;
    args.dwOverallPercentage = dwOverallPercentage;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnProgress args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwProgressPercentage);
    ExitOnFailure(hr, "Failed to write progress of OnProgress args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to write overall progress of OnProgress args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnProgress results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPROGRESS, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnProgress failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnProgress result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnProgress result.");

LExit:
    hr = FilterExecuteResult(pUserExperience, hr, fRollback, results.fCancel, L"OnProgress");

    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnRegisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_REGISTRATION_TYPE* pRegistrationType
    )
{
    HRESULT hr = S_OK;
    BA_ONREGISTERBEGIN_ARGS args = { };
    BA_ONREGISTERBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.recommendedRegistrationType = *pRegistrationType;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.registrationType = *pRegistrationType;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnRegisterBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedRegistrationType);
    ExitOnFailure(hr, "Failed to write recommended registration type of OnRegisterBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnRegisterBegin results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.registrationType);
    ExitOnFailure(hr, "Failed to write registration type of OnRegisterBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnRegisterBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnRegisterBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.fCancel));
    ExitOnFailure(hr, "Failed to read cancel of OnRegisterBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.registrationType));
    ExitOnFailure(hr, "Failed to read registration type of OnRegisterBegin result.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    else if (BOOTSTRAPPER_REGISTRATION_TYPE_NONE < results.registrationType && BOOTSTRAPPER_REGISTRATION_TYPE_FULL >= results.registrationType)
    {
        *pRegistrationType = results.registrationType;
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnRegisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONREGISTERCOMPLETE_ARGS args = { };
    BA_ONREGISTERCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnRegisterComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status type of OnRegisterComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnRegisterComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnRegisterComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnRollbackMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    )
{
    HRESULT hr = S_OK;
    BA_ONROLLBACKMSITRANSACTIONBEGIN_ARGS args = { };
    BA_ONROLLBACKMSITRANSACTIONBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzTransactionId = wzTransactionId;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnRollbackMsiTransactionBegin args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzTransactionId);
    ExitOnFailure(hr, "Failed to write transaction id of OnRollbackMsiTransactionBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnRollbackMsiTransactionBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnRollbackMsiTransactionBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnRollbackMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION *pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONROLLBACKMSITRANSACTIONCOMPLETE_ARGS args = { };
    BA_ONROLLBACKMSITRANSACTIONCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.wzTransactionId = wzTransactionId;
    args.hrStatus = hrStatus;
    args.restart = restart;
    args.recommendation = *pAction;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pAction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnRollbackMsiTransactionComplete args.");

    hr = BuffWriteStringToBuffer(&bufferArgs, args.wzTransactionId);
    ExitOnFailure(hr, "Failed to write transaction id of OnRollbackMsiTransactionComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status type of OnRollbackMsiTransactionComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.restart);
    ExitOnFailure(hr, "Failed to write restart of OnRollbackMsiTransactionComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendation);
    ExitOnFailure(hr, "Failed to write recommedation of OnRollbackMsiTransactionComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnRollbackMsiTransactionComplete results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write action of OnRollbackMsiTransactionComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnRollbackMsiTransactionComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnRollbackMsiTransactionComplete result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read cancel of OnRollbackMsiTransactionComplete result.");

    *pAction = results.action;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_SHUTDOWN_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONSHUTDOWN_ARGS args = { sizeof(args) };
    BA_ONSHUTDOWN_RESULTS results = { sizeof(results) };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.action = *pAction;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnShutdown args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnShutdown results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.action);
    ExitOnFailure(hr, "Failed to write action of OnShutdown results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnShutdown failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnShutdown result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read result action of OnShutdown result.");

    *pAction = results.action;

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnStartup(
    __in BURN_USER_EXPERIENCE* pUserExperience
)
{
    HRESULT hr = S_OK;
    BA_ONSTARTUP_ARGS args = { };
    BA_ONSTARTUP_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnStartup args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnStartup results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnStartup failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnSystemRestorePointBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONSYSTEMRESTOREPOINTBEGIN_ARGS args = { };
    BA_ONSYSTEMRESTOREPOINTBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnSystemRestorePointBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnSystemRestorePointBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnSystemRestorePointBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnSystemRestorePointComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONSYSTEMRESTOREPOINTCOMPLETE_ARGS args = { };
    BA_ONSYSTEMRESTOREPOINTCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnSystemRestorePointComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnSystemRestorePointComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnSystemRestorePointComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnSystemRestorePointComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnUnregisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_REGISTRATION_TYPE* pRegistrationType
    )
{
    HRESULT hr = S_OK;
    BA_ONUNREGISTERBEGIN_ARGS args = { };
    BA_ONUNREGISTERBEGIN_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };
    SIZE_T iBuffer = 0;

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.recommendedRegistrationType = *pRegistrationType;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    results.registrationType = *pRegistrationType;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnUnregisterBegin args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.recommendedRegistrationType);
    ExitOnFailure(hr, "Failed to write recommended registration type of OnUnregisterBegin args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnUnregisterBegin results.");

    hr = BuffWriteNumberToBuffer(&bufferResults, results.registrationType);
    ExitOnFailure(hr, "Failed to write registration type of OnUnregisterBegin results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERBEGIN, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnUnregisterBegin failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    // Read results.
    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read size of OnUnregisterBegin result.");

    hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.registrationType));
    ExitOnFailure(hr, "Failed to read registration type of OnUnregisterBegin result.");

    if (BOOTSTRAPPER_REGISTRATION_TYPE_NONE < results.registrationType && BOOTSTRAPPER_REGISTRATION_TYPE_FULL >= results.registrationType)
    {
        *pRegistrationType = results.registrationType;
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

EXTERN_C HRESULT BACallbackOnUnregisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONUNREGISTERCOMPLETE_ARGS args = { };
    BA_ONUNREGISTERCOMPLETE_RESULTS results = { };
    BUFF_BUFFER bufferArgs = { };
    BUFF_BUFFER bufferResults = { };
    PIPE_RPC_RESULT rpc = { };

    // Init structs.
    args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
    args.hrStatus = hrStatus;

    results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

    // Send args.
    hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnUnregisterComplete args.");

    hr = BuffWriteNumberToBuffer(&bufferArgs, args.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnUnregisterComplete args.");

    // Send results.
    hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
    ExitOnFailure(hr, "Failed to write API version of OnUnregisterComplete results.");

    // Callback.
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERCOMPLETE, &bufferArgs, &bufferResults, &rpc);
    ExitOnFailure(hr, "BA OnUnregisterComplete failed.");

    if (S_FALSE == hr)
    {
        ExitFunction();
    }

LExit:
    PipeFreeRpcResult(&rpc);
    ReleaseBuffer(bufferResults);
    ReleaseBuffer(bufferArgs);

    return hr;
}

// internal functions

// This filters the BA's responses to events during apply.
// If an apply thread failed, then return its error so this thread will bail out.
// During rollback, the BA can't cancel.
static HRESULT FilterExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOL fRollback,
    __in BOOL fCancel,
    __in LPCWSTR sczEventName
    )
{
    HRESULT hr = hrStatus;
    HRESULT hrApplyError = pUserExperience->hrApplyError; // make sure to use the same value for the whole method, since it can be changed in other threads.

    // If we failed return that error unless this is rollback which should roll on.
    if (FAILED(hrApplyError) && !fRollback)
    {
        hr = hrApplyError;
    }
    else if (fRollback)
    {
        if (fCancel)
        {
            LogId(REPORT_STANDARD, MSG_APPLY_CANCEL_IGNORED_DURING_ROLLBACK, sczEventName);
        }
        // TODO: since cancel isn't allowed, should the BA's HRESULT be ignored as well?
        // In the previous code, they could still alter rollback by returning IDERROR.
    }
    else
    {
        ExitOnFailure(hr, "BA %ls failed.", sczEventName);

        if (fCancel)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
        }
    }

LExit:
    return hr;
}

static HRESULT SendBAMessage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in BUFF_BUFFER* pBufferArgs,
    __in BUFF_BUFFER* pBufferResults,
    __in PIPE_RPC_RESULT* pResult
    )
{
    HRESULT hr = S_OK;
    BUFF_BUFFER buffer = { };

    if (PipeRpcInitialized(&pUserExperience->hBARpcPipe))
    {
        // Send the combined counted args and results buffer to the BA.
        hr = CombineArgsAndResults(pBufferArgs, pBufferResults, &buffer);
        if (SUCCEEDED(hr))
        {
            hr = PipeRpcRequest(&pUserExperience->hBARpcPipe, message, buffer.pbData, buffer.cbData, pResult);
        }
    }
    else
    {
        hr = S_FALSE;
    }

    ReleaseBuffer(buffer);
    return hr;
}

static HRESULT SendBAMessageFromInactiveEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in BUFF_BUFFER* pBufferArgs,
    __in BUFF_BUFFER* pBufferResults,
    __in PIPE_RPC_RESULT* pResult
)
{
    HRESULT hr = S_OK;
    BUFF_BUFFER buffer = { };

    if (PipeRpcInitialized(&pUserExperience->hBARpcPipe))
    {
        BootstrapperApplicationDeactivateEngine(pUserExperience);

        // Send the combined counted args and results buffer to the BA.
        hr = CombineArgsAndResults(pBufferArgs, pBufferResults, &buffer);
        if (SUCCEEDED(hr))
        {
            hr = PipeRpcRequest(&pUserExperience->hBARpcPipe, message, buffer.pbData, buffer.cbData, pResult);
        }

        BootstrapperApplicationActivateEngine(pUserExperience);
    }
    else
    {
        hr = S_FALSE;
    }

    ReleaseBuffer(buffer);
    return hr;
}

static HRESULT CombineArgsAndResults(
    __in BUFF_BUFFER* pBufferArgs,
    __in BUFF_BUFFER* pBufferResults,
    __in BUFF_BUFFER* pBufferCombined
    )
{
    HRESULT hr = S_OK;

    // Write args to buffer.
    hr = BuffWriteStreamToBuffer(pBufferCombined, pBufferArgs->pbData, pBufferArgs->cbData);
    ExitOnFailure(hr, "Failed to write args buffer.");

    // Write results to buffer.
    hr = BuffWriteStreamToBuffer(pBufferCombined, pBufferResults->pbData, pBufferResults->cbData);
    ExitOnFailure(hr, "Failed to write results buffer.");

LExit:
    return hr;
}
