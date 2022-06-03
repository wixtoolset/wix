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

#define PATH_GOOD_ENOUGH 64

static BOOL IsPathSeparatorChar(
    __in WCHAR wc
    );
static BOOL IsValidDriveChar(
    __in WCHAR wc
    );


DAPI_(LPWSTR) PathFile(
    __in_z LPCWSTR wzPath
    )
{
    if (!wzPath)
    {
        return NULL;
    }

    LPWSTR wzFile = const_cast<LPWSTR>(wzPath);
    for (LPWSTR wz = wzFile; *wz; ++wz)
    {
        // valid delineators 
        //     \ => Windows path
        //     / => unix and URL path
        //     : => relative path from mapped root
        if (IsPathSeparatorChar(*wz) || (L':' == *wz && wz == wzPath + 1))
        {
            wzFile = wz + 1;
        }
    }

    return wzFile;
}


DAPI_(LPCWSTR) PathExtension(
    __in_z LPCWSTR wzPath
    )
{
    if (!wzPath)
    {
        return NULL;
    }

    // Find the last dot in the last thing that could be a file.
    LPCWSTR wzExtension = NULL;
    for (LPCWSTR wz = wzPath; *wz; ++wz)
    {
        if (IsPathSeparatorChar(*wz) || L':' == *wz)
        {
            wzExtension = NULL;
        }
        else if (L'.' == *wz)
        {
            wzExtension = wz;
        }
    }

    return wzExtension;
}


DAPI_(HRESULT) PathGetDirectory(
    __in_z LPCWSTR wzPath,
    __out_z LPWSTR *psczDirectory
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzRemaining = NULL;
    SIZE_T cchDirectory = 0;

    for (LPCWSTR wz = wzPath; *wz; ++wz)
    {
        // valid delineators:
        //     \ => Windows path
        //     / => unix and URL path
        //     : => relative path from mapped root
        if (IsPathSeparatorChar(*wz) || (L':' == *wz && wz == wzPath + 1))
        {
            wzRemaining = wz;
        }
    }

    if (!wzRemaining)
    {
        // we were given just a file name, so there's no directory available
        ExitFunction1(hr = S_FALSE);
    }

    cchDirectory = static_cast<SIZE_T>(wzRemaining - wzPath) + 1;

    hr = StrAllocString(psczDirectory, wzPath, cchDirectory);
    PathExitOnFailure(hr, "Failed to copy directory.");

LExit:
    return hr;
}


DAPI_(HRESULT) PathGetParentPath(
    __in_z LPCWSTR wzPath,
    __out_z LPWSTR* psczParent,
    __out_opt SIZE_T* pcchRoot
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzPastRoot = NULL;
    LPCWSTR wzParent = NULL;
    LPCWSTR wz = NULL;

    wzPastRoot = PathSkipPastRoot(wzPath, NULL, NULL, NULL);

    if (pcchRoot)
    {
        *pcchRoot = !wzPastRoot ? 0 : wzPastRoot - wzPath;
    }

    if (wzPastRoot && *wzPastRoot)
    {
        Assert(wzPastRoot > wzPath);
        wz = wzPastRoot;
        wzParent = wzPastRoot - 1;
    }
    else
    {
        wz = wzPath;
    }

    for (; *wz; ++wz)
    {
        if (IsPathSeparatorChar(*wz) && wz[1])
        {
            wzParent = wz;
        }
    }

    if (wzParent)
    {
        size_t cchPath = static_cast<size_t>(wzParent - wzPath) + 1;

        hr = StrAllocString(psczParent, wzPath, cchPath);
        PathExitOnFailure(hr, "Failed to copy directory.");
    }
    else
    {
        ReleaseNullStr(*psczParent);
    }

LExit:
    return hr;
}


