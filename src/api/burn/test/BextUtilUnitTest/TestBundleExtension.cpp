// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BextBaseBundleExtension.h"
#include "BextBaseBundleExtensionProc.h"

class CTestBundleExtension : public CBextBaseBundleExtension
{
public:
    CTestBundleExtension(
        __in IBundleExtensionEngine* pEngine
        ) : CBextBaseBundleExtension(pEngine)
    {
    }
};

HRESULT TestBundleExtensionCreate(
    __in IBundleExtensionEngine* pEngine,
    __in const BUNDLE_EXTENSION_CREATE_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CREATE_RESULTS* pResults,
    __out IBundleExtension** ppBundleExtension
    )
{
    HRESULT hr = S_OK;
    CTestBundleExtension* pExtension = NULL;

    pExtension = new CTestBundleExtension(pEngine);
    ExitOnNull(pExtension, hr, E_OUTOFMEMORY, "Failed to create new CTestBundleExtension.");

    hr = pExtension->Initialize(pArgs);
    ExitOnFailure(hr, "CTestBundleExtension initialization failed");

    pResults->pfnBundleExtensionProc = BextBaseBundleExtensionProc;
    pResults->pvBundleExtensionProcContext = pExtension;

    *ppBundleExtension = pExtension;
    pExtension = NULL;

LExit:
    ReleaseObject(pExtension);
    return hr;
}
