#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

// structs

typedef struct _BURN_EXTENSION_ENGINE_CONTEXT
{
    BURN_ENGINE_STATE* pEngineState;
} BURN_EXTENSION_ENGINE_CONTEXT;

// function declarations

HRESULT WINAPI EngineForExtensionProc(
    __in BUNDLE_EXTENSION_ENGINE_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    );

#if defined(__cplusplus)
}
#endif
