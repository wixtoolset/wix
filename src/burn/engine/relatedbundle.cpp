// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// internal function declarations

static __callback int __cdecl CompareRelatedBundles(
    __in void* pvContext,
    __in const void* pvLeft,
    __in const void* pvRight
);
static HRESULT InitializeForScopeAndBitness(
    __in BOOL fPerMachine,
    __in BOOL fWow6432,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    );
static HRESULT LoadIfRelatedBundle(
    __in BOOL fPerMachine,
    __in BOOL fWow6432,
    __in HKEY hkUninstallKey,
    __in_z LPCWSTR sczRelatedBundleId,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    );
static HRESULT DetermineRelationType(
    __in HKEY hkBundleId,
    __in BURN_REGISTRATION* pRegistration,
    __out BOOTSTRAPPER_RELATION_TYPE* pRelationType
    );
static HRESULT LoadRelatedBundleFromKey(
    __in_z LPCWSTR wzRelatedBundleId,
    __in HKEY hkBundleId,
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

    hr = InitializeForScopeAndBitness(fPerMachine, /*fWow6432*/FALSE, pRegistration, pRelatedBundles);
    ExitOnFailure(hr, "Failed to open platform-native uninstall registry key.");

#if defined(_WIN64)
    hr = InitializeForScopeAndBitness(fPerMachine, /*fWow6432*/TRUE, pRegistration, pRelatedBundles);
    ExitOnFailure(hr, "Failed to open 32-bit uninstall registry key.");
#endif

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

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pPackage->sczId, -1, wzId, -1))
        {
            *ppRelatedBundle = pRelatedBundle;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

extern "C" void RelatedBundlesSort(
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    )
{
    qsort_s(pRelatedBundles->rgRelatedBundles, pRelatedBundles->cRelatedBundles, sizeof(BURN_RELATED_BUNDLE), CompareRelatedBundles, NULL);
}


// internal helper functions

static __callback int __cdecl CompareRelatedBundles(
    __in void* /*pvContext*/,
    __in const void* pvLeft,
    __in const void* pvRight
    )
{
    int ret = 0;
    const BURN_RELATED_BUNDLE* pBundleLeft = static_cast<const BURN_RELATED_BUNDLE*>(pvLeft);
    const BURN_RELATED_BUNDLE* pBundleRight = static_cast<const BURN_RELATED_BUNDLE*>(pvRight);

    // Sort by relation type, then version, then bundle id.
    if (pBundleLeft->relationType != pBundleRight->relationType)
    {
        // Upgrade bundles last, everything else according to the enum.
        if (BOOTSTRAPPER_RELATION_UPGRADE == pBundleLeft->relationType)
        {
            ret = 1;
        }
        else if (BOOTSTRAPPER_RELATION_UPGRADE == pBundleRight->relationType)
        {
            ret = -1;
        }
        else if (pBundleLeft->relationType < pBundleRight->relationType)
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
            ret = ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pBundleLeft->package.sczId, -1, pBundleRight->package.sczId, -1) - 2;
        }
    }

    return ret;
}

static HRESULT InitializeForScopeAndBitness(
    __in BOOL fPerMachine,
    __in BOOL fWow6432,
    __in BURN_REGISTRATION * pRegistration,
    __in BURN_RELATED_BUNDLES * pRelatedBundles
)
{
    HRESULT hr = S_OK;
    HKEY hkRoot = fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;
    HKEY hkUninstallKey = NULL;
    LPWSTR sczRelatedBundleId = NULL;

    hr = RegOpen(hkRoot, BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY, KEY_READ | (fWow6432 ? KEY_WOW64_32KEY : 0), &hkUninstallKey);
    if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to open uninstall registry key.");

    for (DWORD dwIndex = 0; /* exit via break below */; ++dwIndex)
    {
        hr = RegKeyEnum(hkUninstallKey, dwIndex, &sczRelatedBundleId);
        if (E_NOMOREITEMS == hr)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to enumerate uninstall key for related bundles.");

        // If we did not find our bundle id, try to load the subkey as a related bundle.
        if (CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, sczRelatedBundleId, -1, pRegistration->sczId, -1))
        {
            // Ignore failures here since we'll often find products that aren't actually
            // related bundles (or even bundles at all).
            HRESULT hrRelatedBundle = LoadIfRelatedBundle(fPerMachine, fWow6432, hkUninstallKey, sczRelatedBundleId, pRegistration, pRelatedBundles);
            UNREFERENCED_PARAMETER(hrRelatedBundle);
        }
    }

LExit:
    ReleaseStr(sczRelatedBundleId);
    ReleaseRegKey(hkUninstallKey);

    return hr;
}

