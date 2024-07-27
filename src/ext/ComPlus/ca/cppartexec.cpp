// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// private structs

struct CPI_PARTITION_ATTRIBUTES
{
    int iActionType;
    int iActionCost;
    LPWSTR pwzKey;
    LPWSTR pwzID;
    LPWSTR pwzName;
    CPI_PROPERTY* pPropList;
};

struct CPI_PARTITION_USER_ATTRIBUTES
{
    int iActionType;
    int iActionCost;
    LPWSTR pwzKey;
    LPWSTR pwzAccount;
    LPWSTR pwzPartID;
};


// prototypes for private helper functions

static HRESULT ReadPartitionAttributes(
    LPWSTR* ppwzData,
    CPI_PARTITION_ATTRIBUTES* pAttrs
    );
static void FreePartitionAttributes(
    CPI_PARTITION_ATTRIBUTES* pAttrs
    );
static HRESULT CpiEnsurePartitionsEnabled();
static HRESULT CreatePartition(
    CPI_PARTITION_ATTRIBUTES* pAttrs
    );
static HRESULT RemovePartition(
    CPI_PARTITION_ATTRIBUTES* pAttrs
    );
static HRESULT ReadPartitionUserAttributes(
    LPWSTR* ppwzData,
    CPI_PARTITION_USER_ATTRIBUTES* pAttrs
    );
static void FreePartitionUserAttributes(
    CPI_PARTITION_USER_ATTRIBUTES* pAttrs
    );
static HRESULT CreatePartitionUser(
    CPI_PARTITION_USER_ATTRIBUTES* pAttrs
    );
static HRESULT RemovePartitionUser(
    CPI_PARTITION_USER_ATTRIBUTES* pAttrs
    );


// function definitions

