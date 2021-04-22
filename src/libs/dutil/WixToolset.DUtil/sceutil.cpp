// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#include "sceutil.h"

// Limit is documented as 4 GB, but for some reason the API's don't let us specify anything above 4091 MB.
#define MAX_SQLCE_DATABASE_SIZE 4091

// In case of some older versions of sqlce_oledb.h don't have these definitions, define some types.
#ifndef DBTYPEFOR_DBLENGTH
#ifdef _WIN64
#define SKIP_SCE_COMPILE
#else
#define SCE_32BIT_ONLY
typedef DWORD DBLENGTH;
typedef LONG  DBROWOFFSET;
typedef LONG  DBROWCOUNT;
typedef DWORD DBCOUNTITEM;
typedef DWORD DBORDINAL;
typedef LONG  DB_LORDINAL;
typedef DWORD DBBKMARK;
typedef DWORD DBBYTEOFFSET;
typedef DWORD DBREFCOUNT;
typedef DWORD DB_UPARAMS;
typedef LONG  DB_LPARAMS;
typedef DWORD DBHASHVALUE;
typedef DWORD DB_DWRESERVE;
typedef LONG  DB_LRESERVE;
typedef DWORD DB_URESERVE;
#endif

#endif

#ifndef SKIP_SCE_COMPILE // If the sce headers don't support 64-bit, don't build for 64-bit

// structs
struct SCE_DATABASE_INTERNAL
{
    // In case we call DllGetClassObject on a specific file
    HMODULE hSqlCeDll;

    volatile LONG dwTransactionRefcount;
    IDBInitialize *pIDBInitialize;
    IDBCreateSession *pIDBCreateSession;
    ITransactionLocal *pITransactionLocal;
    IDBProperties *pIDBProperties;
    IOpenRowset *pIOpenRowset;
    ISessionProperties *pISessionProperties;

    BOOL fChanges; // This database has changed
    BOOL fPendingChanges; // Some changes are pending, upon transaction commit
    BOOL fRollbackTransaction; // If this flag is true, the current transaction was requested to be rolled back
    BOOL fTransactionBadState; // If this flag is true, we were unable to get out of a transaction, so starting a new transaction should fail

    // If the database was opened as read-only, we copied it here - so delete it on close
    LPWSTR sczTempDbFile;
};

struct SCE_ROW
{
    SCE_DATABASE_INTERNAL *pDatabaseInternal;

    SCE_TABLE_SCHEMA *pTableSchema;
    IRowset *pIRowset;
    HROW hRow;
    BOOL fInserting;

    DWORD dwBindingIndex;
    DBBINDING *rgBinding;
    SIZE_T cbOffset;
    BYTE *pbData;
};

struct SCE_QUERY
{
    SCE_TABLE_SCHEMA *pTableSchema;
    SCE_INDEX_SCHEMA *pIndexSchema;
    SCE_DATABASE_INTERNAL *pDatabaseInternal;

    // Accessor build-up members
    DWORD dwBindingIndex;
    DBBINDING *rgBinding;
    SIZE_T cbOffset;
    BYTE *pbData;
};

struct SCE_QUERY_RESULTS
{
    SCE_DATABASE_INTERNAL *pDatabaseInternal;
    IRowset *pIRowset;
    SCE_TABLE_SCHEMA *pTableSchema;
};

extern const int SCE_ROW_HANDLE_BYTES = sizeof(SCE_ROW);
extern const int SCE_QUERY_HANDLE_BYTES = sizeof(SCE_QUERY);
extern const int SCE_QUERY_RESULTS_HANDLE_BYTES = sizeof(SCE_QUERY_RESULTS);

// The following is the internal Sce-maintained table to tell the identifier and version of the schema
const SCE_COLUMN_SCHEMA SCE_INTERNAL_VERSION_TABLE_VERSION_COLUMN_SCHEMA[] =
{
    {
        L"AppIdentifier",
        DBTYPE_WSTR,
        0,
        FALSE,
        TRUE,
        FALSE,
        NULL,
        0,
        0
    },
    {
        L"Version",
        DBTYPE_I4,
        0,
        FALSE,
        FALSE,
        FALSE,
        NULL,
        0,
        0
    }
};

const SCE_TABLE_SCHEMA SCE_INTERNAL_VERSION_TABLE_SCHEMA[] =
{
    L"SceSchemaTablev1",
    _countof(SCE_INTERNAL_VERSION_TABLE_VERSION_COLUMN_SCHEMA),
    (SCE_COLUMN_SCHEMA *)SCE_INTERNAL_VERSION_TABLE_VERSION_COLUMN_SCHEMA,
    0,
    NULL,
    NULL,
    NULL
};

// internal function declarations

// Creates an instance of SQL Server CE object, returning IDBInitialize object
// If a file path is provided in wzSqlCeDllPath parameter, it calls DllGetClassObject
// on that file specifically. Otherwise it calls CoCreateInstance
static HRESULT CreateSqlCe(
    __in_z_opt LPCWSTR wzSqlCeDllPath,
    __out IDBInitialize **ppIDBInitialize,
    __out_opt HMODULE *phSqlCeDll
    );
static HRESULT RunQuery(
    __in BOOL fRange,
    __in_bcount(SCE_QUERY_BYTES) SCE_QUERY_HANDLE psqhHandle,
    __out SCE_QUERY_RESULTS **ppsqrhHandle
    );
static HRESULT FillOutColumnDescFromSchema(
    __in const SCE_COLUMN_SCHEMA *pSchema,
    __out DBCOLUMNDESC pColumnDesc
    );
static HRESULT EnsureSchema(
    __in SCE_DATABASE *pDatabase,
    __in SCE_DATABASE_SCHEMA *pDatabaseSchema
    );
static HRESULT OpenSchema(
    __in SCE_DATABASE *pDatabase,
    __in SCE_DATABASE_SCHEMA *pdsSchema
    );
static HRESULT SetColumnValue(
    __in const SCE_TABLE_SCHEMA *pTableSchema,
    __in DWORD dwColumnIndex,
    __in_bcount_opt(cbSize) const BYTE *pbData,
    __in SIZE_T cbSize,
    __inout DBBINDING *pBinding,
    __inout SIZE_T *pcbOffset,
    __inout BYTE **ppbBuffer
    );
static HRESULT GetColumnValue(
    __in SCE_ROW *pRow,
    __in DWORD dwColumnIndex,
    __out_opt BYTE **ppbData,
    __out SIZE_T *pcbSize
    );
static HRESULT GetColumnValueFixed(
    __in SCE_ROW *pRow,
    __in DWORD dwColumnIndex,
    __in DWORD cbSize,
    __out BYTE *pbData
    );
static HRESULT EnsureLocalColumnConstraints(
    __in ITableDefinition *pTableDefinition,
    __in DBID *pTableID,
    __in SCE_TABLE_SCHEMA *pTableSchema
    );
static HRESULT EnsureForeignColumnConstraints(
    __in ITableDefinition *pTableDefinition,
    __in DBID *pTableID,
    __in SCE_TABLE_SCHEMA *pTableSchema,
    __in SCE_DATABASE_SCHEMA *pDatabaseSchema
    );
static HRESULT SetSessionProperties(
    __in ISessionProperties *pISessionProperties
    );
static HRESULT GetDatabaseSchemaInfo(
    __in SCE_DATABASE *pDatabase,
    __out LPWSTR *psczSchemaType,
    __out DWORD *pdwVersion
    );
static HRESULT SetDatabaseSchemaInfo(
    __in SCE_DATABASE *pDatabase,
    __in LPCWSTR wzSchemaType,
    __in DWORD dwVersion
    );
static void ReleaseDatabase(
    SCE_DATABASE *pDatabase
    );
static void ReleaseDatabaseInternal(
    SCE_DATABASE_INTERNAL *pDatabaseInternal
    );

// function definitions
extern "C" HRESULT DAPI SceCreateDatabase(
    __in_z LPCWSTR sczFile,
    __in_z_opt LPCWSTR wzSqlCeDllPath,
    __out SCE_DATABASE **ppDatabase
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDirectory = NULL;
    SCE_DATABASE *pNewSceDatabase = NULL;
    SCE_DATABASE_INTERNAL *pNewSceDatabaseInternal = NULL;
    IDBDataSourceAdmin *pIDBDataSourceAdmin = NULL; 
    DBPROPSET rgdbpDataSourcePropSet[2] = { };
    DBPROP rgdbpDataSourceProp[2] = { };
    DBPROP rgdbpDataSourceSsceProp[1] = { };

    pNewSceDatabase = reinterpret_cast<SCE_DATABASE *>(MemAlloc(sizeof(SCE_DATABASE), TRUE));
    ExitOnNull(pNewSceDatabase, hr, E_OUTOFMEMORY, "Failed to allocate SCE_DATABASE struct");

    pNewSceDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(MemAlloc(sizeof(SCE_DATABASE_INTERNAL), TRUE));
    ExitOnNull(pNewSceDatabaseInternal, hr, E_OUTOFMEMORY, "Failed to allocate SCE_DATABASE_INTERNAL struct");

    pNewSceDatabase->sdbHandle = reinterpret_cast<void *>(pNewSceDatabaseInternal);

    hr = CreateSqlCe(wzSqlCeDllPath, &pNewSceDatabaseInternal->pIDBInitialize, &pNewSceDatabaseInternal->hSqlCeDll);
    ExitOnFailure(hr, "Failed to get IDBInitialize interface");
    
    hr = pNewSceDatabaseInternal->pIDBInitialize->QueryInterface(IID_IDBDataSourceAdmin, reinterpret_cast<void **>(&pIDBDataSourceAdmin));
    ExitOnFailure(hr, "Failed to get IDBDataSourceAdmin interface");

    hr = PathGetDirectory(sczFile, &sczDirectory);
    ExitOnFailure(hr, "Failed to get directory portion of path: %ls", sczFile);

    hr = DirEnsureExists(sczDirectory, NULL);
    ExitOnFailure(hr, "Failed to ensure directory exists: %ls", sczDirectory);

    rgdbpDataSourceProp[0].dwPropertyID = DBPROP_INIT_DATASOURCE;
    rgdbpDataSourceProp[0].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpDataSourceProp[0].vValue.vt = VT_BSTR;
    rgdbpDataSourceProp[0].vValue.bstrVal = ::SysAllocString(sczFile);

    rgdbpDataSourceProp[1].dwPropertyID = DBPROP_INIT_MODE;
    rgdbpDataSourceProp[1].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpDataSourceProp[1].vValue.vt = VT_I4;
    rgdbpDataSourceProp[1].vValue.lVal = DB_MODE_SHARE_DENY_NONE;

    // SQL CE doesn't seem to allow us to specify DBPROP_INIT_PROMPT if we include any properties from DBPROPSET_SSCE_DBINIT 
    rgdbpDataSourcePropSet[0].guidPropertySet = DBPROPSET_DBINIT;
    rgdbpDataSourcePropSet[0].rgProperties = rgdbpDataSourceProp;
    rgdbpDataSourcePropSet[0].cProperties = _countof(rgdbpDataSourceProp);

    rgdbpDataSourceSsceProp[0].dwPropertyID = DBPROP_SSCE_MAX_DATABASE_SIZE;
    rgdbpDataSourceSsceProp[0].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpDataSourceSsceProp[0].vValue.vt = VT_I4;
    rgdbpDataSourceSsceProp[0].vValue.intVal = MAX_SQLCE_DATABASE_SIZE;

    rgdbpDataSourcePropSet[1].guidPropertySet = DBPROPSET_SSCE_DBINIT;
    rgdbpDataSourcePropSet[1].rgProperties = rgdbpDataSourceSsceProp;
    rgdbpDataSourcePropSet[1].cProperties = _countof(rgdbpDataSourceSsceProp);

    hr = pIDBDataSourceAdmin->CreateDataSource(_countof(rgdbpDataSourcePropSet), rgdbpDataSourcePropSet, NULL, IID_IUnknown, NULL);
    ExitOnFailure(hr, "Failed to create data source");

    hr = pNewSceDatabaseInternal->pIDBInitialize->QueryInterface(IID_IDBProperties, reinterpret_cast<void **>(&pNewSceDatabaseInternal->pIDBProperties));
    ExitOnFailure(hr, "Failed to get IDBProperties interface");

    hr = pNewSceDatabaseInternal->pIDBInitialize->QueryInterface(IID_IDBCreateSession, reinterpret_cast<void **>(&pNewSceDatabaseInternal->pIDBCreateSession));
    ExitOnFailure(hr, "Failed to get IDBCreateSession interface");

    hr = pNewSceDatabaseInternal->pIDBCreateSession->CreateSession(NULL, IID_ISessionProperties, reinterpret_cast<IUnknown **>(&pNewSceDatabaseInternal->pISessionProperties));
    ExitOnFailure(hr, "Failed to get ISessionProperties interface");

    hr = SetSessionProperties(pNewSceDatabaseInternal->pISessionProperties);
    ExitOnFailure(hr, "Failed to set session properties");

    hr = pNewSceDatabaseInternal->pISessionProperties->QueryInterface(IID_IOpenRowset, reinterpret_cast<void **>(&pNewSceDatabaseInternal->pIOpenRowset));
    ExitOnFailure(hr, "Failed to get IOpenRowset interface");

    hr = pNewSceDatabaseInternal->pISessionProperties->QueryInterface(IID_ITransactionLocal, reinterpret_cast<void **>(&pNewSceDatabaseInternal->pITransactionLocal));
    ExitOnFailure(hr, "Failed to get ITransactionLocal interface");

    *ppDatabase = pNewSceDatabase;
    pNewSceDatabase = NULL;

LExit:
    ReleaseStr(sczDirectory);
    ReleaseObject(pIDBDataSourceAdmin);
    ReleaseDatabase(pNewSceDatabase);
    ReleaseBSTR(rgdbpDataSourceProp[0].vValue.bstrVal);

    return hr;
}

