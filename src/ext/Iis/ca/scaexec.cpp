// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


/********************************************************************
 StartMetabaseTransaction - CUSTOM ACTION ENTRY POINT for backing up metabase

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall StartMetabaseTransaction(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug StartMetabaseTransaction here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    IMSAdminBase* piMetabase = NULL;
    LPWSTR pwzData = NULL;
    BOOL fInitializedCom = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "StartMetabaseTransaction");
    ExitOnFailure(hr, "failed to initialize StartMetabaseTransaction");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;
    hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
    if (hr == REGDB_E_CLASSNOTREG)
    {
        WcaLog(LOGMSG_STANDARD, "Failed to get IIMSAdminBase to backup - continuing");
        hr = S_OK;
    }
    else
    {
        MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");

        hr = WcaGetProperty(L"CustomActionData", &pwzData);
        ExitOnFailure(hr, "failed to get CustomActionData");

        // back up the metabase
        Assert(lstrlenW(pwzData) < MD_BACKUP_MAX_LEN);

        // MD_BACKUP_OVERWRITE = Overwrite if a backup of the same name and version exists in the backup location
        hr = piMetabase->Backup(pwzData, MD_BACKUP_NEXT_VERSION, MD_BACKUP_OVERWRITE | MD_BACKUP_FORCE_BACKUP | MD_BACKUP_SAVE_FIRST);
        if (MD_WARNING_SAVE_FAILED == hr)
        {
            WcaLog(LOGMSG_STANDARD, "Failed to save metabase before backing up - continuing");
            hr = S_OK;
        }
        MessageExitOnFailure(hr, msierrIISFailedStartTransaction, "failed to begin metabase transaction: '%ls'", pwzData);
    }
    hr = WcaProgressMessage(COST_IIS_TRANSACTIONS, FALSE);
LExit:
    ReleaseStr(pwzData);
    ReleaseObject(piMetabase);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 RollbackMetabaseTransaction - CUSTOM ACTION ENTRY POINT for unbacking up metabase

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall RollbackMetabaseTransaction(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug RollbackMetabaseTransaction here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    IMSAdminBase* piMetabase = NULL;
    LPWSTR pwzData = NULL;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "RollbackMetabaseTransaction");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;
    hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
    if (hr == REGDB_E_CLASSNOTREG)
    {
        WcaLog(LOGMSG_STANDARD, "Failed to get IIMSAdminBase to rollback - continuing");
        hr = S_OK;
        ExitFunction();
    }
    ExitOnFailure(hr, "failed to get IID_IIMSAdminBase object");


    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    hr = piMetabase->Restore(pwzData, MD_BACKUP_HIGHEST_VERSION, 0);
    ExitOnFailure(hr, "failed to rollback metabase transaction: '%ls'", pwzData);

    hr = piMetabase->DeleteBackup(pwzData, MD_BACKUP_HIGHEST_VERSION);
    ExitOnFailure(hr, "failed to cleanup metabase transaction '%ls', continuing", pwzData);

LExit:
    ReleaseStr(pwzData);
    ReleaseObject(piMetabase);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/********************************************************************
 CommitMetabaseTransaction - CUSTOM ACTION ENTRY POINT for unbacking up metabase

  Input:  deferred CustomActionData - BackupName
 * *****************************************************************/
extern "C" UINT __stdcall CommitMetabaseTransaction(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    IMSAdminBase* piMetabase = NULL;
    LPWSTR pwzData = NULL;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "CommitMetabaseTransaction");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;
    hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
    if (hr == REGDB_E_CLASSNOTREG)
    {
        WcaLog(LOGMSG_STANDARD, "Failed to get IIMSAdminBase to commit - continuing");
        hr = S_OK;
        ExitFunction();
    }
    ExitOnFailure(hr, "failed to get IID_IIMSAdminBase object");

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    hr = piMetabase->DeleteBackup(pwzData, MD_BACKUP_HIGHEST_VERSION);
    ExitOnFailure(hr, "failed to cleanup metabase transaction: '%ls'", pwzData);

LExit:
    ReleaseStr(pwzData);
    ReleaseObject(piMetabase);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/********************************************************************
 CreateMetabaseKey - Installs metabase keys

  Input:  deferred CustomActionData - Key
 * *****************************************************************/
