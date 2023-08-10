// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// prototypes
static HRESULT ConfigureCertificates(
    __in SCA_ACTION saAction
    );

static HRESULT FindExistingCertificate(
    __in LPCWSTR wzName,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzStore,
    __out BYTE** prgbCertificate,
    __out DWORD* pcbCertificate
    );

static HRESULT ResolveCertificate(
    __in LPCWSTR wzId,
    __in LPCWSTR wzName,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzStoreName,
    __in DWORD dwAttributess,
    __in LPCWSTR wzData,
    __in LPCWSTR wzPFXPassword,
    __out BYTE** ppbCertificate,
    __out DWORD* pcbCertificate
    );

static HRESULT ReadCertificateFile(
    __in LPCWSTR wzPath,
    __out BYTE** prgbData,
    __out DWORD* pcbData
    );

static HRESULT CertificateToHash(
    __in BYTE* pbCertificate,
    __in DWORD cbCertificate,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzPFXPassword,
    __in BYTE rgbHash[],
    __in DWORD cbHash
    );

LPCWSTR vcsCertQuery = L"SELECT `Certificate`, `Name`, `Component_`, `StoreLocation`, `StoreName`, `Attributes`, `Binary_`, `CertificatePath`, `PFXPassword` FROM `Wix4HttpSslCertificate`";
enum eCertQuery { cqCertificate = 1, cqName, cqComponent, cqStoreLocation, cqStoreName, cqAttributes, cqCertificateBinary, cqCertificatePath, cqPFXPassword };

