// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static HRESULT SendRequiredBextMessage(
    __in BURN_EXTENSION* pExtension,
    __in BOOTSTRAPPER_EXTENSION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    );

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
    LPWSTR scz = NULL;

    // Select BootstrapperExtension nodes.
    hr = XmlSelectNodes(pixnBundle, L"BootstrapperExtension", &pixnNodes);
    ExitOnFailure(hr, "Failed to select BootstrapperExtension nodes.");

    // Get BootstrapperExtension node count.
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get BootstrapperExtension node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // Allocate memory for BootstrapperExtensions.
    pBurnExtensions->rgExtensions = (BURN_EXTENSION*)MemAlloc(sizeof(BURN_EXTENSION) * cNodes, TRUE);
    ExitOnNull(pBurnExtensions->rgExtensions, hr, E_OUTOFMEMORY, "Failed to allocate memory for BootstrapperExtension structs.");

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
        hr = XmlGetAttributeEx(pixnNode, L"EntryPayloadSourcePath", &scz);
        ExitOnFailure(hr, "Failed to get @EntryPayloadSourcePath.");

        hr = PayloadFindEmbeddedBySourcePath(pBaPayloads->sdhPayloads, scz, &pExtension->pEntryPayload);
        ExitOnFailure(hr, "Failed to find BootstrapperExtension EntryPayload '%ls'.", pExtension->sczId);

        // prepare next iteration
        ReleaseNullObject(pixnNode);
    }

    hr = S_OK;

