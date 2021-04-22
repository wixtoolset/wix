// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBootstrapperApplication.h"
#include "BalBaseBootstrapperApplicationProc.h"

class CTestBootstrapperApplication : public CBalBaseBootstrapperApplication
{
public:
    CTestBootstrapperApplication(
        __in IBootstrapperEngine* pEngine,
        __in const BOOTSTRAPPER_CREATE_ARGS* pArgs
        ) : CBalBaseBootstrapperApplication(pEngine, pArgs)
    {
    }
};

HRESULT CreateBootstrapperApplication(
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults,
    __out IBootstrapperApplication** ppApplication
    )
{
    HRESULT hr = S_OK;
    CTestBootstrapperApplication* pApplication = NULL;

    pApplication = new CTestBootstrapperApplication(pEngine, pArgs);
    ExitOnNull(pApplication, hr, E_OUTOFMEMORY, "Failed to create new test bootstrapper application object.");

    pResults->pfnBootstrapperApplicationProc = BalBaseBootstrapperApplicationProc;
    pResults->pvBootstrapperApplicationProcContext = pApplication;
    *ppApplication = pApplication;
    pApplication = NULL;

LExit:
    ReleaseObject(pApplication);
    return hr;
}
