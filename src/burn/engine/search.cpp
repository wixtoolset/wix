// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// internal function declarations

static HRESULT DirectorySearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT DirectorySearchPath(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT FileSearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT FileSearchVersion(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT FileSearchPath(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT RegistrySearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT RegistrySearchValue(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT MsiComponentSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT MsiProductSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT PerformExtensionSearch(
    __in BURN_SEARCH* pSearch
    );
static HRESULT PerformSetVariable(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
);


// function definitions

extern "C" HRESULT SearchesParseFromXml(
    __in BURN_SEARCHES* pSearches,
    __in BURN_EXTENSIONS* pBurnExtensions,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    BSTR bstrNodeName = NULL;
    BOOL fXmlFound = FALSE;
    LPWSTR scz = NULL;

    // select search nodes
    hr = XmlSelectNodes(pixnBundle, L"DirectorySearch|FileSearch|RegistrySearch|MsiComponentSearch|MsiProductSearch|MsiFeatureSearch|ExtensionSearch|SetVariable", &pixnNodes);
    ExitOnFailure(hr, "Failed to select search nodes.");

    // get search node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnRootFailure(hr, "Failed to get search node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // allocate memory for searches
    pSearches->rgSearches = (BURN_SEARCH*)MemAlloc(sizeof(BURN_SEARCH) * cNodes, TRUE);
    ExitOnNull(pSearches->rgSearches, hr, E_OUTOFMEMORY, "Failed to allocate memory for search structs.");

    pSearches->cSearches = cNodes;

    // parse search elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_SEARCH* pSearch = &pSearches->rgSearches[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, &bstrNodeName);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pSearch->sczKey);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Id.");

        // @Variable
        hr = XmlGetAttributeEx(pixnNode, L"Variable", &pSearch->sczVariable);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Variable.");

        // @Condition
        hr = XmlGetAttributeEx(pixnNode, L"Condition", &pSearch->sczCondition);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @Condition.");

        // read type specific attributes
        if (CSTR_EQUAL == ::CompareStringOrdinal(bstrNodeName, -1, L"DirectorySearch", -1, FALSE))
        {
            pSearch->Type = BURN_SEARCH_TYPE_DIRECTORY;

            // @Path
            hr = XmlGetAttributeEx(pixnNode, L"Path", &pSearch->DirectorySearch.sczPath);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Path.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"exists", -1, FALSE))
            {
                pSearch->DirectorySearch.Type = BURN_DIRECTORY_SEARCH_TYPE_EXISTS;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"path", -1, FALSE))
            {
                pSearch->DirectorySearch.Type = BURN_DIRECTORY_SEARCH_TYPE_PATH;
            }
            else
            {
                ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringOrdinal(bstrNodeName, -1, L"FileSearch", -1, FALSE))
        {
            pSearch->Type = BURN_SEARCH_TYPE_FILE;

            // @Path
            hr = XmlGetAttributeEx(pixnNode, L"Path", &pSearch->FileSearch.sczPath);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Path.");

            // @DisableFileRedirection
            hr = XmlGetYesNoAttribute(pixnNode, L"DisableFileRedirection", &pSearch->FileSearch.fDisableFileRedirection);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get DisableFileRedirection attribute.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"exists", -1, FALSE))
            {
                pSearch->FileSearch.Type = BURN_FILE_SEARCH_TYPE_EXISTS;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"version", -1, FALSE))
            {
                pSearch->FileSearch.Type = BURN_FILE_SEARCH_TYPE_VERSION;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"path", -1, FALSE))
            {
                pSearch->FileSearch.Type = BURN_FILE_SEARCH_TYPE_PATH;
            }
            else
            {
                ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringOrdinal(bstrNodeName, -1, L"RegistrySearch", -1, FALSE))
        {
            pSearch->Type = BURN_SEARCH_TYPE_REGISTRY;

            // @Root
            hr = XmlGetAttributeEx(pixnNode, L"Root", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Root.");

            if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"HKCR", -1, FALSE))
            {
                pSearch->RegistrySearch.hRoot = HKEY_CLASSES_ROOT;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"HKCU", -1, FALSE))
            {
                pSearch->RegistrySearch.hRoot = HKEY_CURRENT_USER;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"HKLM", -1, FALSE))
            {
                pSearch->RegistrySearch.hRoot = HKEY_LOCAL_MACHINE;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"HKU", -1, FALSE))
            {
                pSearch->RegistrySearch.hRoot = HKEY_USERS;
            }
            else
            {
                ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @Root: %ls", scz);
            }

            // @Key
            hr = XmlGetAttributeEx(pixnNode, L"Key", &pSearch->RegistrySearch.sczKey);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get Key attribute.");

            // @Value
            hr = XmlGetAttributeEx(pixnNode, L"Value", &pSearch->RegistrySearch.sczValue);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get Value attribute.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Type.");

            hr = XmlGetYesNoAttribute(pixnNode, L"Win64", &pSearch->RegistrySearch.fWin64);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get Win64 attribute.");

            if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"exists", -1, FALSE))
            {
                pSearch->RegistrySearch.Type = BURN_REGISTRY_SEARCH_TYPE_EXISTS;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"value", -1, FALSE))
            {
                pSearch->RegistrySearch.Type = BURN_REGISTRY_SEARCH_TYPE_VALUE;

                // @ExpandEnvironment
                hr = XmlGetYesNoAttribute(pixnNode, L"ExpandEnvironment", &pSearch->RegistrySearch.fExpandEnvironment);
                ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @ExpandEnvironment.");

                // @VariableType
                hr = XmlGetAttributeEx(pixnNode, L"VariableType", &scz);
                ExitOnRequiredXmlQueryFailure(hr, "Failed to get @VariableType.");

                if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"formatted", -1, FALSE))
                {
                    pSearch->RegistrySearch.VariableType = BURN_VARIANT_TYPE_FORMATTED;
                }
                else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"numeric", -1, FALSE))
                {
                    pSearch->RegistrySearch.VariableType = BURN_VARIANT_TYPE_NUMERIC;
                }
                else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"string", -1, FALSE))
                {
                    pSearch->RegistrySearch.VariableType = BURN_VARIANT_TYPE_STRING;
                }
                else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"version", -1, FALSE))
                {
                    pSearch->RegistrySearch.VariableType = BURN_VARIANT_TYPE_VERSION;
                }
                else
                {
                    ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @VariableType: %ls", scz);
                }
            }
            else
            {
                ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringOrdinal(bstrNodeName, -1, L"MsiComponentSearch", -1, FALSE))
        {
            pSearch->Type = BURN_SEARCH_TYPE_MSI_COMPONENT;

            // @ProductCode
            hr = XmlGetAttributeEx(pixnNode, L"ProductCode", &pSearch->MsiComponentSearch.sczProductCode);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @ProductCode.");

            // @ComponentId
            hr = XmlGetAttributeEx(pixnNode, L"ComponentId", &pSearch->MsiComponentSearch.sczComponentId);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @ComponentId.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"keyPath", -1, FALSE))
            {
                pSearch->MsiComponentSearch.Type = BURN_MSI_COMPONENT_SEARCH_TYPE_KEYPATH;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"state", -1, FALSE))
            {
                pSearch->MsiComponentSearch.Type = BURN_MSI_COMPONENT_SEARCH_TYPE_STATE;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"directory", -1, FALSE))
            {
                pSearch->MsiComponentSearch.Type = BURN_MSI_COMPONENT_SEARCH_TYPE_DIRECTORY;
            }
            else
            {
                ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringOrdinal(bstrNodeName, -1, L"MsiProductSearch", -1, FALSE))
        {
            pSearch->Type = BURN_SEARCH_TYPE_MSI_PRODUCT;
            pSearch->MsiProductSearch.GuidType = BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_NONE;

            // @ProductCode (if we don't find a product code then look for an upgrade code)
            hr = XmlGetAttributeEx(pixnNode, L"ProductCode", &pSearch->MsiProductSearch.sczGuid);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @ProductCode.");

            if (fXmlFound)
            {
                pSearch->MsiProductSearch.GuidType = BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_PRODUCTCODE;
            }
            else
            {
                // @UpgradeCode
                hr = XmlGetAttributeEx(pixnNode, L"UpgradeCode", &pSearch->MsiProductSearch.sczGuid);
                ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @UpgradeCode.");

                if (fXmlFound)
                {
                    pSearch->MsiProductSearch.GuidType = BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_UPGRADECODE;
                }
            }

            // make sure we found either a product or upgrade code
            if (BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_NONE == pSearch->MsiProductSearch.GuidType)
            {
                ExitWithRootFailure(hr, E_NOTFOUND, "Failed to get @ProductCode or @UpgradeCode.");
            }

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"version", -1, FALSE))
            {
                pSearch->MsiProductSearch.Type = BURN_MSI_PRODUCT_SEARCH_TYPE_VERSION;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"language", -1, FALSE))
            {
                pSearch->MsiProductSearch.Type = BURN_MSI_PRODUCT_SEARCH_TYPE_LANGUAGE;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"state", -1, FALSE))
            {
                pSearch->MsiProductSearch.Type = BURN_MSI_PRODUCT_SEARCH_TYPE_STATE;
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"assignment", -1, FALSE))
            {
                pSearch->MsiProductSearch.Type = BURN_MSI_PRODUCT_SEARCH_TYPE_ASSIGNMENT;
            }
            else
            {
                ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringOrdinal(bstrNodeName, -1, L"ExtensionSearch", -1, FALSE))
        {
            pSearch->Type = BURN_SEARCH_TYPE_EXTENSION;

            // @ExtensionId
            hr = XmlGetAttributeEx(pixnNode, L"ExtensionId", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @ExtensionId.");

            hr = BurnExtensionFindById(pBurnExtensions, scz, &pSearch->ExtensionSearch.pExtension);
            ExitOnRootFailure(hr, "Failed to find extension '%ls' for search '%ls'", scz, pSearch->sczKey);
        }
        else if (CSTR_EQUAL == ::CompareStringOrdinal(bstrNodeName, -1, L"SetVariable", -1, FALSE))
        {
            pSearch->Type = BURN_SEARCH_TYPE_SET_VARIABLE;

            // @Value
            hr = XmlGetAttributeEx(pixnNode, L"Value", &scz);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @Value.");

            if (fXmlFound)
            {
                pSearch->SetVariable.sczValue = scz;
                scz = NULL;

                // @Type
                hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
                ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Type.");

                if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"formatted", -1, FALSE))
                {
                    pSearch->SetVariable.targetType = BURN_VARIANT_TYPE_FORMATTED;
                }
                else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"numeric", -1, FALSE))
                {
                    pSearch->SetVariable.targetType = BURN_VARIANT_TYPE_NUMERIC;
                }
                else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"string", -1, FALSE))
                {
                    pSearch->SetVariable.targetType = BURN_VARIANT_TYPE_STRING;
                }
                else if (CSTR_EQUAL == ::CompareStringOrdinal(scz, -1, L"version", -1, FALSE))
                {
                    pSearch->SetVariable.targetType = BURN_VARIANT_TYPE_VERSION;
                }
                else
                {
                    ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @Type: %ls", scz);
                }
            }
            else
            {
                pSearch->SetVariable.targetType = BURN_VARIANT_TYPE_NONE;
            }
        }
        else
        {
            ExitWithRootFailure(hr, E_UNEXPECTED, "Unexpected element name: %ls", bstrNodeName);
        }

        // prepare next iteration
        ReleaseNullObject(pixnNode);
        ReleaseNullBSTR(bstrNodeName);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseBSTR(bstrNodeName);
    ReleaseStr(scz);
    return hr;
}