DAPI_(HRESULT) PathExpand(
    __out LPWSTR *psczFullPath,
    __in_z LPCWSTR wzRelativePath,
    __in DWORD dwResolveFlags
    )
{
    Assert(wzRelativePath);

    HRESULT hr = S_OK;
    DWORD cch = 0;
    LPWSTR sczExpandedPath = NULL;
    SIZE_T cchWritten = 0;
    DWORD cchExpandedPath = 0;
    LPWSTR sczFullPath = NULL;
    DWORD dwPrefixFlags = 0;

    //
    // First, expand any environment variables.
    //
    if (dwResolveFlags & PATH_EXPAND_ENVIRONMENT)
    {
        cchExpandedPath = PATH_GOOD_ENOUGH;

        hr = StrAlloc(&sczExpandedPath, cchExpandedPath);
        PathExitOnFailure(hr, "Failed to allocate space for expanded path.");

        cch = ::ExpandEnvironmentStringsW(wzRelativePath, sczExpandedPath, cchExpandedPath);
        if (0 == cch)
        {
            PathExitWithLastError(hr, "Failed to expand environment variables in string: %ls", wzRelativePath);
        }
        else if (cchExpandedPath < cch)
        {
            cchExpandedPath = cch;
            hr = StrAlloc(&sczExpandedPath, cchExpandedPath);
            PathExitOnFailure(hr, "Failed to re-allocate more space for expanded path.");

            cch = ::ExpandEnvironmentStringsW(wzRelativePath, sczExpandedPath, cchExpandedPath);
            if (0 == cch)
            {
                PathExitWithLastError(hr, "Failed to expand environment variables in string: %ls", wzRelativePath);
            }
            else if (cchExpandedPath < cch)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                PathExitOnRootFailure(hr, "Failed to allocate buffer for expanded path.");
            }
        }

        cchWritten = cch;
    }

    //
    // Second, get the full path.
    //
    if (dwResolveFlags & PATH_EXPAND_FULLPATH)
    {
        LPCWSTR wzPath = sczExpandedPath ? sczExpandedPath : wzRelativePath;

        hr = PathGetFullPathName(wzPath, &sczFullPath, NULL, &cchWritten);
        PathExitOnFailure(hr, "Failed to get full path for string: %ls", wzPath);

        dwPrefixFlags |= PATH_PREFIX_EXPECT_FULLY_QUALIFIED;
    }
    else
    {
        sczFullPath = sczExpandedPath;
        sczExpandedPath = NULL;
    }

    if (dwResolveFlags)
    {
        hr = PathPrefix(&sczFullPath, cchWritten, dwPrefixFlags);
        PathExitOnFailure(hr, "Failed to prefix path after expanding.");
    }

    hr = StrAllocString(psczFullPath, sczFullPath ? sczFullPath : wzRelativePath, 0);
    PathExitOnFailure(hr, "Failed to copy relative path into full path.");

LExit:
    ReleaseStr(sczFullPath);
    ReleaseStr(sczExpandedPath);

    return hr;
}


DAPI_(HRESULT) PathGetFullPathName(
    __in_z LPCWSTR wzPath,
    __deref_out_z LPWSTR* psczFullPath,
    __inout_z_opt LPCWSTR* pwzFileName,
    __out_opt SIZE_T* pcch
    )
{
    HRESULT hr = S_OK;
    SIZE_T cchMax = 0;
    DWORD cchBuffer = 0;
    DWORD cch = 0;
    DWORD dwAttempts = 0;
    const DWORD dwMaxAttempts = 10;

    if (!wzPath || !*wzPath)
    {
        hr = DirGetCurrent(psczFullPath, pcch);
        PathExitOnFailure(hr, "Failed to get current directory.");

        ExitFunction();
    }

    if (*psczFullPath)
    {
        hr = StrMaxLength(*psczFullPath, &cchMax);
        PathExitOnFailure(hr, "Failed to get max length of input buffer.");

        cchBuffer = (DWORD)min(DWORD_MAX, cchMax);
    }
    else
    {
        cchBuffer = MAX_PATH + 1;

        hr = StrAlloc(psczFullPath, cchBuffer);
        PathExitOnFailure(hr, "Failed to allocate space for full path.");
    }

    for (; dwAttempts < dwMaxAttempts; ++dwAttempts)
    {
        cch = ::GetFullPathNameW(wzPath, cchBuffer, *psczFullPath, const_cast<LPWSTR*>(pwzFileName));
        PathExitOnNullWithLastError(cch, hr, "Failed to get full path for string: %ls", wzPath);

        if (cch < cchBuffer)
        {
            break;
        }

        hr = StrAlloc(psczFullPath, cch);
        PathExitOnFailure(hr, "Failed to reallocate space for full path.");

        cchBuffer = cch;
    }

    if (dwMaxAttempts == dwAttempts)
    {
        PathExitWithRootFailure(hr, E_INSUFFICIENT_BUFFER, "GetFullPathNameW results never converged.");
    }

    if (pcch)
    {
        *pcch = cch;
    }

LExit:
    return hr;
}


