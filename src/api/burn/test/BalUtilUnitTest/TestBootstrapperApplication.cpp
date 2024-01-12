// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBootstrapperApplication.h"

class CTestBootstrapperApplication : public CBalBaseBootstrapperApplication
{
public:
    CTestBootstrapperApplication() : CBalBaseBootstrapperApplication()
    {
    }
};

HRESULT CreateBootstrapperApplication(
    __out IBootstrapperApplication** ppApplication
    )
{
    HRESULT hr = S_OK;
    CTestBootstrapperApplication* pApplication = NULL;

    pApplication = new CTestBootstrapperApplication();
    ExitOnNull(pApplication, hr, E_OUTOFMEMORY, "Failed to create new test bootstrapper application object.");

    *ppApplication = pApplication;
    pApplication = NULL;

LExit:
    ReleaseObject(pApplication);
    return hr;
}