extern "C" HRESULT DAPI SceOpenDatabase(
    __in_z LPCWSTR sczFile,
    __in_z_opt LPCWSTR wzSqlCeDllPath,
    __in LPCWSTR wzExpectedSchemaType,
    __in DWORD dwExpectedVersion,
    __out SCE_DATABASE **ppDatabase,
    __in BOOL fReadOnly
    )
{
    HRESULT hr = S_OK;
    DWORD dwVersionFound = 0;
    WCHAR wzTempDbFile[MAX_PATH];
    LPCWSTR wzPathToOpen = NULL;
    LPWSTR sczSchemaType = NULL;
    SCE_DATABASE *pNewSceDatabase = NULL;
    SCE_DATABASE_INTERNAL *pNewSceDatabaseInternal = NULL;
    DBPROPSET rgdbpDataSourcePropSet[2] = { };
    DBPROP rgdbpDataSourceProp[2] = { };
    DBPROP rgdbpDataSourceSsceProp[1] =  { };

    pNewSceDatabase = reinterpret_cast<SCE_DATABASE *>(MemAlloc(sizeof(SCE_DATABASE), TRUE));
    ExitOnNull(pNewSceDatabase, hr, E_OUTOFMEMORY, "Failed to allocate SCE_DATABASE struct");

    pNewSceDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(MemAlloc(sizeof(SCE_DATABASE_INTERNAL), TRUE));
    ExitOnNull(pNewSceDatabaseInternal, hr, E_OUTOFMEMORY, "Failed to allocate SCE_DATABASE_INTERNAL struct");

    pNewSceDatabase->sdbHandle = reinterpret_cast<void *>(pNewSceDatabaseInternal);

    hr = CreateSqlCe(wzSqlCeDllPath, &pNewSceDatabaseInternal->pIDBInitialize, &pNewSceDatabaseInternal->hSqlCeDll);
    ExitOnFailure(hr, "Failed to get IDBInitialize interface");
    
    hr = pNewSceDatabaseInternal->pIDBInitialize->QueryInterface(IID_IDBProperties, reinterpret_cast<void **>(&pNewSceDatabaseInternal->pIDBProperties));
    ExitOnFailure(hr, "Failed to get IDBProperties interface");

    // TODO: had trouble getting SQL CE to read a file read-only, so we're copying it to a temp path for now.
    if (fReadOnly)
    {
        hr = DirCreateTempPath(PathFile(sczFile), (LPWSTR)wzTempDbFile, _countof(wzTempDbFile));
        ExitOnFailure(hr, "Failed to get temp path");

        hr = FileEnsureCopy(sczFile, (LPCWSTR)wzTempDbFile, TRUE);
        ExitOnFailure(hr, "Failed to copy file to temp path");

        hr = StrAllocString(&pNewSceDatabaseInternal->sczTempDbFile, (LPCWSTR)wzTempDbFile, 0);
        ExitOnFailure(hr, "Failed to copy temp db file path");

        wzPathToOpen = (LPCWSTR)wzTempDbFile;
    }
    else
    {
        wzPathToOpen = sczFile;
    }

    rgdbpDataSourceProp[0].dwPropertyID = DBPROP_INIT_DATASOURCE;
    rgdbpDataSourceProp[0].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpDataSourceProp[0].vValue.vt = VT_BSTR;
    rgdbpDataSourceProp[0].vValue.bstrVal = ::SysAllocString(wzPathToOpen);

    rgdbpDataSourceProp[1].dwPropertyID = DBPROP_INIT_MODE;
    rgdbpDataSourceProp[1].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpDataSourceProp[1].vValue.vt = VT_I4;
    rgdbpDataSourceProp[1].vValue.lVal = DB_MODE_SHARE_DENY_NONE;

    // SQL CE doesn't seem to allow us to specify DBPROP_INIT_PROMPT if we include any properties from DBPROPSET_SSCE_DBINIT 
    rgdbpDataSourcePropSet[0].guidPropertySet = DBPROPSET_DBINIT;
    rgdbpDataSourcePropSet[0].rgProperties = rgdbpDataSourceProp;
    rgdbpDataSourcePropSet[0].cProperties = _countof(rgdbpDataSourceProp);

    rgdbpDataSourceSsceProp[0].dwPropertyID = DBPROP_SSCE_MAX_DATABASE_SIZE;
    rgdbpDataSourceSsceProp[0].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpDataSourceSsceProp[0].vValue.vt = VT_I4;
    rgdbpDataSourceSsceProp[0].vValue.lVal = MAX_SQLCE_DATABASE_SIZE;

    rgdbpDataSourcePropSet[1].guidPropertySet = DBPROPSET_SSCE_DBINIT;
    rgdbpDataSourcePropSet[1].rgProperties = rgdbpDataSourceSsceProp;
    rgdbpDataSourcePropSet[1].cProperties = _countof(rgdbpDataSourceSsceProp);

    hr = pNewSceDatabaseInternal->pIDBProperties->SetProperties(_countof(rgdbpDataSourcePropSet), rgdbpDataSourcePropSet);
    ExitOnFailure(hr, "Failed to set initial properties to open database");

    hr = pNewSceDatabaseInternal->pIDBInitialize->Initialize();
    ExitOnFailure(hr, "Failed to open database: %ls", sczFile);

    hr = pNewSceDatabaseInternal->pIDBInitialize->QueryInterface(IID_IDBCreateSession, reinterpret_cast<void **>(&pNewSceDatabaseInternal->pIDBCreateSession));
    ExitOnFailure(hr, "Failed to get IDBCreateSession interface");

    hr = pNewSceDatabaseInternal->pIDBCreateSession->CreateSession(NULL, IID_ISessionProperties, reinterpret_cast<IUnknown **>(&pNewSceDatabaseInternal->pISessionProperties));
    ExitOnFailure(hr, "Failed to get ISessionProperties interface");

    hr = SetSessionProperties(pNewSceDatabaseInternal->pISessionProperties);
    ExitOnFailure(hr, "Failed to set session properties");

    hr = pNewSceDatabaseInternal->pISessionProperties->QueryInterface(IID_IOpenRowset, reinterpret_cast<void **>(&pNewSceDatabaseInternal->pIOpenRowset));
    ExitOnFailure(hr, "Failed to get IOpenRowset interface");

    hr = pNewSceDatabaseInternal->pISessionProperties->QueryInterface(IID_ITransactionLocal, reinterpret_cast<void **>(&pNewSceDatabaseInternal->pITransactionLocal));
    ExitOnFailure(hr, "Failed to get ITransactionLocal interface");

    hr = GetDatabaseSchemaInfo(pNewSceDatabase, &sczSchemaType, &dwVersionFound);
    ExitOnFailure(hr, "Failed to find schema version of database");

    if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, sczSchemaType, -1, wzExpectedSchemaType, -1))
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FILE_TYPE);
        ExitOnRootFailure(hr, "Tried to open wrong database type - expected type %ls, found type %ls", wzExpectedSchemaType, sczSchemaType);
    }
    else if (dwVersionFound != dwExpectedVersion)
    {
        hr = HRESULT_FROM_WIN32(ERROR_PRODUCT_VERSION);
        ExitOnRootFailure(hr, "Tried to open wrong database schema version - expected version %u, found version %u", dwExpectedVersion, dwVersionFound);
    }

    *ppDatabase = pNewSceDatabase;
    pNewSceDatabase = NULL;

LExit:
    ReleaseBSTR(rgdbpDataSourceProp[0].vValue.bstrVal);
    ReleaseStr(sczSchemaType);
    ReleaseDatabase(pNewSceDatabase);

    return hr;
}

extern "C" HRESULT DAPI SceEnsureDatabase(
    __in_z LPCWSTR sczFile,
    __in_z_opt LPCWSTR wzSqlCeDllPath,
    __in LPCWSTR wzSchemaType,
    __in DWORD dwExpectedVersion,
    __in SCE_DATABASE_SCHEMA *pdsSchema,
    __out SCE_DATABASE **ppDatabase
    )
{
    HRESULT hr = S_OK;
    SCE_DATABASE *pDatabase = NULL;

    if (FileExistsEx(sczFile, NULL))
    {
        hr = SceOpenDatabase(sczFile, wzSqlCeDllPath, wzSchemaType, dwExpectedVersion, &pDatabase, FALSE);
        ExitOnFailure(hr, "Failed to open database while ensuring database exists: %ls", sczFile);
    }
    else
    {
        hr = SceCreateDatabase(sczFile, wzSqlCeDllPath, &pDatabase);
        ExitOnFailure(hr, "Failed to create database while ensuring database exists: %ls", sczFile);

        hr = SetDatabaseSchemaInfo(pDatabase, wzSchemaType, dwExpectedVersion);
        ExitOnFailure(hr, "Failed to set schema version of database");
    }

    hr = EnsureSchema(pDatabase, pdsSchema);
    ExitOnFailure(hr, "Failed to ensure schema is correct in database: %ls", sczFile);

    // Keep a pointer to the schema in the SCE_DATABASE object for future reference
    pDatabase->pdsSchema = pdsSchema;

    *ppDatabase = pDatabase;
    pDatabase = NULL;

LExit:
    ReleaseDatabase(pDatabase);

    return hr;
}

extern "C" HRESULT DAPI SceIsTableEmpty(
    __in SCE_DATABASE *pDatabase,
    __in DWORD dwTableIndex,
    __out BOOL *pfEmpty
    )
{
    HRESULT hr = S_OK;
    SCE_ROW_HANDLE row = NULL;

    hr = SceGetFirstRow(pDatabase, dwTableIndex, &row);
    if (E_NOTFOUND == hr)
    {
        *pfEmpty = TRUE;
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to get first row while checking if table is empty");

    *pfEmpty = FALSE;

LExit:
    ReleaseSceRow(row);

    return hr;
}

extern "C" HRESULT DAPI SceGetFirstRow(
    __in SCE_DATABASE *pDatabase,
    __in DWORD dwTableIndex,
    __out_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE *pRowHandle
    )
{
    HRESULT hr = S_OK;
    DBCOUNTITEM cRowsObtained = 0;
    HROW hRow = DB_NULL_HROW;
    HROW *phRow = &hRow;
    SCE_ROW *pRow = NULL;
    SCE_TABLE_SCHEMA *pTable = &(pDatabase->pdsSchema->rgTables[dwTableIndex]);

    hr = pTable->pIRowset->RestartPosition(DB_NULL_HCHAPTER);
    ExitOnFailure(hr, "Failed to reset IRowset position to beginning");

    hr = pTable->pIRowset->GetNextRows(DB_NULL_HCHAPTER, 0, 1, &cRowsObtained, &phRow);
    if (DB_S_ENDOFROWSET == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    ExitOnFailure(hr, "Failed to get next first row");

    pRow = reinterpret_cast<SCE_ROW *>(MemAlloc(sizeof(SCE_ROW), TRUE));
    ExitOnNull(pRow, hr, E_OUTOFMEMORY, "Failed to allocate SCE_ROW struct");

    pRow->pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);
    pRow->hRow = hRow;
    pRow->pTableSchema = pTable;
    pRow->pIRowset = pTable->pIRowset;
    pRow->pIRowset->AddRef();

    *pRowHandle = reinterpret_cast<SCE_ROW_HANDLE>(pRow);

LExit:
    return hr;
}

HRESULT DAPI SceGetNextRow(
    __in SCE_DATABASE *pDatabase,
    __in DWORD dwTableIndex,
    __out_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE *pRowHandle
    )
{
    HRESULT hr = S_OK;
    DBCOUNTITEM cRowsObtained = 0;
    HROW hRow = DB_NULL_HROW;
    HROW *phRow = &hRow;
    SCE_ROW *pRow = NULL;
    SCE_TABLE_SCHEMA *pTable = &(pDatabase->pdsSchema->rgTables[dwTableIndex]);

    hr = pTable->pIRowset->GetNextRows(DB_NULL_HCHAPTER, 0, 1, &cRowsObtained, &phRow);
    if (DB_S_ENDOFROWSET == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    ExitOnFailure(hr, "Failed to get next first row");

    pRow = reinterpret_cast<SCE_ROW *>(MemAlloc(sizeof(SCE_ROW), TRUE));
    ExitOnNull(pRow, hr, E_OUTOFMEMORY, "Failed to allocate SCE_ROW struct");

    pRow->pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);
    pRow->hRow = hRow;
    pRow->pTableSchema = pTable;
    pRow->pIRowset = pTable->pIRowset;
    pRow->pIRowset->AddRef();

    *pRowHandle = reinterpret_cast<SCE_ROW_HANDLE>(pRow);

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceBeginTransaction(
    __in SCE_DATABASE *pDatabase
    )
{
    HRESULT hr = S_OK;
    SCE_DATABASE_INTERNAL *pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);

    if (pDatabaseInternal->fTransactionBadState)
    {
        // We're in a hosed transaction state - we can't start a new one
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_RECOVERY_FAILURE));
    }

    ::InterlockedIncrement(&pDatabaseInternal->dwTransactionRefcount);

    if (1 == pDatabaseInternal->dwTransactionRefcount)
    {
        hr = pDatabaseInternal->pITransactionLocal->StartTransaction(ISOLATIONLEVEL_SERIALIZABLE, 0, NULL, NULL);
        ExitOnFailure(hr, "Failed to start transaction");
    }

