#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "dutil.h"


#ifdef __cplusplus
extern "C" {
#endif

#define BextExitOnFailure(x, f, ...) if (FAILED(x)) { BextLogError(x, f, __VA_ARGS__); ExitTrace(x, f, __VA_ARGS__); goto LExit; }
#define BextExitOnRootFailure(x, f, ...) if (FAILED(x)) { BextLogError(x, f, __VA_ARGS__); Dutil_RootFailure(__FILE__, __LINE__, x); ExitTrace(x, f, __VA_ARGS__); goto LExit; }
#define BextExitOnNullWithLastError(p, x, f, ...) if (NULL == p) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } BextLogError(x, f, __VA_ARGS__); ExitTrace(x, f, __VA_ARGS__); goto LExit; }

const LPCWSTR BUNDLE_EXTENSION_MANIFEST_FILENAME = L"BundleExtensionData.xml";


/*******************************************************************
 BextInitialize - remembers the engine interface to enable logging and
                  other functions.

********************************************************************/
DAPI_(void) BextInitialize(
    __in IBundleExtensionEngine* pEngine
    );

/*******************************************************************
 BextInitializeFromCreateArgs - convenience function to call BextBundleExtensionEngineCreate
                                then pass it along to BextInitialize.

********************************************************************/
DAPI_(HRESULT) BextInitializeFromCreateArgs(
    __in const BUNDLE_EXTENSION_CREATE_ARGS* pArgs,
    __out IBundleExtensionEngine** ppEngine
    );

/*******************************************************************
 BextUninitialize - cleans up utility layer internals.

********************************************************************/
DAPI_(void) BextUninitialize();

/*******************************************************************
 BextGetBundleExtensionDataNode - gets the requested BundleExtension node.

********************************************************************/
DAPI_(HRESULT) BextGetBundleExtensionDataNode(
    __in IXMLDOMDocument* pixdManifest,
    __in LPCWSTR wzExtensionId,
    __out IXMLDOMNode** ppixnBundleExtension
    );

/*******************************************************************
 BextLog - logs a message with the engine.

********************************************************************/
DAPIV_(HRESULT) BextLog(
    __in BUNDLE_EXTENSION_LOG_LEVEL level,
    __in_z __format_string LPCSTR szFormat,
    ...
    );

/*******************************************************************
 BextLogError - logs an error message with the engine.

********************************************************************/
DAPIV_(HRESULT) BextLogError(
    __in HRESULT hr,
    __in_z __format_string LPCSTR szFormat,
    ...
    );

#ifdef __cplusplus
}
#endif
