// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// Exit macros
#define ButilExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_BUTIL, x, e, s, __VA_ARGS__)
#define ButilExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_BUTIL, x, s, __VA_ARGS__)
#define ButilExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_BUTIL, p, x, e, s, __VA_ARGS__)
#define ButilExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_BUTIL, p, x, s, __VA_ARGS__)
#define ButilExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_BUTIL, p, x, e, s, __VA_ARGS__)
#define ButilExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_BUTIL, p, x, s, __VA_ARGS__)
#define ButilExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_BUTIL, e, x, s, __VA_ARGS__)
#define ButilExitOnPathFailure(x, b, s, ...) ExitOnPathFailureSource(DUTIL_SOURCE_BUTIL, x, b, s, __VA_ARGS__)

// constants
// From engine/registration.h
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE = L"BundleAddonCode";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE = L"BundleDetectCode";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE = L"BundlePatchCode";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE = L"BundleUpgradeCode";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY = L"BundleProviderKey";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_VARIABLE_KEY = L"variables";

enum INTERNAL_BUNDLE_STATUS
{
    INTERNAL_BUNDLE_STATUS_SUCCESS,
    INTERNAL_BUNDLE_STATUS_UNKNOWN_BUNDLE,
    INTERNAL_BUNDLE_STATUS_UNKNOWN_PROPERTY,
};

typedef struct _BUNDLE_QUERY_CONTEXT
{
    BUNDLE_INSTALL_CONTEXT installContext;
    REG_KEY_BITNESS regBitness;
    PFNBUNDLE_QUERY_RELATED_BUNDLE_CALLBACK pfnCallback;
    LPVOID pvContext;

    LPCWSTR* rgwzDetectCodes;
    DWORD cDetectCodes;

    LPCWSTR* rgwzUpgradeCodes;
    DWORD cUpgradeCodes;

    LPCWSTR* rgwzAddonCodes;
    DWORD cAddonCodes;

    LPCWSTR* rgwzPatchCodes;
    DWORD cPatchCodes;
} BUNDLE_QUERY_CONTEXT;

// Forward declarations.
static HRESULT QueryRelatedBundlesForScopeAndBitness(
    __in BUNDLE_QUERY_CONTEXT* pQueryContext
    );
static HRESULT QueryPotentialRelatedBundle(
    __in BUNDLE_QUERY_CONTEXT* pQueryContext,
    __in HKEY hkUninstallKey,
    __in_z LPCWSTR wzRelatedBundleId,
    __inout BUNDLE_QUERY_CALLBACK_RESULT* pResult
    );
static HRESULT DetermineRelationType(
    __in BUNDLE_QUERY_CONTEXT* pQueryContext,
    __in HKEY hkBundleId,
    __out BUNDLE_RELATION_TYPE* pRelationType
    );
/********************************************************************
LocateAndQueryBundleValue - Locates the requested key for the bundle,
    then queries the registry type for requested value.

NOTE: caller is responsible for closing key
********************************************************************/
static HRESULT LocateAndQueryBundleValue(
    __in_z LPCWSTR wzBundleId,
    __in_opt LPCWSTR wzSubKey,
    __in LPCWSTR wzValueName,
    __inout HKEY* phKey,
    __inout DWORD* pdwType,
    __out INTERNAL_BUNDLE_STATUS* pStatus
    );
static HRESULT CopyStringToBuffer(
    __in_z LPWSTR wzValue,
    __in_z_opt LPWSTR wzBuffer,
    __inout SIZE_T* pcchBuffer
    );


DAPI_(HRESULT) BundleGetBundleInfo(
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzAttribute,
    __deref_out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    HKEY hkBundle = NULL;
    INTERNAL_BUNDLE_STATUS status = INTERNAL_BUNDLE_STATUS_SUCCESS;
    DWORD dwType = 0;
    DWORD dwValue = 0;

    if (!wzBundleId || !wzAttribute || !psczValue)
    {
        ButilExitWithRootFailure(hr, E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    hr = LocateAndQueryBundleValue(wzBundleId, NULL, wzAttribute, &hkBundle, &dwType, &status);
    ButilExitOnFailure(hr, "Failed to locate and query bundle attribute.");

    switch (status)
    {
    case INTERNAL_BUNDLE_STATUS_UNKNOWN_BUNDLE:
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT));
    case INTERNAL_BUNDLE_STATUS_UNKNOWN_PROPERTY:
        // If the bundle doesn't have the property defined, return ERROR_UNKNOWN_PROPERTY
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY));
    }

    switch (dwType)
    {
        case REG_SZ:
            hr = RegReadString(hkBundle, wzAttribute, psczValue);
            ButilExitOnFailure(hr, "Failed to read string property.");
            break;
        case REG_DWORD:
            hr = RegReadNumber(hkBundle, wzAttribute, &dwValue);
            ButilExitOnFailure(hr, "Failed to read dword property.");

            hr = StrAllocFormatted(psczValue, L"%d", dwValue);
            ButilExitOnFailure(hr, "Failed to format dword property as string.");
            break;
        default:
            ButilExitWithRootFailure(hr, E_NOTIMPL, "Reading bundle info of type 0x%x not implemented.", dwType);
    }