static HRESULT CreateMetabaseKey(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
//AssertSz(FALSE, "debug CreateMetabaseKey here");
    HRESULT hr = S_OK;
    METADATA_HANDLE mhRoot = 0;
    LPWSTR pwzData = NULL;
    LPCWSTR pwzKey;

    int i;

    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzData);
    ExitOnFailure(hr, "failed to read key from custom action data");

    hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
    for (i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
    {
        ::Sleep(1000);
        WcaLog(LOGMSG_STANDARD, "failed to open root key, retrying %d time(s)...", i);
        hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
    }
    MessageExitOnFailure(hr, msierrIISFailedOpenKey, "failed to open metabase key: %ls", L"/LM");

    pwzKey = pwzData + 3;

    WcaLog(LOGMSG_VERBOSE, "Creating Metabase Key: %ls", pwzKey);

    hr = piMetabase->AddKey(mhRoot, pwzKey);
    if (HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS) == hr)
    {
        WcaLog(LOGMSG_VERBOSE, "Key `%ls` already existed, continuing.", pwzData);
        hr = S_OK;
    }
    MessageExitOnFailure(hr, msierrIISFailedCreateKey, "failed to create metabase key: %ls", pwzKey);

    hr = WcaProgressMessage(COST_IIS_CREATEKEY, FALSE);

LExit:
    if (mhRoot)
    {
        piMetabase->CloseKey(mhRoot);
    }

    return hr;
}


/********************************************************************
 WriteMetabaseValue -Installs metabase values

  Input:  deferred CustomActionData - Key\tIdentifier\tAttributes\tUserType\tDataType\tData
 * *****************************************************************/
static HRESULT WriteMetabaseValue(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
    //AssertSz(FALSE, "debug WriteMetabaseValue here");
    HRESULT hr = S_OK;

    METADATA_HANDLE mhKey = 0;

    LPWSTR pwzKey = NULL;
    LPWSTR pwzTemp = NULL;
    DWORD dwData = 0;
    DWORD dwTemp = 0;
    BOOL fFreeData = FALSE;
    METADATA_RECORD mr;
    ::ZeroMemory((LPVOID)&mr, sizeof(mr));
    METADATA_RECORD mrGet;
    ::ZeroMemory((LPVOID)&mrGet, sizeof(mrGet));

    int i;

    // get the key first
    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzKey);
    ExitOnFailure(hr, "failed to read key");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&mr.dwMDIdentifier));
    ExitOnFailure(hr, "failed to read identifier");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&mr.dwMDAttributes));
    ExitOnFailure(hr, "failed to read attributes");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&mr.dwMDUserType));
    ExitOnFailure(hr, "failed to read user type");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&mr.dwMDDataType));
    ExitOnFailure(hr, "failed to read data type");

    switch (mr.dwMDDataType) // data
    {
    case DWORD_METADATA:
        hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&dwData));
        mr.dwMDDataLen = sizeof(dwData);
        mr.pbMDData = reinterpret_cast<BYTE*>(&dwData);
        break;
    case STRING_METADATA:
        hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzTemp);
        mr.dwMDDataLen = (lstrlenW(pwzTemp) + 1) * sizeof(WCHAR);
        mr.pbMDData = reinterpret_cast<BYTE*>(pwzTemp);
        break;
    case MULTISZ_METADATA:
        {
        hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzTemp);
        mr.dwMDDataLen = (lstrlenW(pwzTemp) + 1) * sizeof(WCHAR);
        for (LPWSTR pwzT = pwzTemp; *pwzT; ++pwzT)
        {
            if (MAGIC_MULTISZ_CHAR == *pwzT)
            {
                *pwzT = L'\0';
            }
        }
        mr.pbMDData = reinterpret_cast<BYTE*>(pwzTemp);
        }
        break;
    case BINARY_METADATA:
        hr = WcaReadStreamFromCaData(ppwzCustomActionData, &mr.pbMDData, reinterpret_cast<DWORD_PTR *>(&mr.dwMDDataLen));
        fFreeData = TRUE;
        break;
    default:
        hr = E_UNEXPECTED;
        break;
    }
    ExitOnFailure(hr, "failed to parse CustomActionData into metabase record");

    WcaLog(LOGMSG_VERBOSE, "Writing Metabase Value Under Key: %ls ID: %d", pwzKey, mr.dwMDIdentifier);

    hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhKey);
    for (i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
    {
        ::Sleep(1000);
        WcaLog(LOGMSG_STANDARD, "failed to open '%ls' key, retrying %d time(s)...", pwzKey, i);
        hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhKey);
    }
    MessageExitOnFailure(hr, msierrIISFailedOpenKey, "failed to open metabase key: %ls", pwzKey);

    if (lstrlenW(pwzKey) < 3)
    {
        ExitOnFailure(hr = E_INVALIDARG, "Key didn't begin with \"/LM\" as expected - key value: %ls", pwzKey);
    }

    hr = piMetabase->SetData(mhKey, pwzKey + 3, &mr); // pwzKey + 3 to skip "/LM" that was used to open the key.

    // This means we're trying to write to a secure key without the secure flag set - let's try again with the secure flag set
    if (MD_ERROR_CANNOT_REMOVE_SECURE_ATTRIBUTE == hr)
    {
        mr.dwMDAttributes |= METADATA_SECURE;
        hr = piMetabase->SetData(mhKey, pwzKey + 3, &mr);
    }

    // If IIS6 returned failure, let's check if the correct value exists in the metabase before actually failing the CA
    if (FAILED(hr))
    {
        // Backup the original failure error, so we can log it below if necessary
        HRESULT hrOldValue = hr;

        mrGet.dwMDIdentifier = mr.dwMDIdentifier;
        mrGet.dwMDAttributes = METADATA_NO_ATTRIBUTES;
        mrGet.dwMDUserType = mr.dwMDUserType;
        mrGet.dwMDDataType = mr.dwMDDataType;
        mrGet.dwMDDataLen = mr.dwMDDataLen;
        mrGet.pbMDData = static_cast<BYTE*>(MemAlloc(mr.dwMDDataLen, TRUE));

        hr = piMetabase->GetData(mhKey, pwzKey + 3, &mrGet, &dwTemp);
        if (SUCCEEDED(hr))
        {
            if (mrGet.dwMDDataType == mr.dwMDDataType && mrGet.dwMDDataLen == mr.dwMDDataLen && 0 == memcmp(mrGet.pbMDData, mr.pbMDData, mr.dwMDDataLen))
            {
                WcaLog(LOGMSG_VERBOSE, "Received error while writing metabase value under key: %ls ID: %d with error 0x%x, but the correct value is in the metabase - continuing", pwzKey, mr.dwMDIdentifier, hrOldValue);
                hr = S_OK;
            }
            else
            {
                WcaLog(LOGMSG_VERBOSE, "Succeeded in checking metabase value after write value, but the values didn't match");
                hr = hrOldValue;
            }
        }
        else
        {
            WcaLog(LOGMSG_VERBOSE, "Failed to check value after metabase write failure (error code 0x%x)", hr);
            hr = hrOldValue;
        }
    }
    MessageExitOnFailure(hr, msierrIISFailedWriteData, "failed to write data to metabase key: %ls", pwzKey);

    hr = WcaProgressMessage(COST_IIS_WRITEVALUE, FALSE);

