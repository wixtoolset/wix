// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


STDMETHODIMP UtilSearchParseFromXml(
    __in UTIL_SEARCHES* pSearches,
    __in IXMLDOMNode* pixnBootstrapperExtension
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    BSTR bstrNodeName = NULL;
    LPWSTR scz = NULL;

    // Select Util search nodes.
    hr = XmlSelectNodes(pixnBootstrapperExtension, L"WixWindowsFeatureSearch", &pixnNodes);
    BextExitOnFailure(hr, "Failed to select Util search nodes.");

    // Get Util search node count.
    hr = pixnNodes->get_length((long*)&cNodes);
    BextExitOnFailure(hr, "Failed to get Util search node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // Allocate memory for searches.
    pSearches->rgSearches = (UTIL_SEARCH*)MemAlloc(sizeof(UTIL_SEARCH) * cNodes, TRUE);
    BextExitOnNull(pSearches->rgSearches, hr, E_OUTOFMEMORY, "Failed to allocate memory for search structs.");

    pSearches->cSearches = cNodes;

    // Parse search elements.
    for (DWORD i = 0; i < cNodes; ++i)
    {
        UTIL_SEARCH* pSearch = &pSearches->rgSearches[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, &bstrNodeName);
        BextExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pSearch->sczId);
        BextExitOnFailure(hr, "Failed to get @Id.");

        // Read type specific attributes.
        if (CSTR_EQUAL == ::CompareStringOrdinal(bstrNodeName, -1, L"WixWindowsFeatureSearch", -1, FALSE))
        {
            pSearch->Type = UTIL_SEARCH_TYPE_WINDOWS_FEATURE_SEARCH;

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            BextExitOnFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"sha2CodeSigning", -1, FALSE))
            {
                pSearch->WindowsFeatureSearch.type = UTIL_WINDOWS_FEATURE_SEARCH_TYPE_SHA2_CODE_SIGNING;
            }
            else
            {
                BextExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @Type: %ls", scz);
            }
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
    ReleaseStr(scz);
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
    __in IBootstrapperExtensionEngine* pEngine
    )
{
    HRESULT hr = S_OK;
    UTIL_SEARCH* pSearch = NULL;

    hr = UtilSearchFindById(pSearches, wzSearchId, &pSearch);
    BextExitOnFailure(hr, "Search id '%ls' is unknown to the util extension.", wzSearchId);

    switch (pSearch->Type)
    {
    case UTIL_SEARCH_TYPE_WINDOWS_FEATURE_SEARCH:
        switch (pSearch->WindowsFeatureSearch.type)
        {
        case UTIL_WINDOWS_FEATURE_SEARCH_TYPE_SHA2_CODE_SIGNING:
            hr = UtilPerformDetectSHA2CodeSigning(wzVariable, pSearch, pEngine);
            break;
        default:
            hr = E_UNEXPECTED;
        }
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

        if (CSTR_EQUAL == ::CompareStringOrdinal(pSearch->sczId, -1, wzId, -1, FALSE))
        {
            *ppSearch = pSearch;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}
