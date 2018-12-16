// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


/********************************************************************
ConfigureSmb - CUSTOM ACTION ENTRY POINT for installing fileshare settings

********************************************************************/
extern "C" UINT __stdcall ConfigureSmbInstall(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    SCA_SMB* pssList = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ConfigureSmbInstall");
    ExitOnFailure(hr, "Failed to initialize");

    // check to see if necessary tables are specified
    if (WcaTableExists(L"FileShare") != S_OK)
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping SMB CustomAction, no FileShare table");
        ExitFunction1(hr = S_FALSE);
    }

    hr = ScaSmbRead(&pssList);
    ExitOnFailure(hr, "failed to read FileShare table");

    hr = ScaSmbInstall(pssList);
    ExitOnFailure(hr, "failed to install FileShares");

LExit:
    if (pssList)
        ScaSmbFreeList(pssList);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
ConfigureSmb - CUSTOM ACTION ENTRY POINT for installing fileshare settings

********************************************************************/
extern "C" UINT __stdcall ConfigureSmbUninstall(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    SCA_SMB* pssList = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ConfigureSmbUninstall");
    ExitOnFailure(hr, "Failed to initialize");

    // check to see if necessary tables are specified
    if (WcaTableExists(L"FileShare") != S_OK)
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping SMB CustomAction, no FileShare table");
        ExitFunction1(hr = S_FALSE);
    }

    hr = ScaSmbRead(&pssList);
    ExitOnFailure(hr, "failed to read FileShare table");

    hr = ScaSmbUninstall(pssList);
    ExitOnFailure(hr, "failed to uninstall FileShares");

LExit:
    if (pssList)
        ScaSmbFreeList(pssList);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
ConfigureUsers - CUSTOM ACTION ENTRY POINT for installing users

********************************************************************/
extern "C" UINT __stdcall ConfigureUsers(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(0, "Debug ConfigureUsers");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    BOOL fInitializedCom = FALSE;
    SCA_USER* psuList = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ConfigureUsers");
    ExitOnFailure(hr, "Failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = ScaUserRead(&psuList);
    ExitOnFailure(hr, "failed to read User table");

    hr = ScaUserExecute(psuList);
    ExitOnFailure(hr, "failed to add/remove User actions");

LExit:
    if (psuList)
    {
        ScaUserFreeList(psuList);
    }

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}