LExit:
    ReleaseStr(pwzTemp);
    ReleaseStr(pwzKey);

    if (mhKey)
    {
        piMetabase->CloseKey(mhKey);
    }

    if (fFreeData && mr.pbMDData)
    {
        MemFree(mr.pbMDData);
    }

    return hr;
}


/********************************************************************
 DeleteMetabaseValue -Installs metabase values

  Input:  deferred CustomActionData - Key\tIdentifier\tAttributes\tUserType\tDataType\tData
 * *****************************************************************/
static HRESULT DeleteMetabaseValue(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
    //AssertSz(FALSE, "debug DeleteMetabaseValue here");
    HRESULT hr = S_OK;

    METADATA_HANDLE mhKey = 0;

    LPWSTR pwzKey = NULL;
    DWORD dwIdentifier = 0;
    DWORD dwDataType = 0;

    int i;

    // get the key first
    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzKey);
    ExitOnFailure(hr, "failed to read key");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&dwIdentifier));
    ExitOnFailure(hr, "failed to read identifier");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&dwDataType));
    ExitOnFailure(hr, "failed to read data type");

    WcaLog(LOGMSG_VERBOSE, "Deleting Metabase Value Under Key: %ls ID: %d", pwzKey, dwIdentifier);

    hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhKey);
    for (i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
    {
        ::Sleep(1000);
        WcaLog(LOGMSG_STANDARD, "failed to open '%ls' key, retrying %d time(s)...", pwzKey, i);
        hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhKey);
    }
    MessageExitOnFailure(hr, msierrIISFailedOpenKey, "failed to open metabase key: %ls", pwzKey);

    if (lstrlenW(pwzKey) < 3)
    {
        ExitOnFailure(hr = E_INVALIDARG, "Key didn't begin with \"/LM\" as expected - key value: %ls", pwzKey);
    }

    hr = piMetabase->DeleteData(mhKey, pwzKey + 3, dwIdentifier, dwDataType); // pwzKey + 3 to skip "/LM" that was used to open the key.
    if (MD_ERROR_DATA_NOT_FOUND == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
    {
        hr = S_OK;
    }
    MessageExitOnFailure(hr, msierrIISFailedDeleteValue, "failed to delete value %d from metabase key: %ls", dwIdentifier, pwzKey);

    hr = WcaProgressMessage(COST_IIS_DELETEVALUE, FALSE);
LExit:
    ReleaseStr(pwzKey);

    if (mhKey)
        piMetabase->CloseKey(mhKey);

    return hr;
}


