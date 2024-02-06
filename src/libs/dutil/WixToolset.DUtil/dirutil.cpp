// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define DirExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_DIRUTIL, x, s, __VA_ARGS__)
#define DirExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_DIRUTIL, x, s, __VA_ARGS__)
#define DirExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_DIRUTIL, x, s, __VA_ARGS__)
#define DirExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_DIRUTIL, x, s, __VA_ARGS__)
#define DirExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_DIRUTIL, x, s, __VA_ARGS__)
#define DirExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_DIRUTIL, x, e, s, __VA_ARGS__)
#define DirExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_DIRUTIL, x, s, __VA_ARGS__)
#define DirExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_DIRUTIL, p, x, e, s, __VA_ARGS__)
#define DirExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_DIRUTIL, p, x, s, __VA_ARGS__)
#define DirExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_DIRUTIL, p, x, e, s, __VA_ARGS__)
#define DirExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_DIRUTIL, p, x, s, __VA_ARGS__)
#define DirExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_DIRUTIL, e, x, s, __VA_ARGS__)
#define DirExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_DIRUTIL, g, x, s, __VA_ARGS__)
#define DirExitOnPathFailure(x, b, s, ...) ExitOnPathFailureSource(DUTIL_SOURCE_DIRUTIL, x, b, s, __VA_ARGS__)
#define DirExitWithPathLastError(x, s, ...) ExitWithPathLastErrorSource(DUTIL_SOURCE_DIRUTIL, x, s, __VA_ARGS__)


/*******************************************************************
 DirExists

*******************************************************************/
extern "C" BOOL DAPI DirExists(
    __in_z LPCWSTR wzPath,
    __out_opt DWORD *pdwAttributes
    )
{
    Assert(wzPath);

    BOOL fExists = FALSE;

    DWORD dwAttributes = ::GetFileAttributesW(wzPath);
    if (0xFFFFFFFF == dwAttributes) // TODO: figure out why "INVALID_FILE_ATTRIBUTES" can't be used here
    {
        ExitFunction();
    }

    if (dwAttributes & FILE_ATTRIBUTE_DIRECTORY)
    {
        if (pdwAttributes)
        {
            *pdwAttributes = dwAttributes;
        }

        fExists = TRUE;
    }

LExit:
    return fExists;
}


/*******************************************************************
 DirCreateTempPath

 *******************************************************************/
extern "C" HRESULT DAPI DirCreateTempPath(
    __in_z LPCWSTR wzPrefix,
    __out_opt LPWSTR* psczTempFile
    )
{
    return PathCreateTempFile(NULL, NULL, 0, wzPrefix, 0, psczTempFile, NULL);
}


/*******************************************************************
 DirEnsureExists

*******************************************************************/
extern "C" HRESULT DAPI DirEnsureExists(
    __in_z LPCWSTR wzPath,
    __in_opt LPSECURITY_ATTRIBUTES psa
    )
{
    HRESULT hr = S_OK;
    UINT er;

    // try to create this directory
    if (!::CreateDirectoryW(wzPath, psa))
    {
        // if the directory already exists, bail
        er = ::GetLastError();
        if (ERROR_ALREADY_EXISTS == er)
        {
            ExitFunction1(hr = S_OK);
        }
        else if (ERROR_PATH_NOT_FOUND != er && DirExists(wzPath, NULL)) // if the directory happens to exist (don't check if CreateDirectory said it doesn't), declare success.
        {
            ExitFunction1(hr = S_OK);
        }

        // get the parent path and try to create it
        LPWSTR pwzLastSlash = NULL;
        for (LPWSTR pwz = const_cast<LPWSTR>(wzPath); *pwz; ++pwz)
        {
            if (*pwz == L'\\')
            {
                pwzLastSlash = pwz;
            }
        }

        // if there is no parent directory fail
        DirExitOnNullDebugTrace(pwzLastSlash, hr, HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND), "cannot find parent path");

        *pwzLastSlash = L'\0'; // null terminate the parent path
        hr = DirEnsureExists(wzPath, psa);   // recurse!
        *pwzLastSlash = L'\\';  // put the slash back
        DirExitOnFailureDebugTrace(hr, "failed to create path: %ls", wzPath);

        // try to create the directory now that all parents are created
        if (!::CreateDirectoryW(wzPath, psa))
        {
            // if the directory already exists for some reason no error
            er = ::GetLastError();
            if (ERROR_ALREADY_EXISTS == er)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = HRESULT_FROM_WIN32(er);
            }
        }
        else
        {
            hr = S_OK;
        }
    }

