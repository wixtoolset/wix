// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

typedef struct _DETECT_AUTHENTICATION_REQUIRED_DATA
{
    BURN_USER_EXPERIENCE* pUX;
    LPCWSTR wzPackageOrContainerId;
} DETECT_AUTHENTICATION_REQUIRED_DATA;

// internal function definitions
static HRESULT WINAPI AuthenticationRequired(
    __in LPVOID pData,
    __in HINTERNET hUrl,
    __in long lHttpCode,
    __out BOOL* pfRetrySend,
    __out BOOL* pfRetry
    );

static HRESULT DetectAtomFeedUpdate(
    __in_z LPCWSTR wzBundleCode,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate
    );

static HRESULT DownloadUpdateFeed(
    __in_z LPCWSTR wzBundleCode,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate,
    __deref_inout_z LPWSTR* psczTempFile
    );

// function definitions

extern "C" void DetectReset(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PACKAGES* pPackages
    )
{
    RelatedBundlesUninitialize(&pRegistration->relatedBundles);
    ReleaseNullStr(pRegistration->sczDetectedProviderKeyBundleCode);
    pRegistration->fDetectedForeignProviderKeyBundleCode = FALSE;
    pRegistration->fSelfRegisteredAsDependent = FALSE;
    pRegistration->fParentRegisteredAsDependent = FALSE;
    pRegistration->fForwardCompatibleBundleExists = FALSE;
    pRegistration->fEligibleForCleanup = FALSE;

    if (pRegistration->rgDependents)
    {
        ReleaseDependencyArray(pRegistration->rgDependents, pRegistration->cDependents);
    }
    pRegistration->rgDependents = NULL;
    pRegistration->cDependents = 0;

    for (DWORD iPackage = 0; iPackage < pPackages->cPackages; ++iPackage)
    {
        BURN_PACKAGE* pPackage = pPackages->rgPackages + iPackage;

        pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN;
        pPackage->cacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN;
        pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN;

        pPackage->fCached = FALSE;

        if (BURN_PACKAGE_TYPE_MSI == pPackage->type)
        {
            pPackage->Msi.operation = BOOTSTRAPPER_RELATED_OPERATION_NONE;

            for (DWORD iFeature = 0; iFeature < pPackage->Msi.cFeatures; ++iFeature)
            {
                BURN_MSIFEATURE* pFeature = pPackage->Msi.rgFeatures + iFeature;

                pFeature->currentState = BOOTSTRAPPER_FEATURE_STATE_UNKNOWN;
            }

            for (DWORD iSlipstreamMsp = 0; iSlipstreamMsp < pPackage->Msi.cSlipstreamMspPackages; ++iSlipstreamMsp)
            {
                BURN_SLIPSTREAM_MSP* pSlipstreamMsp = pPackage->Msi.rgSlipstreamMsps + iSlipstreamMsp;

                pSlipstreamMsp->dwMsiChainedPatchIndex = BURN_PACKAGE_INVALID_PATCH_INDEX;
            }

            ReleaseNullMem(pPackage->Msi.rgChainedPatches);
            pPackage->Msi.cChainedPatches = 0;
        }
        else if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
        {
            ReleaseNullMem(pPackage->Msp.rgTargetProducts);
            pPackage->Msp.cTargetProductCodes = 0;
        }

        for (DWORD iProvider = 0; iProvider < pPackage->cDependencyProviders; ++iProvider)
        {
            BURN_DEPENDENCY_PROVIDER* pProvider = pPackage->rgDependencyProviders + iProvider;

            pProvider->fExists = FALSE;
            pProvider->fBundleRegisteredAsDependent = FALSE;

            if (pProvider->rgDependents)
            {
                ReleaseDependencyArray(pProvider->rgDependents, pProvider->cDependents);
            }
            pProvider->rgDependents = NULL;
            pProvider->cDependents = 0;
        }

        PackageUninitializeCompatible(&pPackage->compatiblePackage);
    }

    for (DWORD iPatchInfo = 0; iPatchInfo < pPackages->cPatchInfo; ++iPatchInfo)
    {
        MSIPATCHSEQUENCEINFOW* pPatchInfo = pPackages->rgPatchInfo + iPatchInfo;
        pPatchInfo->dwOrder = 0;
        pPatchInfo->uStatus = 0;
    }
}

