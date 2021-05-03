// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsRemoveFolderExQuery =
    L"SELECT `Wix4RemoveFolderEx`, `Component_`, `Property`, `InstallMode`, `WixRemoveFolderEx`.`Condition`, `Component`.`Attributes`"
    L"FROM `Wix4RemoveFolderEx``,`Component` "
    L"WHERE `Wix4RemoveFolderEx`.`Component_`=`Component`.`Component`";
enum eRemoveFolderExQuery { rfqId = 1, rfqComponent, rfqProperty, rfqMode, rfqCondition, rfqComponentAttributes };

static HRESULT RecursePath(
    __in_z LPCWSTR wzPath,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzComponent,
    __in_z LPCWSTR wzProperty,
    __in int iMode,
    __in BOOL fDisableWow64Redirection,
    __inout DWORD* pdwCounter,
    __inout MSIHANDLE* phTable,
    __inout MSIHANDLE* phColumns
    )
{
    HRESULT hr = S_OK;
    DWORD er;
    LPWSTR sczSearch = NULL;
    LPWSTR sczProperty = NULL;
    HANDLE hFind = INVALID_HANDLE_VALUE;
    WIN32_FIND_DATAW wfd;
    LPWSTR sczNext = NULL;

    if (fDisableWow64Redirection)
    {
        hr = WcaDisableWow64FSRedirection();
        ExitOnFailure(hr, "Custom action was told to act on a 64-bit component, but was unable to disable filesystem redirection through the Wow64 API.");
    }

    // First recurse down to all the child directories.
    hr = StrAllocFormatted(&sczSearch, L"%s*", wzPath);
    ExitOnFailure(hr, "Failed to allocate file search string in path: %S", wzPath);

    hFind = ::FindFirstFileW(sczSearch, &wfd);
    if (INVALID_HANDLE_VALUE == hFind)
    {
        er = ::GetLastError();
        if (ERROR_PATH_NOT_FOUND == er)
        {
            WcaLog(LOGMSG_STANDARD, "Search path not found: %ls; skipping", sczSearch);
            ExitFunction1(hr = S_FALSE);
        }
        else
        {
            hr = HRESULT_FROM_WIN32(er);
        }
        ExitOnFailure(hr, "Failed to find all files in path: %S", wzPath);
    }

    do
    {
        // Skip files and the dot directories.
        if (FILE_ATTRIBUTE_DIRECTORY != (wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) || L'.' == wfd.cFileName[0] && (L'\0' == wfd.cFileName[1] || (L'.' == wfd.cFileName[1] && L'\0' == wfd.cFileName[2])))
        {
            continue;
        }

        hr = StrAllocFormatted(&sczNext, L"%s%s\\", wzPath, wfd.cFileName);
        ExitOnFailure(hr, "Failed to concat filename '%S' to string: %S", wfd.cFileName, wzPath);

        // Don't re-disable redirection; if it was necessary, we've already done it.
        hr = RecursePath(sczNext, wzId, wzComponent, wzProperty, iMode, FALSE, pdwCounter, phTable, phColumns);
        ExitOnFailure(hr, "Failed to recurse path: %S", sczNext);
    } while (::FindNextFileW(hFind, &wfd));

    er = ::GetLastError();
    if (ERROR_NO_MORE_FILES == er)
    {
        hr = S_OK;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "Failed while looping through files in directory: %S", wzPath);
    }

    // Finally, set a property that points at our path.
    hr = StrAllocFormatted(&sczProperty, L"_%s_%u", wzProperty, *pdwCounter);
    ExitOnFailure(hr, "Failed to allocate Property for RemoveFile table with property: %S.", wzProperty);

    ++(*pdwCounter);

    hr = WcaSetProperty(sczProperty, wzPath);
    ExitOnFailure(hr, "Failed to set Property: %S with path: %S", sczProperty, wzPath);

    // Add the row to remove any files and another row to remove the folder.
    hr = WcaAddTempRecord(phTable, phColumns, L"RemoveFile", NULL, 1, 5, L"RfxFiles", wzComponent, L"*.*", sczProperty, iMode);
    ExitOnFailure(hr, "Failed to add row to remove all files for Wix4RemoveFolderEx row: %ls under path: %ls", wzId, wzPath);

    hr = WcaAddTempRecord(phTable, phColumns, L"RemoveFile", NULL, 1, 5, L"RfxFolder", wzComponent, NULL, sczProperty, iMode);
    ExitOnFailure(hr, "Failed to add row to remove folder for Wix4RemoveFolderEx row: %ls under path: %ls", wzId, wzPath);

LExit:
    if (INVALID_HANDLE_VALUE != hFind)
    {
        ::FindClose(hFind);
    }

    if (fDisableWow64Redirection)
    {
        WcaRevertWow64FSRedirection();
    }

    ReleaseStr(sczNext);
    ReleaseStr(sczProperty);
    ReleaseStr(sczSearch);
    return hr;
}

