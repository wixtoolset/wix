// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// internal function declarations

static int FilterResult(
    __in DWORD dwAllowedResults,
    __in int nResult
    );

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
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    );

static HRESULT SendBAMessageFromInactiveEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    );


// function definitions

/*******************************************************************
 UserExperienceParseFromXml - 

*******************************************************************/
extern "C" HRESULT UserExperienceParseFromXml(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixnUserExperienceNode = NULL;

    // select UX node
    hr = XmlSelectSingleNode(pixnBundle, L"UX", &pixnUserExperienceNode);
    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
    }
    ExitOnFailure(hr, "Failed to select user experience node.");

    // parse splash screen
    hr = XmlGetYesNoAttribute(pixnUserExperienceNode, L"SplashScreen", &pUserExperience->fSplashScreen);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to to get UX/@SplashScreen");
    }

    // parse payloads
    hr = PayloadsParseFromXml(&pUserExperience->payloads, NULL, NULL, pixnUserExperienceNode);
    ExitOnFailure(hr, "Failed to parse user experience payloads.");

    // make sure we have at least one payload
    if (0 == pUserExperience->payloads.cPayloads)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Too few UX payloads.");
    }

LExit:
    ReleaseObject(pixnUserExperienceNode);

    return hr;
}

/*******************************************************************
 UserExperienceUninitialize - 

*******************************************************************/
extern "C" void UserExperienceUninitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    ReleaseStr(pUserExperience->sczTempDirectory);
    PayloadsUninitialize(&pUserExperience->payloads);

    // clear struct
    memset(pUserExperience, 0, sizeof(BURN_USER_EXPERIENCE));
}

/*******************************************************************
 UserExperienceLoad - 

*******************************************************************/
extern "C" HRESULT UserExperienceLoad(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in BOOTSTRAPPER_COMMAND* pCommand
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_CREATE_ARGS args = { };
    BOOTSTRAPPER_CREATE_RESULTS results = { };

    args.cbSize = sizeof(BOOTSTRAPPER_CREATE_ARGS);
    args.pCommand = pCommand;
    args.pfnBootstrapperEngineProc = EngineForApplicationProc;
    args.pvBootstrapperEngineProcContext = pEngineContext;
    args.qwEngineAPIVersion = MAKEQWORDVERSION(2021, 4, 14, 0);

    results.cbSize = sizeof(BOOTSTRAPPER_CREATE_RESULTS);

    // Load BA DLL.
    pUserExperience->hUXModule = ::LoadLibraryExW(pUserExperience->payloads.rgPayloads[0].sczLocalFilePath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    ExitOnNullWithLastError(pUserExperience->hUXModule, hr, "Failed to load BA DLL.");

    // Get BootstrapperApplicationCreate entry-point.
    PFN_BOOTSTRAPPER_APPLICATION_CREATE pfnCreate = (PFN_BOOTSTRAPPER_APPLICATION_CREATE)::GetProcAddress(pUserExperience->hUXModule, "BootstrapperApplicationCreate");
    ExitOnNullWithLastError(pfnCreate, hr, "Failed to get BootstrapperApplicationCreate entry-point");

    // Create BA.
    hr = pfnCreate(&args, &results);
    ExitOnFailure(hr, "Failed to create BA.");

    pUserExperience->pfnBAProc = results.pfnBootstrapperApplicationProc;
    pUserExperience->pvBAProcContext = results.pvBootstrapperApplicationProcContext;
    pUserExperience->fDisableUnloading = results.fDisableUnloading;

LExit:
    return hr;
}

/*******************************************************************
 UserExperienceUnload - 

*******************************************************************/
extern "C" HRESULT UserExperienceUnload(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;

    if (pUserExperience->hUXModule)
    {
        // Get BootstrapperApplicationDestroy entry-point and call it if it exists.
        PFN_BOOTSTRAPPER_APPLICATION_DESTROY pfnDestroy = (PFN_BOOTSTRAPPER_APPLICATION_DESTROY)::GetProcAddress(pUserExperience->hUXModule, "BootstrapperApplicationDestroy");
        if (pfnDestroy)
        {
            pfnDestroy();
        }

        // Free BA DLL if it supports it.
        if (!pUserExperience->fDisableUnloading && !::FreeLibrary(pUserExperience->hUXModule))
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            TraceError(hr, "Failed to unload BA DLL.");
        }
        pUserExperience->hUXModule = NULL;
    }

//LExit:
    return hr;
}

