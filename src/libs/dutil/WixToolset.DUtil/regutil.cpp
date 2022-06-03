// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define RegExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_REGUTIL, x, e, s, __VA_ARGS__)
#define RegExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_REGUTIL, x, s, __VA_ARGS__)
#define RegExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_REGUTIL, p, x, e, s, __VA_ARGS__)
#define RegExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_REGUTIL, p, x, s, __VA_ARGS__)
#define RegExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_REGUTIL, p, x, e, s, __VA_ARGS__)
#define RegExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_REGUTIL, p, x, s, __VA_ARGS__)
#define RegExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_REGUTIL, e, x, s, __VA_ARGS__)
#define RegExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_REGUTIL, g, x, s, __VA_ARGS__)

static PFN_REGCREATEKEYEXW vpfnRegCreateKeyExW = ::RegCreateKeyExW;
static PFN_REGOPENKEYEXW vpfnRegOpenKeyExW = ::RegOpenKeyExW;
static PFN_REGDELETEKEYEXW vpfnRegDeleteKeyExW = NULL;
static PFN_REGDELETEKEYEXW vpfnRegDeleteKeyExWFromLibrary = NULL;
static PFN_REGDELETEKEYW vpfnRegDeleteKeyW = ::RegDeleteKeyW;
static PFN_REGENUMKEYEXW vpfnRegEnumKeyExW = ::RegEnumKeyExW;
static PFN_REGENUMVALUEW vpfnRegEnumValueW = ::RegEnumValueW;
static PFN_REGQUERYINFOKEYW vpfnRegQueryInfoKeyW = ::RegQueryInfoKeyW;
static PFN_REGQUERYVALUEEXW vpfnRegQueryValueExW = ::RegQueryValueExW;
static PFN_REGSETVALUEEXW vpfnRegSetValueExW = ::RegSetValueExW;
static PFN_REGDELETEVALUEW vpfnRegDeleteValueW = ::RegDeleteValueW;
static PFN_REGGETVALUEW vpfnRegGetValueW = NULL;
static PFN_REGGETVALUEW vpfnRegGetValueWFromLibrary = NULL;

static HMODULE vhAdvApi32Dll = NULL;
static BOOL vfRegInitialized = FALSE;

static HRESULT GetRegValue(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __deref_out_bcount_opt(*pcbBuffer) BYTE* pbBuffer,
    __inout SIZE_T* pcbBuffer,
    __out DWORD* pdwType
    );
static HRESULT WriteStringToRegistry(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD dwType
    );

DAPI_(HRESULT) RegInitialize()
{
    HRESULT hr = S_OK;

    hr = LoadSystemLibrary(L"AdvApi32.dll", &vhAdvApi32Dll);
    RegExitOnFailure(hr, "Failed to load AdvApi32.dll");

    // Ignore failures - if this doesn't exist, we'll fall back to RegDeleteKeyW.
    vpfnRegDeleteKeyExWFromLibrary = reinterpret_cast<PFN_REGDELETEKEYEXW>(::GetProcAddress(vhAdvApi32Dll, "RegDeleteKeyExW"));

    // Ignore failures - if this doesn't exist, we'll fall back to RegQueryValueExW.
    vpfnRegGetValueWFromLibrary = reinterpret_cast<PFN_REGGETVALUEW>(::GetProcAddress(vhAdvApi32Dll, "RegGetValueW"));

    if (!vpfnRegDeleteKeyExW)
    {
        vpfnRegDeleteKeyExW = vpfnRegDeleteKeyExWFromLibrary;
    }

    if (!vpfnRegGetValueW)
    {
        vpfnRegGetValueW = vpfnRegGetValueWFromLibrary;
    }

    vfRegInitialized = TRUE;

LExit:
    return hr;
}


DAPI_(void) RegUninitialize()
{
    if (vhAdvApi32Dll)
    {
        ::FreeLibrary(vhAdvApi32Dll);
        vhAdvApi32Dll = NULL;
        vpfnRegDeleteKeyExWFromLibrary = NULL;
        vpfnRegGetValueWFromLibrary = NULL;
        vpfnRegDeleteKeyExW = NULL;
        vpfnRegGetValueW = NULL;
    }

    vfRegInitialized = FALSE;
}


