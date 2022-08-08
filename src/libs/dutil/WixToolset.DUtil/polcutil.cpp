// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define PolcExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_POLCUTIL, x, s, __VA_ARGS__)
#define PolcExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_POLCUTIL, x, s, __VA_ARGS__)
#define PolcExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_POLCUTIL, x, s, __VA_ARGS__)
#define PolcExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_POLCUTIL, x, s, __VA_ARGS__)
#define PolcExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_POLCUTIL, x, s, __VA_ARGS__)
#define PolcExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_POLCUTIL, x, s, __VA_ARGS__)
#define PolcExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_POLCUTIL, p, x, e, s, __VA_ARGS__)
#define PolcExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_POLCUTIL, p, x, s, __VA_ARGS__)
#define PolcExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_POLCUTIL, p, x, e, s, __VA_ARGS__)
#define PolcExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_POLCUTIL, p, x, s, __VA_ARGS__)
#define PolcExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_POLCUTIL, e, x, s, __VA_ARGS__)
#define PolcExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_POLCUTIL, g, x, s, __VA_ARGS__)
#define PolcExitOnPathFailure(x, b, s, ...) ExitOnPathFailureSource(DUTIL_SOURCE_POLCUTIL, x, b, s, __VA_ARGS__)

const LPCWSTR REGISTRY_POLICIES_KEY = L"SOFTWARE\\Policies\\";

static HRESULT OpenPolicyKey(
    __in_z LPCWSTR wzPolicyPath,
    __out HKEY* phk
    );


extern "C" HRESULT DAPI PolcReadNumber(
    __in_z LPCWSTR wzPolicyPath,
    __in_z LPCWSTR wzPolicyName,
    __in DWORD dwDefault,
    __out DWORD* pdw
    )
{
    HRESULT hr = S_OK;
    HKEY hk = NULL;
    BOOL fExists = FALSE;

    hr = OpenPolicyKey(wzPolicyPath, &hk);
    PolcExitOnFailure(hr, "Failed to open policy key: %ls", wzPolicyPath);

    if (!hk)
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = RegReadNumber(hk, wzPolicyName, pdw);
    PolcExitOnPathFailure(hr, fExists, "Failed to open policy key: %ls, name: %ls", wzPolicyPath, wzPolicyName);

    if (!fExists)
    {
        ExitFunction1(hr = S_FALSE);
    }

LExit:
    ReleaseRegKey(hk);

    if (!fExists)
    {
        *pdw = dwDefault;
    }

    return hr;
}

extern "C" HRESULT DAPI PolcReadString(
    __in_z LPCWSTR wzPolicyPath,
    __in_z LPCWSTR wzPolicyName,
    __in_z_opt LPCWSTR wzDefault,
    __deref_out_z LPWSTR* pscz
    )
{
    HRESULT hr = S_OK;
    HKEY hk = NULL;
    BOOL fExists = FALSE;

    hr = OpenPolicyKey(wzPolicyPath, &hk);
    PolcExitOnFailure(hr, "Failed to open policy key: %ls", wzPolicyPath);

    if (!hk)
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = RegReadString(hk, wzPolicyName, pscz);
    PolcExitOnPathFailure(hr, fExists, "Failed to open policy key: %ls, name: %ls", wzPolicyPath, wzPolicyName);

    if (!fExists)
    {
        ExitFunction1(hr = S_FALSE);
    }

LExit:
    ReleaseRegKey(hk);

    if (!fExists)
    {
        if (NULL == wzDefault)
        {
            ReleaseNullStr(*pscz);
        }
        else
        {
            hr = StrAllocString(pscz, wzDefault, 0);
        }
    }

    return hr;
}

extern "C" HRESULT DAPI PolcReadUnexpandedString(
    __in_z LPCWSTR wzPolicyPath,
    __in_z LPCWSTR wzPolicyName,
    __in_z_opt LPCWSTR wzDefault,
    __inout BOOL* pfNeedsExpansion,
    __deref_out_z LPWSTR* pscz
    )
{
    HRESULT hr = S_OK;
    HKEY hk = NULL;
    BOOL fExists = FALSE;

    hr = OpenPolicyKey(wzPolicyPath, &hk);
    PolcExitOnFailure(hr, "Failed to open policy key: %ls", wzPolicyPath);

    if (!hk)
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = RegReadUnexpandedString(hk, wzPolicyName, pfNeedsExpansion, pscz);
    PolcExitOnPathFailure(hr, fExists, "Failed to open policy key: %ls, name: %ls", wzPolicyPath, wzPolicyName);

    if (!fExists)
    {
        ExitFunction1(hr = S_FALSE);
    }

LExit:
    ReleaseRegKey(hk);

    if (!fExists)
    {
        if (NULL == wzDefault)
        {
            ReleaseNullStr(*pscz);
        }
        else
        {
            hr = StrAllocString(pscz, wzDefault, 0);
        }
    }

    return hr;
}


// internal functions

static HRESULT OpenPolicyKey(
    __in_z LPCWSTR wzPolicyPath,
    __out HKEY* phk
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;
    BOOL fExists = FALSE;

    hr = PathConcat(REGISTRY_POLICIES_KEY, wzPolicyPath, &sczPath);
    PolcExitOnFailure(hr, "Failed to combine logging path with root path.");

    hr = RegOpen(HKEY_LOCAL_MACHINE, sczPath, KEY_READ, phk);
    PolcExitOnPathFailure(hr, fExists, "Failed to open policy registry key.");

    if (!fExists)
    {
        ReleaseRegKey(*phk);
    }

LExit:
    ReleaseStr(sczPath);

    return hr;
}