/********************************************************************
InstallHttpCertificates - CUSTOM ACTION ENTRY POINT for installing
                      certificates

********************************************************************/
extern "C" UINT __stdcall InstallHttpCertificates(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    // initialize
    hr = WcaInitialize(hInstall, "InstallHttpCertificates");
    ExitOnFailure(hr, "Failed to initialize");

    hr = ConfigureCertificates(SCA_ACTION_INSTALL);

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
UninstallHttpCertificates - CUSTOM ACTION ENTRY POINT for uninstalling
                        certificates

********************************************************************/
extern "C" UINT __stdcall UninstallHttpCertificates(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    // initialize
    hr = WcaInitialize(hInstall, "UninstallHttpCertificates");
    ExitOnFailure(hr, "Failed to initialize");

    hr = ConfigureCertificates(SCA_ACTION_UNINSTALL);

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

static HRESULT ConfigureCertificates(
    __in SCA_ACTION saAction
    )
{
    //AssertSz(FALSE, "debug ConfigureCertificates().");

    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    PMSIHANDLE hViewCertificate;
    PMSIHANDLE hRecCertificate;
    INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
    INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;

    WCHAR* pwzId = NULL;
    WCHAR* pwzName = NULL;
    WCHAR* pwzComponent = NULL;
    int iData = 0;
    DWORD dwStoreLocation = 0;
    LPWSTR pwzStoreName = 0;
    DWORD dwAttributes = 0;
    WCHAR* pwzData = NULL;
    WCHAR* pwzPFXPassword = NULL;
    WCHAR* pwzCaData = NULL;
    WCHAR* pwzRollbackCaData = NULL;

    BYTE* pbCertificate = NULL;
    DWORD cbCertificate = 0;
    DWORD_PTR cbPFXPassword = 0;

    // Bail quickly if the Certificate table isn't around.
    if (S_OK != WcaTableExists(L"Wix4HttpSslCertificate"))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigureCertificates() - required table not present.");
        ExitFunction1(hr = S_FALSE);
    }

    // Process the Certificate table.
    hr = WcaOpenExecuteView(vcsCertQuery, &hViewCertificate);
    ExitOnFailure(hr, "failed to open view on Certificate table");

    while (SUCCEEDED(hr = WcaFetchRecord(hViewCertificate, &hRecCertificate)))
    {
        hr = WcaGetRecordString(hRecCertificate, cqCertificate, &pwzId); // the id is just useful to have up front
        ExitOnFailure(hr, "failed to get Certificate.Certificate");

        hr = WcaGetRecordString(hRecCertificate, cqComponent, &pwzComponent);
        ExitOnFailure(hr, "failed to get Certificate.Component_");

        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzComponent, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get state for component: %ls", pwzComponent);

        if (!(WcaIsInstalling(isInstalled, isAction) && SCA_ACTION_INSTALL == saAction) &&
            !(WcaIsUninstalling(isInstalled, isAction) && SCA_ACTION_UNINSTALL == saAction) &&
            !(WcaIsReInstalling(isInstalled, isAction)))
        {
            WcaLog(LOGMSG_VERBOSE, "Skipping non-action certificate: %ls", pwzId);
            continue;
        }

        // extract the rest of the data from the Certificate table
        hr = WcaGetRecordFormattedString(hRecCertificate, cqName, &pwzName);
        ExitOnFailure(hr, "failed to get Certificate.Name");

        hr = WcaGetRecordInteger(hRecCertificate, cqStoreLocation, &iData);
        ExitOnFailure(hr, "failed to get Certificate.StoreLocation");

        switch (iData)
        {
        case SCA_CERTSYSTEMSTORE_CURRENTUSER:
            dwStoreLocation = CERT_SYSTEM_STORE_CURRENT_USER;
            break;
        case SCA_CERTSYSTEMSTORE_LOCALMACHINE:
            dwStoreLocation = CERT_SYSTEM_STORE_LOCAL_MACHINE;
            break;
        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Invalid store location value: %d", iData);
        }

        hr = WcaGetRecordString(hRecCertificate, cqStoreName, &pwzStoreName);
        ExitOnFailure(hr, "failed to get Certificate.StoreName");

        hr = WcaGetRecordInteger(hRecCertificate, cqAttributes, reinterpret_cast<int*>(&dwAttributes));
        ExitOnFailure(hr, "failed to get Certificate.Attributes");

        if (dwAttributes & SCA_CERT_ATTRIBUTE_BINARYDATA)
        {
            hr = WcaGetRecordString(hRecCertificate, cqCertificateBinary, &pwzData);
            ExitOnFailure(hr, "failed to get Certificate.Binary_");
        }
        else
        {
            hr = WcaGetRecordFormattedString(hRecCertificate, cqCertificatePath, &pwzData);
            ExitOnFailure(hr, "failed to get Certificate.CertificatePath");
        }

        hr = WcaGetRecordFormattedString(hRecCertificate, cqPFXPassword, &pwzPFXPassword);
        ExitOnFailure(hr, "failed to get Certificate.PFXPassword");

        // Write the common data (for both install and uninstall) to the CustomActionData
        // to pass data to the deferred CustomAction.
        hr = StrAllocString(&pwzCaData, pwzName, 0);
        ExitOnFailure(hr, "Failed to pass Certificate.Certificate to deferred CustomAction.");
        hr = WcaWriteStringToCaData(pwzStoreName, &pwzCaData);
        ExitOnFailure(hr, "Failed to pass Certificate.StoreName to deferred CustomAction.");
        hr = WcaWriteIntegerToCaData(dwAttributes, &pwzCaData);
        ExitOnFailure(hr, "Failed to pass Certificate.Attributes to deferred CustomAction.");

        // Copy the rollback data from the deferred data because it's the same up to this point.
        hr = StrAllocString(&pwzRollbackCaData, pwzCaData, 0);
        ExitOnFailure(hr, "Failed to allocate string for rollback CustomAction.");

        // Finally, schedule the correct deferred CustomAction to actually do work.
        LPCWSTR wzAction = NULL;
        LPCWSTR wzRollbackAction = NULL;
        DWORD dwCost = 0;
        if (SCA_ACTION_UNINSTALL == saAction)
        {
            // Find an existing certificate one (if there is one) to so we have it for rollback.
            hr = FindExistingCertificate(pwzName, dwStoreLocation, pwzStoreName, &pbCertificate, &cbCertificate);
            ExitOnFailure(hr, "Failed to search for existing certificate with friendly name: %ls", pwzName);

            if (pbCertificate)
            {
                hr = WcaWriteStreamToCaData(pbCertificate, cbCertificate, &pwzRollbackCaData);
                ExitOnFailure(hr, "Failed to pass Certificate.Data to rollback CustomAction.");

                hr = WcaWriteStringToCaData(pwzPFXPassword, &pwzRollbackCaData);
                ExitOnFailure(hr, "Failed to pass Certificate.PFXPassword to rollback CustomAction.");

                hr = WcaWriteIntegerToCaData(dwAttributes, &pwzCaData);
                ExitOnFailure(hr, "Failed to pass Certificate.Attributes to deferred CustomAction.");
            }

            // Pick the right action to run based on what store we're uninstalling from.
            if (CERT_SYSTEM_STORE_LOCAL_MACHINE == dwStoreLocation)
            {
                wzAction = CUSTOM_ACTION_DECORATION(L"DeleteMachineHttpCertificate");
                if (pbCertificate)
                {
                    wzRollbackAction = L"RollbackDeleteMachineHttpCertificate";
                }
            }
            else
            {
                wzAction = CUSTOM_ACTION_DECORATION(L"DeleteUserHttpCertificate");
                if (pbCertificate)
                {
                    wzRollbackAction = L"RollbackDeleteUserHttpCertificate";
                }
            }
            dwCost = COST_CERT_DELETE;
        }
        else
        {
            // Actually get the certificate, resolve it to a blob, and get the blob's hash.
            hr = ResolveCertificate(pwzId, pwzName, dwStoreLocation, pwzStoreName, dwAttributes, pwzData, pwzPFXPassword, &pbCertificate, &cbCertificate);
            ExitOnFailure(hr, "Failed to resolve certificate: %ls", pwzId);

            hr = WcaWriteStreamToCaData(pbCertificate, cbCertificate, &pwzCaData);
            ExitOnFailure(hr, "Failed to pass Certificate.Data to deferred CustomAction.");

            hr = WcaWriteStringToCaData(pwzPFXPassword, &pwzCaData);
            ExitOnFailure(hr, "Failed to pass Certificate.PFXPassword to deferred CustomAction.");

            // Pick the right action to run based on what store we're installing into.
            if (CERT_SYSTEM_STORE_LOCAL_MACHINE == dwStoreLocation)
            {
                wzAction = CUSTOM_ACTION_DECORATION(L"AddMachineHttpCertificate");
                wzRollbackAction = CUSTOM_ACTION_DECORATION(L"RollbackAddMachineHttpCertificate");
            }
            else
            {
                wzAction = CUSTOM_ACTION_DECORATION(L"AddUserHttpCertificate");
                wzRollbackAction = CUSTOM_ACTION_DECORATION(L"RollbackAddUserHttpCertificate");
            }
            dwCost = COST_CERT_ADD;
        }

        if (wzRollbackAction)
        {
            hr = WcaDoDeferredAction(wzRollbackAction, pwzRollbackCaData, dwCost);
            ExitOnFailure(hr, "Failed to schedule rollback certificate action '%ls' for: %ls", wzRollbackAction, pwzId);
        }

        hr = WcaDoDeferredAction(wzAction, pwzCaData, dwCost);
        ExitOnFailure(hr, "Failed to schedule certificate action '%ls' for: %ls", wzAction, pwzId);

        // Clean up for the next certificate.
        ReleaseNullMem(pbCertificate);
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }

LExit:
    if (NULL != pwzPFXPassword && SUCCEEDED(StrSize(pwzPFXPassword, &cbPFXPassword)))
    {
        SecureZeroMemory(pwzPFXPassword, cbPFXPassword);
    }

    ReleaseMem(pbCertificate);
    ReleaseStr(pwzCaData);
    ReleaseStr(pwzPFXPassword);
    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzStoreName);
    ReleaseStr(pwzComponent);
    ReleaseStr(pwzId);

    return hr;
}