/********************************************************************
 DeleteAspApp - Deletes applications in IIS

  Input:  deferred CustomActionData - MetabaseRoot\tRecursive
 * *****************************************************************/
static HRESULT DeleteAspApp(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase, __in ICatalogCollection* pApplicationCollection, __in IWamAdmin* piWam)
{
    const int BUFFER_BYTES = 512;
    const BSTR bstrPropName = SysAllocString(L"Deleteable");

    HRESULT hr = S_OK;
    ICatalogObject* pApplication = NULL;

    LPWSTR pwzRoot = NULL;
    DWORD dwActualBufferSize = 0;
    long lSize = 0;
    long lIndex = 0;
    long lChanges = 0;

    VARIANT keyValue;
    ::VariantInit(&keyValue);
    VARIANT propValue;
    propValue.vt = VT_BOOL;
    propValue.boolVal = TRUE;

    METADATA_RECORD mr;
    // Get current set of web service extensions.
    ::ZeroMemory(&mr, sizeof(mr));
    mr.dwMDIdentifier = MD_APP_PACKAGE_ID;
    mr.dwMDAttributes = 0;
    mr.dwMDUserType  = ASP_MD_UT_APP;
    mr.dwMDDataType = STRING_METADATA;
    mr.pbMDData = new unsigned char[BUFFER_BYTES];
    mr.dwMDDataLen = BUFFER_BYTES;

    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzRoot); // MetabaseRoot
    ExitOnFailure(hr, "failed to get metabase root");

    hr = piMetabase->GetData(METADATA_MASTER_ROOT_HANDLE, pwzRoot, &mr, &dwActualBufferSize);
    if (HRESULT_FROM_WIN32(MD_ERROR_DATA_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
    {
        // This one doesn't have an independent app GUID associated with it - it may have been already partially deleted, or a low isolation app
        WcaLog(LOGMSG_VERBOSE, "No independent COM+ application found associated with %ls. It either doesn't exist, or was already removed - continuing", pwzRoot);
        ExitFunction1(hr = S_OK);
    }
    MessageExitOnFailure(hr, msierrIISFailedDeleteApp, "failed to get GUID for application at path: %ls", pwzRoot);

    WcaLog(LOGMSG_VERBOSE, "Deleting ASP App (used query: %ls) with GUID: %ls", pwzRoot, (LPWSTR)(mr.pbMDData));

    // Delete the application association from IIS's point of view before it's obliterated from the application collection
    hr = piWam->AppDelete(pwzRoot, FALSE);
    if (FAILED(hr))
    {
        // This isn't necessarily an error if we fail here, but let's log a failure if it happens
        WcaLog(LOGMSG_VERBOSE, "error 0x%x: failed to call IWamAdmin::AppDelete() while removing web application - continuing");
        hr = S_OK;
    }

    if (!pApplicationCollection)
    {
        WcaLog(LOGMSG_STANDARD, "Could not remove application with GUID %ls because the application collection could not be found", (LPWSTR)(mr.pbMDData));
        ExitFunction1(hr = S_OK);
    }

    hr = pApplicationCollection->Populate();
    MessageExitOnFailure(hr, msierrIISFailedDeleteApp, "failed to populate Application collection");

    hr = pApplicationCollection->get_Count(&lSize);
    MessageExitOnFailure(hr, msierrIISFailedDeleteApp, "failed to get size of Application collection");
    WcaLog(LOGMSG_TRACEONLY, "Found %u items in application collection", lSize);

    // No need to check this too early, as we may not even need this to have successfully allocated
    ExitOnNull(bstrPropName, hr, E_OUTOFMEMORY, "failed to allocate memory for \"Deleteable\" string");

    for (lIndex = 0; lIndex < lSize; ++lIndex)
    {
        hr = pApplicationCollection->get_Item(lIndex, reinterpret_cast<IDispatch**>(&pApplication));
        MessageExitOnFailure(hr, msierrIISFailedDeleteApp, "failed to get COM+ application while enumerating through COM+ applications");

        hr = pApplication->get_Key(&keyValue);
        MessageExitOnFailure(hr, msierrIISFailedDeleteApp, "failed to get key of COM+ application while enumerating through COM+ applications");

        WcaLog(LOGMSG_TRACEONLY, "While enumerating through COM+ applications, found an application with GUID: %ls", (LPWSTR)keyValue.bstrVal);

        if (VT_BSTR == keyValue.vt && 0 == lstrcmpW((LPWSTR)keyValue.bstrVal, (LPWSTR)(mr.pbMDData)))
        {
            hr = pApplication->put_Value(bstrPropName, propValue);
            if (FAILED(hr))
            {
                // This isn't necessarily a critical error unless we fail to actually delete it in the next step
                WcaLog(LOGMSG_VERBOSE, "error 0x%x: failed to ensure COM+ application with guid %ls is deletable - continuing", hr, (LPWSTR)(mr.pbMDData));
            }

            hr = pApplicationCollection->SaveChanges(&lChanges);
            if (FAILED(hr))
            {
                // This isn't necessarily a critical error unless we fail to actually delete it in the next step
                WcaLog(LOGMSG_VERBOSE, "error 0x%x: failed to save changes while ensuring COM+ application with guid %ls is deletable - continuing", hr, (LPWSTR)(mr.pbMDData));
            }

            hr = pApplicationCollection->Remove(lIndex);
            if (FAILED(hr))
            {
                WcaLog(LOGMSG_STANDARD, "error 0x%x: failed to remove COM+ application with guid %ls. The COM application will not be removed", hr, (LPWSTR)(mr.pbMDData));
            }
            else
            {
                hr = pApplicationCollection->SaveChanges(&lChanges);
                if (FAILED(hr))
                {
                    WcaLog(LOGMSG_STANDARD, "error 0x%x: failed to save changes when removing COM+ application with guid %ls. The COM application will not be removed - continuing", hr, (LPWSTR)(mr.pbMDData));
                }
                else
                {
                    WcaLog(LOGMSG_VERBOSE, "Found and removed application with GUID %ls", (LPWSTR)(mr.pbMDData));
                }
            }

            // We've found the right key and successfully deleted the app - let's exit the loop now
            lIndex = lSize;
        }
    }
    // If we didn't find it, it isn't an error, because the application we want to delete doesn't seem to exist!

    hr = WcaProgressMessage(COST_IIS_DELETEAPP, FALSE);
LExit:
    ReleaseBSTR(bstrPropName);

    ReleaseStr(pwzRoot);
    // Don't release pApplication, because it points to an object within the collection

    delete [] mr.pbMDData;

    return hr;
}


/********************************************************************
 CreateAspApp - Creates applications in IIS

  Input:  deferred CustomActionData - MetabaseRoot\tInProc
 * ****************************************************************/
static HRESULT CreateAspApp(__in LPWSTR* ppwzCustomActionData, __in IWamAdmin* piWam)
{
    HRESULT hr = S_OK;

    LPWSTR pwzRoot = NULL;
    BOOL fInProc;

    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzRoot); // MetabaseRoot
    ExitOnFailure(hr, "failed to get metabase root");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&fInProc)); // InProc
    ExitOnFailure(hr, "failed to get in proc flag");

    WcaLog(LOGMSG_VERBOSE, "Creating ASP App: %ls", pwzRoot);

    hr = piWam->AppCreate(pwzRoot, fInProc);
    MessageExitOnFailure(hr, msierrIISFailedCreateApp, "failed to create web application: %ls", pwzRoot);

    hr = WcaProgressMessage(COST_IIS_CREATEAPP, FALSE);