LExit:
    if (FAILED(hr))
    {
        ::InterlockedDecrement(&pDatabaseInternal->dwTransactionRefcount);
    }

    return hr;
}

extern "C" HRESULT DAPI SceCommitTransaction(
    __in SCE_DATABASE *pDatabase
    )
{
    HRESULT hr = S_OK;
    SCE_DATABASE_INTERNAL *pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);
    Assert(0 < pDatabaseInternal->dwTransactionRefcount);

    ::InterlockedDecrement(&pDatabaseInternal->dwTransactionRefcount);

    if (0 == pDatabaseInternal->dwTransactionRefcount)
    {
        if (pDatabaseInternal->fRollbackTransaction)
        {
            hr = pDatabaseInternal->pITransactionLocal->Abort(NULL, FALSE, FALSE);
            ExitOnFailure(hr, "Failed to abort transaction");
        }
        else
        {
            hr = pDatabaseInternal->pITransactionLocal->Commit(FALSE, XACTTC_SYNC, 0);
            ExitOnFailure(hr, "Failed to commit transaction");
        }

        if (pDatabaseInternal->fPendingChanges)
        {
            pDatabaseInternal->fPendingChanges = FALSE;
            pDatabaseInternal->fChanges = TRUE;
        }

        pDatabaseInternal->fRollbackTransaction = FALSE;
    }

LExit:
    // If we tried to commit and failed, the caller should subsequently call rollback
    if (FAILED(hr))
    {
        ::InterlockedIncrement(&pDatabaseInternal->dwTransactionRefcount);
    }

    return hr;
}

extern "C" HRESULT DAPI SceRollbackTransaction(
    __in SCE_DATABASE *pDatabase
    )
{
    HRESULT hr = S_OK;
    SCE_DATABASE_INTERNAL *pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);
    Assert(0 < pDatabaseInternal->dwTransactionRefcount);

    ::InterlockedDecrement(&pDatabaseInternal->dwTransactionRefcount);

    if (0 == pDatabaseInternal->dwTransactionRefcount)
    {
        hr = pDatabaseInternal->pITransactionLocal->Abort(NULL, FALSE, FALSE);
        ExitOnFailure(hr, "Failed to abort transaction");
        pDatabaseInternal->fPendingChanges = FALSE;

        pDatabaseInternal->fRollbackTransaction = FALSE;
    }
    else
    {
        pDatabaseInternal->fRollbackTransaction = TRUE;
    }

LExit:
    // We're just in a bad state now. Don't increment the transaction refcount (what is the user going to do - call us again?)
    // but mark the database as bad so the user gets an error if they try to start a new transaction.
    if (FAILED(hr))
    {
        TraceError(hr, "Failed to rollback transaction");
        pDatabaseInternal->fTransactionBadState = TRUE;
    }

    return hr;
}

extern "C" HRESULT DAPI SceDeleteRow(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE *pRowHandle
    )
{
    HRESULT hr = S_OK;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(*pRowHandle);
    IRowsetChange *pIRowsetChange = NULL;
    DBROWSTATUS rowStatus = DBROWSTATUS_S_OK;

    hr = pRow->pIRowset->QueryInterface(IID_IRowsetChange, reinterpret_cast<void **>(&pIRowsetChange));
    ExitOnFailure(hr, "Failed to get IRowsetChange interface");

    hr = pIRowsetChange->DeleteRows(DB_NULL_HCHAPTER, 1, &pRow->hRow, &rowStatus);
    ExitOnFailure(hr, "Failed to delete row with status: %u", rowStatus);

    ReleaseNullSceRow(*pRowHandle);

LExit:
    ReleaseObject(pIRowsetChange);

    return hr;
}

extern "C" HRESULT DAPI ScePrepareInsert(
    __in SCE_DATABASE *pDatabase,
    __in DWORD dwTableIndex,
    __out_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE *pRowHandle
    )
{
    HRESULT hr = S_OK;
    SCE_ROW *pRow = NULL;

    pRow = reinterpret_cast<SCE_ROW *>(MemAlloc(sizeof(SCE_ROW), TRUE));
    ExitOnNull(pRow, hr, E_OUTOFMEMORY, "Failed to allocate SCE_ROW struct");

    pRow->pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);
    pRow->hRow = DB_NULL_HROW;
    pRow->pTableSchema = &(pDatabase->pdsSchema->rgTables[dwTableIndex]);
    pRow->pIRowset = pRow->pTableSchema->pIRowset;
    pRow->pIRowset->AddRef();
    pRow->fInserting = TRUE;

    *pRowHandle = reinterpret_cast<SCE_ROW_HANDLE>(pRow);
    pRow = NULL;

LExit:
    ReleaseSceRow(pRow);

    return hr;
}

extern "C" HRESULT DAPI SceFinishUpdate(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowHandle
    )
{
    HRESULT hr = S_OK;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowHandle);
    IAccessor *pIAccessor = NULL;
    IRowsetChange *pIRowsetChange = NULL;
    DBBINDSTATUS *rgBindStatus = NULL;
    HACCESSOR hAccessor = DB_NULL_HACCESSOR;
    HROW hRow = DB_NULL_HROW;

    hr = pRow->pIRowset->QueryInterface(IID_IAccessor, reinterpret_cast<void **>(&pIAccessor));
    ExitOnFailure(hr, "Failed to get IAccessor interface");

// This can be used when stepping through the debugger to see bind failures
#ifdef DEBUG
    if (0 < pRow->dwBindingIndex)
    {
        hr = MemEnsureArraySize(reinterpret_cast<void **>(&rgBindStatus), pRow->dwBindingIndex, sizeof(DBBINDSTATUS), 0);
        ExitOnFailure(hr, "Failed to ensure binding status array size");
    }
#endif

    hr = pIAccessor->CreateAccessor(DBACCESSOR_ROWDATA, pRow->dwBindingIndex, pRow->rgBinding, 0, &hAccessor, rgBindStatus);
    ExitOnFailure(hr, "Failed to create accessor");

    hr = pRow->pIRowset->QueryInterface(IID_IRowsetChange, reinterpret_cast<void **>(&pIRowsetChange));
    ExitOnFailure(hr, "Failed to get IRowsetChange interface");

    if (pRow->fInserting)
    {
        hr = pIRowsetChange->InsertRow(DB_NULL_HCHAPTER, hAccessor, pRow->pbData, &hRow);
        ExitOnFailure(hr, "Failed to insert new row");

        pRow->hRow = hRow;
        ReleaseNullObject(pRow->pIRowset);
        pRow->pIRowset = pRow->pTableSchema->pIRowset;
        pRow->pIRowset->AddRef();
    }
    else
    {
        hr = pIRowsetChange->SetData(pRow->hRow, hAccessor, pRow->pbData);
        ExitOnFailure(hr, "Failed to update existing row");
    }

    if (0 < pRow->pDatabaseInternal->dwTransactionRefcount)
    {
        pRow->pDatabaseInternal->fPendingChanges = TRUE;
    }
    else
    {
        pRow->pDatabaseInternal->fChanges = TRUE;
    }

LExit:
    if (DB_NULL_HACCESSOR != hAccessor)
    {
        pIAccessor->ReleaseAccessor(hAccessor, NULL);
    }
    ReleaseMem(rgBindStatus);
    ReleaseObject(pIAccessor);
    ReleaseObject(pIRowsetChange);

    return hr;
}

extern "C" HRESULT DAPI SceSetColumnBinary(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowHandle,
    __in DWORD dwColumnIndex,
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowHandle);

    hr = ::SizeTMult(sizeof(DBBINDING), pRow->pTableSchema->cColumns, &cbAllocSize);
    ExitOnFailure(hr, "Overflow while calculating allocation size for DBBINDING to set binary, columns: %u", pRow->pTableSchema->cColumns);

    if (NULL == pRow->rgBinding)
    {
        pRow->rgBinding = static_cast<DBBINDING *>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pRow->rgBinding, hr, E_OUTOFMEMORY, "Failed to allocate DBBINDINGs for sce row writer");
    }

    hr = SetColumnValue(pRow->pTableSchema, dwColumnIndex, pbBuffer, cbBuffer, &pRow->rgBinding[pRow->dwBindingIndex++], &pRow->cbOffset, &pRow->pbData);
    ExitOnFailure(hr, "Failed to set column value as binary");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceSetColumnDword(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowHandle,
    __in DWORD dwColumnIndex,
    __in const DWORD dwValue
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowHandle);

    hr = ::SizeTMult(sizeof(DBBINDING), pRow->pTableSchema->cColumns, &cbAllocSize);
    ExitOnFailure(hr, "Overflow while calculating allocation size for DBBINDING to set dword, columns: %u", pRow->pTableSchema->cColumns);

    if (NULL == pRow->rgBinding)
    {
        pRow->rgBinding = static_cast<DBBINDING *>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pRow->rgBinding, hr, E_OUTOFMEMORY, "Failed to allocate DBBINDINGs for sce row writer");
    }

    hr = SetColumnValue(pRow->pTableSchema, dwColumnIndex, reinterpret_cast<const BYTE *>(&dwValue), 4, &pRow->rgBinding[pRow->dwBindingIndex++], &pRow->cbOffset, &pRow->pbData);
    ExitOnFailure(hr, "Failed to set column value as binary");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceSetColumnQword(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowHandle,
    __in DWORD dwColumnIndex,
    __in const DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowHandle);

    hr = ::SizeTMult(sizeof(DBBINDING), pRow->pTableSchema->cColumns, &cbAllocSize);
    ExitOnFailure(hr, "Overflow while calculating allocation size for DBBINDING to set qword, columns: %u", pRow->pTableSchema->cColumns);

    if (NULL == pRow->rgBinding)
    {
        pRow->rgBinding = static_cast<DBBINDING *>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pRow->rgBinding, hr, E_OUTOFMEMORY, "Failed to allocate DBBINDINGs for sce row writer");
    }

    hr = SetColumnValue(pRow->pTableSchema, dwColumnIndex, reinterpret_cast<const BYTE *>(&qwValue), 8, &pRow->rgBinding[pRow->dwBindingIndex++], &pRow->cbOffset, &pRow->pbData);
    ExitOnFailure(hr, "Failed to set column value as qword");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceSetColumnBool(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowHandle,
    __in DWORD dwColumnIndex,
    __in const BOOL fValue
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    short int sValue = fValue ? 0xFFFF : 0x0000;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowHandle);

    hr = ::SizeTMult(sizeof(DBBINDING), pRow->pTableSchema->cColumns, &cbAllocSize);
    ExitOnFailure(hr, "Overflow while calculating allocation size for DBBINDING to set bool, columns: %u", pRow->pTableSchema->cColumns);

    if (NULL == pRow->rgBinding)
    {
        pRow->rgBinding = static_cast<DBBINDING *>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pRow->rgBinding, hr, E_OUTOFMEMORY, "Failed to allocate DBBINDINGs for sce row writer");
    }

    hr = SetColumnValue(pRow->pTableSchema, dwColumnIndex, reinterpret_cast<const BYTE *>(&sValue), 2, &pRow->rgBinding[pRow->dwBindingIndex++], &pRow->cbOffset, &pRow->pbData);
    ExitOnFailure(hr, "Failed to set column value as binary");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceSetColumnString(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowHandle,
    __in DWORD dwColumnIndex,
    __in_z_opt LPCWSTR wzValue
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowHandle);
    SIZE_T cbSize = (NULL == wzValue) ? 0 : ((lstrlenW(wzValue) + 1) * sizeof(WCHAR));

    hr = ::SizeTMult(sizeof(DBBINDING), pRow->pTableSchema->cColumns, &cbAllocSize);
    ExitOnFailure(hr, "Overflow while calculating allocation size for DBBINDING to set string, columns: %u", pRow->pTableSchema->cColumns);

    if (NULL == pRow->rgBinding)
    {
        pRow->rgBinding = static_cast<DBBINDING *>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pRow->rgBinding, hr, E_OUTOFMEMORY, "Failed to allocate DBBINDINGs for sce row writer");
    }

    hr = SetColumnValue(pRow->pTableSchema, dwColumnIndex, reinterpret_cast<const BYTE *>(wzValue), cbSize, &pRow->rgBinding[pRow->dwBindingIndex++], &pRow->cbOffset, &pRow->pbData);
    ExitOnFailure(hr, "Failed to set column value as string: %ls", wzValue);

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceSetColumnNull(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowHandle,
    __in DWORD dwColumnIndex
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowHandle);

    hr = ::SizeTMult(sizeof(DBBINDING), pRow->pTableSchema->cColumns, &cbAllocSize);
    ExitOnFailure(hr, "Overflow while calculating allocation size for DBBINDING to set empty, columns: %u", pRow->pTableSchema->cColumns);

    if (NULL == pRow->rgBinding)
    {
        pRow->rgBinding = static_cast<DBBINDING *>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pRow->rgBinding, hr, E_OUTOFMEMORY, "Failed to allocate DBBINDINGs for sce row writer");
    }

    hr = SetColumnValue(pRow->pTableSchema, dwColumnIndex, NULL, 0, &pRow->rgBinding[pRow->dwBindingIndex++], &pRow->cbOffset, &pRow->pbData);
    ExitOnFailure(hr, "Failed to set column value as empty value");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceSetColumnSystemTime(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowHandle,
    __in DWORD dwColumnIndex,
    __in const SYSTEMTIME *pst
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    DBTIMESTAMP dbTimeStamp = { };

    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowHandle);

    hr = ::SizeTMult(sizeof(DBBINDING), pRow->pTableSchema->cColumns, &cbAllocSize);
    ExitOnFailure(hr, "Overflow while calculating allocation size for DBBINDING to set systemtime, columns: %u", pRow->pTableSchema->cColumns);

    if (NULL == pRow->rgBinding)
    {
        pRow->rgBinding = static_cast<DBBINDING *>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pRow->rgBinding, hr, E_OUTOFMEMORY, "Failed to allocate DBBINDINGs for sce row writer");
    }

    dbTimeStamp.year = pst->wYear;
    dbTimeStamp.month = pst->wMonth;
    dbTimeStamp.day = pst->wDay;
    dbTimeStamp.hour = pst->wHour;
    dbTimeStamp.minute = pst->wMinute;
    dbTimeStamp.second = pst->wSecond;
    // Don't use .fraction because milliseconds are not reliable in SQL CE. They are rounded to the nearest 1/300th of a second,
    // and it is not supported (or at least I can't figure out how) to query for an exact timestamp if milliseconds
    // are involved (even when rounded the way SQL CE returns them).

    hr = SetColumnValue(pRow->pTableSchema, dwColumnIndex, reinterpret_cast<const BYTE *>(&dbTimeStamp), sizeof(dbTimeStamp), &pRow->rgBinding[pRow->dwBindingIndex++], &pRow->cbOffset, &pRow->pbData);
    ExitOnFailure(hr, "Failed to set column value as DBTIMESTAMP");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceGetColumnBinary(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowReadHandle,
    __in DWORD dwColumnIndex,
    __out_opt BYTE **ppbBuffer,
    __inout SIZE_T *pcbBuffer
    )
{
    HRESULT hr = S_OK;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowReadHandle);

    hr = GetColumnValue(pRow, dwColumnIndex, ppbBuffer, pcbBuffer);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get binary data out of column");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceGetColumnDword(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowReadHandle,
    __in DWORD dwColumnIndex,
    __out DWORD *pdwValue
    )
{
    HRESULT hr = S_OK;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowReadHandle);

    hr = GetColumnValueFixed(pRow, dwColumnIndex, 4, reinterpret_cast<BYTE *>(pdwValue));
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get dword data out of column");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceGetColumnQword(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowReadHandle,
    __in DWORD dwColumnIndex,
    __in DWORD64 *pqwValue
    )
{
    HRESULT hr = S_OK;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowReadHandle);

    hr = GetColumnValueFixed(pRow, dwColumnIndex, 8, reinterpret_cast<BYTE *>(pqwValue));
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get qword data out of column");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceGetColumnBool(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowReadHandle,
    __in DWORD dwColumnIndex,
    __out BOOL *pfValue
    )
{
    HRESULT hr = S_OK;
    short int sValue = 0;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowReadHandle);

    hr = GetColumnValueFixed(pRow, dwColumnIndex, 2, reinterpret_cast<BYTE *>(&sValue));
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get data out of column");

    if (sValue == 0x0000)
    {
        *pfValue = FALSE;
    }
    else
    {
        *pfValue = TRUE;
    }

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceGetColumnString(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowReadHandle,
    __in DWORD dwColumnIndex,
    __out_z LPWSTR *psczValue
    )
{
    HRESULT hr = S_OK;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowReadHandle);
    SIZE_T cbSize = 0;

    hr = GetColumnValue(pRow, dwColumnIndex, reinterpret_cast<BYTE **>(psczValue), &cbSize);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get string data out of column");

