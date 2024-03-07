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
        }
    }

    return hr;
}
