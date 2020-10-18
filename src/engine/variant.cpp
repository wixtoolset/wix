// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define VARIANT_ENCRYPTION_SCOPE CRYPTPROTECTMEMORY_SAME_PROCESS

// internal function declarations

static HRESULT GetVersionInternal(
    __in BURN_VARIANT* pVariant,
    __in BOOL fHidden,
    __in BOOL fSilent,
    __out VERUTIL_VERSION** ppValue
    );
static HRESULT BVariantEncryptString(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    );

static void BVariantRetrieveNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    );

static HRESULT BVariantRetrieveDecryptedString(
    __in BURN_VARIANT* pVariant,
    __out LPWSTR* psczValue
    );

static void BVariantRetrieveVersion(
    __in BURN_VARIANT* pVariant,
    __out VERUTIL_VERSION** ppValue
    );

// function definitions

extern "C" void BVariantUninitialize(
    __in BURN_VARIANT* pVariant
    )
{
    if (BURN_VARIANT_TYPE_FORMATTED == pVariant->Type ||
        BURN_VARIANT_TYPE_STRING == pVariant->Type)
    {
        StrSecureZeroFreeString(pVariant->sczValue);
    }
    SecureZeroMemory(pVariant, sizeof(BURN_VARIANT));
}

// The contents of pllValue may be sensitive, should keep encrypted and SecureZeroMemory.
extern "C" HRESULT BVariantGetNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    VERUTIL_VERSION* pVersionValue = NULL;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        BVariantRetrieveNumeric(pVariant, pllValue);
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantRetrieveDecryptedString(pVariant, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = StrStringToInt64(sczValue, 0, pllValue);
            if (FAILED(hr))
            {
                hr = DISP_E_TYPEMISMATCH;
            }
        }
        StrSecureZeroFreeString(sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        BVariantRetrieveVersion(pVariant, &pVersionValue);

        hr = StrStringToInt64(pVersionValue->sczVersion, 0, pllValue);
        if (FAILED(hr))
        {
            hr = DISP_E_TYPEMISMATCH;
        }
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

    return hr;
}