HRESULT CpiConfigurePartitions(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    )
{
    HRESULT hr = S_OK;

    CPI_PARTITION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, FALSE);
    ExitOnFailure(hr, "Failed to send action start message");

    // get partition count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    // write count to rollback file
    hr = CpiWriteIntegerToRollbackFile(hRollbackFile, iCnt);
    ExitOnFailure(hr, "Failed to write count to rollback file");

    for (int i = 0; i < iCnt; i++)
    {
        // read partition attributes from CustomActionData
        hr = ReadPartitionAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // progress message
        hr = CpiActionDataMessage(1, attrs.pwzName);
        ExitOnFailure(hr, "Failed to send progress messages");

        if (S_FALSE == hr)
            ExitFunction();

        // write key to rollback file
        hr = CpiWriteKeyToRollbackFile(hRollbackFile, attrs.pwzKey);
        ExitOnFailure(hr, "Failed to write key to rollback file");

        // action
        switch (attrs.iActionType)
        {
        case atCreate:
            hr = CreatePartition(&attrs);
            ExitOnFailure(hr, "Failed to create partition, key: %S", attrs.pwzKey);
            break;
        case atRemove:
            hr = RemovePartition(&attrs);
            ExitOnFailure(hr, "Failed to remove partition, key: %S", attrs.pwzKey);
            break;
        }

        // write completion status to rollback file
        hr = CpiWriteIntegerToRollbackFile(hRollbackFile, 1);
        ExitOnFailure(hr, "Failed to write completion status to rollback file");

        // progress
        hr = WcaProgressMessage(attrs.iActionCost, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreePartitionAttributes(&attrs);

    return hr;
}

HRESULT CpiRollbackConfigurePartitions(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    )
{
    HRESULT hr = S_OK;

    int iRollbackStatus;

    CPI_PARTITION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, NULL == pRollbackDataList);
    ExitOnFailure(hr, "Failed to send action start message");

    // get count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read partition attributes from CustomActionData
        hr = ReadPartitionAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // rollback status
        hr = CpiFindRollbackStatus(pRollbackDataList, attrs.pwzKey, &iRollbackStatus);

        if (S_FALSE == hr)
            continue; // not found, nothing to rollback

        // progress message
        hr = CpiActionDataMessage(1, attrs.pwzName);
        ExitOnFailure(hr, "Failed to send progress messages");

        if (S_FALSE == hr)
            ExitFunction();

        // action
        switch (attrs.iActionType)
        {
        case atCreate:
            hr = CreatePartition(&attrs);
            if (FAILED(hr))
                WcaLog(LOGMSG_STANDARD, "Failed to create partition, hr: 0x%x, key: %S", hr, attrs.pwzKey);
            break;
        case atRemove:
            hr = RemovePartition(&attrs);
            if (FAILED(hr))
                WcaLog(LOGMSG_STANDARD, "Failed to remove partition, hr: 0x%x, key: %S", hr, attrs.pwzKey);
            break;
        }

        // check rollback status
        if (0 == iRollbackStatus)
            continue; // operation did not complete, skip progress

        // progress
        hr = WcaProgressMessage(attrs.iActionCost, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreePartitionAttributes(&attrs);

    return hr;
}

HRESULT CpiConfigurePartitionUsers(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    )
{
    HRESULT hr = S_OK;

    CPI_PARTITION_USER_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, FALSE);
    ExitOnFailure(hr, "Failed to send action start message");

    // get partition count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    // write count to rollback file
    hr = CpiWriteIntegerToRollbackFile(hRollbackFile, iCnt);
    ExitOnFailure(hr, "Failed to write count to rollback file");

    for (int i = 0; i < iCnt; i++)
    {
        // read partition attributes from CustomActionData
        hr = ReadPartitionUserAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // progress message
        hr = CpiActionDataMessage(1, attrs.pwzAccount);
        ExitOnFailure(hr, "Failed to send progress messages");

        if (S_FALSE == hr)
            ExitFunction();

        // write key to rollback file
        hr = CpiWriteKeyToRollbackFile(hRollbackFile, attrs.pwzKey);
        ExitOnFailure(hr, "Failed to write key to rollback file");

        // action
        switch (attrs.iActionType)
        {
        case atCreate:
            hr = CreatePartitionUser(&attrs);
            ExitOnFailure(hr, "Failed to create partition user, key: %S", attrs.pwzKey);
            break;
        case atRemove:
            hr = RemovePartitionUser(&attrs);
            ExitOnFailure(hr, "Failed to remove partition user, key: %S", attrs.pwzKey);
            break;
        }

        // write completion status to rollback file
        hr = CpiWriteIntegerToRollbackFile(hRollbackFile, 1);
        ExitOnFailure(hr, "Failed to write completion status to rollback file");

        // progress
        hr = WcaProgressMessage(attrs.iActionCost, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreePartitionUserAttributes(&attrs);

    return hr;
}

HRESULT CpiRollbackConfigurePartitionUsers(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    )
{
    HRESULT hr = S_OK;

    int iRollbackStatus;

    CPI_PARTITION_USER_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, NULL == pRollbackDataList);
    ExitOnFailure(hr, "Failed to send action start message");

    // get count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read partition attributes from CustomActionData
        hr = ReadPartitionUserAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // rollback status
        hr = CpiFindRollbackStatus(pRollbackDataList, attrs.pwzKey, &iRollbackStatus);

        if (S_FALSE == hr)
            continue; // not found, nothing to rollback

        // progress message
        hr = CpiActionDataMessage(1, attrs.pwzAccount);
        ExitOnFailure(hr, "Failed to send progress messages");

        if (S_FALSE == hr)
            ExitFunction();

        // action
        switch (attrs.iActionType)
        {
        case atCreate:
            hr = CreatePartitionUser(&attrs);
            ExitOnFailure(hr, "Failed to create partition user, key: %S", attrs.pwzKey);
            break;
        case atRemove:
            hr = RemovePartitionUser(&attrs);
            ExitOnFailure(hr, "Failed to remove partition user, key: %S", attrs.pwzKey);
            break;
        }

        // check rollback status
        if (0 == iRollbackStatus)
            continue; // operation did not complete, skip progress

        // progress
        hr = WcaProgressMessage(attrs.iActionCost, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreePartitionUserAttributes(&attrs);

    return hr;
}


// helper function definitions

static HRESULT ReadPartitionAttributes(
    LPWSTR* ppwzData,
    CPI_PARTITION_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionType);
    ExitOnFailure(hr, "Failed to read action type");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionCost);
    ExitOnFailure(hr, "Failed to read action cost");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzKey);
    ExitOnFailure(hr, "Failed to read key");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzID);
    ExitOnFailure(hr, "Failed to read id");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzName);
    ExitOnFailure(hr, "Failed to read name");
    hr = CpiReadPropertyList(ppwzData, &pAttrs->pPropList);
    ExitOnFailure(hr, "Failed to read properties");

    hr = S_OK;

