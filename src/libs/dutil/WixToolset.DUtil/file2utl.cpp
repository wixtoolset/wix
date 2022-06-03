// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define FileExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_FILEUTIL, p, x, e, s, __VA_ARGS__)
#define FileExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_FILEUTIL, p, x, s, __VA_ARGS__)
#define FileExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_FILEUTIL, p, x, e, s, __VA_ARGS__)
#define FileExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_FILEUTIL, p, x, s, __VA_ARGS__)
#define FileExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_FILEUTIL, e, x, s, __VA_ARGS__)
#define FileExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_FILEUTIL, g, x, s, __VA_ARGS__)

// constants

const LPCWSTR REGISTRY_PENDING_FILE_RENAME_KEY = L"SYSTEM\\CurrentControlSet\\Control\\Session Manager";
const LPCWSTR REGISTRY_PENDING_FILE_RENAME_VALUE = L"PendingFileRenameOperations";


/*******************************************************************
 FileExistsAfterRestart - checks that a file exists and will continue
                          to exist after restart.

********************************************************************/
extern "C" BOOL DAPI FileExistsAfterRestart(
    __in_z LPCWSTR wzPath,
    __out_opt DWORD *pdwAttributes
    )
{
    HRESULT hr = S_OK;
    BOOL fExists = FALSE;
    HKEY hkPendingFileRename = NULL;
    LPWSTR* rgsczRenames = NULL;
    DWORD cRenames = 0;
    BOOL fPathEqual = FALSE;

    fExists = FileExistsEx(wzPath, pdwAttributes);
    if (fExists)
    {
        hr = RegOpen(HKEY_LOCAL_MACHINE, REGISTRY_PENDING_FILE_RENAME_KEY, KEY_QUERY_VALUE, &hkPendingFileRename);
        if (E_FILENOTFOUND == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        FileExitOnFailure(hr, "Failed to open pending file rename registry key.");

        hr = RegReadStringArray(hkPendingFileRename, REGISTRY_PENDING_FILE_RENAME_VALUE, &rgsczRenames, &cRenames);
        if (E_FILENOTFOUND == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        FileExitOnFailure(hr, "Failed to read pending file renames.");

        // The pending file renames array is pairs of source and target paths. We only care
        // about checking the source paths so skip the target paths (i += 2).
        for (DWORD i = 0; i < cRenames; i += 2)
        {
            LPWSTR wzRename = rgsczRenames[i];
            if (wzRename && *wzRename)
            {
                hr = PathCompareCanonicalized(wzPath, wzRename, &fPathEqual);
                FileExitOnFailure(hr, "Failed to compare path from pending file rename to check path.");

                if (fPathEqual)
                {
                    fExists = FALSE;
                    break;
                }
            }
        }
    }

LExit:
    ReleaseStrArray(rgsczRenames, cRenames);
    ReleaseRegKey(hkPendingFileRename);

    return fExists;
}


/*******************************************************************
 FileRemoveFromPendingRename - removes the file path from the pending
                               file rename list.

********************************************************************/
extern "C" HRESULT DAPI FileRemoveFromPendingRename(
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    HKEY hkPendingFileRename = NULL;
    LPWSTR* rgsczRenames = NULL;
    DWORD cRenames = 0;
    BOOL fPathEqual = FALSE;
    BOOL fRemoved = FALSE;
    DWORD cNewRenames = 0;

    hr = RegOpen(HKEY_LOCAL_MACHINE, REGISTRY_PENDING_FILE_RENAME_KEY, KEY_QUERY_VALUE | KEY_SET_VALUE, &hkPendingFileRename);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    FileExitOnFailure(hr, "Failed to open pending file rename registry key.");

    hr = RegReadStringArray(hkPendingFileRename, REGISTRY_PENDING_FILE_RENAME_VALUE, &rgsczRenames, &cRenames);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    FileExitOnFailure(hr, "Failed to read pending file renames.");

    // The pending file renames array is pairs of source and target paths. We only care
    // about checking the source paths so skip the target paths (i += 2).
    for (DWORD i = 0; i < cRenames; i += 2)
    {
        LPWSTR wzRename = rgsczRenames[i];
        if (wzRename && *wzRename)
        {
            hr = PathCompareCanonicalized(wzPath, wzRename, &fPathEqual);
            FileExitOnFailure(hr, "Failed to compare path from pending file rename to check path.");

            // If we find our path in the list, null out the source and target slot and
            // we'll compact the array next.
            if (fPathEqual)
            {
                ReleaseNullStr(rgsczRenames[i]);
                ReleaseNullStr(rgsczRenames[i + 1]);
                fRemoved = TRUE;
            }
        }
    }

    if (fRemoved)
    {
        // Compact the array by removing any nulls.
        for (DWORD i = 0; i < cRenames; ++i)
        {
            LPWSTR wzRename = rgsczRenames[i];
            if (wzRename)
            {
                rgsczRenames[cNewRenames] = wzRename;
                ++cNewRenames;
            }
        }

        cRenames = cNewRenames; // ignore the pointers on the end of the array since an early index points to them already.

        // Write the new array back to the pending file rename key.
        hr = RegWriteStringArray(hkPendingFileRename, REGISTRY_PENDING_FILE_RENAME_VALUE, rgsczRenames, cRenames);
        FileExitOnFailure(hr, "Failed to update pending file renames.");
    }

LExit:
    ReleaseStrArray(rgsczRenames, cRenames);
    ReleaseRegKey(hkPendingFileRename);

    return hr;
}
