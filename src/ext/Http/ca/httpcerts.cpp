// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#if _WIN32_WINNT < 0x0602

typedef struct _HTTP_SERVICE_CONFIG_SSL_SNI_KEY
{
    SOCKADDR_STORAGE IpPort;
    PWSTR Host;
} HTTP_SERVICE_CONFIG_SSL_SNI_KEY, * PHTTP_SERVICE_CONFIG_SSL_SNI_KEY;

typedef struct _HTTP_SERVICE_CONFIG_SSL_SNI_SET
{
    HTTP_SERVICE_CONFIG_SSL_SNI_KEY KeyDesc;
    HTTP_SERVICE_CONFIG_SSL_PARAM   ParamDesc;
} HTTP_SERVICE_CONFIG_SSL_SNI_SET, * PHTTP_SERVICE_CONFIG_SSL_SNI_SET;

typedef struct _HTTP_SERVICE_CONFIG_SSL_SNI_QUERY
{
    HTTP_SERVICE_CONFIG_QUERY_TYPE  QueryDesc;
    HTTP_SERVICE_CONFIG_SSL_SNI_KEY KeyDesc;
    DWORD                           dwToken;
} HTTP_SERVICE_CONFIG_SSL_SNI_QUERY, * PHTTP_SERVICE_CONFIG_SSL_SNI_QUERY;

#define HttpServiceConfigSslSniCertInfo static_cast<HTTP_SERVICE_CONFIG_ID>(HttpServiceConfigCache + 1)

#endif

static UINT SchedHttpCertificates(
    __in WCA_TODO todoSched
);
static HRESULT FindExistingSniSslCertificate(
    __in_z LPWSTR wzHost,
    __in int nPort,
    __out HTTP_SERVICE_CONFIG_SSL_SNI_SET** ppSet
);
static HRESULT FindExistingIpSslCertificate(
    __in int nPort,
    __out HTTP_SERVICE_CONFIG_SSL_SET** ppSet
);
static HRESULT WriteSniSslCertCustomActionData(
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzHost,
    __in int iPort,
    __in int iHandleExisting,
    __in HTTP_SERVICE_CONFIG_SSL_SNI_SET* pSniSslSet,
    __inout_z LPWSTR* psczCustomActionData
);
static HRESULT WriteIpSslCertCustomActionData(
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in int iPort,
    __in int iHandleExisting,
    __in HTTP_SERVICE_CONFIG_SSL_SET* pSniSslSet,
    __inout_z LPWSTR* psczCustomActionData
);
static HRESULT AddSniSslCert(
    __in_z LPCWSTR wzId,
    __in_z LPWSTR wzHost,
    __in int iPort,
    __in BYTE rgbCertificateThumbprint[],
    __in DWORD cbCertificateThumbprint,
    __in GUID* pAppId,
    __in_z LPWSTR wzSslCertStore
);
static HRESULT AddIpSslCert(
    __in_z LPCWSTR wzId,
    __in int iPort,
    __in BYTE rgbCertificateThumbprint[],
    __in DWORD cbCertificateThumbprint,
    __in GUID* pAppId,
    __in_z LPWSTR wzSslCertStore
);
static HRESULT RemoveSniSslCert(
    __in_z_opt LPCWSTR wzId,
    __in_z LPWSTR wzHost,
    __in int iPort
);
static HRESULT RemoveIpSslCert(
    __in_z_opt LPCWSTR wzId,
    __in int iPort
);
static void SetSniSslCertificateKeyPort(
    __in HTTP_SERVICE_CONFIG_SSL_SNI_KEY* pKey,
    __in_z LPWSTR wzHost,
    __in int iPort
);
static void SetIpSslCertificateKeyPort(
    __in HTTP_SERVICE_CONFIG_SSL_KEY* pKey,
    __in SOCKADDR_IN* pSin,
    __in int iPort
);
static HRESULT EnsureAppId(
    __inout_z LPWSTR* psczAppId,
    __in_opt GUID* pGuid
);
static HRESULT StringFromGuid(
    __in REFGUID rguid,
    __inout_z LPWSTR* psczGuid
);
static HRESULT WriteCertificateCaData(
    __in eCertificateType certType,
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in_z_opt LPCWSTR wzHost,
    __in int iPort,
    __in int iHandleExisting,
    __in_z LPCWSTR wzCertificateThumbprint,
    __in_z_opt LPCWSTR wzAppId,
    __in_z_opt LPCWSTR wzCertificateStore,
    __inout_z LPWSTR* psczCustomActionData
);