DAPI_(HRESULT) PathPrefix(
    __inout_z LPWSTR* psczFullPath,
    __in SIZE_T cchFullPath,
    __in DWORD dwPrefixFlags
    )
{
    Assert(psczFullPath);

    HRESULT hr = S_OK;
    LPWSTR wzFullPath = *psczFullPath;
    BOOL fFullyQualified = FALSE;
    BOOL fHasPrefix = FALSE;
    BOOL fUNC = FALSE;
    SIZE_T cbFullPath = 0;

    PathSkipPastRoot(wzFullPath, &fHasPrefix, &fFullyQualified, &fUNC);

    if (fHasPrefix)
    {
        ExitFunction();
    }

    // The prefix is only allowed on fully qualified paths.
    if (!fFullyQualified)
    {
        if (dwPrefixFlags & PATH_PREFIX_EXPECT_FULLY_QUALIFIED)
        {
            PathExitWithRootFailure(hr, E_INVALIDARG, "Expected fully qualified path provided to prefix: %ls.", wzFullPath);
        }

        ExitFunction();
    }

    if (!(dwPrefixFlags & PATH_PREFIX_SHORT_PATHS))
    {
        // The prefix is not necessary unless the path is longer than MAX_PATH.
        if (!cchFullPath)
        {
            hr = ::StringCchLengthW(wzFullPath, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cchFullPath));
            PathExitOnFailure(hr, "Failed to get length of path to prefix.");
        }

        if (MAX_PATH >= cchFullPath)
        {
            ExitFunction();
        }
    }

    if (fUNC)
    {
        hr = StrSize(*psczFullPath, &cbFullPath);
        PathExitOnFailure(hr, "Failed to get size of full path.");

        memmove_s(wzFullPath, cbFullPath, wzFullPath + 1, cbFullPath - sizeof(WCHAR));
        wzFullPath[0] = L'\\';

        hr = StrAllocPrefix(psczFullPath, L"\\\\?\\UNC", 7);
        PathExitOnFailure(hr, "Failed to add prefix to UNC path.");
    }
    else // must be a normal path
    {
        hr = StrAllocPrefix(psczFullPath, L"\\\\?\\", 4);
        PathExitOnFailure(hr, "Failed to add prefix to file path.");
    }

LExit:
    return hr;
}


DAPI_(HRESULT) PathFixedNormalizeSlashes(
    __inout_z LPWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    size_t cchLength = 0;
    BOOL fAllowDoubleSlash = FALSE;
    SIZE_T iSource = 0;
    SIZE_T jDestination = 0;

    hr = ::StringCchLengthW(wzPath, STRSAFE_MAX_CCH, &cchLength);
    PathExitOnFailure(hr, "Failed to get length of path.");

    if (1 < cchLength && IsPathSeparatorChar(wzPath[0]))
    {
        if (IsPathSeparatorChar(wzPath[1]))
        {
            // \\?\\a\ is not equivalent to \\?\a\ and \\server\\a\ is not equivalent to \\server\a\.
            fAllowDoubleSlash = TRUE;
            wzPath[0] = '\\';
            wzPath[1] = '\\';
            iSource = 2;
            jDestination = 2;
        }
        else if (2 < cchLength && L'?' == wzPath[1] && L'?' == wzPath[2])
        {
            // \??\\a\ is not equivalent to \??\a\.
            fAllowDoubleSlash = TRUE;
            wzPath[0] = '\\';
            wzPath[1] = '?';
            wzPath[2] = '?';
            iSource = 3;
            jDestination = 3;
        }
    }

    for (; iSource < cchLength; ++iSource)
    {
        if (IsPathSeparatorChar(wzPath[iSource]))
        {
            if (fAllowDoubleSlash)
            {
                fAllowDoubleSlash = FALSE;
            }
            else if (IsPathSeparatorChar(wzPath[iSource + 1]))
            {
                // Skip consecutive slashes.
                continue;
            }

            wzPath[jDestination] = '\\';
        }
        else
        {
            wzPath[jDestination] = wzPath[iSource];
        }

        ++jDestination;
    }

    for (; jDestination < cchLength; ++jDestination)
    {
        wzPath[jDestination] = '\0';
    }

LExit:
    return hr;
}


DAPI_(void) PathFixedReplaceForwardSlashes(
    __inout_z LPWSTR wzPath
    )
{
    for (LPWSTR wz = wzPath; *wz; ++wz)
    {
        if (L'/' == *wz)
        {
            *wz = L'\\';
        }
    }
}


DAPI_(HRESULT) PathFixedBackslashTerminate(
    __inout_ecount_z(cchPath) LPWSTR wzPath,
    __in SIZE_T cchPath
    )
{
    HRESULT hr = S_OK;
    size_t cchLength = 0;

    if (!cchPath)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
    }

    hr = ::StringCchLengthW(wzPath, cchPath, &cchLength);
    PathExitOnFailure(hr, "Failed to get length of path.");

    LPWSTR wzLast = wzPath + (cchLength - 1);
    if (cchLength && L'/' == wzLast[0])
    {
        wzLast[0] = L'\\';
    }
    else if (!cchLength || L'\\' != wzLast[0])
    {
        if (cchLength + 2 > cchPath)
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
        }

        wzLast[1] = L'\\';
        wzLast[2] = L'\0';
    }

LExit:
    return hr;
}