DAPI_(void) RegFunctionOverride(
    __in_opt PFN_REGCREATEKEYEXW pfnRegCreateKeyExW,
    __in_opt PFN_REGOPENKEYEXW pfnRegOpenKeyExW,
    __in_opt PFN_REGDELETEKEYEXW pfnRegDeleteKeyExW,
    __in_opt PFN_REGENUMKEYEXW pfnRegEnumKeyExW,
    __in_opt PFN_REGENUMVALUEW pfnRegEnumValueW,
    __in_opt PFN_REGQUERYINFOKEYW pfnRegQueryInfoKeyW,
    __in_opt PFN_REGQUERYVALUEEXW pfnRegQueryValueExW,
    __in_opt PFN_REGSETVALUEEXW pfnRegSetValueExW,
    __in_opt PFN_REGDELETEVALUEW pfnRegDeleteValueW,
    __in_opt PFN_REGGETVALUEW pfnRegGetValueW
    )
{
    vpfnRegCreateKeyExW = pfnRegCreateKeyExW ? pfnRegCreateKeyExW : ::RegCreateKeyExW;
    vpfnRegOpenKeyExW = pfnRegOpenKeyExW ? pfnRegOpenKeyExW : ::RegOpenKeyExW;
    vpfnRegDeleteKeyExW = pfnRegDeleteKeyExW ? pfnRegDeleteKeyExW : vpfnRegDeleteKeyExWFromLibrary;
    vpfnRegEnumKeyExW = pfnRegEnumKeyExW ? pfnRegEnumKeyExW : ::RegEnumKeyExW;
    vpfnRegEnumValueW = pfnRegEnumValueW ? pfnRegEnumValueW : ::RegEnumValueW;
    vpfnRegQueryInfoKeyW = pfnRegQueryInfoKeyW ? pfnRegQueryInfoKeyW : ::RegQueryInfoKeyW;
    vpfnRegQueryValueExW = pfnRegQueryValueExW ? pfnRegQueryValueExW : ::RegQueryValueExW;
    vpfnRegSetValueExW = pfnRegSetValueExW ? pfnRegSetValueExW : ::RegSetValueExW;
    vpfnRegDeleteValueW = pfnRegDeleteValueW ? pfnRegDeleteValueW : ::RegDeleteValueW;
    vpfnRegGetValueW = pfnRegGetValueW ? pfnRegGetValueW : vpfnRegGetValueWFromLibrary;
}


DAPI_(void) RegFunctionForceFallback()
{
    vpfnRegDeleteKeyExW = NULL;
    vpfnRegGetValueW = NULL;
}


DAPI_(HRESULT) RegCreate(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in DWORD dwAccess,
    __out HKEY* phk
    )
{
    HRESULT hr = S_OK;

    hr = RegCreateEx(hkRoot, wzSubKey, dwAccess, REG_KEY_DEFAULT, FALSE, NULL, phk, NULL);
    RegExitOnFailure(hr, "Failed to create registry key.");

LExit:
    return hr;
}


DAPI_(HRESULT) RegCreateEx(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in DWORD dwAccess,
    __in REG_KEY_BITNESS kbKeyBitness,
    __in BOOL fVolatile,
    __in_opt SECURITY_ATTRIBUTES* pSecurityAttributes,
    __out HKEY* phk,
    __out_opt BOOL* pfCreated
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwDisposition;

    REGSAM samDesired = RegTranslateKeyBitness(kbKeyBitness);
    er = vpfnRegCreateKeyExW(hkRoot, wzSubKey, 0, NULL, fVolatile ? REG_OPTION_VOLATILE : REG_OPTION_NON_VOLATILE, dwAccess | samDesired, pSecurityAttributes, phk, &dwDisposition);
    RegExitOnWin32Error(er, hr, "Failed to create registry key.");

    if (pfCreated)
    {
        *pfCreated = (REG_CREATED_NEW_KEY == dwDisposition);
    }

LExit:
    return hr;
}


DAPI_(HRESULT) RegOpen(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in DWORD dwAccess,
    __out HKEY* phk
)
{
    return RegOpenEx(hkRoot, wzSubKey, dwAccess, REG_KEY_DEFAULT, phk);
}


DAPI_(HRESULT) RegOpenEx(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in DWORD dwAccess,
    __in REG_KEY_BITNESS kbKeyBitness,
    __out HKEY* phk
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    REGSAM samDesired = RegTranslateKeyBitness(kbKeyBitness);
    er = vpfnRegOpenKeyExW(hkRoot, wzSubKey, 0, dwAccess | samDesired, phk);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to open registry key.");

LExit:
    return hr;
}