LExit:
    return hr;
}


/*******************************************************************
 DirEnsureDelete - removes an entire directory structure

*******************************************************************/
extern "C" HRESULT DAPI DirEnsureDelete(
    __in_z LPCWSTR wzPath,
    __in BOOL fDeleteFiles,
    __in BOOL fRecurse
    )
{
    HRESULT hr = S_OK;
    DWORD dwDeleteFlags = 0;

    dwDeleteFlags |= fDeleteFiles ? DIR_DELETE_FILES : 0;
    dwDeleteFlags |= fRecurse ? DIR_DELETE_RECURSE : 0;

    hr = DirEnsureDeleteEx(wzPath, dwDeleteFlags);
    return hr;
}


/*******************************************************************
 DirEnsureDeleteEx - removes an entire directory structure

*******************************************************************/
extern "C" HRESULT DAPI DirEnsureDeleteEx(
    __in_z LPCWSTR wzPath,
    __in DWORD dwFlags
    )
{
    Assert(wzPath && *wzPath);

    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    DWORD dwAttrib = 0;
    HANDLE hFind = INVALID_HANDLE_VALUE;
    LPWSTR sczDelete = NULL;
    WIN32_FIND_DATAW wfd = { };

    BOOL fDeleteFiles = (DIR_DELETE_FILES == (dwFlags & DIR_DELETE_FILES));
    BOOL fRecurse = (DIR_DELETE_RECURSE == (dwFlags & DIR_DELETE_RECURSE));
    BOOL fScheduleDelete = (DIR_DELETE_SCHEDULE == (dwFlags & DIR_DELETE_SCHEDULE));
    WCHAR wzSafeFileName[MAX_PATH + 1] = { };
    LPWSTR sczTempDirectory = NULL;
    LPWSTR sczTempPath = NULL;

    if (-1 == (dwAttrib = ::GetFileAttributesW(wzPath)))
    {
        DirExitWithPathLastError(hr, "Failed to get attributes for path: %ls", wzPath);

        ExitFunction1(hr = E_PATHNOTFOUND);
    }

    if (dwAttrib & FILE_ATTRIBUTE_DIRECTORY)
    {
        if (dwAttrib & FILE_ATTRIBUTE_READONLY)
        {
            if (!::SetFileAttributesW(wzPath, FILE_ATTRIBUTE_NORMAL))
            {
                DirExitWithPathLastError(hr, "Failed to remove read-only attribute from path: %ls", wzPath);

                ExitFunction1(hr = E_PATHNOTFOUND);
            }
        }

        // If we're deleting files and/or child directories loop through the contents of the directory, but skip junctions.
        if ((fDeleteFiles || fRecurse) && (0 == (dwAttrib & FILE_ATTRIBUTE_REPARSE_POINT)))
        {
            if (fScheduleDelete)
            {
                hr = PathGetTempPath(&sczTempDirectory, NULL);
                DirExitOnFailure(hr, "Failed to get temp directory.");
            }

            // Delete everything in this directory.
            hr = PathConcat(wzPath, L"*.*", &sczDelete);
            DirExitOnFailure(hr, "Failed to concat wild cards to string: %ls", wzPath);

            hFind = ::FindFirstFileW(sczDelete, &wfd);
            if (INVALID_HANDLE_VALUE == hFind)
            {
                DirExitWithLastError(hr, "failed to get first file in directory: %ls", wzPath);
            }

            do
            {
                // Skip the dot directories.
                if (L'.' == wfd.cFileName[0] && (L'\0' == wfd.cFileName[1] || (L'.' == wfd.cFileName[1] && L'\0' == wfd.cFileName[2])))
                {
                    continue;
                }

                // For extra safety and to silence OACR.
                hr = ::StringCchCopyNExW(wzSafeFileName, countof(wzSafeFileName), wfd.cFileName, countof(wfd.cFileName), NULL, NULL, STRSAFE_FILL_BEHIND_NULL | STRSAFE_NULL_ON_FAILURE);
                DirExitOnFailure(hr, "Failed to ensure file name was null terminated.");

                hr = PathConcat(wzPath, wzSafeFileName, &sczDelete);
                DirExitOnFailure(hr, "Failed to concat filename '%ls' to directory: %ls", wzSafeFileName, wzPath);

                if (fRecurse && wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
                {
                    hr = PathBackslashTerminate(&sczDelete);
                    DirExitOnFailure(hr, "Failed to ensure path is backslash terminated: %ls", sczDelete);

                    hr = DirEnsureDeleteEx(sczDelete, dwFlags); // recursive call
                    if (FAILED(hr))
                    {
                        // if we failed to delete a subdirectory, keep trying to finish any remaining files
                        if (E_PATHNOTFOUND != hr)
                        {
                            ExitTraceSource(DUTIL_SOURCE_DIRUTIL, hr, "Failed to delete subdirectory; continuing: %ls", sczDelete);
                        }
                        hr = S_OK;
                    }
                }
                else if (fDeleteFiles)  // this is a file, just delete it
                {
                    if (wfd.dwFileAttributes & FILE_ATTRIBUTE_READONLY || wfd.dwFileAttributes & FILE_ATTRIBUTE_HIDDEN || wfd.dwFileAttributes & FILE_ATTRIBUTE_SYSTEM)
                    {
                        if (!::SetFileAttributesW(sczDelete, FILE_ATTRIBUTE_NORMAL))
                        {
                            DirExitWithPathLastError(hr, "Failed to remove attributes from file: %ls", sczDelete);
                            continue;
                        }
                    }

                    if (!::DeleteFileW(sczDelete))
                    {
                        if (fScheduleDelete)
                        {
                            hr = PathGetTempFileName(sczTempDirectory, L"DEL", 0, &sczTempPath);
                            DirExitOnFailure(hr, "Failed to get temp file to move to.");

                            // Try to move the file to the temp directory then schedule for delete,
                            // otherwise just schedule for delete.
                            if (::MoveFileExW(sczDelete, sczTempPath, MOVEFILE_REPLACE_EXISTING))
                            {
                                ::MoveFileExW(sczTempPath, NULL, MOVEFILE_DELAY_UNTIL_REBOOT);
                            }
                            else
                            {
                                ::MoveFileExW(sczDelete, NULL, MOVEFILE_DELAY_UNTIL_REBOOT);
                            }
                        }
                        else
                        {
                            DirExitWithPathLastError(hr, "Failed to delete file: %ls", sczDelete);
                        }
                    }
                }
            } while (::FindNextFileW(hFind, &wfd));

            er = ::GetLastError();
            if (ERROR_NO_MORE_FILES == er)
            {
                hr = S_OK;
            }
            else
            {
                DirExitWithLastError(hr, "Failed while looping through files in directory: %ls", wzPath);
            }
        }

        if (!::RemoveDirectoryW(wzPath))
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            if (HRESULT_FROM_WIN32(ERROR_SHARING_VIOLATION) == hr && fScheduleDelete)
            {
                if (::MoveFileExW(wzPath, NULL, MOVEFILE_DELAY_UNTIL_REBOOT))
                {
                    hr = S_OK;
                }
            }

            if (E_PATHNOTFOUND == hr || E_FILENOTFOUND == hr)
            {
                ExitFunction1(hr = E_PATHNOTFOUND);
            }
            else if (HRESULT_FROM_WIN32(ERROR_DIR_NOT_EMPTY) == hr && !fDeleteFiles && !fRecurse)
            {
                ExitFunction();
            }

            DirExitOnRootFailure(hr, "Failed to remove directory: %ls", wzPath);
        }
    }
    else
    {
        DirExitWithRootFailure(hr, E_UNEXPECTED, "Directory delete cannot delete file: %ls", wzPath);
    }

    Assert(S_OK == hr);

LExit:
    ReleaseFileFindHandle(hFind);
    ReleaseStr(sczDelete);
    ReleaseStr(sczTempDirectory);
    ReleaseStr(sczTempPath);

    return hr;
}


