// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#ifdef __cplusplus
extern "C" {
#endif

// function declarations

HRESULT BextBundleExtensionEngineCreate(
    __in PFN_BUNDLE_EXTENSION_ENGINE_PROC pfnBundleExtensionEngineProc,
    __in_opt LPVOID pvBundleExtensionEngineProcContext,
    __out IBundleExtensionEngine** ppEngineForExtension
    );

#ifdef __cplusplus
}
#endif
