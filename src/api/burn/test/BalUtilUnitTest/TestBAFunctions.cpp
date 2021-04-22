// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBAFunctions.h"
#include "BalBaseBAFunctionsProc.h"

class CTestBAFunctions : public CBalBaseBAFunctions
{
public:
    CTestBAFunctions(
        __in HMODULE hModule,
        __in IBootstrapperEngine* pEngine,
        __in const BA_FUNCTIONS_CREATE_ARGS* pArgs
        ) : CBalBaseBAFunctions(hModule, pEngine, pArgs)
    {
    }
};

HRESULT CreateBAFunctions(
    __in HMODULE hModule,
    __in IBootstrapperEngine* pEngine,
    __in const BA_FUNCTIONS_CREATE_ARGS* pArgs,
    __in BA_FUNCTIONS_CREATE_RESULTS* pResults,
    __out IBAFunctions** ppApplication
    )
{
    HRESULT hr = S_OK;
    CTestBAFunctions* pApplication = NULL;

    pApplication = new CTestBAFunctions(hModule, pEngine, pArgs);
    ExitOnNull(pApplication, hr, E_OUTOFMEMORY, "Failed to create new test bafunctions object.");

    pResults->pfnBAFunctionsProc = BalBaseBAFunctionsProc;
    pResults->pvBAFunctionsProcContext = pApplication;
    *ppApplication = pApplication;
    pApplication = NULL;

LExit:
    ReleaseObject(pApplication);
    return hr;
}