DAPI_(HRESULT) RegDelete(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in REG_KEY_BITNESS kbKeyBitness,
    __in BOOL fDeleteTree
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR pszEnumeratedSubKey = NULL;
    LPWSTR pszRecursiveSubKey = NULL;
    HKEY hkKey = NULL;

    if (!vfRegInitialized && REG_KEY_DEFAULT != kbKeyBitness)
    {
        hr = E_INVALIDARG;
        RegExitOnFailure(hr, "RegInitialize must be called first in order to RegDelete() a key with non-default bit attributes!");
    }

    if (fDeleteTree)
    {
        hr = RegOpenEx(hkRoot, wzSubKey, KEY_READ, kbKeyBitness, &hkKey);
        if (E_FILENOTFOUND == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        RegExitOnFailure(hr, "Failed to open this key for enumerating subkeys: %ls", wzSubKey);

        // Yes, keep enumerating the 0th item, because we're deleting it every time
        while (E_NOMOREITEMS != (hr = RegKeyEnum(hkKey, 0, &pszEnumeratedSubKey)))
        {
            RegExitOnFailure(hr, "Failed to enumerate key 0");

            hr = PathConcat(wzSubKey, pszEnumeratedSubKey, &pszRecursiveSubKey);
            RegExitOnFailure(hr, "Failed to concatenate paths while recursively deleting subkeys. Path1: %ls, Path2: %ls", wzSubKey, pszEnumeratedSubKey);

            hr = RegDelete(hkRoot, pszRecursiveSubKey, kbKeyBitness, fDeleteTree);
            RegExitOnFailure(hr, "Failed to recursively delete subkey: %ls", pszRecursiveSubKey);
        }

        hr = S_OK;
    }

    if (NULL != vpfnRegDeleteKeyExW)
    {
        REGSAM samDesired = RegTranslateKeyBitness(kbKeyBitness);
        er = vpfnRegDeleteKeyExW(hkRoot, wzSubKey, samDesired, 0);
        if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
        {
            ExitFunction1(hr = E_FILENOTFOUND);
        }
        RegExitOnWin32Error(er, hr, "Failed to delete registry key (ex).");
    }
    else
    {
        er = vpfnRegDeleteKeyW(hkRoot, wzSubKey);
        if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
        {
            ExitFunction1(hr = E_FILENOTFOUND);
        }
        RegExitOnWin32Error(er, hr, "Failed to delete registry key.");
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(pszEnumeratedSubKey);
    ReleaseStr(pszRecursiveSubKey);

    return hr;
}

DAPI_(HRESULT) RegKeyEnum(
    __in HKEY hk,
    __in DWORD dwIndex,
    __deref_out_z LPWSTR* psczKey
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    SIZE_T cb = 0;
    DWORD cch = 0;

    if (psczKey && *psczKey)
    {
        hr = StrMaxLength(*psczKey, &cb);
        RegExitOnFailure(hr, "Failed to determine length of string.");

        cch = (DWORD)min(DWORD_MAX, cb);
    }

    if (2 > cch)
    {
        cch = 2;

        hr = StrAlloc(psczKey, cch);
        RegExitOnFailure(hr, "Failed to allocate string to minimum size.");
    }

    er = vpfnRegEnumKeyExW(hk, dwIndex, *psczKey, &cch, NULL, NULL, NULL, NULL);
    if (ERROR_MORE_DATA == er)
    {
        er = vpfnRegQueryInfoKeyW(hk, NULL, NULL, NULL, NULL, &cch, NULL, NULL, NULL, NULL, NULL, NULL);
        RegExitOnWin32Error(er, hr, "Failed to get max size of subkey name under registry key.");

        ++cch; // add one because RegQueryInfoKeyW() returns the length of the subkeys without the null terminator.
        hr = StrAlloc(psczKey, cch);
        RegExitOnFailure(hr, "Failed to allocate string bigger for enum registry key.");

        er = vpfnRegEnumKeyExW(hk, dwIndex, *psczKey, &cch, NULL, NULL, NULL, NULL);
    }
    else if (ERROR_NO_MORE_ITEMS == er)
    {
        ExitFunction1(hr = E_NOMOREITEMS);
    }
    RegExitOnWin32Error(er, hr, "Failed to enum registry key.");

    // Always ensure the registry key name is null terminated.
#pragma prefast(push)
#pragma prefast(disable:26018)
    (*psczKey)[cch] = L'\0'; // note that cch will always be one less than the size of the buffer because that's how RegEnumKeyExW() works.
#pragma prefast(pop)

LExit:
    return hr;
}


DAPI_(HRESULT) RegValueEnum(
    __in HKEY hk,
    __in DWORD dwIndex,
    __deref_out_z LPWSTR* psczName,
    __out_opt DWORD *pdwType
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD cbValueName = 0;

    er = vpfnRegQueryInfoKeyW(hk, NULL, NULL, NULL, NULL, NULL, NULL, NULL, &cbValueName, NULL, NULL, NULL);
    RegExitOnWin32Error(er, hr, "Failed to get max size of value name under registry key.");

    // Add one for null terminator
    ++cbValueName;

    hr = StrAlloc(psczName, cbValueName);
    RegExitOnFailure(hr, "Failed to allocate array for registry value name");

    er = vpfnRegEnumValueW(hk, dwIndex, *psczName, &cbValueName, NULL, pdwType, NULL, NULL);
    if (ERROR_NO_MORE_ITEMS == er)
    {
        ExitFunction1(hr = E_NOMOREITEMS);
    }
    RegExitOnWin32Error(er, hr, "Failed to enumerate registry value");

LExit:
    return hr;
}

DAPI_(HRESULT) RegGetType(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __out DWORD *pdwType
     )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegQueryValueExW(hk, wzName, NULL, pdwType, NULL, NULL);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to read registry value.");
LExit:

    return hr;
}

DAPI_(HRESULT) RegReadValue(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in BOOL fExpand,
    __deref_out_bcount_opt(*pcbBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* pcbBuffer,
    __out DWORD* pdwType
    )
{
    HRESULT hr = S_OK;
    DWORD dwAttempts = 0;
    LPWSTR sczExpand = NULL;

    hr = GetRegValue(hk, wzName, *ppbBuffer, pcbBuffer, pdwType);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction();
    }
    else if (E_MOREDATA != hr)
    {
        RegExitOnFailure(hr, "Failed to get size of raw registry value.");

        // Zero-length raw values can exist
        if (!*ppbBuffer && 0 < *pcbBuffer)
        {
            hr = E_MOREDATA;
        }
    }

    while (E_MOREDATA == hr && dwAttempts < 10)
    {
        ++dwAttempts;

        if (*ppbBuffer)
        {
            *ppbBuffer = static_cast<LPBYTE>(MemReAlloc(*ppbBuffer, *pcbBuffer, FALSE));
        }
        else
        {
            *ppbBuffer = static_cast<LPBYTE>(MemAlloc(*pcbBuffer, FALSE));
        }
        RegExitOnNull(*ppbBuffer, hr, E_OUTOFMEMORY, "Failed to allocate buffer for raw registry value.");

        hr = GetRegValue(hk, wzName, *ppbBuffer, pcbBuffer, pdwType);
        if (E_FILENOTFOUND == hr)
        {
            ExitFunction();
        }
        else if (E_MOREDATA != hr)
        {
            RegExitOnFailure(hr, "Failed to read raw registry value.");
        }
    }

    if (fExpand && SUCCEEDED(hr) && REG_EXPAND_SZ == *pdwType)
    {
        LPWSTR sczValue = reinterpret_cast<LPWSTR>(*ppbBuffer);
        hr = PathExpand(&sczExpand, sczValue, PATH_EXPAND_ENVIRONMENT);
        RegExitOnFailure(hr, "Failed to expand registry value: %ls", sczValue);

        *ppbBuffer = reinterpret_cast<LPBYTE>(sczExpand);
        *pcbBuffer = (lstrlenW(sczExpand) + 1) * sizeof(WCHAR);
        sczExpand = NULL;
        ReleaseMem(sczValue);
    }

LExit:
    ReleaseStr(sczExpand);

    return hr;
}

DAPI_(HRESULT) RegReadBinary(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __deref_out_bcount_opt(*pcbBuffer) BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    )
{
    HRESULT hr = S_OK;
    DWORD dwType = 0;

    hr = RegReadValue(hk, wzName, FALSE, ppbBuffer, pcbBuffer, &dwType);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction();
    }
    RegExitOnFailure(hr, "Failed to read binary registry value.");

    if (REG_BINARY != dwType)
    {
        RegExitWithRootFailure(hr, HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE), "Error reading binary registry value due to unexpected data type: %u", dwType);
    }