LExit:
    ReleaseStr(scz);
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
    LPWSTR sczBootstrapperExtensionDataPath = NULL;
    BOOTSTRAPPER_EXTENSION_CREATE_ARGS args = { };
    BOOTSTRAPPER_EXTENSION_CREATE_RESULTS results = { };

    if (!pBurnExtensions->rgExtensions || !pBurnExtensions->cExtensions)
    {
        ExitFunction();
    }

    hr = PathConcat(pEngineContext->pEngineState->userExperience.sczTempDirectory, L"BootstrapperExtensionData.xml", &sczBootstrapperExtensionDataPath);
    ExitOnFailure(hr, "Failed to get BootstrapperExtensionDataPath.");

    for (DWORD i = 0; i < pBurnExtensions->cExtensions; ++i)
    {
        BURN_EXTENSION* pExtension = &pBurnExtensions->rgExtensions[i];

        memset(&args, 0, sizeof(BOOTSTRAPPER_EXTENSION_CREATE_ARGS));
        memset(&results, 0, sizeof(BOOTSTRAPPER_EXTENSION_CREATE_RESULTS));

        args.cbSize = sizeof(BOOTSTRAPPER_EXTENSION_CREATE_ARGS);
        args.pfnBootstrapperExtensionEngineProc = EngineForExtensionProc;
        args.pvBootstrapperExtensionEngineProcContext = pEngineContext;
        args.qwEngineAPIVersion = MAKEQWORDVERSION(2021, 4, 27, 0);
        args.wzBootstrapperWorkingFolder = pEngineContext->pEngineState->userExperience.sczTempDirectory;
        args.wzBootstrapperExtensionDataPath = sczBootstrapperExtensionDataPath;
        args.wzExtensionId = pExtension->sczId;

        results.cbSize = sizeof(BOOTSTRAPPER_EXTENSION_CREATE_RESULTS);

        // Load BootstrapperExtension DLL.
        pExtension->hBextModule = ::LoadLibraryExW(pExtension->pEntryPayload->sczLocalFilePath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
        ExitOnNullWithLastError(pExtension->hBextModule, hr, "Failed to load BootstrapperExtension DLL '%ls': '%ls'.", pExtension->sczId, pExtension->pEntryPayload->sczLocalFilePath);

        // Get BootstrapperExtensionCreate entry-point.
        PFN_BOOTSTRAPPER_EXTENSION_CREATE pfnCreate = (PFN_BOOTSTRAPPER_EXTENSION_CREATE)::GetProcAddress(pExtension->hBextModule, "BootstrapperExtensionCreate");
        ExitOnNullWithLastError(pfnCreate, hr, "Failed to get BootstrapperExtensionCreate entry-point '%ls'.", pExtension->sczId);

        // Create BootstrapperExtension.
        hr = pfnCreate(&args, &results);
        ExitOnFailure(hr, "Failed to create BootstrapperExtension '%ls'.", pExtension->sczId);

        pExtension->pfnBurnExtensionProc = results.pfnBootstrapperExtensionProc;
        pExtension->pvBurnExtensionProcContext = results.pvBootstrapperExtensionProcContext;
    }

LExit:
    ReleaseStr(sczBootstrapperExtensionDataPath);

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
                // Get BootstrapperExtensionDestroy entry-point and call it if it exists.
                PFN_BOOTSTRAPPER_EXTENSION_DESTROY pfnDestroy = (PFN_BOOTSTRAPPER_EXTENSION_DESTROY)::GetProcAddress(pExtension->hBextModule, "BootstrapperExtensionDestroy");
                if (pfnDestroy)
                {
                    pfnDestroy();
                }

                // Free BootstrapperExtension DLL.
                if (!::FreeLibrary(pExtension->hBextModule))
                {
                    hr = HRESULT_FROM_WIN32(::GetLastError());
                    TraceError(hr, "Failed to unload BootstrapperExtension DLL.");
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
    BOOTSTRAPPER_EXTENSION_SEARCH_ARGS args = { };
    BOOTSTRAPPER_EXTENSION_SEARCH_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzId = wzSearchId;
    args.wzVariable = wzVariable;

    results.cbSize = sizeof(results);

    hr = SendRequiredBextMessage(pExtension, BOOTSTRAPPER_EXTENSION_MESSAGE_SEARCH, &args, &results);
    ExitOnFailure(hr, "BootstrapperExtension '%ls' Search '%ls' failed.", pExtension->sczId, wzSearchId);

LExit:
    return hr;
}

EXTERN_C BEEAPI BurnExtensionContainerOpen(
    __in BURN_EXTENSION* pExtension,
    __in LPCWSTR wzContainerId,
    __in LPCWSTR wzFilePath,
    __in BURN_CONTAINER_CONTEXT* pContext
)
{
    HRESULT hr = S_OK;
    BUNDLE_EXTENSION_CONTAINER_OPEN_ARGS args = { };
    BUNDLE_EXTENSION_CONTAINER_OPEN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzContainerId = wzContainerId;
    args.wzFilePath = wzFilePath;

    results.cbSize = sizeof(results);

    hr = SendRequiredBextMessage(pExtension, BUNDLE_EXTENSION_MESSAGE_CONTAINER_OPEN, &args, &results);
    pContext->Bex.pExtensionContext = results.pContext;
    ExitOnFailure(hr, "BundleExtension '%ls' open container '%ls' failed.", pExtension->sczId, wzFilePath);

LExit:
    return hr;
}

EXTERN_C BEEAPI BurnExtensionContainerOpenAttached(
    __in BURN_EXTENSION* pExtension,
    __in LPCWSTR wzContainerId,
    __in HANDLE hBundle,
    __in DWORD64 qwContainerStartPos,
    __in DWORD64 qwContainerSize,
    __in BURN_CONTAINER_CONTEXT* pContext
)
{
    HRESULT hr = S_OK;
    BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_ARGS args = { };
    BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzContainerId = wzContainerId;
    args.hBundle = hBundle;
    args.qwContainerStartPos = qwContainerStartPos;
    args.qwContainerSize = qwContainerSize;

    results.cbSize = sizeof(results);

    hr = SendRequiredBextMessage(pExtension, BUNDLE_EXTENSION_MESSAGE_CONTAINER_OPEN_ATTACHED, &args, &results);
    pContext->Bex.pExtensionContext = results.pContext;
    ExitOnFailure(hr, "BundleExtension '%ls' open attached container failed.", pExtension->sczId);

LExit:
    return hr;
}

BEEAPI BurnExtensionContainerExtractFiles(
    __in BURN_EXTENSION* pExtension,
    __in DWORD cFiles,
    __in LPCWSTR *psczEmbeddedIds,
    __in LPCWSTR *psczTargetPaths,
    __in BURN_CONTAINER_CONTEXT* pContext
)
{
    HRESULT hr = S_OK;
    BUNDLE_EXTENSION_CONTAINER_EXTRACT_FILES_ARGS args = { };
    BUNDLE_EXTENSION_CONTAINER_EXTRACT_FILES_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.pContext = pContext->Bex.pExtensionContext;
    args.cFiles = cFiles;
    args.psczEmbeddedIds = psczEmbeddedIds;
    args.psczTargetPaths = psczTargetPaths;

    results.cbSize = sizeof(results);

    hr = SendRequiredBextMessage(pExtension, BUNDLE_EXTENSION_MESSAGE_CONTAINER_EXTRACT_FILES, &args, &results);
    ExitOnFailure(hr, "BundleExtension '%ls' failed to extract files.", pExtension->sczId);

LExit:
    return hr;
}

BEEAPI BurnExtensionContainerClose(
    __in BURN_EXTENSION* pExtension,
    __in BURN_CONTAINER_CONTEXT* pContext
)
{
    HRESULT hr = S_OK;
    BUNDLE_EXTENSION_CONTAINER_CLOSE_ARGS args = { };
    BUNDLE_EXTENSION_CONTAINER_CLOSE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.pContext = pContext->Bex.pExtensionContext;

    results.cbSize = sizeof(results);

    hr = SendRequiredBextMessage(pExtension, BUNDLE_EXTENSION_MESSAGE_CONTAINER_CLOSE, &args, &results);
    ExitOnFailure(hr, "BundleExtension '%ls' failed to close container.", pExtension->sczId);

LExit:
    return hr;
}

static HRESULT SendRequiredBextMessage(
    __in BURN_EXTENSION* pExtension,
    __in BOOTSTRAPPER_EXTENSION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults
    )
{
    HRESULT hr = S_OK;

    hr = pExtension->pfnBurnExtensionProc(message, pvArgs, pvResults, pExtension->pvBurnExtensionProcContext);

    return hr;
}
