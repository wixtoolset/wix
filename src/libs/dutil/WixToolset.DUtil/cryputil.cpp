// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define CrypExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_CRYPUTIL, x, s, __VA_ARGS__)
#define CrypExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_CRYPUTIL, x, s, __VA_ARGS__)
#define CrypExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_CRYPUTIL, x, s, __VA_ARGS__)
#define CrypExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_CRYPUTIL, x, s, __VA_ARGS__)
#define CrypExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_CRYPUTIL, x, s, __VA_ARGS__)
#define CrypExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_CRYPUTIL, x, s, __VA_ARGS__)
#define CrypExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_CRYPUTIL, p, x, e, s, __VA_ARGS__)
#define CrypExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_CRYPUTIL, p, x, s, __VA_ARGS__)
#define CrypExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_CRYPUTIL, p, x, e, s, __VA_ARGS__)
#define CrypExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_CRYPUTIL, p, x, s, __VA_ARGS__)
#define CrypExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_CRYPUTIL, e, x, s, __VA_ARGS__)
#define CrypExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_CRYPUTIL, g, x, s, __VA_ARGS__)

static PFN_RTLENCRYPTMEMORY vpfnRtlEncryptMemory = NULL;
static PFN_RTLDECRYPTMEMORY vpfnRtlDecryptMemory = NULL;
static PFN_CRYPTPROTECTMEMORY vpfnCryptProtectMemory = NULL;
static PFN_CRYPTUNPROTECTMEMORY vpfnCryptUnprotectMemory = NULL;

static HMODULE vhAdvApi32Dll = NULL;
static HMODULE vhCrypt32Dll = NULL;
static BOOL vfCrypInitialized = FALSE;

// function definitions

/********************************************************************
 CrypInitialize - initializes cryputil

*********************************************************************/
extern "C" HRESULT DAPI CrypInitialize(
    )
{
    HRESULT hr = S_OK;

    hr = LoadSystemLibrary(L"AdvApi32.dll", &vhAdvApi32Dll);
    if (SUCCEEDED(hr))
    {
        // Ignore failures - if these don't exist, we'll try the Crypt methods.
        vpfnRtlEncryptMemory = reinterpret_cast<PFN_RTLENCRYPTMEMORY>(::GetProcAddress(vhAdvApi32Dll, "SystemFunction040"));
        vpfnRtlDecryptMemory = reinterpret_cast<PFN_RTLDECRYPTMEMORY>(::GetProcAddress(vhAdvApi32Dll, "SystemFunction041"));
    }
    if (!vpfnRtlEncryptMemory || !vpfnRtlDecryptMemory)
    {
        hr = LoadSystemLibrary(L"Crypt32.dll", &vhCrypt32Dll);
        CrypExitOnFailure(hr, "Failed to load Crypt32.dll");
        
        vpfnCryptProtectMemory = reinterpret_cast<PFN_CRYPTPROTECTMEMORY>(::GetProcAddress(vhCrypt32Dll, "CryptProtectMemory"));
        if (!vpfnRtlEncryptMemory && !vpfnCryptProtectMemory)
        {
            CrypExitWithLastError(hr, "Failed to load an encryption method");
        }
        vpfnCryptUnprotectMemory = reinterpret_cast<PFN_CRYPTUNPROTECTMEMORY>(::GetProcAddress(vhCrypt32Dll, "CryptUnprotectMemory"));
        if (!vpfnRtlDecryptMemory && !vpfnCryptUnprotectMemory)
        {
            CrypExitWithLastError(hr, "Failed to load a decryption method");
        }
    }

    vfCrypInitialized = TRUE;

LExit:
    return hr;
}


/********************************************************************
 CrypUninitialize - uninitializes cryputil

*********************************************************************/
extern "C" void DAPI CrypUninitialize(
    )
{
    if (vhAdvApi32Dll)
    {
        ::FreeLibrary(vhAdvApi32Dll);
        vhAdvApi32Dll = NULL;
        vpfnRtlEncryptMemory = NULL;
        vpfnRtlDecryptMemory = NULL;
    }
    
    if (vhCrypt32Dll)
    {
        ::FreeLibrary(vhCrypt32Dll);
        vhCrypt32Dll = NULL;
        vpfnCryptProtectMemory = NULL;
        vpfnCryptUnprotectMemory = NULL;
    }

    vfCrypInitialized = FALSE;
}

