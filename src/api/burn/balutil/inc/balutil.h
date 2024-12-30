#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "dutil.h"


#ifdef __cplusplus
extern "C" {
#endif

#define BalExitOnFailureSource(d, x, f, ...) if (FAILED(x)) { BalLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BalExitOnRootFailureSource(d, x, f, ...) if (FAILED(x)) { BalLogError(x, f, __VA_ARGS__); Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BalExitWithRootFailureSource(d, x, e, f, ...) { x = FAILED(e) ? e : E_FAIL; BalLogError(x, f, __VA_ARGS__); Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BalExitOnLastErrorSource(d, x, f, ...) { x = ::GetLastError(); x = HRESULT_FROM_WIN32(x); if (FAILED(x)) { BalLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; } }
#define BalExitOnNullSource(d, p, x, e, f, ...) if (NULL == p) { x = e; BalLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BalExitOnNullWithLastErrorSource(d, p, x, f, ...) if (NULL == p) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } BalLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BalExitWithLastErrorSource(d, x, f, ...) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } BalLogError(x, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BalExitOnWin32ErrorSource(d, e, x, f, ...) if (ERROR_SUCCESS != e) { x = HRESULT_FROM_WIN32(e); if (!FAILED(x)) { x = E_FAIL; } BalLogError(x, f, __VA_ARGS__); Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define BalExitOnOptionalXmlQueryFailureSource(d, x, b, f, ...) { { if (S_FALSE == x || E_NOTFOUND == x) { b = FALSE; x = S_OK; } else { b = SUCCEEDED(x); } }; BalExitOnRootFailureSource(d, x, f, __VA_ARGS__); }
#define BalExitOnRequiredXmlQueryFailureSource(d, x, f, ...) { if (S_FALSE == x) { x = E_NOTFOUND; } BalExitOnRootFailureSource(d, x, f, __VA_ARGS__); }

#define BalExitOnFailure(x, f, ...) BalExitOnFailureSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)
#define BalExitOnRootFailure(x, f, ...) BalExitOnRootFailureSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)
#define BalExitWithRootFailure(x, e, f, ...) BalExitWithRootFailureSource(DUTIL_SOURCE_DEFAULT, x, e, f, __VA_ARGS__)
#define BalExitOnLastError(x, f, ...) BalExitOnLastErrorSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)
#define BalExitOnNull(p, x, e, f, ...) BalExitOnNullSource(DUTIL_SOURCE_DEFAULT, p, x, e, f, __VA_ARGS__)
#define BalExitOnNullWithLastError(p, x, f, ...) BalExitOnNullWithLastErrorSource(DUTIL_SOURCE_DEFAULT, p, x, f, __VA_ARGS__)
#define BalExitWithLastError(x, f, ...) BalExitWithLastErrorSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)
#define BalExitOnWin32Error(e, x, f, ...) BalExitOnWin32ErrorSource(DUTIL_SOURCE_DEFAULT, e, x, f, __VA_ARGS__)
#define BalExitOnOptionalXmlQueryFailure(x, b, f, ...) BalExitOnOptionalXmlQueryFailureSource(DUTIL_SOURCE_DEFAULT, x, b, f, __VA_ARGS__)
#define BalExitOnRequiredXmlQueryFailure(x, f, ...) BalExitOnRequiredXmlQueryFailureSource(DUTIL_SOURCE_DEFAULT, x, f, __VA_ARGS__)

#ifndef FACILITY_WIX
#define FACILITY_WIX 500
#endif

const LPCWSTR BAL_MANIFEST_FILENAME = L"BootstrapperApplicationData.xml";

static const HRESULT E_WIXSTDBA_CONDITION_FAILED = MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIX, 1);

static const HRESULT E_PREREQBA_INFINITE_LOOP = MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIX, 1002);


/*******************************************************************
 BalDebuggerCheck - allows bootstrapper application to explicitly check
        whether a debugger should be attached to the bootstrapper application.

 Note: Automatically called in BootstrapperApplicationRun().
********************************************************************/
DAPI_(VOID) BalDebuggerCheck();

/*******************************************************************
 BalInitialize - remembers the engine interface to enable logging and
                 other functions.

********************************************************************/
DAPI_(void) BalInitialize(
    __in IBootstrapperEngine* pEngine
    );

/*******************************************************************
 BalUninitialize - cleans up utility layer internals.

********************************************************************/
DAPI_(void) BalUninitialize();

/*******************************************************************
 BalManifestLoad - loads the Application manifest into an XML document.

********************************************************************/
DAPI_(HRESULT) BalManifestLoad(
    __in HMODULE hUXModule,
    __out IXMLDOMDocument** ppixdManifest
    );

/*******************************************************************
BalEvaluateCondition - evaluates a condition using variables in the engine.

********************************************************************/
DAPI_(HRESULT) BalEvaluateCondition(
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    );

/*******************************************************************
BalEscapeString - escapes a string to use as part of a formatted string variable.

 Note: Use StrFree() to release psczOut.
********************************************************************/
DAPI_(HRESULT) BalEscapeString(
    __in_z LPCWSTR wzIn,
    __inout LPWSTR* psczOut
    );

