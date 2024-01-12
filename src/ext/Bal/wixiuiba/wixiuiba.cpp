// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// function definitions

EXTERN_C int WINAPI wWinMain(
    __in HINSTANCE hInstance,
    __in_opt HINSTANCE /* hPrevInstance */,
    __in_z_opt LPWSTR /*lpCmdLine*/,
    __in int /*nCmdShow*/
    )
{
    HRESULT hr = S_OK;
    IBootstrapperApplication* pApplication = NULL;

    hr = CreateWixInternalUIBootstrapperApplication(hInstance, &pApplication);
    ExitOnFailure(hr, "Failed to create WiX internal UI bootstrapper application.");

    hr = BootstrapperApplicationRun(pApplication);
    ExitOnFailure(hr, "Failed to run WiX internal UI bootstrapper application.");

LExit:
    ReleaseObject(pApplication);

    return 0;
}