/*******************************************************************
DirDeleteEmptyDirectoriesToRoot - removes an empty directory and as many
                                  of its parents as possible.

 Returns: count of directories deleted.
*******************************************************************/
extern "C" DWORD DAPI DirDeleteEmptyDirectoriesToRoot(
    __in_z LPCWSTR wzPath,
    __in DWORD /*dwFlags*/
    )
{
    HRESULT hr = S_OK;
    DWORD cDeletedDirs = 0;
    LPWSTR sczPath = NULL;
    LPCWSTR wzPastRoot = NULL;
    SIZE_T cchRoot = 0;

    // Make sure the path is normalized and prefixed.
    hr = PathExpand(&sczPath, wzPath, PATH_EXPAND_FULLPATH);
    DirExitOnFailure(hr, "Failed to get full path for: %ls", wzPath);

    wzPastRoot = PathSkipPastRoot(sczPath, NULL, NULL, NULL);
    DirExitOnNull(wzPastRoot, hr, E_INVALIDARG, "Full path was not rooted: %ls", sczPath);

    cchRoot = wzPastRoot - sczPath;

    while (sczPath && sczPath[cchRoot] && ::RemoveDirectoryW(sczPath))
    {
        ++cDeletedDirs;

        hr = PathGetParentPath(sczPath, &sczPath, &cchRoot);
        DirExitOnFailure(hr, "Failed to get parent directory for path: %ls", sczPath);
    }

LExit:
    ReleaseStr(sczPath);

    return cDeletedDirs;
}


