// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

typedef struct _BUNDLE_QUERY_CONTEXT
{
    BURN_REGISTRATION* pRegistration;
    BURN_RELATED_BUNDLES* pRelatedBundles;
} BUNDLE_QUERY_CONTEXT;

// internal function declarations

static __callback int __cdecl CompareRelatedBundlesDetect(
    __in void* pvContext,
    __in const void* pvLeft,
    __in const void* pvRight
    );
static __callback int __cdecl CompareRelatedBundlesPlan(
    __in void* /*pvContext*/,
    __in const void* pvLeft,
    __in const void* pvRight
    );
static BUNDLE_QUERY_CALLBACK_RESULT CALLBACK QueryRelatedBundlesCallback(
    __in const BUNDLE_QUERY_RELATED_BUNDLE_RESULT* pBundle,
    __in LPVOID pvContext
    );
static HRESULT LoadIfRelatedBundle(
    __in const BUNDLE_QUERY_RELATED_BUNDLE_RESULT* pBundle,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    );
static HRESULT LoadRelatedBundleFromKey(
    __in_z LPCWSTR wzRelatedBundleCode,
    __in HKEY hkBundleCode,
    __in BOOL fPerMachine,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_RELATED_BUNDLE *pRelatedBundle
    );


// function definitions

extern "C" HRESULT RelatedBundlesInitializeForScope(
    __in BOOL fPerMachine,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    )
{
    HRESULT hr = S_OK;
    BUNDLE_INSTALL_CONTEXT installContext = fPerMachine ? BUNDLE_INSTALL_CONTEXT_MACHINE : BUNDLE_INSTALL_CONTEXT_USER;
    BUNDLE_QUERY_CONTEXT queryContext = { };

    queryContext.pRegistration = pRegistration;
    queryContext.pRelatedBundles = pRelatedBundles;

    hr = BundleQueryRelatedBundles(
        installContext,
        const_cast<LPCWSTR*>(pRegistration->rgsczDetectCodes),
        pRegistration->cDetectCodes,
        const_cast<LPCWSTR*>(pRegistration->rgsczUpgradeCodes),
        pRegistration->cUpgradeCodes,
        const_cast<LPCWSTR*>(pRegistration->rgsczAddonCodes),
        pRegistration->cAddonCodes,
        const_cast<LPCWSTR*>(pRegistration->rgsczPatchCodes),
        pRegistration->cPatchCodes,
        QueryRelatedBundlesCallback,
        &queryContext);
    ExitOnFailure(hr, "Failed to initialize related bundles for scope.");

LExit:
    return hr;
}

extern "C" void RelatedBundlesUninitialize(
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    )
{
    if (pRelatedBundles->rgRelatedBundles)
    {
        for (DWORD i = 0; i < pRelatedBundles->cRelatedBundles; ++i)
        {
            BURN_PACKAGE* pPackage = &pRelatedBundles->rgRelatedBundles[i].package;

            for (DWORD j = 0; j < pPackage->payloads.cItems; ++j)
            {
                PayloadUninitialize(pPackage->payloads.rgItems[j].pPayload);
            }

            PackageUninitialize(pPackage);
            ReleaseStr(pRelatedBundles->rgRelatedBundles[i].sczTag);
        }

        MemFree(pRelatedBundles->rgRelatedBundles);
    }

    ReleaseMem(pRelatedBundles->rgpPlanSortedRelatedBundles);

    memset(pRelatedBundles, 0, sizeof(BURN_RELATED_BUNDLES));
}