LExit:
    return hr;
}


/********************************************************************
 DeleteMetabaseKey - Deletes metabase keys

  Input:  deferred CustomActionData - Key
 ******************************************************************/
static HRESULT DeleteMetabaseKey(__in LPWSTR *ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
    HRESULT hr = S_OK;

    METADATA_HANDLE mhRoot = 0;

    LPWSTR pwzData = NULL;

    LPCWSTR pwzKey;
    int i;

    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzData);
    ExitOnFailure(hr, "failed to read key to be deleted");

    hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
    for (i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
    {
        ::Sleep(1000);
        WcaLog(LOGMSG_STANDARD, "failed to open root key, retrying %d time(s)...", i);
        hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
    }
    MessageExitOnFailure(hr, msierrIISFailedOpenKey, "failed to open metabase key: %ls", L"/LM");

    pwzKey = pwzData + 3;

    WcaLog(LOGMSG_VERBOSE, "Deleting Metabase Key: %ls", pwzKey);

    hr = piMetabase->DeleteKey(mhRoot, pwzKey);
    if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Key `%ls` did not exist, continuing.", pwzData);
        hr = S_OK;
    }
    MessageExitOnFailure(hr, msierrIISFailedDeleteKey, "failed to delete metabase key: %ls", pwzData);

    hr = WcaProgressMessage(COST_IIS_DELETEKEY, FALSE);
LExit:
    if (mhRoot)
    {
        piMetabase->CloseKey(mhRoot);
    }

    return hr;
}