static HRESULT LoadIfRelatedBundle(
    __in BOOL fPerMachine,
    __in BOOL fWow6432,
    __in HKEY hkUninstallKey,
    __in_z LPCWSTR sczRelatedBundleId,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    )
{
    HRESULT hr = S_OK;
    HKEY hkBundleId = NULL;
    BOOTSTRAPPER_RELATION_TYPE relationType = BOOTSTRAPPER_RELATION_NONE;

    hr = RegOpen(hkUninstallKey, sczRelatedBundleId, KEY_READ | (fWow6432 ? KEY_WOW64_32KEY : 0), &hkBundleId);
    ExitOnFailure(hr, "Failed to open uninstall key for potential related bundle: %ls", sczRelatedBundleId);

    hr = DetermineRelationType(hkBundleId, pRegistration, &relationType);
    if (FAILED(hr) || BOOTSTRAPPER_RELATION_NONE == relationType)
    {
        // Must not be a related bundle.
        hr = E_NOTFOUND;
    }
    else // load the related bundle.
    {
        hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pRelatedBundles->rgRelatedBundles), pRelatedBundles->cRelatedBundles + 1, sizeof(BURN_RELATED_BUNDLE), 5);
        ExitOnFailure(hr, "Failed to ensure there is space for related bundles.");

        BURN_RELATED_BUNDLE* pRelatedBundle = pRelatedBundles->rgRelatedBundles + pRelatedBundles->cRelatedBundles;

        hr = LoadRelatedBundleFromKey(sczRelatedBundleId, hkBundleId, fPerMachine, relationType, pRelatedBundle);
        ExitOnFailure(hr, "Failed to initialize package from related bundle id: %ls", sczRelatedBundleId);

        ++pRelatedBundles->cRelatedBundles;
    }

LExit:
    ReleaseRegKey(hkBundleId);

    return hr;
}

