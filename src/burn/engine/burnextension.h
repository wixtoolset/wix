#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#define BEEAPI HRESULT __stdcall

#if defined(__cplusplus)
extern "C" {
#endif

// structs

typedef struct _BURN_EXTENSION_ENGINE_CONTEXT BURN_EXTENSION_ENGINE_CONTEXT;

typedef struct _BURN_EXTENSION
{
    LPWSTR sczId;

    BURN_PAYLOAD* pEntryPayload;

    HMODULE hBextModule;
    PFN_BUNDLE_EXTENSION_PROC pfnBurnExtensionProc;
    LPVOID pvBurnExtensionProcContext;
} BURN_EXTENSION;

typedef struct _BURN_EXTENSIONS
{
    BURN_EXTENSION* rgExtensions;
    DWORD cExtensions;
} BURN_EXTENSIONS;

// functions

HRESULT BurnExtensionParseFromXml(
    __in BURN_EXTENSIONS* pBurnExtensions,
    __in BURN_PAYLOADS* pBaPayloads,
    __in IXMLDOMNode* pixnBundle
    );
void BurnExtensionUninitialize(
    __in BURN_EXTENSIONS* pBurnExtensions
    );
HRESULT BurnExtensionLoad(
    __in BURN_EXTENSIONS* pBurnExtensions,
    __in BURN_EXTENSION_ENGINE_CONTEXT* pEngineContext
    );
void BurnExtensionUnload(
    __in BURN_EXTENSIONS* pBurnExtensions
    );
HRESULT BurnExtensionFindById(
    __in BURN_EXTENSIONS* pBurnExtensions,
    __in_z LPCWSTR wzId,
    __out BURN_EXTENSION** ppExtension
    );
BEEAPI BurnExtensionPerformSearch(
    __in BURN_EXTENSION* pExtension,
    __in LPWSTR wzSearchId,
    __in LPWSTR wzVariable
    );
BEEAPI BurnExtensionContainerOpen(
    __in BURN_EXTENSION* pExtension,
    __in LPCWSTR wzContainerId,
    __in LPCWSTR wzFilePath,
    __in BURN_CONTAINER_CONTEXT* pContext
    );
BEEAPI BurnExtensionContainerOpenAttached(
    __in BURN_EXTENSION* pExtension,
    __in LPCWSTR wzContainerId,
    __in HANDLE hBundle,
    __in DWORD64 qwContainerStartPos,
    __in DWORD64 qwContainerSize,
    __in BURN_CONTAINER_CONTEXT* pContext
    );
BEEAPI BurnExtensionContainerNextStream(
    __in BURN_EXTENSION* pExtension,
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout_z LPWSTR *psczStreamName
    );
BEEAPI BurnExtensionContainerStreamToFile(
    __in BURN_EXTENSION* pExtension,
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in_z LPCWSTR wzFileName
    );
BEEAPI BurnExtensionContainerStreamToBuffer(
    __in BURN_EXTENSION* pExtension,
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout LPBYTE * ppbBuffer,
    __inout SIZE_T * pcbBuffer
    );
BEEAPI BurnExtensionContainerSkipStream(
    __in BURN_EXTENSION* pExtension,
    __in BURN_CONTAINER_CONTEXT* pContext
    );
BEEAPI BurnExtensionContainerClose(
    __in BURN_EXTENSION* pExtension,
    __in BURN_CONTAINER_CONTEXT* pContext
    );

#if defined(__cplusplus)
}
#endif