LPCWSTR vcsHttpCertificatesQuery =
L"SELECT `HttpCertificate`, `Host`, `Port`, `Thumbprint`, `AppId`, `Store`, `HandleExisting`, `Type`, `Component_` "
L"FROM `Wix6HttpCertificate`";
enum eHttpCertificatesQuery { hcqId = 1, hcqHost, hcqPort, hcqCertificateThumbprint, hcqAppId, hcqCertificateStore, hcqHandleExisting, hcqType, hcqComponent };

/******************************************************************
 SchedHttpCertificatesInstall - immediate custom action entry
   point to prepare adding certificates.

********************************************************************/
extern "C" UINT __stdcall SchedHttpCertificatesInstall(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;

    hr = WcaInitialize(hInstall, "SchedHttpCertificatesInstall");
    ExitOnFailure(hr, "Failed to initialize");

    hr = SchedHttpCertificates(WCA_TODO_INSTALL);

LExit:
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}

/******************************************************************
 SchedWixHttpSniSslCertsUninstall - immediate custom action entry
   point to prepare removing certificates.

********************************************************************/
extern "C" UINT __stdcall SchedHttpCertificatesUninstall(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;

    hr = WcaInitialize(hInstall, "SchedHttpCertificatesUninstall");
    ExitOnFailure(hr, "Failed to initialize");

    hr = SchedHttpCertificates(WCA_TODO_UNINSTALL);

LExit:
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}

