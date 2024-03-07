#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include <BootstrapperExtensionEngine.h>

#if defined(__cplusplus)
extern "C" {
#endif

enum BOOTSTRAPPER_EXTENSION_MESSAGE
{
    BOOTSTRAPPER_EXTENSION_MESSAGE_SEARCH,
};

typedef struct _BOOTSTRAPPER_EXTENSION_SEARCH_ARGS
{
    DWORD cbSize;
    LPCWSTR wzId;
    LPCWSTR wzVariable;
} BOOTSTRAPPER_EXTENSION_SEARCH_ARGS;

typedef struct _BOOTSTRAPPER_EXTENSION_SEARCH_RESULTS
{
    DWORD cbSize;
} BOOTSTRAPPER_EXTENSION_SEARCH_RESULTS;

extern "C" typedef HRESULT(WINAPI *PFN_BOOTSTRAPPER_EXTENSION_PROC)(
    __in BOOTSTRAPPER_EXTENSION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    );

typedef struct _BOOTSTRAPPER_EXTENSION_CREATE_ARGS
{
    DWORD cbSize;
    DWORD64 qwEngineAPIVersion;
    PFN_BOOTSTRAPPER_EXTENSION_ENGINE_PROC pfnBootstrapperExtensionEngineProc;
    LPVOID pvBootstrapperExtensionEngineProcContext;
    LPCWSTR wzBootstrapperWorkingFolder;
    LPCWSTR wzBootstrapperExtensionDataPath;
    LPCWSTR wzExtensionId;
} BOOTSTRAPPER_EXTENSION_CREATE_ARGS;

typedef struct _BOOTSTRAPPER_EXTENSION_CREATE_RESULTS
{
    DWORD cbSize;
    PFN_BOOTSTRAPPER_EXTENSION_PROC pfnBootstrapperExtensionProc;
    LPVOID pvBootstrapperExtensionProcContext;
} BOOTSTRAPPER_EXTENSION_CREATE_RESULTS;

extern "C" typedef HRESULT(WINAPI *PFN_BOOTSTRAPPER_EXTENSION_CREATE)(
    __in const BOOTSTRAPPER_EXTENSION_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_EXTENSION_CREATE_RESULTS* pResults
    );

extern "C" typedef void (WINAPI *PFN_BOOTSTRAPPER_EXTENSION_DESTROY)();

#if defined(__cplusplus)
}
#endif
