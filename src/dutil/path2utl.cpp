// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define PathExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_PATHUTIL, p, x, e, s, __VA_ARGS__)
#define PathExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_PATHUTIL, p, x, s, __VA_ARGS__)
#define PathExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_PATHUTIL, p, x, e, s, __VA_ARGS__)
#define PathExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_PATHUTIL, p, x, s, __VA_ARGS__)
#define PathExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_PATHUTIL, e, x, s, __VA_ARGS__)
#define PathExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_PATHUTIL, g, x, s, __VA_ARGS__)


DAPI_(HRESULT) PathCanonicalizePath(
    __in_z LPCWSTR wzPath,
    __deref_out_z LPWSTR* psczCanonicalized
    )
{
    HRESULT hr = S_OK;
    int cch = MAX_PATH + 1;

    hr = StrAlloc(psczCanonicalized, cch);
    PathExitOnFailure(hr, "Failed to allocate string for the canonicalized path.");

    if (::PathCanonicalizeW(*psczCanonicalized, wzPath))
    {
        hr = S_OK;
    }
    else
    {
        ExitFunctionWithLastError(hr);
    }

LExit:
    return hr;
}

DAPI_(HRESULT) PathDirectoryContainsPath(
    __in_z LPCWSTR wzDirectory,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;
    LPWSTR sczDirectory = NULL;
    LPWSTR sczOriginalPath = NULL;
    LPWSTR sczOriginalDirectory = NULL;

    hr = PathCanonicalizePath(wzPath, &sczOriginalPath);
    PathExitOnFailure(hr, "Failed to canonicalize the path.");

    hr = PathCanonicalizePath(wzDirectory, &sczOriginalDirectory);
    PathExitOnFailure(hr, "Failed to canonicalize the directory.");

    if (!sczOriginalPath || !*sczOriginalPath)
    {
        ExitFunction1(hr = S_FALSE);
    }
    if (!sczOriginalDirectory || !*sczOriginalDirectory)
    {
        ExitFunction1(hr = S_FALSE);
    }

    sczPath = sczOriginalPath;
    sczDirectory = sczOriginalDirectory;

    for (; *sczDirectory;)
    {
        if (!*sczPath)
        {
            ExitFunction1(hr = S_FALSE);
        }

        if (CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, sczDirectory, 1, sczPath, 1))
        {
            ExitFunction1(hr = S_FALSE);
        }

        ++sczDirectory;
        ++sczPath;
    }

    --sczDirectory;
    if (('\\' == *sczDirectory && *sczPath) || '\\' == *sczPath)
    {
        hr = S_OK;
    }
    else
    {
        hr = S_FALSE;
    }

LExit:
    ReleaseStr(sczOriginalPath);
    ReleaseStr(sczOriginalDirectory);
    return hr;
}