LExit:
    ReleaseRegKey(hkBundle);

    return hr;
}


DAPI_(HRESULT) BundleGetBundleInfoFixed(
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzAttribute,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout SIZE_T* pcchValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    if (!pcchValue)
    {
        ButilExitWithRootFailure(hr, E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    hr = BundleGetBundleInfo(wzBundleId, wzAttribute, &sczValue);
    if (SUCCEEDED(hr))
    {
        hr = CopyStringToBuffer(sczValue, wzValue, pcchValue);
    }

LExit:
    ReleaseStr(sczValue);

    return hr;
}


DAPI_(HRESULT) BundleEnumRelatedBundle(
    __in_z LPCWSTR wzUpgradeCode,
    __in BUNDLE_INSTALL_CONTEXT context,
    __in REG_KEY_BITNESS kbKeyBitness,
    __inout PDWORD pdwStartIndex,
    __deref_out_z LPWSTR* psczBundleId
    )
{
    HRESULT hr = S_OK;
    BOOL fUpgradeCodeFound = FALSE;
    HKEY hkUninstall = NULL;
    HKEY hkBundle = NULL;
    LPWSTR sczUninstallSubKey = NULL;
    LPWSTR sczUninstallSubKeyPath = NULL;
    HKEY hkRoot = BUNDLE_INSTALL_CONTEXT_USER == context ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
    BUNDLE_QUERY_CONTEXT queryContext = { };
    BUNDLE_RELATION_TYPE relationType = BUNDLE_RELATION_NONE;

    queryContext.installContext = context;
    queryContext.rgwzUpgradeCodes = &wzUpgradeCode;
    queryContext.cUpgradeCodes = 1;

    if (!wzUpgradeCode || !pdwStartIndex)
    {
        ButilExitOnFailure(hr = E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    hr = RegOpenEx(hkRoot, BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, KEY_READ, kbKeyBitness, &hkUninstall);
    ButilExitOnFailure(hr, "Failed to open bundle uninstall key path.");

    for (DWORD dwIndex = *pdwStartIndex; !fUpgradeCodeFound; dwIndex++)
    {
        hr = RegKeyEnum(hkUninstall, dwIndex, &sczUninstallSubKey);
        ButilExitOnFailure(hr, "Failed to enumerate bundle uninstall key path.");

        hr = StrAllocFormatted(&sczUninstallSubKeyPath, L"%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, sczUninstallSubKey);
        ButilExitOnFailure(hr, "Failed to allocate bundle uninstall key path.");

        hr = RegOpenEx(hkRoot, sczUninstallSubKeyPath, KEY_READ, kbKeyBitness, &hkBundle);
        ButilExitOnFailure(hr, "Failed to open uninstall key path.");

        hr = DetermineRelationType(&queryContext, hkBundle, &relationType);
        if (SUCCEEDED(hr) && BUNDLE_RELATION_UPGRADE == relationType)
        {
            fUpgradeCodeFound = TRUE;
            *pdwStartIndex = dwIndex;

            if (psczBundleId)
            {
                *psczBundleId = sczUninstallSubKey;
                sczUninstallSubKey = NULL;
            }

            break;
        }

        // Cleanup before next iteration
        ReleaseRegKey(hkBundle);
    }

LExit:
    ReleaseStr(sczUninstallSubKey);
    ReleaseStr(sczUninstallSubKeyPath);
    ReleaseRegKey(hkBundle);
    ReleaseRegKey(hkUninstall);

    return FAILED(hr) ? hr : fUpgradeCodeFound ? S_OK : S_FALSE;
}


DAPI_(HRESULT) BundleEnumRelatedBundleFixed(
    __in_z LPCWSTR wzUpgradeCode,
    __in BUNDLE_INSTALL_CONTEXT context,
    __in REG_KEY_BITNESS kbKeyBitness,
    __inout PDWORD pdwStartIndex,
    __out_ecount(MAX_GUID_CHARS+1) LPWSTR wzBundleId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    size_t cchValue = 0;

    hr = BundleEnumRelatedBundle(wzUpgradeCode, context, kbKeyBitness, pdwStartIndex, &sczValue);
    if (S_OK == hr && wzBundleId)
    {
        hr = ::StringCchLengthW(sczValue, STRSAFE_MAX_CCH, &cchValue);
        ButilExitOnRootFailure(hr, "Failed to calculate length of string.");

        hr = ::StringCchCopyNExW(wzBundleId, MAX_GUID_CHARS + 1, sczValue, cchValue, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
        ButilExitOnRootFailure(hr, "Failed to copy the property value to the output buffer.");
    }

LExit:
    ReleaseStr(sczValue);

    return hr;
}


DAPI_(HRESULT) BundleGetBundleVariable(
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzVariable,
    __deref_out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    HKEY hkBundle = NULL;
    INTERNAL_BUNDLE_STATUS status = INTERNAL_BUNDLE_STATUS_SUCCESS;
    DWORD dwType = 0;

    if (!wzBundleId || !wzVariable || !psczValue)
    {
        ButilExitWithRootFailure(hr, E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    hr = LocateAndQueryBundleValue(wzBundleId, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_VARIABLE_KEY, wzVariable, &hkBundle, &dwType, &status);
    ButilExitOnFailure(hr, "Failed to locate and query bundle variable.");

    switch (status)
    {
    case INTERNAL_BUNDLE_STATUS_UNKNOWN_BUNDLE:
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT));
    case INTERNAL_BUNDLE_STATUS_UNKNOWN_PROPERTY:
        // If the bundle doesn't have the shared variable defined, return ERROR_UNKNOWN_PROPERTY
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY));
    }

    switch (dwType)
    {
    case REG_SZ:
        hr = RegReadString(hkBundle, wzVariable, psczValue);
        ButilExitOnFailure(hr, "Failed to read string shared variable.");
        break;
    case REG_NONE:
        hr = S_OK;
        break;
    default:
        ButilExitWithRootFailure(hr, E_NOTIMPL, "Reading bundle variable of type 0x%x not implemented.", dwType);
    }

LExit:
    ReleaseRegKey(hkBundle);

    return hr;
}


DAPI_(HRESULT) BundleGetBundleVariableFixed(
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzVariable,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout SIZE_T* pcchValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    if (!pcchValue)
    {
        ButilExitWithRootFailure(hr, E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    hr = BundleGetBundleVariable(wzBundleId, wzVariable, &sczValue);
    if (SUCCEEDED(hr))
    {
        hr = CopyStringToBuffer(sczValue, wzValue, pcchValue);
    }

LExit:
    ReleaseStr(sczValue);

    return hr;
}

DAPI_(HRESULT) BundleQueryRelatedBundles(
    __in BUNDLE_INSTALL_CONTEXT installContext,
    __in_z_opt LPCWSTR* rgwzDetectCodes,
    __in DWORD cDetectCodes,
    __in_z_opt LPCWSTR* rgwzUpgradeCodes,
    __in DWORD cUpgradeCodes,
    __in_z_opt LPCWSTR* rgwzAddonCodes,
    __in DWORD cAddonCodes,
    __in_z_opt LPCWSTR* rgwzPatchCodes,
    __in DWORD cPatchCodes,
    __in PFNBUNDLE_QUERY_RELATED_BUNDLE_CALLBACK pfnCallback,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    BUNDLE_QUERY_CONTEXT queryContext = { };
    BOOL fSearch64 = TRUE;

#if !defined(_WIN64)
    // On 32-bit OS's, the requested bitness of the key is ignored so need to avoid searching the same place twice.
    ProcWow64(::GetCurrentProcess(), &fSearch64);
#endif

    queryContext.installContext = installContext;
    queryContext.rgwzDetectCodes = rgwzDetectCodes;
    queryContext.cDetectCodes = cDetectCodes;
    queryContext.rgwzUpgradeCodes = rgwzUpgradeCodes;
    queryContext.cUpgradeCodes = cUpgradeCodes;
    queryContext.rgwzAddonCodes = rgwzAddonCodes;
    queryContext.cAddonCodes = cAddonCodes;
    queryContext.rgwzPatchCodes = rgwzPatchCodes;
    queryContext.cPatchCodes = cPatchCodes;
    queryContext.pfnCallback = pfnCallback;
    queryContext.pvContext = pvContext;

    queryContext.regBitness = REG_KEY_32BIT;

    hr = QueryRelatedBundlesForScopeAndBitness(&queryContext);
    ButilExitOnFailure(hr, "Failed to query 32-bit related bundles.");

    if (fSearch64)
    {
        queryContext.regBitness = REG_KEY_64BIT;

        hr = QueryRelatedBundlesForScopeAndBitness(&queryContext);
        ButilExitOnFailure(hr, "Failed to query 64-bit related bundles.");
    }

LExit:
    return hr;
}

static HRESULT QueryRelatedBundlesForScopeAndBitness(
    __in BUNDLE_QUERY_CONTEXT* pQueryContext
    )
{
    HRESULT hr = S_OK;
    HKEY hkRoot = BUNDLE_INSTALL_CONTEXT_USER == pQueryContext->installContext ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
    HKEY hkUninstallKey = NULL;
    BOOL fExists = FALSE;
    LPWSTR sczRelatedBundleId = NULL;
    BUNDLE_QUERY_CALLBACK_RESULT result = BUNDLE_QUERY_CALLBACK_RESULT_CONTINUE;

    hr = RegOpenEx(hkRoot, BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, KEY_READ, pQueryContext->regBitness, &hkUninstallKey);
    ButilExitOnPathFailure(hr, fExists, "Failed to open uninstall registry key.");

    if (!fExists)
    {
        ExitFunction1(hr = S_OK);
    }

    for (DWORD dwIndex = 0; /* exit via break below */; ++dwIndex)
    {
        hr = RegKeyEnum(hkUninstallKey, dwIndex, &sczRelatedBundleId);
        if (E_NOMOREITEMS == hr)
        {
            hr = S_OK;
            break;
        }
        ButilExitOnFailure(hr, "Failed to enumerate uninstall key for related bundles.");

        // Ignore failures here since we'll often find products that aren't actually
        // related bundles (or even bundles at all).
        HRESULT hrRelatedBundle = QueryPotentialRelatedBundle(pQueryContext, hkUninstallKey, sczRelatedBundleId, &result);
        if (SUCCEEDED(hrRelatedBundle) && BUNDLE_QUERY_CALLBACK_RESULT_CONTINUE != result)
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_REQUEST_ABORTED));
        }
    }

LExit:
    ReleaseStr(sczRelatedBundleId);
    ReleaseRegKey(hkUninstallKey);

    return hr;
}

static HRESULT QueryPotentialRelatedBundle(
    __in BUNDLE_QUERY_CONTEXT* pQueryContext,
    __in HKEY hkUninstallKey,
    __in_z LPCWSTR wzRelatedBundleId,
    __inout BUNDLE_QUERY_CALLBACK_RESULT* pResult
    )
{
    HRESULT hr = S_OK;
    HKEY hkBundleId = NULL;
    BUNDLE_RELATION_TYPE relationType = BUNDLE_RELATION_NONE;
    BUNDLE_QUERY_RELATED_BUNDLE_RESULT bundle = { };

    hr = RegOpenEx(hkUninstallKey, wzRelatedBundleId, KEY_READ, pQueryContext->regBitness, &hkBundleId);
    ButilExitOnFailure(hr, "Failed to open uninstall key for potential related bundle: %ls", wzRelatedBundleId);

    hr = DetermineRelationType(pQueryContext, hkBundleId, &relationType);
    if (FAILED(hr))
    {
        ExitFunction();
    }

    bundle.installContext = pQueryContext->installContext;
    bundle.regBitness = pQueryContext->regBitness;
    bundle.wzBundleId = wzRelatedBundleId;
    bundle.relationType = relationType;
    bundle.hkBundle = hkBundleId;

    *pResult = pQueryContext->pfnCallback(&bundle, pQueryContext->pvContext);

LExit:
    ReleaseRegKey(hkBundleId);

    return hr;
}

static HRESULT DetermineRelationType(
    __in BUNDLE_QUERY_CONTEXT* pQueryContext,
    __in HKEY hkBundleId,
    __out BUNDLE_RELATION_TYPE* pRelationType
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

    *pRelationType = BUNDLE_RELATION_NONE;

    hr = RegReadStringArray(hkBundleId, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &rgsczUpgradeCodes, &cUpgradeCodes);
    if (HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE) == hr)
    {
        TraceError(hr, "Failed to read upgrade codes as REG_MULTI_SZ. Trying again as REG_SZ in case of older bundles.");

        rgsczUpgradeCodes = reinterpret_cast<LPWSTR*>(MemAlloc(sizeof(LPWSTR), TRUE));
        ButilExitOnNull(rgsczUpgradeCodes, hr, E_OUTOFMEMORY, "Failed to allocate list for a single upgrade code from older bundle.");

        hr = RegReadString(hkBundleId, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &rgsczUpgradeCodes[0]);
        if (SUCCEEDED(hr))
        {
            cUpgradeCodes = 1;
        }
    }

    // Compare upgrade codes.
    if (SUCCEEDED(hr))
    {
        hr = DictCreateStringListFromArray(&sdUpgradeCodes, rgsczUpgradeCodes, cUpgradeCodes, DICT_FLAG_CASEINSENSITIVE);
        ButilExitOnFailure(hr, "Failed to create string dictionary for %hs.", "upgrade codes");

        // Upgrade relationship: when their upgrade codes match our upgrade codes.
        hr = DictCompareStringListToArray(sdUpgradeCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzUpgradeCodes), pQueryContext->cUpgradeCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for upgrade code match.");

            *pRelationType = BUNDLE_RELATION_UPGRADE;
            ExitFunction();
        }

        // Detect relationship: when their upgrade codes match our detect codes.
        hr = DictCompareStringListToArray(sdUpgradeCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzDetectCodes), pQueryContext->cDetectCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for detect code match.");

            *pRelationType = BUNDLE_RELATION_DETECT;
            ExitFunction();
        }

        // Dependent relationship: when their upgrade codes match our addon codes.
        hr = DictCompareStringListToArray(sdUpgradeCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzAddonCodes), pQueryContext->cAddonCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BUNDLE_RELATION_DEPENDENT_ADDON;
            ExitFunction();
        }

        // Dependent relationship: when their upgrade codes match our patch codes.
        hr = DictCompareStringListToArray(sdUpgradeCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzPatchCodes), pQueryContext->cPatchCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for patch code match.");

            *pRelationType = BUNDLE_RELATION_DEPENDENT_PATCH;
            ExitFunction();
        }

        ReleaseNullDict(sdUpgradeCodes);
        ReleaseNullStrArray(rgsczUpgradeCodes, cUpgradeCodes);
    }

    // Compare addon codes.
    hr = RegReadStringArray(hkBundleId, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE, &rgsczAddonCodes, &cAddonCodes);
    if (SUCCEEDED(hr))
    {
        hr = DictCreateStringListFromArray(&sdAddonCodes, rgsczAddonCodes, cAddonCodes, DICT_FLAG_CASEINSENSITIVE);
        ButilExitOnFailure(hr, "Failed to create string dictionary for %hs.", "addon codes");

        // Addon relationship: when their addon codes match our detect codes.
        hr = DictCompareStringListToArray(sdAddonCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzDetectCodes), pQueryContext->cDetectCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BUNDLE_RELATION_ADDON;
            ExitFunction();
        }

        // Addon relationship: when their addon codes match our upgrade codes.
        hr = DictCompareStringListToArray(sdAddonCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzUpgradeCodes), pQueryContext->cUpgradeCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BUNDLE_RELATION_ADDON;
            ExitFunction();
        }

        ReleaseNullDict(sdAddonCodes);
        ReleaseNullStrArray(rgsczAddonCodes, cAddonCodes);
    }

    // Compare patch codes.
    hr = RegReadStringArray(hkBundleId, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE, &rgsczPatchCodes, &cPatchCodes);
    if (SUCCEEDED(hr))
    {
        hr = DictCreateStringListFromArray(&sdPatchCodes, rgsczPatchCodes, cPatchCodes, DICT_FLAG_CASEINSENSITIVE);
        ButilExitOnFailure(hr, "Failed to create string dictionary for %hs.", "patch codes");

        // Patch relationship: when their patch codes match our detect codes.
        hr = DictCompareStringListToArray(sdPatchCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzDetectCodes), pQueryContext->cDetectCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for patch code match.");

            *pRelationType = BUNDLE_RELATION_PATCH;
            ExitFunction();
        }

        // Patch relationship: when their patch codes match our upgrade codes.
        hr = DictCompareStringListToArray(sdPatchCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzUpgradeCodes), pQueryContext->cUpgradeCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for patch code match.");

            *pRelationType = BUNDLE_RELATION_PATCH;
            ExitFunction();
        }

        ReleaseNullDict(sdPatchCodes);
        ReleaseNullStrArray(rgsczPatchCodes, cPatchCodes);
    }

    // Compare detect codes.
    hr = RegReadStringArray(hkBundleId, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE, &rgsczDetectCodes, &cDetectCodes);
    if (SUCCEEDED(hr))
    {
        hr = DictCreateStringListFromArray(&sdDetectCodes, rgsczDetectCodes, cDetectCodes, DICT_FLAG_CASEINSENSITIVE);
        ButilExitOnFailure(hr, "Failed to create string dictionary for %hs.", "detect codes");

        // Detect relationship: when their detect codes match our detect codes.
        hr = DictCompareStringListToArray(sdDetectCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzDetectCodes), pQueryContext->cDetectCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for detect code match.");

            *pRelationType = BUNDLE_RELATION_DETECT;
            ExitFunction();
        }

        // Dependent relationship: when their detect codes match our addon codes.
        hr = DictCompareStringListToArray(sdDetectCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzAddonCodes), pQueryContext->cAddonCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for addon code match.");

            *pRelationType = BUNDLE_RELATION_DEPENDENT_ADDON;
            ExitFunction();
        }

        // Dependent relationship: when their detect codes match our patch codes.
        hr = DictCompareStringListToArray(sdDetectCodes, const_cast<LPCWSTR*>(pQueryContext->rgwzPatchCodes), pQueryContext->cPatchCodes);
        if (HRESULT_FROM_WIN32(ERROR_NO_MATCH) == hr)
        {
            hr = S_OK;
        }
        else
        {
            ButilExitOnFailure(hr, "Failed to do array search for patch code match.");

            *pRelationType = BUNDLE_RELATION_DEPENDENT_PATCH;
            ExitFunction();
        }

        ReleaseNullDict(sdDetectCodes);
        ReleaseNullStrArray(rgsczDetectCodes, cDetectCodes);
    }

