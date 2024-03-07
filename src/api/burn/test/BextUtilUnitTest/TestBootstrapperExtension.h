#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

HRESULT TestBootstrapperExtensionCreate(
    __in IBootstrapperExtensionEngine* pEngine,
    __in const BOOTSTRAPPER_EXTENSION_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_EXTENSION_CREATE_RESULTS* pResults,
    __out IBootstrapperExtension** ppBootstrapperExtension
    );
