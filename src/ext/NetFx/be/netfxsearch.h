#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


// constants

enum NETFX_SEARCH_TYPE
{
    NETFX_SEARCH_TYPE_NONE,
    NETFX_SEARCH_TYPE_NET_CORE_SEARCH,
};

enum NETFX_NET_CORE_RUNTIME_TYPE
{
    NETFX_NET_CORE_RUNTIME_TYPE_CORE,
    NETFX_NET_CORE_RUNTIME_TYPE_ASPNET,
    NETFX_NET_CORE_RUNTIME_TYPE_DESKTOP,
};

enum NETFX_NET_CORE_PLATFORM
{
    NETFX_NET_CORE_PLATFORM_X86,
    NETFX_NET_CORE_PLATFORM_X64,
    NETFX_NET_CORE_PLATFORM_ARM64,
};


// structs

typedef struct _NETFX_SEARCH
{
    LPWSTR sczId;

    NETFX_SEARCH_TYPE Type;
    union
    {
        struct
        {
            NETFX_NET_CORE_RUNTIME_TYPE runtimeType;
            NETFX_NET_CORE_PLATFORM platform;
            LPWSTR sczMajorVersion;
        } NetCoreSearch;
    };
} NETFX_SEARCH;

typedef struct _NETFX_SEARCHES
{
    NETFX_SEARCH* rgSearches;
    DWORD cSearches;
} NETFX_SEARCHES;


// function declarations

STDMETHODIMP NetfxSearchParseFromXml(
    __in NETFX_SEARCHES* pSearches,
    __in IXMLDOMNode* pixnBundleExtension
    );

void NetfxSearchUninitialize(
    __in NETFX_SEARCHES* pSearches
    );

STDMETHODIMP NetfxSearchExecute(
    __in NETFX_SEARCHES* pSearches,
    __in LPCWSTR wzSearchId,
    __in LPCWSTR wzVariable,
    __in IBundleExtensionEngine* pEngine,
    __in LPCWSTR wzBaseDirectory
    );

STDMETHODIMP NetfxSearchFindById(
    __in NETFX_SEARCHES* pSearches,
    __in LPCWSTR wzId,
    __out NETFX_SEARCH** ppSearch
    );
