// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


STDMETHODIMP UtilSearchParseFromXml(
    __in UTIL_SEARCHES* pSearches,
    __in IXMLDOMNode* pixnBundleExtension
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    BSTR bstrNodeName = NULL;

    // Select Util search nodes.
    hr = XmlSelectNodes(pixnBundleExtension, L"WixDetectSHA2Support", &pixnNodes);
    ExitOnFailure(hr, "Failed to select Util search nodes.");

    // Get Util search node count.
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get Util search node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // Allocate memory for searches.
    pSearches->rgSearches = (UTIL_SEARCH*)MemAlloc(sizeof(UTIL_SEARCH) * cNodes, TRUE);
    ExitOnNull(pSearches->rgSearches, hr, E_OUTOFMEMORY, "Failed to allocate memory for search structs.");

    pSearches->cSearches = cNodes;

    // Parse search elements.
    for (DWORD i = 0; i < cNodes; ++i)
    {
        UTIL_SEARCH* pSearch = &pSearches->rgSearches[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, &bstrNodeName);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pSearch->sczId);
        ExitOnFailure(hr, "Failed to get @Id.");

        // Read type specific attributes.
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"WixDetectSHA2Support", -1))
        {
            pSearch->Type = UTIL_SEARCH_TYPE_DETECT_SHA2_SUPPORT;
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected element name: %ls", bstrNodeName);
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

void UtilSearchUninitialize(
    __in UTIL_SEARCHES* pSearches
    )
{
    if (pSearches->rgSearches)
    {
        for (DWORD i = 0; i < pSearches->cSearches; ++i)
        {
            UTIL_SEARCH* pSearch = &pSearches->rgSearches[i];

            ReleaseStr(pSearch->sczId);
        }
        MemFree(pSearches->rgSearches);
    }
}

STDMETHODIMP UtilSearchExecute(
    __in UTIL_SEARCHES* pSearches,
    __in LPCWSTR wzSearchId,
    __in LPCWSTR wzVariable,
    __in IBundleExtensionEngine* pEngine
    )
{
    HRESULT hr = S_OK;
    UTIL_SEARCH* pSearch = NULL;

    hr = UtilSearchFindById(pSearches, wzSearchId, &pSearch);
    ExitOnFailure(hr, "Search id '%ls' is unknown to the util extension.");

    switch (pSearch->Type)
    {
    case UTIL_SEARCH_TYPE_DETECT_SHA2_SUPPORT:
        hr = UtilPerformDetectSHA2Support(wzVariable, pSearch, pEngine);
        break;
    default:
        hr = E_UNEXPECTED;
    }

LExit:
    return hr;
}

STDMETHODIMP UtilSearchFindById(
    __in UTIL_SEARCHES* pSearches,
    __in LPCWSTR wzId,
    __out UTIL_SEARCH** ppSearch
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pSearches->cSearches; ++i)
    {
        UTIL_SEARCH* pSearch = &pSearches->rgSearches[i];

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