static HRESULT DetermineRelationType(
    __in HKEY hkBundleId,
    __in BURN_REGISTRATION* pRegistration,
    __out BOOTSTRAPPER_RELATION_TYPE* pRelationType
    )
{
    HRESULT hr = S_OK;
    LPWSTR* rgsczUpgradeCodes = NULL;
    DWORD cUpgradeCodes = 0;
    STRINGDICT_HANDLE sdUpgradeCodes = NULL;
    LPWSTR* rgsczAddonCodes = NULL;
    DWORD cAddonCodes = 0;
    STRINGDICT_HANDLE sdAddonCodes = NULL;
    LPWSTR* rgsczDetectCodes = NULL;
    DWORD cDetectCodes = 0;
    STRINGDICT_HANDLE sdDetectCodes = NULL;
    LPWSTR* rgsczPatchCodes = NULL;
    DWORD cPatchCodes = 0;
    STRINGDICT_HANDLE sdPatchCodes = NULL;

    *pRelationType = BOOTSTRAPPER_RELATION_NONE;

    // All remaining operations should treat all related bundles as non-vital.
    hr = RegReadStringArray(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &rgsczUpgradeCodes, &cUpgradeCodes);
    if (HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE) == hr)
    {
        TraceError(hr, "Failed to read upgrade codes as REG_MULTI_SZ. Trying again as REG_SZ in case of older bundles.");

        rgsczUpgradeCodes = reinterpret_cast<LPWSTR*>(MemAlloc(sizeof(LPWSTR), TRUE));
        ExitOnNull(rgsczUpgradeCodes, hr, E_OUTOFMEMORY, "Failed to allocate list for a single upgrade code from older bundle.");

        hr = RegReadString(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &rgsczUpgradeCodes[0]);
        if (SUCCEEDED(hr))
        {
            cUpgradeCodes = 1;
        }
    }

    // Compare upgrade codes.
    if (SUCCEEDED(hr))
    {
        hr = DictCreateStringListFromArray(&sdUpgradeCodes, rgsczUpgradeCodes, cUpgradeCodes, DICT_FLAG_CASEINSENSITIVE);
        ExitOnFailure(hr, "Failed to create string dictionary for %hs.", "upgrade codes");

        // Upgrade relationship: when their upgrade codes match our upgrade codes.
        hr = DictCompareStringListToArray(sdUpgradeCodes, const_cast<LPCWSTR*>(pRegistration->rgsczUpgradeCodes), pRegistration->cUpgradeCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for upgrade code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_UPGRADE;
            ExitFunction();
        }

        // Detect relationship: when their upgrade codes match our detect codes.
        hr = DictCompareStringListToArray(sdUpgradeCodes, const_cast<LPCWSTR*>(pRegistration->rgsczDetectCodes), pRegistration->cDetectCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for detect code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_DETECT;
            ExitFunction();
        }

        // Dependent relationship: when their upgrade codes match our addon codes.
        hr = DictCompareStringListToArray(sdUpgradeCodes, const_cast<LPCWSTR*>(pRegistration->rgsczAddonCodes), pRegistration->cAddonCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_DEPENDENT;
            ExitFunction();
        }

        // Dependent relationship: when their upgrade codes match our patch codes.
        hr = DictCompareStringListToArray(sdUpgradeCodes, const_cast<LPCWSTR*>(pRegistration->rgsczPatchCodes), pRegistration->cPatchCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_DEPENDENT;
            ExitFunction();
        }

        ReleaseNullDict(sdUpgradeCodes);
        ReleaseNullStrArray(rgsczUpgradeCodes, cUpgradeCodes);
    }

    // Compare addon codes.
    hr = RegReadStringArray(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE, &rgsczAddonCodes, &cAddonCodes);
    if (SUCCEEDED(hr))
    {
        hr = DictCreateStringListFromArray(&sdAddonCodes, rgsczAddonCodes, cAddonCodes, DICT_FLAG_CASEINSENSITIVE);
        ExitOnFailure(hr, "Failed to create string dictionary for %hs.", "addon codes");

        // Addon relationship: when their addon codes match our detect codes.
        hr = DictCompareStringListToArray(sdAddonCodes, const_cast<LPCWSTR*>(pRegistration->rgsczDetectCodes), pRegistration->cDetectCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_ADDON;
            ExitFunction();
        }

        // Addon relationship: when their addon codes match our upgrade codes.
        hr = DictCompareStringListToArray(sdAddonCodes, const_cast<LPCWSTR*>(pRegistration->rgsczUpgradeCodes), pRegistration->cUpgradeCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_ADDON;
            ExitFunction();
        }

        ReleaseNullDict(sdAddonCodes);
        ReleaseNullStrArray(rgsczAddonCodes, cAddonCodes);
    }

    // Compare patch codes.
    hr = RegReadStringArray(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE, &rgsczPatchCodes, &cPatchCodes);
    if (SUCCEEDED(hr))
    {
        hr = DictCreateStringListFromArray(&sdPatchCodes, rgsczPatchCodes, cPatchCodes, DICT_FLAG_CASEINSENSITIVE);
        ExitOnFailure(hr, "Failed to create string dictionary for %hs.", "patch codes");

        // Patch relationship: when their patch codes match our detect codes.
        hr = DictCompareStringListToArray(sdPatchCodes, const_cast<LPCWSTR*>(pRegistration->rgsczDetectCodes), pRegistration->cDetectCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for patch code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_PATCH;
            ExitFunction();
        }

        // Patch relationship: when their patch codes match our upgrade codes.
        hr = DictCompareStringListToArray(sdPatchCodes, const_cast<LPCWSTR*>(pRegistration->rgsczUpgradeCodes), pRegistration->cUpgradeCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for patch code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_PATCH;
            ExitFunction();
        }

        ReleaseNullDict(sdPatchCodes);
        ReleaseNullStrArray(rgsczPatchCodes, cPatchCodes);
    }

    // Compare detect codes.
    hr = RegReadStringArray(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE, &rgsczDetectCodes, &cDetectCodes);
    if (SUCCEEDED(hr))
    {
        hr = DictCreateStringListFromArray(&sdDetectCodes, rgsczDetectCodes, cDetectCodes, DICT_FLAG_CASEINSENSITIVE);
        ExitOnFailure(hr, "Failed to create string dictionary for %hs.", "detect codes");

        // Detect relationship: when their detect codes match our detect codes.
        hr = DictCompareStringListToArray(sdDetectCodes, const_cast<LPCWSTR*>(pRegistration->rgsczDetectCodes), pRegistration->cDetectCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for detect code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_DETECT;
            ExitFunction();
        }

        // Dependent relationship: when their detect codes match our addon codes.
        hr = DictCompareStringListToArray(sdDetectCodes, const_cast<LPCWSTR*>(pRegistration->rgsczAddonCodes), pRegistration->cAddonCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_DEPENDENT;
            ExitFunction();
        }

        // Dependent relationship: when their detect codes match our patch codes.
        hr = DictCompareStringListToArray(sdDetectCodes, const_cast<LPCWSTR*>(pRegistration->rgsczPatchCodes), pRegistration->cPatchCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BOOTSTRAPPER_RELATION_DEPENDENT;
            ExitFunction();
        }

        ReleaseNullDict(sdDetectCodes);
        ReleaseNullStrArray(rgsczDetectCodes, cDetectCodes);
    }

