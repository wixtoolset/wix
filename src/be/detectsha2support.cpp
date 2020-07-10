// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// https://gist.github.com/navossoc/7572c7d82243e9f818989e2765e7793a
HRESULT DetectSHA2CodeSigning(
    __out BOOL* pfSupported
    )
{
    HRESULT hr = S_OK;
    HMODULE hModule = NULL;
    FARPROC pfn = NULL;
    DWORD er = ERROR_SUCCESS;

    hr = LoadSystemLibrary(L"wintrust.dll", &hModule);
    ExitOnFailure(hr, "Failed to load wintrust.dll");

    pfn = ::GetProcAddress(hModule, "CryptCATAdminAcquireContext2");
    if (pfn)
    {
        *pfSupported = TRUE;
        ExitFunction1(hr = S_OK);
    }

    er = ::GetLastError();
    if (er == ERROR_PROC_NOT_FOUND)
    {
        *pfSupported = FALSE;
        ExitFunction1(hr = S_OK);
    }

    hr = HRESULT_FROM_WIN32(er);
    ExitOnFailure(hr, "Failed to probe for CryptCATAdminAcquireContext2 in wintrust.dll");

LExit:
    ::FreeLibrary(hModule);

    return hr;
}

HRESULT UtilPerformDetectSHA2CodeSigning(
    __in LPCWSTR wzVariable,
    __in UTIL_SEARCH* /*pSearch*/,
    __in IBundleExtensionEngine* pEngine
    )
{
    HRESULT hr = S_OK;
    BOOL fSupported = FALSE;

    hr = DetectSHA2CodeSigning(&fSupported);
    ExitOnFailure(hr, "DetectSHA2CodeSigning failed.");

    hr = pEngine->SetVariableNumeric(wzVariable, fSupported ? 1 : 0);
    ExitOnFailure(hr, "Failed to set variable '%ls'", wzVariable);

LExit:
    return hr;
}