static HRESULT ResolveCertificate(
    __in LPCWSTR wzId,
    __in LPCWSTR /*wzName*/,
    __in DWORD dwStoreLocation,
    __in LPCWSTR /*wzStoreName*/,
    __in DWORD dwAttributes,
    __in LPCWSTR wzData,
    __in LPCWSTR wzPFXPassword,
    __out BYTE** ppbCertificate,
    __out DWORD* pcbCertificate
    )
{
    HRESULT hr = S_OK;

    LPWSTR pwzSql = NULL;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;
    MSIHANDLE hCertificateHashView = NULL;
    MSIHANDLE hCertificateHashColumns = NULL;

    BYTE rgbCertificateHash[CB_CERTIFICATE_HASH] = { 0 };
    WCHAR wzEncodedCertificateHash[CB_CERTIFICATE_HASH * 2 + 1] = { 0 };

    PMSIHANDLE hViewCertificateRequest, hRecCertificateRequest;

    WCHAR* pwzDistinguishedName = NULL;
    WCHAR* pwzCA = NULL;

    BYTE* pbData = NULL;
    DWORD cbData = 0;

    if (dwAttributes & SCA_CERT_ATTRIBUTE_REQUEST)
    {
        hr = E_NOTIMPL;
        ExitOnFailure(hr, "Installing certificates by requesting them from a certificate authority is not currently supported");
    }
    else if (dwAttributes & SCA_CERT_ATTRIBUTE_BINARYDATA)
    {
        // get the binary stream in Binary
        hr = WcaTableExists(L"Binary");
        if (S_OK != hr)
        {
            if (SUCCEEDED(hr))
            {
                hr = E_UNEXPECTED;
            }
            ExitOnFailure(hr, "Binary was referenced but there is no Binary table.");
        }

        hr = StrAllocFormatted(&pwzSql, L"SELECT `Data` FROM `Binary` WHERE `Name`=\'%s\'", wzData);
        ExitOnFailure(hr, "Failed to allocate Binary table query.");

        hr = WcaOpenExecuteView(pwzSql, &hView);
        ExitOnFailure(hr, "Failed to open view on Binary table");

        hr = WcaFetchSingleRecord(hView, &hRec);
        ExitOnFailure(hr, "Failed to retrieve request from Binary table");

        hr = WcaGetRecordStream(hRec, 1, &pbData, &cbData);
        ExitOnFailure(hr, "Failed to ready Binary.Data for certificate.");
    }
    else if (dwAttributes == SCA_CERT_ATTRIBUTE_DEFAULT)
    {
        hr = ReadCertificateFile(wzData, &pbData, &cbData);
        ExitOnFailure(hr, "Failed to read certificate from file path.");
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid Certificate.Attributes.");
    }

    // If we have loaded a certificate, update the Certificate.Hash column.
    if (pbData)
    {
        hr = CertificateToHash(pbData, cbData, dwStoreLocation, wzPFXPassword, rgbCertificateHash, countof(rgbCertificateHash));
        ExitOnFailure(hr, "Failed to get SHA1 hash of certificate.");

        hr = StrHexEncode(rgbCertificateHash, countof(rgbCertificateHash), wzEncodedCertificateHash, countof(wzEncodedCertificateHash));
        ExitOnFailure(hr, "Failed to hex encode SHA1 hash of certificate.");

        // Update the Wix4HttpSslCertificateHash table.
        hr = WcaAddTempRecord(&hCertificateHashView, &hCertificateHashColumns, L"Wix4HttpSslCertificateHash", NULL, 0, 2, wzId, wzEncodedCertificateHash);
        ExitOnFailure(hr, "Failed to add encoded hash for certificate: %ls", wzId);
    }

    *ppbCertificate = pbData;
    *pcbCertificate = cbData;
    pbData = NULL;

LExit:
    if (hCertificateHashColumns)
    {
        ::MsiCloseHandle(hCertificateHashColumns);
    }

    if (hCertificateHashView)
    {
        ::MsiCloseHandle(hCertificateHashView);
    }

    ReleaseStr(pwzDistinguishedName);
    ReleaseStr(pwzCA);
    ReleaseMem(pbData);
    ReleaseStr(pwzSql);

    return hr;
}

