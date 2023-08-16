// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// prototypes
HRESULT HttpSslCertificateRead(
    __in LPCWSTR wzBindingId,
    __in WCA_WRAPQUERY_HANDLE hSslCertQuery,
    __deref_out_z LPWSTR* ppwzCertificateThumbprint,
    __deref_out_z LPWSTR* ppwzCertificateStore
    );

static UINT SchedHttpSslBindings(
    __in WCA_TODO todoSched
);
static HRESULT WriteExistingSslBinding(
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzHost,
    __in int iPort,
    __in int iHandleExisting,
    __in HTTP_SERVICE_CONFIG_SSL_SET* pSslSet,
    __inout_z LPWSTR* psczCustomActionData
);
static HRESULT WriteSslBinding(
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
    __in_opt HTTP_SERVICE_CONFIG_SSL_SET* pExistingSslSet
);
static HRESULT StringFromGuid(
    __in REFGUID rguid,
    __inout_z LPWSTR* psczGuid
);
static HRESULT AddSslBinding(
    __in_z LPCWSTR wzId,
    __in_z LPWSTR wzHost,
    __in int iPort,
    __in BYTE rgbCertificateThumbprint[],
    __in DWORD cbCertificateThumbprint,
    __in GUID* pAppId,
    __in_z LPWSTR wzSslCertStore
);
static HRESULT GetSslBinding(
    __in_z LPWSTR wzHost,
    __in int nPort,
    __out HTTP_SERVICE_CONFIG_SSL_SET** ppSet
);
static HRESULT RemoveSslBinding(
    __in_z LPCWSTR wzId,
    __in_z LPWSTR wzHost,
    __in int iPort
);
static HRESULT SetSslBindingSetKey(
    __in HTTP_SERVICE_CONFIG_SSL_KEY* pKey,
    __in_z LPWSTR wzHost,
    __in int iPort
);

LPCWSTR vcsWixHttpSslBindingQuery =
L"SELECT `WixHttpSslBinding`, `Host`, `Port`, `Thumbprint`, `AppId`, `Store`, `HandleExisting`, `Component_` "
L"FROM `Wix4HttpSslBinding`";

enum eWixHttpSslBindingQuery { hurqId = 1, hurqHost, hurqPort, hurqCertificateThumbprint, hurqAppId, hurqCertificateStore, hurqHandleExisting, hurqComponent };

LPCWSTR vcsSslCertificateQuery = L"SELECT `Wix4HttpSslCertificate`.`StoreName`, `Wix4HttpSslCertificateHash`.`Hash`, `Wix4HttpSslBindingCertificates`.`Binding_` FROM `Wix4HttpSslCertificate`, `Wix4HttpSslCertificateHash`, `Wix4HttpSslBindingCertificates` WHERE `Wix4HttpSslCertificate`.`Certificate`=`Wix4HttpSslCertificateHash`.`Certificate_` AND `Wix4HttpSslCertificateHash`.`Certificate_`=`Wix4HttpSslBindingCertificates`.`Certificate_`";

enum eSslCertificateQuery { scqStoreName = 1, scqHash, scqBinding };

#define msierrCERTFailedOpen                   26351

/******************************************************************
 SchedWixHttpSslBindingsInstall - immediate custom action entry
   point to prepare adding URL reservations.

********************************************************************/
extern "C" UINT __stdcall SchedHttpSslBindingsInstall(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;

    hr = WcaInitialize(hInstall, "SchedHttpSslBindingsInstall");
    ExitOnFailure(hr, "Failed to initialize");

    hr = SchedHttpSslBindings(WCA_TODO_INSTALL);

LExit:
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}

/******************************************************************
 SchedWixHttpSslBindingsUninstall - immediate custom action entry
   point to prepare removing URL reservations.

********************************************************************/
extern "C" UINT __stdcall SchedHttpSslBindingsUninstall(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;

    hr = WcaInitialize(hInstall, "SchedHttpSslBindingsUninstall");
    ExitOnFailure(hr, "Failed to initialize");

    hr = SchedHttpSslBindings(WCA_TODO_UNINSTALL);

LExit:
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}