LExit:
    return hr;
}


DAPI_(HRESULT) RegReadString(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __deref_out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    SIZE_T cbValue = 0;
    DWORD dwType = 0;

    if (psczValue && *psczValue)
    {
        hr = MemSizeChecked(*psczValue, &cbValue);
        RegExitOnFailure(hr, "Failed to get size of input buffer.");
    }

    hr = RegReadValue(hk, wzName, TRUE, reinterpret_cast<LPBYTE*>(psczValue), &cbValue, &dwType);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction();
    }
    RegExitOnFailure(hr, "Failed to read string registry value.");

    if (REG_EXPAND_SZ != dwType && REG_SZ != dwType)
    {
        RegExitWithRootFailure(hr, HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE), "Error reading string registry value due to unexpected data type: %u", dwType);
    }

LExit:
    return hr;
}


DAPI_(HRESULT) RegReadUnexpandedString(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __inout BOOL* pfNeedsExpansion,
    __deref_out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    SIZE_T cbValue = 0;
    DWORD dwType = 0;
    LPWSTR sczExpand = NULL;

    if (psczValue && *psczValue)
    {
        hr = MemSizeChecked(*psczValue, &cbValue);
        RegExitOnFailure(hr, "Failed to get size of input buffer.");
    }

    hr = RegReadValue(hk, wzName, FALSE, reinterpret_cast<LPBYTE*>(psczValue), &cbValue, &dwType);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction();
    }
    RegExitOnFailure(hr, "Failed to read expand string registry value.");

    if (REG_EXPAND_SZ != dwType && REG_SZ != dwType)
    {
        RegExitWithRootFailure(hr, HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE), "Error reading expand string registry value due to unexpected data type: %u", dwType);
    }

    *pfNeedsExpansion = REG_EXPAND_SZ == dwType;

