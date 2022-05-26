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
        if (L'\\' == sczNormalizedPath[0] && (L'\\' == sczNormalizedPath[1] || L'?' == sczNormalizedPath[1]) && L'?' == sczNormalizedPath[2] && L'\\' == sczNormalizedPath[3])
        {
            if (L'U' == sczNormalizedPath[4] && L'N' == sczNormalizedPath[5] && L'C' == sczNormalizedPath[6] && L'\\' == sczNormalizedPath[7])
            {
                cchUncRootLength = 8;
            }
        }
        else if (L'\\' == sczNormalizedPath[0] && L'\\' == sczNormalizedPath[1])
        {
            cchUncRootLength = 2;
        }

        if (cchUncRootLength)
        {
            DWORD dwRemainingSlashes = 2;

            for (wzNormalizedPath += cchUncRootLength; *wzNormalizedPath && dwRemainingSlashes; ++wzNormalizedPath)
            {
                ++cchUncRootLength;

                if (L'\\' == *wzNormalizedPath)
                {
                    --dwRemainingSlashes;
                }
            }
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
        PathExitOnFailure(hr, "Failed to backslash terminate the canonicalized path");
    }

    if ((PATH_CANONICALIZE_APPEND_LONG_PATH_PREFIX & dwCanonicalizeFlags) &&
        PathIsFullyQualified(*psczCanonicalized, &fHasPrefix) && !fHasPrefix)
    {
        hr = PathPrefix(psczCanonicalized);
        PathExitOnFailure(hr, "Failed to ensure the long path prefix on the canonicalized path");
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

DAPI_(HRESULT) PathDirectoryContainsPath(
    __in_z LPCWSTR wzDirectory,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCanonicalizedDirectory = NULL;
    LPWSTR sczCanonicalizedPath = NULL;
    DWORD dwDefaultFlags = PATH_CANONICALIZE_APPEND_LONG_PATH_PREFIX | PATH_CANONICALIZE_KEEP_UNC_ROOT;
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

    if (!PathIsFullyQualified(sczCanonicalizedDirectory, NULL))
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
