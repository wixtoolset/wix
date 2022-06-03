// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define PathExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_PATHUTIL, x, e, s, __VA_ARGS__)
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

DAPI_(HRESULT) PathCanonicalizeForComparison(
    __in_z LPCWSTR wzPath,
    __in DWORD dwCanonicalizeFlags,
    __deref_out_z LPWSTR* psczCanonicalized
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczNormalizedPath = NULL;
    LPCWSTR wzNormalizedPath = NULL;
    SIZE_T cchUncRootLength = 0;
    BOOL fHasPrefix = FALSE;

    hr = StrAllocString(&sczNormalizedPath, wzPath, 0);
    PathExitOnFailure(hr, "Failed to allocate string for the normalized path.");

    PathFixedNormalizeSlashes(sczNormalizedPath);

    wzNormalizedPath = sczNormalizedPath;

    if (PATH_CANONICALIZE_KEEP_UNC_ROOT & dwCanonicalizeFlags)
    {
        BOOL fUNC = FALSE;
        LPCWSTR wzPastRoot = PathSkipPastRoot(sczNormalizedPath, NULL, NULL, &fUNC);
        if (fUNC)
        {
            wzNormalizedPath = wzPastRoot;
            cchUncRootLength = wzPastRoot - sczNormalizedPath;
        }
    }

    if (*wzNormalizedPath)
    {
        hr = PathCanonicalizePath(wzNormalizedPath, psczCanonicalized);
        PathExitOnFailure(hr, "Failed to canonicalize: %ls", wzNormalizedPath);
    }
    else
    {
        Assert(cchUncRootLength);
        ReleaseStr(*psczCanonicalized);
        *psczCanonicalized = sczNormalizedPath;
        sczNormalizedPath = NULL;
        cchUncRootLength = 0;
    }

    if (cchUncRootLength)
    {
        hr = StrAllocPrefix(psczCanonicalized, sczNormalizedPath, cchUncRootLength);
        PathExitOnFailure(hr, "Failed to prefix the UNC root to the canonicalized path.");
    }

    if (PATH_CANONICALIZE_BACKSLASH_TERMINATE & dwCanonicalizeFlags)
    {
        hr = PathBackslashTerminate(psczCanonicalized);
        PathExitOnFailure(hr, "Failed to backslash terminate the canonicalized path.");
    }

    if (PATH_CANONICALIZE_APPEND_EXTENDED_PATH_PREFIX & dwCanonicalizeFlags)
    {
        hr = PathPrefix(psczCanonicalized, 0, PATH_PREFIX_SHORT_PATHS);
        PathExitOnFailure(hr, "Failed to ensure the extended path prefix on the canonicalized path.");
    }

    PathSkipPastRoot(*psczCanonicalized, &fHasPrefix, NULL, NULL);

    if (fHasPrefix)
    {
        // Canonicalize prefix into \\?\.
        (*psczCanonicalized)[0] = L'\\';
        (*psczCanonicalized)[1] = L'\\';
        (*psczCanonicalized)[2] = L'?';
        (*psczCanonicalized)[3] = L'\\';
    }

LExit:
    ReleaseStr(sczNormalizedPath);

    return hr;
}

DAPI_(HRESULT) PathConcatRelativeToBase(
    __in LPCWSTR wzBase,
    __in_opt LPCWSTR wzRelative,
    __deref_out_z LPWSTR* psczCombined
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCanonicalizedRelative = NULL;

    if (!wzBase || !*wzBase)
    {
        PathExitWithRootFailure(hr, E_INVALIDARG, "wzBase is required.");
    }

    if (PathIsRooted(wzRelative))
    {
        PathExitWithRootFailure(hr, E_INVALIDARG, "wzRelative cannot be rooted.");
    }

    hr = StrAllocString(psczCombined, wzBase, 0);
    PathExitOnFailure(hr, "Failed to copy base to output.");

    if (wzRelative && *wzRelative)
    {
        hr = PathBackslashTerminate(psczCombined);
        PathExitOnFailure(hr, "Failed to backslashify.");

        hr = PathCanonicalizeForComparison(wzRelative, 0, &sczCanonicalizedRelative);
        PathExitOnFailure(hr, "Failed to canonicalize wzRelative.");

        hr = StrAllocConcat(psczCombined, sczCanonicalizedRelative, 0);
        PathExitOnFailure(hr, "Failed to append relative to output.");
    }

LExit:
    ReleaseStr(sczCanonicalizedRelative);

    return hr;
}

