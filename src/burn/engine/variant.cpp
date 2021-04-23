// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// internal function declarations

static HRESULT GetVersionInternal(
    __in BURN_VARIANT* pVariant,
    __in BOOL fHidden,
    __in BOOL fSilent,
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

extern "C" HRESULT BVariantGetNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        *pllValue = pVariant->llValue;
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = StrStringToInt64(pVariant->sczValue, 0, pllValue);
        if (FAILED(hr))
        {
            hr = DISP_E_TYPEMISMATCH;
        }
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = StrStringToInt64(pVariant->pValue ? pVariant->pValue->sczVersion : NULL, 0, pllValue);
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

extern "C" HRESULT BVariantGetString(
    __in BURN_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = StrAllocFormattedSecure(psczValue, L"%I64d", pVariant->llValue);
        ExitOnFailure(hr, "Failed to convert int64 to string.");
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = StrAllocStringSecure(psczValue, pVariant->sczValue, 0);
        ExitOnFailure(hr, "Failed to copy string value.");
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = StrAllocStringSecure(psczValue, pVariant->pValue ? pVariant->pValue->sczVersion : NULL, 0);
        ExitOnFailure(hr, "Failed to copy version value.");
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT BVariantGetVersion(
    __in BURN_VARIANT* pVariant,
    __out VERUTIL_VERSION** ppValue
    )
{
    return GetVersionInternal(pVariant, FALSE, FALSE, ppValue);
}

extern "C" HRESULT BVariantGetVersionHidden(
    __in BURN_VARIANT* pVariant,
    __in BOOL fHidden,
    __out VERUTIL_VERSION** ppValue
    )
{
    return GetVersionInternal(pVariant, fHidden, FALSE, ppValue);
}

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

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = VerVersionFromQword(pVariant->llValue, ppValue);
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = VerParseVersion(pVariant->sczValue, 0, FALSE, ppValue);
        if (SUCCEEDED(hr) && !fSilent && (*ppValue)->fInvalid)
        {
            LogId(REPORT_WARNING, MSG_INVALID_VERSION_COERSION, fHidden ? L"*****" : pVariant->sczValue);
        }
        break;
    case BURN_VARIANT_TYPE_VERSION:
        if (!pVariant->pValue)
        {
            *ppValue = NULL;
        }
        else
        {
            hr = VerCopyVersion(pVariant->pValue, ppValue);
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

    if (BURN_VARIANT_TYPE_FORMATTED == pVariant->Type ||
        BURN_VARIANT_TYPE_STRING == pVariant->Type)
    {
        StrSecureZeroFreeString(pVariant->sczValue);
    }
    memset(pVariant, 0, sizeof(BURN_VARIANT));
    pVariant->llValue = llValue;
    pVariant->Type = BURN_VARIANT_TYPE_NUMERIC;

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

        hr = StrAllocStringSecure(&pVariant->sczValue, wzValue, cchValue);
        ExitOnFailure(hr, "Failed to copy string.");

        pVariant->Type = fFormatted ? BURN_VARIANT_TYPE_FORMATTED : BURN_VARIANT_TYPE_STRING;
    }

LExit:
    return hr;
}

extern "C" HRESULT BVariantSetVersion(
    __in BURN_VARIANT* pVariant,
    __in VERUTIL_VERSION* pValue
    )
{
    HRESULT hr = S_OK;

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

    return hr;
}

extern "C" HRESULT BVariantSetValue(
    __in BURN_VARIANT* pVariant,
    __in BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;

    switch (pValue->Type)
    {
    case BURN_VARIANT_TYPE_NONE:
        BVariantUninitialize(pVariant);
        break;
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantSetNumeric(pVariant, pValue->llValue);
        break;
    case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantSetString(pVariant, pValue->sczValue, 0, BURN_VARIANT_TYPE_FORMATTED == pValue->Type);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantSetVersion(pVariant, pValue->pValue);
        break;
    default:
        hr = E_INVALIDARG;
    }
    ExitOnFailure(hr, "Failed to copy variant value.");

LExit:
    return hr;
}

extern "C" HRESULT BVariantCopy(
    __in BURN_VARIANT* pSource,
    __out BURN_VARIANT* pTarget
    )
{
    return BVariantSetValue(pTarget, pSource);
}

extern "C" HRESULT BVariantChangeType(
    __in BURN_VARIANT* pVariant,
    __in BURN_VARIANT_TYPE type
    )
{
    HRESULT hr = S_OK;
    BURN_VARIANT variant = { };

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

LExit:
    return hr;
}