extern "C" HRESULT DetectForwardCompatibleBundles(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;
    int nCompareResult = 0;

    if (pRegistration->fDetectedForeignProviderKeyBundleCode)
    {
        for (DWORD iRelatedBundle = 0; iRelatedBundle < pRegistration->relatedBundles.cRelatedBundles; ++iRelatedBundle)
        {
            BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + iRelatedBundle;

            if (BOOTSTRAPPER_RELATION_UPGRADE == pRelatedBundle->detectRelationType &&
                CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pRegistration->sczDetectedProviderKeyBundleCode, -1, pRelatedBundle->package.sczId, -1))
            {
                hr = VerCompareParsedVersions(pRegistration->pVersion, pRelatedBundle->pVersion, &nCompareResult);
                ExitOnFailure(hr, "Failed to compare bundle version '%ls' to related bundle version '%ls'", pRegistration->pVersion->sczVersion, pRelatedBundle->pVersion->sczVersion);

                if (nCompareResult <= 0)
                {
                    if (pRelatedBundle->fPlannable)
                    {
                        pRelatedBundle->fForwardCompatible = TRUE;
                        pRegistration->fForwardCompatibleBundleExists = TRUE;
                    }

                    hr = BACallbackOnDetectForwardCompatibleBundle(pUX, pRelatedBundle->package.sczId, pRelatedBundle->detectRelationType, pRelatedBundle->sczTag, pRelatedBundle->package.fPerMachine, pRelatedBundle->pVersion, !pRelatedBundle->package.fCached);
                    ExitOnRootFailure(hr, "BA aborted detect forward compatible bundle.");

                    LogId(REPORT_STANDARD, MSG_DETECTED_FORWARD_COMPATIBLE_BUNDLE, pRelatedBundle->package.sczId, LoggingRelationTypeToString(pRelatedBundle->detectRelationType), LoggingPerMachineToString(pRelatedBundle->package.fPerMachine), pRelatedBundle->pVersion->sczVersion, LoggingBoolToString(pRelatedBundle->package.fCached));
                }
            }
        }
    }

LExit:
    return hr;
}

extern "C" HRESULT DetectReportRelatedBundles(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __out BOOL* pfEligibleForCleanup
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE planRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_NONE;
    BOOTSTRAPPER_REQUEST_STATE uninstallRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
    *pfEligibleForCleanup = BOOTSTRAPPER_REGISTRATION_TYPE_NONE != pRegistration->detectedRegistrationType || pRegistration->fCached;

    for (DWORD iRelatedBundle = 0; iRelatedBundle < pRegistration->relatedBundles.cRelatedBundles; ++iRelatedBundle)
    {
        const BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + iRelatedBundle;

        LogId(REPORT_STANDARD, MSG_DETECTED_RELATED_BUNDLE, pRelatedBundle->package.sczId, LoggingRelationTypeToString(pRelatedBundle->detectRelationType), LoggingPerMachineToString(pRelatedBundle->package.fPerMachine), pRelatedBundle->pVersion->sczVersion, LoggingBoolToString(pRelatedBundle->package.fCached));

        hr = BACallbackOnDetectRelatedBundle(pUX, pRelatedBundle->package.sczId, pRelatedBundle->detectRelationType, pRelatedBundle->sczTag, pRelatedBundle->package.fPerMachine, pRelatedBundle->pVersion, !pRelatedBundle->package.fCached);
        ExitOnRootFailure(hr, "BA aborted detect related bundle.");

        // For now, if any related bundles will be executed during uninstall by default then never automatically clean up the bundle.
        if (*pfEligibleForCleanup && pRelatedBundle->fPlannable)
        {
            planRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_NONE;
            uninstallRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;

            hr = PlanDefaultRelatedBundlePlanType(pRelatedBundle->detectRelationType, pRegistration->pVersion, pRelatedBundle->pVersion, &planRelationType);
            ExitOnFailure(hr, "Failed to get the default plan type for related bundle for calculating fEligibleForCleanup");

            hr = PlanDefaultRelatedBundleRequestState(relationType, planRelationType, BOOTSTRAPPER_ACTION_UNINSTALL, &uninstallRequestState);
            ExitOnFailure(hr, "Failed to get the default request state for related bundle for calculating fEligibleForCleanup");

            if (BOOTSTRAPPER_REQUEST_STATE_NONE != uninstallRequestState)
            {
                *pfEligibleForCleanup = FALSE;
            }
        }
    }

LExit:
    return hr;
}

