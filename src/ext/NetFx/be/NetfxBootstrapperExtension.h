#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


// constants

#define NETFX_BOOTSTRAPPER_EXTENSION_ID BOOTSTRAPPER_EXTENSION_DECORATION(L"NetfxBootstrapperExtension")


// function declarations

HRESULT NetfxBootstrapperExtensionCreate(
    __in HINSTANCE hInstance,
    __in IBootstrapperExtensionEngine* pEngine,
    __in const BOOTSTRAPPER_EXTENSION_CREATE_ARGS* pArgs,
    __out IBootstrapperExtension** ppBootstrapperExtension
    );