LExit:
    ReleaseStr(sczExpand);

    return hr;
}


DAPI_(HRESULT) RegReadStringArray(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __deref_out_ecount_opt(*pcStrings) LPWSTR** prgsczStrings,
    __out DWORD *pcStrings
    )
{
    HRESULT hr = S_OK;
    DWORD dwNullCharacters = 0;
    DWORD dwType = 0;
    SIZE_T cb = 0;
    SIZE_T cch = 0;
    LPCWSTR wzSource = NULL;
    LPWSTR sczValue = NULL;

    hr = RegReadValue(hk, wzName, FALSE, reinterpret_cast<LPBYTE*>(&sczValue), &cb, &dwType);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction();
    }
    RegExitOnFailure(hr, "Failed to read string array registry value.");

    if (REG_MULTI_SZ != dwType)
    {
        RegExitWithRootFailure(hr, HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE), "Tried to read string array, but registry value %ls is of an incorrect type", wzName);
    }

    cch = cb / sizeof(WCHAR);

    for (DWORD i = 0; i < cch; ++i)
    {
        if (L'\0' == sczValue[i])
        {
            ++dwNullCharacters;
        }
    }

    // Value exists, but is empty, so no strings to return.
    if (!cb || 1 == dwNullCharacters && 1 == cch || 2 == dwNullCharacters && 2 == cch)
    {
        *prgsczStrings = NULL;
        *pcStrings = 0;
        ExitFunction1(hr = S_OK);
    }

    if (sczValue[cch - 1] != L'\0')
    {
        // Count the terminating null character that RegReadValue added past the end.
        ++dwNullCharacters;
        Assert(!sczValue[cch]);
    }
    else if (cch > 1 && sczValue[cch - 2] == L'\0')
    {
        // Don't count the extra 1 at the end of the properly double-null terminated string.
        --dwNullCharacters;
    }

    // There's one string for every null character encountered.
    *pcStrings = dwNullCharacters;
    hr = MemEnsureArraySize(reinterpret_cast<LPVOID *>(prgsczStrings), *pcStrings, sizeof(LPWSTR), 0);
    RegExitOnFailure(hr, "Failed to resize array while reading REG_MULTI_SZ value");

#pragma prefast(push)
#pragma prefast(disable:26010)
    wzSource = sczValue;
    for (DWORD i = 0; i < *pcStrings; ++i)
    {
        cch = lstrlenW(wzSource);

        hr = StrAllocString(&(*prgsczStrings)[i], wzSource, cch);
        RegExitOnFailure(hr, "Failed to allocate copy of string");

        // Skip past this string
        wzSource += cch + 1;
    }
#pragma prefast(pop)

LExit:
    ReleaseStr(sczValue);

    return hr;
}


