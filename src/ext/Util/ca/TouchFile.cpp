// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsTouchFileQuery = L"SELECT `TouchFile`, `Component_`, `Path`, `Attributes` FROM `Wix4TouchFile`";
enum TOUCH_FILE_QUERY { tfqId = 1, tfqComponent, tfqPath, tfqTouchFileAttributes };

enum TOUCH_FILE_ATTRIBUTE
{
    TOUCH_FILE_ATTRIBUTE_ON_INSTALL = 0x01,
    TOUCH_FILE_ATTRIBUTE_ON_REINSTALL = 0x02,
    TOUCH_FILE_ATTRIBUTE_ON_UNINSTALL = 0x04,
    TOUCH_FILE_ATTRIBUTE_64BIT = 0x10,
    TOUCH_FILE_ATTRIBUTE_VITAL = 0x20
};


static HRESULT SetExistingFileModifiedTime(
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzPath,
    __in BOOL f64Bit,
    __in FILETIME* pftModified
    )
{
    HRESULT hr = S_OK;
    BOOL fReenableFileSystemRedirection = FALSE;

    if (f64Bit)
    {
        hr = WcaDisableWow64FSRedirection();
        ExitOnFailure(hr, "Failed to disable 64-bit file system redirection to path: '%ls' for: %ls", wzPath, wzId);

        fReenableFileSystemRedirection = TRUE;
    }

    hr = FileSetTime(wzPath, NULL, NULL, pftModified);

LExit:
    if (fReenableFileSystemRedirection)
    {
        WcaRevertWow64FSRedirection();
    }

    return hr;
}


static HRESULT AddDataToCustomActionData(
    __deref_inout_z LPWSTR* psczCustomActionData,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzPath,
    __in int iTouchFileAttributes,
    __in FILETIME ftModified
    )
{
    HRESULT hr = S_OK;

    hr = WcaWriteStringToCaData(wzId, psczCustomActionData);
    ExitOnFailure(hr, "Failed to add touch file identity to custom action data.");

    hr = WcaWriteStringToCaData(wzPath, psczCustomActionData);
    ExitOnFailure(hr, "Failed to add touch file path to custom action data.");

    hr = WcaWriteIntegerToCaData(iTouchFileAttributes, psczCustomActionData);
    ExitOnFailure(hr, "Failed to add touch file attributes to custom action data.");

    hr = WcaWriteIntegerToCaData(ftModified.dwHighDateTime, psczCustomActionData);
    ExitOnFailure(hr, "Failed to add touch file high date/time to custom action data.");

    hr = WcaWriteIntegerToCaData(ftModified.dwLowDateTime, psczCustomActionData);
    ExitOnFailure(hr, "Failed to add touch file low date/time to custom action data.");

LExit:
    return hr;
}


static BOOL TryGetExistingFileModifiedTime(
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzPath,
    __in BOOL f64Bit,
    __inout FILETIME* pftModified
    )
{
    HRESULT hr = S_OK;
    BOOL fReenableFileSystemRedirection = FALSE;

    if (f64Bit)
    {
        hr = WcaDisableWow64FSRedirection();
        ExitOnFailure(hr, "Failed to disable 64-bit file system redirection to path: '%ls' for: %ls", wzPath, wzId);

        fReenableFileSystemRedirection = TRUE;
    }

    hr = FileGetTime(wzPath, NULL, NULL, pftModified);
    if (E_PATHNOTFOUND == hr || E_FILENOTFOUND == hr)
    {
        // If the file doesn't exist yet there is nothing to rollback (i.e. file will probably be removed during rollback), so
        // keep the error code but don't log anything.
    }
    else if (FAILED(hr))
    {
        WcaLog(LOGMSG_STANDARD, "Cannot access modified timestamp for file: '%ls' due to error: 0x%x. Continuing with out rollback for: %ls", wzPath, hr, wzId);
    }

LExit:
    if (fReenableFileSystemRedirection)
    {
        WcaRevertWow64FSRedirection();
    }

    return SUCCEEDED(hr);
}


