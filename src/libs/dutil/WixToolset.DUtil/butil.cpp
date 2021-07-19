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

// constants
// From engine/registration.h
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE = L"BundleUpgradeCode";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY = L"BundleProviderKey";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_VARIABLE_KEY = L"variables";

enum INTERNAL_BUNDLE_STATUS
{
    INTERNAL_BUNDLE_STATUS_SUCCESS,
    INTERNAL_BUNDLE_STATUS_UNKNOWN_BUNDLE,
    INTERNAL_BUNDLE_STATUS_UNKNOWN_PROPERTY,
};

// Forward declarations.
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

/********************************************************************
OpenBundleKey - Opens the bundle uninstallation key for a given bundle

NOTE: caller is responsible for closing key
********************************************************************/
static HRESULT OpenBundleKey(
    __in_z LPCWSTR wzBundleId,
    __in BUNDLE_INSTALL_CONTEXT context,
    __in_opt LPCWSTR wzSubKey,
    __inout HKEY* phKey
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


DAPI_(HRESULT) BundleEnumRelatedBundle(
  __in_z LPCWSTR wzUpgradeCode,
  __in BUNDLE_INSTALL_CONTEXT context,
  __inout PDWORD pdwStartIndex,
  __out_ecount(MAX_GUID_CHARS+1) LPWSTR lpBundleIdBuf
    )
{
    HRESULT hr = S_OK;
    HKEY hkRoot = BUNDLE_INSTALL_CONTEXT_USER == context ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
    HKEY hkUninstall = NULL;
    HKEY hkBundle = NULL;
    LPWSTR sczUninstallSubKey = NULL;
    size_t cchUninstallSubKey = 0;
    LPWSTR sczUninstallSubKeyPath = NULL;
    LPWSTR sczValue = NULL;
    DWORD dwType = 0;

    LPWSTR* rgsczBundleUpgradeCodes = NULL;
    DWORD cBundleUpgradeCodes = 0;
    BOOL fUpgradeCodeFound = FALSE;

    if (!wzUpgradeCode || !lpBundleIdBuf || !pdwStartIndex)
    {
        ButilExitOnFailure(hr = E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    hr = RegOpen(hkRoot, BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, KEY_READ, &hkUninstall);
    ButilExitOnFailure(hr, "Failed to open bundle uninstall key path.");

    for (DWORD dwIndex = *pdwStartIndex; !fUpgradeCodeFound; dwIndex++)
    {
        hr = RegKeyEnum(hkUninstall, dwIndex, &sczUninstallSubKey);
        ButilExitOnFailure(hr, "Failed to enumerate bundle uninstall key path.");

        hr = StrAllocFormatted(&sczUninstallSubKeyPath, L"%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, sczUninstallSubKey);
        ButilExitOnFailure(hr, "Failed to allocate bundle uninstall key path.");
        
        hr = RegOpen(hkRoot, sczUninstallSubKeyPath, KEY_READ, &hkBundle);
        ButilExitOnFailure(hr, "Failed to open uninstall key path.");

        // If it's a bundle, it should have a BundleUpgradeCode value of type REG_SZ (old) or REG_MULTI_SZ
        hr = RegGetType(hkBundle, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &dwType);
        if (FAILED(hr))
        {
            ReleaseRegKey(hkBundle);
            ReleaseNullStr(sczUninstallSubKey);
            ReleaseNullStr(sczUninstallSubKeyPath);
            // Not a bundle
            continue;
        }

        switch (dwType)
        {
            case REG_SZ:
                hr = RegReadString(hkBundle, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &sczValue);
                ButilExitOnFailure(hr, "Failed to read BundleUpgradeCode string property.");

                if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczValue, -1, wzUpgradeCode, -1))
                {
                    *pdwStartIndex = dwIndex;
                    fUpgradeCodeFound = TRUE;
                    break;
                }

                ReleaseNullStr(sczValue);

                break;
            case REG_MULTI_SZ:
                hr = RegReadStringArray(hkBundle, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &rgsczBundleUpgradeCodes, &cBundleUpgradeCodes);
                ButilExitOnFailure(hr, "Failed to read BundleUpgradeCode  multi-string property.");

                for (DWORD i = 0; i < cBundleUpgradeCodes; i++)
                {
                    LPWSTR wzBundleUpgradeCode = rgsczBundleUpgradeCodes[i];
                    if (wzBundleUpgradeCode && *wzBundleUpgradeCode)
                    {
                        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzBundleUpgradeCode, -1, wzUpgradeCode, -1))
                        {
                            *pdwStartIndex = dwIndex;
                            fUpgradeCodeFound = TRUE;
                            break;
                        }
                    }
                }
                ReleaseNullStrArray(rgsczBundleUpgradeCodes, cBundleUpgradeCodes);

                break;

            default:
                ButilExitWithRootFailure(hr, E_NOTIMPL, "BundleUpgradeCode of type 0x%x not implemented.", dwType);
        }

        if (fUpgradeCodeFound)
        {
            if (lpBundleIdBuf)
            {
                hr = ::StringCchLengthW(sczUninstallSubKey, STRSAFE_MAX_CCH, &cchUninstallSubKey);
                ButilExitOnRootFailure(hr, "Failed to calculate length of string.");

                hr = ::StringCchCopyNExW(lpBundleIdBuf, MAX_GUID_CHARS + 1, sczUninstallSubKey, cchUninstallSubKey, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
                ButilExitOnRootFailure(hr, "Failed to copy the property value to the output buffer.");
            }

            break;
        }

        // Cleanup before next iteration
        ReleaseRegKey(hkBundle);
        ReleaseNullStr(sczUninstallSubKey);
        ReleaseNullStr(sczUninstallSubKeyPath);
    }

LExit:
    ReleaseStr(sczValue);
    ReleaseStr(sczUninstallSubKey);
    ReleaseStr(sczUninstallSubKeyPath);
    ReleaseRegKey(hkBundle);
    ReleaseRegKey(hkUninstall);
    ReleaseStrArray(rgsczBundleUpgradeCodes, cBundleUpgradeCodes);

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

    *pStatus = INTERNAL_BUNDLE_STATUS_SUCCESS;

    if (FAILED(hr = OpenBundleKey(wzBundleId, BUNDLE_INSTALL_CONTEXT_MACHINE, wzSubKey, phKey)) &&
        FAILED(hr = OpenBundleKey(wzBundleId, BUNDLE_INSTALL_CONTEXT_USER, wzSubKey, phKey)))
    {
        if (E_FILENOTFOUND == hr)
        {
            *pStatus = INTERNAL_BUNDLE_STATUS_UNKNOWN_BUNDLE;
            ExitFunction1(hr = S_OK);
        }

        ButilExitOnFailure(hr, "Failed to open bundle key.");
    }

    // If the bundle doesn't have the value defined, return ERROR_UNKNOWN_PROPERTY
    hr = RegGetType(*phKey, wzValueName, pdwType);
    if (FAILED(hr))
    {
        if (E_FILENOTFOUND == hr)
        {
            *pStatus = INTERNAL_BUNDLE_STATUS_UNKNOWN_PROPERTY;
            ExitFunction1(hr = S_OK);
        }

        ButilExitOnFailure(hr, "Failed to read bundle value.");
    }

LExit:
    return hr;
}

static HRESULT OpenBundleKey(
    __in_z LPCWSTR wzBundleId,
    __in BUNDLE_INSTALL_CONTEXT context,
    __in_opt LPCWSTR wzSubKey,
    __inout HKEY* phKey
    )
{
    Assert(phKey && wzBundleId);
    AssertSz(NULL == *phKey, "*key should be null");

    HRESULT hr = S_OK;
    HKEY hkRoot = BUNDLE_INSTALL_CONTEXT_USER == context ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
    LPWSTR sczKeypath = NULL;

    if (wzSubKey)
    {
        hr = StrAllocFormatted(&sczKeypath, L"%ls\\%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, wzBundleId, wzSubKey);
    }
    else
    {
        hr = StrAllocFormatted(&sczKeypath, L"%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, wzBundleId);
    }
    ButilExitOnFailure(hr, "Failed to allocate bundle uninstall key path.");

    hr = RegOpen(hkRoot, sczKeypath, KEY_READ, phKey);
    ButilExitOnFailure(hr, "Failed to open bundle uninstall key path.");

LExit:
    ReleaseStr(sczKeypath);

    return hr;
}
