#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>

#include <IBootstrapperExtensionEngine.h>
#include <IBootstrapperExtension.h>

static HRESULT BextBaseBEProcSearch(
    __in IBootstrapperExtension* pBE,
    __in BOOTSTRAPPER_EXTENSION_SEARCH_ARGS* pArgs,
    __inout BOOTSTRAPPER_EXTENSION_SEARCH_RESULTS* /*pResults*/
    )
{
    return pBE->Search(pArgs->wzId, pArgs->wzVariable);
}

static HRESULT BextBaseBEProcContainerOpen(
    __in IBootstrapperExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_OPEN_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_OPEN_RESULTS* pResults
    )
{
    return pBE->ContainerOpen(pArgs->wzContainerId, pArgs->wzFilePath, &pResults->pContext);
}

static HRESULT BextBaseBEProcContainerOpenAttached(
    __in IBootstrapperExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_RESULTS* pResults
    )
{
    return pBE->ContainerOpenAttached(pArgs->wzContainerId, pArgs->hBundle, pArgs->qwContainerStartPos, pArgs->qwContainerSize, &pResults->pContext);
}

static HRESULT BextBaseBEProcContainerExtractFiles(
    __in IBootstrapperExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_EXTRACT_FILES_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_EXTRACT_FILES_RESULTS* /*pResults*/
    )
{
    return pBE->ContainerExtractFiles(pArgs->pContext, pArgs->cFiles, pArgs->psczEmbeddedIds, pArgs->psczTargetPaths);
}

static HRESULT BextBaseBEProcContainerClose(
    __in IBootstrapperExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_CLOSE_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_CLOSE_RESULTS* /*pResults*/
    )
{
    return pBE->ContainerClose(pArgs->pContext);
}

/*******************************************************************
BextBaseBootstrapperExtensionProc - requires pvContext to be of type IBootstrapperExtension.
                              Provides a default mapping between the message based
                              BootstrapperExtension interface and the COM-based BootstrapperExtension interface.

*******************************************************************/
static HRESULT WINAPI BextBaseBootstrapperExtensionProc(
    __in BOOTSTRAPPER_EXTENSION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    )
{
    IBootstrapperExtension* pBE = reinterpret_cast<IBootstrapperExtension*>(pvContext);
    HRESULT hr = pBE->BootstrapperExtensionProc(message, pvArgs, pvResults, pvContext);

    if (E_NOTIMPL == hr)
    {
        switch (message)
        {
        case BOOTSTRAPPER_EXTENSION_MESSAGE_SEARCH:
            hr = BextBaseBEProcSearch(pBE, reinterpret_cast<BOOTSTRAPPER_EXTENSION_SEARCH_ARGS*>(pvArgs), reinterpret_cast<BOOTSTRAPPER_EXTENSION_SEARCH_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_OPEN:
            hr = BextBaseBEProcContainerOpen(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_OPEN_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_OPEN_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_OPEN_ATTACHED:
            hr = BextBaseBEProcContainerOpenAttached(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_EXTRACT_FILES:
            hr = BextBaseBEProcContainerExtractFiles(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_EXTRACT_FILES_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_EXTRACT_FILES_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_CLOSE:
            hr = BextBaseBEProcContainerClose(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_CLOSE_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_CLOSE_RESULTS*>(pvResults));
            break;
        }
    }

    return hr;
}
