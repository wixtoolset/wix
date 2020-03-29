// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// function definitions

/*******************************************************************
 BurnExtensionParseFromXml - 

*******************************************************************/
EXTERN_C HRESULT BurnExtensionParseFromXml(
    __in BURN_EXTENSIONS* pBurnExtensions,
    __in BURN_PAYLOADS* pBaPayloads,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;

    // Select BundleExtension nodes.
    hr = XmlSelectNodes(pixnBundle, L"BundleExtension", &pixnNodes);
    ExitOnFailure(hr, "Failed to select BundleExtension nodes.");

    // Get BundleExtension node count.
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get BundleExtension node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // Allocate memory for BundleExtensions.
    pBurnExtensions->rgExtensions = (BURN_EXTENSION*)MemAlloc(sizeof(BURN_EXTENSION) * cNodes, TRUE);
    ExitOnNull(pBurnExtensions->rgExtensions, hr, E_OUTOFMEMORY, "Failed to allocate memory for BundleExtension structs.");

    pBurnExtensions->cExtensions = cNodes;

    // parse search elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_EXTENSION* pExtension = &pBurnExtensions->rgExtensions[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pExtension->sczId);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @EntryPayloadId
        hr = XmlGetAttributeEx(pixnNode, L"EntryPayloadId", &pExtension->sczEntryPayloadId);
        ExitOnFailure(hr, "Failed to get @EntryPayloadId.");

        hr = PayloadFindById(pBaPayloads, pExtension->sczEntryPayloadId, &pExtension->pEntryPayload);
        ExitOnFailure(hr, "Failed to find BundleExtension EntryPayload '%ls'.", pExtension->sczEntryPayloadId);

        // prepare next iteration
        ReleaseNullObject(pixnNode);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNode);
    ReleaseObject(pixnNodes);

    return hr;
}

/*******************************************************************
 BurnExtensionUninitialize - 

*******************************************************************/
EXTERN_C void BurnExtensionUninitialize(
    __in BURN_EXTENSIONS* pBurnExtensions
    )
{
    if (pBurnExtensions->rgExtensions)
    {
        for (DWORD i = 0; i < pBurnExtensions->cExtensions; ++i)
        {
            BURN_EXTENSION* pExtension = &pBurnExtensions->rgExtensions[i];

            ReleaseStr(pExtension->sczEntryPayloadId);
            ReleaseStr(pExtension->sczId);
        }
        MemFree(pBurnExtensions->rgExtensions);
    }

    // clear struct
    memset(pBurnExtensions, 0, sizeof(BURN_EXTENSIONS));
}

