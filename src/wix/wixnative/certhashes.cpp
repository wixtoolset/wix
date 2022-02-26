// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define SHA1_HASH_LEN 20

static HRESULT GetPublicKeyIdentifierAndThumbprint(
    __in_z LPCWSTR wzPath,
    __inout_z LPWSTR* psczPublicKeyIdentifier,
    __inout_z LPWSTR* psczThumbprint);

static HRESULT GetChainContext(
    __in_z LPCWSTR wzPath,
    __out PCCERT_CHAIN_CONTEXT* ppChainContext);


HRESULT CertificateHashesCommand(
    __in int argc,
    __in_ecount(argc) LPWSTR argv[])
{
    Unused(argc);
    Unused(argv);

    HRESULT hr = S_OK;

    LPWSTR sczFilePath = NULL;
    LPWSTR sczPublicKeyIdentifier = NULL;
    LPWSTR sczThumbprint = NULL;

    hr = WixNativeReadStdinPreamble();
    ExitOnFailure(hr, "Failed to read stdin preamble before reading paths to get certificate hashes");

    // Get the hash for each provided file.
    for (;;)
    {
        hr = ConsoleReadW(&sczFilePath);
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to read file path to signed file from stdin");

        if (!*sczFilePath)
        {
            break;
        }

        hr = GetPublicKeyIdentifierAndThumbprint(sczFilePath, &sczPublicKeyIdentifier, &sczThumbprint);
        if (FAILED(hr))
        {
            // Treat no signature as success without finding certificate hashes.
            ConsoleWriteLine(CONSOLE_COLOR_NORMAL, "%ls\t\t\t0x%x", sczFilePath, TRUST_E_NOSIGNATURE == hr ? 0 : hr);
        }
        else
        {
            ConsoleWriteLine(CONSOLE_COLOR_NORMAL, "%ls\t%ls\t%ls\t0x%x", sczFilePath, sczPublicKeyIdentifier, sczThumbprint, hr);
        }
    }

LExit:
    ReleaseStr(sczThumbprint);
    ReleaseStr(sczPublicKeyIdentifier);
    ReleaseStr(sczFilePath);

    return hr;
}

static HRESULT GetPublicKeyIdentifierAndThumbprint(
    __in_z LPCWSTR wzPath,
    __inout_z LPWSTR* psczPublicKeyIdentifier,
    __inout_z LPWSTR* psczThumbprint)
{
    HRESULT hr = S_OK;
    PCCERT_CHAIN_CONTEXT pChainContext = NULL;
    PCCERT_CONTEXT pCertContext = NULL;
    BYTE rgbPublicKeyIdentifier[SHA1_HASH_LEN] = { };
    DWORD cbPublicKeyIdentifier = sizeof(rgbPublicKeyIdentifier);
    BYTE* pbThumbprint = NULL;
    DWORD cbThumbprint = 0;

    hr = GetChainContext(wzPath, &pChainContext);
    ExitOnFailure(hr, "Failed to get chain context for file: %ls", wzPath);

    pCertContext = pChainContext->rgpChain[0]->rgpElement[0]->pCertContext;

    // Get the certificate's public key identifier and thumbprint.
    if (!::CryptHashPublicKeyInfo(NULL, CALG_SHA1, 0, X509_ASN_ENCODING, &pCertContext->pCertInfo->SubjectPublicKeyInfo, rgbPublicKeyIdentifier, &cbPublicKeyIdentifier))
    {
        ExitWithLastError(hr, "Failed to get certificate public key identifier from file: %ls", wzPath);
    }

    hr = CertReadProperty(pCertContext, CERT_SHA1_HASH_PROP_ID, &pbThumbprint, &cbThumbprint);
    ExitOnFailure(hr, "Failed to read certificate thumbprint from file: %ls", wzPath);

    // Get the public key indentifier and thumbprint in hex.
    hr = StrAllocHexEncode(rgbPublicKeyIdentifier, cbPublicKeyIdentifier, psczPublicKeyIdentifier);
    ExitOnFailure(hr, "Failed to convert certificate public key to hex for file: %ls", wzPath);

    hr = StrAllocHexEncode(pbThumbprint, cbThumbprint, psczThumbprint);
    ExitOnFailure(hr, "Failed to convert certificate thumbprint to hex for file: %ls", wzPath);

LExit:
    ReleaseMem(pbThumbprint);
    return hr;
}

static HRESULT GetChainContext(
    __in_z LPCWSTR wzPath,
    __out PCCERT_CHAIN_CONTEXT* ppChainContext)
{
    HRESULT hr = S_OK;

    GUID guidAuthenticode = WINTRUST_ACTION_GENERIC_VERIFY_V2;
    WINTRUST_FILE_INFO wfi = { };
    WINTRUST_DATA wtd = { };
    CRYPT_PROVIDER_DATA* pProviderData = NULL;
    CRYPT_PROVIDER_SGNR* pSigner = NULL;

    wfi.cbStruct = sizeof(wfi);
    wfi.pcwszFilePath = wzPath;

    wtd.cbStruct = sizeof(wtd);
    wtd.dwUnionChoice = WTD_CHOICE_FILE;
    wtd.pFile = &wfi;
    wtd.dwStateAction = WTD_STATEACTION_VERIFY;
    wtd.dwProvFlags = WTD_REVOCATION_CHECK_NONE | WTD_HASH_ONLY_FLAG | WTD_CACHE_ONLY_URL_RETRIEVAL;
    wtd.dwUIChoice = WTD_UI_NONE;

    hr = ::WinVerifyTrust(static_cast<HWND>(INVALID_HANDLE_VALUE), &guidAuthenticode, &wtd);
    ExitOnFailure(hr, "Failed to verify certificate on file: %ls", wzPath);

    pProviderData = ::WTHelperProvDataFromStateData(wtd.hWVTStateData);
    ExitOnNullWithLastError(pProviderData, hr, "Failed to get provider state from authenticode certificate on file: %ls", wzPath);

    pSigner = ::WTHelperGetProvSignerFromChain(pProviderData, 0, FALSE, 0);
    ExitOnNullWithLastError(pSigner, hr, "Failed to get signer chain from authenticode certificate on file: %ls", wzPath);

    *ppChainContext = pSigner->pChainContext;

LExit:
    return hr;
}
