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
    __in_z LPCWSTR wzBundleId,
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
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzAttribute,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout SIZE_T* pcchValue
    );

/********************************************************************
BundleEnumRelatedBundle - Queries the bundle installation metadata for installs with the given upgrade code
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
    __inout PDWORD pdwStartIndex,
    __deref_out_z LPWSTR* psczBundleId
    );

/********************************************************************
BundleEnumRelatedBundleFixed - Queries the bundle installation metadata for installs with the given upgrade code

NOTE: lpBundleIdBuff is a buffer to receive the bundle GUID. This buffer must be 39 characters long.
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
    __inout PDWORD pdwStartIndex,
    __out_ecount(MAX_GUID_CHARS+1) LPWSTR wzBundleId
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
    __in_z LPCWSTR wzBundleId,
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
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzVariable,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout SIZE_T* pcchValue
    );


#ifdef __cplusplus
}
#endif