LExit:
    return hr;
}

extern "C" HRESULT DAPI SceGetColumnSystemTime(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowReadHandle,
    __in DWORD dwColumnIndex,
    __out SYSTEMTIME *pst
    )
{
    HRESULT hr = S_OK;
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowReadHandle);
    DBTIMESTAMP dbTimeStamp = { };

    hr = GetColumnValueFixed(pRow, dwColumnIndex, sizeof(dbTimeStamp), reinterpret_cast<BYTE *>(&dbTimeStamp));
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get string data out of column");

    pst->wYear = dbTimeStamp.year;
    pst->wMonth = dbTimeStamp.month;
    pst->wDay = dbTimeStamp.day;
    pst->wHour = dbTimeStamp.hour;
    pst->wMinute = dbTimeStamp.minute;
    pst->wSecond = dbTimeStamp.second;

LExit:
    return hr;
}

extern "C" void DAPI SceCloseTable(
    __in SCE_TABLE_SCHEMA *pTable
    )
{
    ReleaseNullObject(pTable->pIRowsetChange);
    ReleaseNullObject(pTable->pIRowset);
}

extern "C" BOOL DAPI SceDatabaseChanged(
    __in SCE_DATABASE *pDatabase
    )
{
    SCE_DATABASE_INTERNAL *pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);

    return pDatabaseInternal->fChanges;
}

void DAPI SceResetDatabaseChanged(
    __in SCE_DATABASE *pDatabase
    )
{
    SCE_DATABASE_INTERNAL *pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);

    pDatabaseInternal->fChanges = FALSE;
}

extern "C" HRESULT DAPI SceCloseDatabase(
    __in SCE_DATABASE *pDatabase
    )
{
    HRESULT hr = S_OK;

    ReleaseDatabase(pDatabase);

    return hr;
}

extern "C" HRESULT DAPI SceBeginQuery(
    __in SCE_DATABASE *pDatabase,
    __in DWORD dwTableIndex,
    __in DWORD dwIndex,
    __deref_out_bcount(SCE_QUERY_HANDLE_BYTES) SCE_QUERY_HANDLE *psqhHandle
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    SCE_QUERY *psq = static_cast<SCE_QUERY*>(MemAlloc(sizeof(SCE_QUERY), TRUE));
    ExitOnNull(psq, hr, E_OUTOFMEMORY, "Failed to allocate new sce query");

    psq->pTableSchema = &(pDatabase->pdsSchema->rgTables[dwTableIndex]);
    psq->pIndexSchema = &(psq->pTableSchema->rgIndexes[dwIndex]);
    psq->pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);

    hr = ::SizeTMult(sizeof(DBBINDING), psq->pTableSchema->cColumns, &cbAllocSize);
    ExitOnFailure(hr, "Overflow while calculating allocation size for DBBINDING to begin query, columns: %u", psq->pTableSchema->cColumns);

    psq->rgBinding = static_cast<DBBINDING *>(MemAlloc(cbAllocSize, TRUE));
    ExitOnNull(psq, hr, E_OUTOFMEMORY, "Failed to allocate DBBINDINGs for new sce query");

    *psqhHandle = static_cast<SCE_QUERY_HANDLE>(psq);
    psq = NULL;

LExit:
    ReleaseSceQuery(psq);

    return hr;
}

HRESULT DAPI SceSetQueryColumnBinary(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_HANDLE sqhHandle,
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY *pQuery = reinterpret_cast<SCE_QUERY *>(sqhHandle);

    hr = SetColumnValue(pQuery->pTableSchema, pQuery->pIndexSchema->rgColumns[pQuery->dwBindingIndex], pbBuffer, cbBuffer, &pQuery->rgBinding[pQuery->dwBindingIndex], &pQuery->cbOffset, &pQuery->pbData);
    ExitOnFailure(hr, "Failed to set query column value as binary of size: %u", cbBuffer);

    ++(pQuery->dwBindingIndex);

LExit:
    return hr;
}

HRESULT DAPI SceSetQueryColumnDword(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_HANDLE sqhHandle,
    __in const DWORD dwValue
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY *pQuery = reinterpret_cast<SCE_QUERY *>(sqhHandle);

    hr = SetColumnValue(pQuery->pTableSchema, pQuery->pIndexSchema->rgColumns[pQuery->dwBindingIndex], reinterpret_cast<const BYTE *>(&dwValue), 4, &pQuery->rgBinding[pQuery->dwBindingIndex], &pQuery->cbOffset, &pQuery->pbData);
    ExitOnFailure(hr, "Failed to set query column value as dword");

    ++(pQuery->dwBindingIndex);

LExit:
    return hr;
}

HRESULT DAPI SceSetQueryColumnQword(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_HANDLE sqhHandle,
    __in const DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY *pQuery = reinterpret_cast<SCE_QUERY *>(sqhHandle);

    hr = SetColumnValue(pQuery->pTableSchema, pQuery->pIndexSchema->rgColumns[pQuery->dwBindingIndex], reinterpret_cast<const BYTE *>(&qwValue), 8, &pQuery->rgBinding[pQuery->dwBindingIndex], &pQuery->cbOffset, &pQuery->pbData);
    ExitOnFailure(hr, "Failed to set query column value as qword");

    ++(pQuery->dwBindingIndex);

LExit:
    return hr;
}

HRESULT DAPI SceSetQueryColumnBool(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_HANDLE sqhHandle,
    __in const BOOL fValue
    )
{
    HRESULT hr = S_OK;
    short int sValue = fValue ? 1 : 0;
    SCE_QUERY *pQuery = reinterpret_cast<SCE_QUERY *>(sqhHandle);

    hr = SetColumnValue(pQuery->pTableSchema, pQuery->pIndexSchema->rgColumns[pQuery->dwBindingIndex], reinterpret_cast<const BYTE *>(&sValue), 2, &pQuery->rgBinding[pQuery->dwBindingIndex], &pQuery->cbOffset, &pQuery->pbData);
    ExitOnFailure(hr, "Failed to set query column value as boolean");

    ++(pQuery->dwBindingIndex);

LExit:
    return hr;
}

HRESULT DAPI SceSetQueryColumnString(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_HANDLE sqhHandle,
    __in_z_opt LPCWSTR wzString
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY *pQuery = reinterpret_cast<SCE_QUERY *>(sqhHandle);
    SIZE_T cbSize = (NULL == wzString) ? 0 : ((lstrlenW(wzString) + 1) * sizeof(WCHAR));

    hr = SetColumnValue(pQuery->pTableSchema, pQuery->pIndexSchema->rgColumns[pQuery->dwBindingIndex], reinterpret_cast<const BYTE *>(wzString), cbSize, &pQuery->rgBinding[pQuery->dwBindingIndex], &pQuery->cbOffset, &pQuery->pbData);
    ExitOnFailure(hr, "Failed to set query column value as string");

    ++(pQuery->dwBindingIndex);

LExit:
    return hr;
}

HRESULT DAPI SceSetQueryColumnSystemTime(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_HANDLE sqhHandle,
    __in const SYSTEMTIME *pst
    )
{
    HRESULT hr = S_OK;
    DBTIMESTAMP dbTimeStamp = { };
    SCE_QUERY *pQuery = reinterpret_cast<SCE_QUERY *>(sqhHandle);

    dbTimeStamp.year = pst->wYear;
    dbTimeStamp.month = pst->wMonth;
    dbTimeStamp.day = pst->wDay;
    dbTimeStamp.hour = pst->wHour;
    dbTimeStamp.minute = pst->wMinute;
    dbTimeStamp.second = pst->wSecond;

    hr = SetColumnValue(pQuery->pTableSchema, pQuery->pIndexSchema->rgColumns[pQuery->dwBindingIndex], reinterpret_cast<const BYTE *>(&dbTimeStamp), sizeof(dbTimeStamp), &pQuery->rgBinding[pQuery->dwBindingIndex], &pQuery->cbOffset, &pQuery->pbData);
    ExitOnFailure(hr, "Failed to set query column value as DBTIMESTAMP");

    ++(pQuery->dwBindingIndex);

LExit:
    return hr;
}

HRESULT DAPI SceSetQueryColumnEmpty(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_HANDLE sqhHandle
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY *pQuery = reinterpret_cast<SCE_QUERY *>(sqhHandle);

    hr = SetColumnValue(pQuery->pTableSchema, pQuery->pIndexSchema->rgColumns[pQuery->dwBindingIndex], NULL, 0, &pQuery->rgBinding[pQuery->dwBindingIndex], &pQuery->cbOffset, &pQuery->pbData);
    ExitOnFailure(hr, "Failed to set query column value as empty value");

    ++(pQuery->dwBindingIndex);

LExit:
    return hr;
}

