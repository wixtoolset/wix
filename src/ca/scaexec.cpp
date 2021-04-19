// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


/********************************************************************
 * CreateDatabase - CUSTOM ACTION ENTRY POINT for creating databases
 *
 *  Input:  deferred CustomActionData - DbKey\tServer\tInstance\tDatabase\tAttributes\tIntegratedAuth\tUser\tPassword
 * ****************************************************************/
extern "C" UINT __stdcall CreateDatabase(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug CreateDatabase here");
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;
    IDBCreateSession* pidbSession = NULL;
    BSTR bstrErrorDescription = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzDatabaseKey = NULL;
    LPWSTR pwzServer = NULL;
    LPWSTR pwzInstance = NULL;
    LPWSTR pwzDatabase = NULL;
    LPWSTR pwzTemp = NULL;
    int iAttributes;
    BOOL fIntegratedAuth;
    LPWSTR pwzUser = NULL;
    LPWSTR pwzPassword = NULL;
    BOOL fHaveDbFileSpec = FALSE;
    SQL_FILESPEC sfDb;
    BOOL fHaveLogFileSpec = FALSE;
    SQL_FILESPEC sfLog;
    BOOL fInitializedCom = FALSE;

    memset(&sfDb, 0, sizeof(sfDb));
    memset(&sfLog, 0, sizeof(sfLog));

    hr = WcaInitialize(hInstall, "CreateDatabase");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to intialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabaseKey); // SQL Server
    ExitOnFailure(hr, "failed to read database key from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzServer); // SQL Server
    ExitOnFailure(hr, "failed to read server from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzInstance); // SQL Server Instance
    ExitOnFailure(hr, "failed to read server instance from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabase); // SQL Database
    ExitOnFailure(hr, "failed to read server instance from custom action data: %ls", pwz);
    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data: %ls", pwz);
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fIntegratedAuth)); // Integrated Windows Authentication?
    ExitOnFailure(hr, "failed to read integrated auth flag from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzUser); // SQL User
    ExitOnFailure(hr, "failed to read user from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzPassword); // SQL User Password
    ExitOnFailure(hr, "failed to read user from custom action data: %ls", pwz);

    // db file spec
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fHaveDbFileSpec));
    ExitOnFailure(hr, "failed to read db file spec from custom action data: %ls", pwz);

    if (fHaveDbFileSpec)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read db file spec name from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzName, countof(sfDb.wzName), pwzTemp);
        ExitOnFailure(hr, "failed to copy db file spec name: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read db file spec filename from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzFilename, countof(sfDb.wzFilename), pwzTemp);
        ExitOnFailure(hr, "failed to copy db file spec filename: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read db file spec size from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzSize, countof(sfDb.wzSize), pwzTemp);
        ExitOnFailure(hr, "failed to copy db file spec size value: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read db file spec max size from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzMaxSize, countof(sfDb.wzMaxSize), pwzTemp);
        ExitOnFailure(hr, "failed to copy db file spec max size: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read db file spec grow from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzGrow, countof(sfDb.wzGrow), pwzTemp);
        ExitOnFailure(hr, "failed to copy db file spec grow value: %ls", pwzTemp);
    }

    // log file spec
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fHaveLogFileSpec));
    ExitOnFailure(hr, "failed to read log file spec from custom action data: %ls", pwz);
    if (fHaveLogFileSpec)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read log file spec name from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzName, countof(sfDb.wzName), pwzTemp);
        ExitOnFailure(hr, "failed to copy log file spec name: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read log file spec filename from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzFilename, countof(sfDb.wzFilename), pwzTemp);
        ExitOnFailure(hr, "failed to copy log file spec filename: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read log file spec size from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzSize, countof(sfDb.wzSize), pwzTemp);
        ExitOnFailure(hr, "failed to copy log file spec size value: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read log file spec max size from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzMaxSize, countof(sfDb.wzMaxSize), pwzTemp);
        ExitOnFailure(hr, "failed to copy log file spec max size: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure(hr, "failed to read log file spec grow from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzGrow, countof(sfDb.wzGrow), pwzTemp);
        ExitOnFailure(hr, "failed to copy log file spec grow value: %ls", pwzTemp);
    }

    if (iAttributes & SCADB_CONFIRM_OVERWRITE)
    {
        // Check if the database already exists
        hr = SqlDatabaseExists(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, &bstrErrorDescription);
        MessageExitOnFailure(hr, msierrSQLFailedCreateDatabase, "failed to check if database exists: '%ls', error: %ls", pwzDatabase, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription);

        if (S_OK == hr) // found an existing database, confirm that they don't want to stop before it gets trampled, in no UI case just continue anyways
        {
            hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
            if (IDNO == WcaErrorMessage(msierrSQLDatabaseAlreadyExists, hr, MB_YESNO, 1, pwzDatabase))
                ExitOnFailure(hr, "failed to initialize");
        }
    }

    hr = SqlDatabaseEnsureExists(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, fHaveDbFileSpec ? &sfDb : NULL, fHaveLogFileSpec ? &sfLog : NULL, &bstrErrorDescription);
    if ((iAttributes & SCADB_CONTINUE_ON_ERROR) && FAILED(hr))
    {
        WcaLog(LOGMSG_STANDARD, "Error 0x%x: failed to create SQL database but continuing, error: %ls, Database: %ls", hr, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzDatabase);
        hr = S_OK;
    }
    MessageExitOnFailure(hr, msierrSQLFailedCreateDatabase, "failed to create to database: '%ls', error: %ls", pwzDatabase, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription);

    hr = WcaProgressMessage(COST_SQL_CONNECTDB, FALSE);
LExit:
    ReleaseStr(pwzDatabaseKey);
    ReleaseStr(pwzServer);
    ReleaseStr(pwzInstance);
    ReleaseStr(pwzDatabase);
    ReleaseStr(pwzUser);
    ReleaseStr(pwzPassword);
    ReleaseObject(pidbSession);
    ReleaseBSTR(bstrErrorDescription);

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
 DropDatabase - CUSTOM ACTION ENTRY POINT for removing databases

  Input:  deferred CustomActionData - DbKey\tServer\tInstance\tDatabase\tAttributes\tIntegratedAuth\tUser\tPassword
 * ****************************************************************/
extern "C" UINT __stdcall DropDatabase(MSIHANDLE hInstall)
{
//Assert(FALSE);
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;
    IDBCreateSession* pidbSession = NULL;
    BSTR bstrErrorDescription = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzDatabaseKey = NULL;
    LPWSTR pwzServer = NULL;
    LPWSTR pwzInstance = NULL;
    LPWSTR pwzDatabase = NULL;
    long lAttributes;
    BOOL fIntegratedAuth;
    LPWSTR pwzUser = NULL;
    LPWSTR pwzPassword = NULL;
    BOOL fInitializedCom = TRUE;

    hr = WcaInitialize(hInstall, "DropDatabase");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to intialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabaseKey);
    ExitOnFailure(hr, "failed to read database key");
    hr = WcaReadStringFromCaData(&pwz, &pwzServer);
    ExitOnFailure(hr, "failed to read server");
    hr = WcaReadStringFromCaData(&pwz, &pwzInstance);
    ExitOnFailure(hr, "failed to read instance");
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabase);
    ExitOnFailure(hr, "failed to read database");
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&lAttributes));
    ExitOnFailure(hr, "failed to read attributes");
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fIntegratedAuth)); // Integrated Windows Authentication?
    ExitOnFailure(hr, "failed to read integrated auth flag");
    hr = WcaReadStringFromCaData(&pwz, &pwzUser);
    ExitOnFailure(hr, "failed to read user");
    hr = WcaReadStringFromCaData(&pwz, &pwzPassword);
    ExitOnFailure(hr, "failed to read password");

    hr = SqlDropDatabase(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, &bstrErrorDescription);
    if ((lAttributes & SCADB_CONTINUE_ON_ERROR) && FAILED(hr))
    {
        WcaLog(LOGMSG_STANDARD, "Error 0x%x: failed to drop SQL database but continuing, error: %ls, Database: %ls", hr, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzDatabase);
        hr = S_OK;
    }
    MessageExitOnFailure(hr, msierrSQLFailedDropDatabase, "failed to drop to database: '%ls', error: %ls", pwzDatabase, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription);

    hr = WcaProgressMessage(COST_SQL_CONNECTDB, FALSE);