LExit:
    return hr;
}

static void FreePartitionAttributes(
    CPI_PARTITION_ATTRIBUTES* pAttrs
    )
{
    ReleaseStr(pAttrs->pwzKey);
    ReleaseStr(pAttrs->pwzID);
    ReleaseStr(pAttrs->pwzName);

    if (pAttrs->pPropList)
        CpiFreePropertyList(pAttrs->pPropList);
}

static HRESULT CpiEnsurePartitionsEnabled()
{
    HRESULT hr = S_OK;

    ICatalogCollection* piLocalComputerColl = NULL;
    IDispatch* piDisp = NULL;
    ICatalogObject* piLocalComputerObj = NULL;
    VARIANT vtVal;
    BSTR bsPartitionsEnabledName = ::SysAllocString(L"PartitionsEnabled");
    long numChanges = 0;

    ::VariantInit(&vtVal);

    // get collection
    hr = CpiExecGetCatalogCollection(L"LocalComputer", &piLocalComputerColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    // find object, there will be only one in the LocalComputer collection
    hr = piLocalComputerColl->get_Item(0, &piDisp);
    ExitOnFailure(hr, "Failed to get object from collection");

    hr = piDisp->QueryInterface(IID_ICatalogObject, (void**)&piLocalComputerObj);
    ExitOnFailure(hr, "Failed to get IID_ICatalogObject interface");

    // and then we get the value of the PartitionsEnabled property
    hr = piLocalComputerObj->get_Value(bsPartitionsEnabledName, &vtVal);
    if (!vtVal.boolVal)
    {
        vtVal.boolVal = true;
        hr = piLocalComputerObj->put_Value(bsPartitionsEnabledName, vtVal);
        ExitOnFailure(hr, "Failed to put value to Enable COM+ PartitionsEnabled property");
        hr = piLocalComputerColl->SaveChanges(&numChanges);
        ExitOnFailure(hr, "Failed to save PartitionsEnabled property");

        // we'll read back the hopefully updated values of the PartitionsEnabled property
        // if it's still False, then we're on a Windows Desktop that doesn't allow Partitions
        // (as of Windows Server2003 Microsoft limited Partitions to only ServerOS platforms)
        hr = piLocalComputerObj->get_Value(bsPartitionsEnabledName, &vtVal);
        ExitOnFailure(hr, "Failed to read PartitionsEnabled property");
    }

    if (vtVal.boolVal)
    {
        // everything went well, we have the Partitioning available
        hr = S_OK;
    }
    else
    {
        // we're on a Desktop OS, or couldn't otherwise enable partitioning
        WcaLog(LOGMSG_STANDARD, "Failed to Enable COM+ PartitionEnabled property. This suggests Partitioning was attempted on a Desktop OS, which is not supported");
        hr = S_FALSE;
    }

LExit:
    // clean up
    ReleaseObject(piLocalComputerColl);
    ReleaseObject(piLocalComputerObj);
    ReleaseBSTR(bsPartitionsEnabledName);
    ::VariantClear(&vtVal);

    return hr;
}

static HRESULT CreatePartition(
    CPI_PARTITION_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piPartColl = NULL;
    ICatalogObject* piPartObj = NULL;

    long lChanges = 0;

    // log
    WcaLog(LOGMSG_VERBOSE, "Creating partition, key: %S", pAttrs->pwzKey);

    // get partitions collection
    hr = CpiExecGetPartitionsCollection(&piPartColl);
    ExitOnFailure(hr, "Failed to get partitions collection");

    // check if partition exists
    hr = CpiFindCollectionObjectByStringKey(piPartColl, pAttrs->pwzID, &piPartObj);
    ExitOnFailure(hr, "Failed to find partition");

    if (S_FALSE == hr)
    {
        hr = CpiEnsurePartitionsEnabled();
        ExitOnFailure(hr, "Failed to enable partitions");

        // create partition
        hr = CpiAddCollectionObject(piPartColl, &piPartObj);
        ExitOnFailure(hr, "Failed to add partition to collection");

        hr = CpiPutCollectionObjectValue(piPartObj, L"ID", pAttrs->pwzID);
        ExitOnFailure(hr, "Failed to set partition id property");

        hr = CpiPutCollectionObjectValue(piPartObj, L"Name", pAttrs->pwzName);
        ExitOnFailure(hr, "Failed to set partition name property");
    }

    // properties
    hr = CpiPutCollectionObjectValues(piPartObj, pAttrs->pPropList);
    ExitOnFailure(hr, "Failed to write properties");

    // save changes
    hr = piPartColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    // log
    WcaLog(LOGMSG_VERBOSE, "%d changes saved to catalog, key: %S", lChanges, pAttrs->pwzKey);

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piPartColl);
    ReleaseObject(piPartObj);

    return hr;
}