static HRESULT ReadCertificateFile(
    __in LPCWSTR wzPath,
    __out BYTE** prgbData,
    __out DWORD* pcbData
    )
{
    HRESULT hr = S_OK;

    PCCERT_CONTEXT pCertContext = NULL;
    DWORD dwContentType;
    BYTE* pbData = NULL;
    DWORD cbData = 0;

    if (!::CryptQueryObject(CERT_QUERY_OBJECT_FILE, reinterpret_cast<LPCVOID>(wzPath), CERT_QUERY_CONTENT_FLAG_ALL, CERT_QUERY_FORMAT_FLAG_ALL, 0, NULL, &dwContentType, NULL, NULL, NULL, (LPCVOID*)&pCertContext))
    {
        ExitOnFailure(hr, "Failed to read certificate from file: %ls", wzPath);
    }

    if (pCertContext)
    {
        cbData = pCertContext->cbCertEncoded;
        pbData = static_cast<BYTE*>(MemAlloc(cbData, FALSE));
        ExitOnNull(pbData, hr, E_OUTOFMEMORY, "Failed to allocate memory to read certificate from file: %ls", wzPath);

        CopyMemory(pbData, pCertContext->pbCertEncoded, pCertContext->cbCertEncoded);
    }
    else
    {
        // If we have a PFX blob, get the first certificate out of the PFX and use that instead of the PFX.
        if (dwContentType & CERT_QUERY_CONTENT_PFX)
        {
            SIZE_T size = 0;

            hr = FileRead(&pbData, &size, wzPath);
            ExitOnFailure(hr, "Failed to read PFX file: %ls", wzPath);

            cbData = (DWORD)size;
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected certificate type read from disk.");
        }
    }

    *pcbData = cbData;
    *prgbData = pbData;
    pbData = NULL;

LExit:
    ReleaseMem(pbData);
    return hr;
}