HRESULT DAPI SceRunQueryExact(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_HANDLE *psqhHandle,
    __out_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE *pRowHandle
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_RESULTS *pQueryResults = NULL;

    hr = RunQuery(FALSE, *psqhHandle, &pQueryResults);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to run query exact");

    hr = SceGetNextResultRow(reinterpret_cast<SCE_QUERY_RESULTS_HANDLE>(pQueryResults), pRowHandle);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get next row out of results");

LExit:
    ReleaseNullSceQuery(*psqhHandle);
    ReleaseSceQueryResults(pQueryResults);

    return hr;
}

extern "C" HRESULT DAPI SceRunQueryRange(
    __in_bcount(SCE_QUERY_BYTES) SCE_QUERY_HANDLE *psqhHandle,
    __deref_out_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_RESULTS_HANDLE *psqrhHandle
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_RESULTS **ppQueryResults = reinterpret_cast<SCE_QUERY_RESULTS **>(psqrhHandle);

    hr = RunQuery(TRUE, *psqhHandle, ppQueryResults);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to run query for range");

LExit:
    ReleaseNullSceQuery(*psqhHandle);

    return hr;
}

extern "C" HRESULT DAPI SceGetNextResultRow(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_RESULTS_HANDLE sqrhHandle,
    __out_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE *pRowHandle
    )
{
    HRESULT hr = S_OK;
    HROW hRow = DB_NULL_HROW;
    HROW *phRow = &hRow;
    DBCOUNTITEM cRowsObtained = 0;
    SCE_ROW *pRow = NULL;
    SCE_QUERY_RESULTS *pQueryResults = reinterpret_cast<SCE_QUERY_RESULTS *>(sqrhHandle);

    Assert(pRowHandle && (*pRowHandle == NULL));

    hr = pQueryResults->pIRowset->GetNextRows(DB_NULL_HCHAPTER, 0, 1, &cRowsObtained, &phRow);
    if (DB_S_ENDOFROWSET == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    ExitOnFailure(hr, "Failed to get next first row");

    pRow = reinterpret_cast<SCE_ROW *>(MemAlloc(sizeof(SCE_ROW), TRUE));
    ExitOnNull(pRow, hr, E_OUTOFMEMORY, "Failed to allocate SCE_ROW struct");

    pRow->pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pQueryResults->pDatabaseInternal);
    pRow->hRow = hRow;
    pRow->pTableSchema = pQueryResults->pTableSchema;
    pRow->pIRowset = pQueryResults->pIRowset;
    pRow->pIRowset->AddRef();

    *pRowHandle = reinterpret_cast<SCE_ROW_HANDLE>(pRow);
    pRow = NULL;
    hRow = DB_NULL_HROW;

LExit:
    if (DB_NULL_HROW != hRow)
    {
        pQueryResults->pIRowset->ReleaseRows(1, &hRow, NULL, NULL, NULL);
    }
    ReleaseSceRow(pRow);

    return hr;
}

extern "C" void DAPI SceFreeRow(
    __in_bcount(SCE_ROW_HANDLE_BYTES) SCE_ROW_HANDLE rowHandle
    )
{
    SCE_ROW *pRow = reinterpret_cast<SCE_ROW *>(rowHandle);

    if (DB_NULL_HROW != pRow->hRow)
    {
        pRow->pIRowset->ReleaseRows(1, &pRow->hRow, NULL, NULL, NULL);
    }
    ReleaseObject(pRow->pIRowset);
    ReleaseMem(pRow->rgBinding);
    ReleaseMem(pRow->pbData);
    ReleaseMem(pRow);
}

void DAPI SceFreeQuery(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_HANDLE sqhHandle
    )
{
    SCE_QUERY *pQuery = reinterpret_cast<SCE_QUERY *>(sqhHandle);

    ReleaseMem(pQuery->rgBinding);
    ReleaseMem(pQuery->pbData);
    ReleaseMem(pQuery);
}

void DAPI SceFreeQueryResults(
    __in_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_RESULTS_HANDLE sqrhHandle
    )
{
    SCE_QUERY_RESULTS *pQueryResults = reinterpret_cast<SCE_QUERY_RESULTS *>(sqrhHandle);

    ReleaseObject(pQueryResults->pIRowset);
    ReleaseMem(pQueryResults);
}

// internal function definitions
static HRESULT CreateSqlCe(
    __in_z_opt LPCWSTR wzSqlCeDllPath,
    __out IDBInitialize **ppIDBInitialize,
    __out_opt HMODULE *phSqlCeDll
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;
    LPWSTR sczDirectory = NULL;
    LPWSTR sczDllFullPath = NULL;

    if (NULL == wzSqlCeDllPath)
    {
        hr = CoCreateInstance(CLSID_SQLSERVERCE, 0, CLSCTX_INPROC_SERVER, IID_IDBInitialize, reinterpret_cast<void **>(ppIDBInitialize));
        ExitOnFailure(hr, "Failed to get IDBInitialize interface");
    }
    else
    {
        // First try loading DLL from the path of our EXE
        hr = PathForCurrentProcess(&sczPath, NULL);
        ExitOnFailure(hr, "Failed to get path for current process");

        hr = PathGetDirectory(sczPath, &sczDirectory);
        ExitOnFailure(hr, "Failed to get directory of current process");

        hr = PathConcat(sczDirectory, wzSqlCeDllPath, &sczDllFullPath);
        ExitOnFailure(hr, "Failed to concatenate current directory and DLL filename");

        *phSqlCeDll = ::LoadLibraryW(sczDllFullPath);

        // If that failed, fallback to loading from current path
        if (NULL == *phSqlCeDll)
        {
            hr = DirGetCurrent(&sczDirectory);
            ExitOnFailure(hr, "Failed to get current directory");

            hr = PathConcat(sczDirectory, wzSqlCeDllPath, &sczDllFullPath);
            ExitOnFailure(hr, "Failed to concatenate current directory and DLL filename");

            *phSqlCeDll = ::LoadLibraryW(sczDllFullPath);
            ExitOnNullWithLastError(*phSqlCeDll, hr, "Failed to open Sql CE DLL: %ls", sczDllFullPath);
        }

        HRESULT (WINAPI *pfnGetFactory)(REFCLSID, REFIID, void**);
        pfnGetFactory = (HRESULT (WINAPI *)(REFCLSID, REFIID, void**))GetProcAddress(*phSqlCeDll, "DllGetClassObject");

        IClassFactory* pFactory = NULL;
        hr = pfnGetFactory(CLSID_SQLSERVERCE, IID_IClassFactory, (void**)&pFactory);
        ExitOnFailure(hr, "Failed to get factory for IID_IDBInitialize from DLL: %ls", sczDllFullPath);
        ExitOnNull(pFactory, hr, E_UNEXPECTED, "GetFactory returned success, but pFactory was NULL");

        hr = pFactory->CreateInstance(NULL, IID_IDBInitialize, (void**)ppIDBInitialize);
        pFactory->Release();
    }

LExit:
    ReleaseStr(sczPath);
    ReleaseStr(sczDirectory);
    ReleaseStr(sczDllFullPath);

    return hr;
}

static HRESULT RunQuery(
    __in BOOL fRange,
    __in_bcount(SCE_QUERY_BYTES) SCE_QUERY_HANDLE psqhHandle,
    __deref_out_bcount(SCE_QUERY_RESULTS_BYTES) SCE_QUERY_RESULTS **ppQueryResults
    )
{
    HRESULT hr = S_OK;
    DBID tableID = { };
    DBID indexID = { };
    IAccessor *pIAccessor = NULL;
    IRowsetIndex *pIRowsetIndex = NULL;
    IRowset *pIRowset = NULL;
    HACCESSOR hAccessor = DB_NULL_HACCESSOR;
    SCE_QUERY *pQuery = reinterpret_cast<SCE_QUERY *>(psqhHandle);
    SCE_QUERY_RESULTS *pQueryResults = NULL;
    DBPROPSET rgdbpIndexPropSet[1];
    DBPROP rgdbpIndexProp[1];

    rgdbpIndexPropSet[0].cProperties     = 1;
    rgdbpIndexPropSet[0].guidPropertySet = DBPROPSET_ROWSET;
    rgdbpIndexPropSet[0].rgProperties    = rgdbpIndexProp;

    rgdbpIndexProp[0].dwPropertyID       = DBPROP_IRowsetIndex;
    rgdbpIndexProp[0].dwOptions          = DBPROPOPTIONS_REQUIRED;
    rgdbpIndexProp[0].colid              = DB_NULLID;
    rgdbpIndexProp[0].vValue.vt          = VT_BOOL;
    rgdbpIndexProp[0].vValue.boolVal     = VARIANT_TRUE;

    tableID.eKind = DBKIND_NAME;
    tableID.uName.pwszName = const_cast<WCHAR *>(pQuery->pTableSchema->wzName);

    indexID.eKind = DBKIND_NAME;
    indexID.uName.pwszName = const_cast<WCHAR *>(pQuery->pIndexSchema->wzName);

    hr = pQuery->pDatabaseInternal->pIOpenRowset->OpenRowset(NULL, &tableID, &indexID, IID_IRowsetIndex, _countof(rgdbpIndexPropSet), rgdbpIndexPropSet, (IUnknown**) &pIRowsetIndex);
    ExitOnFailure(hr, "Failed to open IRowsetIndex");

    hr = pIRowsetIndex->QueryInterface(IID_IRowset, reinterpret_cast<void **>(&pIRowset));
    ExitOnFailure(hr, "Failed to get IRowset interface from IRowsetIndex");

    hr = pIRowset->QueryInterface(IID_IAccessor, reinterpret_cast<void **>(&pIAccessor));
    ExitOnFailure(hr, "Failed to get IAccessor interface");

    hr = pIAccessor->CreateAccessor(DBACCESSOR_ROWDATA, pQuery->dwBindingIndex, pQuery->rgBinding, 0, &hAccessor, NULL);
    ExitOnFailure(hr, "Failed to create accessor");

    if (!fRange)
    {
        hr = pIRowsetIndex->Seek(hAccessor, pQuery->dwBindingIndex, pQuery->pbData, DBSEEK_FIRSTEQ);
        if (DB_E_NOTFOUND == hr)
        {
            ExitFunction1(hr = E_NOTFOUND);
        }
        ExitOnFailure(hr, "Failed to seek to record");
    }
    else
    {
        // If ALL columns in the index were specified, do a full key match
        if (pQuery->dwBindingIndex == pQuery->pIndexSchema->cColumns)
        {
            hr = pIRowsetIndex->SetRange(hAccessor, pQuery->dwBindingIndex, pQuery->pbData, 0, NULL, DBRANGE_MATCH);
        }
        else
        {
            // Otherwise, just match the specified keys.
            // We really want to use DBRANGE_MATCH_N_SHIFT here, but SQL CE doesn't appear to support it
            // So instead, we set the start and end to the same partial key, and then allow inclusive matching
            // This appears to accomplish the same thing
            hr = pIRowsetIndex->SetRange(hAccessor, pQuery->dwBindingIndex, pQuery->pbData, pQuery->dwBindingIndex, pQuery->pbData, 0);
        }
        if (DB_E_NOTFOUND == hr || E_NOTFOUND == hr)
        {
            ExitFunction1(hr = E_NOTFOUND);
        }
        ExitOnFailure(hr, "Failed to set range to find records");
    }

    pQueryResults = reinterpret_cast<SCE_QUERY_RESULTS *>(MemAlloc(sizeof(SCE_QUERY_RESULTS), TRUE));
    ExitOnNull(pQueryResults, hr, E_OUTOFMEMORY, "Failed to allocate query results struct");

    pQueryResults->pDatabaseInternal = pQuery->pDatabaseInternal;
    pQueryResults->pTableSchema = pQuery->pTableSchema;
    pQueryResults->pIRowset = pIRowset;
    pIRowset = NULL;

    *ppQueryResults = pQueryResults;
    pQueryResults = NULL;

LExit:
    if (DB_NULL_HACCESSOR != hAccessor)
    {
        pIAccessor->ReleaseAccessor(hAccessor, NULL);
    }
    ReleaseObject(pIAccessor);
    ReleaseObject(pIRowset);
    ReleaseObject(pIRowsetIndex);
    ReleaseMem(pQueryResults);
    ReleaseSceQueryResults(pQueryResults);

    return hr;
}