DAPI_(HRESULT) RegReadVersion(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __out DWORD64* pdw64Version
    )
{
    HRESULT hr = S_OK;
    DWORD dwType = 0;
    SIZE_T cb = 0;
    LPWSTR sczValue = NULL;

    hr = RegReadValue(hk, wzName, TRUE, reinterpret_cast<LPBYTE*>(&sczValue), &cb, &dwType);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction();
    }
    RegExitOnFailure(hr, "Failed to read version registry value.");

    if (REG_SZ == dwType || REG_EXPAND_SZ == dwType)
    {
        hr = FileVersionFromStringEx(sczValue, 0, pdw64Version);
        RegExitOnFailure(hr, "Failed to convert registry string to version.");
    }
    else if (REG_QWORD == dwType)
    {
        if (memcpy_s(pdw64Version, sizeof(DWORD64), sczValue, cb))
        {
            RegExitWithRootFailure(hr, E_INVALIDARG, "Failed to copy QWORD version value.");
        }
    }
    else // unexpected data type
    {
        RegExitWithRootFailure(hr, HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE), "Error reading version registry value due to unexpected data type: %u", dwType);
    }

LExit:
    ReleaseStr(sczValue);

    return hr;
}


DAPI_(HRESULT) RegReadWixVersion(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __out VERUTIL_VERSION** ppVersion
    )
{
    HRESULT hr = S_OK;
    DWORD dwType = 0;
    SIZE_T cb = 0;
    DWORD64 dw64Version = 0;
    LPWSTR sczValue = NULL;
    VERUTIL_VERSION* pVersion = NULL;

    hr = RegReadValue(hk, wzName, TRUE, reinterpret_cast<LPBYTE*>(&sczValue), &cb, &dwType);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction();
    }
    RegExitOnFailure(hr, "Failed to read wix version registry value.");

    if (REG_SZ == dwType || REG_EXPAND_SZ == dwType)
    {
        hr = VerParseVersion(sczValue, 0, FALSE, &pVersion);
        RegExitOnFailure(hr, "Failed to convert registry string to wix version.");
    }
    else if (REG_QWORD == dwType)
    {
        if (memcpy_s(&dw64Version, sizeof(DWORD64), sczValue, cb))
        {
            RegExitWithRootFailure(hr, E_INVALIDARG, "Failed to copy QWORD wix version value.");
        }

        hr = VerVersionFromQword(dw64Version, &pVersion);
        RegExitOnFailure(hr, "Failed to convert registry string to wix version.");
    }
    else // unexpected data type
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Error reading wix version registry value due to unexpected data type: %u", dwType);
    }

    *ppVersion = pVersion;
    pVersion = NULL;

LExit:
    ReleaseVerutilVersion(pVersion);
    ReleaseStr(sczValue);

    return hr;
}


DAPI_(HRESULT) RegReadNone(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName
    )
{
    HRESULT hr = S_OK;
    DWORD dwType = 0;

    hr = RegGetType(hk, wzName, &dwType);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction();
    }
    RegExitOnFailure(hr, "Error reading none registry value type.");

    if (REG_NONE != dwType)
    {
        RegExitWithRootFailure(hr, HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE), "Error reading none registry value due to unexpected data type: %u", dwType);
    }

LExit:
    return hr;
}

DAPI_(HRESULT) RegReadNumber(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __out DWORD* pdwValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwType = 0;
    DWORD cb = sizeof(DWORD);

    er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, reinterpret_cast<LPBYTE>(pdwValue), &cb);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to query registry key value.");

    if (REG_DWORD != dwType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Error reading version registry value due to unexpected data type: %u", dwType);
    }

LExit:
    return hr;
}


DAPI_(HRESULT) RegReadQword(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __out DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwType = 0;
    DWORD cb = sizeof(DWORD64);

    er = vpfnRegQueryValueExW(hk, wzName, NULL, &dwType, reinterpret_cast<LPBYTE>(pqwValue), &cb);
    if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
    {
        ExitFunction1(hr = E_FILENOTFOUND);
    }
    RegExitOnWin32Error(er, hr, "Failed to query registry key value.");

    if (REG_QWORD != dwType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATATYPE);
        RegExitOnRootFailure(hr, "Error reading version registry value due to unexpected data type: %u", dwType);
    }

LExit:
    return hr;
}


DAPI_(HRESULT) RegWriteBinary(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_bcount(cbBuffer) const BYTE *pbBuffer,
    __in DWORD cbBuffer
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_BINARY, pbBuffer, cbBuffer);
    RegExitOnWin32Error(er, hr, "Failed to write binary registry value with name: %ls", wzName);

LExit:
    return hr;
}


DAPI_(HRESULT) RegWriteExpandString(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_z_opt LPCWSTR wzValue
    )
{
    return WriteStringToRegistry(hk, wzName, wzValue, REG_EXPAND_SZ);
}


