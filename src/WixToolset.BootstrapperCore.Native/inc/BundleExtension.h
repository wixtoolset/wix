#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

enum BUNDLE_EXTENSION_MESSAGE
{
    BUNDLE_EXTENSION_MESSAGE_SEARCH,
};

typedef struct _BUNDLE_EXTENSION_SEARCH_ARGS
{
    DWORD cbSize;
    LPCWSTR wzId;
    LPCWSTR wzVariable;
} BUNDLE_EXTENSION_SEARCH_ARGS;

typedef struct _BUNDLE_EXTENSION_SEARCH_RESULTS
{
    DWORD cbSize;
} BUNDLE_EXTENSION_SEARCH_RESULTS;

extern "C" typedef HRESULT(WINAPI *PFN_BUNDLE_EXTENSION_PROC)(
    __in BUNDLE_EXTENSION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    );

typedef struct _BUNDLE_EXTENSION_CREATE_ARGS
{
    DWORD cbSize;
    DWORD64 qwEngineAPIVersion;
    PFN_BUNDLE_EXTENSION_ENGINE_PROC pfnBundleExtensionEngineProc;
    LPVOID pvBundleExtensionEngineProcContext;
    LPCWSTR wzBootstrapperWorkingFolder;
    LPCWSTR wzBundleExtensionDataPath;
    LPCWSTR wzExtensionId;
} BUNDLE_EXTENSION_CREATE_ARGS;

typedef struct _BUNDLE_EXTENSION_CREATE_RESULTS
{
    DWORD cbSize;
    PFN_BUNDLE_EXTENSION_PROC pfnBundleExtensionProc;
    LPVOID pvBundleExtensionProcContext;
} BUNDLE_EXTENSION_CREATE_RESULTS;

extern "C" typedef HRESULT(WINAPI *PFN_BUNDLE_EXTENSION_CREATE)(
    __in const BUNDLE_EXTENSION_CREATE_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CREATE_RESULTS* pResults
    );

extern "C" typedef void (WINAPI *PFN_BUNDLE_EXTENSION_DESTROY)();

#if defined(__cplusplus)
}
#endif