LExit:
    if (SUCCEEDED(hr) && BOOTSTRAPPER_RELATION_NONE == *pRelationType)
    {
        hr = E_NOTFOUND;
    }

    ReleaseDict(sdUpgradeCodes);
    ReleaseStrArray(rgsczUpgradeCodes, cUpgradeCodes);
    ReleaseDict(sdAddonCodes);
    ReleaseStrArray(rgsczAddonCodes, cAddonCodes);
    ReleaseDict(sdDetectCodes);
    ReleaseStrArray(rgsczDetectCodes, cDetectCodes);
    ReleaseDict(sdPatchCodes);
    ReleaseStrArray(rgsczPatchCodes, cPatchCodes);

    return hr;
}

static HRESULT LoadRelatedBundleFromKey(
    __in_z LPCWSTR wzRelatedBundleId,
    __in HKEY hkBundleId,
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
    BURN_DEPENDENCY_PROVIDER dependencyProvider = { };
    BURN_DEPENDENCY_PROVIDER* pBundleDependencyProvider = NULL;

    // Only support progress from engines that are compatible.
    hr = RegReadNumber(hkBundleId, BURN_REGISTRATION_REGISTRY_ENGINE_PROTOCOL_VERSION, &dwEngineProtocolVersion);
    if (SUCCEEDED(hr))
    {
        fSupportsBurnProtocol = BURN_PROTOCOL_VERSION == dwEngineProtocolVersion;
    }
    else
    {
        // Rely on version checking (aka: version greater than or equal to last protocol breaking change *and* versions that are older or the same as this engine)
        hr = RegReadVersion(hkBundleId, BURN_REGISTRATION_REGISTRY_ENGINE_VERSION, &qwEngineVersion);
        if (SUCCEEDED(hr))
        {
            fSupportsBurnProtocol = (FILEMAKEVERSION(3, 6, 2221, 0) <= qwEngineVersion && qwEngineVersion <= FILEMAKEVERSION(rmj, rmm, rup, rpr));
        }

        hr = S_OK;
    }

    hr = RegReadString(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION, &sczBundleVersion);
    ExitOnFailure(hr, "Failed to read version from registry for bundle: %ls", wzRelatedBundleId);

    hr = VerParseVersion(sczBundleVersion, 0, FALSE, &pRelatedBundle->pVersion);
    ExitOnFailure(hr, "Failed to parse pseudo bundle version: %ls", sczBundleVersion);

    if (pRelatedBundle->pVersion->fInvalid)
    {
        LogId(REPORT_WARNING, MSG_RELATED_PACKAGE_INVALID_VERSION, wzRelatedBundleId, sczBundleVersion);
    }

    hr = RegReadString(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH, &sczCachePath);
    ExitOnFailure(hr, "Failed to read cache path from registry for bundle: %ls", wzRelatedBundleId);

    if (FileExistsEx(sczCachePath, NULL))
    {
        fCached = TRUE;
    }
    else
    {
        LogId(REPORT_STANDARD, MSG_DETECT_RELATED_BUNDLE_NOT_CACHED, wzRelatedBundleId, sczCachePath);
    }

    pRelatedBundle->fPlannable = fCached;

    hr = RegReadString(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY, &dependencyProvider.sczKey);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to read provider key from registry for bundle: %ls", wzRelatedBundleId);
    }

    if (dependencyProvider.sczKey && *dependencyProvider.sczKey)
    {
        pBundleDependencyProvider = &dependencyProvider;

        dependencyProvider.fImported = TRUE;

        hr = StrAllocString(&dependencyProvider.sczVersion, pRelatedBundle->pVersion->sczVersion, 0);
        ExitOnFailure(hr, "Failed to copy version for bundle: %ls", wzRelatedBundleId);

        hr = RegReadString(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_DISPLAY_NAME, &dependencyProvider.sczDisplayName);
        if (E_FILENOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to copy display name for bundle: %ls", wzRelatedBundleId);
        }
    }

    hr = RegReadString(hkBundleId, BURN_REGISTRATION_REGISTRY_BUNDLE_TAG, &pRelatedBundle->sczTag);
    if (E_FILENOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to read tag from registry for bundle: %ls", wzRelatedBundleId);

    pRelatedBundle->relationType = relationType;

    hr = PseudoBundleInitializeRelated(&pRelatedBundle->package, fSupportsBurnProtocol, fPerMachine, wzRelatedBundleId,
#ifdef DEBUG
                                       pRelatedBundle->relationType,
#endif
                                       fCached, sczCachePath, qwFileSize, pBundleDependencyProvider);
    ExitOnFailure(hr, "Failed to initialize related bundle to represent bundle: %ls", wzRelatedBundleId);

LExit:
    DependencyUninitializeProvider(&dependencyProvider);
    ReleaseStr(sczCachePath);
    ReleaseStr(sczBundleVersion);

    return hr;
}