DAPI_(HRESULT) PathBackslashTerminate(
    __inout_z LPWSTR* psczPath
    )
{
    Assert(psczPath);

    HRESULT hr = S_OK;
    SIZE_T cchPath = 0;
    size_t cchLength = 0;

    hr = StrMaxLength(*psczPath, &cchPath);
    PathExitOnFailure(hr, "Failed to get size of path string.");

    hr = ::StringCchLengthW(*psczPath, cchPath, &cchLength);
    PathExitOnFailure(hr, "Failed to get length of path.");

    LPWSTR wzLast = *psczPath + (cchLength - 1);
    if (cchLength && L'/' == wzLast[0])
    {
        wzLast[0] = L'\\';
    }
    else if (!cchLength || L'\\' != wzLast[0])
    {
        hr = StrAllocConcat(psczPath, L"\\", 1);
        PathExitOnFailure(hr, "Failed to concat backslash onto string.");
    }

LExit:
    return hr;
}


DAPI_(HRESULT) PathForCurrentProcess(
    __inout LPWSTR *psczFullPath,
    __in_opt HMODULE hModule
    )
{
    HRESULT hr = S_OK;
    DWORD cch = MAX_PATH;

    do
    {
        hr = StrAlloc(psczFullPath, cch);
        PathExitOnFailure(hr, "Failed to allocate string for module path.");

        DWORD cchRequired = ::GetModuleFileNameW(hModule, *psczFullPath, cch);
        if (0 == cchRequired)
        {
            PathExitWithLastError(hr, "Failed to get path for executing process.");
        }
        else if (cchRequired == cch)
        {
            cch = cchRequired + 1;
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        }
        else
        {
            hr = S_OK;
        }
    } while (HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) == hr);

LExit:
    return hr;
}


DAPI_(HRESULT) PathRelativeToModule(
    __inout LPWSTR *psczFullPath,
    __in_opt LPCWSTR wzFileName,
    __in_opt HMODULE hModule
    )
{
    HRESULT hr = PathForCurrentProcess(psczFullPath, hModule);
    PathExitOnFailure(hr, "Failed to get current module path.");

    hr = PathGetDirectory(*psczFullPath, psczFullPath);
    PathExitOnFailure(hr, "Failed to get current module directory.");

    if (wzFileName)
    {
        hr = PathConcat(*psczFullPath, wzFileName, psczFullPath);
        PathExitOnFailure(hr, "Failed to append filename.");
    }

LExit:
    return hr;
}


DAPI_(HRESULT) PathCreateTempFile(
    __in_opt LPCWSTR wzDirectory,
    __in_opt __format_string LPCWSTR wzFileNameTemplate,
    __in DWORD dwUniqueCount,
    __in DWORD dwFileAttributes,
    __out_opt LPWSTR* psczTempFile,
    __out_opt HANDLE* phTempFile
    )
{
    AssertSz(0 < dwUniqueCount, "Must specify a non-zero unique count.");

    HRESULT hr = S_OK;

    LPWSTR sczTempPath = NULL;
    DWORD cchTempPath = MAX_PATH;

    HANDLE hTempFile = INVALID_HANDLE_VALUE;
    LPWSTR scz = NULL;
    LPWSTR sczTempFile = NULL;

    if (wzDirectory && *wzDirectory)
    {
        hr = StrAllocString(&sczTempPath, wzDirectory, 0);
        PathExitOnFailure(hr, "Failed to copy temp path.");
    }
    else
    {
        hr = StrAlloc(&sczTempPath, cchTempPath);
        PathExitOnFailure(hr, "Failed to allocate memory for the temp path.");

        if (!::GetTempPathW(cchTempPath, sczTempPath))
        {
            PathExitWithLastError(hr, "Failed to get temp path.");
        }
    }

    if (wzFileNameTemplate && *wzFileNameTemplate)
    {
        for (DWORD i = 1; i <= dwUniqueCount && INVALID_HANDLE_VALUE == hTempFile; ++i)
        {
            hr = StrAllocFormatted(&scz, wzFileNameTemplate, i);
            PathExitOnFailure(hr, "Failed to allocate memory for file template.");

            hr = StrAllocFormatted(&sczTempFile, L"%s%s", sczTempPath, scz);
            PathExitOnFailure(hr, "Failed to allocate temp file name.");

            hTempFile = ::CreateFileW(sczTempFile, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, CREATE_NEW, dwFileAttributes, NULL);
            if (INVALID_HANDLE_VALUE == hTempFile)
            {
                // if the file already exists, just try again
                hr = HRESULT_FROM_WIN32(::GetLastError());
                if (HRESULT_FROM_WIN32(ERROR_FILE_EXISTS) == hr)
                {
                    hr = S_OK;
                }
                PathExitOnFailure(hr, "Failed to create file: %ls", sczTempFile);
            }
        }
    }

    // If we were not able to or we did not try to create a temp file, ask
    // the system to provide us a temp file using its built-in mechanism.
    if (INVALID_HANDLE_VALUE == hTempFile)
    {
        hr = StrAlloc(&sczTempFile, MAX_PATH);
        PathExitOnFailure(hr, "Failed to allocate memory for the temp path");

        if (!::GetTempFileNameW(sczTempPath, L"TMP", 0, sczTempFile))
        {
            PathExitWithLastError(hr, "Failed to create new temp file name.");
        }

        hTempFile = ::CreateFileW(sczTempFile, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, dwFileAttributes, NULL);
        if (INVALID_HANDLE_VALUE == hTempFile)
        {
            PathExitWithLastError(hr, "Failed to open new temp file: %ls", sczTempFile);
        }
    }

    // If the caller wanted the temp file name or handle, return them here.
    if (psczTempFile)
    {
        hr = StrAllocString(psczTempFile, sczTempFile, 0);
        PathExitOnFailure(hr, "Failed to copy temp file string.");
    }

    if (phTempFile)
    {
        *phTempFile = hTempFile;
        hTempFile = INVALID_HANDLE_VALUE;
    }

LExit:
    if (INVALID_HANDLE_VALUE != hTempFile)
    {
        ::CloseHandle(hTempFile);
    }

    ReleaseStr(scz);
    ReleaseStr(sczTempFile);
    ReleaseStr(sczTempPath);

    return hr;
}


