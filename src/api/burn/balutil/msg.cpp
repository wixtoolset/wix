// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT OnApplyBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONAPPLYBEGIN_ARGS args = { };
    BA_ONAPPLYBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnApplyBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwPhaseCount);
    ExitOnFailure(hr, "Failed to read phase count of OnApplyBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnApplyBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnApplyBegin(args.dwPhaseCount, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnApplyBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnApplyBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnApplyBegin struct.");

LExit:
    return hr;
}

static HRESULT OnApplyComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONAPPLYCOMPLETE_ARGS args = { };
    BA_ONAPPLYCOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnApplyComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnApplyComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.restart));
    ExitOnFailure(hr, "Failed to read restart of OnApplyComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnApplyComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnApplyComplete results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnApplyComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnApplyComplete(args.hrStatus, args.restart, args.recommendation, &results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnApplyComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnApplyComplete struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnApplyComplete struct.");

LExit:
    return hr;
}

static HRESULT OnApplyDowngrade(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONAPPLYDOWNGRADE_ARGS args = { };
    BA_ONAPPLYDOWNGRADE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnApplyDowngrade args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrRecommended));
    ExitOnFailure(hr, "Failed to read recommended of OnApplyDowngrade args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnApplyDowngrade results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnApplyDowngrade results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYDOWNGRADE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnApplyDowngrade(args.hrRecommended, &results.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYDOWNGRADE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnApplyDowngrade failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnApplyDowngrade struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.hrStatus);
    ExitOnFailure(hr, "Failed to write status of OnApplyDowngrade struct.");

LExit:
    return hr;
}

static HRESULT OnBeginMsiTransactionBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONBEGINMSITRANSACTIONBEGIN_ARGS args = { };
    BA_ONBEGINMSITRANSACTIONBEGIN_RESULTS results = { };
    LPWSTR sczTransactionId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnBeginMsiTransactionBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczTransactionId);
    ExitOnFailure(hr, "Failed to read recommended of OnBeginMsiTransactionBegin args.");

    args.wzTransactionId = sczTransactionId;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnBeginMsiTransactionBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnBeginMsiTransactionBegin(args.wzTransactionId, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnBeginMsiTransactionBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnBeginMsiTransactionBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnBeginMsiTransactionBegin struct.");

LExit:
    ReleaseStr(sczTransactionId);
    return hr;
}

static HRESULT OnBeginMsiTransactionComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONBEGINMSITRANSACTIONCOMPLETE_ARGS args = { };
    BA_ONBEGINMSITRANSACTIONCOMPLETE_RESULTS results = { };
    LPWSTR sczTransactionId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnBeginMsiTransactionComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczTransactionId);
    ExitOnFailure(hr, "Failed to read transaction id of OnBeginMsiTransactionComplete args.");

    args.wzTransactionId = sczTransactionId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnBeginMsiTransactionComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnBeginMsiTransactionComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnBeginMsiTransactionComplete(args.wzTransactionId, args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnBeginMsiTransactionComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnBeginMsiTransactionComplete struct.");

LExit:
    ReleaseStr(sczTransactionId);
    return hr;
}