static HRESULT FillOutColumnDescFromSchema(
    __in const SCE_COLUMN_SCHEMA *pColumnSchema,
    __out DBCOLUMNDESC *pColumnDesc
    )
{
    HRESULT hr = S_OK;
    DWORD dwColumnProperties = 0;
    DWORD dwColumnPropertyIndex = 0;
    BOOL fFixedSize = FALSE;

    pColumnDesc->dbcid.eKind = DBKIND_NAME;
    pColumnDesc->dbcid.uName.pwszName = (WCHAR *)pColumnSchema->wzName;
    pColumnDesc->wType = pColumnSchema->dbtColumnType;
    pColumnDesc->ulColumnSize = pColumnSchema->dwLength;
    if (0 == pColumnDesc->ulColumnSize && (DBTYPE_WSTR == pColumnDesc->wType || DBTYPE_BYTES == pColumnDesc->wType))
    {
        fFixedSize = FALSE;
    }
    else
    {
        fFixedSize = TRUE;
    }

    dwColumnProperties = 1;
    if (pColumnSchema->fAutoIncrement)
    {
        ++dwColumnProperties;
    }
    if (!pColumnSchema->fNullable)
    {
        ++dwColumnProperties;
    }

    if (0 < dwColumnProperties)
    {
        pColumnDesc->cPropertySets = 1;
        pColumnDesc->rgPropertySets = reinterpret_cast<DBPROPSET *>(MemAlloc(sizeof(DBPROPSET), TRUE));
        ExitOnNull(pColumnDesc->rgPropertySets, hr, E_OUTOFMEMORY, "Failed to allocate propset object while setting up column parameters");

        pColumnDesc->rgPropertySets[0].cProperties = dwColumnProperties;
        pColumnDesc->rgPropertySets[0].guidPropertySet = DBPROPSET_COLUMN;
        pColumnDesc->rgPropertySets[0].rgProperties = reinterpret_cast<DBPROP *>(MemAlloc(sizeof(DBPROP) * dwColumnProperties, TRUE));

        dwColumnPropertyIndex = 0;
        if (pColumnSchema->fAutoIncrement)
        {
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].dwPropertyID = DBPROP_COL_AUTOINCREMENT;
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].dwOptions = DBPROPOPTIONS_REQUIRED;
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].colid = DB_NULLID;
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].vValue.vt = VT_BOOL;
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].vValue.boolVal = VARIANT_TRUE;
            ++dwColumnPropertyIndex;
        }
        if (!pColumnSchema->fNullable)
        {
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].dwPropertyID = DBPROP_COL_NULLABLE;
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].dwOptions = DBPROPOPTIONS_REQUIRED;
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].colid = DB_NULLID;
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].vValue.vt = VT_BOOL;
            pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].vValue.boolVal = VARIANT_FALSE;
            ++dwColumnPropertyIndex;
        }

        pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].dwPropertyID = DBPROP_COL_FIXEDLENGTH;
        pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].dwOptions = DBPROPOPTIONS_REQUIRED;
        pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].colid = DB_NULLID;
        pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].vValue.vt = VT_BOOL;
        pColumnDesc->rgPropertySets[0].rgProperties[dwColumnPropertyIndex].vValue.boolVal = fFixedSize ? VARIANT_TRUE : VARIANT_FALSE;
        ++dwColumnPropertyIndex;
    }

LExit:
    return hr;
}

static HRESULT EnsureSchema(
    __in SCE_DATABASE *pDatabase,
    __in SCE_DATABASE_SCHEMA *pdsSchema
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    BOOL fInTransaction = FALSE;
    BOOL fSchemaNeedsSetup = TRUE;
    DBID tableID = { };
    DBID indexID = { };
    DBPROPSET rgdbpIndexPropSet[1];
    DBPROPSET rgdbpRowSetPropSet[1];
    DBPROP rgdbpIndexProp[1];
    DBPROP rgdbpRowSetProp[1];
    DBCOLUMNDESC *rgColumnDescriptions = NULL;
    DBINDEXCOLUMNDESC *rgIndexColumnDescriptions = NULL;
    DWORD cIndexColumnDescriptions = 0;
    DWORD dwTableColumnIndex = 0;
    SCE_DATABASE_INTERNAL *pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);
    ITableDefinition *pTableDefinition = NULL;
    IIndexDefinition *pIndexDefinition = NULL;
    IRowsetIndex *pIRowsetIndex = NULL;

    rgdbpRowSetPropSet[0].cProperties = 1;
    rgdbpRowSetPropSet[0].guidPropertySet = DBPROPSET_ROWSET;
    rgdbpRowSetPropSet[0].rgProperties = rgdbpRowSetProp;

    rgdbpRowSetProp[0].dwPropertyID = DBPROP_IRowsetChange;
    rgdbpRowSetProp[0].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpRowSetProp[0].colid = DB_NULLID;
    rgdbpRowSetProp[0].vValue.vt = VT_BOOL;
    rgdbpRowSetProp[0].vValue.boolVal = VARIANT_TRUE;

    rgdbpIndexPropSet[0].cProperties = 1;
    rgdbpIndexPropSet[0].guidPropertySet = DBPROPSET_INDEX;
    rgdbpIndexPropSet[0].rgProperties = rgdbpIndexProp;

    rgdbpIndexProp[0].dwPropertyID = DBPROP_INDEX_NULLS;
    rgdbpIndexProp[0].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpIndexProp[0].colid = DB_NULLID;
    rgdbpIndexProp[0].vValue.vt = VT_I4;
    rgdbpIndexProp[0].vValue.lVal = DBPROPVAL_IN_DISALLOWNULL;

    hr = pDatabaseInternal->pISessionProperties->QueryInterface(IID_ITableDefinition, reinterpret_cast<void **>(&pTableDefinition));
    ExitOnFailure(hr, "Failed to get ITableDefinition for table creation");

    hr = pDatabaseInternal->pISessionProperties->QueryInterface(IID_IIndexDefinition, reinterpret_cast<void **>(&pIndexDefinition));
    ExitOnFailure(hr, "Failed to get IIndexDefinition for index creation");

    hr = SceBeginTransaction(pDatabase);
    ExitOnFailure(hr, "Failed to start transaction for ensuring schema");
    fInTransaction = TRUE;

    for (DWORD dwTable = 0; dwTable < pdsSchema->cTables; ++dwTable)
    {
        tableID.eKind = DBKIND_NAME;
        tableID.uName.pwszName = const_cast<WCHAR *>(pdsSchema->rgTables[dwTable].wzName);

        // Fill out each column description struct as appropriate, to be used for creating the table, or confirming the table's columns all exist
        rgColumnDescriptions = static_cast<DBCOLUMNDESC *>(MemAlloc(sizeof(DBCOLUMNDESC) * pdsSchema->rgTables[dwTable].cColumns, TRUE));
        ExitOnNull(rgColumnDescriptions, hr, E_OUTOFMEMORY, "Failed to allocate column description array while creating table");

        for (DWORD i = 0; i < pdsSchema->rgTables[dwTable].cColumns; ++i)
        {
            hr = FillOutColumnDescFromSchema(pdsSchema->rgTables[dwTable].rgColumns + i, rgColumnDescriptions + i);
            ExitOnFailure(hr, "Failed to fill out column description from schema");
        }

        // First try to open the table - or if it doesn't exist, create it
        hr = pDatabaseInternal->pIOpenRowset->OpenRowset(NULL, &tableID, NULL, IID_IRowset, _countof(rgdbpRowSetPropSet), rgdbpRowSetPropSet, reinterpret_cast<IUnknown **>(&pdsSchema->rgTables[dwTable].pIRowset));
        if (DB_E_NOTABLE == hr)
        {
            // The table doesn't exist, so let's create it
            hr = pTableDefinition->CreateTable(NULL, &tableID, pdsSchema->rgTables[dwTable].cColumns, rgColumnDescriptions, IID_IUnknown, _countof(rgdbpRowSetPropSet), rgdbpRowSetPropSet, NULL, NULL);
            ExitOnFailure(hr, "Failed to create table: %ls", pdsSchema->rgTables[dwTable].wzName);
        }
        else
        {
            ExitOnFailure(hr, "Failed to open table %ls while ensuring schema", tableID.uName.pwszName);

            // Close any rowset we opened
            ReleaseNullObject(pdsSchema->rgTables[dwTable].pIRowset);

            // If it does exist, make sure all columns are in the table
            // Only nullable columns can be added to an existing table
            for (DWORD i = 1; i < pdsSchema->rgTables[dwTable].cColumns; ++i)
            {
                if (pdsSchema->rgTables[dwTable].rgColumns[i].fNullable)
                hr = pTableDefinition->AddColumn(&tableID, rgColumnDescriptions + i, NULL);
                if (DB_E_DUPLICATECOLUMNID == hr)
                {
                    hr = S_OK;
                }
                ExitOnFailure(hr, "Failed to add column %ls", pdsSchema->rgTables[dwTable].rgColumns[i].wzName);
            }
        }

#pragma prefast(push)
#pragma prefast(disable:26010)
        hr = EnsureLocalColumnConstraints(pTableDefinition, &tableID, pdsSchema->rgTables + dwTable);
#pragma prefast(pop)
        ExitOnFailure(hr, "Failed to ensure local column constraints for table: %ls", pdsSchema->rgTables[dwTable].wzName);

        for (DWORD i = 0; i < pdsSchema->rgTables[dwTable].cColumns; ++i)
        {
            if (NULL != rgColumnDescriptions[i].rgPropertySets)
            {
                ReleaseMem(rgColumnDescriptions[i].rgPropertySets[0].rgProperties);
                ReleaseMem(rgColumnDescriptions[i].rgPropertySets);
            }
        }

        ReleaseNullMem(rgColumnDescriptions);
        if (0 < pdsSchema->rgTables[dwTable].cIndexes)
        {
            // Now create indexes for the table
            for (DWORD dwIndex = 0; dwIndex < pdsSchema->rgTables[dwTable].cIndexes; ++dwIndex)
            {
                indexID.eKind = DBKIND_NAME;
                indexID.uName.pwszName = pdsSchema->rgTables[dwTable].rgIndexes[dwIndex].wzName;

                // Check if the index exists
                hr = pDatabaseInternal->pIOpenRowset->OpenRowset(NULL, &tableID, &indexID, IID_IRowsetIndex, 0, NULL, (IUnknown**) &pIRowsetIndex);
                if (SUCCEEDED(hr))
                {
                    // TODO: If one with the same name exists, check if the schema actually matches
                    ReleaseNullObject(pIRowsetIndex);
                    continue;
                }
                hr = S_OK;

                hr = ::SizeTMult(sizeof(DBINDEXCOLUMNDESC), pdsSchema->rgTables[dwTable].rgIndexes[dwIndex].cColumns, &cbAllocSize);
                ExitOnFailure(hr, "Overflow while calculating allocation size for DBINDEXCOLUMNDESC, columns: %u", pdsSchema->rgTables[dwTable].rgIndexes[dwIndex].cColumns);

                rgIndexColumnDescriptions = reinterpret_cast<DBINDEXCOLUMNDESC *>(MemAlloc(cbAllocSize, TRUE));
                ExitOnNull(rgIndexColumnDescriptions, hr, E_OUTOFMEMORY, "Failed to allocate structure to hold index column descriptions");
                cIndexColumnDescriptions = pdsSchema->rgTables[dwTable].rgIndexes[dwIndex].cColumns;

                for (DWORD dwColumnIndex = 0; dwColumnIndex < cIndexColumnDescriptions; ++dwColumnIndex)
                {
                    dwTableColumnIndex = pdsSchema->rgTables[dwTable].rgIndexes[dwIndex].rgColumns[dwColumnIndex];

                    rgIndexColumnDescriptions[dwColumnIndex].pColumnID = reinterpret_cast<DBID *>(MemAlloc(sizeof(DBID), TRUE));
                    rgIndexColumnDescriptions[dwColumnIndex].pColumnID->eKind = DBKIND_NAME;
                    rgIndexColumnDescriptions[dwColumnIndex].pColumnID->uName.pwszName = const_cast<LPOLESTR>(pdsSchema->rgTables[dwTable].rgColumns[dwTableColumnIndex].wzName);
                    rgIndexColumnDescriptions[dwColumnIndex].eIndexColOrder = pdsSchema->rgTables[dwTable].rgColumns[dwTableColumnIndex].fDescending ? DBINDEX_COL_ORDER_DESC  : DBINDEX_COL_ORDER_ASC;
                }

                hr = pIndexDefinition->CreateIndex(&tableID, &indexID, static_cast<DBORDINAL>(pdsSchema->rgTables[dwTable].rgIndexes[dwIndex].cColumns), rgIndexColumnDescriptions, 1, rgdbpIndexPropSet, NULL);
                if (DB_E_DUPLICATEINDEXID == hr)
                {
                    // If the index already exists, no worries
                    hr = S_OK;
                }
                ExitOnFailure(hr, "Failed to create index named %ls into table named %ls", pdsSchema->rgTables[dwTable].rgIndexes[dwIndex].wzName, pdsSchema->rgTables[dwTable].wzName);

                for (DWORD i = 0; i < cIndexColumnDescriptions; ++i)
                {
                    ReleaseMem(rgIndexColumnDescriptions[i].pColumnID);
                }

                cIndexColumnDescriptions = 0;
                ReleaseNullMem(rgIndexColumnDescriptions);
            }
        }
    }

    // Now once all tables have been created, create foreign key relationships
    if (fSchemaNeedsSetup)
    {
        for (DWORD dwTable = 0; dwTable < pdsSchema->cTables; ++dwTable)
        {
            tableID.eKind = DBKIND_NAME;
            tableID.uName.pwszName = const_cast<WCHAR *>(pdsSchema->rgTables[dwTable].wzName);

            // Setup any constraints for the table's columns
            hr = EnsureForeignColumnConstraints(pTableDefinition, &tableID, pdsSchema->rgTables + dwTable, pdsSchema);
            ExitOnFailure(hr, "Failed to ensure foreign column constraints for table: %ls", pdsSchema->rgTables[dwTable].wzName);
        }
    }

    hr = SceCommitTransaction(pDatabase);
    ExitOnFailure(hr, "Failed to commit transaction for ensuring schema");
    fInTransaction = FALSE;

    hr = OpenSchema(pDatabase, pdsSchema);
    ExitOnFailure(hr, "Failed to open schema");