extern "C" HRESULT RelatedBundleFindById(
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in_z LPCWSTR wzId,
    __out BURN_RELATED_BUNDLE** ppRelatedBundle
    )
{
    HRESULT hr = S_OK;
    BURN_RELATED_BUNDLE* pRelatedBundle = NULL;
    BURN_PACKAGE* pPackage = NULL;

    *ppRelatedBundle = NULL;

    for (DWORD i = 0; i < pRelatedBundles->cRelatedBundles; ++i)
    {
        pRelatedBundle = pRelatedBundles->rgRelatedBundles + i;
        pPackage = &pRelatedBundle->package;

        if (CSTR_EQUAL == ::CompareStringOrdinal(pPackage->sczId, -1, wzId, -1, FALSE))
        {
            *ppRelatedBundle = pRelatedBundle;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

extern "C" void RelatedBundlesSortDetect(
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    )
{
    qsort_s(pRelatedBundles->rgRelatedBundles, pRelatedBundles->cRelatedBundles, sizeof(BURN_RELATED_BUNDLE), CompareRelatedBundlesDetect, NULL);
}

extern "C" void RelatedBundlesSortPlan(
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    )
{
    qsort_s(pRelatedBundles->rgpPlanSortedRelatedBundles, pRelatedBundles->cRelatedBundles, sizeof(BURN_RELATED_BUNDLE*), CompareRelatedBundlesPlan, NULL);
}

extern "C" BOOTSTRAPPER_RELATION_TYPE RelatedBundleConvertRelationType(
    __in BUNDLE_RELATION_TYPE relationType
    )
{
    switch (relationType)
    {
    case BUNDLE_RELATION_DETECT:
        return BOOTSTRAPPER_RELATION_DETECT;
    case BUNDLE_RELATION_UPGRADE:
        return BOOTSTRAPPER_RELATION_UPGRADE;
    case BUNDLE_RELATION_ADDON:
        return BOOTSTRAPPER_RELATION_ADDON;
    case BUNDLE_RELATION_PATCH:
        return BOOTSTRAPPER_RELATION_PATCH;
    case BUNDLE_RELATION_DEPENDENT_ADDON:
        return BOOTSTRAPPER_RELATION_DEPENDENT_ADDON;
    case BUNDLE_RELATION_DEPENDENT_PATCH:
        return BOOTSTRAPPER_RELATION_DEPENDENT_PATCH;
    default:
        AssertSz(BUNDLE_RELATION_NONE == relationType, "Unknown BUNDLE_RELATION_TYPE");
        return BOOTSTRAPPER_RELATION_NONE;
    }
}


// internal helper functions

static __callback int __cdecl CompareRelatedBundlesDetect(
    __in void* /*pvContext*/,
    __in const void* pvLeft,
    __in const void* pvRight
    )
{
    int ret = 0;
    const BURN_RELATED_BUNDLE* pBundleLeft = static_cast<const BURN_RELATED_BUNDLE*>(pvLeft);
    const BURN_RELATED_BUNDLE* pBundleRight = static_cast<const BURN_RELATED_BUNDLE*>(pvRight);

    // Sort by relation type, then version, then bundle code.
    if (pBundleLeft->detectRelationType != pBundleRight->detectRelationType)
    {
        // Upgrade bundles last, everything else according to the enum.
        if (BOOTSTRAPPER_RELATION_UPGRADE == pBundleLeft->detectRelationType)
        {
            ret = 1;
        }
        else if (BOOTSTRAPPER_RELATION_UPGRADE == pBundleRight->detectRelationType)
        {
            ret = -1;
        }
        else if (pBundleLeft->detectRelationType < pBundleRight->detectRelationType)
        {
            ret = -1;
        }
        else
        {
            ret = 1;
        }
    }
    else
    {
        VerCompareParsedVersions(pBundleLeft->pVersion, pBundleRight->pVersion, &ret);
        if (0 == ret)
        {
            ret = ::CompareStringOrdinal(pBundleLeft->package.sczId, -1, pBundleRight->package.sczId, -1, TRUE) - 2;
        }
    }

    return ret;
}

static __callback int __cdecl CompareRelatedBundlesPlan(
    __in void* /*pvContext*/,
    __in const void* pvLeft,
    __in const void* pvRight
    )
{
    int ret = 0;
    const BURN_RELATED_BUNDLE* pBundleLeft = *reinterpret_cast<BURN_RELATED_BUNDLE**>(const_cast<void*>(pvLeft));
    const BURN_RELATED_BUNDLE* pBundleRight = *reinterpret_cast<BURN_RELATED_BUNDLE**>(const_cast<void*>(pvRight));

    // Sort by relation type, then version, then bundle code.
    if (pBundleLeft->planRelationType != pBundleRight->planRelationType)
    {
        // Upgrade bundles last, everything else according to the enum.
        if (BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_UPGRADE == pBundleLeft->planRelationType)
        {
            ret = 1;
        }
        else if (BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_UPGRADE == pBundleRight->planRelationType)
        {
            ret = -1;
        }
        else if (pBundleLeft->planRelationType < pBundleRight->planRelationType)
        {
            ret = -1;
        }
        else
        {
            ret = 1;
        }
    }
    else
    {
        VerCompareParsedVersions(pBundleLeft->pVersion, pBundleRight->pVersion, &ret);
        if (0 == ret)
        {
            ret = ::CompareStringOrdinal(pBundleLeft->package.sczId, -1, pBundleRight->package.sczId, -1, TRUE) - 2;
        }
    }

    return ret;
}

static BUNDLE_QUERY_CALLBACK_RESULT CALLBACK QueryRelatedBundlesCallback(
    __in const BUNDLE_QUERY_RELATED_BUNDLE_RESULT* pBundle,
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    BUNDLE_QUERY_CALLBACK_RESULT result = BUNDLE_QUERY_CALLBACK_RESULT_CONTINUE;
    BUNDLE_QUERY_CONTEXT* pContext = reinterpret_cast<BUNDLE_QUERY_CONTEXT*>(pvContext);

    hr = LoadIfRelatedBundle(pBundle, pContext->pRegistration, pContext->pRelatedBundles);
    ExitOnFailure(hr, "Failed to load related bundle: %ls", pBundle->wzBundleCode);

LExit:
    return result;
}

static HRESULT LoadIfRelatedBundle(
    __in const BUNDLE_QUERY_RELATED_BUNDLE_RESULT* pBundle,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    )
{
    HRESULT hr = S_OK;
    BOOL fPerMachine = BUNDLE_INSTALL_CONTEXT_MACHINE == pBundle->installContext;
    BOOTSTRAPPER_RELATION_TYPE relationType = RelatedBundleConvertRelationType(pBundle->relationType);
    BURN_RELATED_BUNDLE* pRelatedBundle = NULL;

    // If we found our bundle code, it's not a related bundle.
    if (CSTR_EQUAL == ::CompareStringOrdinal(pBundle->wzBundleCode, -1, pRegistration->sczCode, -1, TRUE))
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pRelatedBundles->rgRelatedBundles), pRelatedBundles->cRelatedBundles + 1, sizeof(BURN_RELATED_BUNDLE), 5);
    ExitOnFailure(hr, "Failed to ensure there is space for related bundles.");

    pRelatedBundle = pRelatedBundles->rgRelatedBundles + pRelatedBundles->cRelatedBundles;

    hr = LoadRelatedBundleFromKey(pBundle->wzBundleCode, pBundle->hkBundle, fPerMachine, relationType, pRelatedBundle);
    ExitOnFailure(hr, "Failed to initialize package from related bundle code: %ls", pBundle->wzBundleCode);

    hr = DependencyDetectRelatedBundle(pRelatedBundle, pRegistration);
    ExitOnFailure(hr, "Failed to detect dependencies for related bundle.");

    ++pRelatedBundles->cRelatedBundles;

LExit:
    return hr;
}

static HRESULT LoadRelatedBundleFromKey(
    __in_z LPCWSTR wzRelatedBundleCode,
    __in HKEY hkBundleCode,
    __in BOOL fPerMachine,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_RELATED_BUNDLE* pRelatedBundle
    )
{
    HRESULT hr = S_OK;
    DWORD64 qwEngineVersion = 0;
    DWORD dwEngineProtocolVersion = 0;
    BOOL fSupportsBurnProtocol = FALSE;
    LPWSTR sczBundleVersion = NULL;
    LPWSTR sczCachePath = NULL;
    BOOL fCached = FALSE;
    DWORD64 qwFileSize = 0;
    BOOL fExists = FALSE;
    BURN_DEPENDENCY_PROVIDER dependencyProvider = { };
    BURN_DEPENDENCY_PROVIDER* pBundleDependencyProvider = NULL;

    // Only support progress from engines that are compatible.
    hr = RegReadNumber(hkBundleCode, BURN_REGISTRATION_REGISTRY_ENGINE_PROTOCOL_VERSION, &dwEngineProtocolVersion);
    if (SUCCEEDED(hr))
    {
        fSupportsBurnProtocol = BURN_PROTOCOL_VERSION == dwEngineProtocolVersion;
    }
    else
    {
        // Rely on version checking (aka: version greater than or equal to last protocol breaking change *and* versions that are older or the same as this engine)
        hr = RegReadVersion(hkBundleCode, BURN_REGISTRATION_REGISTRY_ENGINE_VERSION, &qwEngineVersion);
        if (SUCCEEDED(hr))
        {
            fSupportsBurnProtocol = (FILEMAKEVERSION(3, 6, 2221, 0) <= qwEngineVersion && qwEngineVersion <= FILEMAKEVERSION(rmj, rmm, rup, rpr));
        }

        hr = S_OK;
    }

    hr = RegReadString(hkBundleCode, BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION, &sczBundleVersion);
    ExitOnFailure(hr, "Failed to read version from registry for bundle: %ls", wzRelatedBundleCode);

    hr = VerParseVersion(sczBundleVersion, 0, FALSE, &pRelatedBundle->pVersion);
    ExitOnFailure(hr, "Failed to parse pseudo bundle version: %ls", sczBundleVersion);

    if (pRelatedBundle->pVersion->fInvalid)
    {
        LogId(REPORT_WARNING, MSG_RELATED_PACKAGE_INVALID_VERSION, wzRelatedBundleCode, sczBundleVersion);
    }

    hr = RegReadString(hkBundleCode, BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH, &sczCachePath);
    ExitOnFailure(hr, "Failed to read cache path from registry for bundle: %ls", wzRelatedBundleCode);

    if (FileExistsEx(sczCachePath, NULL))
    {
        fCached = TRUE;
    }
    else
    {
        LogId(REPORT_STANDARD, MSG_DETECT_RELATED_BUNDLE_NOT_CACHED, wzRelatedBundleCode, sczCachePath);
    }

    pRelatedBundle->fPlannable = fCached;

    hr = RegReadString(hkBundleCode, BURN_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY, &dependencyProvider.sczKey);
    ExitOnPathFailure(hr, fExists, "Failed to read provider key from registry for bundle: %ls", wzRelatedBundleCode);

    if (dependencyProvider.sczKey && *dependencyProvider.sczKey)
    {
        pBundleDependencyProvider = &dependencyProvider;

        dependencyProvider.fImported = TRUE;

        hr = StrAllocString(&dependencyProvider.sczVersion, pRelatedBundle->pVersion->sczVersion, 0);
        ExitOnFailure(hr, "Failed to copy version for bundle: %ls", wzRelatedBundleCode);

        hr = RegReadString(hkBundleCode, BURN_REGISTRATION_REGISTRY_BUNDLE_DISPLAY_NAME, &dependencyProvider.sczDisplayName);
        ExitOnPathFailure(hr, fExists, "Failed to copy display name for bundle: %ls", wzRelatedBundleCode);
    }

    hr = RegReadString(hkBundleCode, BURN_REGISTRATION_REGISTRY_BUNDLE_TAG, &pRelatedBundle->sczTag);
    ExitOnPathFailure(hr, fExists, "Failed to read tag from registry for bundle: %ls", wzRelatedBundleCode);

    pRelatedBundle->detectRelationType = relationType;

    hr = PseudoBundleInitializeRelated(&pRelatedBundle->package, fSupportsBurnProtocol, fPerMachine, wzRelatedBundleCode,
#ifdef DEBUG
                                       pRelatedBundle->detectRelationType,
#endif
                                       fCached, sczCachePath, qwFileSize, pBundleDependencyProvider);
    ExitOnFailure(hr, "Failed to initialize related bundle to represent bundle: %ls", wzRelatedBundleCode);

LExit:
    DependencyUninitializeProvider(&dependencyProvider);
    ReleaseStr(sczCachePath);
    ReleaseStr(sczBundleVersion);

    return hr;
}