static HRESULT CertificateToHash(
    __in BYTE* pbCertificate,
    __in DWORD cbCertificate,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzPFXPassword,
    __in BYTE rgbHash[],
    __in DWORD cbHash
    )
{
    HRESULT hr = S_OK;

    HCERTSTORE hPfxCertStore = NULL;
    PCCERT_CONTEXT pCertContext = NULL;
    PCCERT_CONTEXT pCertContextEnum = NULL;
    CRYPT_DATA_BLOB blob = { 0 };
    CRYPT_KEY_PROV_INFO* pPfxInfo = NULL;
    DWORD dwKeyset = (CERT_SYSTEM_STORE_CURRENT_USER == dwStoreLocation) ? CRYPT_USER_KEYSET : CRYPT_MACHINE_KEYSET;
    DWORD dwEncodingType;
    DWORD dwContentType;
    DWORD dwFormatType;

    blob.pbData = pbCertificate;
    blob.cbData = cbCertificate;

    if (!::CryptQueryObject(CERT_QUERY_OBJECT_BLOB, &blob, CERT_QUERY_CONTENT_FLAG_ALL, CERT_QUERY_FORMAT_FLAG_ALL, 0, &dwEncodingType, &dwContentType, &dwFormatType, NULL, NULL, (LPCVOID*)&pCertContext))
    {
        ExitWithLastError(hr, "Failed to process certificate as a valid certificate.");
    }

    if (!pCertContext)
    {
        // If we have a PFX blob, get the first certificate out of the PFX and use that instead of the PFX.
        if (dwContentType & CERT_QUERY_CONTENT_PFX)
        {
            // If we fail and our password is blank, also try passing in NULL for the password (according to the docs)
            hPfxCertStore = ::PFXImportCertStore((CRYPT_DATA_BLOB*)&blob, wzPFXPassword, dwKeyset);
            if (NULL == hPfxCertStore && !*wzPFXPassword)
            {
                hPfxCertStore = ::PFXImportCertStore((CRYPT_DATA_BLOB*)&blob, NULL, dwKeyset);
            }
            ExitOnNullWithLastError(hPfxCertStore, hr, "Failed to open PFX file.");

            // Find the first cert with a private key, or just use the last one
            for (pCertContextEnum = ::CertEnumCertificatesInStore(hPfxCertStore, pCertContextEnum);
                 pCertContextEnum;
                 pCertContextEnum = ::CertEnumCertificatesInStore(hPfxCertStore, pCertContextEnum))
            {
                pCertContext = pCertContextEnum;

                if (pCertContext && CertHasPrivateKey(pCertContext, NULL))
                {
                    break;
                }
            }

            ExitOnNullWithLastError(pCertContext, hr, "Failed to read first certificate out of PFX file.");

            // Ignore failures, the worst that happens is some parts of the PFX get left behind.
            CertReadProperty(pCertContext, CERT_KEY_PROV_INFO_PROP_ID, &pPfxInfo, NULL);
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected certificate type processed.");
        }
    }

    DWORD cb = cbHash;
    if (!::CertGetCertificateContextProperty(pCertContext, CERT_SHA1_HASH_PROP_ID, static_cast<LPVOID>(rgbHash), &cb))
    {
        ExitWithLastError(hr, "Failed to get certificate SHA1 hash property.");
    }
    AssertSz(cb == cbHash, "Did not correctly read certificate SHA1 hash.");

LExit:
    if (pCertContext)
    {
        ::CertFreeCertificateContext(pCertContext);
    }

    if (hPfxCertStore)
    {
        ::CertCloseStore(hPfxCertStore, 0);
    }

    if (pPfxInfo)
    {
        HCRYPTPROV hProvIgnored = NULL; // ignored on deletes.
        ::CryptAcquireContextW(&hProvIgnored, pPfxInfo->pwszContainerName, pPfxInfo->pwszProvName, pPfxInfo->dwProvType, dwKeyset | CRYPT_DELETEKEYSET | CRYPT_SILENT);

        MemFree(pPfxInfo);
    }

    return hr;
}