extern "C" UINT WINAPI WixRemoveFoldersEx(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug WixRemoveFoldersEx");

    HRESULT hr = S_OK;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;
    LPWSTR sczId = NULL;
    LPWSTR sczComponent = NULL;
    LPWSTR sczProperty = NULL;
    LPWSTR sczCondition = NULL;
    LPWSTR sczPath = NULL;
    LPWSTR sczExpandedPath = NULL;
    int iMode = 0;
    int iComponentAttributes;
    BOOL f64BitComponent = FALSE;
    DWORD dwCounter = 0;
    DWORD_PTR cchLen = 0;
    MSIHANDLE hTable = NULL;
    MSIHANDLE hColumns = NULL;

    hr = WcaInitialize(hInstall, "WixRemoveFoldersEx");
    ExitOnFailure(hr, "Failed to initialize WixRemoveFoldersEx.");

    WcaInitializeWow64();

    // anything to do?
    if (S_OK != WcaTableExists(L"Wix4RemoveFolderEx"))
    {
        WcaLog(LOGMSG_STANDARD, "Wix4RemoveFolderEx table doesn't exist, so there are no folders to remove.");
        ExitFunction();
    }

    // query and loop through all the remove folders exceptions
    hr = WcaOpenExecuteView(vcsRemoveFolderExQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on Wix4RemoveFolderEx table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, rfqId, &sczId);
        ExitOnFailure(hr, "Failed to get remove folder identity.");

        hr = WcaGetRecordString(hRec, rfqCondition, &sczCondition);
        ExitOnFailure(hr, "Failed to get remove folder condition.");

        if (sczCondition && *sczCondition)
        {
            MSICONDITION condition = ::MsiEvaluateConditionW(hInstall, sczCondition);
            if (MSICONDITION_TRUE == condition)
            {
                WcaLog(LOGMSG_STANDARD, "True condition for row %S: %S; processing.", sczId, sczCondition);
            }
            else
            {
                WcaLog(LOGMSG_STANDARD, "False or invalid condition for row %S: %S; skipping.", sczId, sczCondition);
                continue;
            }
        }

        hr = WcaGetRecordString(hRec, rfqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get remove folder component.");

        hr = WcaGetRecordString(hRec, rfqProperty, &sczProperty);
        ExitOnFailure(hr, "Failed to get remove folder property.");

        hr = WcaGetRecordInteger(hRec, rfqMode, &iMode);
        ExitOnFailure(hr, "Failed to get remove folder mode");

        hr = WcaGetProperty(sczProperty, &sczPath);
        ExitOnFailure(hr, "Failed to resolve remove folder property: %S for row: %S", sczProperty, sczId);

        hr = WcaGetRecordInteger(hRec, rfqComponentAttributes, &iComponentAttributes);
        ExitOnFailure(hr, "failed to get component attributes for row: %ls", sczId);

        f64BitComponent = iComponentAttributes & msidbComponentAttributes64bit;

        // fail early if the property isn't set as you probably don't want your installers trying to delete SystemFolder
        // StringCchLengthW succeeds only if the string is zero characters plus 1 for the terminating null
        hr = ::StringCchLengthW(sczPath, 1, reinterpret_cast<UINT_PTR*>(&cchLen));
        if (SUCCEEDED(hr))
        {
            ExitOnFailure(hr = E_INVALIDARG, "Missing folder property: %S for row: %S", sczProperty, sczId);
        }

        hr = PathExpand(&sczExpandedPath, sczPath, PATH_EXPAND_ENVIRONMENT);
        ExitOnFailure(hr, "Failed to expand path: %S for row: %S", sczPath, sczId);
        
        hr = PathBackslashTerminate(&sczExpandedPath);
        ExitOnFailure(hr, "Failed to backslash-terminate path: %S", sczExpandedPath);
    
        WcaLog(LOGMSG_STANDARD, "Recursing path: %S for row: %S.", sczExpandedPath, sczId);
        hr = RecursePath(sczExpandedPath, sczId, sczComponent, sczProperty, iMode, f64BitComponent, &dwCounter, &hTable, &hColumns);
        ExitOnFailure(hr, "Failed while navigating path: %S for row: %S", sczPath, sczId);
    }

    // reaching the end of the list is actually a good thing, not an error
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occured while processing Wix4RemoveFolderEx table");

LExit:
    WcaFinalizeWow64();

    if (hColumns)
    {
        ::MsiCloseHandle(hColumns);
    }

    if (hTable)
    {
        ::MsiCloseHandle(hTable);
    }

    ReleaseStr(sczExpandedPath);
    ReleaseStr(sczPath);
    ReleaseStr(sczProperty);
    ReleaseStr(sczComponent);
    ReleaseStr(sczCondition);
    ReleaseStr(sczId);

    DWORD er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}
