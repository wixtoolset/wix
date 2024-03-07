// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BextBaseBootstrapperExtension.h"
#include "BextBaseBootstrapperExtensionProc.h"

class CTestBootstrapperExtension : public CBextBaseBootstrapperExtension
{
public:
    CTestBootstrapperExtension(
        __in IBootstrapperExtensionEngine* pEngine
        ) : CBextBaseBootstrapperExtension(pEngine)
    {
    }
};

HRESULT TestBootstrapperExtensionCreate(
    __in IBootstrapperExtensionEngine* pEngine,
    __in const BOOTSTRAPPER_EXTENSION_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_EXTENSION_CREATE_RESULTS* pResults,
    __out IBootstrapperExtension** ppBootstrapperExtension
    )
{
    HRESULT hr = S_OK;
    CTestBootstrapperExtension* pExtension = NULL;

    pExtension = new CTestBootstrapperExtension(pEngine);
    ExitOnNull(pExtension, hr, E_OUTOFMEMORY, "Failed to create new CTestBootstrapperExtension.");

    hr = pExtension->Initialize(pArgs);
    ExitOnFailure(hr, "CTestBootstrapperExtension initialization failed");

    pResults->pfnBootstrapperExtensionProc = BextBaseBootstrapperExtensionProc;
    pResults->pvBootstrapperExtensionProcContext = pExtension;

    *ppBootstrapperExtension = pExtension;
    pExtension = NULL;

LExit:
    ReleaseObject(pExtension);
    return hr;
}
