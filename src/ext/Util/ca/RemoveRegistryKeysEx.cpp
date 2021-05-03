// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsRemoveRegistryKeyExQuery =
    L"SELECT `Wix4RemoveRegistryKeyEx`, `Component_`, `Root`, `Key`, `InstallMode`, `Condition` FROM `Wix4RemoveRegistryKeyEx`";
enum eRemoveRegistryKeyExQuery { rrxqId = 1, rrxqComponent, rrxqRoot, rrxqKey, rrxqMode, rrxqCondition };

extern "C" UINT WINAPI WixRemoveRegistryKeysEx(
    __in MSIHANDLE hInstall
)
{
    //AssertSz(FALSE, "debug WixRemoveRegistryKeyEx");

    HRESULT hr = S_OK;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;
    LPWSTR sczId = NULL;
    LPWSTR sczComponent = NULL;
    LPWSTR sczCondition = NULL;
    LPWSTR sczKey = NULL;
    int iRoot = 0;
    int iMode = 0;
    MSIHANDLE hTable = NULL;
    MSIHANDLE hColumns = NULL;

    hr = WcaInitialize(hInstall, __FUNCTION__);
    ExitOnFailure(hr, "Failed to initialize " __FUNCTION__);

    // anything to do?
    if (S_OK != WcaTableExists(L"Wix4RemoveRegistryKeyEx"))
    {
        WcaLog(LOGMSG_STANDARD, "Wix4RemoveRegistryKeyEx table doesn't exist, so there are no registry keys to remove.");
        ExitFunction();
    }

    hr = WcaOpenExecuteView(vcsRemoveRegistryKeyExQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on Wix4RemoveRegistryKeyEx table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, rrxqId, &sczId);
        ExitOnFailure(hr, "Failed to get Wix4RemoveRegistryKeyEx identity.");

        hr = WcaGetRecordString(hRec, rrxqCondition, &sczCondition);
        ExitOnFailure(hr, "Failed to get Wix4RemoveRegistryKeyEx condition.");

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

        hr = WcaGetRecordString(hRec, rrxqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get Wix4RemoveRegistryKeyEx component.");

        hr = WcaGetRecordInteger(hRec, rrxqRoot, &iRoot);
        ExitOnFailure(hr, "Failed to get Wix4RemoveRegistryKeyEx root.");

        hr = WcaGetRecordString(hRec, rrxqKey, &sczKey);
        ExitOnFailure(hr, "Failed to get Wix4RemoveRegistryKeyEx key.");

        hr = WcaGetRecordInteger(hRec, rrxqMode, &iMode);
        ExitOnFailure(hr, "Failed to get Wix4RemoveRegistryKeyEx mode.");

        switch (iMode)
        {
        case 1: // remove on install
            WcaLog(LOGMSG_STANDARD, "Adding RemoveRegistry row: %ls/%d/%ls/-/%ls", sczId, iRoot, sczKey, sczComponent);
            hr = WcaAddTempRecord(&hTable, &hColumns, L"RemoveRegistry", NULL, 0, 5, sczId, iRoot, sczKey, L"-", sczComponent);
            ExitOnFailure(hr, "Failed to add RemoveRegistry row for remove-on-install Wix4RemoveRegistryKeyEx row: %ls:", sczId);
            break;
        case 2: // remove on uninstall
            WcaLog(LOGMSG_STANDARD, "Adding Registry row: %ls/%d/%ls/-/null/%ls", sczId, iRoot, sczKey, sczComponent);
            hr = WcaAddTempRecord(&hTable, &hColumns, L"Registry", NULL, 0, 6, sczId, iRoot, sczKey, L"-", NULL, sczComponent);
            ExitOnFailure(hr, "Failed to add Registry row for remove-on-uninstall Wix4RemoveRegistryKeyEx row: %ls:", sczId);
            break;
        }
    }

    // reaching the end of the list is actually a good thing, not an error
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occured while processing Wix4RemoveRegistryKeyEx table.");

LExit:
    if (hColumns)
    {
        ::MsiCloseHandle(hColumns);
    }

    if (hTable)
    {
        ::MsiCloseHandle(hTable);
    }

    ReleaseStr(sczKey);
    ReleaseStr(sczComponent);
    ReleaseStr(sczCondition);
    ReleaseStr(sczId);

    DWORD er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}
