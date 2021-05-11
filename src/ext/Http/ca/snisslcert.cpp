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

static UINT SchedHttpSniSslCerts(
    __in WCA_TODO todoSched
);
static HRESULT WriteExistingSniSslCert(
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzHost,
    __in int iPort,
    __in int iHandleExisting,
    __in HTTP_SERVICE_CONFIG_SSL_SNI_SET* pSniSslSet,
    __inout_z LPWSTR* psczCustomActionData
);
static HRESULT WriteSniSslCert(
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzHost,
    __in int iPort,
    __in int iHandleExisting,
    __in_z LPCWSTR wzCertificateThumbprint,
    __in_z LPCWSTR wzAppId,
    __in_z_opt LPCWSTR wzCertificateStore,
    __inout_z LPWSTR* psczCustomActionData
);
static HRESULT EnsureAppId(
    __inout_z LPWSTR* psczAppId,
    __in_opt HTTP_SERVICE_CONFIG_SSL_SNI_SET* pExistingSniSslSet
);
static HRESULT StringFromGuid(
    __in REFGUID rguid,
    __inout_z LPWSTR* psczGuid
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
static HRESULT GetSniSslCert(
    __in_z LPWSTR wzHost,
    __in int nPort,
    __out HTTP_SERVICE_CONFIG_SSL_SNI_SET** ppSet
);
static HRESULT RemoveSniSslCert(
    __in_z LPCWSTR wzId,
    __in_z LPWSTR wzHost,
    __in int iPort
);
static void SetSniSslCertSetKey(
    __in HTTP_SERVICE_CONFIG_SSL_SNI_KEY* pKey,
    __in_z LPWSTR wzHost,
    __in int iPort
);


LPCWSTR vcsWixHttpSniSslCertQuery =
L"SELECT `Wix4HttpSniSslCert`.`Wix4HttpSniSslCert`, `Wix4HttpSniSslCert`.`Host`, `Wix4HttpSniSslCert`.`Port`, `Wix4HttpSniSslCert`.`Thumbprint`, `Wix4HttpSniSslCert`.`AppId`, `Wix4HttpSniSslCert`.`Store`, `Wix4HttpSniSslCert`.`HandleExisting`, `Wix4HttpSniSslCert`.`Component_` "
L"FROM `Wix4HttpSniSslCert`";
enum eWixHttpSniSslCertQuery { hurqId = 1, hurqHost, hurqPort, hurqCertificateThumbprint, hurqAppId, hurqCertificateStore, hurqHandleExisting, hurqComponent };

/******************************************************************
 SchedWixHttpSniSslCertsInstall - immediate custom action entry
   point to prepare adding URL reservations.

********************************************************************/
extern "C" UINT __stdcall SchedHttpSniSslCertsInstall(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;

    hr = WcaInitialize(hInstall, "SchedHttpSniSslCertsInstall");
    ExitOnFailure(hr, "Failed to initialize");

    hr = SchedHttpSniSslCerts(WCA_TODO_INSTALL);

LExit:
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}

/******************************************************************
 SchedWixHttpSniSslCertsUninstall - immediate custom action entry
   point to prepare removing URL reservations.

********************************************************************/
extern "C" UINT __stdcall SchedHttpSniSslCertsUninstall(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;

    hr = WcaInitialize(hInstall, "SchedHttpSniSslCertsUninstall");
    ExitOnFailure(hr, "Failed to initialize");

    hr = SchedHttpSniSslCerts(WCA_TODO_UNINSTALL);

LExit:
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}

/******************************************************************
 ExecHttpSniSslCerts - deferred custom action entry point to
   register and remove URL reservations.

********************************************************************/
extern "C" UINT __stdcall ExecHttpSniSslCerts(
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
    hr = WcaInitialize(hInstall, "ExecHttpSniSslCerts");
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
            hr = RemoveSniSslCert(sczId, sczHost, iPort);
            if (S_OK == hr)
            {
                WcaLog(LOGMSG_STANDARD, "Removed SNI SSL certificate '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
            }
            else if (FAILED(hr))
            {
                if (fRollback)
                {
                    WcaLogError(hr, "Failed to remove SNI SSL certificate to rollback '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
                }
                else
                {
                    ExitOnFailure(hr, "Failed to remove SNI SSL certificate '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
                }
            }
        }

        if (fAdd)
        {
            WcaLog(LOGMSG_STANDARD, "Adding SNI SSL certificate '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);

            hr = StrAllocHexDecode(sczCertificateThumbprint, &pbCertificateThumbprint, &cbCertificateThumbprint);
            ExitOnFailure(hr, "Failed to convert thumbprint to bytes for SNI SSL certificate '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);

            hr = ::IIDFromString(sczAppId, &guidAppId);
            ExitOnFailure(hr, "Failed to convert AppId '%ls' back to GUID for SNI SSL certificate '%ls' for hostname: %ls:%d", sczAppId, sczId, sczHost, iPort);

            hr = AddSniSslCert(sczId, sczHost, iPort, pbCertificateThumbprint, cbCertificateThumbprint, &guidAppId, sczCertificateStore && *sczCertificateStore ? sczCertificateStore : L"MY");
            if (S_FALSE == hr && fFailOnExisting)
            {
                hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
            }

            if (S_OK == hr)
            {
                WcaLog(LOGMSG_STANDARD, "Added SNI SSL certificate '%ls' for hostname: %ls:%d with thumbprint: %ls", sczId, sczHost, iPort, sczCertificateThumbprint);
            }
            else if (FAILED(hr))
            {
                if (fRollback)
                {
                    WcaLogError(hr, "Failed to add SNI SSL certificate to rollback '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
                }
                else
                {
                    ExitOnFailure(hr, "Failed to add SNI SSL certificate '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
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

static UINT SchedHttpSniSslCerts(
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
    WCA_TODO todoComponent = WCA_TODO_UNKNOWN;
    LPWSTR sczHost = NULL;
    int iPort = 0;
    LPWSTR sczCertificateThumbprint = NULL;
    LPWSTR sczAppId = NULL;
    LPWSTR sczCertificateStore = NULL;
    int iHandleExisting = 0;

    HTTP_SERVICE_CONFIG_SSL_SNI_SET* pExistingSniSslSet = NULL;

    // Anything to do?
    hr = WcaTableExists(L"Wix4HttpSniSslCert");
    ExitOnFailure(hr, "Failed to check if the Wix4HttpSniSslCert table exists");
    if (S_FALSE == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Wix4HttpSniSslCert table doesn't exist, so there are no URL reservations to configure");
        ExitFunction();
    }

    // Query and loop through all the SNI SSL certificates.
    hr = WcaOpenExecuteView(vcsWixHttpSniSslCertQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on the Wix4HttpSniSslCert table");

    hr = HRESULT_FROM_WIN32(::HttpInitialize(HTTPAPI_VERSION_1, HTTP_INITIALIZE_CONFIG, NULL));
    ExitOnFailure(hr, "Failed to initialize HTTP Server configuration");

    fHttpInitialized = TRUE;

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, hurqId, &sczId);
        ExitOnFailure(hr, "Failed to get Wix4HttpSniSslCert.Wix4HttpSniSslCert");

        hr = WcaGetRecordString(hRec, hurqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get Wix4HttpSniSslCert.Component_");

        // Figure out what we're doing for this reservation, treating reinstall the same as install.
        todoComponent = WcaGetComponentToDo(sczComponent);
        if ((WCA_TODO_REINSTALL == todoComponent ? WCA_TODO_INSTALL : todoComponent) != todoSched)
        {
            WcaLog(LOGMSG_STANDARD, "Component '%ls' action state (%d) doesn't match request (%d) for Wix4HttpSniSslCert '%ls'", sczComponent, todoComponent, todoSched, sczId);
            continue;
        }

        hr = WcaGetRecordFormattedString(hRec, hurqHost, &sczHost);
        ExitOnFailure(hr, "Failed to get Wix4HttpSniSslCert.Host");

        hr = WcaGetRecordFormattedInteger(hRec, hurqPort, &iPort);
        ExitOnFailure(hr, "Failed to get Wix4HttpSniSslCert.Port");

        hr = WcaGetRecordFormattedString(hRec, hurqCertificateThumbprint, &sczCertificateThumbprint);
        ExitOnFailure(hr, "Failed to get Wix4HttpSniSslCert.CertificateThumbprint");

        if (!sczHost || !*sczHost)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Require a Host value for Wix4HttpSniSslCert '%ls'", sczId);
        }

        if (!iPort)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Require a Port value for Wix4HttpSniSslCert '%ls'", sczId);
        }

        if (!sczCertificateThumbprint || !*sczCertificateThumbprint)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Require a CertificateThumbprint value for Wix4HttpSniSslCert '%ls'", sczId);
        }

        hr = WcaGetRecordFormattedString(hRec, hurqAppId, &sczAppId);
        ExitOnFailure(hr, "Failed to get AppId for Wix4HttpSniSslCert '%ls'", sczId);

        hr = WcaGetRecordFormattedString(hRec, hurqCertificateStore, &sczCertificateStore);
        ExitOnFailure(hr, "Failed to get CertificateStore for Wix4HttpSniSslCert '%ls'", sczId);

        hr = WcaGetRecordInteger(hRec, hurqHandleExisting, &iHandleExisting);
        ExitOnFailure(hr, "Failed to get HandleExisting for Wix4HttpSniSslCert '%ls'", sczId);

        hr = GetSniSslCert(sczHost, iPort, &pExistingSniSslSet);
        ExitOnFailure(hr, "Failed to get the existing SNI SSL certificate for Wix4HttpSniSslCert '%ls'", sczId);

        hr = EnsureAppId(&sczAppId, pExistingSniSslSet);
        ExitOnFailure(hr, "Failed to ensure AppId for Wix4HttpSniSslCert '%ls'", sczId);

        hr = WriteExistingSniSslCert(todoComponent, sczId, sczHost, iPort, iHandleExisting, pExistingSniSslSet, &sczRollbackCustomActionData);
        ExitOnFailure(hr, "Failed to write rollback custom action data for Wix4HttpSniSslCert '%ls'", sczId);

        hr = WriteSniSslCert(todoComponent, sczId, sczHost, iPort, iHandleExisting, sczCertificateThumbprint, sczAppId, sczCertificateStore, &sczCustomActionData);
        ExitOnFailure(hr, "Failed to write custom action data for Wix4HttpSniSslCert '%ls'", sczId);
        ++cCertificates;

        ReleaseNullMem(pExistingSniSslSet);
    }

    // Reaching the end of the list is not an error.
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occurred while processing Wix4HttpSniSslCert table");

    // Schedule ExecHttpSniSslCerts if there's anything to do.
    if (cCertificates)
    {
        WcaLog(LOGMSG_STANDARD, "Scheduling SNI SSL certificate (%ls)", sczCustomActionData);
        WcaLog(LOGMSG_STANDARD, "Scheduling rollback SNI SSL certificate (%ls)", sczRollbackCustomActionData);

        if (WCA_TODO_INSTALL == todoSched)
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"WixRollbackHttpSniSslCertsInstall"), sczRollbackCustomActionData, cCertificates * COST_HTTP_SNI_SSL);
            ExitOnFailure(hr, "Failed to schedule install SNI SSL certificate rollback");
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"WixExecHttpSniSslCertsInstall"), sczCustomActionData, cCertificates * COST_HTTP_SNI_SSL);
            ExitOnFailure(hr, "Failed to schedule install SNI SSL certificate execution");
        }
        else
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"WixRollbackHttpSniSslCertsUninstall"), sczRollbackCustomActionData, cCertificates * COST_HTTP_SNI_SSL);
            ExitOnFailure(hr, "Failed to schedule uninstall SNI SSL certificate rollback");
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"WixExecHttpSniSslCertsUninstall"), sczCustomActionData, cCertificates * COST_HTTP_SNI_SSL);
            ExitOnFailure(hr, "Failed to schedule uninstall SNI SSL certificate execution");
        }
    }
    else
    {
        WcaLog(LOGMSG_STANDARD, "No SNI SSL certificates scheduled");
    }

