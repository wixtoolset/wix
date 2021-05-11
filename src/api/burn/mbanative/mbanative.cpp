// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBootstrapperApplicationProc.h"

extern "C" HRESULT WINAPI InitializeFromCreateArgs(
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_COMMAND* pCommand,
    __out IBootstrapperEngine** ppEngine
    )
{
    HRESULT hr = S_OK;

    hr = BalInitializeFromCreateArgs(pArgs, ppEngine);
    ExitOnFailure(hr, "Failed to initialize Bal.");

    memcpy_s(pCommand, pCommand->cbSize, pArgs->pCommand, min(pArgs->pCommand->cbSize, pCommand->cbSize));
LExit:
    return hr;
}

extern "C" void WINAPI StoreBAInCreateResults(
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults,
    __in IBootstrapperApplication* pBA
    )
{
    pResults->pfnBootstrapperApplicationProc = BalBaseBootstrapperApplicationProc;
    pResults->pvBootstrapperApplicationProcContext = pBA;
}
