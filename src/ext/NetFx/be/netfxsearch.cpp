// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


STDMETHODIMP NetfxSearchParseFromXml(
    __in NETFX_SEARCHES* pSearches,
    __in IXMLDOMNode* pixnBundleExtension
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    BSTR bstrNodeName = NULL;

    // Select Netfx search nodes.
    hr = XmlSelectNodes(pixnBundleExtension, L"NetFxNetCoreSearch", &pixnNodes);
    BextExitOnFailure(hr, "Failed to select Netfx search nodes.");

    // Get Netfx search node count.
    hr = pixnNodes->get_length((long*)&cNodes);
    BextExitOnFailure(hr, "Failed to get Netfx search node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // Allocate memory for searches.
    pSearches->rgSearches = (NETFX_SEARCH*)MemAlloc(sizeof(NETFX_SEARCH) * cNodes, TRUE);
    BextExitOnNull(pSearches->rgSearches, hr, E_OUTOFMEMORY, "Failed to allocate memory for search structs.");

    pSearches->cSearches = cNodes;

    // Parse search elements.
    for (DWORD i = 0; i < cNodes; ++i)
    {
        NETFX_SEARCH* pSearch = &pSearches->rgSearches[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, &bstrNodeName);
        BextExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pSearch->sczId);
        BextExitOnFailure(hr, "Failed to get @Id.");

        // Read type specific attributes.
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"NetFxNetCoreSearch", -1))
        {
            pSearch->Type = NETFX_SEARCH_TYPE_NET_CORE_SEARCH;

            // @RuntimeType
            hr = XmlGetAttributeUInt32(pixnNode, L"RuntimeType", reinterpret_cast<DWORD*>(&pSearch->NetCoreSearch.runtimeType));
            BextExitOnFailure(hr, "Failed to get @RuntimeType.");

            // @Platform
            hr = XmlGetAttributeUInt32(pixnNode, L"Platform", reinterpret_cast<DWORD*>(&pSearch->NetCoreSearch.platform));
            BextExitOnFailure(hr, "Failed to get @Platform.");

            // @MajorVersion
            hr = XmlGetAttributeEx(pixnNode, L"MajorVersion", &pSearch->NetCoreSearch.sczMajorVersion);
            BextExitOnFailure(hr, "Failed to get @MajorVersion.");
        }
        else
        {
            BextExitWithRootFailure(hr, E_UNEXPECTED, "Unexpected element name: %ls", bstrNodeName);
        }

        // prepare next iteration
        ReleaseNullObject(pixnNode);
        ReleaseNullBSTR(bstrNodeName);
    }

LExit:
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pixnNode);
    ReleaseObject(pixnNodes);

    return hr;
}

void NetfxSearchUninitialize(
    __in NETFX_SEARCHES* pSearches
    )
{
    if (pSearches->rgSearches)
    {
        for (DWORD i = 0; i < pSearches->cSearches; ++i)
        {
            NETFX_SEARCH* pSearch = &pSearches->rgSearches[i];

            ReleaseStr(pSearch->sczId);
        }
        MemFree(pSearches->rgSearches);
    }
}

STDMETHODIMP NetfxSearchExecute(
    __in NETFX_SEARCHES* pSearches,
    __in LPCWSTR wzSearchId,
    __in LPCWSTR wzVariable,
    __in IBundleExtensionEngine* pEngine,
    __in LPCWSTR wzBaseDirectory
    )
{
    HRESULT hr = S_OK;
    NETFX_SEARCH* pSearch = NULL;

    hr = NetfxSearchFindById(pSearches, wzSearchId, &pSearch);
    BextExitOnFailure(hr, "Search id '%ls' is unknown to the util extension.", wzSearchId);

    switch (pSearch->Type)
    {
    case NETFX_SEARCH_TYPE_NET_CORE_SEARCH:
        hr = NetfxPerformDetectNetCore(wzVariable, pSearch, pEngine, wzBaseDirectory);
        break;
    default:
        hr = E_UNEXPECTED;
    }

LExit:
    return hr;
}

STDMETHODIMP NetfxSearchFindById(
    __in NETFX_SEARCHES* pSearches,
    __in LPCWSTR wzId,
    __out NETFX_SEARCH** ppSearch
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pSearches->cSearches; ++i)
    {
        NETFX_SEARCH* pSearch = &pSearches->rgSearches[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pSearch->sczId, -1, wzId, -1))
        {
            *ppSearch = pSearch;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}