DAPI_(HRESULT) RegWriteString(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_z_opt LPCWSTR wzValue
    )
{
    return WriteStringToRegistry(hk, wzName, wzValue, REG_SZ);
}


DAPIV_(HRESULT) RegWriteStringFormatted(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in __format_string LPCWSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    va_list args;

    va_start(args, szFormat);
    hr = StrAllocFormattedArgs(&sczValue, szFormat, args);
    va_end(args);
    RegExitOnFailure(hr, "Failed to allocate %ls value.", wzName);

    hr = WriteStringToRegistry(hk, wzName, sczValue, REG_SZ);

LExit:
    ReleaseStr(sczValue);

    return hr;
}


DAPI_(HRESULT) RegWriteStringArray(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_ecount(cValues) LPWSTR *rgwzValues,
    __in DWORD cValues
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR wzCopyDestination = NULL;
    LPCWSTR wzWriteValue = NULL;
    LPWSTR sczWriteValue = NULL;
    DWORD dwTotalStringSize = 0;
    DWORD cbTotalStringSize = 0;
    DWORD dwTemp = 0;
    DWORD dwRemainingStringSize = 0;

    if (cValues)
    {
        // Add space for the null terminator
        dwTotalStringSize = 1;

        for (DWORD i = 0; i < cValues; ++i)
        {
            dwTemp = dwTotalStringSize;
            hr = ::DWordAdd(dwTemp, 1 + lstrlenW(rgwzValues[i]), &dwTotalStringSize);
            RegExitOnFailure(hr, "DWORD Overflow while adding length of string to write REG_MULTI_SZ");
        }

        hr = StrAlloc(&sczWriteValue, dwTotalStringSize);
        RegExitOnFailure(hr, "Failed to allocate space for string while writing REG_MULTI_SZ");

        wzCopyDestination = sczWriteValue;
        dwRemainingStringSize = dwTotalStringSize;
        for (DWORD i = 0; i < cValues; ++i)
        {
            hr = ::StringCchCopyW(wzCopyDestination, dwRemainingStringSize, rgwzValues[i]);
            RegExitOnFailure(hr, "failed to copy string: %ls", rgwzValues[i]);

            dwTemp = lstrlenW(rgwzValues[i]) + 1;
            dwRemainingStringSize -= dwTemp;
            wzCopyDestination += dwTemp;
        }

        wzWriteValue = sczWriteValue;

        hr = ::DWordMult(dwTotalStringSize, sizeof(WCHAR), &cbTotalStringSize);
        RegExitOnFailure(hr, "Failed to get total string size in bytes");
    }

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_MULTI_SZ, reinterpret_cast<const BYTE *>(wzWriteValue), cbTotalStringSize);
    RegExitOnWin32Error(er, hr, "Failed to set registry value to array of strings (first string of which is): %ls", wzWriteValue);

LExit:
    ReleaseStr(sczWriteValue);

    return hr;
}

DAPI_(HRESULT) RegWriteNone(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_NONE, NULL, NULL);
    RegExitOnWin32Error(er, hr, "Failed to set %ls value.", wzName);

LExit:
    return hr;
}

DAPI_(HRESULT) RegWriteNumber(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in DWORD dwValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_DWORD, reinterpret_cast<const BYTE *>(&dwValue), sizeof(dwValue));
    RegExitOnWin32Error(er, hr, "Failed to set %ls value.", wzName);

LExit:
    return hr;
}

DAPI_(HRESULT) RegWriteQword(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegSetValueExW(hk, wzName, 0, REG_QWORD, reinterpret_cast<const BYTE *>(&qwValue), sizeof(qwValue));
    RegExitOnWin32Error(er, hr, "Failed to set %ls value.", wzName);

LExit:
    return hr;
}

DAPI_(HRESULT) RegQueryKey(
    __in HKEY hk,
    __out_opt DWORD* pcSubKeys,
    __out_opt DWORD* pcValues
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnRegQueryInfoKeyW(hk, NULL, NULL, NULL, pcSubKeys, NULL, NULL, pcValues, NULL, NULL, NULL, NULL);
    RegExitOnWin32Error(er, hr, "Failed to get the number of subkeys and values under registry key.");

LExit:
    return hr;
}