LExit:
    if (SUCCEEDED(hr) && BUNDLE_RELATION_NONE == *pRelationType)
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

static HRESULT LocateAndQueryBundleValue(
    __in_z LPCWSTR wzBundleId,
    __in_opt LPCWSTR wzSubKey,
    __in LPCWSTR wzValueName,
    __inout HKEY* phKey,
    __inout DWORD* pdwType,
    __out INTERNAL_BUNDLE_STATUS* pStatus
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKeypath = NULL;
    BOOL fExists = TRUE;

    *pStatus = INTERNAL_BUNDLE_STATUS_SUCCESS;

    if (wzSubKey)
    {
        hr = StrAllocFormatted(&sczKeypath, L"%ls\\%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, wzBundleId, wzSubKey);
    }
    else
    {
        hr = StrAllocFormatted(&sczKeypath, L"%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, wzBundleId);
    }
    ButilExitOnFailure(hr, "Failed to allocate bundle uninstall key path.");

    if (FAILED(hr = RegOpenEx(HKEY_LOCAL_MACHINE, sczKeypath, KEY_READ, REG_KEY_32BIT, phKey)) &&
        FAILED(hr = RegOpenEx(HKEY_LOCAL_MACHINE, sczKeypath, KEY_READ, REG_KEY_64BIT, phKey)) &&
        FAILED(hr = RegOpenEx(HKEY_CURRENT_USER, sczKeypath, KEY_READ, REG_KEY_DEFAULT, phKey)))
    {
        ButilExitOnPathFailure(hr, fExists, "Failed to open bundle key.");

        *pStatus = INTERNAL_BUNDLE_STATUS_UNKNOWN_BUNDLE;
        ExitFunction1(hr = S_OK);
    }

    hr = RegGetType(*phKey, wzValueName, pdwType);
    ButilExitOnPathFailure(hr, fExists, "Failed to read bundle value.");

    if (!fExists)
    {
        *pStatus = INTERNAL_BUNDLE_STATUS_UNKNOWN_PROPERTY;
        ExitFunction1(hr = S_OK);
    }

LExit:
    ReleaseStr(sczKeypath);

    return hr;
}

static HRESULT CopyStringToBuffer(
    __in_z LPWSTR wzValue,
    __in_z_opt LPWSTR wzBuffer,
    __inout SIZE_T* pcchBuffer
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
        hr = ::StringCchLengthW(wzValue, STRSAFE_MAX_LENGTH, reinterpret_cast<size_t*>(pcchBuffer));
        if (SUCCEEDED(hr))
        {
            hr = E_MOREDATA;
            *pcchBuffer += 1; // null terminator.
        }
    }

    return hr;
}