static HRESULT RemovePartition(
    CPI_PARTITION_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piPartColl = NULL;

    long lChanges = 0;

    // log
    WcaLog(LOGMSG_VERBOSE, "Removing partition, key: %S", pAttrs->pwzKey);

    // get partitions collection
    hr = CpiExecGetPartitionsCollection(&piPartColl);
    ExitOnFailure(hr, "Failed to get partitions collection");

    // remove
    hr = CpiRemoveCollectionObject(piPartColl, pAttrs->pwzID, NULL, TRUE);
    ExitOnFailure(hr, "Failed to remove partition");

    if (S_FALSE == hr)
    {
        // partition not found
        WcaLog(LOGMSG_VERBOSE, "Partition not found, nothing to delete, key: %S", pAttrs->pwzKey);
        ExitFunction1(hr = S_OK);
    }

    // save changes
    hr = piPartColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    // log
    WcaLog(LOGMSG_VERBOSE, "%d changes saved to catalog, key: %S", lChanges, pAttrs->pwzKey);

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piPartColl);

    return hr;
}

static HRESULT ReadPartitionUserAttributes(
    LPWSTR* ppwzData,
    CPI_PARTITION_USER_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionType);
    ExitOnFailure(hr, "Failed to read action type");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionCost);
    ExitOnFailure(hr, "Failed to read action cost");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzKey);
    ExitOnFailure(hr, "Failed to read key");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzAccount);
    ExitOnFailure(hr, "Failed to read account name");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzPartID);
    ExitOnFailure(hr, "Failed to read partition id");

    hr = S_OK;

LExit:
    return hr;
}

static void FreePartitionUserAttributes(
    CPI_PARTITION_USER_ATTRIBUTES* pAttrs
    )
{
    ReleaseStr(pAttrs->pwzKey);
    ReleaseStr(pAttrs->pwzAccount);
    ReleaseStr(pAttrs->pwzPartID);
}

