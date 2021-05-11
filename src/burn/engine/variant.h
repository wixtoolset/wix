#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

enum BURN_VARIANT_TYPE
{
    BURN_VARIANT_TYPE_NONE,
    BURN_VARIANT_TYPE_FORMATTED,
    BURN_VARIANT_TYPE_NUMERIC,
    BURN_VARIANT_TYPE_STRING, // when formatting this value should be used as is (don't continue recursively formatting).
    BURN_VARIANT_TYPE_VERSION,
};


// struct

typedef struct _BURN_VARIANT
{
    union
    {
        LONGLONG llValue;
        VERUTIL_VERSION* pValue;
        LPWSTR sczValue;
    };
    BURN_VARIANT_TYPE Type;
} BURN_VARIANT;


// function declarations

void BVariantUninitialize(
    __in BURN_VARIANT* pVariant
    );
HRESULT BVariantGetNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    );
HRESULT BVariantGetString(
    __in BURN_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    );
HRESULT BVariantGetVersion(
    __in BURN_VARIANT* pVariant,
    __out VERUTIL_VERSION** ppValue
    );
HRESULT BVariantGetVersionHidden(
    __in BURN_VARIANT* pVariant,
    __in BOOL fHidden,
    __out VERUTIL_VERSION** ppValue
    );
HRESULT BVariantGetVersionSilent(
    __in BURN_VARIANT* pVariant,
    __in BOOL fSilent,
    __out VERUTIL_VERSION** ppValue
    );
HRESULT BVariantSetNumeric(
    __in BURN_VARIANT* pVariant,
    __in LONGLONG llValue
    );
HRESULT BVariantSetString(
    __in BURN_VARIANT* pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue,
    __in BOOL fFormatted
    );
HRESULT BVariantSetVersion(
    __in BURN_VARIANT* pVariant,
    __in VERUTIL_VERSION* pValue
    );
/********************************************************************
BVariantSetValue - Convenience function that calls BVariantUninitialize,
                   BVariantSetNumeric, BVariantSetString, or 
                   BVariantSetVersion based on the type of pValue.
********************************************************************/
HRESULT BVariantSetValue(
    __in BURN_VARIANT* pVariant,
    __in BURN_VARIANT* pValue
    );
/********************************************************************
BVariantCopy - creates a copy of pSource.
********************************************************************/
HRESULT BVariantCopy(
    __in BURN_VARIANT* pSource,
    __out BURN_VARIANT* pTarget
    );
HRESULT BVariantChangeType(
    __in BURN_VARIANT* pVariant,
    __in BURN_VARIANT_TYPE type
    );

#if defined(__cplusplus)
}
#endif
