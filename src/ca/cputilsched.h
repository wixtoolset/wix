#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


enum eRunMode { rmDeferred = 1, rmCommit, rmRollback };

enum eComPlusPropertyType { cpptNone = 0, cpptBoolean, cpptInteger, cpptString, cpptUser };

enum eComPlusTables
{
    cptComPlusPartition               = (1 << 0),
    cptComPlusPartitionProperty       = (1 << 1),
    cptComPlusPartitionRole           = (1 << 2),
    cptComPlusUserInPartitionRole     = (1 << 3),
    cptComPlusGroupInPartitionRole    = (1 << 4),
    cptComPlusPartitionUser           = (1 << 5),
    cptComPlusApplication             = (1 << 6),
    cptComPlusApplicationProperty     = (1 << 7),
    cptComPlusApplicationRole         = (1 << 8),
    cptComPlusApplicationRoleProperty = (1 << 9),
    cptComPlusUserInApplicationRole   = (1 << 10),
    cptComPlusGroupInApplicationRole  = (1 << 11),
    cptComPlusAssembly                = (1 << 12),
    cptComPlusAssemblyDependency      = (1 << 13),
    cptComPlusComponent               = (1 << 14),
    cptComPlusComponentProperty       = (1 << 15),
    cptComPlusRoleForComponent        = (1 << 16),
    cptComPlusInterface               = (1 << 17),
    cptComPlusInterfaceProperty       = (1 << 18),
    cptComPlusRoleForInterface        = (1 << 19),
    cptComPlusMethod                  = (1 << 20),
    cptComPlusMethodProperty          = (1 << 21),
    cptComPlusRoleForMethod           = (1 << 22),
    cptComPlusSubscription            = (1 << 23),
    cptComPlusSubscriptionProperty    = (1 << 24)
};


// structs

struct CPI_PROPERTY_DEFINITION
{
    LPCWSTR pwzName;
    int iType;
    int iMinVersionNT;
};


// function prototypes

void CpiSchedInitialize();
void CpiSchedFinalize();
BOOL CpiTableExists(
    int iTable
    );
HRESULT CpiSchedGetAdminCatalog(
    ICOMAdminCatalog** ppiCatalog
    );
HRESULT CpiSchedGetCatalogCollection(
    LPCWSTR pwzName,
    ICatalogCollection** ppiColl
    );
HRESULT CpiSchedGetCatalogCollection(
    ICatalogCollection* piColl,
    ICatalogObject* piObj,
    LPCWSTR pwzName,
    ICatalogCollection** ppiColl
    );
HRESULT CpiGetKeyForObject(
    ICatalogObject* piObj,
    LPWSTR pwzKey,
    SIZE_T cchKey
    );
HRESULT CpiFindCollectionObject(
    ICatalogCollection* piColl,
    LPCWSTR pwzID,
    LPCWSTR pwzName,
    ICatalogObject** ppiObj
    );
HRESULT CpiSchedGetPartitionsCollection(
    ICatalogCollection** ppiPartColl
    );
HRESULT CpiSchedGetApplicationsCollection(
    ICatalogCollection** ppiAppColl
    );
HRESULT CpiAddActionTextToActionData(
    LPCWSTR pwzAction,
    LPWSTR* ppwzActionData
    );
HRESULT CpiVerifyComponentArchitecure(
    LPCWSTR pwzComponent,
    BOOL* pfMatchingArchitecture
    );
HRESULT CpiPropertiesRead(
    LPCWSTR pwzQuery,
    LPCWSTR pwzKey,
    CPI_PROPERTY_DEFINITION* pPropDefList,
    CPI_PROPERTY** ppPropList,
    int* piCount
    );
void CpiPropertiesFreeList(
    CPI_PROPERTY* pList
    );
HRESULT CpiAddPropertiesToActionData(
    int iPropCount,
    CPI_PROPERTY* pPropList,
    LPWSTR* ppwzActionData
    );
HRESULT CpiBuildAccountName(
    LPCWSTR pwzDomain,
    LPCWSTR pwzName,
    LPWSTR* ppwzAccount
    );
HRESULT CpiGetTempFileName(
    LPWSTR* ppwzTempFile
    );
HRESULT CpiCreateId(
    LPWSTR pwzDest,
    SIZE_T cchDest
    );
BOOL CpiIsInstalled(
    INSTALLSTATE isInstalled
    );
BOOL CpiWillBeInstalled(
    INSTALLSTATE isInstalled,
    INSTALLSTATE isAction
    );
HRESULT PcaGuidToRegFormat(
    LPWSTR pwzGuid,
    LPWSTR pwzDest,
    SIZE_T cchDest
    );