/********************************************************************
 WriteMetabaseChanges - CUSTOM ACTION ENTRY POINT for IIS Metabase changes

 *******************************************************************/
extern "C" UINT __stdcall WriteMetabaseChanges(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug WriteMetabaseChanges here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    IMSAdminBase* piMetabase = NULL;
    IWamAdmin* piWam = NULL;
    ICOMAdminCatalog* pCatalog = NULL;
    ICatalogCollection* pApplicationCollection = NULL;
    WCA_CASCRIPT_HANDLE hWriteMetabaseScript = NULL;
    BSTR bstrApplications = SysAllocString(L"Applications");
    BOOL fInitializedCom = FALSE;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzScriptKey = NULL;
    METABASE_ACTION maAction = MBA_UNKNOWNACTION;

    hr = WcaInitialize(hInstall, "WriteMetabaseChanges");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    // Get the CaScript key
    hr = WcaReadStringFromCaData(&pwzData, &pwzScriptKey);
    ExitOnFailure(hr, "Failed to get CaScript key from custom action data");

    hr = WcaCaScriptOpen(WCA_ACTION_INSTALL, WCA_CASCRIPT_SCHEDULED, FALSE, pwzScriptKey, &hWriteMetabaseScript);
    ExitOnFailure(hr, "Failed to open CaScript file");

    // The rest of our existing custom action data string should be empty - go ahead and overwrite it
    ReleaseNullStr(pwzData);
    hr = WcaCaScriptReadAsCustomActionData(hWriteMetabaseScript, &pwzData);
    ExitOnFailure(hr, "Failed to read script into CustomAction data.");

    pwz = pwzData;

    while (S_OK == (hr = WcaReadIntegerFromCaData(&pwz, (int *)&maAction)))
    {
        switch (maAction)
        {
        case MBA_CREATEAPP:
            if (NULL == piWam)
            {
                hr = ::CoCreateInstance(CLSID_WamAdmin, NULL, CLSCTX_ALL, IID_IWamAdmin, reinterpret_cast<void**>(&piWam));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IWamAdmin object");
            }

            hr = CreateAspApp(&pwz, piWam);
            ExitOnFailure(hr, "failed to create ASP App");
            break;
        case MBA_DELETEAPP:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            if (NULL == pCatalog)
            {
                hr = CoCreateInstance(CLSID_COMAdminCatalog, NULL, CLSCTX_INPROC_SERVER, IID_IUnknown, (void**)&pCatalog);
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_ICOMAdmin object");

                hr = pCatalog->GetCollection(bstrApplications, reinterpret_cast<IDispatch**>(&pApplicationCollection));
                if (FAILED(hr))
                {
                    hr = S_OK;
                    WcaLog(LOGMSG_STANDARD, "error 0x%x: failed to get ApplicationCollection object for list of COM+ applications - COM+ applications will not be able to be uninstalled - continuing", hr);
                }
            }

            if (NULL == piWam)
            {
                hr = ::CoCreateInstance(CLSID_WamAdmin, NULL, CLSCTX_ALL, IID_IWamAdmin, reinterpret_cast<void**>(&piWam));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IWamAdmin object");
            }

            hr = DeleteAspApp(&pwz, piMetabase, pApplicationCollection, piWam);
            ExitOnFailure(hr, "failed to delete ASP App");
            break;
        case MBA_CREATEKEY:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            hr = CreateMetabaseKey(&pwz, piMetabase);
            ExitOnFailure(hr, "failed to create metabase key");
            break;
        case MBA_DELETEKEY:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            hr = DeleteMetabaseKey(&pwz, piMetabase);
            ExitOnFailure(hr, "failed to delete metabase key");
            break;
        case MBA_WRITEVALUE:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            hr = WriteMetabaseValue(&pwz, piMetabase);
            ExitOnFailure(hr, "failed to write metabase value");
            break;
        case MBA_DELETEVALUE:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            hr = DeleteMetabaseValue(&pwz, piMetabase);
            ExitOnFailure(hr, "failed to delete metabase value");
            break;
        default:
            ExitOnFailure(hr = E_UNEXPECTED, "Unexpected metabase action specified: %d", maAction);
            break;
        }
    }
    if (E_NOMOREITEMS == hr) // If there are no more items, all is well
    {
        if (NULL != piMetabase)
        {
            hr = piMetabase->SaveData();
            for (int i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
            {
                ::Sleep(1000);
                WcaLog(LOGMSG_VERBOSE, "Failed to force save of metabase data, retrying %d time(s)...", i);
                hr = piMetabase->SaveData();
            }
            if (FAILED(hr))
            {
                WcaLog(LOGMSG_VERBOSE, "Failed to force save of metabase data: 0x%x - continuing", hr);
            }
            hr = S_OK;
        }
        else
        {
            hr = S_OK;
        }
    }

LExit:
    WcaCaScriptClose(hWriteMetabaseScript, WCA_CASCRIPT_CLOSE_DELETE);

    ReleaseBSTR(bstrApplications);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzData);
    ReleaseObject(piMetabase);
    ReleaseObject(piWam);
    ReleaseObject(pCatalog);
    ReleaseObject(pApplicationCollection);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}