static HRESULT OnCacheAcquireBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEACQUIREBEGIN_ARGS args = { };
    BA_ONCACHEACQUIREBEGIN_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;
    LPWSTR sczSource = NULL;
    LPWSTR sczDownloadUrl = NULL;
    LPWSTR sczPayloadContainerId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheAcquireBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheAcquireBegin args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheAcquireBegin args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadString(pReaderArgs, &sczSource);
    ExitOnFailure(hr, "Failed to read source of OnCacheAcquireBegin args.");

    args.wzSource = sczSource;

    hr = BuffReaderReadString(pReaderArgs, &sczDownloadUrl);
    ExitOnFailure(hr, "Failed to read download url of OnCacheAcquireBegin args.");

    args.wzDownloadUrl = sczDownloadUrl;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadContainerId);
    ExitOnFailure(hr, "Failed to read payload container id of OnCacheAcquireBegin args.");

    args.wzPayloadContainerId = sczPayloadContainerId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnCacheAcquireBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheAcquireBegin results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCacheAcquireBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheAcquireBegin(args.wzPackageOrContainerId, args.wzPayloadId, args.wzSource, args.wzDownloadUrl, args.wzPayloadContainerId, args.recommendation, &results.action, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheAcquireBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheAcquireBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCacheAcquireBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCacheAcquireBegin struct.");

LExit:
    ReleaseStr(sczPayloadContainerId);
    ReleaseStr(sczDownloadUrl);
    ReleaseStr(sczSource);
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT OnCacheAcquireComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEACQUIRECOMPLETE_ARGS args = { };
    BA_ONCACHEACQUIRECOMPLETE_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheAcquireComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheAcquireComplete args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheAcquireComplete args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnCacheAcquireComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnCacheAcquireComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheAcquireComplete results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCacheAcquireComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheAcquireComplete(args.wzPackageOrContainerId, args.wzPayloadId, args.hrStatus, args.recommendation, &results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheAcquireComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheAcquireComplete struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCacheAcquireComplete struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT OnCacheAcquireProgress(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEACQUIREPROGRESS_ARGS args = { };
    BA_ONCACHEACQUIREPROGRESS_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheAcquireProgress args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheAcquireProgress args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheAcquireProgress args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64Progress);
    ExitOnFailure(hr, "Failed to read progress of OnCacheAcquireProgress args.");

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64Total);
    ExitOnFailure(hr, "Failed to read total progress of OnCacheAcquireProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to read overall percentage of OnCacheAcquireProgress args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheAcquireProgress results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREPROGRESS, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheAcquireProgress(args.wzPackageOrContainerId, args.wzPayloadId, args.dw64Progress, args.dw64Total, args.dwOverallPercentage, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREPROGRESS, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheAcquireProgress failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheAcquireProgress struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCacheAcquireProgress struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT OnCacheAcquireResolving(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEACQUIRERESOLVING_ARGS args = { };
    BA_ONCACHEACQUIRERESOLVING_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;
    DWORD cSearchPaths = 0;
    LPWSTR* rgsczSearchPaths = NULL;
    LPWSTR sczDownloadUrl = NULL;
    LPWSTR sczPayloadContainerId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheAcquireResolving args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheAcquireResolving args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheAcquireResolving args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadNumber(pReaderArgs, &cSearchPaths);
    ExitOnFailure(hr, "Failed to read overall percentage of OnCacheAcquireResolving args.");

    if (cSearchPaths)
    {
        rgsczSearchPaths = static_cast<LPWSTR*>(MemAlloc(sizeof(LPWSTR) * cSearchPaths, TRUE));
        ExitOnNull(rgsczSearchPaths, hr, E_OUTOFMEMORY, "Failed to allocate memory for search paths of OnCacheAcquireResolving args.");

        for (DWORD i = 0; i < cSearchPaths; ++i)
        {
            hr = BuffReaderReadString(pReaderArgs, &rgsczSearchPaths[i]);
            ExitOnFailure(hr, "Failed to read search path[%u] of OnCacheAcquireResolving args.", i);
        }
    }

    args.cSearchPaths = cSearchPaths;
    args.rgSearchPaths = const_cast<LPCWSTR*>(rgsczSearchPaths);

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fFoundLocal));
    ExitOnFailure(hr, "Failed to read found local of OnCacheAcquireResolving args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwRecommendedSearchPath);
    ExitOnFailure(hr, "Failed to read recommended search path of OnCacheAcquireResolving args.");

    hr = BuffReaderReadString(pReaderArgs, &sczDownloadUrl);
    ExitOnFailure(hr, "Failed to read download url of OnCacheAcquireResolving args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadContainerId);
    ExitOnFailure(hr, "Failed to read payload container id of OnCacheAcquireResolving args.");

    args.wzPayloadContainerId = sczPayloadContainerId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendedation of OnCacheAcquireResolving args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheAcquireResolving results.");

    hr = BuffReaderReadNumber(pReaderResults, &results.dwChosenSearchPath);
    ExitOnFailure(hr, "Failed to read chosen search path of OnCacheAcquireResolving results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCacheAcquireResolving results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRERESOLVING, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheAcquireResolving(args.wzPackageOrContainerId, args.wzPayloadId, args.rgSearchPaths, args.cSearchPaths, args.fFoundLocal, args.dwRecommendedSearchPath, args.wzDownloadUrl, args.wzPayloadContainerId, args.recommendation, &results.dwChosenSearchPath, &results.action, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRERESOLVING, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheAcquireResolving failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheAcquireResolving struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.dwChosenSearchPath);
    ExitOnFailure(hr, "Failed to write chosen search path of OnCacheAcquireResolving struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCacheAcquireResolving struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCacheAcquireResolving struct.");

LExit:
    for (DWORD i = 0; rgsczSearchPaths && i < cSearchPaths; ++i)
    {
        ReleaseStr(rgsczSearchPaths[i]);
    }
    ReleaseMem(rgsczSearchPaths);

    ReleaseStr(sczPayloadContainerId);
    ReleaseStr(sczDownloadUrl);
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);

    return hr;
}

static HRESULT OnCacheBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEBEGIN_ARGS args = { };
    BA_ONCACHEBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheBegin(&results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCacheBegin struct.");

LExit:
    return hr;
}

static HRESULT OnCacheComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECOMPLETE_ARGS args = { };
    BA_ONCACHECOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnCacheComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheComplete(args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheComplete struct.");

LExit:
    return hr;
}

static HRESULT OnCacheContainerOrPayloadVerifyBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_ARGS args = { };
    BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheContainerOrPayloadVerifyBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheContainerOrPayloadVerifyBegin args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheContainerOrPayloadVerifyBegin args.");

    args.wzPayloadId = sczPayloadId;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheContainerOrPayloadVerifyBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheContainerOrPayloadVerifyBegin(args.wzPackageOrContainerId, args.wzPayloadId, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheContainerOrPayloadVerifyBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheContainerOrPayloadVerifyBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCacheContainerOrPayloadVerifyBegin struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT OnCacheContainerOrPayloadVerifyComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_ARGS args = { };
    BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheContainerOrPayloadVerifyComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheContainerOrPayloadVerifyComplete args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheContainerOrPayloadVerifyComplete args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnCacheContainerOrPayloadVerifyComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheContainerOrPayloadVerifyComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheContainerOrPayloadVerifyComplete(args.wzPackageOrContainerId, args.wzPayloadId, args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheContainerOrPayloadVerifyComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheContainerOrPayloadVerifyComplete struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT OnCacheContainerOrPayloadVerifyProgress(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_ARGS args = { };
    BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheContainerOrPayloadVerifyProgress args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheContainerOrPayloadVerifyProgress args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheContainerOrPayloadVerifyProgress args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64Progress);
    ExitOnFailure(hr, "Failed to read progress of OnCacheContainerOrPayloadVerifyProgress args.");

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64Total);
    ExitOnFailure(hr, "Failed to read total progress of OnCacheContainerOrPayloadVerifyProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to read overall percentage of OnCacheContainerOrPayloadVerifyProgress args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheContainerOrPayloadVerifyProgress results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheContainerOrPayloadVerifyProgress(args.wzPackageOrContainerId, args.wzPayloadId, args.dw64Progress, args.dw64Total, args.dwOverallPercentage, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheContainerOrPayloadVerifyProgress failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheContainerOrPayloadVerifyProgress struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCacheContainerOrPayloadVerifyProgress struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT OnCachePackageBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPACKAGEBEGIN_ARGS args = { };
    BA_ONCACHEPACKAGEBEGIN_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePackageBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnCachePackageBegin args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, &args.cCachePayloads);
    ExitOnFailure(hr, "Failed to read count of cached payloads of OnCachePackageBegin args.");

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64PackageCacheSize);
    ExitOnFailure(hr, "Failed to read package cache size of OnCachePackageBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fVital));
    ExitOnFailure(hr, "Failed to read vital of OnCachePackageBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePackageBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCachePackageBegin(args.wzPackageId, args.cCachePayloads, args.dw64PackageCacheSize, args.fVital, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCachePackageBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCachePackageBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCachePackageBegin struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnCachePackageComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPACKAGECOMPLETE_ARGS args = { };
    BA_ONCACHEPACKAGECOMPLETE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePackageComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnCachePackageComplete args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnCachePackageComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnCachePackageComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePackageComplete results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCachePackageComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCachePackageComplete(args.wzPackageId, args.hrStatus, args.recommendation, &results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCachePackageComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCachePackageComplete struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCachePackageComplete struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnCachePackageNonVitalValidationFailure(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_ARGS args = { };
    BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePackageNonVitalValidationFailure args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnCachePackageNonVitalValidationFailure args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnCachePackageNonVitalValidationFailure args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnCachePackageNonVitalValidationFailure args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePackageNonVitalValidationFailure results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCachePackageNonVitalValidationFailure results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGENONVITALVALIDATIONFAILURE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCachePackageNonVitalValidationFailure(args.wzPackageId, args.hrStatus, args.recommendation, &results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGENONVITALVALIDATIONFAILURE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCachePackageNonVitalValidationFailure failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCachePackageNonVitalValidationFailure struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCachePackageNonVitalValidationFailure struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnCachePayloadExtractBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPAYLOADEXTRACTBEGIN_ARGS args = { };
    BA_ONCACHEPAYLOADEXTRACTBEGIN_RESULTS results = { };
    LPWSTR sczContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePayloadExtractBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczContainerId);
    ExitOnFailure(hr, "Failed to read container id of OnCachePayloadExtractBegin args.");

    args.wzContainerId = sczContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCachePayloadExtractBegin args.");

    args.wzPayloadId = sczPayloadId;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePayloadExtractBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCachePayloadExtractBegin(args.wzContainerId, args.wzPayloadId, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCachePayloadExtractBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCachePayloadExtractBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCachePayloadExtractBegin struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczContainerId);
    return hr;
}

static HRESULT OnCachePayloadExtractComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPAYLOADEXTRACTCOMPLETE_ARGS args = { };
    BA_ONCACHEPAYLOADEXTRACTCOMPLETE_RESULTS results = { };
    LPWSTR sczContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePayloadExtractComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczContainerId);
    ExitOnFailure(hr, "Failed to read container id of OnCachePayloadExtractComplete args.");

    args.wzContainerId = sczContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCachePayloadExtractComplete args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnCachePayloadExtractComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePayloadExtractComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCachePayloadExtractComplete(args.wzContainerId, args.wzPayloadId, args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCachePayloadExtractComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCachePayloadExtractComplete struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczContainerId);
    return hr;
}

static HRESULT OnCachePayloadExtractProgress(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPAYLOADEXTRACTPROGRESS_ARGS args = { };
    BA_ONCACHEPAYLOADEXTRACTPROGRESS_RESULTS results = { };
    LPWSTR sczContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePayloadExtractProgress args.");

    hr = BuffReaderReadString(pReaderArgs, &sczContainerId);
    ExitOnFailure(hr, "Failed to read container id of OnCachePayloadExtractProgress args.");

    args.wzContainerId = sczContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCachePayloadExtractProgress args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64Progress);
    ExitOnFailure(hr, "Failed to read progress of OnCachePayloadExtractProgress args.");

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64Total);
    ExitOnFailure(hr, "Failed to read total progress of OnCachePayloadExtractProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to read overall percentage of OnCachePayloadExtractProgress args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCachePayloadExtractProgress results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTPROGRESS, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCachePayloadExtractProgress(args.wzContainerId, args.wzPayloadId, args.dw64Progress, args.dw64Total, args.dwOverallPercentage, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTPROGRESS, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCachePayloadExtractProgress failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCachePayloadExtractProgress struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCachePayloadExtractProgress struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczContainerId);
    return hr;
}

static HRESULT OnCacheVerifyBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEVERIFYBEGIN_ARGS args = { };
    BA_ONCACHEVERIFYBEGIN_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheVerifyBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheVerifyBegin args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheVerifyBegin args.");

    args.wzPayloadId = sczPayloadId;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheVerifyBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheVerifyBegin(args.wzPackageOrContainerId, args.wzPayloadId, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheVerifyBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheVerifyBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCacheVerifyBegin struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT OnCacheVerifyComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEVERIFYCOMPLETE_ARGS args = { };
    BA_ONCACHEVERIFYCOMPLETE_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheVerifyComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheVerifyComplete args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheVerifyComplete args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnCacheVerifyComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnCacheVerifyComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheVerifyComplete results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read API version of OnCacheVerifyComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheVerifyComplete(args.wzPackageOrContainerId, args.wzPayloadId, args.hrStatus, args.recommendation, &results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheVerifyComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheVerifyComplete struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCacheVerifyComplete struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT OnCacheVerifyProgress(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEVERIFYPROGRESS_ARGS args = { };
    BA_ONCACHEVERIFYPROGRESS_RESULTS results = { };
    LPWSTR sczPackageOrContainerId = NULL;
    LPWSTR sczPayloadId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheVerifyProgress args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageOrContainerId);
    ExitOnFailure(hr, "Failed to read package or container id of OnCacheVerifyProgress args.");

    args.wzPackageOrContainerId = sczPackageOrContainerId;

    hr = BuffReaderReadString(pReaderArgs, &sczPayloadId);
    ExitOnFailure(hr, "Failed to read payload id of OnCacheVerifyProgress args.");

    args.wzPayloadId = sczPayloadId;

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64Progress);
    ExitOnFailure(hr, "Failed to read progress of OnCacheVerifyProgress args.");

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64Total);
    ExitOnFailure(hr, "Failed to read total progress of OnCacheVerifyProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to read overall percentage of OnCacheVerifyProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.verifyStep));
    ExitOnFailure(hr, "Failed to read verify step of OnCacheVerifyProgress args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCacheVerifyProgress results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYPROGRESS, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCacheVerifyProgress(args.wzPackageOrContainerId, args.wzPayloadId, args.dw64Progress, args.dw64Total, args.dwOverallPercentage, args.verifyStep, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYPROGRESS, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCacheVerifyProgress failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCacheVerifyProgress struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCacheVerifyProgress struct.");

LExit:
    ReleaseStr(sczPayloadId);
    ReleaseStr(sczPackageOrContainerId);
    return hr;
}

static HRESULT OnCommitMsiTransactionBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCOMMITMSITRANSACTIONBEGIN_ARGS args = { };
    BA_ONCOMMITMSITRANSACTIONBEGIN_RESULTS results = { };
    LPWSTR sczTransactionId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCommitMsiTransactionBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczTransactionId);
    ExitOnFailure(hr, "Failed to read transaction id of OnCommitMsiTransactionBegin args.");

    args.wzTransactionId = sczTransactionId;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCommitMsiTransactionBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCommitMsiTransactionBegin(args.wzTransactionId, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCommitMsiTransactionBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCommitMsiTransactionBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnCommitMsiTransactionBegin struct.");

LExit:
    ReleaseStr(sczTransactionId);
    return hr;
}

static HRESULT OnCommitMsiTransactionComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCOMMITMSITRANSACTIONCOMPLETE_ARGS args = { };
    BA_ONCOMMITMSITRANSACTIONCOMPLETE_RESULTS results = { };
    LPWSTR sczTransactionId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCommitMsiTransactionComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczTransactionId);
    ExitOnFailure(hr, "Failed to read transaction id of OnCommitMsiTransactionComplete args.");

    args.wzTransactionId = sczTransactionId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnCommitMsiTransactionComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.restart));
    ExitOnFailure(hr, "Failed to read restart of OnCommitMsiTransactionComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnCommitMsiTransactionComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCommitMsiTransactionComplete results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnCommitMsiTransactionComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCommitMsiTransactionComplete(args.wzTransactionId, args.hrStatus, args.restart, args.recommendation, &results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCommitMsiTransactionComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnCommitMsiTransactionComplete struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnCommitMsiTransactionComplete struct.");

LExit:
    ReleaseStr(sczTransactionId);
    return hr;
}

