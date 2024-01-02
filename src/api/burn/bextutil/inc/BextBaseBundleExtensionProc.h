#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>

#include "BundleExtensionEngine.h"
#include "BundleExtension.h"
#include "IBundleExtensionEngine.h"
#include "IBundleExtension.h"

static HRESULT BextBaseBEProcSearch(
    __in IBundleExtension* pBE,
    __in BUNDLE_EXTENSION_SEARCH_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_SEARCH_RESULTS* /*pResults*/
    )
{
    return pBE->Search(pArgs->wzId, pArgs->wzVariable);
}

static HRESULT BextBaseBEProcContainerOpen(
    __in IBundleExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_OPEN_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_OPEN_RESULTS* pResults
    )
{
    return pBE->ContainerOpen(pArgs->wzContainerId, pArgs->wzFilePath, &pResults->pContext);
}

static HRESULT BextBaseBEProcContainerOpenAttached(
    __in IBundleExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_RESULTS* pResults
    )
{
    return pBE->ContainerOpenAttached(pArgs->wzContainerId, pArgs->hBundle, pArgs->qwContainerStartPos, pArgs->qwContainerSize, &pResults->pContext);
}

static HRESULT BextBaseBEProcContainerNextStream(
    __in IBundleExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_NEXT_STREAM_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_NEXT_STREAM_RESULTS* pResults
    )
{
    return pBE->ContainerNextStream(pArgs->pContext, pResults->psczStreamName);
}

static HRESULT BextBaseBEProcContainerStreamToFile(
    __in IBundleExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_STREAM_TO_FILE_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_STREAM_TO_FILE_RESULTS* /*pResults*/
    )
{
    return pBE->ContainerStreamToFile(pArgs->pContext, pArgs->wzFileName);
}

static HRESULT BextBaseBEProcContainerStreamToBuffer(
    __in IBundleExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_STREAM_TO_BUFFER_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_STREAM_TO_BUFFER_RESULTS* pResults
    )
{
    return pBE->ContainerStreamToBuffer(pArgs->pContext, pResults->ppbBuffer, pResults->pcbBuffer);
}

static HRESULT BextBaseBEProcContainerSkipStream(
    __in IBundleExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_SKIP_STREAM_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_SKIP_STREAM_RESULTS* /*pResults*/
    )
{
    return pBE->ContainerSkipStream(pArgs->pContext);
}

static HRESULT BextBaseBEProcContainerClose(
    __in IBundleExtension* pBE,
    __in BUNDLE_EXTENSION_CONTAINER_CLOSE_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CONTAINER_CLOSE_RESULTS* /*pResults*/
    )
{
    return pBE->ContainerClose(pArgs->pContext);
}

/*******************************************************************
BextBaseBundleExtensionProc - requires pvContext to be of type IBundleExtension.
                              Provides a default mapping between the message based
                              BundleExtension interface and the COM-based BundleExtension interface.

*******************************************************************/
static HRESULT WINAPI BextBaseBundleExtensionProc(
    __in BUNDLE_EXTENSION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    )
{
    IBundleExtension* pBE = reinterpret_cast<IBundleExtension*>(pvContext);
    HRESULT hr = pBE->BundleExtensionProc(message, pvArgs, pvResults, pvContext);

    if (E_NOTIMPL == hr)
    {
        switch (message)
        {
        case BUNDLE_EXTENSION_MESSAGE_SEARCH:
            hr = BextBaseBEProcSearch(pBE, reinterpret_cast<BUNDLE_EXTENSION_SEARCH_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_SEARCH_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_OPEN:
            hr = BextBaseBEProcContainerOpen(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_OPEN_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_OPEN_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_OPEN_ATTACHED:
            hr = BextBaseBEProcContainerOpenAttached(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_OPEN_ATTACHED_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_NEXT_STREAM:
            hr = BextBaseBEProcContainerNextStream(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_NEXT_STREAM_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_NEXT_STREAM_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_STREAM_TO_FILE:
            hr = BextBaseBEProcContainerStreamToFile(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_STREAM_TO_FILE_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_STREAM_TO_FILE_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_STREAM_TO_BUFFER:
            hr = BextBaseBEProcContainerStreamToBuffer(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_STREAM_TO_BUFFER_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_STREAM_TO_BUFFER_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_SKIP_STREAM:
            hr = BextBaseBEProcContainerSkipStream(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_SKIP_STREAM_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_SKIP_STREAM_RESULTS*>(pvResults));
            break;
        case BUNDLE_EXTENSION_MESSAGE_CONTAINER_CLOSE:
            hr = BextBaseBEProcContainerClose(pBE, reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_CLOSE_ARGS*>(pvArgs), reinterpret_cast<BUNDLE_EXTENSION_CONTAINER_CLOSE_RESULTS*>(pvResults));
            break;
        }
    }

    return hr;
}
