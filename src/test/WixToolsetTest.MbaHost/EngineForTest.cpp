// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT BAEngineLog(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pContext,
    __in BAENGINE_LOG_ARGS* pArgs,
    __in BAENGINE_LOG_RESULTS* /*pResults*/
)
{
    HRESULT hr = S_OK;

    pContext->pfnLog(pArgs->wzMessage);

    return hr;
}

HRESULT WINAPI EngineForTestProc(
    __in BOOTSTRAPPER_ENGINE_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
)
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ENGINE_CONTEXT* pContext = reinterpret_cast<BOOTSTRAPPER_ENGINE_CONTEXT*>(pvContext);

    if (!pContext || !pvArgs || !pvResults)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    switch (message)
    {
    case BOOTSTRAPPER_ENGINE_MESSAGE_LOG:
        hr = BAEngineLog(pContext, reinterpret_cast<BAENGINE_LOG_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_LOG_RESULTS*>(pvResults));
        break;
    default:
        hr = E_NOTIMPL;
        break;
    }

LExit:
    return hr;
}