static HRESULT OnCreate(
    __in IBootstrapperApplication* pApplication,
    __in IBootstrapperEngine* pEngine,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONCREATE_ARGS args = { };
    BA_ONCREATE_RESULTS results = { };
    LPWSTR sczCommandLine = NULL;
    LPWSTR sczLayoutDirectory = NULL;
    LPWSTR sczBootstrapperWorkingFolder = NULL;
    LPWSTR sczBootstrapperApplicationDataPath = NULL;
    DWORD64 dw64 = 0;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCreate args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.command.cbSize));
    ExitOnFailure(hr, "Failed to size of of OnCreate args command.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.command.action));
    ExitOnFailure(hr, "Failed to read action of OnCreate args command.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.command.display));
    ExitOnFailure(hr, "Failed to read action of OnCreate args command.");

    hr = BuffReaderReadString(pReaderArgs, &sczCommandLine);
    ExitOnFailure(hr, "Failed to read command-line of OnCreate args command.");

    args.command.wzCommandLine = sczCommandLine;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.command.nCmdShow));
    ExitOnFailure(hr, "Failed to read show command of OnCreate args command.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.command.resumeType));
    ExitOnFailure(hr, "Failed to read resume type of OnCreate args command.");

    hr = BuffReaderReadNumber64(pReaderArgs, &dw64);
    ExitOnFailure(hr, "Failed to read splash screen handle of OnCreate args command.");

    args.command.hwndSplashScreen = reinterpret_cast<HWND>(dw64);

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.command.relationType));
    ExitOnFailure(hr, "Failed to read relation type of OnCreate args command.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.command.fPassthrough));
    ExitOnFailure(hr, "Failed to read passthrough of OnCreate args command.");

    hr = BuffReaderReadString(pReaderArgs, &sczLayoutDirectory);
    ExitOnFailure(hr, "Failed to read command-line of OnCreate args command.");

    args.command.wzLayoutDirectory = sczLayoutDirectory;

    hr = BuffReaderReadString(pReaderArgs, &sczBootstrapperWorkingFolder);
    ExitOnFailure(hr, "Failed to read command-line of OnCreate args command.");

    args.command.wzBootstrapperWorkingFolder = sczBootstrapperWorkingFolder;

    hr = BuffReaderReadString(pReaderArgs, &sczBootstrapperApplicationDataPath);
    ExitOnFailure(hr, "Failed to read command-line of OnCreate args command.");

    args.command.wzBootstrapperApplicationDataPath = sczBootstrapperApplicationDataPath;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnCreate results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCREATE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnCreate(pEngine, &args.command);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONCREATE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnCreate failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of BA_ONCREATE_RESULTS struct.");

LExit:
    ReleaseStr(sczBootstrapperApplicationDataPath);
    ReleaseStr(sczBootstrapperWorkingFolder);
    ReleaseStr(sczLayoutDirectory);
    ReleaseStr(sczCommandLine);

    return hr;
}

static HRESULT OnDestroy(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDESTROY_ARGS args = { };
    BA_ONDESTROY_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDestroy args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fReload));
    ExitOnFailure(hr, "Failed to read reload of OnDestroy args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDestroy results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDESTROY, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDestroy(args.fReload);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDESTROY, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDestroy failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDestroy struct.");

LExit:
    return hr;
}

static HRESULT OnDetectBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTBEGIN_ARGS args = { };
    BA_ONDETECTBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.registrationType));
    ExitOnFailure(hr, "Failed to read registration type of OnDetectBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.cPackages);
    ExitOnFailure(hr, "Failed to read package count of OnDetectBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fCached));
    ExitOnFailure(hr, "Failed to read cached of OnDetectBegin args.");


    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectBegin(args.fCached, args.registrationType, args.cPackages, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectBegin struct.");

LExit:
    return hr;
}

static HRESULT OnDetectCompatibleMsiPackage(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTCOMPATIBLEMSIPACKAGE_ARGS args = { };
    BA_ONDETECTCOMPATIBLEMSIPACKAGE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczCompatiblePackageId = NULL;
    LPWSTR sczCompatiblePackageVersion = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectCompatibleMsiPackage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnDetectCompatibleMsiPackage args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczCompatiblePackageId);
    ExitOnFailure(hr, "Failed to read compatible package id of OnDetectCompatibleMsiPackage args.");

    args.wzCompatiblePackageId = sczCompatiblePackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczCompatiblePackageVersion);
    ExitOnFailure(hr, "Failed to read compatible package version of OnDetectCompatibleMsiPackage args.");

    args.wzCompatiblePackageVersion = sczCompatiblePackageVersion;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectCompatibleMsiPackage results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectCompatibleMsiPackage(args.wzPackageId, args.wzCompatiblePackageId, args.wzCompatiblePackageVersion, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectCompatibleMsiPackage failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectCompatibleMsiPackage struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectCompatibleMsiPackage struct.");

LExit:
    ReleaseStr(sczCompatiblePackageVersion);
    ReleaseStr(sczCompatiblePackageId);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnDetectComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTCOMPLETE_ARGS args = { };
    BA_ONDETECTCOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnDetectComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fEligibleForCleanup));
    ExitOnFailure(hr, "Failed to read eligible for cleanup of OnDetectComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectComplete(args.hrStatus, args.fEligibleForCleanup);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectComplete struct.");

LExit:
    return hr;
}

static HRESULT OnDetectForwardCompatibleBundle(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS args = { };
    BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS results = { };
    LPWSTR sczBundleCode = NULL;
    LPWSTR sczBundleTag = NULL;
    LPWSTR sczVersion = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectForwardCompatibleBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleCode);
    ExitOnFailure(hr, "Failed to read bundle code of OnDetectForwardCompatibleBundle args.");

    args.wzBundleCode = sczBundleCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.relationType));
    ExitOnFailure(hr, "Failed to read relation type of OnDetectForwardCompatibleBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleTag);
    ExitOnFailure(hr, "Failed to read bundle tag of OnDetectForwardCompatibleBundle args.");

    args.wzBundleTag = sczBundleTag;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fPerMachine));
    ExitOnFailure(hr, "Failed to read per-machine of OnDetectForwardCompatibleBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVersion);
    ExitOnFailure(hr, "Failed to read version of OnDetectForwardCompatibleBundle args.");

    args.wzVersion = sczVersion;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fMissingFromCache));
    ExitOnFailure(hr, "Failed to read missing from cache of OnDetectForwardCompatibleBundle args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectForwardCompatibleBundle results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectForwardCompatibleBundle(args.wzBundleCode, args.relationType, args.wzBundleTag, args.fPerMachine, args.wzVersion, args.fMissingFromCache, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectForwardCompatibleBundle failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectForwardCompatibleBundle struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectForwardCompatibleBundle struct.");

LExit:
    ReleaseStr(sczVersion);
    ReleaseStr(sczBundleTag);
    ReleaseStr(sczBundleCode);
    return hr;
}

static HRESULT OnDetectMsiFeature(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTMSIFEATURE_ARGS args = { };
    BA_ONDETECTMSIFEATURE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczFeatureId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectMsiFeature args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnDetectMsiFeature args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczFeatureId);
    ExitOnFailure(hr, "Failed to read feature id of OnDetectMsiFeature args.");

    args.wzFeatureId = sczFeatureId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.state));
    ExitOnFailure(hr, "Failed to read state of OnDetectMsiFeature args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectMsiFeature results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTMSIFEATURE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectMsiFeature(args.wzPackageId, args.wzFeatureId, args.state, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTMSIFEATURE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectMsiFeature failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectMsiFeature struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectMsiFeature struct.");

LExit:
    ReleaseStr(sczFeatureId);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnDetectPackageBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPACKAGEBEGIN_ARGS args = { };
    BA_ONDETECTPACKAGEBEGIN_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectPackageBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnDetectPackageBegin args.");

    args.wzPackageId = sczPackageId;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectPackageBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectPackageBegin(args.wzPackageId, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectPackageBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectPackageBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectPackageBegin struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnDetectPackageComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPACKAGECOMPLETE_ARGS args = { };
    BA_ONDETECTPACKAGECOMPLETE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectPackageComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnDetectPackageComplete args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnDetectPackageComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.state));
    ExitOnFailure(hr, "Failed to read state of OnDetectPackageComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fCached));
    ExitOnFailure(hr, "Failed to read cached of OnDetectPackageComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectPackageComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectPackageComplete(args.wzPackageId, args.hrStatus, args.state, args.fCached);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectPackageComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectPackageComplete struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnDetectRelatedBundle(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTRELATEDBUNDLE_ARGS args = { };
    BA_ONDETECTRELATEDBUNDLE_RESULTS results = { };
    LPWSTR sczBundleCode = NULL;
    LPWSTR sczBundleTag = NULL;
    LPWSTR sczVersion = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectRelatedBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleCode);
    ExitOnFailure(hr, "Failed to read bundle code of OnDetectRelatedBundle args.");

    args.wzBundleCode = sczBundleCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.relationType));
    ExitOnFailure(hr, "Failed to read relation type of OnDetectRelatedBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleTag);
    ExitOnFailure(hr, "Failed to read bundle tag of OnDetectRelatedBundle args.");

    args.wzBundleTag = sczBundleTag;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fPerMachine));
    ExitOnFailure(hr, "Failed to read per-machine of OnDetectRelatedBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVersion);
    ExitOnFailure(hr, "Failed to read version of OnDetectRelatedBundle args.");

    args.wzVersion = sczVersion;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fMissingFromCache));
    ExitOnFailure(hr, "Failed to read missing from cache of OnDetectRelatedBundle args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectRelatedBundle results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectRelatedBundle(args.wzBundleCode, args.relationType, args.wzBundleTag, args.fPerMachine, args.wzVersion, args.fMissingFromCache, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectRelatedBundle failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectRelatedBundle struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectRelatedBundle struct.");

LExit:
    ReleaseStr(sczVersion);
    ReleaseStr(sczBundleTag);
    ReleaseStr(sczBundleCode);
    return hr;
}