LExit:
    ReleaseObject(pTableDefinition);
    ReleaseObject(pIndexDefinition);
    ReleaseObject(pIRowsetIndex);

    if (fInTransaction)
    {
        SceRollbackTransaction(pDatabase);
    }

    for (DWORD i = 0; i < cIndexColumnDescriptions; ++i)
    {
        ReleaseMem(rgIndexColumnDescriptions[i].pColumnID);
    }

    ReleaseMem(rgIndexColumnDescriptions);
    ReleaseMem(rgColumnDescriptions);

    return hr;
}

static HRESULT OpenSchema(
    __in SCE_DATABASE *pDatabase,
    __in SCE_DATABASE_SCHEMA *pdsSchema
    )
{
    HRESULT hr = S_OK;
    SCE_DATABASE_INTERNAL *pDatabaseInternal = reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle);
    DBID tableID = { };
    DBPROPSET rgdbpRowSetPropSet[1];
    DBPROP rgdbpRowSetProp[1];

    rgdbpRowSetPropSet[0].cProperties = 1;
    rgdbpRowSetPropSet[0].guidPropertySet = DBPROPSET_ROWSET;
    rgdbpRowSetPropSet[0].rgProperties = rgdbpRowSetProp;

    rgdbpRowSetProp[0].dwPropertyID = DBPROP_IRowsetChange;
    rgdbpRowSetProp[0].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpRowSetProp[0].colid = DB_NULLID;
    rgdbpRowSetProp[0].vValue.vt = VT_BOOL;
    rgdbpRowSetProp[0].vValue.boolVal = VARIANT_TRUE;

    // Finally, open all tables
    for (DWORD dwTable = 0; dwTable < pdsSchema->cTables; ++dwTable)
    {
        tableID.eKind = DBKIND_NAME;
        tableID.uName.pwszName = const_cast<WCHAR *>(pdsSchema->rgTables[dwTable].wzName);

        // And finally, open the table's standard interfaces
        hr = pDatabaseInternal->pIOpenRowset->OpenRowset(NULL, &tableID, NULL, IID_IRowset, _countof(rgdbpRowSetPropSet), rgdbpRowSetPropSet, reinterpret_cast<IUnknown **>(&pdsSchema->rgTables[dwTable].pIRowset));
        ExitOnFailure(hr, "Failed to open table %u named %ls after ensuring all indexes and constraints are created", dwTable, pdsSchema->rgTables[dwTable].wzName);

        hr = pdsSchema->rgTables[dwTable].pIRowset->QueryInterface(IID_IRowsetChange, reinterpret_cast<void **>(&pdsSchema->rgTables[dwTable].pIRowsetChange));
        ExitOnFailure(hr, "Failed to get IRowsetChange object for table: %ls", pdsSchema->rgTables[dwTable].wzName);
    }

LExit:
    return hr;
}

static HRESULT SetColumnValue(
    __in const SCE_TABLE_SCHEMA *pTableSchema,
    __in DWORD dwColumnIndex,
    __in_bcount_opt(cbSize) const BYTE *pbData,
    __in SIZE_T cbSize,
    __inout DBBINDING *pBinding,
    __inout SIZE_T *pcbOffset,
    __inout BYTE **ppbBuffer
    )
{
    HRESULT hr = S_OK;
    size_t cbNewOffset = *pcbOffset;

    pBinding->iOrdinal = dwColumnIndex + 1; // Skip bookmark column
    pBinding->dwMemOwner = DBMEMOWNER_CLIENTOWNED;
    pBinding->dwPart = DBPART_VALUE | DBPART_LENGTH | DBPART_STATUS;

    pBinding->obLength = cbNewOffset;

    hr = ::SizeTAdd(cbNewOffset, sizeof(DBBYTEOFFSET), &cbNewOffset);
    ExitOnFailure(hr, "Failed to add sizeof(DBBYTEOFFSET) to alloc size while setting column value");

    pBinding->obValue = cbNewOffset;

    hr = ::SizeTAdd(cbNewOffset, cbSize, &cbNewOffset);
    ExitOnFailure(hr, "Failed to add %u to alloc size while setting column value", cbSize);

    pBinding->obStatus = cbNewOffset;
    pBinding->eParamIO = DBPARAMIO_INPUT;

    hr = ::SizeTAdd(cbNewOffset, sizeof(DBSTATUS), &cbNewOffset);
    ExitOnFailure(hr, "Failed to add sizeof(DBSTATUS) to alloc size while setting column value");

    pBinding->wType = pTableSchema->rgColumns[dwColumnIndex].dbtColumnType;
    pBinding->cbMaxLen = static_cast<DBBYTEOFFSET>(cbSize);

    if (NULL == *ppbBuffer)
    {
        *ppbBuffer = reinterpret_cast<BYTE *>(MemAlloc(cbNewOffset, TRUE));
        ExitOnNull(*ppbBuffer, hr, E_OUTOFMEMORY, "Failed to allocate buffer while setting row string");
    }
    else
    {
        *ppbBuffer = reinterpret_cast<BYTE *>(MemReAlloc(*ppbBuffer, cbNewOffset, TRUE));
        ExitOnNull(*ppbBuffer, hr, E_OUTOFMEMORY, "Failed to reallocate buffer while setting row string");
    }

    *(reinterpret_cast<DBBYTEOFFSET *>(*ppbBuffer + *pcbOffset)) = static_cast<DBBYTEOFFSET>(cbSize);
    *pcbOffset += sizeof(DBBYTEOFFSET);
    memcpy(*ppbBuffer + *pcbOffset, pbData, cbSize);
    *pcbOffset += cbSize;
    if (NULL == pbData)
    {
        *(reinterpret_cast<DBSTATUS *>(*ppbBuffer + *pcbOffset)) = DBSTATUS_S_ISNULL;
    }
    *pcbOffset += sizeof(DBSTATUS);

LExit:
    return hr;
}

static HRESULT GetColumnValue(
    __in SCE_ROW *pRow,
    __in DWORD dwColumnIndex,
    __out_opt BYTE **ppbData,
    __out SIZE_T *pcbSize
    )
{
    HRESULT hr = S_OK;
    const SCE_TABLE_SCHEMA *pTable = pRow->pTableSchema;
    IAccessor *pIAccessor = NULL;
    HACCESSOR hAccessorLength = DB_NULL_HACCESSOR;
    HACCESSOR hAccessorValue = DB_NULL_HACCESSOR;
    DWORD dwDataSize = 0;
    void *pvRawData = NULL;
    DBBINDING dbBinding = { };
    DBBINDSTATUS dbBindStatus = DBBINDSTATUS_OK;

    dbBinding.iOrdinal = dwColumnIndex + 1;
    dbBinding.dwMemOwner = DBMEMOWNER_CLIENTOWNED;
    dbBinding.dwPart = DBPART_LENGTH;
    dbBinding.wType = pTable->rgColumns[dwColumnIndex].dbtColumnType;

    pRow->pIRowset->QueryInterface(IID_IAccessor, reinterpret_cast<void **>(&pIAccessor));
    ExitOnFailure(hr, "Failed to get IAccessor interface");

    hr = pIAccessor->CreateAccessor(DBACCESSOR_ROWDATA, 1, &dbBinding, 0, &hAccessorLength, &dbBindStatus);
    ExitOnFailure(hr, "Failed to create accessor");

    hr = pRow->pIRowset->GetData(pRow->hRow, hAccessorLength, reinterpret_cast<void *>(&dwDataSize));
    ExitOnFailure(hr, "Failed to get size of data");

    // For variable-length columns, zero data returned means NULL
    if (0 == dwDataSize)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    if (NULL != ppbData)
    {
        dbBinding.dwPart = DBPART_VALUE;
        dbBinding.cbMaxLen = dwDataSize;

        hr = pIAccessor->CreateAccessor(DBACCESSOR_ROWDATA, 1, &dbBinding, 0, &hAccessorValue, &dbBindStatus);
        ExitOnFailure(hr, "Failed to create accessor");

        if (DBBINDSTATUS_OK != dbBindStatus)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Bad bind status while creating accessor to get value");
        }

        if (DBTYPE_WSTR == dbBinding.wType)
        {
            hr = StrAlloc(reinterpret_cast<LPWSTR *>(&pvRawData), dwDataSize / sizeof(WCHAR));
            ExitOnFailure(hr, "Failed to allocate space for string data while reading column %u", dwColumnIndex);
        }
        else
        {
            pvRawData = MemAlloc(dwDataSize, TRUE);
            ExitOnNull(pvRawData, hr, E_OUTOFMEMORY, "Failed to allocate space for data while reading column %u", dwColumnIndex);
        }

        hr = pRow->pIRowset->GetData(pRow->hRow, hAccessorValue, pvRawData);
        ExitOnFailure(hr, "Failed to read data value");

        ReleaseMem(*ppbData);
        *ppbData = reinterpret_cast<BYTE *>(pvRawData);
        pvRawData = NULL;
    }

    *pcbSize = dwDataSize;

LExit:
    ReleaseMem(pvRawData);

    if (DB_NULL_HACCESSOR != hAccessorLength)
    {
        pIAccessor->ReleaseAccessor(hAccessorLength, NULL);
    }
    if (DB_NULL_HACCESSOR != hAccessorValue)
    {
        pIAccessor->ReleaseAccessor(hAccessorValue, NULL);
    }
    ReleaseObject(pIAccessor);

    return hr;
}

static HRESULT GetColumnValueFixed(
    __in SCE_ROW *pRow,
    __in DWORD dwColumnIndex,
    __in DWORD cbSize,
    __out BYTE *pbData
    )
{
    HRESULT hr = S_OK;
    const SCE_TABLE_SCHEMA *pTable = pRow->pTableSchema;
    IAccessor *pIAccessor = NULL;
    HACCESSOR hAccessorLength = DB_NULL_HACCESSOR;
    HACCESSOR hAccessorValue = DB_NULL_HACCESSOR;
    DWORD dwDataSize = 0;
    DBBINDSTATUS dbBindStatus = DBBINDSTATUS_OK;
    DBBINDING dbBinding = { };

    dbBinding.iOrdinal = dwColumnIndex + 1;
    dbBinding.dwMemOwner = DBMEMOWNER_CLIENTOWNED;
    dbBinding.dwPart = DBPART_LENGTH;
    dbBinding.wType = pTable->rgColumns[dwColumnIndex].dbtColumnType;

    pRow->pIRowset->QueryInterface(IID_IAccessor, reinterpret_cast<void **>(&pIAccessor));
    ExitOnFailure(hr, "Failed to get IAccessor interface");

    hr = pIAccessor->CreateAccessor(DBACCESSOR_ROWDATA, 1, &dbBinding, 0, &hAccessorLength, &dbBindStatus);
    ExitOnFailure(hr, "Failed to create accessor");

    if (DBBINDSTATUS_OK != dbBindStatus)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Bad bind status while creating accessor to get length of value");
    }

    hr = pRow->pIRowset->GetData(pRow->hRow, hAccessorLength, reinterpret_cast<void *>(&dwDataSize));
    ExitOnFailure(hr, "Failed to get size of data");

    if (0 == dwDataSize)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    dbBinding.dwPart = DBPART_VALUE;
    dbBinding.cbMaxLen = cbSize;

    hr = pIAccessor->CreateAccessor(DBACCESSOR_ROWDATA, 1, &dbBinding, 0, &hAccessorValue, &dbBindStatus);
    ExitOnFailure(hr, "Failed to create accessor");

    if (DBBINDSTATUS_OK != dbBindStatus)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Bad bind status while creating accessor to get value");
    }

    hr = pRow->pIRowset->GetData(pRow->hRow, hAccessorValue, reinterpret_cast<void *>(pbData));
    ExitOnFailure(hr, "Failed to read data value");

LExit:
    if (DB_NULL_HACCESSOR != hAccessorLength)
    {
        pIAccessor->ReleaseAccessor(hAccessorLength, NULL);
    }
    if (DB_NULL_HACCESSOR != hAccessorValue)
    {
        pIAccessor->ReleaseAccessor(hAccessorValue, NULL);
    }
    ReleaseObject(pIAccessor);

    return hr;
}