extern "C" HRESULT DetectUpdate(
    __in_z LPCWSTR wzBundleCode,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate
    )
{
    HRESULT hr = S_OK;
    BOOL fBeginCalled = FALSE;
    BOOL fSkip = TRUE;
    BOOL fIgnoreError = FALSE;
    LPWSTR sczOriginalSource = NULL;

    // If no update source was specified, skip update detection.
    if (!pUpdate->sczUpdateSource || !*pUpdate->sczUpdateSource)
    {
        ExitFunction();
    }

    fBeginCalled = TRUE;

    hr = StrAllocString(&sczOriginalSource, pUpdate->sczUpdateSource, 0);
    ExitOnFailure(hr, "Failed to duplicate update feed source.");

    hr = BACallbackOnDetectUpdateBegin(pUX, sczOriginalSource, &fSkip);
    ExitOnRootFailure(hr, "BA aborted detect update begin.");

    if (!fSkip)
    {
        hr = DetectAtomFeedUpdate(wzBundleCode, pUX, pUpdate);
        ExitOnFailure(hr, "Failed to detect atom feed update.");
    }

LExit:
    ReleaseStr(sczOriginalSource);

    if (fBeginCalled)
    {
        BACallbackOnDetectUpdateComplete(pUX, hr, &fIgnoreError);
        if (fIgnoreError)
        {
            hr = S_OK;
        }
    }

    return hr;
}

static HRESULT WINAPI AuthenticationRequired(
    __in LPVOID pData,
    __in HINTERNET hUrl,
    __in long lHttpCode,
    __out BOOL* pfRetrySend,
    __out BOOL* pfRetry
    )
{
    Assert(401 == lHttpCode || 407 == lHttpCode);

    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    BOOTSTRAPPER_ERROR_TYPE errorType = (401 == lHttpCode) ? BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_SERVER : BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_PROXY;
    LPWSTR sczError = NULL;
    DETECT_AUTHENTICATION_REQUIRED_DATA* pAuthenticationData = reinterpret_cast<DETECT_AUTHENTICATION_REQUIRED_DATA*>(pData);
    int nResult = IDNOACTION;

    *pfRetrySend = FALSE;
    *pfRetry = FALSE;

    hr = StrAllocFromError(&sczError, HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED), NULL);
    ExitOnFailure(hr, "Failed to allocation error string.");

    BACallbackOnError(pAuthenticationData->pUX, errorType, pAuthenticationData->wzPackageOrContainerId, ERROR_ACCESS_DENIED, sczError, MB_RETRYCANCEL, 0, NULL, &nResult); // ignore return value.
    nResult = BootstrapperApplicationCheckExecuteResult(pAuthenticationData->pUX, FALSE, BURN_MB_RETRYTRYAGAIN, nResult);
    if (IDTRYAGAIN == nResult && pAuthenticationData->pUX->hwndDetect)
    {
        er = ::InternetErrorDlg(pAuthenticationData->pUX->hwndDetect, hUrl, ERROR_INTERNET_INCORRECT_PASSWORD, FLAGS_ERROR_UI_FILTER_FOR_ERRORS | FLAGS_ERROR_UI_FLAGS_CHANGE_OPTIONS | FLAGS_ERROR_UI_FLAGS_GENERATE_DATA, NULL);
        if (ERROR_SUCCESS == er || ERROR_CANCELLED == er)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
        }
        else if (ERROR_INTERNET_FORCE_RETRY == er)
        {
            *pfRetrySend = TRUE;
            hr = S_OK;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
        }
    }
    else if (IDRETRY == nResult)
    {
        *pfRetry = TRUE;
        hr = S_OK;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
    }

LExit:
    ReleaseStr(sczError);

    return hr;
}

static HRESULT DownloadUpdateFeed(
    __in_z LPCWSTR wzBundleCode,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate,
    __deref_inout_z LPWSTR* psczTempFile
    )
{
    HRESULT hr = S_OK;
    DOWNLOAD_SOURCE downloadSource = { };
    DOWNLOAD_CACHE_CALLBACK cacheCallback = { };
    DOWNLOAD_AUTHENTICATION_CALLBACK authenticationCallback = { };
    DETECT_AUTHENTICATION_REQUIRED_DATA authenticationData = { };
    LPWSTR sczUpdateId = NULL;
    LPWSTR sczError = NULL;
    DWORD64 qwDownloadSize = 0;

    // Always do our work in the working folder, even if cached.
    hr = PathCreateTimeBasedTempFile(NULL, L"UpdateFeed", NULL, L"xml", psczTempFile, NULL);
    ExitOnFailure(hr, "Failed to create UpdateFeed based on current system time.");

    // Do we need a means of the BA to pass in a user name and password? If so, we should copy it to downloadSource here
    hr = StrAllocString(&downloadSource.sczUrl, pUpdate->sczUpdateSource, 0);
    ExitOnFailure(hr, "Failed to copy update url.");

    cacheCallback.pfnProgress = NULL; //UpdateProgressRoutine;
    cacheCallback.pfnCancel = NULL; // TODO: set this
    cacheCallback.pv = NULL; //pProgress;

    authenticationData.pUX = pUX;
    authenticationData.wzPackageOrContainerId = wzBundleCode;

    authenticationCallback.pv =  static_cast<LPVOID>(&authenticationData);
    authenticationCallback.pfnAuthenticate = &AuthenticationRequired;

    hr = DownloadUrl(&downloadSource, qwDownloadSize, *psczTempFile, &cacheCallback, &authenticationCallback);
    ExitOnFailure(hr, "Failed attempt to download update feed from URL: '%ls' to: '%ls'", downloadSource.sczUrl, *psczTempFile);

LExit:
    if (FAILED(hr))
    {
        if (*psczTempFile)
        {
            FileEnsureDelete(*psczTempFile);
        }

        ReleaseNullStr(*psczTempFile);
    }

    ReleaseStr(downloadSource.sczUrl);
    ReleaseStr(downloadSource.sczUser);
    ReleaseStr(downloadSource.sczPassword);
    ReleaseStr(downloadSource.sczAuthorizationHeader);
    ReleaseStr(sczUpdateId);
    ReleaseStr(sczError);
    return hr;
}


