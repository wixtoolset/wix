// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define EnvExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_ENVUTIL, x, s, __VA_ARGS__)
#define EnvExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_ENVUTIL, x, s, __VA_ARGS__)
#define EnvExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_ENVUTIL, x, s, __VA_ARGS__)
#define EnvExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_ENVUTIL, x, s, __VA_ARGS__)
#define EnvExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_ENVUTIL, x, s, __VA_ARGS__)
#define EnvExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_ENVUTIL, x, e, s, __VA_ARGS__)
#define EnvExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_ENVUTIL, x, s, __VA_ARGS__)
#define EnvExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_ENVUTIL, p, x, e, s, __VA_ARGS__)
#define EnvExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_ENVUTIL, p, x, s, __VA_ARGS__)
#define EnvExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_ENVUTIL, p, x, e, s, __VA_ARGS__)
#define EnvExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_ENVUTIL, p, x, s, __VA_ARGS__)
#define EnvExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_ENVUTIL, e, x, s, __VA_ARGS__)
#define EnvExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_ENVUTIL, g, x, s, __VA_ARGS__)

#define ENV2_GOOD_ENOUGH 64

DAPI_(HRESULT) EnvExpandEnvironmentStringsForUser(
    __in_opt HANDLE hToken,
    __in LPCWSTR wzSource,
    __out LPWSTR* psczExpanded,
    __out_opt SIZE_T* pcchExpanded
    )
{
    HRESULT hr = S_OK;
    DWORD cchExpanded = 0;
    SIZE_T cchMax = 0;
    const DWORD dwMaxAttempts = 20;

    if (*psczExpanded)
    {
        hr = StrMaxLength(*psczExpanded, &cchMax);
        EnvExitOnFailure(hr, "Failed to get max length of input buffer.");

        cchExpanded = (DWORD)min(DWORD_MAX, cchMax);
    }
    else
    {
        cchExpanded = ENV2_GOOD_ENOUGH;

        hr = StrAlloc(psczExpanded, cchExpanded);
        EnvExitOnFailure(hr, "Failed to allocate space for expanded path.");
    }

    for (DWORD i = 0; i < dwMaxAttempts; ++i)
    {
        if (::ExpandEnvironmentStringsForUserW(hToken, wzSource, *psczExpanded, cchExpanded))
        {
            break;
        }

        hr = HRESULT_FROM_WIN32(::GetLastError());
        if (E_INSUFFICIENT_BUFFER != hr || (dwMaxAttempts - 1) == i)
        {
            EnvExitWithRootFailure(hr, hr, "Failed to expand environment variables in string: %ls", wzSource);
        }

        cchExpanded *= 2;

        hr = StrAlloc(psczExpanded, cchExpanded);
        EnvExitOnFailure(hr, "Failed to re-allocate more space for expanded path.");
    }

    if (pcchExpanded)
    {
        hr = ::StringCchLengthW(*psczExpanded, STRSAFE_MAX_LENGTH, reinterpret_cast<size_t*>(pcchExpanded));
        EnvExitOnFailure(hr, "Failed to get max length of written input buffer.");

        // Add 1 for null terminator.
        *pcchExpanded += 1;
    }

LExit:
    return hr;
}