extern "C" HRESULT SearchesExecute(
    __in BURN_SEARCHES* pSearches,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BOOL f = FALSE;

    for (DWORD i = 0; i < pSearches->cSearches; ++i)
    {
        BURN_SEARCH* pSearch = &pSearches->rgSearches[i];

        // evaluate condition
        if (pSearch->sczCondition && *pSearch->sczCondition)
        {
            hr = ConditionEvaluate(pVariables, pSearch->sczCondition, &f);
            if (E_INVALIDDATA == hr)
            {
                TraceError(hr, "Failed to parse search condition. Id = '%ls', Condition = '%ls'", pSearch->sczKey, pSearch->sczCondition);
                hr = S_OK;
                continue;
            }
            ExitOnFailure(hr, "Failed to evaluate search condition. Id = '%ls', Condition = '%ls'", pSearch->sczKey, pSearch->sczCondition);

            if (!f)
            {
                continue; // condition evaluated to false, skip
            }
        }

        switch (pSearch->Type)
        {
        case BURN_SEARCH_TYPE_DIRECTORY:
            switch (pSearch->DirectorySearch.Type)
            {
            case BURN_DIRECTORY_SEARCH_TYPE_EXISTS:
                hr = DirectorySearchExists(pSearch, pVariables);
                break;
            case BURN_DIRECTORY_SEARCH_TYPE_PATH:
                hr = DirectorySearchPath(pSearch, pVariables);
                break;
            default:
                hr = E_UNEXPECTED;
            }
            break;
        case BURN_SEARCH_TYPE_FILE:
            switch (pSearch->FileSearch.Type)
            {
            case BURN_FILE_SEARCH_TYPE_EXISTS:
                hr = FileSearchExists(pSearch, pVariables);
                break;
            case BURN_FILE_SEARCH_TYPE_VERSION:
                hr = FileSearchVersion(pSearch, pVariables);
                break;
            case BURN_FILE_SEARCH_TYPE_PATH:
                hr = FileSearchPath(pSearch, pVariables);
                break;
            default:
                hr = E_UNEXPECTED;
            }
            break;
        case BURN_SEARCH_TYPE_REGISTRY:
            switch (pSearch->RegistrySearch.Type)
            {
            case BURN_REGISTRY_SEARCH_TYPE_EXISTS:
                hr = RegistrySearchExists(pSearch, pVariables);
                break;
            case BURN_REGISTRY_SEARCH_TYPE_VALUE:
                hr = RegistrySearchValue(pSearch, pVariables);
                break;
            default:
                hr = E_UNEXPECTED;
            }
            break;
        case BURN_SEARCH_TYPE_MSI_COMPONENT:
            hr = MsiComponentSearch(pSearch, pVariables);
            break;
        case BURN_SEARCH_TYPE_MSI_PRODUCT:
            hr = MsiProductSearch(pSearch, pVariables);
            break;
        case BURN_SEARCH_TYPE_EXTENSION:
            hr = PerformExtensionSearch(pSearch);
            break;
        case BURN_SEARCH_TYPE_SET_VARIABLE:
            hr = PerformSetVariable(pSearch, pVariables);
            break;
        default:
            hr = E_UNEXPECTED;
        }

        if (FAILED(hr))
        {
            TraceError(hr, "Search failed. Id = '%ls'", pSearch->sczKey);
            continue;
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

extern "C" void SearchesUninitialize(
    __in BURN_SEARCHES* pSearches
    )
{
    if (pSearches->rgSearches)
    {
        for (DWORD i = 0; i < pSearches->cSearches; ++i)
        {
            BURN_SEARCH* pSearch = &pSearches->rgSearches[i];

            ReleaseStr(pSearch->sczKey);
            ReleaseStr(pSearch->sczVariable);
            ReleaseStr(pSearch->sczCondition);

            switch (pSearch->Type)
            {
            case BURN_SEARCH_TYPE_DIRECTORY:
                ReleaseStr(pSearch->DirectorySearch.sczPath);
                break;
            case BURN_SEARCH_TYPE_FILE:
                ReleaseStr(pSearch->FileSearch.sczPath);
                break;
            case BURN_SEARCH_TYPE_REGISTRY:
                ReleaseStr(pSearch->RegistrySearch.sczKey);
                ReleaseStr(pSearch->RegistrySearch.sczValue);
                break;
            case BURN_SEARCH_TYPE_MSI_COMPONENT:
                ReleaseStr(pSearch->MsiComponentSearch.sczProductCode);
                ReleaseStr(pSearch->MsiComponentSearch.sczComponentId);
                break;
            case BURN_SEARCH_TYPE_MSI_PRODUCT:
                ReleaseStr(pSearch->MsiProductSearch.sczGuid);
                break;
            case BURN_SEARCH_TYPE_SET_VARIABLE:
                ReleaseStr(pSearch->SetVariable.sczValue);
                break;
            }
        }
        MemFree(pSearches->rgSearches);
    }
}


// internal function definitions

#if !defined(_WIN64)

typedef struct _BURN_FILE_SEARCH
{
    BURN_SEARCH* pSearch;
    PROC_FILESYSTEMREDIRECTION pfsr;
} BURN_FILE_SEARCH;

static HRESULT FileSystemSearchStart(
    __in BURN_FILE_SEARCH* pFileSearch
    )
{
    HRESULT hr = S_OK;

    if (pFileSearch->pSearch->FileSearch.fDisableFileRedirection)
    {
        hr = ProcDisableWowFileSystemRedirection(&pFileSearch->pfsr);
        if (hr == E_NOTIMPL)
        {
            hr = S_FALSE;
        }
        ExitOnFailure(hr, "Failed to disable file system redirection.");
    }

LExit:
    return hr;
}

static void FileSystemSearchEnd(
    __in BURN_FILE_SEARCH* pFileSearch
    )
{
    HRESULT hr = S_OK;

    hr = ProcRevertWowFileSystemRedirection(&pFileSearch->pfsr);
    ExitOnFailure(hr, "Failed to revert file system redirection.");

LExit:
    return;
}

#endif

static HRESULT DirectorySearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR sczPath = NULL;
    BOOL fExists = FALSE;

#if !defined(_WIN64)
    BURN_FILE_SEARCH bfs = { };

    bfs.pSearch = pSearch;

    hr = FileSystemSearchStart(&bfs);
    ExitOnFailure(hr, "Failed to initialize file search.");
#endif

    // format path
    hr = VariableFormatString(pVariables, pSearch->DirectorySearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format variable string.");

    DWORD dwAttributes = ::GetFileAttributesW(sczPath);
    if (INVALID_FILE_ATTRIBUTES == dwAttributes)
    {
        er = ::GetLastError();
        if (ERROR_FILE_NOT_FOUND == er || ERROR_PATH_NOT_FOUND == er)
        {
            LogStringLine(REPORT_STANDARD, "Directory search: %ls, did not find path: %ls", pSearch->sczKey, pSearch->DirectorySearch.sczPath);
        }
        else
        {
            ExitOnWin32Error(er, hr, "Directory search: %ls, failed get to directory attributes. '%ls'", pSearch->sczKey, pSearch->DirectorySearch.sczPath);
        }
    }
    else if (FILE_ATTRIBUTE_DIRECTORY != (dwAttributes & FILE_ATTRIBUTE_DIRECTORY))
    {
        LogStringLine(REPORT_STANDARD, "Directory search: %ls, found file at path: %ls", pSearch->sczKey, pSearch->DirectorySearch.sczPath);
    }
    else
    {
        fExists = TRUE;
    }

    // set variable
    hr = VariableSetNumeric(pVariables, pSearch->sczVariable, fExists, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
#if !defined(_WIN64)
    FileSystemSearchEnd(&bfs);
#endif

    StrSecureZeroFreeString(sczPath);

    return hr;
}

static HRESULT DirectorySearchPath(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

#if !defined(_WIN64)
    BURN_FILE_SEARCH bfs = { };

    bfs.pSearch = pSearch;

    hr = FileSystemSearchStart(&bfs);
    ExitOnFailure(hr, "Failed to initialize file search.");
#endif

    // format path
    hr = VariableFormatString(pVariables, pSearch->DirectorySearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format variable string.");

    DWORD dwAttributes = ::GetFileAttributesW(sczPath);
    if (INVALID_FILE_ATTRIBUTES == dwAttributes)
    {
        hr = HRESULT_FROM_WIN32(::GetLastError());
    }
    else if (dwAttributes & FILE_ATTRIBUTE_DIRECTORY)
    {
        hr = VariableSetString(pVariables, pSearch->sczVariable, sczPath, FALSE, FALSE);
        ExitOnFailure(hr, "Failed to set directory search path variable.");
    }
    else // must have found a file.
    {
        hr = E_PATHNOTFOUND;
    }

    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        LogStringLine(REPORT_STANDARD, "Directory search: %ls, did not find path: %ls, reason: 0x%x", pSearch->sczKey, pSearch->DirectorySearch.sczPath, hr);
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed while searching directory search: %ls, for path: %ls", pSearch->sczKey, pSearch->DirectorySearch.sczPath);

LExit:
#if !defined(_WIN64)
    FileSystemSearchEnd(&bfs);
#endif

    StrSecureZeroFreeString(sczPath);

    return hr;
}

static HRESULT FileSearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR sczPath = NULL;
    BOOL fExists = FALSE;

#if !defined(_WIN64)
    BURN_FILE_SEARCH bfs = { };

    bfs.pSearch = pSearch;

    hr = FileSystemSearchStart(&bfs);
    ExitOnFailure(hr, "Failed to initialize file search.");
#endif

    // format path
    hr = VariableFormatString(pVariables, pSearch->FileSearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format variable string.");

    // find file
    DWORD dwAttributes = ::GetFileAttributesW(sczPath);
    if (INVALID_FILE_ATTRIBUTES == dwAttributes)
    {
        er = ::GetLastError();
        if (ERROR_FILE_NOT_FOUND == er || ERROR_PATH_NOT_FOUND == er)
        {
            LogStringLine(REPORT_STANDARD, "File search: %ls, did not find path: %ls", pSearch->sczKey, pSearch->FileSearch.sczPath);
        }
        else
        {
            ExitOnWin32Error(er, hr, "File search: %ls, failed get to file attributes. '%ls'", pSearch->sczKey, pSearch->FileSearch.sczPath);
        }
    }
    else if (FILE_ATTRIBUTE_DIRECTORY == (dwAttributes & FILE_ATTRIBUTE_DIRECTORY))
    {
        LogStringLine(REPORT_STANDARD, "File search: %ls, found directory at path: %ls", pSearch->sczKey, pSearch->FileSearch.sczPath);
    }
    else
    {
        fExists = TRUE;
    }

    // set variable
    hr = VariableSetNumeric(pVariables, pSearch->sczVariable, fExists, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
#if !defined(_WIN64)
    FileSystemSearchEnd(&bfs);
#endif

    StrSecureZeroFreeString(sczPath);
    return hr;
}

static HRESULT FileSearchVersion(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    ULARGE_INTEGER uliVersion = { };
    LPWSTR sczPath = NULL;
    VERUTIL_VERSION* pVersion = NULL;

#if !defined(_WIN64)
    BURN_FILE_SEARCH bfs = { };

    bfs.pSearch = pSearch;

    hr = FileSystemSearchStart(&bfs);
    ExitOnFailure(hr, "Failed to initialize file search.");
#endif

    // format path
    hr = VariableFormatString(pVariables, pSearch->FileSearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format path string.");

    // get file version
    hr = FileVersion(sczPath, &uliVersion.HighPart, &uliVersion.LowPart);
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        LogStringLine(REPORT_STANDARD, "File search: %ls, did not find path: %ls", pSearch->sczKey, pSearch->FileSearch.sczPath);
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to get file version.");

    hr = VerVersionFromQword(uliVersion.QuadPart, &pVersion);
    ExitOnFailure(hr, "Failed to create version from file version.");

    // set variable
    hr = VariableSetVersion(pVariables, pSearch->sczVariable, pVersion, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
#if !defined(_WIN64)
    FileSystemSearchEnd(&bfs);
#endif

    StrSecureZeroFreeString(sczPath);
    ReleaseVerutilVersion(pVersion);
    return hr;
}

static HRESULT FileSearchPath(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

#if !defined(_WIN64)
    BURN_FILE_SEARCH bfs = { };

    bfs.pSearch = pSearch;

    hr = FileSystemSearchStart(&bfs);
    ExitOnFailure(hr, "Failed to initialize file search.");
#endif

    // format path
    hr = VariableFormatString(pVariables, pSearch->FileSearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format variable string.");

    DWORD dwAttributes = ::GetFileAttributesW(sczPath);
    if (INVALID_FILE_ATTRIBUTES == dwAttributes)
    {
        hr = HRESULT_FROM_WIN32(::GetLastError());
    }
    else if (dwAttributes & FILE_ATTRIBUTE_DIRECTORY) // found a directory.
    {
        hr = E_FILENOTFOUND;
    }
    else // found our file.
    {
        hr = VariableSetString(pVariables, pSearch->sczVariable, sczPath, FALSE, FALSE);
        ExitOnFailure(hr, "Failed to set variable to file search path.");
    }

    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        LogStringLine(REPORT_STANDARD, "File search: %ls, did not find path: %ls", pSearch->sczKey, pSearch->FileSearch.sczPath);
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed while searching file search: %ls, for path: %ls", pSearch->sczKey, pSearch->FileSearch.sczPath);

LExit:
#if !defined(_WIN64)
    FileSystemSearchEnd(&bfs);
#endif

    StrSecureZeroFreeString(sczPath);

    return hr;
}

static HRESULT RegistrySearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR sczKey = NULL;
    LPWSTR sczValue = NULL;
    HKEY hKey = NULL;
    DWORD dwType = 0;
    BOOL fExists = FALSE;

    // format key string
    hr = VariableFormatString(pVariables, pSearch->RegistrySearch.sczKey, &sczKey, NULL);
    ExitOnFailure(hr, "Failed to format key string.");

    // open key
    hr = RegOpenEx(pSearch->RegistrySearch.hRoot, sczKey, KEY_QUERY_VALUE, pSearch->RegistrySearch.fWin64 ? REG_KEY_64BIT : REG_KEY_32BIT, &hKey);
    ExitOnPathFailure(hr, fExists, "Failed to open registry key. Key = '%ls'", pSearch->RegistrySearch.sczKey);

    if (!fExists)
    {
        LogStringLine(REPORT_STANDARD, "Registry key not found. Key = '%ls'", pSearch->RegistrySearch.sczKey);
    }
    else if (pSearch->RegistrySearch.sczValue)
    {
        // format value string
        hr = VariableFormatString(pVariables, pSearch->RegistrySearch.sczValue, &sczValue, NULL);
        ExitOnFailure(hr, "Failed to format value string.");

        // query value
        er = ::RegQueryValueExW(hKey, sczValue, NULL, &dwType, NULL, NULL);
        switch (er)
        {
        case ERROR_SUCCESS:
            fExists = TRUE;
            break;
        case ERROR_FILE_NOT_FOUND:
            LogStringLine(REPORT_STANDARD, "Registry value not found. Key = '%ls', Value = '%ls'", pSearch->RegistrySearch.sczKey, pSearch->RegistrySearch.sczValue);
            fExists = FALSE;
            break;
        default:
            ExitOnWin32Error(er, hr, "Failed to query registry key value.");
        }
    }

    // set variable
    hr = VariableSetNumeric(pVariables, pSearch->sczVariable, fExists, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    if (FAILED(hr))
    {
        LogStringLine(REPORT_STANDARD, "RegistrySearchExists failed: ID '%ls', HRESULT 0x%x", pSearch->sczKey, hr);
    }

    StrSecureZeroFreeString(sczKey);
    StrSecureZeroFreeString(sczValue);
    ReleaseRegKey(hKey);

    return hr;
}

static HRESULT RegistrySearchValue(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;
    LPWSTR sczValue = NULL;
    HKEY hKey = NULL;
    BOOL fExists = FALSE;
    DWORD dwType = 0;
    SIZE_T cbData = 0;
    LPBYTE pData = NULL;
    BURN_VARIANT value = { };
    DWORD dwValue = 0;
    LONGLONG llValue = 0;

    // format key string
    hr = VariableFormatString(pVariables, pSearch->RegistrySearch.sczKey, &sczKey, NULL);
    ExitOnFailure(hr, "Failed to format key string.");

    // format value string
    if (pSearch->RegistrySearch.sczValue)
    {
        hr = VariableFormatString(pVariables, pSearch->RegistrySearch.sczValue, &sczValue, NULL);
        ExitOnFailure(hr, "Failed to format value string.");
    }

    // open key
    hr = RegOpenEx(pSearch->RegistrySearch.hRoot, sczKey, KEY_QUERY_VALUE, pSearch->RegistrySearch.fWin64 ? REG_KEY_64BIT : REG_KEY_32BIT, &hKey);
    ExitOnPathFailure(hr, fExists, "Failed to open registry key.");

    if (!fExists)
    {
        LogStringLine(REPORT_STANDARD, "Registry key not found. Key = '%ls'", pSearch->RegistrySearch.sczKey);

        ExitFunction();
    }

    // get value
    hr = RegReadValue(hKey, sczValue, pSearch->RegistrySearch.fExpandEnvironment, &pData, &cbData, &dwType);
    if (E_FILENOTFOUND == hr)
    {
        LogStringLine(REPORT_STANDARD, "Registry value not found. Key = '%ls', Value = '%ls'", pSearch->RegistrySearch.sczKey, pSearch->RegistrySearch.sczValue);

        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to query registry key value.");

    switch (dwType)
    {
    case REG_DWORD:
        if (memcpy_s(&dwValue, sizeof(DWORD), pData, cbData))
        {
            ExitFunction1(hr = E_UNEXPECTED);
        }
        hr = BVariantSetNumeric(&value, dwValue);
        break;
    case REG_QWORD:
        if (memcpy_s(&llValue, sizeof(LONGLONG), pData, cbData))
        {
            ExitFunction1(hr = E_UNEXPECTED);
        }
        hr = BVariantSetNumeric(&value, llValue);
        break;
    case REG_EXPAND_SZ: __fallthrough;
    case REG_SZ:
        hr = BVariantSetString(&value, (LPCWSTR)pData, 0, FALSE);
        break;
    default:
        ExitWithRootFailure(hr, E_NOTIMPL, "Unsupported registry key value type. Type = '%u'", dwType);
    }
    ExitOnFailure(hr, "Failed to read registry value.");

    // change value to requested type
    hr = BVariantChangeType(&value, pSearch->RegistrySearch.VariableType);
    ExitOnFailure(hr, "Failed to change value type.");

    // Set variable.
    hr = VariableSetVariant(pVariables, pSearch->sczVariable, &value);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    if (FAILED(hr))
    {
        LogStringLine(REPORT_STANDARD, "RegistrySearchValue failed: ID '%ls', HRESULT 0x%x", pSearch->sczKey, hr);
    }

    StrSecureZeroFreeString(sczKey);
    StrSecureZeroFreeString(sczValue);
    ReleaseRegKey(hKey);
    ReleaseMem(pData);
    BVariantUninitialize(&value);

    return hr;
}

static HRESULT MsiComponentSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    INSTALLSTATE is = INSTALLSTATE_BROKEN;
    LPWSTR sczComponentId = NULL;
    LPWSTR sczProductCode = NULL;
    LPWSTR sczPath = NULL;

    // format component id string
    hr = VariableFormatString(pVariables, pSearch->MsiComponentSearch.sczComponentId, &sczComponentId, NULL);
    ExitOnFailure(hr, "Failed to format component id string.");

    if (pSearch->MsiComponentSearch.sczProductCode)
    {
        // format product code string
        hr = VariableFormatString(pVariables, pSearch->MsiComponentSearch.sczProductCode, &sczProductCode, NULL);
        ExitOnFailure(hr, "Failed to format product code string.");
    }

    if (sczProductCode)
    {
        hr = WiuGetComponentPath(sczProductCode, sczComponentId, &is, &sczPath);
    }
    else
    {
        hr = WiuLocateComponent(sczComponentId, &is, &sczPath);
    }

    if (INSTALLSTATE_SOURCEABSENT == is)
    {
        is = INSTALLSTATE_SOURCE;
    }
    else if (INSTALLSTATE_UNKNOWN == is || INSTALLSTATE_NOTUSED == is)
    {
        is = INSTALLSTATE_ABSENT;
    }
    else if (INSTALLSTATE_ABSENT != is && INSTALLSTATE_LOCAL != is && INSTALLSTATE_SOURCE != is)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Failed to get component path: %d", is);
    }

    // set variable
    switch (pSearch->MsiComponentSearch.Type)
    {
    case BURN_MSI_COMPONENT_SEARCH_TYPE_KEYPATH:
        if (INSTALLSTATE_ABSENT == is || INSTALLSTATE_LOCAL == is || INSTALLSTATE_SOURCE == is)
        {
            hr = VariableSetString(pVariables, pSearch->sczVariable, sczPath, FALSE, FALSE);
        }
        break;
    case BURN_MSI_COMPONENT_SEARCH_TYPE_STATE:
        hr = VariableSetNumeric(pVariables, pSearch->sczVariable, is, FALSE);
        break;
    case BURN_MSI_COMPONENT_SEARCH_TYPE_DIRECTORY:
        if (INSTALLSTATE_ABSENT == is || INSTALLSTATE_LOCAL == is || INSTALLSTATE_SOURCE == is)
        {
            // remove file part from path, if any
            LPWSTR wz = wcsrchr(sczPath, L'\\');
            if (wz)
            {
                wz[1] = L'\0';
            }

            hr = VariableSetString(pVariables, pSearch->sczVariable, sczPath, FALSE, FALSE);
        }
        break;
    }
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    if (FAILED(hr))
    {
        LogStringLine(REPORT_STANDARD, "MsiComponentSearch failed: ID '%ls', HRESULT 0x%x", pSearch->sczKey, hr);
    }

    StrSecureZeroFreeString(sczComponentId);
    StrSecureZeroFreeString(sczProductCode);
    ReleaseStr(sczPath);
    return hr;
}

static HRESULT MsiProductSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczGuid = NULL;
    LPCWSTR wzProperty = NULL;
    LPWSTR *rgsczRelatedProductCodes = NULL;
    DWORD dwRelatedProducts = 0;
    BURN_VARIANT_TYPE type = BURN_VARIANT_TYPE_NONE;
    BURN_VARIANT value = { };

    switch (pSearch->MsiProductSearch.Type)
    {
    case BURN_MSI_PRODUCT_SEARCH_TYPE_VERSION:
        wzProperty = INSTALLPROPERTY_VERSIONSTRING;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_LANGUAGE:
        wzProperty = INSTALLPROPERTY_LANGUAGE;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_STATE:
        wzProperty = INSTALLPROPERTY_PRODUCTSTATE;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_ASSIGNMENT:
        wzProperty = INSTALLPROPERTY_ASSIGNMENTTYPE;
        break;
    default:
        ExitOnFailure(hr = E_NOTIMPL, "Unsupported product search type: %u", pSearch->MsiProductSearch.Type);
    }

    // format guid string
    hr = VariableFormatString(pVariables, pSearch->MsiProductSearch.sczGuid, &sczGuid, NULL);
    ExitOnFailure(hr, "Failed to format GUID string.");

    // get product info
    value.Type = BURN_VARIANT_TYPE_STRING;

    // if this is an upgrade code then get the product code of the highest versioned related product
    if (BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_UPGRADECODE == pSearch->MsiProductSearch.GuidType)
    {
        // WiuEnumRelatedProductCodes will log sczGuid on errors, what if there's a hidden variable in there?
        hr = WiuEnumRelatedProductCodes(sczGuid, &rgsczRelatedProductCodes, &dwRelatedProducts, TRUE);
        ExitOnFailure(hr, "Failed to enumerate related products for upgrade code.");

        // if we actually found a related product then use its upgrade code for the rest of the search
        if (1 == dwRelatedProducts)
        {
            hr = StrAllocStringSecure(&sczGuid, rgsczRelatedProductCodes[0], 0);
            ExitOnFailure(hr, "Failed to copy upgrade code.");
        }
        else
        {
            // set this here so we have a way of knowing that we don't need to bother
            // querying for the product information below
            hr = HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT);
        }
    }

    if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) != hr)
    {
        hr = WiuGetProductInfo(sczGuid, wzProperty, &value.sczValue);
        if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY) == hr)
        {
            // product state is available only through MsiGetProductInfoEx
            // What if there is a hidden variable in sczGuid?
            LogStringLine(REPORT_VERBOSE, "Trying per-machine extended info for property '%ls' for product: %ls", wzProperty, sczGuid);
            hr = WiuGetProductInfoEx(sczGuid, NULL, MSIINSTALLCONTEXT_MACHINE, wzProperty, &value.sczValue);

            // if not in per-machine context, try per-user (unmanaged)
            if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) == hr)
            {
                // What if there is a hidden variable in sczGuid?
                LogStringLine(REPORT_STANDARD, "Trying per-user extended info for property '%ls' for product: %ls", wzProperty, sczGuid);
                hr = WiuGetProductInfoEx(sczGuid, NULL, MSIINSTALLCONTEXT_USERUNMANAGED, wzProperty, &value.sczValue);
            }
        }
    }

    if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) == hr)
    {
        // What if there is a hidden variable in sczGuid?
        LogStringLine(REPORT_STANDARD, "Product or related product not found: %ls", sczGuid);

        // set value to indicate absent
        switch (pSearch->MsiProductSearch.Type)
        {
        case BURN_MSI_PRODUCT_SEARCH_TYPE_ASSIGNMENT: __fallthrough;
        case BURN_MSI_PRODUCT_SEARCH_TYPE_VERSION:
            value.Type = BURN_VARIANT_TYPE_NUMERIC;
            value.llValue = 0;
            break;
        case BURN_MSI_PRODUCT_SEARCH_TYPE_LANGUAGE:
            // is supposed to remain empty
            break;
        case BURN_MSI_PRODUCT_SEARCH_TYPE_STATE:
            value.Type = BURN_VARIANT_TYPE_NUMERIC;
            value.llValue = INSTALLSTATE_ABSENT;
            break;
        }

        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get product info.");

    // change value type
    switch (pSearch->MsiProductSearch.Type)
    {
    case BURN_MSI_PRODUCT_SEARCH_TYPE_VERSION:
        type = BURN_VARIANT_TYPE_VERSION;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_LANGUAGE:
        type = BURN_VARIANT_TYPE_STRING;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_STATE: __fallthrough;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_ASSIGNMENT:
        type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    }
    hr = BVariantChangeType(&value, type);
    ExitOnFailure(hr, "Failed to change value type.");

    // Set variable.
    hr = VariableSetVariant(pVariables, pSearch->sczVariable, &value);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    if (FAILED(hr))
    {
        LogStringLine(REPORT_STANDARD, "MsiProductSearch failed: ID '%ls', HRESULT 0x%x", pSearch->sczKey, hr);
    }

    StrSecureZeroFreeString(sczGuid);
    ReleaseStrArray(rgsczRelatedProductCodes, dwRelatedProducts);
    BVariantUninitialize(&value);

    return hr;
}