static HRESULT CreatePartitionUser(
    CPI_PARTITION_USER_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    ICatalogCollection* piUserColl = NULL;
    ICatalogObject* piUserObj = NULL;

    PSID pSid = NULL;
    long lChanges = 0;

    // log
    WcaLog(LOGMSG_VERBOSE, "Setting default partition for user, key: %S", pAttrs->pwzKey);

    // get partition users collection
    hr = CpiGetPartitionUsersCollection(&piUserColl);
    ExitOnFailure(hr, "Failed to get partition users collection");

    // get SID for account
    do {
        er = ERROR_SUCCESS;
        hr = CpiAccountNameToSid(pAttrs->pwzAccount, &pSid);
        if (HRESULT_FROM_WIN32(ERROR_NONE_MAPPED) == hr && !::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_ROLLBACK))
        {
            WcaLog(LOGMSG_STANDARD, "Failed to lookup account name, hr: 0x%x, account: '%S'", hr, pAttrs->pwzAccount);
            er = WcaErrorMessage(msierrComPlusFailedLookupNames, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
            switch (er)
            {
            case IDABORT:
                ExitFunction(); // exit with error code from CpiAccountNameToSid()
            case IDRETRY:
                break;
            case IDIGNORE:
            default:
                ExitFunction1(hr = S_OK);
            }
        }
        else
            ExitOnFailure(hr, "Failed to get SID for account");
    } while (IDRETRY == er);

    // remove any existing entry
    hr = CpiRemoveUserCollectionObject(piUserColl, pSid);
    if (HRESULT_FROM_WIN32(ERROR_NONE_MAPPED) == hr || HRESULT_FROM_WIN32(ERROR_SOME_NOT_MAPPED) == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Failed to lookup account names, hr: 0x%x", hr);
        hr = S_FALSE;
    }
    else
        ExitOnFailure(hr, "Failed to remove user");

    if (S_OK == hr)
        WcaLog(LOGMSG_VERBOSE, "Existing default partition for user was removed, key: %S", pAttrs->pwzKey);

    // add partition user
    hr = CpiAddCollectionObject(piUserColl, &piUserObj);
    ExitOnFailure(hr, "Failed to add partition to collection");

    hr = CpiPutCollectionObjectValue(piUserObj, L"AccountName", pAttrs->pwzAccount);
    ExitOnFailure(hr, "Failed to set account name property");

    hr = CpiPutCollectionObjectValue(piUserObj, L"DefaultPartitionID", pAttrs->pwzPartID);
    ExitOnFailure(hr, "Failed to set default partition id property");

    // save changes
    hr = piUserColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    // log
    WcaLog(LOGMSG_VERBOSE, "%d changes saved to catalog, key: %S", lChanges, pAttrs->pwzKey);

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piUserColl);
    ReleaseObject(piUserObj);

    if (pSid)
        ::HeapFree(::GetProcessHeap(), 0, pSid);

    return hr;
}

static HRESULT RemovePartitionUser(
    CPI_PARTITION_USER_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    ICatalogCollection* piUserColl = NULL;

    PSID pSid = NULL;
    long lChanges = 0;

    // log
    WcaLog(LOGMSG_VERBOSE, "Removing default partition for user, key: %S", pAttrs->pwzKey);

    // get partition users collection
    hr = CpiGetPartitionUsersCollection(&piUserColl);
    ExitOnFailure(hr, "Failed to get partition users collection");

    // get SID for account
    do {
        er = ERROR_SUCCESS;
        hr = CpiAccountNameToSid(pAttrs->pwzAccount, &pSid);
        if (HRESULT_FROM_WIN32(ERROR_NONE_MAPPED) == hr && !::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_ROLLBACK))
        {
            WcaLog(LOGMSG_STANDARD, "Failed to lookup account name, hr: 0x%x, account: '%S'", hr, pAttrs->pwzAccount);
            er = WcaErrorMessage(msierrComPlusFailedLookupNames, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
            switch (er)
            {
            case IDABORT:
                ExitFunction(); // exit with error code from CpiAccountNameToSid()
            case IDRETRY:
                break;
            case IDIGNORE:
            default:
                ExitFunction1(hr = S_OK);
            }
        }
        else
            ExitOnFailure(hr, "Failed to get SID for account");
    } while (IDRETRY == er);

    // remove
    hr = CpiRemoveUserCollectionObject(piUserColl, pSid);
    if (HRESULT_FROM_WIN32(ERROR_NONE_MAPPED) == hr || HRESULT_FROM_WIN32(ERROR_SOME_NOT_MAPPED) == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Failed to lookup account names, hr: 0x%x", hr);
        hr = S_FALSE;
    }
    else
        ExitOnFailure(hr, "Failed to remove user");

    if (S_FALSE == hr)
    {
        // user not found
        WcaLog(LOGMSG_VERBOSE, "Default partition for user not found, nothing to delete, key: %S", pAttrs->pwzKey);
        ExitFunction1(hr = S_OK);
    }

    // save changes
    hr = piUserColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    // log
    WcaLog(LOGMSG_VERBOSE, "%d changes saved to catalog, key: %S", lChanges, pAttrs->pwzKey);

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piUserColl);

    if (pSid)
        ::HeapFree(::GetProcessHeap(), 0, pSid);

    return hr;
}