/******************************************************************
 ExecHttpCertificates - deferred custom action entry point to
   bind/unbind certificates.

********************************************************************/
extern "C" UINT __stdcall ExecHttpCertificates(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;
    BOOL fHttpInitialized = FALSE;
    LPWSTR sczCustomActionData = NULL;
    LPWSTR wz = NULL;
    int iTodo = WCA_TODO_UNKNOWN;
    LPWSTR sczId = NULL;
    LPWSTR sczHost = NULL;
    int iPort = 0;
    eHandleExisting handleExisting = heIgnore;
    eCertificateType certificateType = ctSniSsl;
    LPWSTR sczCertificateThumbprint = NULL;
    LPWSTR sczAppId = NULL;
    LPWSTR sczCertificateStore = NULL;

    BOOL fRollback = ::MsiGetMode(hInstall, MSIRUNMODE_ROLLBACK);
    BOOL fRemove = FALSE;
    BOOL fAdd = FALSE;
    BOOL fFailOnExisting = FALSE;

    GUID guidAppId = { };
    BYTE* pbCertificateThumbprint = NULL;
    DWORD cbCertificateThumbprint = 0;

    // Initialize.
    hr = WcaInitialize(hInstall, "ExecHttpCertificates");
    ExitOnFailure(hr, "Failed to initialize");

    hr = HRESULT_FROM_WIN32(::HttpInitialize(HTTPAPI_VERSION_1, HTTP_INITIALIZE_CONFIG, NULL));
    ExitOnFailure(hr, "Failed to initialize HTTP Server configuration");

    fHttpInitialized = TRUE;

    hr = WcaGetProperty(L"CustomActionData", &sczCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", sczCustomActionData);

    wz = sczCustomActionData;
    while (wz && *wz)
    {
        // Extract the custom action data and if rolling back, swap INSTALL and UNINSTALL.
        hr = WcaReadIntegerFromCaData(&wz, reinterpret_cast<int*>(&certificateType));
        ExitOnFailure(hr, "Failed to read Type from custom action data");

        hr = WcaReadIntegerFromCaData(&wz, &iTodo);
        ExitOnFailure(hr, "Failed to read todo from custom action data");

        hr = WcaReadStringFromCaData(&wz, &sczId);
        ExitOnFailure(hr, "Failed to read Id from custom action data");

        hr = WcaReadStringFromCaData(&wz, &sczHost);
        ExitOnFailure(hr, "Failed to read Host from custom action data");

        hr = WcaReadIntegerFromCaData(&wz, &iPort);
        ExitOnFailure(hr, "Failed to read Port from custom action data");

        hr = WcaReadIntegerFromCaData(&wz, reinterpret_cast<int*>(&handleExisting));
        ExitOnFailure(hr, "Failed to read HandleExisting from custom action data");

        hr = WcaReadStringFromCaData(&wz, &sczCertificateThumbprint);
        ExitOnFailure(hr, "Failed to read CertificateThumbprint from custom action data");

        hr = WcaReadStringFromCaData(&wz, &sczAppId);
        ExitOnFailure(hr, "Failed to read AppId from custom action data");

        hr = WcaReadStringFromCaData(&wz, &sczCertificateStore);
        ExitOnFailure(hr, "Failed to read CertificateStore from custom action data");

        switch (iTodo)
        {
        case WCA_TODO_INSTALL:
        case WCA_TODO_REINSTALL:
            fRemove = heReplace == handleExisting || fRollback;
            fAdd = !fRollback || *sczCertificateThumbprint;
            fFailOnExisting = heFail == handleExisting && !fRollback;
            break;

        case WCA_TODO_UNINSTALL:
            fRemove = !fRollback;
            fAdd = fRollback && *sczCertificateThumbprint;
            fFailOnExisting = FALSE;
            break;
        }

        if (fRemove)
        {
            if (ctSniSsl == certificateType)
            {
                hr = RemoveSniSslCert(sczId, sczHost, iPort);
            }
            else
            {
                hr = RemoveIpSslCert(sczId, iPort);
            }

            if (S_OK == hr)
            {
                WcaLog(LOGMSG_STANDARD, "Removed SSL certificate '%ls' for hostname: %ls:%d.", sczId, sczHost, iPort);
            }
            else if (FAILED(hr))
            {
                if (fRollback)
                {
                    WcaLogError(hr, "Failed to remove SSL certificate to rollback '%ls' for hostname: %ls:%d.", sczId, sczHost, iPort);
                }
                else
                {
                    ExitOnFailure(hr, "Failed to remove SSL certificate '%ls' for hostname: %ls:%d.", sczId, sczHost, iPort);
                }
            }
        }

        if (fAdd)
        {
            WcaLog(LOGMSG_STANDARD, "Adding SSL certificate '%ls' for hostname: %ls:%d.", sczId, sczHost, iPort);

            hr = StrAllocHexDecode(sczCertificateThumbprint, &pbCertificateThumbprint, &cbCertificateThumbprint);
            ExitOnFailure(hr, "Failed to convert thumbprint to bytes for SSL certificate '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);

            hr = ::IIDFromString(sczAppId, &guidAppId);
            ExitOnFailure(hr, "Failed to convert AppId '%ls' back to GUID for SSL certificate '%ls' for hostname: %ls:%d", sczAppId, sczId, sczHost, iPort);
            if (ctSniSsl == certificateType)
            {
                hr = AddSniSslCert(sczId, sczHost, iPort, pbCertificateThumbprint, cbCertificateThumbprint, &guidAppId, sczCertificateStore && *sczCertificateStore ? sczCertificateStore : L"MY");
            }
            else
            {
                hr = AddIpSslCert(sczId, iPort, pbCertificateThumbprint, cbCertificateThumbprint, &guidAppId, sczCertificateStore && *sczCertificateStore ? sczCertificateStore : L"MY");
            }

            if (S_FALSE == hr && fFailOnExisting)
            {
                hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
            }

            if (S_OK == hr)
            {
                WcaLog(LOGMSG_STANDARD, "Added SSL certificate '%ls' for hostname: %ls:%d with thumbprint: %ls.", sczId, sczHost, iPort, sczCertificateThumbprint);
            }
            else if (FAILED(hr))
            {
                if (fRollback)
                {
                    WcaLogError(hr, "Failed to add SSL certificate to rollback '%ls' for hostname: %ls:%d.", sczId, sczHost, iPort);
                }
                else
                {
                    ExitOnFailure(hr, "Failed to add SSL certificate '%ls' for hostname: %ls:%d.", sczId, sczHost, iPort);
                }
            }

            ReleaseNullMem(pbCertificateThumbprint);
        }
    }

LExit:
    ReleaseMem(pbCertificateThumbprint);
    ReleaseStr(sczCertificateStore);
    ReleaseStr(sczAppId);
    ReleaseStr(sczCertificateThumbprint);
    ReleaseStr(sczHost);
    ReleaseStr(sczId);
    ReleaseStr(sczCustomActionData);

    if (fHttpInitialized)
    {
        ::HttpTerminate(HTTP_INITIALIZE_CONFIG, NULL);
    }

    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}

static UINT SchedHttpCertificates(
    __in WCA_TODO todoSched
)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    BOOL fHttpInitialized = FALSE;
    DWORD cCertificates = 0;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;
    PMSIHANDLE hQueryReq = NULL;
    PMSIHANDLE hAceView = NULL;

    LPWSTR sczCustomActionData = NULL;
    LPWSTR sczRollbackCustomActionData = NULL;

    LPWSTR sczId = NULL;
    LPWSTR sczComponent = NULL;
    eCertificateType certificateType = ctSniSsl;
    WCA_TODO todoComponent = WCA_TODO_UNKNOWN;
    LPWSTR sczHost = NULL;
    int iPort = 0;
    LPWSTR sczCertificateThumbprint = NULL;
    LPWSTR sczAppId = NULL;
    LPWSTR sczCertificateStore = NULL;
    int iHandleExisting = 0;

    HTTP_SERVICE_CONFIG_SSL_SNI_SET* pExistingSniSslSet = NULL;
    HTTP_SERVICE_CONFIG_SSL_SET* pExistingIpSslSet = NULL;

    // Anything to do?
    hr = WcaTableExists(L"Wix6HttpCertificate");
    ExitOnFailure(hr, "Failed to check if the Wix6HttpCertificate table exists");
    if (S_FALSE == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Wix6HttpCertificate table doesn't exist, so there are no certificates to configure.");
        ExitFunction();
    }

    // Query and loop through all the SNI SSL certificates.
    hr = WcaOpenExecuteView(vcsHttpCertificatesQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on the Wix6HttpCertificate table");

    hr = HRESULT_FROM_WIN32(::HttpInitialize(HTTPAPI_VERSION_1, HTTP_INITIALIZE_CONFIG, NULL));
    ExitOnFailure(hr, "Failed to initialize HTTP Server configuration");

    fHttpInitialized = TRUE;

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, hcqId, &sczId);
        ExitOnFailure(hr, "Failed to get Wix6HttpCertificate.Wix6HttpCertificate");

        hr = WcaGetRecordString(hRec, hcqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get Wix6HttpCertificate.Component_");

        // Figure out what we're doing for this certificate, treating reinstall the same as install.
        todoComponent = WcaGetComponentToDo(sczComponent);
        if ((WCA_TODO_REINSTALL == todoComponent ? WCA_TODO_INSTALL : todoComponent) != todoSched)
        {
            WcaLog(LOGMSG_VERBOSE, "Component '%ls' action state (%d) doesn't match request (%d) for Wix6HttpCertificate '%ls'.", sczComponent, todoComponent, todoSched, sczId);
            continue;
        }

        hr = WcaGetRecordInteger(hRec, hcqType, reinterpret_cast<int*>(&certificateType));
        ExitOnFailure(hr, "Failed to get Type for Wix6HttpCertificate '%ls'", sczId);

        hr = WcaGetRecordFormattedString(hRec, hcqHost, &sczHost);
        ExitOnFailure(hr, "Failed to get Wix6HttpCertificate.Host");

        hr = WcaGetRecordFormattedInteger(hRec, hcqPort, &iPort);
        ExitOnFailure(hr, "Failed to get Wix6HttpCertificate.Port");

        hr = WcaGetRecordFormattedString(hRec, hcqCertificateThumbprint, &sczCertificateThumbprint);
        ExitOnFailure(hr, "Failed to get Wix6HttpCertificate.CertificateThumbprint");

        if (!iPort)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Missing Port value for Wix6HttpCertificate '%ls'", sczId);
        }

        if (!sczCertificateThumbprint || !*sczCertificateThumbprint)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Missing CertificateThumbprint value for Wix6HttpCertificate '%ls'", sczId);
        }

        hr = WcaGetRecordFormattedString(hRec, hcqAppId, &sczAppId);
        ExitOnFailure(hr, "Failed to get AppId for Wix6HttpCertificate '%ls'", sczId);

        hr = WcaGetRecordFormattedString(hRec, hcqCertificateStore, &sczCertificateStore);
        ExitOnFailure(hr, "Failed to get CertificateStore for Wix6HttpCertificate '%ls'", sczId);

        hr = WcaGetRecordInteger(hRec, hcqHandleExisting, &iHandleExisting);
        ExitOnFailure(hr, "Failed to get HandleExisting for Wix6HttpCertificate '%ls'", sczId);

        if (ctIpSsl == certificateType)
        {
            WcaLog(LOGMSG_STANDARD, "Processing IP SSL certificate: %ls on port %d.", sczId, iPort);

            hr = FindExistingIpSslCertificate(iPort, &pExistingIpSslSet);
            ExitOnFailure(hr, "Failed to search for an existing IP SSL certificate for '%ls' on port %d", sczId, iPort);

            if (S_FALSE != hr)
            {
                hr = WriteIpSslCertCustomActionData(todoComponent, sczId, iPort, iHandleExisting, pExistingIpSslSet, &sczRollbackCustomActionData);
                ExitOnFailure(hr, "Failed to write rollback custom action data for IP SSL '%ls' on port %d", sczId, iPort);
            }

            hr = EnsureAppId(&sczAppId, pExistingIpSslSet ? &(pExistingIpSslSet->ParamDesc.AppId) : NULL);
            ExitOnFailure(hr, "Failed to ensure AppId for IP SSL '%ls'", sczId);
        }
        else if (ctSniSsl == certificateType)
        {
            WcaLog(LOGMSG_STANDARD, "Processing SNI SSL certificate: %ls on host %ls:%d.", sczId, sczHost, iPort);

            hr = FindExistingSniSslCertificate(sczHost, iPort, &pExistingSniSslSet);
            ExitOnFailure(hr, "Failed to search for an existing SNI SSL certificate for '%ls' on host '%ls', port %d", sczId, sczHost, iPort);

            if (S_FALSE != hr)
            {
                hr = WriteSniSslCertCustomActionData(todoComponent, sczId, sczHost, iPort, iHandleExisting, pExistingSniSslSet, &sczRollbackCustomActionData);
                ExitOnFailure(hr, "Failed to write rollback custom action data for SNI SSL Wix6HttpCertificate '%ls' on host '%ls', port %d", sczId, sczHost, iPort);
            }

            hr = EnsureAppId(&sczAppId, pExistingSniSslSet ? &(pExistingSniSslSet->ParamDesc.AppId) : NULL);
            ExitOnFailure(hr, "Failed to ensure AppId for SNI SSL '%ls'", sczId);
        }

        hr = WriteCertificateCaData(certificateType, todoComponent, sczId, sczHost, iPort, iHandleExisting, sczCertificateThumbprint, sczAppId, sczCertificateStore, &sczCustomActionData);
        ExitOnFailure(hr, "Failed to write custom action data for SSL '%ls'", sczId);
        ++cCertificates;

        ReleaseNullMem(pExistingSniSslSet);
        ReleaseNullMem(pExistingIpSslSet);
    }

    // Reaching the end of the list is not an error.
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occurred while processing Wix6HttpCertificate table");

    WcaLog(LOGMSG_VERBOSE, "Scheduling %d certificates", cCertificates);

    // Schedule ExecHttpSniSslCerts if there's anything to do.
    if (cCertificates)
    {
        WcaLog(LOGMSG_TRACEONLY, "Scheduling SSL certificate: `%ls`", sczCustomActionData);
        WcaLog(LOGMSG_TRACEONLY, "Scheduling rollback SSL certificate: `%ls`", sczRollbackCustomActionData);

        if (WCA_TODO_INSTALL == todoSched)
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"RollbackHttpCertificatesInstall"), sczRollbackCustomActionData, cCertificates * COST_HTTP_SNI_SSL);
            ExitOnFailure(hr, "Failed to schedule install SSL certificate rollback");
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"ExecHttpCertificatesInstall"), sczCustomActionData, cCertificates * COST_HTTP_SNI_SSL);
            ExitOnFailure(hr, "Failed to schedule install SSL certificate execution");
        }
        else
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"RollbackHttpCertificatesUninstall"), sczRollbackCustomActionData, cCertificates * COST_HTTP_SNI_SSL);
            ExitOnFailure(hr, "Failed to schedule uninstall SSL certificate rollback");
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"ExecHttpCertificatesUninstall"), sczCustomActionData, cCertificates * COST_HTTP_SNI_SSL);
            ExitOnFailure(hr, "Failed to schedule uninstall SSL certificate execution");
        }
    }
    else
    {
        WcaLog(LOGMSG_STANDARD, "No SNI SSL certificates scheduled.");
    }