DAPI_(HRESULT) PathCompareCanonicalized(
    __in_z LPCWSTR wzPath1,
    __in_z LPCWSTR wzPath2,
    __out BOOL* pfEqual
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCanonicalized1 = NULL;
    LPWSTR sczCanonicalized2 = NULL;
    DWORD dwDefaultFlags = PATH_CANONICALIZE_APPEND_EXTENDED_PATH_PREFIX | PATH_CANONICALIZE_KEEP_UNC_ROOT;
    int nResult = 0;

    if (!wzPath1 || !wzPath2)
    {
        PathExitWithRootFailure(hr, E_INVALIDARG, "Both paths are required.");
    }

    hr = PathCanonicalizeForComparison(wzPath1, dwDefaultFlags, &sczCanonicalized1);
    PathExitOnFailure(hr, "Failed to canonicalize wzPath1.");

    hr = PathCanonicalizeForComparison(wzPath2, dwDefaultFlags, &sczCanonicalized2);
    PathExitOnFailure(hr, "Failed to canonicalize wzPath2.");

    nResult = ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, sczCanonicalized1, -1, sczCanonicalized2, -1);
    PathExitOnNullWithLastError(nResult, hr, "Failed to compare canonicalized paths.");

    *pfEqual = CSTR_EQUAL == nResult;

LExit:
    ReleaseStr(sczCanonicalized1);
    ReleaseStr(sczCanonicalized2);
    return hr;
}

DAPI_(HRESULT) PathDirectoryContainsPath(
    __in_z LPCWSTR wzDirectory,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCanonicalizedDirectory = NULL;
    LPWSTR sczCanonicalizedPath = NULL;
    DWORD dwDefaultFlags = PATH_CANONICALIZE_APPEND_EXTENDED_PATH_PREFIX | PATH_CANONICALIZE_KEEP_UNC_ROOT;
    size_t cchDirectory = 0;

    if (!wzDirectory || !*wzDirectory)
    {
        PathExitWithRootFailure(hr, E_INVALIDARG, "wzDirectory is required.");
    }
    if (!wzPath || !*wzPath)
    {
        PathExitWithRootFailure(hr, E_INVALIDARG, "wzPath is required.");
    }

    hr = PathCanonicalizeForComparison(wzDirectory, dwDefaultFlags | PATH_CANONICALIZE_BACKSLASH_TERMINATE, &sczCanonicalizedDirectory);
    PathExitOnFailure(hr, "Failed to canonicalize the directory.");

    hr = PathCanonicalizeForComparison(wzPath, dwDefaultFlags, &sczCanonicalizedPath);
    PathExitOnFailure(hr, "Failed to canonicalize the path.");

    if (!PathIsFullyQualified(sczCanonicalizedDirectory))
    {
        PathExitWithRootFailure(hr, E_INVALIDARG, "wzDirectory must be a fully qualified path.");
    }
    if (!sczCanonicalizedPath || !*sczCanonicalizedPath)
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = ::StringCchLengthW(sczCanonicalizedDirectory, STRSAFE_MAX_CCH, &cchDirectory);
    PathExitOnFailure(hr, "Failed to get length of canonicalized directory.");

    if (CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, sczCanonicalizedDirectory, (DWORD)cchDirectory, sczCanonicalizedPath, (DWORD)cchDirectory))
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = sczCanonicalizedPath[cchDirectory] ? S_OK : S_FALSE;

LExit:
    ReleaseStr(sczCanonicalizedPath);
    ReleaseStr(sczCanonicalizedDirectory);
    return hr;
}


DAPI_(HRESULT) PathSystemWindowsSubdirectory(
    __in_z_opt LPCWSTR wzSubdirectory,
    __out_z LPWSTR* psczFullPath
    )
{
    HRESULT hr = S_OK;
    WCHAR wzTempPath[MAX_PATH + 1] = { };
    DWORD cch = 0;

    cch = ::GetSystemWindowsDirectoryW(wzTempPath, countof(wzTempPath));
    if (!cch)
    {
        PathExitWithLastError(hr, "Failed to get Windows directory path.");
    }
    else if (cch >= countof(wzTempPath))
    {
        PathExitWithRootFailure(hr, E_INSUFFICIENT_BUFFER, "Windows directory path too long.");
    }

    if (wzSubdirectory)
    {
        hr = PathConcatRelativeToBase(wzTempPath, wzSubdirectory, psczFullPath);
        PathExitOnFailure(hr, "Failed to concat subdirectory on Windows directory path.");
    }
    else
    {
        hr = StrAllocString(psczFullPath, wzTempPath, 0);
        PathExitOnFailure(hr, "Failed to copy Windows directory path.");
    }

    hr = PathBackslashTerminate(psczFullPath);
    PathExitOnFailure(hr, "Failed to terminate Windows directory path with backslash.");

LExit:
    return hr;
}