/*******************************************************************
 BurnExtensionLoad - 

*******************************************************************/
EXTERN_C HRESULT BurnExtensionLoad(
    __in BURN_EXTENSIONS * pBurnExtensions,
    __in BURN_EXTENSION_ENGINE_CONTEXT* pEngineContext
    )
{
    HRESULT hr = S_OK;
    BUNDLE_EXTENSION_CREATE_ARGS args = { };
    BUNDLE_EXTENSION_CREATE_RESULTS results = { };

    if (!pBurnExtensions->rgExtensions || !pBurnExtensions->cExtensions)
    {
        ExitFunction();
    }

    for (DWORD i = 0; i < pBurnExtensions->cExtensions; ++i)
    {
        BURN_EXTENSION* pExtension = &pBurnExtensions->rgExtensions[i];

        memset(&args, 0, sizeof(BUNDLE_EXTENSION_CREATE_ARGS));
        memset(&results, 0, sizeof(BUNDLE_EXTENSION_CREATE_RESULTS));

        args.cbSize = sizeof(BUNDLE_EXTENSION_CREATE_ARGS);
        args.pfnBundleExtensionEngineProc = EngineForExtensionProc;
        args.pvBundleExtensionEngineProcContext = pEngineContext;
        args.qwEngineAPIVersion = MAKEQWORDVERSION(0, 0, 0, 1); // TODO: need to decide whether to keep this, and if so when to update it.

        results.cbSize = sizeof(BUNDLE_EXTENSION_CREATE_RESULTS);

        // Load BundleExtension DLL.
        pExtension->hBextModule = ::LoadLibraryExW(pExtension->pEntryPayload->sczLocalFilePath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
        ExitOnNullWithLastError(pExtension->hBextModule, hr, "Failed to load BundleExtension DLL '%ls': '%ls'.", pExtension->sczId, pExtension->pEntryPayload->sczLocalFilePath);

        // Get BundleExtensionCreate entry-point.
        PFN_BUNDLE_EXTENSION_CREATE pfnCreate = (PFN_BUNDLE_EXTENSION_CREATE)::GetProcAddress(pExtension->hBextModule, "BundleExtensionCreate");
        ExitOnNullWithLastError(pfnCreate, hr, "Failed to get BundleExtensionCreate entry-point '%ls'.", pExtension->sczId);

        // Create BundleExtension.
        hr = pfnCreate(&args, &results);
        ExitOnFailure(hr, "Failed to create BundleExtension '%ls'.", pExtension->sczId);

        pExtension->pfnBurnExtensionProc = results.pfnBundleExtensionProc;
        pExtension->pvBurnExtensionProcContext = results.pvBundleExtensionProcContext;
    }

LExit:
    return hr;
}

/*******************************************************************
 BurnExtensionUnload - 

*******************************************************************/
EXTERN_C void BurnExtensionUnload(
    __in BURN_EXTENSIONS * pBurnExtensions
    )
{
    HRESULT hr = S_OK;

    if (pBurnExtensions->rgExtensions)
    {
        for (DWORD i = 0; i < pBurnExtensions->cExtensions; ++i)
        {
            BURN_EXTENSION* pExtension = &pBurnExtensions->rgExtensions[i];

            if (pExtension->hBextModule)
            {
                // Get BundleExtensionDestroy entry-point and call it if it exists.
                PFN_BUNDLE_EXTENSION_DESTROY pfnDestroy = (PFN_BUNDLE_EXTENSION_DESTROY)::GetProcAddress(pExtension->hBextModule, "BundleExtensionDestroy");
                if (pfnDestroy)
                {
                    pfnDestroy();
                }

                // Free BundleExtension DLL.
                if (!::FreeLibrary(pExtension->hBextModule))
                {
                    hr = HRESULT_FROM_WIN32(::GetLastError());
                    TraceError(hr, "Failed to unload BundleExtension DLL.");
                }
                pExtension->hBextModule = NULL;
            }
        }
    }
}

EXTERN_C HRESULT BurnExtensionFindById(
    __in BURN_EXTENSIONS* pBurnExtensions,
    __in_z LPCWSTR wzId,
    __out BURN_EXTENSION** ppExtension
    )
{
    HRESULT hr = S_OK;
    BURN_EXTENSION* pExtension = NULL;

    for (DWORD i = 0; i < pBurnExtensions->cExtensions; ++i)
    {
        pExtension = &pBurnExtensions->rgExtensions[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pExtension->sczId, -1, wzId, -1))
        {
            *ppExtension = pExtension;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

EXTERN_C BEEAPI BurnExtensionPerformSearch(
    __in BURN_EXTENSION* pExtension,
    __in LPWSTR wzSearchId,
    __in LPWSTR wzVariable
    )
{
    HRESULT hr = S_OK;
    BUNDLE_EXTENSION_SEARCH_ARGS args = { };
    BUNDLE_EXTENSION_SEARCH_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzId = wzSearchId;
    args.wzVariable = wzVariable;

    results.cbSize = sizeof(results);

    hr = pExtension->pfnBurnExtensionProc(BUNDLE_EXTENSION_MESSAGE_SEARCH, &args, &results, pExtension->pvBurnExtensionProcContext);
    ExitOnFailure(hr, "BundleExtension '%ls' Search '%ls' failed.", pExtension->sczId, wzSearchId);

LExit:
    return hr;
}