LExit:
    ReleaseMem(pExistingSniSslSet);
    ReleaseMem(pExistingIpSslSet);
    ReleaseStr(sczCertificateStore);
    ReleaseStr(sczAppId);
    ReleaseStr(sczCertificateThumbprint);
    ReleaseStr(sczHost);
    ReleaseStr(sczComponent);
    ReleaseStr(sczId);
    ReleaseStr(sczRollbackCustomActionData);
    ReleaseStr(sczCustomActionData);

    if (fHttpInitialized)
    {
        ::HttpTerminate(HTTP_INITIALIZE_CONFIG, NULL);
    }

    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}

static HRESULT FindExistingSniSslCertificate(
    __in_z LPWSTR wzHost,
    __in int nPort,
    __out HTTP_SERVICE_CONFIG_SSL_SNI_SET** ppSet
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_SSL_SNI_QUERY query = { };
    HTTP_SERVICE_CONFIG_SSL_SNI_SET* pSet = NULL;
    ULONG cbSet = 0;

    *ppSet = NULL;

    query.QueryDesc = HttpServiceConfigQueryExact;
    SetSniSslCertificateKeyPort(&query.KeyDesc, wzHost, nPort);

    WcaLog(LOGMSG_TRACEONLY, "Querying for SNI SSL certificate on port %d...", nPort);

    er = ::HttpQueryServiceConfiguration(NULL, HttpServiceConfigSslSniCertInfo, &query, sizeof(query), pSet, cbSet, &cbSet, NULL);
    if (ERROR_INSUFFICIENT_BUFFER == er)
    {
        pSet = reinterpret_cast<HTTP_SERVICE_CONFIG_SSL_SNI_SET*>(MemAlloc(cbSet, TRUE));
        ExitOnNull(pSet, hr, E_OUTOFMEMORY, "Failed to allocate query SN SSL certificate buffer");

        er = ::HttpQueryServiceConfiguration(NULL, HttpServiceConfigSslSniCertInfo, &query, sizeof(query), pSet, cbSet, &cbSet, NULL);
    }

    if (ERROR_SUCCESS == er)
    {
        *ppSet = pSet;
        pSet = NULL;
    }
    else if (ERROR_FILE_NOT_FOUND == er || ERROR_NO_MORE_ITEMS == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

LExit:
    ReleaseMem(pSet);

    return hr;
}

static HRESULT FindExistingIpSslCertificate(
    __in int nPort,
    __out HTTP_SERVICE_CONFIG_SSL_SET** ppSet
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_SSL_QUERY query = { };
    SOCKADDR_IN sin = { };
    HTTP_SERVICE_CONFIG_SSL_SET* pSet = NULL;
    ULONG cbSet = 0;

    *ppSet = NULL;

    query.QueryDesc = HttpServiceConfigQueryNext;

    SetIpSslCertificateKeyPort(&query.KeyDesc, &sin, nPort);

    WcaLog(LOGMSG_TRACEONLY, "Querying for IP SSL certificate on port %d...", nPort);

    er = ::HttpQueryServiceConfiguration(NULL, HttpServiceConfigSSLCertInfo, &query, sizeof(query), pSet, cbSet, &cbSet, NULL);
    if (ERROR_INSUFFICIENT_BUFFER == er)
    {
        pSet = reinterpret_cast<HTTP_SERVICE_CONFIG_SSL_SET*>(MemAlloc(cbSet, TRUE));
        ExitOnNull(pSet, hr, E_OUTOFMEMORY, "Failed to allocate query IP SSL certificate buffer");

        er = ::HttpQueryServiceConfiguration(NULL, HttpServiceConfigSSLCertInfo, &query, sizeof(query), pSet, cbSet, &cbSet, NULL);
    }

    if (ERROR_SUCCESS == er)
    {
        *ppSet = pSet;
        pSet = NULL;
    }
    else if (ERROR_FILE_NOT_FOUND == er || ERROR_NO_MORE_ITEMS == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

LExit:
    ReleaseMem(pSet);

    return hr;
}

static HRESULT WriteSniSslCertCustomActionData(
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzHost,
    __in int iPort,
    __in int iHandleExisting,
    __in HTTP_SERVICE_CONFIG_SSL_SNI_SET* pSniSslSet,
    __inout_z LPWSTR* psczCustomActionData
)
{
    HRESULT hr = S_OK;
    LPWSTR sczCertificateThumbprint = NULL;
    LPWSTR sczAppId = NULL;
    LPCWSTR wzCertificateStore = NULL;

    if (pSniSslSet)
    {
        hr = StrAllocHexEncode(reinterpret_cast<BYTE*>(pSniSslSet->ParamDesc.pSslHash), pSniSslSet->ParamDesc.SslHashLength, &sczCertificateThumbprint);
        ExitOnFailure(hr, "Failed to convert existing certificate thumbprint to hex for Wix6HttpCertificate '%ls'", wzId);

        hr = StringFromGuid(pSniSslSet->ParamDesc.AppId, &sczAppId);
        ExitOnFailure(hr, "Failed to copy existing AppId for Wix6HttpCertificate '%ls'", wzId);

        wzCertificateStore = pSniSslSet->ParamDesc.pSslCertStoreName;
    }

    hr = WriteCertificateCaData(ctSniSsl, action, wzId, wzHost, iPort, iHandleExisting, sczCertificateThumbprint ? sczCertificateThumbprint : L"", sczAppId ? sczAppId : L"", wzCertificateStore ? wzCertificateStore : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write custom action data for Wix6HttpCertificate '%ls'", wzId);

LExit:
    ReleaseStr(sczAppId);
    ReleaseStr(sczCertificateThumbprint);

    return hr;
}

static HRESULT WriteIpSslCertCustomActionData(
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in int iPort,
    __in int iHandleExisting,
    __in HTTP_SERVICE_CONFIG_SSL_SET* pSslSet,
    __inout_z LPWSTR* psczCustomActionData
)
{
    HRESULT hr = S_OK;
    LPWSTR sczCertificateThumbprint = NULL;
    LPWSTR sczAppId = NULL;
    LPCWSTR wzCertificateStore = NULL;

    if (pSslSet)
    {
        hr = StrAllocHexEncode(reinterpret_cast<BYTE*>(pSslSet->ParamDesc.pSslHash), pSslSet->ParamDesc.SslHashLength, &sczCertificateThumbprint);
        ExitOnFailure(hr, "Failed to convert existing IP SSL certificate thumbprint to hex for Wix6HttpCertificate '%ls'", wzId);

        hr = StringFromGuid(pSslSet->ParamDesc.AppId, &sczAppId);
        ExitOnFailure(hr, "Failed to copy existing IP SSL AppId for Wix6HttpCertificate '%ls'", wzId);

        wzCertificateStore = pSslSet->ParamDesc.pSslCertStoreName;
    }

    hr = WriteCertificateCaData(ctIpSsl, action, wzId, /*wzHost*/NULL, iPort, iHandleExisting, sczCertificateThumbprint ? sczCertificateThumbprint : L"", sczAppId ? sczAppId : L"", wzCertificateStore ? wzCertificateStore : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write custom action data for IP SSL Wix6HttpCertificate '%ls'", wzId);

LExit:
    ReleaseStr(sczAppId);
    ReleaseStr(sczCertificateThumbprint);

    return hr;
}

static HRESULT AddSniSslCert(
    __in_z LPCWSTR /*wzId*/,
    __in_z LPWSTR wzHost,
    __in int iPort,
    __in BYTE rgbCertificateThumbprint[],
    __in DWORD cbCertificateThumbprint,
    __in GUID* pAppId,
    __in_z LPWSTR wzSslCertStore
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_SSL_SNI_SET set = { };

    SetSniSslCertificateKeyPort(&set.KeyDesc, wzHost, iPort);
    set.ParamDesc.SslHashLength = cbCertificateThumbprint;
    set.ParamDesc.pSslHash = rgbCertificateThumbprint;
    set.ParamDesc.AppId = *pAppId;
    set.ParamDesc.pSslCertStoreName = wzSslCertStore;

    er = ::HttpSetServiceConfiguration(NULL, HttpServiceConfigSslSniCertInfo, &set, sizeof(set), NULL);
    if (ERROR_ALREADY_EXISTS == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

    return hr;
}

static HRESULT AddIpSslCert(
    __in_z LPCWSTR /*wzId*/,
    __in int iPort,
    __in BYTE rgbCertificateThumbprint[],
    __in DWORD cbCertificateThumbprint,
    __in GUID* pAppId,
    __in_z LPWSTR wzSslCertStore
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_SSL_SET set = { };
    SOCKADDR_IN sin = { };

    SetIpSslCertificateKeyPort(&set.KeyDesc, &sin, iPort);
    set.ParamDesc.SslHashLength = cbCertificateThumbprint;
    set.ParamDesc.pSslHash = rgbCertificateThumbprint;
    set.ParamDesc.AppId = *pAppId;
    set.ParamDesc.pSslCertStoreName = wzSslCertStore;

    er = ::HttpSetServiceConfiguration(NULL, HttpServiceConfigSSLCertInfo, &set, sizeof(set), NULL);
    if (ERROR_ALREADY_EXISTS == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

    return hr;
}

static HRESULT RemoveSniSslCert(
    __in_z_opt LPCWSTR /*wzId*/,
    __in_z LPWSTR wzHost,
    __in int iPort
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_SSL_SNI_SET set = { };

    SetSniSslCertificateKeyPort(&set.KeyDesc, wzHost, iPort);

    er = ::HttpDeleteServiceConfiguration(NULL, HttpServiceConfigSslSniCertInfo, &set, sizeof(set), NULL);
    if (ERROR_FILE_NOT_FOUND == er || ERROR_NO_MORE_ITEMS == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

    return hr;
}

static HRESULT RemoveIpSslCert(
    __in_z_opt LPCWSTR /*wzId*/,
    __in int iPort
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_SSL_SET set = { };
    SOCKADDR_IN sin = { };

    SetIpSslCertificateKeyPort(&set.KeyDesc, &sin, iPort);

    er = ::HttpDeleteServiceConfiguration(NULL, HttpServiceConfigSSLCertInfo, &set, sizeof(set), NULL);
    if (ERROR_FILE_NOT_FOUND == er || ERROR_NO_MORE_ITEMS == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

    return hr;
}

static void SetSniSslCertificateKeyPort(
    __in HTTP_SERVICE_CONFIG_SSL_SNI_KEY* pKey,
    __in_z LPWSTR wzHost,
    __in int iPort
)
{
    pKey->Host = wzHost;
    SOCKADDR_IN* pss = reinterpret_cast<SOCKADDR_IN*>(&pKey->IpPort);
    pss->sin_family = AF_INET;
    pss->sin_port = htons(static_cast<USHORT>(iPort));
}

static void SetIpSslCertificateKeyPort(
    __in HTTP_SERVICE_CONFIG_SSL_KEY* pKey,
    __in SOCKADDR_IN* pSin,
    __in int iPort
)
{
    pSin->sin_family = AF_INET;
    pSin->sin_port = htons(static_cast<USHORT>(iPort));
    pKey->pIpPort = reinterpret_cast<PSOCKADDR>(pSin);
}

static HRESULT EnsureAppId(
    __inout_z LPWSTR* psczAppId,
    __in_opt GUID* pGuid
)
{
    HRESULT hr = S_OK;
    GUID guid = { };

    if (!psczAppId || !*psczAppId || !**psczAppId)
    {
        if (pGuid)
        {
            hr = StringFromGuid(*pGuid, psczAppId);
            ExitOnFailure(hr, "Failed to ensure AppId guid");
        }
        else
        {
            hr = HRESULT_FROM_RPC(::UuidCreate(&guid));
            ExitOnRootFailure(hr, "Failed to create guid for AppId");

            hr = StringFromGuid(guid, psczAppId);
            ExitOnFailure(hr, "Failed to ensure AppId guid");
        }
    }

LExit:
    return hr;
}

static HRESULT StringFromGuid(
    __in REFGUID rguid,
    __inout_z LPWSTR* psczGuid
)
{
    HRESULT hr = S_OK;
    WCHAR wzGuid[39];

    if (!::StringFromGUID2(rguid, wzGuid, countof(wzGuid)))
    {
        hr = E_OUTOFMEMORY;
        ExitOnRootFailure(hr, "Failed to convert guid into string");
    }

    hr = StrAllocString(psczGuid, wzGuid, 0);
    ExitOnFailure(hr, "Failed to copy guid");

LExit:
    return hr;
}

static HRESULT WriteCertificateCaData(
    __in eCertificateType certType,
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in_z_opt LPCWSTR wzHost,
    __in int iPort,
    __in int iHandleExisting,
    __in_z LPCWSTR wzCertificateThumbprint,
    __in_z_opt LPCWSTR wzAppId,
    __in_z_opt LPCWSTR wzCertificateStore,
    __inout_z LPWSTR* psczCustomActionData
)
{
    HRESULT hr = S_OK;

    hr = WcaWriteIntegerToCaData(certType, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write IP SSL certificate type to custom action data");

    hr = WcaWriteIntegerToCaData(action, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write action to custom action data");

    hr = WcaWriteStringToCaData(wzId, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write id to custom action data");

    hr = WcaWriteStringToCaData(wzHost ? wzHost : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write Host to custom action data");

    hr = WcaWriteIntegerToCaData(iPort, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write Port to custom action data");

    hr = WcaWriteIntegerToCaData(iHandleExisting, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write HandleExisting to custom action data");

    hr = WcaWriteStringToCaData(wzCertificateThumbprint, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write CertificateThumbprint to custom action data");

    hr = WcaWriteStringToCaData(wzAppId ? wzAppId : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write AppId to custom action data");

    hr = WcaWriteStringToCaData(wzCertificateStore ? wzCertificateStore : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write CertificateStore to custom action data");

LExit:
    return hr;
}