static HRESULT OnDetectRelatedBundlePackage(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTRELATEDBUNDLEPACKAGE_ARGS args = { };
    BA_ONDETECTRELATEDBUNDLEPACKAGE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczBundleCode = NULL;
    LPWSTR sczVersion = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectRelatedBundlePackage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnDetectRelatedBundlePackage args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczBundleCode);
    ExitOnFailure(hr, "Failed to read bundle code of OnDetectRelatedBundlePackage args.");

    args.wzBundleCode = sczBundleCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.relationType));
    ExitOnFailure(hr, "Failed to read relation type of OnDetectRelatedBundlePackage args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fPerMachine));
    ExitOnFailure(hr, "Failed to read per-machine of OnDetectRelatedBundlePackage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVersion);
    ExitOnFailure(hr, "Failed to read version of OnDetectRelatedBundlePackage args.");

    args.wzVersion = sczVersion;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectRelatedBundlePackage results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLEPACKAGE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectRelatedBundlePackage(args.wzPackageId, args.wzBundleCode, args.relationType, args.fPerMachine, args.wzVersion, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLEPACKAGE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectRelatedBundlePackage failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectRelatedBundlePackage struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectRelatedBundlePackage struct.");

LExit:
    ReleaseStr(sczVersion);
    ReleaseStr(sczBundleCode);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnDetectRelatedMsiPackage(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTRELATEDMSIPACKAGE_ARGS args = { };
    BA_ONDETECTRELATEDMSIPACKAGE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczUpgradeCode = NULL;
    LPWSTR sczProductCode = NULL;
    LPWSTR sczVersion = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectRelatedMsiPackage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnDetectRelatedMsiPackage args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczUpgradeCode);
    ExitOnFailure(hr, "Failed to read upgrade code of OnDetectRelatedMsiPackage args.");

    args.wzUpgradeCode = sczUpgradeCode;

    hr = BuffReaderReadString(pReaderArgs, &sczProductCode);
    ExitOnFailure(hr, "Failed to read product code of OnDetectRelatedMsiPackage args.");

    args.wzProductCode = sczProductCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fPerMachine));
    ExitOnFailure(hr, "Failed to read per-machine of OnDetectRelatedMsiPackage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVersion);
    ExitOnFailure(hr, "Failed to read version of OnDetectRelatedMsiPackage args.");

    args.wzVersion = sczVersion;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.operation));
    ExitOnFailure(hr, "Failed to read per-machine of OnDetectRelatedMsiPackage args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectRelatedMsiPackage results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDMSIPACKAGE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectRelatedMsiPackage(args.wzPackageId, args.wzUpgradeCode, args.wzProductCode, args.fPerMachine, args.wzVersion, args.operation, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDMSIPACKAGE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectRelatedMsiPackage failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectRelatedMsiPackage struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectRelatedMsiPackage struct.");

LExit:
    ReleaseStr(sczVersion);
    ReleaseStr(sczProductCode);
    ReleaseStr(sczUpgradeCode);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnDetectPatchTarget(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPATCHTARGET_ARGS args = { };
    BA_ONDETECTPATCHTARGET_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczProductCode = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectPatchTarget args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnDetectPatchTarget args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczProductCode);
    ExitOnFailure(hr, "Failed to read product code of OnDetectPatchTarget args.");

    args.wzProductCode = sczProductCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.patchState));
    ExitOnFailure(hr, "Failed to read patch state of OnDetectPatchTarget args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectPatchTarget results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPATCHTARGET, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectPatchTarget(args.wzPackageId, args.wzProductCode, args.patchState, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPATCHTARGET, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectPatchTarget failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectPatchTarget struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectPatchTarget struct.");

LExit:
    ReleaseStr(sczProductCode);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnDetectUpdate(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATE_ARGS args = { };
    BA_ONDETECTUPDATE_RESULTS results = { };
    LPWSTR sczUpdateLocation = NULL;
    LPWSTR sczHash = NULL;
    LPWSTR sczVersion = NULL;
    LPWSTR sczTitle = NULL;
    LPWSTR sczSummary = NULL;
    LPWSTR sczContentType = NULL;
    LPWSTR sczContent = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectUpdate args.");

    hr = BuffReaderReadString(pReaderArgs, &sczUpdateLocation);
    ExitOnFailure(hr, "Failed to read update location of OnDetectUpdate args.");

    args.wzUpdateLocation = sczUpdateLocation;

    hr = BuffReaderReadNumber64(pReaderArgs, &args.dw64Size);
    ExitOnFailure(hr, "Failed to read update size of OnDetectUpdate args.");

    hr = BuffReaderReadString(pReaderArgs, &sczHash);
    ExitOnFailure(hr, "Failed to read hash of OnDetectUpdate args.");

    args.wzHash = sczHash;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hashAlgorithm));
    ExitOnFailure(hr, "Failed to read hash algorithm of OnDetectUpdate args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVersion);
    ExitOnFailure(hr, "Failed to read version of OnDetectUpdate args.");

    args.wzVersion = sczVersion;

    hr = BuffReaderReadString(pReaderArgs, &sczTitle);
    ExitOnFailure(hr, "Failed to read title of OnDetectUpdate args.");

    args.wzTitle = sczTitle;

    hr = BuffReaderReadString(pReaderArgs, &sczSummary);
    ExitOnFailure(hr, "Failed to read summary of OnDetectUpdate args.");

    args.wzSummary = sczSummary;

    hr = BuffReaderReadString(pReaderArgs, &sczContentType);
    ExitOnFailure(hr, "Failed to read content type of OnDetectUpdate args.");

    args.wzContentType = sczContentType;

    hr = BuffReaderReadString(pReaderArgs, &sczContent);
    ExitOnFailure(hr, "Failed to read content of OnDetectUpdate args.");

    args.wzContent = sczContent;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectUpdate results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.fStopProcessingUpdates));
    ExitOnFailure(hr, "Failed to read stop processing updates of OnDetectUpdate results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectUpdate(args.wzUpdateLocation, args.dw64Size, args.wzHash, args.hashAlgorithm, args.wzVersion, args.wzTitle, args.wzSummary, args.wzContentType, args.wzContent, &results.fCancel, &results.fStopProcessingUpdates);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectUpdate failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectUpdate struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectUpdate struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fStopProcessingUpdates);
    ExitOnFailure(hr, "Failed to write stop processing updates of OnDetectUpdate struct.");

LExit:
    ReleaseStr(sczContent);
    ReleaseStr(sczContentType);
    ReleaseStr(sczSummary);
    ReleaseStr(sczTitle);
    ReleaseStr(sczVersion);
    ReleaseStr(sczHash);
    ReleaseStr(sczUpdateLocation);
    return hr;
}

static HRESULT OnDetectUpdateBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATEBEGIN_ARGS args = { };
    BA_ONDETECTUPDATEBEGIN_RESULTS results = { };
    LPWSTR sczUpdateLocation = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectUpdateBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczUpdateLocation);
    ExitOnFailure(hr, "Failed to read update location of OnDetectUpdateBegin args.");

    args.wzUpdateLocation = sczUpdateLocation;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectUpdateBegin results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.fSkip));
    ExitOnFailure(hr, "Failed to read skip of OnDetectUpdateBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectUpdateBegin(args.wzUpdateLocation, &results.fCancel, &results.fSkip);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectUpdateBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectUpdateBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnDetectUpdateBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fSkip);
    ExitOnFailure(hr, "Failed to write skip processing updates of OnDetectUpdateBegin struct.");

LExit:
    ReleaseStr(sczUpdateLocation);
    return hr;
}


static HRESULT OnDetectUpdateComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATECOMPLETE_ARGS args = { };
    BA_ONDETECTUPDATECOMPLETE_RESULTS results = { };
    LPWSTR sczUpdateLocation = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectUpdateComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnDetectUpdateComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnDetectUpdateComplete results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.fIgnoreError));
    ExitOnFailure(hr, "Failed to read ignore error of OnDetectUpdateComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnDetectUpdateComplete(args.hrStatus, &results.fIgnoreError);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnDetectUpdateComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnDetectUpdateComplete struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fIgnoreError);
    ExitOnFailure(hr, "Failed to write ignore error of OnDetectUpdateComplete struct.");

LExit:
    ReleaseStr(sczUpdateLocation);
    return hr;
}

static HRESULT OnElevateBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONELEVATEBEGIN_ARGS args = { };
    BA_ONELEVATEBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnElevateBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnElevateBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnElevateBegin(&results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnElevateBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnElevateBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnElevateBegin struct.");

LExit:
    return hr;
}

static HRESULT OnElevateComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONELEVATECOMPLETE_ARGS args = { };
    BA_ONELEVATECOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnElevateComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnElevateComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnElevateComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnElevateComplete(args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnElevateComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnElevateComplete struct.");

LExit:
    return hr;
}

static HRESULT OnError(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONERROR_ARGS args = { };
    BA_ONERROR_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczError = NULL;
    DWORD cData = 0;
    LPWSTR* rgsczData = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnError args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.errorType));
    ExitOnFailure(hr, "Failed to read error type of OnError args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnError args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwCode);
    ExitOnFailure(hr, "Failed to read code of OnError args.");

    hr = BuffReaderReadString(pReaderArgs, &sczError);
    ExitOnFailure(hr, "Failed to read error of OnError args.");

    args.wzError = sczError;

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwUIHint);
    ExitOnFailure(hr, "Failed to read UI hint of OnError args.");

    hr = BuffReaderReadNumber(pReaderArgs, &cData);
    ExitOnFailure(hr, "Failed to read count of data of OnError args.");

    if (cData)
    {
        rgsczData = static_cast<LPWSTR*>(MemAlloc(sizeof(LPWSTR) * cData, TRUE));
        ExitOnNull(rgsczData, hr, E_OUTOFMEMORY, "Failed to allocate memory for data of OnError args.");

        for (DWORD i = 0; i < cData; ++i)
        {
            hr = BuffReaderReadString(pReaderArgs, &rgsczData[i]);
            ExitOnFailure(hr, "Failed to read search path[%u] of OnError args.", i);
        }
    }

    args.cData = cData;
    args.rgwzData = const_cast<LPCWSTR*>(rgsczData);

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.nRecommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnError args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnError results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.nResult));
    ExitOnFailure(hr, "Failed to read cancel of OnError results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONERROR, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnError(args.errorType, args.wzPackageId, args.dwCode, args.wzError, args.dwUIHint, args.cData, args.rgwzData, args.nRecommendation, &results.nResult);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONERROR, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnError failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnError struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.nResult);
    ExitOnFailure(hr, "Failed to write result of OnError struct.");

LExit:
    for (DWORD i = 0; rgsczData && i < cData; ++i)
    {
        ReleaseStr(rgsczData[i]);
    }
    ReleaseMem(rgsczData);

    ReleaseStr(sczError);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnExecuteBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEBEGIN_ARGS args = { };
    BA_ONEXECUTEBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.cExecutingPackages);
    ExitOnFailure(hr, "Failed to executing packages of OnExecuteBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnExecuteBegin(args.cExecutingPackages, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnExecuteBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnExecuteBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnExecuteBegin struct.");

LExit:
    return hr;
}


static HRESULT OnExecuteComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTECOMPLETE_ARGS args = { };
    BA_ONEXECUTECOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to status of OnExecuteComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnExecuteComplete(args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnExecuteComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnExecuteComplete struct.");

LExit:
    return hr;
}