/******************************************************************
 ExecHttpSslBindings - deferred custom action entry point to
   register and remove URL reservations.

********************************************************************/
extern "C" UINT __stdcall ExecHttpSslBindings(
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
    WCA_WRAPQUERY_HANDLE hSslCertQuery = NULL;

    BOOL fRollback = ::MsiGetMode(hInstall, MSIRUNMODE_ROLLBACK);
    BOOL fRemove = FALSE;
    BOOL fAdd = FALSE;
    BOOL fFailOnExisting = FALSE;

    GUID guidAppId = { };
    BYTE* pbCertificateThumbprint = NULL;
    DWORD cbCertificateThumbprint = 0;

    //AssertSz(FALSE, "Debug ExecHttpSslBindings() here.");

    // Initialize.
    hr = WcaInitialize(hInstall, "ExecHttpSslBindings");
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

        hr = WcaBeginUnwrapQuery(&hSslCertQuery, &wz);
        ExitOnFailure(hr, "Failed to unwrap ssl certificate query");

        switch (iTodo)
        {
        case WCA_TODO_INSTALL:
        case WCA_TODO_REINSTALL:
            fRemove = heReplace == handleExisting || fRollback;
            fAdd = !fRollback;
            fFailOnExisting = heFail == handleExisting && !fRollback;
            break;

        case WCA_TODO_UNINSTALL:
            fRemove = !fRollback;
            fAdd = fRollback;
            fFailOnExisting = FALSE;
            break;
        }

        if (fRemove)
        {
            hr = RemoveSslBinding(sczId, sczHost, iPort);
            if (S_OK == hr)
            {
                WcaLog(LOGMSG_STANDARD, "Removed SSL certificate binding '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
            }
            else if (FAILED(hr))
            {
                if (fRollback)
                {
                    WcaLogError(hr, "Failed to remove SSL certificate binding to rollback '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
                }
                else
                {
                    ExitOnFailure(hr, "Failed to remove SSL certificate binding '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
                }
            }
        }

        if (fAdd)
        {
            WcaLog(LOGMSG_STANDARD, "Adding SSL certificate binding '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);

            // if we have been provided a thumbprint, then use that
            if (*sczCertificateThumbprint)
            {
            }
            else
            {
                hr = HttpSslCertificateRead(sczId, hSslCertQuery, &sczCertificateThumbprint, &sczCertificateStore);
                ExitOnFailure(hr, "Failed to get SSL Certificate thumbprint.");
            }

            hr = StrAllocHexDecode(sczCertificateThumbprint, &pbCertificateThumbprint, &cbCertificateThumbprint);
            ExitOnFailure(hr, "Failed to convert thumbprint to bytes for SSL certificate binding '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);

            hr = ::IIDFromString(sczAppId, &guidAppId);
            ExitOnFailure(hr, "Failed to convert AppId '%ls' back to GUID for SSL certificate binding '%ls' for hostname: %ls:%d", sczAppId, sczId, sczHost, iPort);

            hr = AddSslBinding(sczId, sczHost, iPort, pbCertificateThumbprint, cbCertificateThumbprint, &guidAppId, sczCertificateStore && *sczCertificateStore ? sczCertificateStore : L"MY");
            if (S_FALSE == hr && fFailOnExisting)
            {
                hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
            }

            if (S_OK == hr)
            {
                WcaLog(LOGMSG_STANDARD, "Added SSL certificate binding '%ls' for hostname: %ls:%d with thumbprint: %ls", sczId, sczHost, iPort, sczCertificateThumbprint);
            }
            else if (FAILED(hr))
            {
                if (fRollback)
                {
                    WcaLogError(hr, "Failed to add SSL certificate binding to rollback '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
                }
                else
                {
                    ExitOnFailure(hr, "Failed to add SSL certificate binding '%ls' for hostname: %ls:%d", sczId, sczHost, iPort);
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
    WcaFinishUnwrapQuery(hSslCertQuery);

    if (fHttpInitialized)
    {
        ::HttpTerminate(HTTP_INITIALIZE_CONFIG, NULL);
    }

    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}

static UINT SchedHttpSslBindings(
    __in WCA_TODO todoSched
)
{
    HRESULT hr = S_OK;
    //UINT er = ERROR_SUCCESS;
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

    HTTP_SERVICE_CONFIG_SSL_SET* pExistingSslSet = NULL;

    //AssertSz(FALSE, "Debug SchedHttpSslBindings() here.");

    // Anything to do?
    hr = WcaTableExists(L"Wix4HttpSslBinding");
    ExitOnFailure(hr, "Failed to check if the Wix4HttpSslBinding table exists");
    if (S_FALSE == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Wix4HttpSslBinding table doesn't exist, so there are no URL reservations to configure");
        ExitFunction();
    }

    // Query and loop through all the SSL certificate bindings.
    hr = WcaOpenExecuteView(vcsWixHttpSslBindingQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on the Wix4HttpSslBinding table");

    hr = HRESULT_FROM_WIN32(::HttpInitialize(HTTPAPI_VERSION_1, HTTP_INITIALIZE_CONFIG, NULL));
    ExitOnFailure(hr, "Failed to initialize HTTP Server configuration");

    fHttpInitialized = TRUE;

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, hurqId, &sczId);
        ExitOnFailure(hr, "Failed to get Wix4HttpSslBinding.WixHttpSslBinding");

        hr = WcaGetRecordString(hRec, hurqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get Wix4HttpSslBinding.Component_");

        // Figure out what we're doing for this reservation, treating reinstall the same as install.
        todoComponent = WcaGetComponentToDo(sczComponent);
        if ((WCA_TODO_REINSTALL == todoComponent ? WCA_TODO_INSTALL : todoComponent) != todoSched)
        {
            WcaLog(LOGMSG_STANDARD, "Component '%ls' action state (%d) doesn't match request (%d) for Wix4HttpSslBinding '%ls'", sczComponent, todoComponent, todoSched, sczId);
            continue;
        }

        hr = WcaGetRecordFormattedString(hRec, hurqHost, &sczHost);
        ExitOnFailure(hr, "Failed to get Wix4HttpSslBinding.Host");

        hr = WcaGetRecordFormattedInteger(hRec, hurqPort, &iPort);
        ExitOnFailure(hr, "Failed to get Wix4HttpSslBinding.Port");

        if (!sczHost || !*sczHost)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Require a Host value for Wix4HttpSslBinding '%ls'", sczId);
        }

        if (!iPort)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Require a Port value for Wix4HttpSslBinding '%ls'", sczId);
        }

        hr = WcaGetRecordFormattedString(hRec, hurqCertificateThumbprint, &sczCertificateThumbprint);
        ExitOnFailure(hr, "Failed to get Wix4HttpSniSslCert.CertificateThumbprint");

        hr = WcaGetRecordFormattedString(hRec, hurqAppId, &sczAppId);
        ExitOnFailure(hr, "Failed to get AppId for Wix4HttpSslBinding '%ls'", sczId);

        hr = WcaGetRecordFormattedString(hRec, hurqCertificateStore, &sczCertificateStore);
        ExitOnFailure(hr, "Failed to get CertificateStore for Wix4HttpSslBinding '%ls'", sczId);

        hr = WcaGetRecordInteger(hRec, hurqHandleExisting, &iHandleExisting);
        ExitOnFailure(hr, "Failed to get HandleExisting for Wix4HttpSslBinding '%ls'", sczId);

        hr = GetSslBinding(sczHost, iPort, &pExistingSslSet);
        ExitOnFailure(hr, "Failed to get the existing SSL certificate for Wix4HttpSslBinding '%ls'", sczId);

        hr = EnsureAppId(&sczAppId, pExistingSslSet);
        ExitOnFailure(hr, "Failed to ensure AppId for Wix4HttpSslBinding '%ls'", sczId);

        hr = WriteExistingSslBinding(todoComponent, sczId, sczHost, iPort, iHandleExisting, pExistingSslSet, &sczRollbackCustomActionData);
        ExitOnFailure(hr, "Failed to write rollback custom action data for Wix4HttpSslBinding '%ls'", sczId);

        hr = WriteSslBinding(todoComponent, sczId, sczHost, iPort, iHandleExisting, sczCertificateThumbprint, sczAppId, sczCertificateStore, &sczCustomActionData);
        ExitOnFailure(hr, "Failed to write custom action data for Wix4HttpSslBinding '%ls'", sczId);
        ++cCertificates;

        ReleaseNullMem(pExistingSslSet);
    }

    // Reaching the end of the list is not an error.
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occurred while processing Wix4HttpSslBinding table");

    // Schedule ExecHttpSslCerts if there's anything to do.
    if (cCertificates)
    {
        WcaLog(LOGMSG_STANDARD, "Scheduling SSL certificate binding (%ls)", sczCustomActionData);
        WcaLog(LOGMSG_STANDARD, "Scheduling rollback SSL certificate binding (%ls)", sczRollbackCustomActionData);

        if (WCA_TODO_INSTALL == todoSched)
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"RollbackHttpSslBindingsInstall"), sczRollbackCustomActionData, cCertificates * COST_HTTP_SSL);
            ExitOnFailure(hr, "Failed to schedule install SSL certificate binding rollback");
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"ExecHttpSslBindingsInstall"), sczCustomActionData, cCertificates * COST_HTTP_SSL);
            ExitOnFailure(hr, "Failed to schedule install SSL certificate binding execution");
        }
        else
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"RollbackHttpSslBindingsUninstall"), sczRollbackCustomActionData, cCertificates * COST_HTTP_SSL);
            ExitOnFailure(hr, "Failed to schedule uninstall SSL certificate binding rollback");
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"ExecHttpSslBindingsUninstall"), sczCustomActionData, cCertificates * COST_HTTP_SSL);
            ExitOnFailure(hr, "Failed to schedule uninstall SSL certificate binding execution");
        }
    }
    else
    {
        WcaLog(LOGMSG_STANDARD, "No SSL certificate bindings scheduled");
    }