/********************************************************************
 WriteIIS7ConfigChanges - CUSTOM ACTION ENTRY POINT for IIS7 config changes

 *******************************************************************/
extern "C" UINT __stdcall WriteIIS7ConfigChanges(MSIHANDLE hInstall)
{
    //AssertSz(FALSE, "debug WriteIIS7ConfigChanges here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzData = NULL;
    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzHashString = NULL;
    BYTE rgbActualHash[SHA1_HASH_LEN] = { };
    DWORD dwHashedBytes = SHA1_HASH_LEN;

    WCA_CASCRIPT_HANDLE hWriteIis7Script = NULL;

    hr = WcaInitialize(hInstall, "WriteIIS7ConfigChanges");
    ExitOnFailure(hr, "Failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzScriptKey);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    WcaLog(LOGMSG_TRACEONLY, "Script WriteIIS7ConfigChanges: %ls", pwzScriptKey);

    hr = WcaCaScriptOpen(WCA_ACTION_INSTALL, WCA_CASCRIPT_SCHEDULED, FALSE, pwzScriptKey, &hWriteIis7Script);
    ExitOnFailure(hr, "Failed to open CaScript file");

    hr = WcaCaScriptReadAsCustomActionData(hWriteIis7Script, &pwzData);
    ExitOnFailure(hr, "Failed to read script into CustomAction data.");

    hr = CrypHashBuffer((BYTE*)pwzData, sizeof(pwzData) * sizeof(WCHAR), PROV_RSA_AES, CALG_SHA1, rgbActualHash, dwHashedBytes);
    ExitOnFailure(hr, "Failed to calculate hash of CustomAction data.");

    hr = StrAlloc(&pwzHashString, ((dwHashedBytes * 2) + 1));
    ExitOnFailure(hr, "Failed to allocate string for script hash");

    hr = StrHexEncode(rgbActualHash, dwHashedBytes, pwzHashString, ((dwHashedBytes * 2) + 1));
    ExitOnFailure(hr, "Failed to convert hash bytes to string.");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData WriteIIS7ConfigChanges: %ls", pwzData);
    WcaLog(LOGMSG_VERBOSE,  "Custom action data hash: %ls", pwzHashString);
    WcaLog(LOGMSG_VERBOSE, "CustomActionData WriteIIS7ConfigChanges length: %d", wcslen(pwzData));

    hr = IIS7ConfigChanges(hInstall, pwzData);
    ExitOnFailure(hr, "WriteIIS7ConfigChanges Failed.");

LExit:
    WcaCaScriptClose(hWriteIis7Script, WCA_CASCRIPT_CLOSE_DELETE);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzData);
    ReleaseStr(pwzHashString);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}


/********************************************************************
 CommitIIS7ConfigTransaction - CUSTOM ACTION ENTRY POINT for unbacking up config

  Input:  deferred CustomActionData - BackupName
 * *****************************************************************/
extern "C" UINT __stdcall CommitIIS7ConfigTransaction(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzData = NULL;
    WCHAR wzConfigCopy[MAX_PATH];
    DWORD dwSize = 0;

    BOOL fIsWow64Process = FALSE;
    BOOL fIsFSRedirectDisabled = FALSE;

    hr = WcaInitialize(hInstall, "CommitIIS7ConfigTransaction");
    ExitOnFailure(hr, "failed to initialize IIS7 commit transaction");

    WcaInitializeWow64();
    fIsWow64Process = WcaIsWow64Process();
    if (fIsWow64Process)
    {
        hr = WcaDisableWow64FSRedirection();
        if(FAILED(hr))
        {
            //eat this error
            hr = S_OK;
        }
        else
        {
            fIsFSRedirectDisabled = TRUE;
        }
    }

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    // Config AdminMgr changes already committed, just
    // delete backup config file.

    dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\applicationHost.config",
                                      wzConfigCopy,
                                      MAX_PATH
                                      );
    if ( dwSize == 0 )
    {
        ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
    }

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, L".");
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of .");

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, pwzData);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of extension");

    if (!::DeleteFileW(wzConfigCopy))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr ||
            HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
        {
            WcaLog(LOGMSG_STANDARD, "Failed to delete backup applicationHost, not found - continuing");
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "failed to delete config backup");
        }
    }