/*******************************************************************
 DirGetCurrent - gets the current directory.

*******************************************************************/
extern "C" HRESULT DAPI DirGetCurrent(
    __deref_out_z LPWSTR* psczCurrentDirectory,
    __out_opt SIZE_T* pcch
    )
{
    Assert(psczCurrentDirectory);

    HRESULT hr = S_OK;
    SIZE_T cchMax = 0;
    DWORD cch = 0;
    DWORD cchBuffer = 0;
    DWORD dwAttempts = 0;
    const DWORD dwMaxAttempts = 10;

    if (*psczCurrentDirectory)
    {
        hr = StrMaxLength(*psczCurrentDirectory, &cchMax);
        DirExitOnFailure(hr, "Failed to get max length of input buffer.");

        cchBuffer = (DWORD)min(DWORD_MAX, cchMax);
    }
    else
    {
        cchBuffer = MAX_PATH + 1;

        hr = StrAlloc(psczCurrentDirectory, cchBuffer);
        DirExitOnFailure(hr, "Failed to allocate space for current directory.");
    }

    for (; dwAttempts < dwMaxAttempts; ++dwAttempts)
    {
        cch = ::GetCurrentDirectoryW(cchBuffer, *psczCurrentDirectory);
        DirExitOnNullWithLastError(cch, hr, "Failed to get current directory.");

        if (cch < cchBuffer)
        {
            break;
        }

        hr = StrAlloc(psczCurrentDirectory, cch);
        DirExitOnFailure(hr, "Failed to reallocate space for current directory.");

        cchBuffer = cch;
    }

    if (dwMaxAttempts == dwAttempts)
    {
        DirExitWithRootFailure(hr, E_INSUFFICIENT_BUFFER, "GetCurrentDirectoryW results never converged.");
    }

    if (pcch)
    {
        *pcch = cch;
    }

LExit:
    return hr;
}


/*******************************************************************
 DirSetCurrent - sets the current directory.

*******************************************************************/
extern "C" HRESULT DAPI DirSetCurrent(
    __in_z LPCWSTR wzDirectory
    )
{
    HRESULT hr = S_OK;

    if (!::SetCurrentDirectoryW(wzDirectory))
    {
        DirExitWithLastError(hr, "Failed to set current directory to: %ls", wzDirectory);
    }

LExit:
    return hr;
}