static HRESULT EnsureLocalColumnConstraints(
    __in ITableDefinition *pTableDefinition,
    __in DBID *pTableID,
    __in SCE_TABLE_SCHEMA *pTableSchema
    )
{
    HRESULT hr = S_OK;
    SCE_COLUMN_SCHEMA *pCurrentColumn = NULL;
    DBCONSTRAINTDESC dbcdConstraint = { };
    DBID dbConstraintID = { };
    DBID dbLocalColumnID = { };
    ITableDefinitionWithConstraints *pTableDefinitionWithConstraints = NULL;

    hr = pTableDefinition->QueryInterface(IID_ITableDefinitionWithConstraints, reinterpret_cast<void **>(&pTableDefinitionWithConstraints));
    ExitOnFailure(hr, "Failed to query for ITableDefinitionWithConstraints interface in order to create column constraints");

    for (DWORD i = 0; i < pTableSchema->cColumns; ++i)
    {
        pCurrentColumn = pTableSchema->rgColumns + i;

        // Add a primary key constraint for this column, if one exists
        if (pCurrentColumn->fPrimaryKey)
        {
            // Setup DBID for new constraint
            dbConstraintID.eKind = DBKIND_NAME;
            dbConstraintID.uName.pwszName = const_cast<LPOLESTR>(L"PrimaryKey");
            dbcdConstraint.pConstraintID = &dbConstraintID;

            dbcdConstraint.ConstraintType = DBCONSTRAINTTYPE_PRIMARYKEY;

            dbLocalColumnID.eKind = DBKIND_NAME;
            dbLocalColumnID.uName.pwszName = const_cast<LPOLESTR>(pCurrentColumn->wzName);
            dbcdConstraint.cColumns = 1;
            dbcdConstraint.rgColumnList = &dbLocalColumnID;

            dbcdConstraint.pReferencedTableID = NULL;
            dbcdConstraint.cForeignKeyColumns = 0;
            dbcdConstraint.rgForeignKeyColumnList = NULL;
            dbcdConstraint.pwszConstraintText = NULL;
            dbcdConstraint.UpdateRule = DBUPDELRULE_NOACTION;
            dbcdConstraint.DeleteRule = DBUPDELRULE_NOACTION;
            dbcdConstraint.MatchType = DBMATCHTYPE_NONE;
            dbcdConstraint.Deferrability = 0;
            dbcdConstraint.cReserved = 0;
            dbcdConstraint.rgReserved = NULL;

            hr = pTableDefinitionWithConstraints->AddConstraint(pTableID, &dbcdConstraint);
            if (DB_E_DUPLICATECONSTRAINTID == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to add primary key constraint for column %ls, table %ls", pCurrentColumn->wzName, pTableSchema->wzName);
        }
    }

LExit:
    ReleaseObject(pTableDefinitionWithConstraints);

    return hr;
}

static HRESULT EnsureForeignColumnConstraints(
    __in ITableDefinition *pTableDefinition,
    __in DBID *pTableID,
    __in SCE_TABLE_SCHEMA *pTableSchema,
    __in SCE_DATABASE_SCHEMA *pDatabaseSchema
    )
{
    HRESULT hr = S_OK;
    SCE_COLUMN_SCHEMA *pCurrentColumn = NULL;
    DBCONSTRAINTDESC dbcdConstraint = { };
    DBID dbConstraintID = { };
    DBID dbLocalColumnID = { };
    DBID dbForeignTableID = { };
    DBID dbForeignColumnID = { };
    ITableDefinitionWithConstraints *pTableDefinitionWithConstraints = NULL;

    hr = pTableDefinition->QueryInterface(IID_ITableDefinitionWithConstraints, reinterpret_cast<void **>(&pTableDefinitionWithConstraints));
    ExitOnFailure(hr, "Failed to query for ITableDefinitionWithConstraints interface in order to create column constraints");

    for (DWORD i = 0; i < pTableSchema->cColumns; ++i)
    {
        pCurrentColumn = pTableSchema->rgColumns + i;

        // Add a foreign key constraint for this column, if one exists
        if (NULL != pCurrentColumn->wzRelationName)
        {
            // Setup DBID for new constraint
            dbConstraintID.eKind = DBKIND_NAME;
            dbConstraintID.uName.pwszName = const_cast<LPOLESTR>(pCurrentColumn->wzRelationName);
            dbcdConstraint.pConstraintID = &dbConstraintID;

            dbcdConstraint.ConstraintType = DBCONSTRAINTTYPE_FOREIGNKEY;

            dbForeignColumnID.eKind = DBKIND_NAME;
            dbForeignColumnID.uName.pwszName = const_cast<LPOLESTR>(pDatabaseSchema->rgTables[pCurrentColumn->dwForeignKeyTable].rgColumns[pCurrentColumn->dwForeignKeyColumn].wzName);
            dbcdConstraint.cColumns = 1;
            dbcdConstraint.rgColumnList = &dbForeignColumnID;

            dbForeignTableID.eKind = DBKIND_NAME;
            dbForeignTableID.uName.pwszName = const_cast<LPOLESTR>(pDatabaseSchema->rgTables[pCurrentColumn->dwForeignKeyTable].wzName);
            dbcdConstraint.pReferencedTableID = &dbForeignTableID;

            dbLocalColumnID.eKind = DBKIND_NAME;
            dbLocalColumnID.uName.pwszName = const_cast<LPOLESTR>(pCurrentColumn->wzName);
            dbcdConstraint.cForeignKeyColumns = 1;
            dbcdConstraint.rgForeignKeyColumnList = &dbLocalColumnID;

            dbcdConstraint.pwszConstraintText = NULL;
            dbcdConstraint.UpdateRule = DBUPDELRULE_NOACTION;
            dbcdConstraint.DeleteRule = DBUPDELRULE_NOACTION;
            dbcdConstraint.MatchType = DBMATCHTYPE_FULL;
            dbcdConstraint.Deferrability = 0;
            dbcdConstraint.cReserved = 0;
            dbcdConstraint.rgReserved = NULL;

            hr = pTableDefinitionWithConstraints->AddConstraint(pTableID, &dbcdConstraint);
            if (DB_E_DUPLICATECONSTRAINTID == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to add constraint named: %ls to table: %ls", pCurrentColumn->wzRelationName, pTableSchema->wzName);
        }
    }

LExit:
    ReleaseObject(pTableDefinitionWithConstraints);

    return hr;
}

static HRESULT SetSessionProperties(
    __in ISessionProperties *pISessionProperties
    )
{
    HRESULT hr = S_OK;
    DBPROP rgdbpDataSourceProp[1];
    DBPROPSET rgdbpDataSourcePropSet[1];

    rgdbpDataSourceProp[0].dwPropertyID = DBPROP_SSCE_TRANSACTION_COMMIT_MODE;
    rgdbpDataSourceProp[0].dwOptions = DBPROPOPTIONS_REQUIRED;
    rgdbpDataSourceProp[0].vValue.vt = VT_I4;
    rgdbpDataSourceProp[0].vValue.lVal = DBPROPVAL_SSCE_TCM_FLUSH; 
        
    rgdbpDataSourcePropSet[0].guidPropertySet = DBPROPSET_SSCE_SESSION;
    rgdbpDataSourcePropSet[0].rgProperties = rgdbpDataSourceProp;
    rgdbpDataSourcePropSet[0].cProperties = 1;

    hr = pISessionProperties->SetProperties(1, rgdbpDataSourcePropSet);
    ExitOnFailure(hr, "Failed to set session properties");

LExit:
    return hr;
}

static HRESULT GetDatabaseSchemaInfo(
    __in SCE_DATABASE *pDatabase,
    __out LPWSTR *psczSchemaType,
    __out DWORD *pdwVersion
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczSchemaType = NULL;
    DWORD dwVersionFound = 0;
    SCE_TABLE_SCHEMA schemaTable = SCE_INTERNAL_VERSION_TABLE_SCHEMA[0];
    SCE_DATABASE_SCHEMA fullSchema = { 1, &schemaTable};
    // Database object with our alternate schema
    SCE_DATABASE database = { pDatabase->sdbHandle, &fullSchema };
    SCE_ROW_HANDLE sceRow = NULL;

    hr = OpenSchema(pDatabase, &fullSchema);
    ExitOnFailure(hr, "Failed to ensure internal version schema");

    hr = SceGetFirstRow(&database, 0, &sceRow);
    ExitOnFailure(hr, "Failed to get first row in internal version schema table");

    hr = SceGetColumnString(sceRow, 0, &sczSchemaType);
    ExitOnFailure(hr, "Failed to get internal schematype");

    hr = SceGetColumnDword(sceRow, 1, &dwVersionFound);
    ExitOnFailure(hr, "Failed to get internal version");

    *psczSchemaType = sczSchemaType;
    sczSchemaType = NULL;
    *pdwVersion = dwVersionFound;

LExit:
    SceCloseTable(&schemaTable); // ignore failure
    ReleaseStr(sczSchemaType);
    ReleaseSceRow(sceRow);

    return hr;
}

static HRESULT SetDatabaseSchemaInfo(
    __in SCE_DATABASE *pDatabase,
    __in LPCWSTR wzSchemaType,
    __in DWORD dwVersion
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    SCE_TABLE_SCHEMA schemaTable = SCE_INTERNAL_VERSION_TABLE_SCHEMA[0];
    SCE_DATABASE_SCHEMA fullSchema = { 1, &schemaTable};
    // Database object with our alternate schema
    SCE_DATABASE database = { pDatabase->sdbHandle, &fullSchema };
    SCE_ROW_HANDLE sceRow = NULL;

    hr = EnsureSchema(pDatabase, &fullSchema);
    ExitOnFailure(hr, "Failed to ensure internal version schema");

    hr = SceBeginTransaction(&database);
    ExitOnFailure(hr, "Failed to begin transaction");
    fInSceTransaction = TRUE;

    hr = SceGetFirstRow(&database, 0, &sceRow);
    if (E_NOTFOUND == hr)
    {
        hr = ScePrepareInsert(&database, 0, &sceRow);
        ExitOnFailure(hr, "Failed to insert only row into internal version schema table");
    }
    else
    {
        ExitOnFailure(hr, "Failed to get first row in internal version schema table");
    }

    hr = SceSetColumnString(sceRow, 0, wzSchemaType);
    ExitOnFailure(hr, "Failed to set internal schematype to: %ls", wzSchemaType);

    hr = SceSetColumnDword(sceRow, 1, dwVersion);
    ExitOnFailure(hr, "Failed to set internal version to: %u", dwVersion);

    hr = SceFinishUpdate(sceRow);
    ExitOnFailure(hr, "Failed to insert first row in internal version schema table");

    hr = SceCommitTransaction(&database);
    ExitOnFailure(hr, "Failed to commit transaction");
    fInSceTransaction = FALSE;

LExit:
    SceCloseTable(&schemaTable); // ignore failure
    ReleaseSceRow(sceRow);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(&database);
    }

    return hr;
}

static void ReleaseDatabase(
    SCE_DATABASE *pDatabase
    )
{
    if (NULL != pDatabase)
    {
        if (NULL != pDatabase->pdsSchema)
        {
            for (DWORD i = 0; i < pDatabase->pdsSchema->cTables; ++i)
            {
                SceCloseTable(pDatabase->pdsSchema->rgTables + i);
            }
        }

        if (NULL != pDatabase->sdbHandle)
        {
            ReleaseDatabaseInternal(reinterpret_cast<SCE_DATABASE_INTERNAL *>(pDatabase->sdbHandle));
        }
    }
    ReleaseMem(pDatabase);
}

static void ReleaseDatabaseInternal(
    SCE_DATABASE_INTERNAL *pDatabaseInternal
    )
{
    HRESULT hr = S_OK;

    if (NULL != pDatabaseInternal)
    {
        ReleaseObject(pDatabaseInternal->pITransactionLocal);
        ReleaseObject(pDatabaseInternal->pIOpenRowset);
        ReleaseObject(pDatabaseInternal->pISessionProperties);
        ReleaseObject(pDatabaseInternal->pIDBCreateSession);
        ReleaseObject(pDatabaseInternal->pIDBProperties);

        if (NULL != pDatabaseInternal->pIDBInitialize)
        {
            hr = pDatabaseInternal->pIDBInitialize->Uninitialize();
            if (FAILED(hr))
            {
                TraceError(hr, "Failed to call uninitialize on IDBInitialize");
            }
            ReleaseObject(pDatabaseInternal->pIDBInitialize);
        }

        if (NULL != pDatabaseInternal->hSqlCeDll)
        {
            if (!::FreeLibrary(pDatabaseInternal->hSqlCeDll))
            {
                hr = HRESULT_FROM_WIN32(::GetLastError());
                TraceError(hr, "Failed to free sql ce dll");
            }
        }
    }

    // If there was a temp file we copied to (for read-only databases), delete it after close
    if (NULL != pDatabaseInternal->sczTempDbFile)
    {
        hr = FileEnsureDelete(pDatabaseInternal->sczTempDbFile);
        if (FAILED(hr))
        {
            TraceError(hr, "Failed to delete temporary database file (copied here because the database was opened as read-only): %ls", pDatabaseInternal->sczTempDbFile);
        }
        ReleaseStr(pDatabaseInternal->sczTempDbFile);
    }

    ReleaseMem(pDatabaseInternal);
}

#endif // end SKIP_SCE_COMPILE
