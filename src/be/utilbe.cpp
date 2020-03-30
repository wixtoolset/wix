// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BextBaseBundleExtensionProc.h"

static IBundleExtension* vpBundleExtension = NULL;

// function definitions

extern "C" HRESULT WINAPI BundleExtensionCreate(
    __in const BUNDLE_EXTENSION_CREATE_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    IBundleExtensionEngine* pEngine = NULL;

    hr = XmlInitialize();
    ExitOnFailure(hr, "Failed to initialize XML.");

    hr = BextInitializeFromCreateArgs(pArgs, &pEngine);
    ExitOnFailure(hr, "Failed to initialize bext");

    hr = UtilBundleExtensionCreate(pEngine, pArgs, &vpBundleExtension);
    BextExitOnFailure(hr, "Failed to create WixUtilBundleExtension");

    pResults->pfnBundleExtensionProc = BextBaseBundleExtensionProc;
    pResults->pvBundleExtensionProcContext = vpBundleExtension;

LExit:
    ReleaseObject(pEngine);

    return hr;
}

extern "C" void WINAPI BundleExtensionDestroy()
{
    BextUninitialize();
    ReleaseNullObject(vpBundleExtension);
    XmlUninitialize();
}