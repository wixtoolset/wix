#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

typedef enum _BUNDLE_INSTALL_CONTEXT
{
    BUNDLE_INSTALL_CONTEXT_MACHINE,
    BUNDLE_INSTALL_CONTEXT_USER,
} BUNDLE_INSTALL_CONTEXT;

typedef enum _BUNDLE_QUERY_CALLBACK_RESULT
{
    BUNDLE_QUERY_CALLBACK_RESULT_CONTINUE,
    BUNDLE_QUERY_CALLBACK_RESULT_CANCEL,
} BUNDLE_QUERY_CALLBACK_RESULT;

typedef enum _BUNDLE_RELATION_TYPE
{
    BUNDLE_RELATION_NONE,
    BUNDLE_RELATION_DETECT,
    BUNDLE_RELATION_UPGRADE,
    BUNDLE_RELATION_ADDON,
    BUNDLE_RELATION_PATCH,
    BUNDLE_RELATION_DEPENDENT_ADDON,
    BUNDLE_RELATION_DEPENDENT_PATCH,
} BUNDLE_RELATION_TYPE;

typedef struct _BUNDLE_QUERY_RELATED_BUNDLE_RESULT
{
    LPCWSTR wzBundleCode;
    BUNDLE_INSTALL_CONTEXT installContext;
    REG_KEY_BITNESS regBitness;
    HKEY hkBundle;
    BUNDLE_RELATION_TYPE relationType;
} BUNDLE_QUERY_RELATED_BUNDLE_RESULT;

typedef BUNDLE_QUERY_CALLBACK_RESULT(CALLBACK *PFNBUNDLE_QUERY_RELATED_BUNDLE_CALLBACK)(
    __in const BUNDLE_QUERY_RELATED_BUNDLE_RESULT* pBundle,
    __in_opt LPVOID pvContext
    );


/********************************************************************
BundleGetBundleInfo - Queries the bundle installation metadata for a given property,
    the caller is expected to free the memory returned vis psczValue

RETURNS:
    E_INVALIDARG
        An invalid parameter was passed to the function.
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT)
        The bundle is not installed
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY)
        The property is unrecognized
    E_NOTIMPL:
        Tried to read a bundle attribute for a type which has not been implemented

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
HRESULT DAPI BundleGetBundleInfo(
    __in_z LPCWSTR wzBundleCode,
    __in_z LPCWSTR wzAttribute,
    __deref_out_z LPWSTR* psczValue
    );

/********************************************************************
BundleGetBundleInfoFixed - Queries the bundle installation metadata for a given property

RETURNS:
    E_INVALIDARG
        An invalid parameter was passed to the function.
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT)
        The bundle is not installed
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY)
        The property is unrecognized
    HRESULT_FROM_WIN32(ERROR_MORE_DATA)
        A buffer is too small to hold the requested data.
    E_NOTIMPL:
        Tried to read a bundle attribute for a type which has not been implemented

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
HRESULT DAPI BundleGetBundleInfoFixed(
    __in_z LPCWSTR wzBundleCode,
    __in_z LPCWSTR wzAttribute,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout SIZE_T* pcchValue
    );

/********************************************************************
BundleEnumRelatedBundle - Queries the bundle installation metadata for installs with the given upgrade code.
Enumerate 32-bit and 64-bit in two passes.

RETURNS:
    E_INVALIDARG
        An invalid parameter was passed to the function.
    S_OK
        Related bundle was found.
    S_FALSE
        Related bundle was not found.

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
HRESULT DAPI BundleEnumRelatedBundle(
    __in_z LPCWSTR wzUpgradeCode,
    __in BUNDLE_INSTALL_CONTEXT context,
    __in REG_KEY_BITNESS kbKeyBitness,
    __inout PDWORD pdwStartIndex,
    __deref_out_z LPWSTR* psczBundleCode
    );

/********************************************************************
BundleEnumRelatedBundleFixed - Queries the bundle installation metadata for installs with the given upgrade code
Enumerate 32-bit and 64-bit in two passes.

NOTE: wzBundleCode is a buffer to receive the bundle GUID. This buffer must be 39 characters long.
      The first 38 characters are for the GUID, and the last character is for the terminating null character.

RETURNS:
    E_INVALIDARG
        An invalid parameter was passed to the function.
    S_OK
        Related bundle was found.
    S_FALSE
        Related bundle was not found.

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
HRESULT DAPI BundleEnumRelatedBundleFixed(
    __in_z LPCWSTR wzUpgradeCode,
    __in BUNDLE_INSTALL_CONTEXT context,
    __in REG_KEY_BITNESS kbKeyBitness,
    __inout PDWORD pdwStartIndex,
    __out_ecount(MAX_GUID_CHARS+1) LPWSTR wzBundleCode
    );

/********************************************************************
BundleGetBundleVariable - Queries the bundle installation metadata for a given variable,
    the caller is expected to free the memory returned vis psczValue

RETURNS:
    S_OK
        Success, if the variable had a value, it's returned in psczValue
    E_INVALIDARG
        An invalid parameter was passed to the function.
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT)
        The bundle is not installed
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY)
        The variable is unrecognized
    E_NOTIMPL:
        Tried to read a bundle variable for a type which has not been implemented

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
HRESULT DAPI BundleGetBundleVariable(
    __in_z LPCWSTR wzBundleCode,
    __in_z LPCWSTR wzVariable,
    __deref_out_z LPWSTR* psczValue
    );

/********************************************************************
BundleGetBundleVariableFixed - Queries the bundle installation metadata for a given variable

RETURNS:
    S_OK
        Success, if the variable had a value, it's returned in psczValue
    E_INVALIDARG
        An invalid parameter was passed to the function.
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT)
        The bundle is not installed
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY)
        The variable is unrecognized
    HRESULT_FROM_WIN32(ERROR_MORE_DATA)
        A buffer is too small to hold the requested data.
    E_NOTIMPL:
        Tried to read a bundle variable for a type which has not been implemented

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
HRESULT DAPI BundleGetBundleVariableFixed(
    __in_z LPCWSTR wzBundleCode,
    __in_z LPCWSTR wzVariable,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout SIZE_T* pcchValue
    );

/********************************************************************
BundleQueryRelatedBundles - Queries the bundle installation metadata for installs with the given detect, upgrade, addon, and patch codes.
                            Passes each related bundle to the callback function.
********************************************************************/
HRESULT BundleQueryRelatedBundles(
    __in BUNDLE_INSTALL_CONTEXT installContext,
    __in_z_opt LPCWSTR* rgwzDetectCodes,
    __in DWORD cDetectCodes,
    __in_z_opt LPCWSTR* rgwzUpgradeCodes,
    __in DWORD cUpgradeCodes,
    __in_z_opt LPCWSTR* rgwzAddonCodes,
    __in DWORD cAddonCodes,
    __in_z_opt LPCWSTR* rgwzPatchCodes,
    __in DWORD cPatchCodes,
    __in PFNBUNDLE_QUERY_RELATED_BUNDLE_CALLBACK pfnCallback,
    __in_opt LPVOID pvContext
    );


#ifdef __cplusplus
}
#endif
