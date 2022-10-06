#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


// constants

#define NETFX_BUNDLE_EXTENSION_ID BUNDLE_EXTENSION_DECORATION(L"NetfxBundleExtension")


// function declarations

HRESULT NetfxBundleExtensionCreate(
    __in HINSTANCE hInstance,
    __in IBundleExtensionEngine* pEngine,
    __in const BUNDLE_EXTENSION_CREATE_ARGS* pArgs,
    __out IBundleExtension** ppBundleExtension
    );
