#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "dutil.h"

#include "IBundleExtensionEngine.h"
#include "IBundleExtension.h"

#ifdef __cplusplus
extern "C" {
#endif

#define BextExitOnFailureSource(d, x, f, ...) if (FAILED(x)) { BextLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BextExitOnRootFailureSource(d, x, f, ...) if (FAILED(x)) { BextLogError(x, f, __VA_ARGS__); Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BextExitWithRootFailureSource(d, x, e, f, ...) { x = FAILED(e) ? e : E_FAIL; BextLogError(x, f, __VA_ARGS__); Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BextExitOnLastErrorSource(d, x, f, ...) { x = ::GetLastError(); x = HRESULT_FROM_WIN32(x); if (FAILED(x)) { BextLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; } }
#define BextExitOnNullSource(d, p, x, e, f, ...) if (NULL == p) { x = e; BextLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BextExitOnNullWithLastErrorSource(d, p, x, f, ...) if (NULL == p) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } BextLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BextExitWithLastErrorSource(d, x, f, ...) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } BextLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BextExitOnWin32ErrorSource(d, e, x, f, ...) if (ERROR_SUCCESS != e) { x = HRESULT_FROM_WIN32(e); if (!FAILED(x)) { x = E_FAIL; } BextLogError(x, f, __VA_ARGS__); Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BextExitOnOptionalXmlQueryFailureSource(d, x, b, f, ...) { { if (S_FALSE == x || E_NOTFOUND == x) { b = FALSE; x = S_OK; } else { b = SUCCEEDED(x); } }; BextExitOnRootFailureSource(d, x, f, __VA_ARGS__); }
#define BextExitOnRequiredXmlQueryFailureSource(d, x, f, ...) { if (S_FALSE == x) { x = E_NOTFOUND; } BextExitOnRootFailureSource(d, x, f, __VA_ARGS__); }

#define BextExitOnFailure(x, f, ...) BextExitOnFailureSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)
#define BextExitOnRootFailure(x, f, ...) BextExitOnRootFailureSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)
#define BextExitWithRootFailure(x, e, f, ...) BextExitWithRootFailureSource(DUTIL_SOURCE_DEFAULT, x, e, f, __VA_ARGS__)
#define BextExitOnLastError(x, f, ...) BextExitOnLastErrorSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)
#define BextExitOnNull(p, x, e, f, ...) BextExitOnNullSource(DUTIL_SOURCE_DEFAULT, p, x, e, f, __VA_ARGS__)
#define BextExitOnNullWithLastError(p, x, f, ...) BextExitOnNullWithLastErrorSource(DUTIL_SOURCE_DEFAULT, p, x, f, __VA_ARGS__)
#define BextExitWithLastError(x, f, ...) BextExitWithLastErrorSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)
#define BextExitOnWin32Error(e, x, f, ...) BextExitOnWin32ErrorSource(DUTIL_SOURCE_DEFAULT, e, x, f, __VA_ARGS__)
#define BextExitOnOptionalXmlQueryFailure(x, b, f, ...) BextExitOnOptionalXmlQueryFailureSource(DUTIL_SOURCE_DEFAULT, x, b, f, __VA_ARGS__)
#define BextExitOnRequiredXmlQueryFailure(x, f, ...) BextExitOnRequiredXmlQueryFailureSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)

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
 BextLogArgs - logs a message with the engine.

********************************************************************/
DAPI_(HRESULT) BextLogArgs(
    __in BUNDLE_EXTENSION_LOG_LEVEL level,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

/*******************************************************************
 BextLogError - logs an error message with the engine.

********************************************************************/
DAPIV_(HRESULT) BextLogError(
    __in HRESULT hr,
    __in_z __format_string LPCSTR szFormat,
    ...
    );

/*******************************************************************
 BextLogErrorArgs - logs an error message with the engine.

********************************************************************/
DAPI_(HRESULT) BextLogErrorArgs(
    __in HRESULT hr,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

#ifdef __cplusplus
}
#endif