static HRESULT OnExecuteFilesInUse(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEFILESINUSE_ARGS args = { };
    BA_ONEXECUTEFILESINUSE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    DWORD cFiles = 0;
    LPWSTR* rgsczFiles = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteFilesInUse args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnExecuteFilesInUse args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, &cFiles);
    ExitOnFailure(hr, "Failed to read count of files of OnExecuteFilesInUse args.");

    if (cFiles)
    {
        rgsczFiles = static_cast<LPWSTR*>(MemAlloc(sizeof(LPWSTR) * cFiles, TRUE));
        ExitOnNull(rgsczFiles, hr, E_OUTOFMEMORY, "Failed to allocate memory for files of OnExecuteFilesInUse args.");

        for (DWORD i = 0; i < cFiles; ++i)
        {
            hr = BuffReaderReadString(pReaderArgs, &rgsczFiles[i]);
            ExitOnFailure(hr, "Failed to read file[%u] of OnExecuteFilesInUse args.", i);
        }
    }

    args.cFiles = cFiles;
    args.rgwzFiles = const_cast<LPCWSTR*>(rgsczFiles);

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.nRecommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnExecuteFilesInUse args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.source));
    ExitOnFailure(hr, "Failed to read source of OnExecuteFilesInUse args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteFilesInUse results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.nResult));
    ExitOnFailure(hr, "Failed to read result of OnExecuteFilesInUse results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEFILESINUSE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnExecuteFilesInUse(args.wzPackageId, args.cFiles, args.rgwzFiles, args.nRecommendation, args.source, &results.nResult);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEFILESINUSE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnExecuteFilesInUse failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnExecuteFilesInUse struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.nResult);
    ExitOnFailure(hr, "Failed to write result of OnExecuteFilesInUse struct.");

LExit:
    for (DWORD i = 0; rgsczFiles && i < cFiles; ++i)
    {
        ReleaseStr(rgsczFiles[i]);
    }
    ReleaseMem(rgsczFiles);

    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnExecuteMsiMessage(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEMSIMESSAGE_ARGS args = { };
    BA_ONEXECUTEMSIMESSAGE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczMessage = NULL;
    DWORD cData = 0;
    LPWSTR* rgsczData = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteMsiMessage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnExecuteMsiMessage args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.messageType));
    ExitOnFailure(hr, "Failed to read messageType of OnExecuteMsiMessage args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwUIHint);
    ExitOnFailure(hr, "Failed to read UI hint of OnExecuteMsiMessage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczMessage);
    ExitOnFailure(hr, "Failed to read message of OnExecuteMsiMessage args.");

    args.wzMessage = sczMessage;

    hr = BuffReaderReadNumber(pReaderArgs, &cData);
    ExitOnFailure(hr, "Failed to read count of files of OnExecuteMsiMessage args.");

    if (cData)
    {
        rgsczData = static_cast<LPWSTR*>(MemAlloc(sizeof(LPWSTR) * cData, TRUE));
        ExitOnNull(rgsczData, hr, E_OUTOFMEMORY, "Failed to allocate memory for data of OnExecuteMsiMessage args.");

        for (DWORD i = 0; i < cData; ++i)
        {
            hr = BuffReaderReadString(pReaderArgs, &rgsczData[i]);
            ExitOnFailure(hr, "Failed to read data[%u] of OnExecuteMsiMessage args.", i);
        }
    }

    args.cData = cData;
    args.rgwzData = const_cast<LPCWSTR*>(rgsczData);

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.nRecommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnExecuteMsiMessage args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteMsiMessage results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.nResult));
    ExitOnFailure(hr, "Failed to read result of OnExecuteMsiMessage results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEMSIMESSAGE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnExecuteMsiMessage(args.wzPackageId, args.messageType, args.dwUIHint, args.wzMessage, args.cData, args.rgwzData, args.nRecommendation, &results.nResult);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEMSIMESSAGE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnExecuteMsiMessage failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnExecuteMsiMessage struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.nResult);
    ExitOnFailure(hr, "Failed to write result of OnExecuteMsiMessage struct.");

LExit:
    for (DWORD i = 0; rgsczData && i < cData; ++i)
    {
        ReleaseStr(rgsczData[i]);
    }
    ReleaseMem(rgsczData);

    ReleaseStr(sczMessage);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnExecutePackageBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPACKAGEBEGIN_ARGS args = { };
    BA_ONEXECUTEPACKAGEBEGIN_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecutePackageBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnExecutePackageBegin args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fExecute));
    ExitOnFailure(hr, "Failed to read execute of OnExecutePackageBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.action));
    ExitOnFailure(hr, "Failed to read action of OnExecutePackageBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.uiLevel));
    ExitOnFailure(hr, "Failed to read UI level of OnExecutePackageBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fDisableExternalUiHandler));
    ExitOnFailure(hr, "Failed to read disable external UI handler of OnExecutePackageBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecutePackageBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnExecutePackageBegin(args.wzPackageId, args.fExecute, args.action, args.uiLevel, args.fDisableExternalUiHandler, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnExecutePackageBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnExecutePackageBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnExecutePackageBegin struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnExecutePackageComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPACKAGECOMPLETE_ARGS args = { };
    BA_ONEXECUTEPACKAGECOMPLETE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecutePackageComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnExecutePackageComplete args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnExecutePackageComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.restart));
    ExitOnFailure(hr, "Failed to read restart of OnExecutePackageComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnExecutePackageComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecutePackageComplete results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnExecutePackageComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnExecutePackageComplete(args.wzPackageId, args.hrStatus, args.restart, args.recommendation, &results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnExecutePackageComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnExecutePackageComplete struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnExecutePackageComplete struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnExecutePatchTarget(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPATCHTARGET_ARGS args = { };
    BA_ONEXECUTEPATCHTARGET_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczTargetProductCode = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecutePatchTarget args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnExecutePatchTarget args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczTargetProductCode);
    ExitOnFailure(hr, "Failed to read target product code of OnExecutePatchTarget args.");

    args.wzTargetProductCode = sczTargetProductCode;


    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecutePatchTarget results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPATCHTARGET, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnExecutePatchTarget(args.wzPackageId, args.wzTargetProductCode, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPATCHTARGET, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnExecutePatchTarget failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnExecutePatchTarget struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnExecutePatchTarget struct.");

LExit:
    ReleaseStr(sczTargetProductCode);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnExecuteProcessCancel(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPROCESSCANCEL_ARGS args = { };
    BA_ONEXECUTEPROCESSCANCEL_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteProcessCancel args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnExecuteProcessCancel args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwProcessId);
    ExitOnFailure(hr, "Failed to read process id of OnExecuteProcessCancel args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnExecuteProcessCancel args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteProcessCancel results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnExecuteProcessCancel results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROCESSCANCEL, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnExecuteProcessCancel(args.wzPackageId, args.dwProcessId, args.recommendation, &results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROCESSCANCEL, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnExecuteProcessCancel failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnExecuteProcessCancel struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnExecuteProcessCancel struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnExecuteProgress(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPROGRESS_ARGS args = { };
    BA_ONEXECUTEPROGRESS_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteProgress args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnExecuteProgress args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwProgressPercentage);
    ExitOnFailure(hr, "Failed to read progress of OnExecuteProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to read overall progress of OnExecuteProgress args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnExecuteProgress results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROGRESS, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnExecuteProgress(args.wzPackageId, args.dwProgressPercentage, args.dwOverallPercentage, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROGRESS, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnExecuteProgress failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnExecuteProgress struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnExecuteProgress struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnLaunchApprovedExeBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONLAUNCHAPPROVEDEXEBEGIN_ARGS args = { };
    BA_ONLAUNCHAPPROVEDEXEBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnLaunchApprovedExeBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnLaunchApprovedExeBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnLaunchApprovedExeBegin(&results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnLaunchApprovedExeBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnLaunchApprovedExeBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnLaunchApprovedExeBegin struct.");

LExit:
    return hr;
}

static HRESULT OnLaunchApprovedExeComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONLAUNCHAPPROVEDEXECOMPLETE_ARGS args = { };
    BA_ONLAUNCHAPPROVEDEXECOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnLaunchApprovedExeComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnLaunchApprovedExeComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwProcessId);
    ExitOnFailure(hr, "Failed to read process id of OnLaunchApprovedExeComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnLaunchApprovedExeComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnLaunchApprovedExeComplete(args.hrStatus, args.dwProcessId);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnLaunchApprovedExeComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnLaunchApprovedExeComplete struct.");

LExit:
    return hr;
}

static HRESULT OnPauseAutomaticUpdatesBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPAUSEAUTOMATICUPDATESBEGIN_ARGS args = { };
    BA_ONPAUSEAUTOMATICUPDATESBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPauseAutomaticUpdatesBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPauseAutomaticUpdatesBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPauseAutomaticUpdatesBegin();
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPauseAutomaticUpdatesBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPauseAutomaticUpdatesBegin struct.");

LExit:
    return hr;
}

static HRESULT OnPauseAutomaticUpdatesComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_ARGS args = { };
    BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPauseAutomaticUpdatesComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnPauseAutomaticUpdatesComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPauseAutomaticUpdatesComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPauseAutomaticUpdatesComplete(args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPauseAutomaticUpdatesComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPauseAutomaticUpdatesComplete struct.");

LExit:
    return hr;
}

static HRESULT OnPlanBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANBEGIN_ARGS args = { };
    BA_ONPLANBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.cPackages);
    ExitOnFailure(hr, "Failed to read count of packages of OnPlanBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanBegin(args.cPackages, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanBegin struct.");

LExit:
    return hr;
}