LExit:
    ReleaseMem(pExistingSslSet);
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

    return hr;
}

static HRESULT WriteExistingSslBinding(
    __in WCA_TODO action,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzHost,
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
        ExitOnFailure(hr, "Failed to convert existing certificate thumbprint to hex for Wix4HttpSslBinding '%ls'", wzId);

        hr = StringFromGuid(pSslSet->ParamDesc.AppId, &sczAppId);
        ExitOnFailure(hr, "Failed to copy existing AppId for Wix4HttpSslBinding '%ls'", wzId);

        wzCertificateStore = pSslSet->ParamDesc.pSslCertStoreName;
    }

    hr = WriteSslBinding(action, wzId, wzHost, iPort, iHandleExisting, sczCertificateThumbprint ? sczCertificateThumbprint : L"", sczAppId ? sczAppId : L"", wzCertificateStore ? wzCertificateStore : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write custom action data for Wix4HttpSslBinding '%ls'", wzId);

LExit:
    ReleaseStr(sczAppId);
    ReleaseStr(sczCertificateThumbprint);

    return hr;
}

static HRESULT WriteSslBinding(
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

    hr = WcaWriteStringToCaData(wzCertificateThumbprint ? wzCertificateThumbprint : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write CertificateThumbprint to custom action data");

    hr = WcaWriteStringToCaData(wzAppId, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write AppId to custom action data");

    hr = WcaWriteStringToCaData(wzCertificateStore ? wzCertificateStore : L"", psczCustomActionData);
    ExitOnFailure(hr, "Failed to write CertificateStore to custom action data");

    // Wrap vcsSslCertificateQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"Wix4HttpSslCertificate") && S_OK == WcaTableExists(L"Wix4HttpSslCertificateHash") && S_OK == WcaTableExists(L"Wix4HttpSslBindingCertificates"))
    {
        hr = WcaWrapQuery(vcsSslCertificateQuery, psczCustomActionData, 0, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap SslCertificate query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(psczCustomActionData);
        ExitOnFailure(hr, "Failed to wrap SslCertificate empty query");
    }

LExit:
    return hr;
}

static HRESULT EnsureAppId(
    __inout_z LPWSTR* psczAppId,
    __in_opt HTTP_SERVICE_CONFIG_SSL_SET* pExistingSslSet
)
{
    HRESULT hr = S_OK;
    RPC_STATUS rs = RPC_S_OK;
    GUID guid = { };

    if (!psczAppId || !*psczAppId || !**psczAppId)
    {
        if (pExistingSslSet)
        {
            hr = StringFromGuid(pExistingSslSet->ParamDesc.AppId, psczAppId);
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

static HRESULT AddSslBinding(
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
    HTTP_SERVICE_CONFIG_SSL_SET set = { };
    SOCKADDR_STORAGE addr = { };

    set.KeyDesc.pIpPort = reinterpret_cast<PSOCKADDR>(&addr);
    SetSslBindingSetKey(&set.KeyDesc, wzHost, iPort);
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

static HRESULT GetSslBinding(
    __in_z LPWSTR wzHost,
    __in int nPort,
    __out HTTP_SERVICE_CONFIG_SSL_SET** ppSet
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_SSL_QUERY query = { };
    HTTP_SERVICE_CONFIG_SSL_SET* pSet = NULL;
    ULONG cbSet = 0;
    SOCKADDR_STORAGE addr = { };

    *ppSet = NULL;

    query.QueryDesc = HttpServiceConfigQueryExact;
    query.KeyDesc.pIpPort = reinterpret_cast<PSOCKADDR>(&addr);
    SetSslBindingSetKey(&query.KeyDesc, wzHost, nPort);

    er = ::HttpQueryServiceConfiguration(NULL, HttpServiceConfigSSLCertInfo, &query, sizeof(query), pSet, cbSet, &cbSet, NULL);
    if (ERROR_INSUFFICIENT_BUFFER == er)
    {
        pSet = reinterpret_cast<HTTP_SERVICE_CONFIG_SSL_SET*>(MemAlloc(cbSet, TRUE));
        ExitOnNull(pSet, hr, E_OUTOFMEMORY, "Failed to allocate query SSL certificate buffer");

        er = ::HttpQueryServiceConfiguration(NULL, HttpServiceConfigSSLCertInfo, &query, sizeof(query), pSet, cbSet, &cbSet, NULL);
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

static HRESULT RemoveSslBinding(
    __in_z LPCWSTR /*wzId*/,
    __in_z LPWSTR wzHost,
    __in int iPort
)
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_SSL_SET set = { };
    SOCKADDR_STORAGE addr = { };

    set.KeyDesc.pIpPort = reinterpret_cast<PSOCKADDR>(&addr);
    SetSslBindingSetKey(&set.KeyDesc, wzHost, iPort);

    er = ::HttpDeleteServiceConfiguration(NULL, HttpServiceConfigSSLCertInfo, &set, sizeof(set), NULL);
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

static HRESULT SetSslBindingSetKey(
    __in HTTP_SERVICE_CONFIG_SSL_KEY* pKey,
    __in_z LPWSTR wzHost,
    __in int iPort
)
{
    DWORD er = ERROR_SUCCESS;

    SOCKADDR_IN* pss = reinterpret_cast<SOCKADDR_IN*>(pKey->pIpPort);
    pss->sin_family = AF_INET;
    pss->sin_port = htons(static_cast<USHORT>(iPort));
    if (!InetPtonW(AF_INET, wzHost, &pss->sin_addr))
    {
        er = WSAGetLastError();
    }

    HRESULT hr = HRESULT_FROM_WIN32(er);
    return hr;
}


HRESULT HttpSslCertificateRead(
    __in LPCWSTR wzBindingId,
    __in WCA_WRAPQUERY_HANDLE hSslCertQuery,
    __deref_out_z LPWSTR* ppwzCertificateThumbprint,
    __deref_out_z LPWSTR* ppwzCertificateStore
    )
{
    HRESULT hr = S_OK;

    MSIHANDLE hRec;
    LPWSTR pwzData = NULL;

    WcaFetchWrappedReset(hSslCertQuery);

    // Get the certificate information.
    while (S_OK == (hr = WcaFetchWrappedRecordWhereString(hSslCertQuery, scqBinding, wzBindingId, &hRec)))
    {
        hr = WcaGetRecordString(hRec, scqStoreName, &pwzData);
        ExitOnFailure(hr, "Failed to get http ssl certificate store name.");

        hr = StrAllocString(ppwzCertificateStore, pwzData, 0);
        ExitOnFailure(hr, "Failed to copy certificate store name");

        hr = WcaGetRecordString(hRec, scqHash, &pwzData);
        ExitOnFailure(hr, "Failed to get hash for http ssl certificate.");

        hr = StrAllocString(ppwzCertificateThumbprint, pwzData, 0);
        ExitOnFailure(hr, "Failed to copy http ssl certificate thumbprint.");

        // only one
        break;
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to read HttpSslBindingCertificates table.");

LExit:
    ReleaseStr(pwzData);
    return hr;
}