// The contents of psczValue may be sensitive, should keep encrypted and SecureZeroFree.
extern "C" HRESULT BVariantGetString(
    __in BURN_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    VERUTIL_VERSION* pVersionValue = NULL;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        BVariantRetrieveNumeric(pVariant, &llValue);
        if (SUCCEEDED(hr))
        {
            hr = StrAllocFormattedSecure(psczValue, L"%I64d", llValue);
            ExitOnFailure(hr, "Failed to convert int64 to string.");
        }
        SecureZeroMemory(&llValue, sizeof(llValue));
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantRetrieveDecryptedString(pVariant, psczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        BVariantRetrieveVersion(pVariant, &pVersionValue);

        hr = StrAllocStringSecure(psczValue, pVersionValue->sczVersion, 0);
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

LExit:
    return hr;
}

// The contents of ppValue may be sensitive, should keep encrypted and SecureZeroMemory.
extern "C" HRESULT BVariantGetVersion(
    __in BURN_VARIANT* pVariant,
    __out VERUTIL_VERSION** ppValue
    )
{
    return GetVersionInternal(pVariant, FALSE, FALSE, ppValue);
}

// The contents of ppValue may be sensitive, should keep encrypted and SecureZeroMemory.
extern "C" HRESULT BVariantGetVersionHidden(
    __in BURN_VARIANT* pVariant,
    __in BOOL fHidden,
    __out VERUTIL_VERSION** ppValue
    )
{
    return GetVersionInternal(pVariant, fHidden, FALSE, ppValue);
}

// The contents of ppValue may be sensitive, should keep encrypted and SecureZeroMemory.
extern "C" HRESULT BVariantGetVersionSilent(
    __in BURN_VARIANT* pVariant,
    __in BOOL fSilent,
    __out VERUTIL_VERSION** ppValue
    )
{
    return GetVersionInternal(pVariant, FALSE, fSilent, ppValue);
}

static HRESULT GetVersionInternal(
    __in BURN_VARIANT* pVariant,
    __in BOOL fHidden,
    __in BOOL fSilent,
    __out VERUTIL_VERSION** ppValue
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    LPWSTR sczValue = NULL;
    VERUTIL_VERSION* pValue = NULL;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        BVariantRetrieveNumeric(pVariant, &llValue);

        hr = VerVersionFromQword(llValue, ppValue);
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantRetrieveDecryptedString(pVariant, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = VerParseVersion(sczValue, 0, FALSE, ppValue);
            if (FAILED(hr))
            {
                hr = DISP_E_TYPEMISMATCH;
            }
            else if (!fSilent && (*ppValue)->fInvalid)
            {
                LogId(REPORT_WARNING, MSG_INVALID_VERSION_COERSION, fHidden ? L"*****" : sczValue);
            }
        }
        StrSecureZeroFreeString(sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        BVariantRetrieveVersion(pVariant, &pValue);

        if (!pValue)
        {
            *ppValue = NULL;
        }
        else
        {
            hr = VerCopyVersion(pValue, ppValue);
        }
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

    return hr;
}

extern "C" HRESULT BVariantSetNumeric(
    __in BURN_VARIANT* pVariant,
    __in LONGLONG llValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncrypt = pVariant->fEncryptString;

    if (BURN_VARIANT_TYPE_FORMATTED == pVariant->Type ||
        BURN_VARIANT_TYPE_STRING == pVariant->Type)
    {
        StrSecureZeroFreeString(pVariant->sczValue);
    }
    memset(pVariant, 0, sizeof(BURN_VARIANT));
    pVariant->llValue = llValue;
    pVariant->Type = BURN_VARIANT_TYPE_NUMERIC;
    BVariantSetEncryption(pVariant, fEncrypt);

    return hr;
}

extern "C" HRESULT BVariantSetString(
    __in BURN_VARIANT* pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue,
    __in BOOL fFormatted
    )
{
    HRESULT hr = S_OK;
    BOOL fEncrypt = pVariant->fEncryptString;

    if (!wzValue) // if we're nulling out the string, make the variable NONE.
    {
        BVariantUninitialize(pVariant);
    }
    else // assign the value.
    {
        if (BURN_VARIANT_TYPE_FORMATTED != pVariant->Type &&
            BURN_VARIANT_TYPE_STRING != pVariant->Type)
        {
            memset(pVariant, 0, sizeof(BURN_VARIANT));
        }
        else
        {
            // We're about to copy an unencrypted value.
            pVariant->fEncryptString = FALSE;
        }

        hr = StrAllocStringSecure(&pVariant->sczValue, wzValue, cchValue);
        ExitOnFailure(hr, "Failed to copy string.");

        pVariant->Type = fFormatted ? BURN_VARIANT_TYPE_FORMATTED : BURN_VARIANT_TYPE_STRING;
    }

LExit:
    BVariantSetEncryption(pVariant, fEncrypt);
    return hr;
}

extern "C" HRESULT BVariantSetVersion(
    __in BURN_VARIANT* pVariant,
    __in VERUTIL_VERSION* pValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncryptValue = pVariant->fEncryptString;

    if (!pValue) // if we're nulling out the version, make the variable NONE.
    {
        BVariantUninitialize(pVariant);
    }
    else // assign the value.
    {
        if (BURN_VARIANT_TYPE_FORMATTED == pVariant->Type ||
            BURN_VARIANT_TYPE_STRING == pVariant->Type)
        {
            StrSecureZeroFreeString(pVariant->sczValue);
        }
        memset(pVariant, 0, sizeof(BURN_VARIANT));
        hr = VerCopyVersion(pValue, &pVariant->pValue);
        pVariant->Type = BURN_VARIANT_TYPE_VERSION;
    }

    BVariantSetEncryption(pVariant, fEncryptValue);

    return hr;
}

extern "C" HRESULT BVariantSetValue(
    __in BURN_VARIANT* pVariant,
    __in BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    LPWSTR sczValue = NULL;
    VERUTIL_VERSION* pVersionValue = NULL;
    BOOL fEncrypt = pVariant->fEncryptString;

    switch (pValue->Type)
    {
    case BURN_VARIANT_TYPE_NONE:
        BVariantUninitialize(pVariant);
        break;
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantGetNumeric(pValue, &llValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetNumeric(pVariant, llValue);
        }
        SecureZeroMemory(&llValue, sizeof(llValue));
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantGetString(pValue, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetString(pVariant, sczValue, 0, BURN_VARIANT_TYPE_FORMATTED == pValue->Type);
        }
        StrSecureZeroFreeString(sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantGetVersionSilent(pValue, TRUE, &pVersionValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetVersion(pVariant, pVersionValue);
        }
        break;
    default:
        hr = E_INVALIDARG;
    }
    ExitOnFailure(hr, "Failed to copy variant.");

    hr = BVariantSetEncryption(pVariant, fEncrypt);

LExit:
    return hr;
}

extern "C" HRESULT BVariantCopy(
    __in BURN_VARIANT* pSource,
    __out BURN_VARIANT* pTarget
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    LPWSTR sczValue = NULL;
    VERUTIL_VERSION* pVersionValue = 0;

    BVariantUninitialize(pTarget);

    switch (pSource->Type)
    {
    case BURN_VARIANT_TYPE_NONE:
        break;
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantGetNumeric(pSource, &llValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetNumeric(pTarget, llValue);
        }
        SecureZeroMemory(&llValue, sizeof(llValue));
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantGetString(pSource, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetString(pTarget, sczValue, 0, BURN_VARIANT_TYPE_FORMATTED == pSource->Type);
        }
        StrSecureZeroFreeString(sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantGetVersionSilent(pSource, TRUE, &pVersionValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetVersion(pTarget, pVersionValue);
        }
        break;
    default:
        hr = E_INVALIDARG;
    }
    ExitOnFailure(hr, "Failed to copy variant.");

    hr = BVariantSetEncryption(pTarget, pSource->fEncryptString);

LExit:
    return hr;
}

extern "C" HRESULT BVariantChangeType(
    __in BURN_VARIANT* pVariant,
    __in BURN_VARIANT_TYPE type
    )
{
    HRESULT hr = S_OK;
    BURN_VARIANT variant = { };
    BOOL fEncrypt = pVariant->fEncryptString;

    if (pVariant->Type == type)
    {
        ExitFunction(); // variant already is of the requested type
    }
    else if (BURN_VARIANT_TYPE_FORMATTED == pVariant->Type && BURN_VARIANT_TYPE_STRING == type ||
             BURN_VARIANT_TYPE_STRING == pVariant->Type && BURN_VARIANT_TYPE_FORMATTED == type)
    {
        pVariant->Type = type;
        ExitFunction();
    }

    switch (type)
    {
    case BURN_VARIANT_TYPE_NONE:
        hr = S_OK;
        break;
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantGetNumeric(pVariant, &variant.llValue);
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantGetString(pVariant, &variant.sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantGetVersionSilent(pVariant, TRUE, &variant.pValue);
        break;
    default:
        ExitFunction1(hr = E_INVALIDARG);
    }
    variant.Type = type;
    ExitOnFailure(hr, "Failed to copy variant value.");

    BVariantUninitialize(pVariant);
    memcpy_s(pVariant, sizeof(BURN_VARIANT), &variant, sizeof(BURN_VARIANT));
    SecureZeroMemory(&variant, sizeof(BURN_VARIANT));
    BVariantSetEncryption(pVariant, fEncrypt);

LExit:
    return hr;
}

extern "C" HRESULT BVariantSetEncryption(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    )
{
    HRESULT hr = S_OK;

    if (pVariant->fEncryptString == fEncrypt)
    {
        // The requested encryption state is already applied.
        ExitFunction();
    }
    
    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NONE:
    case BURN_VARIANT_TYPE_NUMERIC:
    case BURN_VARIANT_TYPE_VERSION:
        hr = S_OK;
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantEncryptString(pVariant, fEncrypt);
        break;
    default:
        hr = E_INVALIDARG;
    }
    ExitOnFailure(hr, "Failed to set the variant's encryption state");
    pVariant->fEncryptString = fEncrypt;

LExit:
    return hr;
}

static HRESULT BVariantEncryptString(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    )
{
    HRESULT hr = S_OK;
    SIZE_T cbData = 0;

    if (NULL == pVariant->sczValue)
    {
        ExitFunction();
    }

    cbData = MemSize(pVariant->sczValue);
    if (-1 == cbData)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Failed to get the size of the string");
    }
    
    DWORD remainder = fEncrypt ? cbData % CRYP_ENCRYPT_MEMORY_SIZE : 0;
    DWORD extraNeeded = 0 < remainder ? CRYP_ENCRYPT_MEMORY_SIZE - remainder : 0;
    if ((MAXDWORD - extraNeeded) < cbData)
    {
        hr = E_INVALIDDATA;
        ExitOnFailure(hr, "The string is too big: size %u", cbData);
    }
    else if (0 < extraNeeded)
    {
        cbData += extraNeeded;
        LPVOID pvNew = NULL;
        hr = MemReAllocSecure(static_cast<LPVOID>(pVariant->sczValue), cbData, TRUE, &pvNew);
        ExitOnFailure(hr, "Failed to resize the string so it could be encrypted");
        pVariant->sczValue = static_cast<LPWSTR>(pvNew);
    }
    
    if (fEncrypt)
    {
        hr = CrypEncryptMemory(pVariant->sczValue, static_cast<DWORD>(cbData), VARIANT_ENCRYPTION_SCOPE);
    }
    else
    {
        hr = CrypDecryptMemory(pVariant->sczValue, static_cast<DWORD>(cbData), VARIANT_ENCRYPTION_SCOPE);
    }

LExit:
    return hr;
}

static void BVariantRetrieveNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    Assert(NULL != pllValue);

    *pllValue = pVariant->llValue;
}

// The contents of psczValue may be sensitive, should keep encrypted and SecureZeroFree.
static HRESULT BVariantRetrieveDecryptedString(
    __in BURN_VARIANT* pVariant,
    __out LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;

    if (!pVariant->sczValue)
    {
        *psczValue = NULL;
        ExitFunction();
    }

    if (pVariant->fEncryptString)
    {
        hr = BVariantEncryptString(pVariant, FALSE);
        ExitOnFailure(hr, "Failed to decrypt string");
    }

    hr = StrAllocStringSecure(psczValue, pVariant->sczValue, 0);
    ExitOnFailure(hr, "Failed to copy value.");

    if (pVariant->fEncryptString)
    {
        hr = BVariantEncryptString(pVariant, TRUE);
    }

LExit:
    return hr;
}

static void BVariantRetrieveVersion(
    __in BURN_VARIANT* pVariant,
    __out VERUTIL_VERSION** ppValue
    )
{
    Assert(ppValue);

    *ppValue = pVariant->pValue;
}