DAPI_(HRESULT) PathCreateTimeBasedTempFile(
    __in_z_opt LPCWSTR wzDirectory,
    __in_z LPCWSTR wzPrefix,
    __in_z_opt LPCWSTR wzPostfix,
    __in_z LPCWSTR wzExtension,
    __deref_opt_out_z LPWSTR* psczTempFile,
    __out_opt HANDLE* phTempFile
    )
{
    HRESULT hr = S_OK;
    BOOL fRetry = FALSE;
    WCHAR wzTempPath[MAX_PATH] = { };
    LPWSTR sczPrefix = NULL;
    LPWSTR sczPrefixFolder = NULL;
    SYSTEMTIME time = { };

    LPWSTR sczTempPath = NULL;
    HANDLE hTempFile = INVALID_HANDLE_VALUE;
    DWORD dwAttempts = 0;

    if (wzDirectory && *wzDirectory)
    {
        hr = PathConcat(wzDirectory, wzPrefix, &sczPrefix);
        PathExitOnFailure(hr, "Failed to combine directory and log prefix.");
    }
    else
    {
        if (!::GetTempPathW(countof(wzTempPath), wzTempPath))
        {
            PathExitWithLastError(hr, "Failed to get temp folder.");
        }

        hr = PathConcat(wzTempPath, wzPrefix, &sczPrefix);
        PathExitOnFailure(hr, "Failed to concatenate the temp folder and log prefix.");
    }

    hr = PathGetDirectory(sczPrefix, &sczPrefixFolder);
    if (S_OK == hr)
    {
        hr = DirEnsureExists(sczPrefixFolder, NULL);
        PathExitOnFailure(hr, "Failed to ensure temp file path exists: %ls", sczPrefixFolder);
    }

    if (!wzPostfix)
    {
        wzPostfix = L"";
    }

    do
    {
        fRetry = FALSE;
        ++dwAttempts;

        ::GetLocalTime(&time);

        // Log format:                         pre YYYY MM  dd  hh  mm  ss post ext
        hr = StrAllocFormatted(&sczTempPath, L"%ls_%04u%02u%02u%02u%02u%02u%ls%ls%ls", sczPrefix, time.wYear, time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond, wzPostfix, L'.' == *wzExtension ? L"" : L".", wzExtension);
        PathExitOnFailure(hr, "failed to allocate memory for the temp path");

        hTempFile = ::CreateFileW(sczTempPath, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
        if (INVALID_HANDLE_VALUE == hTempFile)
        {
            // If the file already exists, just try again.
            DWORD er = ::GetLastError();
            if (ERROR_FILE_EXISTS == er || ERROR_ACCESS_DENIED == er)
            {
                ::Sleep(100);

                if (10 > dwAttempts)
                {
                    er = ERROR_SUCCESS;
                    fRetry = TRUE;
                }
            }

            hr = HRESULT_FROM_WIN32(er);
            PathExitOnFailureDebugTrace(hr, "Failed to create temp file: %ls", sczTempPath);
        }
    } while (fRetry);

    if (psczTempFile)
    {
        hr = StrAllocString(psczTempFile, sczTempPath, 0);
        PathExitOnFailure(hr, "Failed to copy temp path to return.");
    }

    if (phTempFile)
    {
        *phTempFile = hTempFile;
        hTempFile = INVALID_HANDLE_VALUE;
    }

LExit:
    ReleaseFile(hTempFile);
    ReleaseStr(sczTempPath);
    ReleaseStr(sczPrefixFolder);
    ReleaseStr(sczPrefix);

    return hr;
}


DAPI_(HRESULT) PathCreateTempDirectory(
    __in_opt LPCWSTR wzDirectory,
    __in __format_string LPCWSTR wzDirectoryNameTemplate,
    __in DWORD dwUniqueCount,
    __out LPWSTR* psczTempDirectory
    )
{
    AssertSz(wzDirectoryNameTemplate && *wzDirectoryNameTemplate, "DirectoryNameTemplate must be specified.");
    AssertSz(0 < dwUniqueCount, "Must specify a non-zero unique count.");

    HRESULT hr = S_OK;

    LPWSTR sczTempPath = NULL;
    DWORD cchTempPath = MAX_PATH;

    LPWSTR scz = NULL;

    if (wzDirectory && *wzDirectory)
    {
        hr = StrAllocString(&sczTempPath, wzDirectory, 0);
        PathExitOnFailure(hr, "Failed to copy temp path.");

        hr = PathBackslashTerminate(&sczTempPath);
        PathExitOnFailure(hr, "Failed to ensure path ends in backslash: %ls", wzDirectory);
    }
    else
    {
        hr = StrAlloc(&sczTempPath, cchTempPath);
        PathExitOnFailure(hr, "Failed to allocate memory for the temp path.");

        if (!::GetTempPathW(cchTempPath, sczTempPath))
        {
            PathExitWithLastError(hr, "Failed to get temp path.");
        }
    }

    for (DWORD i = 1; i <= dwUniqueCount; ++i)
    {
        hr = StrAllocFormatted(&scz, wzDirectoryNameTemplate, i);
        PathExitOnFailure(hr, "Failed to allocate memory for directory name template.");

        hr = StrAllocFormatted(psczTempDirectory, L"%s%s", sczTempPath, scz);
        PathExitOnFailure(hr, "Failed to allocate temp directory name.");

        if (!::CreateDirectoryW(*psczTempDirectory, NULL))
        {
            DWORD er = ::GetLastError();
            if (ERROR_ALREADY_EXISTS == er)
            {
                hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
                continue;
            }
            else if (ERROR_PATH_NOT_FOUND == er)
            {
                hr = DirEnsureExists(*psczTempDirectory, NULL);
                break;
            }
            else
            {
                hr = HRESULT_FROM_WIN32(er);
                break;
            }
        }
        else
        {
            hr = S_OK;
            break;
        }
    }
    PathExitOnFailure(hr, "Failed to create temp directory.");

    hr = PathBackslashTerminate(psczTempDirectory);
    PathExitOnFailure(hr, "Failed to ensure temp directory is backslash terminated.");

LExit:
    ReleaseStr(scz);
    ReleaseStr(sczTempPath);

    return hr;
}


DAPI_(HRESULT) PathGetTempPath(
    __out_z LPWSTR* psczTempPath
    )
{
    HRESULT hr = S_OK;
    WCHAR wzTempPath[MAX_PATH + 1] = { };
    DWORD cch = 0;

    cch = ::GetTempPathW(countof(wzTempPath), wzTempPath);
    if (!cch)
    {
        PathExitWithLastError(hr, "Failed to GetTempPath.");
    }
    else if (cch >= countof(wzTempPath))
    {
        PathExitWithRootFailure(hr, E_INSUFFICIENT_BUFFER, "TEMP directory path too long.");
    }

    hr = StrAllocString(psczTempPath, wzTempPath, cch);
    PathExitOnFailure(hr, "Failed to copy TEMP directory path.");

LExit:
    return hr;
}


DAPI_(HRESULT) PathGetSystemTempPath(
    __out_z LPWSTR* psczSystemTempPath
    )
{
    HRESULT hr = S_OK;
    HKEY hKey = NULL;
    WCHAR wzTempPath[MAX_PATH + 1] = { };
    DWORD cch = 0;

    // There is no documented API to get system environment variables, so read them from the registry.
    hr = RegOpen(HKEY_LOCAL_MACHINE, L"System\\CurrentControlSet\\Control\\Session Manager\\Environment", KEY_READ, &hKey);
    if (E_FILENOTFOUND != hr)
    {
        PathExitOnFailure(hr, "Failed to open system environment registry key.");

        // Follow documented precedence rules for TMP/TEMP from ::GetTempPath.
        // TODO: values will be expanded with the current environment variables instead of the system environment variables.
        hr = RegReadString(hKey, L"TMP", psczSystemTempPath);
        if (E_FILENOTFOUND != hr)
        {
            PathExitOnFailure(hr, "Failed to get system TMP value.");

            hr = PathBackslashTerminate(psczSystemTempPath);
            PathExitOnFailure(hr, "Failed to backslash terminate system TMP value.");

            ExitFunction();
        }

        hr = RegReadString(hKey, L"TEMP", psczSystemTempPath);
        if (E_FILENOTFOUND != hr)
        {
            PathExitOnFailure(hr, "Failed to get system TEMP value.");

            hr = PathBackslashTerminate(psczSystemTempPath);
            PathExitOnFailure(hr, "Failed to backslash terminate system TEMP value.");

            ExitFunction();
        }
    }

    cch = ::GetSystemWindowsDirectoryW(wzTempPath, countof(wzTempPath));
    if (!cch)
    {
        PathExitWithLastError(hr, "Failed to get Windows directory path.");
    }
    else if (cch >= countof(wzTempPath))
    {
        PathExitWithRootFailure(hr, E_INSUFFICIENT_BUFFER, "Windows directory path too long.");
    }

    hr = PathConcat(wzTempPath, L"TEMP\\", psczSystemTempPath);
    PathExitOnFailure(hr, "Failed to concat Temp directory on Windows directory path.");

LExit:
    ReleaseRegKey(hKey);

    return hr;
}


DAPI_(HRESULT) PathGetKnownFolder(
    __in int csidl,
    __out LPWSTR* psczKnownFolder
    )
{
    HRESULT hr = S_OK;

    hr = StrAlloc(psczKnownFolder, MAX_PATH);
    PathExitOnFailure(hr, "Failed to allocate memory for known folder.");

    hr = ::SHGetFolderPathW(NULL, csidl, NULL, SHGFP_TYPE_CURRENT, *psczKnownFolder);
    PathExitOnFailure(hr, "Failed to get known folder path.");

    hr = PathBackslashTerminate(psczKnownFolder);
    PathExitOnFailure(hr, "Failed to ensure known folder path is backslash terminated.");

LExit:
    return hr;
}


DAPI_(LPCWSTR) PathSkipPastRoot(
    __in_z_opt LPCWSTR wzPath,
    __out_opt BOOL* pfHasExtendedPrefix,
    __out_opt BOOL* pfFullyQualified,
    __out_opt BOOL* pfUNC
    )
{
    LPCWSTR wzPastRoot = NULL;
    BOOL fHasPrefix = FALSE;
    BOOL fFullyQualified = FALSE;
    BOOL fUNC = FALSE;
    DWORD dwRootMissingSlashes = 0;

    if (!wzPath || !*wzPath)
    {
        ExitFunction();
    }

    if (IsPathSeparatorChar(wzPath[0]))
    {
        if (IsPathSeparatorChar(wzPath[1]) && (L'?' == wzPath[2] || L'.' == wzPath[2]) && IsPathSeparatorChar(wzPath[3]) ||
            L'?' == wzPath[1] && L'?' == wzPath[2] && IsPathSeparatorChar(wzPath[3]))
        {
            fHasPrefix = TRUE;

            if (L'U' == wzPath[4] && L'N' == wzPath[5] && L'C' == wzPath[6] && IsPathSeparatorChar(wzPath[7]))
            {
                fUNC = TRUE;
                wzPastRoot = wzPath + 8;
                dwRootMissingSlashes = 2;
            }
            else
            {
                wzPastRoot = wzPath + 4;
                dwRootMissingSlashes = 1;
            }
        }
        else if (IsPathSeparatorChar(wzPath[1]))
        {
            fUNC = TRUE;
            wzPastRoot = wzPath + 2;
            dwRootMissingSlashes = 2;
        }
    }

    if (dwRootMissingSlashes)
    {
        Assert(wzPastRoot);
        fFullyQualified = TRUE;

        for (; *wzPastRoot && dwRootMissingSlashes; ++wzPastRoot)
        {
            if (IsPathSeparatorChar(*wzPastRoot))
            {
                --dwRootMissingSlashes;
            }
        }
    }
    else
    {
        Assert(!wzPastRoot);

        if (IsPathSeparatorChar(wzPath[0]))
        {
            wzPastRoot = wzPath + 1;
        }
        else if (IsValidDriveChar(wzPath[0]) && wzPath[1] == L':')
        {
            if (IsPathSeparatorChar(wzPath[2]))
            {
                fFullyQualified = TRUE;
                wzPastRoot = wzPath + 3;
            }
            else
            {
                wzPastRoot = wzPath + 2;
            }
        }
    }

LExit:
    if (pfHasExtendedPrefix)
    {
        *pfHasExtendedPrefix = fHasPrefix;
    }

    if (pfFullyQualified)
    {
        *pfFullyQualified = fFullyQualified;
    }

    if (pfUNC)
    {
        *pfUNC = fUNC;
    }

    return wzPastRoot;
}


DAPI_(BOOL) PathIsFullyQualified(
    __in_z LPCWSTR wzPath
    )
{
    BOOL fFullyQualified = FALSE;

    PathSkipPastRoot(wzPath, NULL, &fFullyQualified, NULL);

    return fFullyQualified;
}


DAPI_(BOOL) PathIsRooted(
    __in_z LPCWSTR wzPath
    )
{
    return NULL != PathSkipPastRoot(wzPath, NULL, NULL, NULL);
}


DAPI_(HRESULT) PathConcat(
    __in_opt LPCWSTR wzPath1,
    __in_opt LPCWSTR wzPath2,
    __deref_out_z LPWSTR* psczCombined
    )
{
    return PathConcatCch(wzPath1, 0, wzPath2, 0, psczCombined);
}


DAPI_(HRESULT) PathConcatCch(
    __in_opt LPCWSTR wzPath1,
    __in SIZE_T cchPath1,
    __in_opt LPCWSTR wzPath2,
    __in SIZE_T cchPath2,
    __deref_out_z LPWSTR* psczCombined
    )
{
    HRESULT hr = S_OK;

    if (!wzPath2 || !*wzPath2)
    {
        hr = StrAllocString(psczCombined, wzPath1, cchPath1);
        PathExitOnFailure(hr, "Failed to copy just path1 to output.");
    }
    else if (!wzPath1 || !*wzPath1 || PathIsRooted(wzPath2))
    {
        hr = StrAllocString(psczCombined, wzPath2, cchPath2);
        PathExitOnFailure(hr, "Failed to copy just path2 to output.");
    }
    else
    {
        hr = StrAllocString(psczCombined, wzPath1, cchPath1);
        PathExitOnFailure(hr, "Failed to copy path1 to output.");

        hr = PathBackslashTerminate(psczCombined);
        PathExitOnFailure(hr, "Failed to backslashify.");

        hr = StrAllocConcat(psczCombined, wzPath2, cchPath2);
        PathExitOnFailure(hr, "Failed to append path2 to output.");
    }

LExit:
    return hr;
}


DAPI_(HRESULT) PathCompress(
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    HANDLE hPath = INVALID_HANDLE_VALUE;

    hPath = ::CreateFileW(wzPath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, NULL);
    if (INVALID_HANDLE_VALUE == hPath)
    {
        PathExitWithLastError(hr, "Failed to open path %ls for compression.", wzPath);
    }

    DWORD dwBytesReturned = 0;
    USHORT usCompressionFormat = COMPRESSION_FORMAT_DEFAULT;
    if (0 == ::DeviceIoControl(hPath, FSCTL_SET_COMPRESSION, &usCompressionFormat, sizeof(usCompressionFormat), NULL, 0, &dwBytesReturned, NULL))
    {
        // ignore compression attempts on file systems that don't support it
        DWORD er = ::GetLastError();
        if (ERROR_INVALID_FUNCTION != er)
        {
            PathExitOnWin32Error(er, hr, "Failed to set compression state for path %ls.", wzPath);
        }
    }

LExit:
    ReleaseFile(hPath);

    return hr;
}

DAPI_(HRESULT) PathGetHierarchyArray(
    __in_z LPCWSTR wzPath,
    __deref_inout_ecount_opt(*pcPathArray) LPWSTR **prgsczPathArray,
    __inout LPUINT pcPathArray
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wz = NULL;
    SIZE_T cch = 0;
    *pcPathArray = 0;

    PathExitOnNull(wzPath, hr, E_INVALIDARG, "wzPath is required.");

    wz = PathSkipPastRoot(wzPath, NULL, NULL, NULL);
    if (wz)
    {
        cch = wz - wzPath;

        hr = MemEnsureArraySize(reinterpret_cast<void**>(prgsczPathArray), 1, sizeof(LPWSTR), 5);
        PathExitOnFailure(hr, "Failed to allocate array.");

        hr = StrAllocString(*prgsczPathArray, wzPath, cch);
        PathExitOnFailure(hr, "Failed to copy root into array.");

        *pcPathArray += 1;
    }
    else
    {
        wz = wzPath;
    }

    for (; *wz; ++wz)
    {
        ++cch;

        if (IsPathSeparatorChar(*wz) || !wz[1])
        {
            hr = MemEnsureArraySizeForNewItems(reinterpret_cast<void**>(prgsczPathArray), *pcPathArray, 1, sizeof(LPWSTR), 5);
            PathExitOnFailure(hr, "Failed to allocate array.");

            hr = StrAllocString(*prgsczPathArray + *pcPathArray, wzPath, cch);
            PathExitOnFailure(hr, "Failed to copy path into array.");

            *pcPathArray += 1;
        }
    }

LExit:
    return hr;
}

static BOOL IsPathSeparatorChar(
    __in WCHAR wc
    )
{
    return L'/' == wc || L'\\' == wc;
}

static BOOL IsValidDriveChar(
    __in WCHAR wc
    )
{
    return L'a' <= wc && L'z' >= wc ||
           L'A' <= wc && L'Z' >= wc;
}