static HRESULT ProcessTouchFileTable(
    __in BOOL fInstalling
    )
{
    HRESULT hr = S_OK;

    FILETIME ftModified = {};

    PMSIHANDLE hView;
    PMSIHANDLE hRec;

    LPWSTR sczId = NULL;
    LPWSTR sczComponent = NULL;
    int iTouchFileAttributes = 0;
    LPWSTR sczPath = NULL;

    FILETIME ftRollbackModified = {};
    LPWSTR sczRollbackData = NULL;
    LPWSTR sczExecuteData = NULL;

    if (S_OK != WcaTableExists(L"Wix4TouchFile"))
    {
        ExitFunction();
    }

    ::GetSystemTimeAsFileTime(&ftModified);

    hr = WcaOpenExecuteView(vcsTouchFileQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on Wix4TouchFile table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, tfqId, &sczId);
        ExitOnFailure(hr, "Failed to get touch file identity.");

        hr = WcaGetRecordString(hRec, tfqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get touch file component for: %ls", sczId);

        hr = WcaGetRecordInteger(hRec, tfqTouchFileAttributes, &iTouchFileAttributes);
        ExitOnFailure(hr, "Failed to get touch file attributes for: %ls", sczId);

        WCA_TODO todo = WcaGetComponentToDo(sczComponent);

        BOOL fOnInstall = fInstalling && WCA_TODO_INSTALL == todo && (iTouchFileAttributes & TOUCH_FILE_ATTRIBUTE_ON_INSTALL);
        BOOL fOnReinstall = fInstalling && WCA_TODO_REINSTALL == todo && (iTouchFileAttributes & TOUCH_FILE_ATTRIBUTE_ON_REINSTALL);
        BOOL fOnUninstall = !fInstalling && WCA_TODO_UNINSTALL == todo && (iTouchFileAttributes & TOUCH_FILE_ATTRIBUTE_ON_UNINSTALL);

        if (fOnInstall || fOnReinstall || fOnUninstall)
        {
            hr = WcaGetRecordFormattedString(hRec, tfqPath, &sczPath);
            ExitOnFailure(hr, "Failed to get touch file path for: %ls", sczId);

            if (TryGetExistingFileModifiedTime(sczId, sczPath, (iTouchFileAttributes & TOUCH_FILE_ATTRIBUTE_64BIT), &ftRollbackModified))
            {
                hr = AddDataToCustomActionData(&sczRollbackData, sczId, sczPath, iTouchFileAttributes, ftRollbackModified);
                ExitOnFailure(hr, "Failed to add to rollback custom action data for: %ls", sczId);
            }

            hr = AddDataToCustomActionData(&sczExecuteData, sczId, sczPath, iTouchFileAttributes, ftModified);
            ExitOnFailure(hr, "Failed to add to execute custom action data for: %ls", sczId);
        }
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occured while processing Wix4TouchFile table");

    if (sczRollbackData)
    {
        hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"RollbackTouchFile"), sczRollbackData, 0);
        ExitOnFailure(hr, "Failed to schedule RollbackTouchFile");
    }

    if (sczExecuteData)
    {
        hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"ExecuteTouchFile"), sczExecuteData, 0);
        ExitOnFailure(hr, "Failed to schedule ExecuteTouchFile");
    }

LExit:
    ReleaseStr(sczExecuteData);
    ReleaseStr(sczRollbackData);
    ReleaseStr(sczPath);
    ReleaseStr(sczComponent);
    ReleaseStr(sczId);

    return hr;
}


extern "C" UINT WINAPI WixTouchFileDuringInstall(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug WixTouchFileDuringInstall");

    HRESULT hr = S_OK;

    hr = WcaInitialize(hInstall, "WixTouchFileDuringInstall");
    ExitOnFailure(hr, "Failed to initialize WixTouchFileDuringInstall.");

    hr = ProcessTouchFileTable(TRUE);

LExit:
    DWORD er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


extern "C" UINT WINAPI WixTouchFileDuringUninstall(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug WixTouchFileDuringUninstall");

    HRESULT hr = S_OK;

    hr = WcaInitialize(hInstall, "WixTouchFileDuringUninstall");
    ExitOnFailure(hr, "Failed to initialize WixTouchFileDuringUninstall.");

    hr = ProcessTouchFileTable(FALSE);

LExit:
    DWORD er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


extern "C" UINT WINAPI WixExecuteTouchFile(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;

    LPWSTR sczData = NULL;
    LPWSTR pwz = NULL;

    LPWSTR sczId = NULL;
    LPWSTR sczPath = NULL;
    int iTouchFileAttributes = 0;
    FILETIME ftModified = {};

    hr = WcaInitialize(hInstall, "WixExecuteTouchFile");
    ExitOnFailure(hr, "Failed to initialize WixExecuteTouchFile.");

    hr = WcaGetProperty(L"CustomActionData", &sczData);
    ExitOnFailure(hr, "Failed to get custom action data for WixExecuteTouchFile.");

    pwz = sczData;

    while (pwz && *pwz)
    {
        hr = WcaReadStringFromCaData(&pwz, &sczId);
        ExitOnFailure(hr, "Failed to get touch file identity from custom action data.");

        hr = WcaReadStringFromCaData(&pwz, &sczPath);
        ExitOnFailure(hr, "Failed to get touch file path from custom action data for: %ls", sczId);

        hr = WcaReadIntegerFromCaData(&pwz, &iTouchFileAttributes);
        ExitOnFailure(hr, "Failed to get touch file attributes from custom action data for: %ls", sczId);

        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&ftModified.dwHighDateTime));
        ExitOnFailure(hr, "Failed to get touch file high date/time from custom action data for: %ls", sczId);

        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&ftModified.dwLowDateTime));
        ExitOnFailure(hr, "Failed to get touch file low date/time from custom action data for: %ls", sczId);

        hr = SetExistingFileModifiedTime(sczId, sczPath, (iTouchFileAttributes & TOUCH_FILE_ATTRIBUTE_64BIT), &ftModified);
        if (FAILED(hr))
        {
            if (iTouchFileAttributes & TOUCH_FILE_ATTRIBUTE_VITAL)
            {
                ExitOnFailure(hr, "Failed to touch file: '%ls' for: %ls", sczPath, sczId);
            }
            else
            {
                WcaLog(LOGMSG_STANDARD, "Could not touch non-vital file: '%ls' for: %ls with error: 0x%x. Continuing...", sczPath, sczId, hr);
                hr = S_OK;
            }
        }
    }

LExit:
    ReleaseStr(sczPath);
    ReleaseStr(sczId);
    ReleaseStr(sczData);

    DWORD er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}
