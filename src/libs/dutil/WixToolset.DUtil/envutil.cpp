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

#define ENV_GOOD_ENOUGH 64

DAPI_(HRESULT) EnvExpandEnvironmentStrings(
    __in LPCWSTR wzSource,
    __out LPWSTR* psczExpanded,
    __out_opt SIZE_T* pcchExpanded
    )
{
    HRESULT hr = S_OK;
    DWORD cch = 0;
    DWORD cchExpanded = 0;
    SIZE_T cchMax = 0;

    if (*psczExpanded)
    {
        hr = StrMaxLength(*psczExpanded, &cchMax);
        EnvExitOnFailure(hr, "Failed to get max length of input buffer.");

        cchExpanded = (DWORD)min(DWORD_MAX, cchMax);
    }
    else
    {
        cchExpanded = ENV_GOOD_ENOUGH;

        hr = StrAlloc(psczExpanded, cchExpanded);
        EnvExitOnFailure(hr, "Failed to allocate space for expanded path.");
    }

    cch = ::ExpandEnvironmentStringsW(wzSource, *psczExpanded, cchExpanded);
    if (!cch)
    {
        EnvExitWithLastError(hr, "Failed to expand environment variables in string: %ls", wzSource);
    }
    else if (cchExpanded < cch)
    {
        cchExpanded = cch;
        hr = StrAlloc(psczExpanded, cchExpanded);
        EnvExitOnFailure(hr, "Failed to re-allocate more space for expanded path.");

        cch = ::ExpandEnvironmentStringsW(wzSource, *psczExpanded, cchExpanded);
        if (!cch)
        {
            EnvExitWithLastError(hr, "Failed to expand environment variables in string: %ls", wzSource);
        }
        else if (cchExpanded < cch)
        {
            EnvExitWithRootFailure(hr, HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER), "Failed to allocate buffer for expanded string.");
        }
    }

    if (pcchExpanded)
    {
        *pcchExpanded = cch;
    }

LExit:
    return hr;
}
