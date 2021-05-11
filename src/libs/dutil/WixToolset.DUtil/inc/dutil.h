#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "dutilsources.h"

#define DAPI __stdcall
#define DAPIV __cdecl // used only for functions taking variable length arguments

#define DAPI_(type) EXTERN_C type DAPI
#define DAPIV_(type) EXTERN_C type DAPIV


// asserts and traces
typedef BOOL (DAPI *DUTIL_ASSERTDISPLAYFUNCTION)(__in_z LPCSTR sz);

typedef void (CALLBACK *DUTIL_CALLBACK_TRACEERROR)(
    __in_z LPCSTR szFile,
    __in int iLine,
    __in REPORT_LEVEL rl,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

#ifdef __cplusplus
extern "C" {
#endif

/********************************************************************
 DutilInitialize - initialize dutil.

*******************************************************************/
HRESULT DAPI DutilInitialize(
    __in_opt DUTIL_CALLBACK_TRACEERROR pfnTraceErrorCallback
    );

/********************************************************************
 DutilUninitialize - uninitialize dutil.

*******************************************************************/
void DAPI DutilUninitialize();

void DAPI Dutil_SetAssertModule(__in HMODULE hAssertModule);
void DAPI Dutil_SetAssertDisplayFunction(__in DUTIL_ASSERTDISPLAYFUNCTION pfn);
void DAPI Dutil_Assert(__in_z LPCSTR szFile, __in int iLine);
void DAPI Dutil_AssertSz(__in_z LPCSTR szFile, __in int iLine, __in_z __format_string LPCSTR szMessage);

void DAPI Dutil_TraceSetLevel(__in REPORT_LEVEL ll, __in BOOL fTraceFilenames);
REPORT_LEVEL DAPI Dutil_TraceGetLevel();
void DAPIV Dutil_Trace(__in_z LPCSTR szFile, __in int iLine, __in REPORT_LEVEL rl, __in_z __format_string LPCSTR szMessage, ...);
void DAPIV Dutil_TraceError(__in_z LPCSTR szFile, __in int iLine, __in REPORT_LEVEL rl, __in HRESULT hr, __in_z __format_string LPCSTR szMessage, ...);
void DAPIV Dutil_TraceErrorSource(__in_z LPCSTR szFile, __in int iLine, __in REPORT_LEVEL rl, __in UINT source, __in HRESULT hr, __in_z __format_string LPCSTR szMessage, ...);
void DAPI Dutil_RootFailure(__in_z LPCSTR szFile, __in int iLine, __in HRESULT hrError);

#ifdef __cplusplus
}
#endif


#ifdef DEBUG

#define AssertSetModule(m) (void)Dutil_SetAssertModule(m)
#define AssertSetDisplayFunction(pfn) (void)Dutil_SetAssertDisplayFunction(pfn)
#define Assert(f)          ((f)    ? (void)0 : (void)Dutil_Assert(__FILE__, __LINE__))
#define AssertSz(f, sz)    ((f)    ? (void)0 : (void)Dutil_AssertSz(__FILE__, __LINE__, sz))

#define TraceSetLevel(l, f) (void)Dutil_TraceSetLevel(l, f)
#define TraceGetLevel() (REPORT_LEVEL)Dutil_TraceGetLevel()
#define Trace(l, f, ...) (void)Dutil_Trace(__FILE__, __LINE__, l, f, __VA_ARGS__)
#define TraceError(x, f, ...) (void)Dutil_TraceError(__FILE__, __LINE__, REPORT_ERROR, x, f, __VA_ARGS__)
#define TraceErrorDebug(x, f, ...) (void)Dutil_TraceError(__FILE__, __LINE__, REPORT_DEBUG, x, f, __VA_ARGS__)

#else // !DEBUG

#define AssertSetModule(m)
#define AssertSetDisplayFunction(pfn)
#define Assert(f)
#define AssertSz(f, sz)

#define TraceSetLevel(l, f)
#define Trace(l, f, ...)
#define TraceError(x, f, ...)
#define TraceErrorDebug(x, f, ...)

#endif // DEBUG

// DUTIL_SOURCE_DEFAULT can be overriden
#ifndef DUTIL_SOURCE_DEFAULT
#define DUTIL_SOURCE_DEFAULT DUTIL_SOURCE_UNKNOWN
#endif

// Exit macros
#define ExitFunction() { goto LExit; }
#define ExitFunction1(x) { x; goto LExit; }

#define ExitFunctionWithLastError(x) { x = HRESULT_FROM_WIN32(::GetLastError()); goto LExit; }

#define ExitTraceSource(d, x, s, ...) { TraceError(x, s, __VA_ARGS__); (void)Dutil_TraceErrorSource(__FILE__, __LINE__, REPORT_ERROR, d, x, s, __VA_ARGS__); }
#define ExitTraceDebugSource(d, x, s, ...) { TraceErrorDebug(x, s, __VA_ARGS__); (void)Dutil_TraceErrorSource(__FILE__, __LINE__, REPORT_DEBUG, d, x, s, __VA_ARGS__); }

#define ExitOnLastErrorSource(d, x, s, ...) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (FAILED(x)) { Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, s, __VA_ARGS__); goto LExit; } }
#define ExitOnLastErrorDebugTraceSource(d, x, s, ...) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (FAILED(x)) { Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceDebugSource(d, x, s, __VA_ARGS__); goto LExit; } }
#define ExitWithLastErrorSource(d, x, s, ...) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, s, __VA_ARGS__); goto LExit; }
#define ExitOnFailureSource(d, x, s, ...) if (FAILED(x)) { ExitTraceSource(d, x, s, __VA_ARGS__);  goto LExit; }
#define ExitOnRootFailureSource(d, x, s, ...) if (FAILED(x)) { Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, s, __VA_ARGS__);  goto LExit; }
#define ExitOnFailureDebugTraceSource(d, x, s, ...) if (FAILED(x)) { ExitTraceDebugSource(d, x, s, __VA_ARGS__);  goto LExit; }
#define ExitOnNullSource(d, p, x, e, s, ...) if (NULL == p) { x = e; Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, s, __VA_ARGS__);  goto LExit; }
#define ExitOnNullWithLastErrorSource(d, p, x, s, ...) if (NULL == p) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, s, __VA_ARGS__); goto LExit; }
#define ExitOnNullDebugTraceSource(d, p, x, e, s, ...) if (NULL == p) { x = e; Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceDebugSource(d, x, s, __VA_ARGS__);  goto LExit; }
#define ExitOnInvalidHandleWithLastErrorSource(d, p, x, s, ...) if (INVALID_HANDLE_VALUE == p) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, s, __VA_ARGS__); goto LExit; }
#define ExitOnWin32ErrorSource(d, e, x, s, ...) if (ERROR_SUCCESS != e) { x = HRESULT_FROM_WIN32(e); if (!FAILED(x)) { x = E_FAIL; } Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, s, __VA_ARGS__); goto LExit; }

#define ExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_DEFAULT, x, s, __VA_ARGS__)
#define ExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_DEFAULT, x, s, __VA_ARGS__)
#define ExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_DEFAULT, x, s, __VA_ARGS__)
#define ExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_DEFAULT, x, s, __VA_ARGS__)
#define ExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_DEFAULT, x, s, __VA_ARGS__)
#define ExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_DEFAULT, x, s, __VA_ARGS__)
#define ExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_DEFAULT, p, x, e, s, __VA_ARGS__)
#define ExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_DEFAULT, p, x, s, __VA_ARGS__)
#define ExitOnNullDebugTrace(p, x, e, s, ...) ExitOnNullDebugTraceSource(DUTIL_SOURCE_DEFAULT, p, x, e, s, __VA_ARGS__)
#define ExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_DEFAULT, p, x, s, __VA_ARGS__)
#define ExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_DEFAULT, e, x, s, __VA_ARGS__)

// release macros
#define ReleaseObject(x) if (x) { x->Release(); }
#define ReleaseObjectArray(prg, cel) if (prg) { for (DWORD Dutil_ReleaseObjectArrayIndex = 0; Dutil_ReleaseObjectArrayIndex < cel; ++Dutil_ReleaseObjectArrayIndex) { ReleaseObject(prg[Dutil_ReleaseObjectArrayIndex]); } ReleaseMem(prg); }
#define ReleaseVariant(x) { ::VariantClear(&x); }
#define ReleaseNullObject(x) if (x) { (x)->Release(); x = NULL; }
#define ReleaseCertificate(x) if (x) { ::CertFreeCertificateContext(x); x=NULL; }
#define ReleaseHandle(x) if (x) { ::CloseHandle(x); x = NULL; }


// useful defines and macros
#define Unused(x) ((void)x)

#ifndef countof
#if 1
#define countof(ary) (sizeof(ary) / sizeof(ary[0]))
#else
#ifndef __cplusplus
#define countof(ary) (sizeof(ary) / sizeof(ary[0]))
#else
template<typename T> static char countofVerify(void const *, T) throw() { return 0; }
template<typename T> static void countofVerify(T *const, T *const *) throw() {};
#define countof(arr) (sizeof(countofVerify(arr,&(arr))) * sizeof(arr)/sizeof(*(arr)))
#endif
#endif
#endif

#define roundup(x, n) roundup_typed(x, n, DWORD)
#define roundup_typed(x, n, t) (((t)(x) + ((t)(n) - 1)) & ~((t)(n) - 1))

#define HRESULT_FROM_RPC(x) ((HRESULT) ((x) | FACILITY_RPC))

#ifndef MAXSIZE_T
#define MAXSIZE_T ((SIZE_T)~((SIZE_T)0))
#endif

typedef const BYTE* LPCBYTE;

#define E_FILENOTFOUND HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND)
#define E_PATHNOTFOUND HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND)
#define E_INVALIDDATA HRESULT_FROM_WIN32(ERROR_INVALID_DATA)
#define E_INVALIDSTATE HRESULT_FROM_WIN32(ERROR_INVALID_STATE)
#define E_INSUFFICIENT_BUFFER HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)
#define E_MOREDATA HRESULT_FROM_WIN32(ERROR_MORE_DATA)
#define E_NOMOREITEMS HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS)
#define E_NOTFOUND HRESULT_FROM_WIN32(ERROR_NOT_FOUND)
#define E_MODNOTFOUND HRESULT_FROM_WIN32(ERROR_MOD_NOT_FOUND)
#define E_BADCONFIGURATION HRESULT_FROM_WIN32(ERROR_BAD_CONFIGURATION)

#define AddRefAndRelease(x) { x->AddRef(); x->Release(); }

#define MAKEDWORD(lo, hi) ((DWORD)MAKELONG(lo, hi))
#define MAKEQWORDVERSION(mj, mi, b, r) (((DWORD64)MAKELONG(r, b)) | (((DWORD64)MAKELONG(mi, mj)) << 32))


#ifdef __cplusplus
extern "C" {
#endif

// other functions
HRESULT DAPI LoadSystemLibrary(__in_z LPCWSTR wzModuleName, __out HMODULE *phModule);
HRESULT DAPI LoadSystemLibraryWithPath(__in_z LPCWSTR wzModuleName, __out HMODULE *phModule, __deref_out_z_opt LPWSTR* psczPath);

#ifdef __cplusplus
}
#endif