static HRESULT OnPlanCompatibleMsiPackageBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_ARGS args = { };
    BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczCompatiblePackageId = NULL;
    LPWSTR sczCompatiblePackageVersion = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanCompatibleMsiPackageBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnPlanCompatibleMsiPackageBegin args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczCompatiblePackageId);
    ExitOnFailure(hr, "Failed to read compatible package id of OnPlanCompatibleMsiPackageBegin args.");

    args.wzCompatiblePackageId = sczCompatiblePackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczCompatiblePackageVersion);
    ExitOnFailure(hr, "Failed to read compatible package version of OnPlanCompatibleMsiPackageBegin args.");

    args.wzCompatiblePackageVersion = sczCompatiblePackageVersion;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fRecommendedRemove));
    ExitOnFailure(hr, "Failed to read recommend remove of OnPlanCompatibleMsiPackageBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanCompatibleMsiPackageBegin results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.fRequestRemove));
    ExitOnFailure(hr, "Failed to read request remove of OnPlanCompatibleMsiPackageBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanCompatibleMsiPackageBegin(args.wzPackageId, args.wzCompatiblePackageId, args.wzCompatiblePackageVersion, args.fRecommendedRemove, &results.fCancel, &results.fRequestRemove);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanCompatibleMsiPackageBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanCompatibleMsiPackageBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanCompatibleMsiPackageBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fRequestRemove);
    ExitOnFailure(hr, "Failed to write requested remove of OnPlanCompatibleMsiPackageBegin struct.");

LExit:
    ReleaseStr(sczCompatiblePackageVersion);
    ReleaseStr(sczCompatiblePackageId);
    ReleaseStr(sczPackageId);

    return hr;
}

static HRESULT OnPlanCompatibleMsiPackageComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_ARGS args = { };
    BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczCompatiblePackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanCompatibleMsiPackageComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnPlanCompatibleMsiPackageComplete args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczCompatiblePackageId);
    ExitOnFailure(hr, "Failed to read compatible package id of OnPlanCompatibleMsiPackageComplete args.");

    args.wzCompatiblePackageId = sczCompatiblePackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnPlanCompatibleMsiPackageComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fRequestedRemove));
    ExitOnFailure(hr, "Failed to read requested remove of OnPlanCompatibleMsiPackageComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanCompatibleMsiPackageComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanCompatibleMsiPackageComplete(args.wzPackageId, args.wzCompatiblePackageId, args.hrStatus, args.fRequestedRemove);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanCompatibleMsiPackageComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanCompatibleMsiPackageComplete struct.");


LExit:
    ReleaseStr(sczCompatiblePackageId);
    ReleaseStr(sczPackageId);

    return hr;
}

static HRESULT OnPlanMsiFeature(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANMSIFEATURE_ARGS args = { };
    BA_ONPLANMSIFEATURE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczFeatureId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanMsiFeature args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnPlanMsiFeature args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczFeatureId);
    ExitOnFailure(hr, "Failed to read feature id of OnPlanMsiFeature args.");

    args.wzFeatureId = sczFeatureId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedState));
    ExitOnFailure(hr, "Failed to read recommended state of OnPlanMsiFeature args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanMsiFeature results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requested state of OnPlanMsiFeature results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIFEATURE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanMsiFeature(args.wzPackageId, args.wzFeatureId, args.recommendedState, &results.requestedState, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIFEATURE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanMsiFeature failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanMsiFeature struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.requestedState);
    ExitOnFailure(hr, "Failed to write requested state of OnPlanMsiFeature struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanMsiFeature struct.");

LExit:
    ReleaseStr(sczFeatureId);
    ReleaseStr(sczPackageId);

    return hr;
}

static HRESULT OnPlanComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPLETE_ARGS args = { };
    BA_ONPLANCOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnPlanComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanComplete(args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanComplete struct.");

LExit:
    return hr;
}

