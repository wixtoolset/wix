// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BextBaseBootstrapperExtensionProc.h"

static IBootstrapperExtension* vpBootstrapperExtension = NULL;

// function definitions

extern "C" HRESULT WINAPI BootstrapperExtensionCreate(
    __in const BOOTSTRAPPER_EXTENSION_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_EXTENSION_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    IBootstrapperExtensionEngine* pEngine = NULL;

    hr = XmlInitialize();
    ExitOnFailure(hr, "Failed to initialize XML.");

    hr = BextInitializeFromCreateArgs(pArgs, &pEngine);
    ExitOnFailure(hr, "Failed to initialize bext");

    hr = UtilBootstrapperExtensionCreate(pEngine, pArgs, &vpBootstrapperExtension);
    BextExitOnFailure(hr, "Failed to create WixUtilBootstrapperExtension");

    pResults->pfnBootstrapperExtensionProc = BextBaseBootstrapperExtensionProc;
    pResults->pvBootstrapperExtensionProcContext = vpBootstrapperExtension;

LExit:
    ReleaseObject(pEngine);

    return hr;
}

extern "C" void WINAPI BootstrapperExtensionDestroy()
{
    BextUninitialize();
    ReleaseNullObject(vpBootstrapperExtension);
    XmlUninitialize();
}