LExit:
    ReleaseStr(pwzData);

    // Make sure we revert FS Redirection if necessary before exiting
    if (fIsFSRedirectDisabled)
    {
        fIsFSRedirectDisabled = FALSE;
        WcaRevertWow64FSRedirection();
    }
    WcaFinalizeWow64();


    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}
/********************************************************************
 StartIIS7Config Transaction - CUSTOM ACTION ENTRY POINT for backing up config

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall StartIIS7ConfigTransaction(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzData = NULL;
    WCHAR wzConfigSource[MAX_PATH];
    WCHAR wzConfigCopy[MAX_PATH];
    DWORD dwSize = 0;


    BOOL fIsWow64Process = FALSE;
    BOOL fIsFSRedirectDisabled = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "StartIIS7ConfigTransaction");
    ExitOnFailure(hr, "failed to initialize StartIIS7ConfigTransaction");

    WcaInitializeWow64();
    fIsWow64Process = WcaIsWow64Process();
    if (fIsWow64Process)
    {
        hr = WcaDisableWow64FSRedirection();
        if(FAILED(hr))
        {
            //eat this error
            hr = S_OK;
        }
        else
        {
            fIsFSRedirectDisabled = TRUE;
        }
    }

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");


    dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\applicationHost.config",
                                      wzConfigSource,
                                      MAX_PATH
                                      );
    if ( dwSize == 0 )
    {
        ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
    }
    hr = ::StringCchCopyW(wzConfigCopy, MAX_PATH, wzConfigSource);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCopyW");

    //add ca action as extension

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, L".");
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of .");

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, pwzData);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of extension");

    if ( !::CopyFileW(wzConfigSource, wzConfigCopy, FALSE) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr ||
            HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
        {
            // IIS may not be installed on the machine, we'll fail later if we try to install anything
            WcaLog(LOGMSG_STANDARD, "Failed to back up applicationHost, not found - continuing");
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to copy config backup %ls -> %ls", wzConfigSource, wzConfigCopy);
        }
    }


    hr = WcaProgressMessage(COST_IIS_TRANSACTIONS, FALSE);


LExit:

    ReleaseStr(pwzData);

    // Make sure we revert FS Redirection if necessary before exiting
    if (fIsFSRedirectDisabled)
    {
        fIsFSRedirectDisabled = FALSE;
        WcaRevertWow64FSRedirection();
    }
    WcaFinalizeWow64();

    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 RollbackIIS7ConfigTransaction - CUSTOM ACTION ENTRY POINT for unbacking up config

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall RollbackIIS7ConfigTransaction(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzData = NULL;
    WCHAR wzConfigSource[MAX_PATH];
    WCHAR wzConfigCopy[MAX_PATH];
    DWORD dwSize = 0;

    BOOL fIsWow64Process = FALSE;
    BOOL fIsFSRedirectDisabled = FALSE;

    hr = WcaInitialize(hInstall, "RollbackIIS7ConfigTransaction");
    ExitOnFailure(hr, "failed to initialize");

    WcaInitializeWow64();
    fIsWow64Process = WcaIsWow64Process();
    if (fIsWow64Process)
    {
        hr = WcaDisableWow64FSRedirection();
        if(FAILED(hr))
        {
            //eat this error
            hr = S_OK;
        }
        else
        {
            fIsFSRedirectDisabled = TRUE;
        }
    }

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\applicationHost.config",
                                      wzConfigSource,
                                      MAX_PATH
                                      );
    if ( dwSize == 0 )
    {
        ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
    }
    hr = ::StringCchCopyW(wzConfigCopy, MAX_PATH, wzConfigSource);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCopyW");

    //add ca action as extension

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, L".");
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of .");

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, pwzData);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of extension");

    //copy is reverse of start transaction
    if (!::CopyFileW(wzConfigCopy, wzConfigSource, FALSE))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr ||
            HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
        {
            WcaLog(LOGMSG_STANDARD, "Failed to restore applicationHost, not found - continuing");
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "failed to restore config backup");
        }
    }

    if (!::DeleteFileW(wzConfigCopy))
    {
        ExitWithLastError(hr, "failed to delete config backup");
    }

    hr = WcaProgressMessage(COST_IIS_TRANSACTIONS, FALSE);

LExit:
    ReleaseStr(pwzData);

    // Make sure we revert FS Redirection if necessary before exiting
    if (fIsFSRedirectDisabled)
    {
        fIsFSRedirectDisabled = FALSE;
        WcaRevertWow64FSRedirection();
    }
    WcaFinalizeWow64();

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}