LExit:
    ReleaseStr(pwzDatabaseKey);
    ReleaseStr(pwzServer);
    ReleaseStr(pwzInstance);
    ReleaseStr(pwzDatabase);
    ReleaseStr(pwzUser);
    ReleaseStr(pwzPassword);
    ReleaseStr(pwzData);
    ReleaseObject(pidbSession);
    ReleaseBSTR(bstrErrorDescription);

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
 ExecuteSqlStrings - CUSTOM ACTION ENTRY POINT for running SQL strings

  Input:  deferred CustomActionData - DbKey\tServer\tInstance\tDatabase\tAttributes\tIntegratedAuth\tUser\tPassword\tSQLKey1\tSQLString1\tSQLKey2\tSQLString2\tSQLKey3\tSQLString3\t...
          rollback CustomActionData - same as above
 * ****************************************************************/
extern "C" UINT __stdcall ExecuteSqlStrings(MSIHANDLE hInstall)
{
//Assert(FALSE);
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;
    HRESULT hrDB = S_OK;

    LPWSTR pwzData = NULL;
    IDBCreateSession* pidbSession = NULL;
    BSTR bstrErrorDescription = NULL;

    LPWSTR pwz = NULL;
    LPWSTR pwzDatabaseKey = NULL;
    LPWSTR pwzServer = NULL;
    LPWSTR pwzInstance = NULL;
    LPWSTR pwzDatabase = NULL;
    int iAttributesDB;
    int iAttributesSQL;
    BOOL fIntegratedAuth;
    LPWSTR pwzUser = NULL;
    LPWSTR pwzPassword = NULL;
    LPWSTR pwzSqlKey = NULL;
    LPWSTR pwzSql = NULL;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "ExecuteSqlStrings");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to intialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabaseKey);
    ExitOnFailure(hr, "failed to read database key");
    hr = WcaReadStringFromCaData(&pwz, &pwzServer);
    ExitOnFailure(hr, "failed to read server");
    hr = WcaReadStringFromCaData(&pwz, &pwzInstance);
    ExitOnFailure(hr, "failed to read instance");
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabase);
    ExitOnFailure(hr, "failed to read database");
    hr = WcaReadIntegerFromCaData(&pwz, &iAttributesDB);
    ExitOnFailure(hr, "failed to read attributes");
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fIntegratedAuth)); // Integrated Windows Authentication?
    ExitOnFailure(hr, "failed to read integrated auth flag");
    hr = WcaReadStringFromCaData(&pwz, &pwzUser);
    ExitOnFailure(hr, "failed to read user");
    hr = WcaReadStringFromCaData(&pwz, &pwzPassword);
    ExitOnFailure(hr, "failed to read password");

    // Store off the result of the connect, only exit if we don't care if the database connection succeeds
    // Wait to fail until later to see if we actually have work to do that is not set to continue on error
    hrDB = SqlConnectDatabase(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, &pidbSession);
    if ((iAttributesDB & SCADB_CONTINUE_ON_ERROR) && FAILED(hrDB))
    {
        WcaLog(LOGMSG_STANDARD, "Error 0x%x: continuing after failure to connect to database: %ls", hrDB, pwzDatabase);
        ExitFunction1(hr = S_OK);
    }

    while (S_OK == hr && S_OK == (hr = WcaReadStringFromCaData(&pwz, &pwzSqlKey)))
    {
        hr = WcaReadIntegerFromCaData(&pwz, &iAttributesSQL);
        ExitOnFailure(hr, "failed to read attributes for SQL string: %ls", pwzSqlKey);

        hr = WcaReadStringFromCaData(&pwz, &pwzSql);
        ExitOnFailure(hr, "failed to read SQL string for key: %ls", pwzSqlKey);

        // If the Wix4SqlString row is set to continue on error and the DB connection failed, skip attempting to execute
        if ((iAttributesSQL & SCASQL_CONTINUE_ON_ERROR) && FAILED(hrDB))
        {
            WcaLog(LOGMSG_STANDARD, "Error 0x%x: continuing after failure to connect to database: %ls", hrDB, pwzDatabase);
            continue;
        }

        // Now check if the DB connection succeeded
        MessageExitOnFailure(hr = hrDB, msierrSQLFailedConnectDatabase, "failed to connect to database: '%ls'", pwzDatabase);

        WcaLog(LOGMSG_VERBOSE, "Executing SQL string: %ls", pwzSql);
        hr = SqlSessionExecuteQuery(pidbSession, pwzSql, NULL, NULL, &bstrErrorDescription);
        if ((iAttributesSQL & SCASQL_CONTINUE_ON_ERROR) && FAILED(hr))
        {
            WcaLog(LOGMSG_STANDARD, "Error 0x%x: failed to execute SQL string but continuing, error: %ls, SQL key: %ls SQL string: %ls", hr, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzSqlKey, pwzSql);
            hr = S_OK;
        }
        MessageExitOnFailure(hr, msierrSQLFailedExecString, "failed to execute SQL string, error: %ls, SQL key: %ls SQL string: %ls", NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzSqlKey, pwzSql);

        WcaProgressMessage(COST_SQL_STRING, FALSE);
    }
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseStr(pwzDatabaseKey);
    ReleaseStr(pwzServer);
    ReleaseStr(pwzInstance);
    ReleaseStr(pwzDatabase);
    ReleaseStr(pwzUser);
    ReleaseStr(pwzPassword);
    ReleaseStr(pwzData);

    ReleaseBSTR(bstrErrorDescription);
    ReleaseObject(pidbSession);

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