static HRESULT PerformExtensionSearch(
    __in BURN_SEARCH* pSearch
    )
{
    HRESULT hr = S_OK;

    hr = BurnExtensionPerformSearch(pSearch->ExtensionSearch.pExtension, pSearch->sczKey, pSearch->sczVariable);

    return hr;
}

static HRESULT PerformSetVariable(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BURN_VARIANT newValue = { };
    LPWSTR sczFormattedValue = NULL;
    SIZE_T cchOut = 0;

    if (BURN_VARIANT_TYPE_NONE == pSearch->SetVariable.targetType)
    {
        BVariantUninitialize(&newValue);
    }
    else
    {
        hr = VariableFormatString(pVariables, pSearch->SetVariable.sczValue, &sczFormattedValue, &cchOut);
        ExitOnFailure(hr, "Failed to format search value.");

        hr = BVariantSetString(&newValue, sczFormattedValue, 0, FALSE);
        ExitOnFailure(hr, "Failed to set variant value.");

        // change value variant to correct type
        hr = BVariantChangeType(&newValue, pSearch->SetVariable.targetType);
        ExitOnFailure(hr, "Failed to change variant type.");
    }

    hr = VariableSetVariant(pVariables, pSearch->sczVariable, &newValue);
    ExitOnFailure(hr, "Failed to set variable: %ls", pSearch->sczVariable);

LExit:
    BVariantUninitialize(&newValue);

    return hr;
}