extern "C" HRESULT DAPI CrypDecodeObject(
    __in_z LPCSTR szStructType,
    __in_ecount(cbData) const BYTE* pbData,
    __in DWORD cbData,
    __in DWORD dwFlags,
    __out LPVOID* ppvObject,
    __out_opt DWORD* pcbObject
    )
{
    HRESULT hr = S_OK;
    LPVOID pvObject = NULL;
    DWORD cbObject = 0;

    if (!::CryptDecodeObject(X509_ASN_ENCODING | PKCS_7_ASN_ENCODING, szStructType, pbData, cbData, dwFlags, NULL, &cbObject))
    {
        CrypExitWithLastError(hr, "Failed to decode object to determine size.");
    }

    pvObject = MemAlloc(cbObject, TRUE);
    CrypExitOnNull(pvObject, hr, E_OUTOFMEMORY, "Failed to allocate memory for decoded object.");

    if (!::CryptDecodeObject(X509_ASN_ENCODING | PKCS_7_ASN_ENCODING, szStructType, pbData, cbData, dwFlags, pvObject, &cbObject))
    {
        CrypExitWithLastError(hr, "Failed to decode object.");
    }

    *ppvObject = pvObject;
    pvObject = NULL;

    if (pcbObject)
    {
        *pcbObject = cbObject;
    }

LExit:
    ReleaseMem(pvObject);

    return hr;
}


extern "C" HRESULT DAPI CrypMsgGetParam(
    __in HCRYPTMSG hCryptMsg,
    __in DWORD dwType,
    __in DWORD dwIndex,
    __out LPVOID* ppvData,
    __out_opt DWORD* pcbData
    )
{
    HRESULT hr = S_OK;
    LPVOID pv = NULL;
    DWORD cb = 0;

    if (!::CryptMsgGetParam(hCryptMsg, dwType, dwIndex, NULL, &cb))
    {
        CrypExitWithLastError(hr, "Failed to get crypt message parameter data size.");
    }

    pv = MemAlloc(cb, TRUE);
    CrypExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to allocate memory for crypt message parameter.");

    if (!::CryptMsgGetParam(hCryptMsg, dwType, dwIndex, pv, &cb))
    {
        CrypExitWithLastError(hr, "Failed to get crypt message parameter.");
    }

    *ppvData = pv;
    pv = NULL;

    if (pcbData)
    {
        *pcbData = cb;
    }

LExit:
    ReleaseMem(pv);

    return hr;
}


extern "C" HRESULT DAPI CrypHashFile(
    __in_z LPCWSTR wzFilePath,
    __in DWORD dwProvType,
    __in ALG_ID algid,
    __out_bcount(cbHash) BYTE* pbHash,
    __in DWORD cbHash,
    __out_opt DWORD64* pqwBytesHashed
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // open input file
    hFile = ::CreateFileW(wzFilePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        CrypExitWithLastError(hr, "Failed to open input file.");
    }

    hr = CrypHashFileHandle(hFile, dwProvType, algid, pbHash, cbHash, pqwBytesHashed);
    CrypExitOnFailure(hr, "Failed to hash file: %ls", wzFilePath);

LExit:
    ReleaseFileHandle(hFile);

    return hr;
}


extern "C" HRESULT DAPI CrypHashFileHandle(
    __in HANDLE hFile,
    __in DWORD dwProvType,
    __in ALG_ID algid,
    __out_bcount(cbHash) BYTE* pbHash,
    __in DWORD cbHash,
    __out_opt DWORD64* pqwBytesHashed
    )
{
    HRESULT hr = S_OK;
    HCRYPTPROV hProv = NULL;
    HCRYPTHASH hHash = NULL;
    DWORD cbRead = 0;
    BYTE rgbBuffer[4096] = { };
    const LARGE_INTEGER liZero = { };

    // get handle to the crypto provider
    if (!::CryptAcquireContextW(&hProv, NULL, NULL, dwProvType, CRYPT_VERIFYCONTEXT | CRYPT_SILENT))
    {
        CrypExitWithLastError(hr, "Failed to acquire crypto context.");
    }

    // initiate hash
    if (!::CryptCreateHash(hProv, algid, 0, 0, &hHash))
    {
        CrypExitWithLastError(hr, "Failed to initiate hash.");
    }

    for (;;)
    {
        // read data block
        if (!::ReadFile(hFile, rgbBuffer, sizeof(rgbBuffer), &cbRead, NULL))
        {
            CrypExitWithLastError(hr, "Failed to read data block.");
        }

        if (!cbRead)
        {
            break; // end of file
        }

        // hash data block
        if (!::CryptHashData(hHash, rgbBuffer, cbRead, 0))
        {
            CrypExitWithLastError(hr, "Failed to hash data block.");
        }
    }

    // get hash value
    if (!::CryptGetHashParam(hHash, HP_HASHVAL, pbHash, &cbHash, 0))
    {
        CrypExitWithLastError(hr, "Failed to get hash value.");
    }

    if (pqwBytesHashed)
    {
        if (!::SetFilePointerEx(hFile, liZero, (LARGE_INTEGER*)pqwBytesHashed, FILE_CURRENT))
        {
            CrypExitWithLastError(hr, "Failed to get file pointer.");
        }
    }

LExit:
    if (hHash)
    {
        ::CryptDestroyHash(hHash);
    }
    if (hProv)
    {
        ::CryptReleaseContext(hProv, 0);
    }

    return hr;
}