static HRESULT FindExistingCertificate(
    __in LPCWSTR wzName,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzStore,
    __out BYTE** prgbCertificate,
    __out DWORD* pcbCertificate
    )
{
    HRESULT hr = S_OK;
    HCERTSTORE hCertStore = NULL;
    PCCERT_CONTEXT pCertContext = NULL;
    BYTE* pbCertificate = NULL;
    DWORD cbCertificate = 0;

    hCertStore = ::CertOpenStore(CERT_STORE_PROV_SYSTEM, 0, NULL, dwStoreLocation | CERT_STORE_READONLY_FLAG, wzStore);
    MessageExitOnNullWithLastError(hCertStore, hr, msierrCERTFailedOpen, "Failed to open certificate store.");

    // Loop through the certificate, looking for certificates that match our friendly name.
    pCertContext = CertFindCertificateInStore(hCertStore, PKCS_7_ASN_ENCODING | X509_ASN_ENCODING, 0, CERT_FIND_ANY, NULL, NULL);
    while (pCertContext)
    {
        WCHAR wzFriendlyName[256] = { 0 };
        DWORD cbFriendlyName = sizeof(wzFriendlyName);

        if (::CertGetCertificateContextProperty(pCertContext, CERT_FRIENDLY_NAME_PROP_ID, reinterpret_cast<BYTE*>(wzFriendlyName), &cbFriendlyName) &&
            CSTR_EQUAL == ::CompareStringW(LOCALE_SYSTEM_DEFAULT, 0, wzName, -1, wzFriendlyName, -1))
        {
            // If the certificate with matching friendly name is valid, let's use that.
            long lVerify = ::CertVerifyTimeValidity(NULL, pCertContext->pCertInfo);
            if (0 == lVerify)
            {
                cbCertificate = pCertContext->cbCertEncoded;
                pbCertificate = static_cast<BYTE*>(MemAlloc(cbCertificate, FALSE));
                ExitOnNull(pbCertificate, hr, E_OUTOFMEMORY, "Failed to allocate memory to copy out exist certificate.");

                CopyMemory(pbCertificate, pCertContext->pbCertEncoded, cbCertificate);
                break; // found a matching certificate, no more searching necessary
            }
        }

         // Next certificate in the store.
        PCCERT_CONTEXT pNext = ::CertFindCertificateInStore(hCertStore, PKCS_7_ASN_ENCODING | X509_ASN_ENCODING, 0, CERT_FIND_ANY, NULL, pCertContext);
        // old pCertContext is freed by CertFindCertificateInStore
        pCertContext = pNext;
    }

    *prgbCertificate = pbCertificate;
    *pcbCertificate = cbCertificate;
    pbCertificate = NULL;

LExit:
    ReleaseMem(pbCertificate);

    if (pCertContext)
    {
        ::CertFreeCertificateContext(pCertContext);
    }

    if (hCertStore)
    {
        ::CertCloseStore(hCertStore, 0);
    }

    return hr;
}