extern "C" HRESULT UserExperienceEnsureWorkingFolder(
    __in LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczUserExperienceWorkingFolder
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;

    hr = CacheEnsureWorkingFolder(wzBundleId, &sczWorkingFolder);
    ExitOnFailure(hr, "Failed to create working folder.");

    hr = StrAllocFormatted(psczUserExperienceWorkingFolder, L"%ls%ls\\", sczWorkingFolder, L".ba");
    ExitOnFailure(hr, "Failed to calculate the bootstrapper application working path.");

    hr = DirEnsureExists(*psczUserExperienceWorkingFolder, NULL);
    ExitOnFailure(hr, "Failed create bootstrapper application working folder.");

LExit:
    ReleaseStr(sczWorkingFolder);

    return hr;
}


extern "C" HRESULT UserExperienceRemove(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;

    // Remove temporary UX directory
    if (pUserExperience->sczTempDirectory)
    {
        hr = DirEnsureDeleteEx(pUserExperience->sczTempDirectory, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
        TraceError(hr, "Could not delete bootstrapper application folder. Some files will be left in the temp folder.");
    }

//LExit:
    return hr;
}

extern "C" int UserExperienceSendError(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in HRESULT hrCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD uiFlags,
    __in int nRecommendation
    )
{
    int nResult = nRecommendation;
    DWORD dwCode = HRESULT_CODE(hrCode);
    LPWSTR sczError = NULL;

    // If no error string was provided, try to get the error string from the HRESULT.
    if (!wzError)
    {
        if (SUCCEEDED(StrAllocFromError(&sczError, hrCode, NULL)))
        {
            wzError = sczError;
        }
    }

    UserExperienceOnError(pUserExperience, errorType, wzPackageId, dwCode, wzError, uiFlags, 0, NULL, &nResult); // ignore return value.

    ReleaseStr(sczError);
    return nResult;
}

extern "C" void UserExperienceActivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    ::EnterCriticalSection(&pUserExperience->csEngineActive);
    AssertSz(!pUserExperience->fEngineActive, "Engine should have been deactivated before activating it.");
    pUserExperience->fEngineActive = TRUE;
    ::LeaveCriticalSection(&pUserExperience->csEngineActive);
}

extern "C" void UserExperienceDeactivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    ::EnterCriticalSection(&pUserExperience->csEngineActive);
    AssertSz(pUserExperience->fEngineActive, "Engine should have been active before deactivating it.");
    pUserExperience->fEngineActive = FALSE;
    ::LeaveCriticalSection(&pUserExperience->csEngineActive);
}

extern "C" HRESULT UserExperienceEnsureEngineInactive(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    // Make a slight optimization here by ignoring the critical section, because all callers should have needed to enter it for their operation anyway.
    HRESULT hr = pUserExperience->fEngineActive ? HRESULT_FROM_WIN32(ERROR_BUSY) : S_OK;
    ExitOnRootFailure(hr, "Engine is active, cannot proceed.");

LExit:
    return hr;
}

extern "C" void UserExperienceExecuteReset(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    pUserExperience->hrApplyError = S_OK;
    pUserExperience->hwndApply = NULL;
}

extern "C" void UserExperienceExecutePhaseComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrResult
    )
{
    if (FAILED(hrResult))
    {
        pUserExperience->hrApplyError = hrResult;
    }
}

