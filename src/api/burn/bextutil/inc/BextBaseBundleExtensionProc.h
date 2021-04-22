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
        }
    }

    return hr;
}
