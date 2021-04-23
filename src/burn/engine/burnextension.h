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
    LPWSTR sczEntryPayloadId;
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
#if defined(__cplusplus)
}
#endif