EXTERN_C BAAPI UserExperienceOnApplyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwPhaseCount
    )
{
    HRESULT hr = S_OK;
    BA_ONAPPLYBEGIN_ARGS args = { };
    BA_ONAPPLYBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.dwPhaseCount = dwPhaseCount;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnApplyBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnApplyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONAPPLYCOMPLETE_ARGS args = { };
    BA_ONAPPLYCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;
    args.restart = restart;
    args.recommendation = *pAction;

    results.cbSize = sizeof(results);
    results.action = *pAction;

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnApplyComplete failed.");

    *pAction = results.action;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnBeginMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    )
{
    HRESULT hr = S_OK;
    BA_ONBEGINMSITRANSACTIONBEGIN_ARGS args = { };
    BA_ONBEGINMSITRANSACTIONBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzTransactionId = wzTransactionId;

    results.cbSize = sizeof(results);
    
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnBeginMsiTransactionBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnBeginMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONBEGINMSITRANSACTIONCOMPLETE_ARGS args = { };
    BA_ONBEGINMSITRANSACTIONCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzTransactionId = wzTransactionId;
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnBeginMsiTransactionComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheAcquireBegin(
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
    *pCacheOperation = BOOTSTRAPPER_CACHE_OPERATION_NONE;

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.wzSource = *pwzSource;
    args.wzDownloadUrl = *pwzDownloadUrl;
    args.wzPayloadContainerId = wzPayloadContainerId;
    args.recommendation = *pCacheOperation;

    results.cbSize = sizeof(results);
    results.action = *pCacheOperation;

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnCacheAcquireBegin failed.");

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
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheAcquireComplete(
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

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.hrStatus = hrStatus;
    args.recommendation = *pfRetry ? BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION_RETRY : BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION_NONE;

    results.cbSize = sizeof(results);
    results.action = args.recommendation;

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnCacheAcquireComplete failed.");

    if (FAILED(hrStatus))
    {
        *pfRetry = BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION_RETRY == results.action;
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheAcquireProgress(
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

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.dw64Progress = dw64Progress;
    args.dw64Total = dw64Total;
    args.dwOverallPercentage = dwOverallPercentage;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREPROGRESS, &args, &results);
    ExitOnFailure(hr, "BA OnCacheAcquireProgress failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheAcquireResolving(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z LPWSTR* rgSearchPaths,
    __in DWORD cSearchPaths,
    __in BOOL fFoundLocal,
    __in DWORD* pdwChosenSearchPath,
    __in_z_opt LPCWSTR wzDownloadUrl,
    __in_z_opt LPCWSTR wzPayloadContainerId,
    __inout BOOTSTRAPPER_CACHE_OPERATION* pCacheOperation
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEACQUIRERESOLVING_ARGS args = { };
    BA_ONCACHEACQUIRERESOLVING_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.rgSearchPaths = const_cast<LPCWSTR*>(rgSearchPaths);
    args.cSearchPaths = cSearchPaths;
    args.fFoundLocal = fFoundLocal;
    args.dwRecommendedSearchPath = *pdwChosenSearchPath;
    args.wzDownloadUrl = wzDownloadUrl;
    args.recommendation = *pCacheOperation;

    results.cbSize = sizeof(results);
    results.dwChosenSearchPath = *pdwChosenSearchPath;
    results.action = *pCacheOperation;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRERESOLVING, &args, &results);
    ExitOnFailure(hr, "BA OnCacheAcquireResolving failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    else
    {
        // Verify the BA requested an action that is possible.
        if (BOOTSTRAPPER_CACHE_OPERATION_DOWNLOAD == results.action && wzDownloadUrl && *wzDownloadUrl ||
            BOOTSTRAPPER_CACHE_OPERATION_EXTRACT == results.action && wzPayloadContainerId ||
            BOOTSTRAPPER_CACHE_OPERATION_NONE == results.action)
        {
            *pCacheOperation = results.action;
        }
        else if (BOOTSTRAPPER_CACHE_OPERATION_COPY == results.action && results.dwChosenSearchPath < cSearchPaths)
        {
            *pdwChosenSearchPath = results.dwChosenSearchPath;
            *pCacheOperation = results.action;
        }
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEBEGIN_ARGS args = { };
    BA_ONCACHEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnCacheBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECOMPLETE_ARGS args = { };
    BA_ONCACHECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnCacheComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheContainerOrPayloadVerifyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_ARGS args = { };
    BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnCacheContainerOrPayloadVerifyBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheContainerOrPayloadVerifyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_ARGS args = { };
    BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnCacheContainerOrPayloadVerifyComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheContainerOrPayloadVerifyProgress(
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

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.dw64Progress = dw64Progress;
    args.dw64Total = dw64Total;
    args.dwOverallPercentage = dwOverallPercentage;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS, &args, &results);
    ExitOnFailure(hr, "BA OnCacheContainerOrPayloadVerifyProgress failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCachePackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cCachePayloads,
    __in DWORD64 dw64PackageCacheSize
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPACKAGEBEGIN_ARGS args = { };
    BA_ONCACHEPACKAGEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.cCachePayloads = cCachePayloads;
    args.dw64PackageCacheSize = dw64PackageCacheSize;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGEBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnCachePackageBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCachePackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPACKAGECOMPLETE_ARGS args = { };
    BA_ONCACHEPACKAGECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.recommendation = *pAction;

    results.cbSize = sizeof(results);
    results.action = *pAction;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnCachePackageComplete failed.");

    if (FAILED(hrStatus))
    {
        *pAction = results.action;
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCachePayloadExtractBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPAYLOADEXTRACTBEGIN_ARGS args = { };
    BA_ONCACHEPAYLOADEXTRACTBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzContainerId = wzContainerId;
    args.wzPayloadId = wzPayloadId;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnCachePayloadExtractBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCachePayloadExtractComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEPAYLOADEXTRACTCOMPLETE_ARGS args = { };
    BA_ONCACHEPAYLOADEXTRACTCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzContainerId = wzContainerId;
    args.wzPayloadId = wzPayloadId;
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnCachePayloadExtractComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCachePayloadExtractProgress(
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

    args.cbSize = sizeof(args);
    args.wzContainerId = wzContainerId;
    args.wzPayloadId = wzPayloadId;
    args.dw64Progress = dw64Progress;
    args.dw64Total = dw64Total;
    args.dwOverallPercentage = dwOverallPercentage;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTPROGRESS, &args, &results);
    ExitOnFailure(hr, "BA OnCachePayloadExtractProgress failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheVerifyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEVERIFYBEGIN_ARGS args = { };
    BA_ONCACHEVERIFYBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnCacheVerifyBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheVerifyComplete(
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

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.hrStatus = hrStatus;
    args.recommendation = *pAction;

    results.cbSize = sizeof(results);
    results.action = *pAction;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnCacheVerifyComplete failed.");

    if (FAILED(hrStatus))
    {
        *pAction = results.action;
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCacheVerifyProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    )
{
    HRESULT hr = S_OK;
    BA_ONCACHEVERIFYPROGRESS_ARGS args = { };
    BA_ONCACHEVERIFYPROGRESS_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageOrContainerId = wzPackageOrContainerId;
    args.wzPayloadId = wzPayloadId;
    args.dw64Progress = dw64Progress;
    args.dw64Total = dw64Total;
    args.dwOverallPercentage = dwOverallPercentage;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYPROGRESS, &args, &results);
    ExitOnFailure(hr, "BA OnCacheVerifyProgress failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCommitMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    )
{
    HRESULT hr = S_OK;
    BA_ONCOMMITMSITRANSACTIONBEGIN_ARGS args = { };
    BA_ONCOMMITMSITRANSACTIONBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzTransactionId = wzTransactionId;

    results.cbSize = sizeof(results);
    
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnCommitMsiTransactionBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnCommitMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONCOMMITMSITRANSACTIONCOMPLETE_ARGS args = { };
    BA_ONCOMMITMSITRANSACTIONCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzTransactionId = wzTransactionId;
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnCommitMsiTransactionComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fCached,
    __in BOOL fInstalled,
    __in DWORD cPackages
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTBEGIN_ARGS args = { };
    BA_ONDETECTBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.cPackages = cPackages;
    args.fInstalled = fInstalled;
    args.fCached = fCached;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnDetectBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOL fEligibleForCleanup
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTCOMPLETE_ARGS args = { };
    BA_ONDETECTCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;
    args.fEligibleForCleanup = fEligibleForCleanup;

    results.cbSize = sizeof(results);

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnDetectComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
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

    args.cbSize = sizeof(args);
    args.wzBundleId = wzBundleId;
    args.relationType = relationType;
    args.wzBundleTag = wzBundleTag;
    args.fPerMachine = fPerMachine;
    args.wzVersion = pVersion->sczVersion;
    args.fMissingFromCache = fMissingFromCache;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE, &args, &results);
    ExitOnFailure(hr, "BA OnDetectForwardCompatibleBundle failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __in BOOTSTRAPPER_FEATURE_STATE state
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTMSIFEATURE_ARGS args = { };
    BA_ONDETECTMSIFEATURE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzFeatureId = wzFeatureId;
    args.state = state;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTMSIFEATURE, &args, &results);
    ExitOnFailure(hr, "BA OnDetectMsiFeature failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPACKAGEBEGIN_ARGS args = { };
    BA_ONDETECTPACKAGEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGEBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnDetectPackageBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPACKAGECOMPLETE_ARGS args = { };
    BA_ONDETECTPACKAGECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.state = state;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnDetectPackageComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOTSTRAPPER_RELATED_OPERATION operation,
    __in BOOL fMissingFromCache
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTRELATEDBUNDLE_ARGS args = { };
    BA_ONDETECTRELATEDBUNDLE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzBundleId = wzBundleId;
    args.relationType = relationType;
    args.wzBundleTag = wzBundleTag;
    args.fPerMachine = fPerMachine;
    args.wzVersion = pVersion->sczVersion;
    args.operation = operation;
    args.fMissingFromCache = fMissingFromCache;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLE, &args, &results);
    ExitOnFailure(hr, "BA OnDetectRelatedBundle failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectRelatedMsiPackage(
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

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzUpgradeCode = wzUpgradeCode;
    args.wzProductCode = wzProductCode;
    args.fPerMachine = fPerMachine;
    args.wzVersion = pVersion->sczVersion;
    args.operation = operation;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDMSIPACKAGE, &args, &results);
    ExitOnFailure(hr, "BA OnDetectRelatedMsiPackage failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectPatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __in BOOTSTRAPPER_PACKAGE_STATE patchState
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPATCHTARGET_ARGS args = { };
    BA_ONDETECTPATCHTARGET_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzProductCode = wzProductCode;
    args.patchState = patchState;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPATCHTARGET, &args, &results);
    ExitOnFailure(hr, "BA OnDetectPatchTarget failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectUpdate(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzUpdateLocation,
    __in DWORD64 dw64Size,
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

    args.cbSize = sizeof(args);
    args.wzUpdateLocation = wzUpdateLocation;
    args.dw64Size = dw64Size;
    args.wzVersion = pVersion->sczVersion;
    args.wzTitle = wzTitle;
    args.wzSummary = wzSummary;
    args.wzContentType = wzContentType;
    args.wzContent = wzContent;

    results.cbSize = sizeof(results);
    results.fStopProcessingUpdates = *pfStopProcessingUpdates;

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATE, &args, &results);
    ExitOnFailure(hr, "BA OnDetectUpdate failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pfStopProcessingUpdates = results.fStopProcessingUpdates;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectUpdateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzUpdateLocation,
    __inout BOOL* pfSkip
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATEBEGIN_ARGS args = { };
    BA_ONDETECTUPDATEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzUpdateLocation = wzUpdateLocation;

    results.cbSize = sizeof(results);
    results.fSkip = *pfSkip;

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATEBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnDetectUpdateBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pfSkip = results.fSkip;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectUpdateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __inout BOOL* pfIgnoreError
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATECOMPLETE_ARGS args = { };
    BA_ONDETECTUPDATECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);
    results.fIgnoreError = *pfIgnoreError;

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnDetectUpdateComplete failed.");

    if (FAILED(hrStatus))
    {
        *pfIgnoreError = results.fIgnoreError;
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnElevateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONELEVATEBEGIN_ARGS args = { };
    BA_ONELEVATEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATEBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnElevateBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnElevateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONELEVATECOMPLETE_ARGS args = { };
    BA_ONELEVATECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnElevateComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnError(
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

    args.cbSize = sizeof(args);
    args.errorType = errorType;
    args.wzPackageId = wzPackageId;
    args.dwCode = dwCode;
    args.wzError = wzError;
    args.dwUIHint = dwUIHint;
    args.cData = cData;
    args.rgwzData = rgwzData;
    args.nRecommendation = *pnResult;

    results.cbSize = sizeof(results);
    results.nResult = *pnResult;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONERROR, &args, &results);
    ExitOnFailure(hr, "BA OnError failed.");

    *pnResult = results.nResult;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnExecuteBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cExecutingPackages
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEBEGIN_ARGS args = { };
    BA_ONEXECUTEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.cExecutingPackages = cExecutingPackages;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnExecuteBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnExecuteComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTECOMPLETE_ARGS args = { };
    BA_ONEXECUTECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnExecuteComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnExecuteFilesInUse(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cFiles,
    __in_ecount_z_opt(cFiles) LPCWSTR* rgwzFiles,
    __inout int* pnResult
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEFILESINUSE_ARGS args = { };
    BA_ONEXECUTEFILESINUSE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.cFiles = cFiles;
    args.rgwzFiles = rgwzFiles;
    args.nRecommendation = *pnResult;

    results.cbSize = sizeof(results);
    results.nResult = *pnResult;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEFILESINUSE, &args, &results);
    ExitOnFailure(hr, "BA OnExecuteFilesInUse failed.");

    *pnResult = results.nResult;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnExecuteMsiMessage(
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

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.messageType = messageType;
    args.dwUIHint = dwUIHint;
    args.wzMessage = wzMessage;
    args.cData = cData;
    args.rgwzData = rgwzData;
    args.nRecommendation = *pnResult;

    results.cbSize = sizeof(results);
    results.nResult = *pnResult;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEMSIMESSAGE, &args, &results);
    ExitOnFailure(hr, "BA OnExecuteMsiMessage failed.");

    *pnResult = results.nResult;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnExecutePackageBegin(
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

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.fExecute = fExecute;
    args.action = action;
    args.uiLevel = uiLevel;
    args.fDisableExternalUiHandler = fDisableExternalUiHandler;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGEBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnExecutePackageBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnExecutePackageComplete(
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

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.restart = restart;
    args.recommendation = *pAction;

    results.cbSize = sizeof(results);
    results.action = *pAction;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnExecutePackageComplete failed.");

    *pAction = results.action;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnExecutePatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzTargetProductCode
    )
{
    HRESULT hr = S_OK;
    BA_ONEXECUTEPATCHTARGET_ARGS args = { };
    BA_ONEXECUTEPATCHTARGET_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzTargetProductCode = wzTargetProductCode;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPATCHTARGET, &args, &results);
    ExitOnFailure(hr, "BA OnExecutePatchTarget failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnExecuteProgress(
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

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.dwProgressPercentage = dwProgressPercentage;
    args.dwOverallPercentage = dwOverallPercentage;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROGRESS, &args, &results);
    ExitOnFailure(hr, "BA OnExecuteProgress failed.");

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
    return hr;
}

EXTERN_C BAAPI UserExperienceOnLaunchApprovedExeBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONLAUNCHAPPROVEDEXEBEGIN_ARGS args = { };
    BA_ONLAUNCHAPPROVEDEXEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXEBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnLaunchApprovedExeBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnLaunchApprovedExeComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in DWORD dwProcessId
    )
{
    HRESULT hr = S_OK;
    BA_ONLAUNCHAPPROVEDEXECOMPLETE_ARGS args = { };
    BA_ONLAUNCHAPPROVEDEXECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;
    args.dwProcessId = dwProcessId;

    results.cbSize = sizeof(results);

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnLaunchApprovedExeComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPauseAUBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONPAUSEAUTOMATICUPDATESBEGIN_ARGS args = { };
    BA_ONPAUSEAUTOMATICUPDATESBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);
    
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnPauseAUBegin failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPauseAUComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_ARGS args = { };
    BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnPauseAUComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cPackages
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANBEGIN_ARGS args = { };
    BA_ONPLANBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.cPackages = cPackages;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnPlanBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __inout BOOTSTRAPPER_FEATURE_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANMSIFEATURE_ARGS args = { };
    BA_ONPLANMSIFEATURE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzFeatureId = wzFeatureId;
    args.recommendedState = *pRequestedState;

    results.cbSize = sizeof(results);
    results.requestedState = *pRequestedState;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIFEATURE, &args, &results);
    ExitOnFailure(hr, "BA OnPlanMsiFeature failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pRequestedState = results.requestedState;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPLETE_ARGS args = { };
    BA_ONPLANCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessageFromInactiveEngine(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnPlanComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
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

    args.cbSize = sizeof(args);
    args.wzBundleId = wzBundleId;
    args.relationType = relationType;
    args.wzBundleTag = wzBundleTag;
    args.fPerMachine = fPerMachine;
    args.wzVersion = pVersion->sczVersion;
    args.fRecommendedIgnoreBundle = *pfIgnoreBundle;

    results.cbSize = sizeof(results);
    results.fIgnoreBundle = *pfIgnoreBundle;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANFORWARDCOMPATIBLEBUNDLE, &args, &results);
    ExitOnFailure(hr, "BA OnPlanForwardCompatibleBundle failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pfIgnoreBundle = results.fIgnoreBundle;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOL fExecute,
    __in BOOTSTRAPPER_ACTION_STATE action,
    __inout BURN_MSI_PROPERTY* pActionMsiProperty,
    __inout INSTALLUILEVEL* pUiLevel,
    __inout BOOL* pfDisableExternalUiHandler
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANMSIPACKAGE_ARGS args = { };
    BA_ONPLANMSIPACKAGE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.fExecute = fExecute;
    args.action = action;

    results.cbSize = sizeof(results);
    results.actionMsiProperty = *pActionMsiProperty;
    results.uiLevel = *pUiLevel;
    results.fDisableExternalUiHandler = *pfDisableExternalUiHandler;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIPACKAGE, &args, &results);
    ExitOnFailure(hr, "BA OnPlanMsiPackage failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pActionMsiProperty = results.actionMsiProperty;
    *pUiLevel = results.uiLevel;
    *pfDisableExternalUiHandler = results.fDisableExternalUiHandler;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlannedPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOTSTRAPPER_ACTION_STATE execute,
    __in BOOTSTRAPPER_ACTION_STATE rollback
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANNEDPACKAGE_ARGS args = { };
    BA_ONPLANNEDPACKAGE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.execute = execute;
    args.rollback = rollback;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDPACKAGE, &args, &results);
    ExitOnFailure(hr, "BA OnPlannedPackage failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOL fInstallCondition,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPACKAGEBEGIN_ARGS args = { };
    BA_ONPLANPACKAGEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.state = state;
    args.fInstallCondition = fInstallCondition;
    args.recommendedState = *pRequestedState;

    results.cbSize = sizeof(results);
    results.requestedState = *pRequestedState;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnPlanPackageBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pRequestedState = results.requestedState;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_REQUEST_STATE requested
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPACKAGECOMPLETE_ARGS args = { };
    BA_ONPLANPACKAGECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.requested = requested;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGECOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnPlanPackageComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANRELATEDBUNDLE_ARGS args = { };
    BA_ONPLANRELATEDBUNDLE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzBundleId = wzBundleId;
    args.recommendedState = *pRequestedState;

    results.cbSize = sizeof(results);
    results.requestedState = *pRequestedState;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE, &args, &results);
    ExitOnFailure(hr, "BA OnPlanRelatedBundle failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pRequestedState = results.requestedState;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanPatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPATCHTARGET_ARGS args = { };
    BA_ONPLANPATCHTARGET_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzProductCode = wzProductCode;
    args.recommendedState = *pRequestedState;

    results.cbSize = sizeof(results);
    results.requestedState = *pRequestedState;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPATCHTARGET, &args, &results);
    ExitOnFailure(hr, "BA OnPlanPatchTarget failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pRequestedState = results.requestedState;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwProgressPercentage,
    __in DWORD dwOverallPercentage
    )
{
    HRESULT hr = S_OK;
    BA_ONPROGRESS_ARGS args = { };
    BA_ONPROGRESS_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.dwProgressPercentage = dwProgressPercentage;
    args.dwOverallPercentage = dwOverallPercentage;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONPROGRESS, &args, &results);
    hr = FilterExecuteResult(pUserExperience, hr, fRollback, results.fCancel, L"OnProgress");

    return hr;
}

EXTERN_C BAAPI UserExperienceOnRegisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONREGISTERBEGIN_ARGS args = { };
    BA_ONREGISTERBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnRegisterBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnRegisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONREGISTERCOMPLETE_ARGS args = { };
    BA_ONREGISTERCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnRegisterComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnRollbackMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    )
{
    HRESULT hr = S_OK;
    BA_ONROLLBACKMSITRANSACTIONBEGIN_ARGS args = { };
    BA_ONROLLBACKMSITRANSACTIONBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzTransactionId = wzTransactionId;

    results.cbSize = sizeof(results);
    
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnRollbackMsiTransactionBegin failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnRollbackMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONROLLBACKMSITRANSACTIONCOMPLETE_ARGS args = { };
    BA_ONROLLBACKMSITRANSACTIONCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzTransactionId = wzTransactionId;
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnRollbackMsiTransactionComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_SHUTDOWN_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONSHUTDOWN_ARGS args = { };
    BA_ONSHUTDOWN_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);
    results.action = *pAction;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN, &args, &results);
    ExitOnFailure(hr, "BA OnShutdown failed.");

    *pAction = results.action;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnStartup(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONSTARTUP_ARGS args = { };
    BA_ONSTARTUP_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP, &args, &results);
    ExitOnFailure(hr, "BA OnStartup failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnSystemRestorePointBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONSYSTEMRESTOREPOINTBEGIN_ARGS args = { };
    BA_ONSYSTEMRESTOREPOINTBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);
    
    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnSystemRestorePointBegin failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnSystemRestorePointComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONSYSTEMRESTOREPOINTCOMPLETE_ARGS args = { };
    BA_ONSYSTEMRESTOREPOINTCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnSystemRestorePointComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnSystemShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwEndSession,
    __inout BOOL* pfCancel
    )
{
    HRESULT hr = S_OK;
    BA_ONSYSTEMSHUTDOWN_ARGS args = { };
    BA_ONSYSTEMSHUTDOWN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.dwEndSession = dwEndSession;

    results.cbSize = sizeof(results);
    results.fCancel = *pfCancel;

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMSHUTDOWN, &args, &results);
    ExitOnFailure(hr, "BA OnSystemShutdown failed.");
    
    *pfCancel = results.fCancel;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnUnregisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOL* pfKeepRegistration
    )
{
    HRESULT hr = S_OK;
    BA_ONUNREGISTERBEGIN_ARGS args = { };
    BA_ONUNREGISTERBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.fKeepRegistration = *pfKeepRegistration;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERBEGIN, &args, &results);
    ExitOnFailure(hr, "BA OnUnregisterBegin failed.");

    if (!args.fKeepRegistration && results.fForceKeepRegistration)
    {
        *pfKeepRegistration = TRUE;
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnUnregisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONUNREGISTERCOMPLETE_ARGS args = { };
    BA_ONUNREGISTERCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = SendBAMessage(pUserExperience, BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERCOMPLETE, &args, &results);
    ExitOnFailure(hr, "BA OnUnregisterComplete failed.");

LExit:
    return hr;
}

extern "C" int UserExperienceCheckExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    // Do not allow canceling while rolling back.
    if (fRollback && (IDCANCEL == nResult || IDABORT == nResult))
    {
        nResult = IDNOACTION;
    }
    else if (FAILED(pUserExperience->hrApplyError) && !fRollback) // if we failed cancel except not during rollback.
    {
        nResult = IDCANCEL;
    }

    nResult = FilterResult(dwAllowedResults, nResult);
    return nResult;
}

extern "C" HRESULT UserExperienceInterpretExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    HRESULT hr = S_OK;

    // If we failed return that error unless this is rollback which should roll on.
    if (FAILED(pUserExperience->hrApplyError) && !fRollback)
    {
        hr = pUserExperience->hrApplyError;
    }
    else
    {
        int nCheckedResult = UserExperienceCheckExecuteResult(pUserExperience, fRollback, dwAllowedResults, nResult);
        hr = IDOK == nCheckedResult || IDNOACTION == nCheckedResult ? S_OK : IDCANCEL == nCheckedResult || IDABORT == nCheckedResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
    }

    return hr;
}


// internal functions

static int FilterResult(
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    DWORD dwFilteredAllowedResults = dwAllowedResults & MB_TYPEMASK;
    if (IDNOACTION == nResult || IDERROR == nResult) // do nothing and errors pass through.
    {
    }
    else
    {
        switch (dwFilteredAllowedResults)
        {
        case MB_OK:
            nResult = IDOK;
            break;

        case MB_OKCANCEL:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDOK;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult || IDNO == nResult)
            {
                nResult = IDCANCEL;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_ABORTRETRYIGNORE:
            if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDIGNORE == nResult)
            {
                nResult = IDIGNORE;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_YESNO:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDYES;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult || IDNO == nResult)
            {
                nResult = IDNO;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_YESNOCANCEL:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDYES;
            }
            else if (IDNO == nResult)
            {
                nResult = IDNO;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDCANCEL;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_RETRYCANCEL:
            if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_CANCELTRYCONTINUE:
            if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDCONTINUE == nResult || IDIGNORE == nResult)
            {
                nResult = IDCONTINUE;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case WIU_MB_OKIGNORECANCELRETRY: // custom Windows Installer utility return code.
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDOK;
            }
            else if (IDCONTINUE == nResult || IDIGNORE == nResult)
            {
                nResult = IDIGNORE;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDCANCEL;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult || IDNO == nResult)
            {
                nResult = IDRETRY;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_RETRYTRYAGAIN: // custom return code.
            if (IDRETRY != nResult && IDTRYAGAIN != nResult)
            {
                nResult = IDNOACTION;
            }
            break;

        default:
            AssertSz(FALSE, "Unknown allowed results.");
            break;
        }
    }

    return nResult;
}

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
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;

    if (!pUserExperience->hUXModule)
    {
        ExitFunction();
    }

    hr = pUserExperience->pfnBAProc(message, pvArgs, pvResults, pUserExperience->pvBAProcContext);
    if (hr == E_NOTIMPL)
    {
        hr = S_OK;
    }

LExit:
    return hr;
}

static HRESULT SendBAMessageFromInactiveEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;

    if (!pUserExperience->hUXModule)
    {
        ExitFunction();
    }

    UserExperienceDeactivateEngine(pUserExperience);

    hr = SendBAMessage(pUserExperience, message, pvArgs, pvResults);

    UserExperienceActivateEngine(pUserExperience);

LExit:
    return hr;
}