/*******************************************************************
BalEscapeStringFromEngine - escapes a string to use as part of a formatted string variable.

 Note: Use StrFree() to release psczOut.
********************************************************************/
DAPI_(HRESULT) BalEscapeStringFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzIn,
    __inout LPWSTR* psczOut
    );

/*******************************************************************
BalFormatString - formats a string using variables in the engine.

 Note: Use StrFree() to release psczOut.
********************************************************************/
DAPI_(HRESULT) BalFormatString(
    __in_z LPCWSTR wzFormat,
    __inout LPWSTR* psczOut
    );

/*******************************************************************
BalFormatStringFromEngine - formats a string using variables in the engine.

 Note: Use StrFree() to release psczOut.
********************************************************************/
DAPI_(HRESULT) BalFormatStringFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzFormat,
    __inout LPWSTR* psczOut
    );

/*******************************************************************
BalGetNumericVariable - gets a number from a variable in the engine.

 Note: Returns E_NOTFOUND if variable does not exist.
********************************************************************/
DAPI_(HRESULT) BalGetNumericVariable(
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    );

/*******************************************************************
BalGetRelatedBundleVariable - gets a string from a shared variable in the engine.

Note: Use StrFree() to release psczValue.
********************************************************************/
DAPI_(HRESULT) BalGetRelatedBundleVariable(
    __in_z LPCWSTR wzBundleCode,
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
);

/*******************************************************************
BalGetRelatedBundleVariableFromEngine - gets a string from a shared variable in the engine.

 Note: Use StrFree() to release psczValue.
********************************************************************/
DAPI_(HRESULT) BalGetRelatedBundleVariableFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzBundleCode,
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
);


/*******************************************************************
BalSetNumericVariable - sets a numeric variable in the engine.

********************************************************************/
DAPI_(HRESULT) BalSetNumericVariable(
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    );

/*******************************************************************
BalVariableExists - checks if a variable exists in the engine.

********************************************************************/
DAPI_(BOOL) BalVariableExists(
    __in_z LPCWSTR wzVariable
    );

/*******************************************************************
BalVariableExistsFromEngine - checks if a variable exists in the engine.

********************************************************************/
DAPI_(BOOL) BalVariableExistsFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzVariable
    );

/*******************************************************************
BalGetStringVariable - gets a string from a variable in the engine.

 Note: Use StrFree() to release psczValue.
********************************************************************/
DAPI_(HRESULT) BalGetStringVariable(
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
    );

/*******************************************************************
BalGetStringVariableFromEngine - gets a string from a variable in the engine.

 Note: Use StrFree() to release psczValue.
********************************************************************/
DAPI_(HRESULT) BalGetStringVariableFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
    );

/*******************************************************************
BalSetStringVariable - sets a string variable in the engine.
    If the value contains unexpanded variables, set fFormatted to true.

********************************************************************/
DAPI_(HRESULT) BalSetStringVariable(
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in BOOL fFormatted
    );

/*******************************************************************
BalGetVersionVariable - gets a version from a variable in the engine.

 Note: Use StrFree() to release psczValue.
********************************************************************/
DAPI_(HRESULT) BalGetVersionVariable(
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
    );

/*******************************************************************
BalGetVersionVariableFromEngine - gets a version from a variable in the engine.

 Note: Use StrFree() to release psczValue.
********************************************************************/
DAPI_(HRESULT) BalGetVersionVariableFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
    );

/*******************************************************************
BalSetVersionVariable - sets a version variable in the engine.

********************************************************************/
DAPI_(HRESULT) BalSetVersionVariable(
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    );

/*******************************************************************
 BalLog - logs a message with the engine.

********************************************************************/
DAPIV_(HRESULT) BalLog(
    __in BOOTSTRAPPER_LOG_LEVEL level,
    __in_z __format_string LPCSTR szFormat,
    ...
    );

/*******************************************************************
 BalLogArgs - logs a message with the engine.

********************************************************************/
DAPI_(HRESULT) BalLogArgs(
    __in BOOTSTRAPPER_LOG_LEVEL level,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

/*******************************************************************
 BalLogError - logs an error message with the engine.

********************************************************************/
DAPIV_(HRESULT) BalLogError(
    __in HRESULT hr,
    __in_z __format_string LPCSTR szFormat,
    ...
    );

/*******************************************************************
 BalLogErrorArgs - logs an error message with the engine.

********************************************************************/
DAPI_(HRESULT) BalLogErrorArgs(
    __in HRESULT hr,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

/*******************************************************************
BalLogId - logs a message with the engine with a string embedded in a
           MESSAGETABLE resource.

********************************************************************/
DAPIV_(HRESULT) BalLogId(
    __in BOOTSTRAPPER_LOG_LEVEL level,
    __in DWORD dwLogId,
    __in HMODULE hModule,
    ...
    );

DAPI_(HRESULT) BalLogIdArgs(
    __in BOOTSTRAPPER_LOG_LEVEL level,
    __in DWORD dwLogId,
    __in HMODULE hModule,
    __in va_list args
    );

#ifdef __cplusplus
}
#endif
