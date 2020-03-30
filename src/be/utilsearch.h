#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


// constants

enum UTIL_SEARCH_TYPE
{
    UTIL_SEARCH_TYPE_NONE,
    UTIL_SEARCH_TYPE_DETECT_SHA2_SUPPORT,
};


// structs

typedef struct _UTIL_SEARCH
{
    LPWSTR sczId;

    UTIL_SEARCH_TYPE Type;
} UTIL_SEARCH;

typedef struct _UTIL_SEARCHES
{
    UTIL_SEARCH* rgSearches;
    DWORD cSearches;
} UTIL_SEARCHES;


// function declarations

STDMETHODIMP UtilSearchParseFromXml(
    __in UTIL_SEARCHES* pSearches,
    __in IXMLDOMNode* pixnBundleExtension
    );

void UtilSearchUninitialize(
    __in UTIL_SEARCHES* pSearches
    );

STDMETHODIMP UtilSearchExecute(
    __in UTIL_SEARCHES* pSearches,
    __in LPCWSTR wzSearchId,
    __in LPCWSTR wzVariable,
    __in IBundleExtensionEngine* pEngine
    );

STDMETHODIMP UtilSearchFindById(
    __in UTIL_SEARCHES* pSearches,
    __in LPCWSTR wzId,
    __out UTIL_SEARCH** ppSearch
    );