static HRESULT DetectAtomFeedUpdate(
    __in_z LPCWSTR wzBundleCode,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate
    )
{
    Assert(pUpdate && pUpdate->sczUpdateSource && *pUpdate->sczUpdateSource);
#ifdef DEBUG
    LogStringLine(REPORT_STANDARD, "DetectAtomFeedUpdate() - update location: %ls", pUpdate->sczUpdateSource);
#endif


    HRESULT hr = S_OK;
    LPWSTR sczUpdateFeedTempFile = NULL;
    ATOM_FEED* pAtomFeed = NULL;
    APPLICATION_UPDATE_CHAIN* pApupChain = NULL;
    BOOL fStopProcessingUpdates = FALSE;
    LPWSTR sczHash = NULL;

    hr = AtomInitialize();
    ExitOnFailure(hr, "Failed to initialize Atom.");

    hr = DownloadUpdateFeed(wzBundleCode, pUX, pUpdate, &sczUpdateFeedTempFile);
    ExitOnFailure(hr, "Failed to download update feed.");

    hr = AtomParseFromFile(sczUpdateFeedTempFile, &pAtomFeed);
    ExitOnFailure(hr, "Failed to parse update atom feed: %ls.", sczUpdateFeedTempFile);

    hr = ApupAllocChainFromAtom(pAtomFeed, &pApupChain);
    ExitOnFailure(hr, "Failed to allocate update chain from atom feed.");

    if (0 < pApupChain->cEntries)
    {
        for (DWORD i = 0; i < pApupChain->cEntries; ++i)
        {
            APPLICATION_UPDATE_ENTRY* pAppUpdateEntry = &pApupChain->rgEntries[i];
            APPLICATION_UPDATE_ENCLOSURE* pEnclosure = pAppUpdateEntry->rgEnclosures;
            LPCWSTR wzHash = L"";
            BOOTSTRAPPER_UPDATE_HASH_TYPE hashType = BOOTSTRAPPER_UPDATE_HASH_TYPE_NONE;

            if (pEnclosure && pEnclosure->rgbDigest && APUP_HASH_ALGORITHM_SHA512 == pEnclosure->digestAlgorithm)
            {
                hr = StrAllocHexEncode(pEnclosure->rgbDigest, pEnclosure->cbDigest, &sczHash);
                ExitOnFailure(hr, "Failed to encode hash as string.");

                wzHash = sczHash;
                hashType = BOOTSTRAPPER_UPDATE_HASH_TYPE_SHA512;
            }

            hr = BACallbackOnDetectUpdate(pUX,
                pEnclosure ? pEnclosure->wzUrl : NULL,
                pEnclosure ? pEnclosure->dw64Size : 0,
                wzHash,
                hashType,
                pAppUpdateEntry->pVersion,
                pAppUpdateEntry->wzTitle,
                pAppUpdateEntry->wzSummary,
                pAppUpdateEntry->wzContentType,
                pAppUpdateEntry->wzContent,
                &fStopProcessingUpdates);
            ExitOnRootFailure(hr, "BA aborted detect update.");

            if (fStopProcessingUpdates)
            {
                break;
            }
        }
    }

LExit:
    if (sczUpdateFeedTempFile && *sczUpdateFeedTempFile)
    {
        FileEnsureDelete(sczUpdateFeedTempFile);
    }

    ApupFreeChain(pApupChain);
    AtomFreeFeed(pAtomFeed);
    ReleaseStr(sczUpdateFeedTempFile);
    ReleaseStr(sczHash);
    AtomUninitialize();

    return hr;
}
