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

    hr = OpenPolicyKey(wzPolicyPath, &hk);
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        ExitFunction1(hr = S_FALSE);
    }
    PolcExitOnFailure(hr, "Failed to open policy key: %ls", wzPolicyPath);

    hr = RegReadNumber(hk, wzPolicyName, pdw);
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        ExitFunction1(hr = S_FALSE);
    }
    PolcExitOnFailure(hr, "Failed to open policy key: %ls, name: %ls", wzPolicyPath, wzPolicyName);

LExit:
    ReleaseRegKey(hk);

    if (S_FALSE == hr || FAILED(hr))
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

    hr = OpenPolicyKey(wzPolicyPath, &hk);
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        ExitFunction1(hr = S_FALSE);
    }
    PolcExitOnFailure(hr, "Failed to open policy key: %ls", wzPolicyPath);

    hr = RegReadString(hk, wzPolicyName, pscz);
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        ExitFunction1(hr = S_FALSE);
    }
    PolcExitOnFailure(hr, "Failed to open policy key: %ls, name: %ls", wzPolicyPath, wzPolicyName);

LExit:
    ReleaseRegKey(hk);

    if (S_FALSE == hr || FAILED(hr))
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

    hr = PathConcat(REGISTRY_POLICIES_KEY, wzPolicyPath, &sczPath);
    PolcExitOnFailure(hr, "Failed to combine logging path with root path.");

    hr = RegOpen(HKEY_LOCAL_MACHINE, sczPath, KEY_READ, phk);
    PolcExitOnFailure(hr, "Failed to open policy registry key.");

LExit:
    ReleaseStr(sczPath);

    return hr;
}