LExit:
    ReleaseMem(pExistingSniSslSet);
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

static HRESULT WriteExistingSniSslCert(
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
        ExitOnFailure(hr, "Failed to convert existing certificate thumbprint to hex for Wix4HttpSniSslCert '%ls'", wzId);

        hr = StringFromGuid(pSniSslSet->ParamDesc.AppId, &sczAppId);
        ExitOnFailure(hr, "Failed to copy existing AppId for Wix4HttpSniSslCert '%ls'", wzId);

        wzCertificateStore = pSniSslSet->ParamDesc.pSslCertStoreName;
    }

    hr = WriteSniSslCert(action, wzId, wzHost, iPort, iHandleExisting, sczCertificateThumbprint ? sczCertificateThumbprint : L"", sczAppId ? sczAppId : L"", wzCertificateStore ? wzCertificateStore : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write custom action data for Wix4HttpSniSslCert '%ls'", wzId);

LExit:
    ReleaseStr(sczAppId);
    ReleaseStr(sczCertificateThumbprint);

    return hr;
}

static HRESULT WriteSniSslCert(
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzHost,
    __in int iPort,
    __in int iHandleExisting,
    __in_z LPCWSTR wzCertificateThumbprint,
    __in_z LPCWSTR wzAppId,
    __in_z_opt LPCWSTR wzCertificateStore,
    __inout_z LPWSTR* psczCustomActionData
)
{
    HRESULT hr = S_OK;

    hr = WcaWriteIntegerToCaData(action, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write action to custom action data");

    hr = WcaWriteStringToCaData(wzId, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write id to custom action data");

    hr = WcaWriteStringToCaData(wzHost, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write Host to custom action data");

    hr = WcaWriteIntegerToCaData(iPort, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write Port to custom action data");

    hr = WcaWriteIntegerToCaData(iHandleExisting, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write HandleExisting to custom action data");

    hr = WcaWriteStringToCaData(wzCertificateThumbprint, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write CertificateThumbprint to custom action data");

    hr = WcaWriteStringToCaData(wzAppId, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write AppId to custom action data");

    hr = WcaWriteStringToCaData(wzCertificateStore ? wzCertificateStore : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write CertificateStore to custom action data");

LExit:
    return hr;
}

static HRESULT EnsureAppId(
    __inout_z LPWSTR* psczAppId,
    __in_opt HTTP_SERVICE_CONFIG_SSL_SNI_SET* pExistingSniSslSet
)
{
    HRESULT hr = S_OK;
    RPC_STATUS rs = RPC_S_OK;
    GUID guid = { };

    if (!psczAppId || !*psczAppId || !**psczAppId)
    {
        if (pExistingSniSslSet)
        {
            hr = StringFromGuid(pExistingSniSslSet->ParamDesc.AppId, psczAppId);
            ExitOnFailure(hr, "Failed to ensure AppId guid");
        }
        else
        {
            rs = ::UuidCreate(&guid);
            hr = HRESULT_FROM_RPC(rs);
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

    SetSniSslCertSetKey(&set.KeyDesc, wzHost, iPort);
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

static HRESULT GetSniSslCert(
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
    SetSniSslCertSetKey(&query.KeyDesc, wzHost, nPort);

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
    else if (ERROR_FILE_NOT_FOUND == er)
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

static HRESULT RemoveSniSslCert(
    __in_z LPCWSTR /*wzId*/,
    __in_z LPWSTR wzHost,
    __in int iPort
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_SSL_SNI_SET set = { };

    SetSniSslCertSetKey(&set.KeyDesc, wzHost, iPort);

    er = ::HttpDeleteServiceConfiguration(NULL, HttpServiceConfigSslSniCertInfo, &set, sizeof(set), NULL);
    if (ERROR_FILE_NOT_FOUND == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

    return hr;
}

static void SetSniSslCertSetKey(
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
