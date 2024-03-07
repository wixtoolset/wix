#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include <IBootstrapperExtensionEngine.h>

#ifdef __cplusplus
extern "C" {
#endif

// function declarations

HRESULT BextBootstrapperExtensionEngineCreate(
    __in PFN_BOOTSTRAPPER_EXTENSION_ENGINE_PROC pfnBootstrapperExtensionEngineProc,
    __in_opt LPVOID pvBootstrapperExtensionEngineProcContext,
    __out IBootstrapperExtensionEngine** ppEngineForExtension
    );

#ifdef __cplusplus
}
#endif
