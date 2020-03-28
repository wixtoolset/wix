// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// function definitions

extern "C" HRESULT WINAPI BundleExtensionCreate(
    __in const BUNDLE_EXTENSION_CREATE_ARGS* /*pArgs*/,
    __inout BUNDLE_EXTENSION_CREATE_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK; 

    return hr;
}

extern "C" void WINAPI BundleExtensionDestroy()
{
}