HRESULT DAPI CrypHashBuffer(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __in DWORD dwProvType,
    __in ALG_ID algid,
    __out_bcount(cbHash) BYTE* pbHash,
    __in DWORD cbHash
    )
{
    HRESULT hr = S_OK;
    HCRYPTPROV hProv = NULL;
    HCRYPTHASH hHash = NULL;
    DWORD cbDataHashed = 0;
    SIZE_T cbTotal = 0;
    SIZE_T cbRemaining = 0;

    // get handle to the crypto provider
    if (!::CryptAcquireContextW(&hProv, NULL, NULL, dwProvType, CRYPT_VERIFYCONTEXT | CRYPT_SILENT))
    {
        CrypExitWithLastError(hr, "Failed to acquire crypto context.");
    }

    // initiate hash
    if (!::CryptCreateHash(hProv, algid, 0, 0, &hHash))
    {
        CrypExitWithLastError(hr, "Failed to initiate hash.");
    }

    do
    {
        cbRemaining = cbBuffer - cbTotal;
        cbDataHashed = (DWORD)min(DWORD_MAX, cbRemaining);
        if (!::CryptHashData(hHash, pbBuffer + cbTotal, cbDataHashed, 0))
        {
            CrypExitWithLastError(hr, "Failed to hash data.");
        }

        cbTotal += cbDataHashed;
    } while (cbTotal < cbBuffer);

    // get hash value
    if (!::CryptGetHashParam(hHash, HP_HASHVAL, pbHash, &cbHash, 0))
    {
        CrypExitWithLastError(hr, "Failed to get hash value.");
    }

LExit:
    if (hHash)
    {
        ::CryptDestroyHash(hHash);
    }
    if (hProv)
    {
        ::CryptReleaseContext(hProv, 0);
    }

    return hr;
}

HRESULT DAPI CrypEncryptMemory(
	__inout LPVOID pData,
	__in DWORD cbData,
	__in DWORD dwFlags
    )
{
    HRESULT hr = E_FAIL;

    if (0 != cbData % CRYP_ENCRYPT_MEMORY_SIZE)
    {
        hr = E_INVALIDARG;
    }
    else if (vpfnRtlEncryptMemory)
    {
        hr = static_cast<HRESULT>(vpfnRtlEncryptMemory(pData, cbData, dwFlags));
    }
    else if (vpfnCryptProtectMemory)
    {
        if (vpfnCryptProtectMemory(pData, cbData, dwFlags))
        {
            hr = S_OK;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
        }
    }
    CrypExitOnFailure(hr, "Failed to encrypt memory");
LExit:
    return hr;
}

HRESULT DAPI CrypDecryptMemory(
	__inout LPVOID pData,
	__in DWORD cbData,
	__in DWORD dwFlags
    )
{
    HRESULT hr = E_FAIL;
    
    if (0 != cbData % CRYP_ENCRYPT_MEMORY_SIZE)
    {
        hr = E_INVALIDARG;
    }
    else if (vpfnRtlDecryptMemory)
    {
        hr = static_cast<HRESULT>(vpfnRtlDecryptMemory(pData, cbData, dwFlags));
    }
    else if (vpfnCryptUnprotectMemory)
    {
        if (vpfnCryptUnprotectMemory(pData, cbData, dwFlags))
        {
            hr = S_OK;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
        }
    }
    CrypExitOnFailure(hr, "Failed to decrypt memory");
LExit:
    return hr;
}