DAPI_(HRESULT) RegKeyReadNumber(
    __in HKEY hk,
    __in_z LPCWSTR wzSubKey,
    __in_z_opt LPCWSTR wzName,
    __in REG_KEY_BITNESS kbKeyBitness,
    __out DWORD* pdwValue
    )
{
    HRESULT hr = S_OK;
    HKEY hkKey = NULL;

    hr = RegOpenEx(hk, wzSubKey, KEY_READ, kbKeyBitness, &hkKey);
    RegExitOnFailure(hr, "Failed to open key: %ls", wzSubKey);

    hr = RegReadNumber(hkKey, wzName, pdwValue);
    RegExitOnFailure(hr, "Failed to read value: %ls/@%ls", wzSubKey, wzName);

LExit:
    ReleaseRegKey(hkKey);

    return hr;
}

DAPI_(BOOL) RegValueExists(
    __in HKEY hk,
    __in_z LPCWSTR wzSubKey,
    __in_z_opt LPCWSTR wzName,
    __in REG_KEY_BITNESS kbKeyBitness
    )
{
    HRESULT hr = S_OK;
    HKEY hkKey = NULL;
    DWORD dwType = 0;

    hr = RegOpenEx(hk, wzSubKey, KEY_READ, kbKeyBitness, &hkKey);
    RegExitOnFailure(hr, "Failed to open key: %ls", wzSubKey);

    hr = RegGetType(hkKey, wzName, &dwType);
    RegExitOnFailure(hr, "Failed to read value type: %ls/@%ls", wzSubKey, wzName);

LExit:
    ReleaseRegKey(hkKey);

    return SUCCEEDED(hr);
}

DAPI_(REGSAM) RegTranslateKeyBitness(
    __in REG_KEY_BITNESS kbKeyBitness
    )
{
    switch (kbKeyBitness)
    {
    case REG_KEY_32BIT:
        return KEY_WOW64_32KEY;
    case REG_KEY_64BIT:
        return KEY_WOW64_64KEY;
    case REG_KEY_DEFAULT:
    default:
        return 0;
    }
}

static HRESULT GetRegValue(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __deref_out_bcount_opt(*pcbBuffer) BYTE* pbBuffer,
    __inout SIZE_T* pcbBuffer,
    __out DWORD* pdwType
    )
{
    HRESULT hr = S_OK;
    DWORD cb = (DWORD)min(DWORD_MAX, *pcbBuffer);

    if (vpfnRegGetValueW)
    {
        hr = HRESULT_FROM_WIN32(vpfnRegGetValueW(hk, NULL, wzName, RRF_RT_ANY | RRF_NOEXPAND, pdwType, pbBuffer, &cb));
    }
    else
    {
        hr = HRESULT_FROM_WIN32(vpfnRegQueryValueExW(hk, wzName, NULL, pdwType, pbBuffer, &cb));

        if (REG_SZ == *pdwType || REG_EXPAND_SZ == *pdwType || REG_MULTI_SZ == *pdwType)
        {
            if (E_MOREDATA == hr || S_OK == hr && (cb + sizeof(WCHAR)) > *pcbBuffer)
            {
                // Make sure there's room for a null terminator at the end.
                HRESULT hrAdd = ::DWordAdd(cb, sizeof(WCHAR), &cb);

                hr = FAILED(hrAdd) ? hrAdd : (pbBuffer ? E_MOREDATA : S_OK);
            }
            else if (S_OK == hr && pbBuffer)
            {
                // Always ensure the registry value is null terminated.
                WCHAR* pch = reinterpret_cast<WCHAR*>(pbBuffer + cb);
                *pch = L'\0';
            }
        }
    }

    *pcbBuffer = cb;

    return hr;
}

static HRESULT WriteStringToRegistry(
    __in HKEY hk,
    __in_z_opt LPCWSTR wzName,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD dwType
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    size_t cbValue = 0;

    if (wzValue)
    {
        hr = ::StringCbLengthW(wzValue, DWORD_MAX, &cbValue);
        RegExitOnFailure(hr, "Failed to determine length of registry value: %ls", wzName);

        // Need to include the null terminator.
        cbValue += sizeof(WCHAR);

        er = vpfnRegSetValueExW(hk, wzName, 0, dwType, reinterpret_cast<const BYTE *>(wzValue), static_cast<DWORD>(cbValue));
        RegExitOnWin32Error(er, hr, "Failed to set registry value: %ls", wzName);
    }
    else
    {
        er = vpfnRegDeleteValueW(hk, wzName);
        if (ERROR_FILE_NOT_FOUND == er || ERROR_PATH_NOT_FOUND == er)
        {
            er = ERROR_SUCCESS;
        }
        RegExitOnWin32Error(er, hr, "Failed to delete registry value: %ls", wzName);
    }

LExit:
    return hr;
}