static HRESULT OnPlanForwardCompatibleBundle(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANFORWARDCOMPATIBLEBUNDLE_ARGS args = { };
    BA_ONPLANFORWARDCOMPATIBLEBUNDLE_RESULTS results = { };
    LPWSTR sczBundleCode = NULL;
    LPWSTR sczBundleTag = NULL;
    LPWSTR sczVersion = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanForwardCompatibleBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleCode);
    ExitOnFailure(hr, "Failed to read bundle code of OnPlanForwardCompatibleBundle args.");

    args.wzBundleCode = sczBundleCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.relationType));
    ExitOnFailure(hr, "Failed to read relation type of OnPlanForwardCompatibleBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleTag);
    ExitOnFailure(hr, "Failed to read bundle tag of OnPlanForwardCompatibleBundle args.");

    args.wzBundleTag = sczBundleTag;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fPerMachine));
    ExitOnFailure(hr, "Failed to read per-machine of OnPlanForwardCompatibleBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczVersion);
    ExitOnFailure(hr, "Failed to read version of OnPlanForwardCompatibleBundle args.");

    args.wzVersion = sczVersion;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fRecommendedIgnoreBundle));
    ExitOnFailure(hr, "Failed to read recommended ignore bundle of OnPlanForwardCompatibleBundle args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanForwardCompatibleBundle results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.fIgnoreBundle));
    ExitOnFailure(hr, "Failed to read requested ignore bundle of OnPlanForwardCompatibleBundle results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANFORWARDCOMPATIBLEBUNDLE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanForwardCompatibleBundle(args.wzBundleCode, args.relationType, args.wzBundleTag, args.fPerMachine, args.wzVersion, args.fRecommendedIgnoreBundle, &results.fCancel, &results.fIgnoreBundle);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANFORWARDCOMPATIBLEBUNDLE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanForwardCompatibleBundle failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanForwardCompatibleBundle struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanForwardCompatibleBundle struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fIgnoreBundle);
    ExitOnFailure(hr, "Failed to write ignore bundle of OnPlanForwardCompatibleBundle struct.");

LExit:
    ReleaseStr(sczVersion);
    ReleaseStr(sczBundleTag);
    ReleaseStr(sczBundleCode);
    return hr;
}

static HRESULT OnPlanMsiPackage(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANMSIPACKAGE_ARGS args = { };
    BA_ONPLANMSIPACKAGE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanMsiPackage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnPlanMsiPackage args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fExecute));
    ExitOnFailure(hr, "Failed to read execute of OnPlanMsiPackage args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.action));
    ExitOnFailure(hr, "Failed to read action of OnPlanMsiPackage args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedFileVersioning));
    ExitOnFailure(hr, "Failed to read recommended file versioning of OnPlanMsiPackage args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanMsiPackage results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.actionMsiProperty));
    ExitOnFailure(hr, "Failed to read action msi property of OnPlanMsiPackage results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.uiLevel));
    ExitOnFailure(hr, "Failed to read UI level of OnPlanMsiPackage results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.fDisableExternalUiHandler));
    ExitOnFailure(hr, "Failed to read disable external UI handler of OnPlanMsiPackage results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.fileVersioning));
    ExitOnFailure(hr, "Failed to read file versioning of OnPlanMsiPackage results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIPACKAGE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanMsiPackage(args.wzPackageId, args.fExecute, args.action, args.recommendedFileVersioning, &results.fCancel, &results.actionMsiProperty, &results.uiLevel, &results.fDisableExternalUiHandler, &results.fileVersioning);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIPACKAGE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanMsiPackage failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanMsiPackage struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanMsiPackage struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.actionMsiProperty);
    ExitOnFailure(hr, "Failed to write action MSI property of OnPlanMsiPackage struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.uiLevel);
    ExitOnFailure(hr, "Failed to write UI level of OnPlanMsiPackage struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fDisableExternalUiHandler);
    ExitOnFailure(hr, "Failed to write external UI handler of OnPlanMsiPackage struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fileVersioning);
    ExitOnFailure(hr, "Failed to write file versioning of OnPlanMsiPackage struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnPlannedCompatiblePackage(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANNEDCOMPATIBLEPACKAGE_ARGS args = { };
    BA_ONPLANNEDCOMPATIBLEPACKAGE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczCompatiblePackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlannedCompatiblePackage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnPlannedCompatiblePackage args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczCompatiblePackageId);
    ExitOnFailure(hr, "Failed to read compatible package id of OnPlannedCompatiblePackage args.");

    args.wzCompatiblePackageId = sczCompatiblePackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fRemove));
    ExitOnFailure(hr, "Failed to read remove of OnPlannedCompatiblePackage args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlannedCompatiblePackage results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDCOMPATIBLEPACKAGE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlannedCompatiblePackage(args.wzPackageId, args.wzCompatiblePackageId, args.fRemove);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDCOMPATIBLEPACKAGE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlannedCompatiblePackage failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlannedCompatiblePackage struct.");

LExit:
    ReleaseStr(sczCompatiblePackageId);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnPlannedPackage(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANNEDPACKAGE_ARGS args = { };
    BA_ONPLANNEDPACKAGE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlannedPackage args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnPlannedPackage args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.execute));
    ExitOnFailure(hr, "Failed to read execute of OnPlannedPackage args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.rollback));
    ExitOnFailure(hr, "Failed to read rollback of OnPlannedPackage args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fPlannedCache));
    ExitOnFailure(hr, "Failed to read planned cache of OnPlannedPackage args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fPlannedUncache));
    ExitOnFailure(hr, "Failed to read planned uncache of OnPlannedPackage args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlannedPackage results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDPACKAGE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlannedPackage(args.wzPackageId, args.execute, args.rollback, args.fPlannedCache, args.fPlannedUncache);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDPACKAGE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlannedPackage failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlannedPackage struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnPlanPackageBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPACKAGEBEGIN_ARGS args = { };
    BA_ONPLANPACKAGEBEGIN_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanPackageBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnPlanPackageBegin args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.state));
    ExitOnFailure(hr, "Failed to read state of OnPlanPackageBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fCached));
    ExitOnFailure(hr, "Failed to read cached of OnPlanPackageBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.installCondition));
    ExitOnFailure(hr, "Failed to read install condition of OnPlanPackageBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.repairCondition));
    ExitOnFailure(hr, "Failed to read repair condition of OnPlanPackageBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedState));
    ExitOnFailure(hr, "Failed to read recommended state of OnPlanPackageBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedCacheType));
    ExitOnFailure(hr, "Failed to read recommended cache type of OnPlanPackageBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanPackageBegin results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requested state of OnPlanPackageBegin results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.requestedCacheType));
    ExitOnFailure(hr, "Failed to read requested cache type of OnPlanPackageBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanPackageBegin(args.wzPackageId, args.state, args.fCached, args.installCondition, args.repairCondition, args.recommendedState, args.recommendedCacheType, &results.requestedState, &results.requestedCacheType, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanPackageBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanPackageBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanPackageBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.requestedState);
    ExitOnFailure(hr, "Failed to write requested state of OnPlanPackageBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.requestedCacheType);
    ExitOnFailure(hr, "Failed to write requested cache type of OnPlanPackageBegin struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnPlanPackageComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPACKAGECOMPLETE_ARGS args = { };
    BA_ONPLANPACKAGECOMPLETE_RESULTS results = { };
    LPWSTR sczPackageId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanPackageComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnPlanPackageComplete args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnPlanPackageComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.requested));
    ExitOnFailure(hr, "Failed to read requested of OnPlanPackageComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanPackageComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGECOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanPackageComplete(args.wzPackageId, args.hrStatus, args.requested);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGECOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanPackageComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanPackageComplete struct.");

LExit:
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnPlanRelatedBundle(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANRELATEDBUNDLE_ARGS args = { };
    BA_ONPLANRELATEDBUNDLE_RESULTS results = { };
    LPWSTR sczBundleCode = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanRelatedBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleCode);
    ExitOnFailure(hr, "Failed to read package id of OnPlanRelatedBundle args.");

    args.wzBundleCode = sczBundleCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedState));
    ExitOnFailure(hr, "Failed to read recommended state of OnPlanRelatedBundle args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanRelatedBundle results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requested state of OnPlanRelatedBundle results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanRelatedBundle(args.wzBundleCode, args.recommendedState, &results.requestedState, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanRelatedBundle failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanRelatedBundle struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanRelatedBundle struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.requestedState);
    ExitOnFailure(hr, "Failed to write requested state of OnPlanRelatedBundle struct.");

LExit:
    ReleaseStr(sczBundleCode);
    return hr;
}

static HRESULT OnPlanRelatedBundleType(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANRELATEDBUNDLETYPE_ARGS args = { };
    BA_ONPLANRELATEDBUNDLETYPE_RESULTS results = { };
    LPWSTR sczBundleCode = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanRelatedBundleType args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleCode);
    ExitOnFailure(hr, "Failed to read package id of OnPlanRelatedBundleType args.");

    args.wzBundleCode = sczBundleCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedType));
    ExitOnFailure(hr, "Failed to read recommended type of OnPlanRelatedBundleType args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanRelatedBundleType results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.requestedType));
    ExitOnFailure(hr, "Failed to read requested type of OnPlanRelatedBundleType results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLETYPE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanRelatedBundleType(args.wzBundleCode, args.recommendedType, &results.requestedType, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLETYPE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanRelatedBundleType failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanRelatedBundleType struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanRelatedBundleType struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.requestedType);
    ExitOnFailure(hr, "Failed to write requested type of OnPlanRelatedBundleType struct.");

LExit:
    ReleaseStr(sczBundleCode);
    return hr;
}

static HRESULT OnPlanRestoreRelatedBundle(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANRESTORERELATEDBUNDLE_ARGS args = { };
    BA_ONPLANRESTORERELATEDBUNDLE_RESULTS results = { };
    LPWSTR sczBundleCode = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanRestoreRelatedBundle args.");

    hr = BuffReaderReadString(pReaderArgs, &sczBundleCode);
    ExitOnFailure(hr, "Failed to read package id of OnPlanRestoreRelatedBundle args.");

    args.wzBundleCode = sczBundleCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedState));
    ExitOnFailure(hr, "Failed to read recommended state of OnPlanRestoreRelatedBundle args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanRestoreRelatedBundle results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requested state of OnPlanRestoreRelatedBundle results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRESTORERELATEDBUNDLE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanRestoreRelatedBundle(args.wzBundleCode, args.recommendedState, &results.requestedState, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRESTORERELATEDBUNDLE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanRestoreRelatedBundle failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanRestoreRelatedBundle struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanRestoreRelatedBundle struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.requestedState);
    ExitOnFailure(hr, "Failed to write requested state of OnPlanRestoreRelatedBundle struct.");

LExit:
    ReleaseStr(sczBundleCode);
    return hr;
}

static HRESULT OnPlanRollbackBoundary(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANROLLBACKBOUNDARY_ARGS args = { };
    BA_ONPLANROLLBACKBOUNDARY_RESULTS results = { };
    LPWSTR sczRollbackBoundaryId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanRollbackBoundary args.");

    hr = BuffReaderReadString(pReaderArgs, &sczRollbackBoundaryId);
    ExitOnFailure(hr, "Failed to read rollback boundary id of OnPlanRollbackBoundary args.");

    args.wzRollbackBoundaryId = sczRollbackBoundaryId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.fRecommendedTransaction));
    ExitOnFailure(hr, "Failed to read recommended transaction of OnPlanRollbackBoundary args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanRollbackBoundary results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.fTransaction));
    ExitOnFailure(hr, "Failed to read transaction of OnPlanRollbackBoundary results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANROLLBACKBOUNDARY, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanRollbackBoundary(args.wzRollbackBoundaryId, args.fRecommendedTransaction, &results.fTransaction, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANROLLBACKBOUNDARY, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanRollbackBoundary failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanRollbackBoundary struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fTransaction);
    ExitOnFailure(hr, "Failed to write transaction of OnPlanRollbackBoundary struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanRollbackBoundary struct.");

LExit:
    ReleaseStr(sczRollbackBoundaryId);
    return hr;
}

static HRESULT OnPlanPatchTarget(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPATCHTARGET_ARGS args = { };
    BA_ONPLANPATCHTARGET_RESULTS results = { };
    LPWSTR sczPackageId = NULL;
    LPWSTR sczProductCode = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanPatchTarget args.");

    hr = BuffReaderReadString(pReaderArgs, &sczPackageId);
    ExitOnFailure(hr, "Failed to read package id of OnPlanPatchTarget args.");

    args.wzPackageId = sczPackageId;

    hr = BuffReaderReadString(pReaderArgs, &sczProductCode);
    ExitOnFailure(hr, "Failed to read product code of OnPlanPatchTarget args.");

    args.wzProductCode = sczProductCode;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedState));
    ExitOnFailure(hr, "Failed to read recommended state transaction of OnPlanPatchTarget args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnPlanPatchTarget results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.requestedState));
    ExitOnFailure(hr, "Failed to read requested state of OnPlanPatchTarget results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPATCHTARGET, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnPlanPatchTarget(args.wzPackageId, args.wzProductCode, args.recommendedState, &results.requestedState, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPATCHTARGET, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnPlanPatchTarget failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnPlanPatchTarget struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnPlanPatchTarget struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.requestedState);
    ExitOnFailure(hr, "Failed to write transaction of OnPlanPatchTarget struct.");

LExit:
    ReleaseStr(sczProductCode);
    ReleaseStr(sczPackageId);
    return hr;
}

static HRESULT OnProgress(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONPROGRESS_ARGS args = { };
    BA_ONPROGRESS_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwProgressPercentage);
    ExitOnFailure(hr, "Failed to read progress of OnProgress args.");

    hr = BuffReaderReadNumber(pReaderArgs, &args.dwOverallPercentage);
    ExitOnFailure(hr, "Failed to read overall progress of OnProgress args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnProgress results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPROGRESS, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnProgress(args.dwProgressPercentage, args.dwOverallPercentage, &results.fCancel);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPROGRESS, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnProgress failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnProgress struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnProgress struct.");

LExit:
    return hr;
}

static HRESULT OnRegisterBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONREGISTERBEGIN_ARGS args = { };
    BA_ONREGISTERBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnRegisterBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedRegistrationType));
    ExitOnFailure(hr, "Failed to read recommended registration type of OnRegisterBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnRegisterBegin results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.registrationType));
    ExitOnFailure(hr, "Failed to read registration type of OnRegisterBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnRegisterBegin(args.recommendedRegistrationType, &results.fCancel, &results.registrationType);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnRegisterBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnRegisterBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.fCancel);
    ExitOnFailure(hr, "Failed to write cancel of OnRegisterBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.registrationType);
    ExitOnFailure(hr, "Failed to write registration type of OnRegisterBegin struct.");

LExit:
    return hr;
}

static HRESULT OnRegisterComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONREGISTERCOMPLETE_ARGS args = { };
    BA_ONREGISTERCOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnRegisterComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnRegisterComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnRegisterComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnRegisterComplete(args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnRegisterComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnRegisterComplete struct.");

LExit:
    return hr;
}

static HRESULT OnRollbackMsiTransactionBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONROLLBACKMSITRANSACTIONBEGIN_ARGS args = { };
    BA_ONROLLBACKMSITRANSACTIONBEGIN_RESULTS results = { };
    LPWSTR sczTransactionId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnRollbackMsiTransactionBegin args.");

    hr = BuffReaderReadString(pReaderArgs, &sczTransactionId);
    ExitOnFailure(hr, "Failed to read transaction id of OnRollbackMsiTransactionBegin args.");

    args.wzTransactionId = sczTransactionId;

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnRollbackMsiTransactionBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnRollbackMsiTransactionBegin(args.wzTransactionId);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnRollbackMsiTransactionBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnRollbackMsiTransactionBegin struct.");

LExit:
    ReleaseStr(sczTransactionId);
    return hr;
}

static HRESULT OnRollbackMsiTransactionComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONROLLBACKMSITRANSACTIONCOMPLETE_ARGS args = { };
    BA_ONROLLBACKMSITRANSACTIONCOMPLETE_RESULTS results = { };
    LPWSTR sczTransactionId = NULL;

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnRollbackMsiTransactionComplete args.");

    hr = BuffReaderReadString(pReaderArgs, &sczTransactionId);
    ExitOnFailure(hr, "Failed to read transaction id of OnRollbackMsiTransactionComplete args.");

    args.wzTransactionId = sczTransactionId;

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnRollbackMsiTransactionComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.restart));
    ExitOnFailure(hr, "Failed to read restart of OnRollbackMsiTransactionComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendation));
    ExitOnFailure(hr, "Failed to read recommendation of OnRollbackMsiTransactionComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnRollbackMsiTransactionComplete results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnRollbackMsiTransactionComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnRollbackMsiTransactionComplete(args.wzTransactionId, args.hrStatus, args.restart, args.recommendation, &results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnRollbackMsiTransactionComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnRollbackMsiTransactionComplete struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnRollbackMsiTransactionComplete struct.");

LExit:
    ReleaseStr(sczTransactionId);
    return hr;
}

static HRESULT OnShutdown(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONSHUTDOWN_ARGS args = { };
    BA_ONSHUTDOWN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnShutdown args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnShutdown results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.action));
    ExitOnFailure(hr, "Failed to read action of OnShutdown results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnShutdown(&results.action);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnStartup failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnShutdown struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.action);
    ExitOnFailure(hr, "Failed to write action of OnShutdown struct.");

LExit:
    return hr;
}

static HRESULT OnStartup(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONSTARTUP_ARGS args = { };
    BA_ONSTARTUP_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnStartup args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnStartup results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnStartup();
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnStartup failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnStartup struct.");

LExit:
    return hr;
}

static HRESULT OnSystemRestorePointBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONSYSTEMRESTOREPOINTBEGIN_ARGS args = { };
    BA_ONSYSTEMRESTOREPOINTBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnSystemRestorePointBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnSystemRestorePointBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnSystemRestorePointBegin();
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnSystemRestorePointBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnSystemRestorePointBegin struct.");

LExit:
    return hr;
}

static HRESULT OnSystemRestorePointComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONSYSTEMRESTOREPOINTCOMPLETE_ARGS args = { };
    BA_ONSYSTEMRESTOREPOINTCOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnSystemRestorePointComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnSystemRestorePointComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnSystemRestorePointComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnSystemRestorePointComplete(args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnSystemRestorePointComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnSystemRestorePointComplete struct.");

LExit:
    return hr;
}

static HRESULT OnUnregisterBegin(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONUNREGISTERBEGIN_ARGS args = { };
    BA_ONUNREGISTERBEGIN_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnUnregisterBegin args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.recommendedRegistrationType));
    ExitOnFailure(hr, "Failed to read recommended registration type of OnUnregisterBegin args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnUnregisterBegin results.");

    hr = BuffReaderReadNumber(pReaderResults, reinterpret_cast<DWORD*>(&results.registrationType));
    ExitOnFailure(hr, "Failed to read registration type of OnUnregisterBegin results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERBEGIN, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnUnregisterBegin(args.recommendedRegistrationType, &results.registrationType);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERBEGIN, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnUnregisterBegin failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnUnregisterBegin struct.");

    hr = BuffWriteNumberToBuffer(pBuffer, results.registrationType);
    ExitOnFailure(hr, "Failed to write registration type of OnUnregisterBegin struct.");

LExit:
    return hr;
}

static HRESULT OnUnregisterComplete(
    __in IBootstrapperApplication* pApplication,
    __in BUFF_READER* pReaderArgs,
    __in BUFF_READER* pReaderResults,
    __in BUFF_BUFFER* pBuffer
    )
{
    HRESULT hr = S_OK;
    BA_ONUNREGISTERCOMPLETE_ARGS args = { };
    BA_ONUNREGISTERCOMPLETE_RESULTS results = { };

    // Read args.
    hr = BuffReaderReadNumber(pReaderArgs, &args.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnUnregisterComplete args.");

    hr = BuffReaderReadNumber(pReaderArgs, reinterpret_cast<DWORD*>(&args.hrStatus));
    ExitOnFailure(hr, "Failed to read status of OnUnregisterComplete args.");

    // Read results.
    hr = BuffReaderReadNumber(pReaderResults, &results.dwApiVersion);
    ExitOnFailure(hr, "Failed to read API version of OnUnregisterComplete results.");

    // Callback.
    hr = pApplication->BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERCOMPLETE, &args, &results);

    if (E_NOTIMPL == hr)
    {
        hr = pApplication->OnUnregisterComplete(args.hrStatus);
    }

    pApplication->BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERCOMPLETE, &args, &results, &hr);
    BalExitOnFailure(hr, "BA OnUnregisterComplete failed.");

    // Write results.
    hr = BuffWriteNumberToBuffer(pBuffer, sizeof(results));
    ExitOnFailure(hr, "Failed to write size of OnUnregisterComplete struct.");

LExit:
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

static HRESULT ProcessMessage(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in IBootstrapperApplication* pApplication,
    __in IBootstrapperEngine* pEngine,
    __in BOOTSTRAPPER_APPLICATION_MESSAGE messageType,
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
        switch (messageType)
        {
            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCREATE:
                hr = OnCreate(pApplication, pEngine, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDESTROY:
                hr = OnDestroy(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP:
                hr = OnStartup(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN:
                hr = OnShutdown(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN:
                hr = OnDetectBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE:
                hr = OnDetectComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE:
                hr = OnDetectForwardCompatibleBundle(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTMSIFEATURE:
                hr = OnDetectMsiFeature(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLE:
                hr = OnDetectRelatedBundle(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGEBEGIN:
                hr = OnDetectPackageBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGECOMPLETE:
                hr = OnDetectPackageComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDMSIPACKAGE:
                hr = OnDetectRelatedMsiPackage(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPATCHTARGET:
                hr = OnDetectPatchTarget(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATEBEGIN:
                hr = OnDetectUpdateBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATE:
                hr = OnDetectUpdate(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATECOMPLETE:
                hr = OnDetectUpdateComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN:
                hr = OnPlanBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE:
                hr = OnPlanComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIFEATURE:
                hr = OnPlanMsiFeature(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN:
                hr = OnPlanPackageBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGECOMPLETE:
                hr = OnPlanPackageComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPATCHTARGET:
                hr = OnPlanPatchTarget(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE:
                hr = OnPlanRelatedBundle(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYBEGIN:
                hr = OnApplyBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATEBEGIN:
                hr = OnElevateBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATECOMPLETE:
                hr = OnElevateComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPROGRESS:
                hr = OnProgress(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONERROR:
                hr = OnError(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERBEGIN:
                hr = OnRegisterBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERCOMPLETE:
                hr = OnRegisterComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEBEGIN:
                hr = OnCacheBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGEBEGIN:
                hr = OnCachePackageBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREBEGIN:
                hr = OnCacheAcquireBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREPROGRESS:
                hr = OnCacheAcquireProgress(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRERESOLVING:
                hr = OnCacheAcquireResolving(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRECOMPLETE:
                hr = OnCacheAcquireComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYBEGIN:
                hr = OnCacheVerifyBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYCOMPLETE:
                hr = OnCacheVerifyComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGECOMPLETE:
                hr = OnCachePackageComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECOMPLETE:
                hr = OnCacheComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEBEGIN:
                hr = OnExecuteBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGEBEGIN:
                hr = OnExecutePackageBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPATCHTARGET:
                hr = OnExecutePatchTarget(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROGRESS:
                hr = OnExecuteProgress(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEMSIMESSAGE:
                hr = OnExecuteMsiMessage(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEFILESINUSE:
                hr = OnExecuteFilesInUse(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGECOMPLETE:
                hr = OnExecutePackageComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTECOMPLETE:
                hr = OnExecuteComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERBEGIN:
                hr = OnUnregisterBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERCOMPLETE:
                hr = OnUnregisterComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYCOMPLETE:
                hr = OnApplyComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXEBEGIN:
                hr = OnLaunchApprovedExeBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXECOMPLETE:
                hr = OnLaunchApprovedExeComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIPACKAGE:
                hr = OnPlanMsiPackage(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONBEGIN:
                hr = OnBeginMsiTransactionBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONCOMPLETE:
                hr = OnBeginMsiTransactionComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONBEGIN:
                hr = OnCommitMsiTransactionBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONCOMPLETE:
                hr = OnCommitMsiTransactionComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONBEGIN:
                hr = OnRollbackMsiTransactionBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONCOMPLETE:
                hr = OnRollbackMsiTransactionComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESBEGIN:
                hr = OnPauseAutomaticUpdatesBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESCOMPLETE:
                hr = OnPauseAutomaticUpdatesComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTBEGIN:
                hr = OnSystemRestorePointBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTCOMPLETE:
                hr = OnSystemRestorePointComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDPACKAGE:
                hr = OnPlannedPackage(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANFORWARDCOMPATIBLEBUNDLE:
                hr = OnPlanForwardCompatibleBundle(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYPROGRESS:
                hr = OnCacheVerifyProgress(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYBEGIN:
                hr = OnCacheContainerOrPayloadVerifyBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE:
                hr = OnCacheContainerOrPayloadVerifyComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS:
                hr = OnCacheContainerOrPayloadVerifyProgress(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTBEGIN:
                hr = OnCachePayloadExtractBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTCOMPLETE:
                hr = OnCachePayloadExtractComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTPROGRESS:
                hr = OnCachePayloadExtractProgress(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANROLLBACKBOUNDARY:
                hr = OnPlanRollbackBoundary(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE:
                hr = OnDetectCompatibleMsiPackage(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN:
                hr = OnPlanCompatibleMsiPackageBegin(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE:
                hr = OnPlanCompatibleMsiPackageComplete(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDCOMPATIBLEPACKAGE:
                hr = OnPlannedCompatiblePackage(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRESTORERELATEDBUNDLE:
                hr = OnPlanRestoreRelatedBundle(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLETYPE:
                hr = OnPlanRelatedBundleType(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYDOWNGRADE:
                hr = OnApplyDowngrade(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROCESSCANCEL:
                hr = OnExecuteProcessCancel(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLEPACKAGE:
                hr = OnDetectRelatedBundlePackage(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGENONVITALVALIDATIONFAILURE:
                hr = OnCachePackageNonVitalValidationFailure(pApplication, &readerArgs, &readerResults, &bufferResponse);
                break;

            default:
                hr = E_NOTIMPL;
                break;
                // BalExitWithRootFailure(hr, E_NOTIMPL, "Unknown message type %d sent to bootstrapper application.", messageType)
        }
    }

    hr = PipeRpcResponse(phRpcPipe, messageType, hr, bufferResponse.pbData, bufferResponse.cbData);
    BalExitOnFailure(hr, "Failed to send bootstrapper application callback result to engine.");

LExit:
    ReleaseBuffer(bufferResponse);

    return hr;
}

EXTERN_C HRESULT MsgPump(
    __in HANDLE hPipe,
    __in IBootstrapperApplication* pApplication,
    __in IBootstrapperEngine* pEngine
    )
{
    HRESULT hr = S_OK;
    PIPE_RPC_HANDLE hRpcPipe = { INVALID_HANDLE_VALUE };
    PIPE_MESSAGE msg = { };

    PipeRpcInitialize(&hRpcPipe, hPipe, FALSE);

    // Pump messages sent to bootstrapper application until the pipe is closed.
    while (S_OK == (hr = PipeRpcReadMessage(&hRpcPipe, &msg)))
    {
        ProcessMessage(&hRpcPipe, pApplication, pEngine, static_cast<BOOTSTRAPPER_APPLICATION_MESSAGE>(msg.dwMessageType), reinterpret_cast<LPCBYTE>(msg.pvData), msg.cbData);

        ReleasePipeMessage(&msg);
    }
    BalExitOnFailure(hr, "Failed to get message over bootstrapper application pipe");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleasePipeMessage(&msg);

    PipeRpcUninitiailize(&hRpcPipe);

    return hr;
}
