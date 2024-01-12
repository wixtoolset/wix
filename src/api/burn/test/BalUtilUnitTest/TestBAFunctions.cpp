// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBAFunctions.h"
#include "BalBaseBAFunctionsProc.h"

class CTestBAFunctions : public CBalBaseBAFunctions
{
public:
    CTestBAFunctions(
        __in HMODULE hModule
        ) : CBalBaseBAFunctions(hModule)
    {
    }
};

HRESULT CreateBAFunctions(
    __in HMODULE hModule,
    __in const BA_FUNCTIONS_CREATE_ARGS* pArgs,
    __inout BA_FUNCTIONS_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    CTestBAFunctions* pFunction = NULL;

    pFunction = new CTestBAFunctions(hModule);
    ExitOnNull(pFunction, hr, E_OUTOFMEMORY, "Failed to create new test bafunctions object.");

    hr = pFunction->OnCreate(pArgs->pEngine, pArgs->pCommand);
    ExitOnFailure(hr, "Failed to initialize new test bafunctions.");

    pResults->pfnBAFunctionsProc = BalBaseBAFunctionsProc;
    pResults->pvBAFunctionsProcContext = pFunction;
    pFunction = NULL;

LExit:
    ReleaseObject(pFunction);
    return hr;
}
