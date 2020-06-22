#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define ConsoleExitOnFailureSource(d, x, c, f, ...) if (FAILED(x)) { ConsoleWriteError(x, c, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define ConsoleExitOnLastErrorSource(d, x, c, f, ...) { x = ::GetLastError(); x = HRESULT_FROM_WIN32(x); if (FAILED(x)) { ConsoleWriteError(x, c, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; } }
#define ConsoleExitOnNullSource(d, p, x, e, c, f, ...) if (NULL == p) { x = e; ConsoleWriteError(x, c, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define ConsoleExitOnNullWithLastErrorSource(d, p, x, c, f, ...) if (NULL == p) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } ConsoleWriteError(x, c, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define ConsoleExitWithLastErrorSource(d, x, c, f, ...) { DWORD Dutil_er = ::GetLastError(); x = HRESULT_FROM_WIN32(Dutil_er); if (!FAILED(x)) { x = E_FAIL; } ConsoleWriteError(x, c, f, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }


#define ConsoleExitOnFailure(x, c, f, ...) ConsoleExitOnFailureSource(DUTIL_SOURCE_DEFAULT, x, c, f, __VA_ARGS__)
#define ConsoleExitOnLastError(x, c, f, ...) ConsoleExitOnLastErrorSource(DUTIL_SOURCE_DEFAULT, x, c, f, __VA_ARGS__)
#define ConsoleExitOnNull(p, x, e, c, f, ...) ConsoleExitOnNullSource(DUTIL_SOURCE_DEFAULT, p, x, e, c, f, __VA_ARGS__)
#define ConsoleExitOnNullWithLastError(p, x, c, f, ...) ConsoleExitOnNullWithLastErrorSource(DUTIL_SOURCE_DEFAULT, p, x, c, f, __VA_ARGS__)
#define ConsoleExitWithLastError(x, c, f, ...) ConsoleExitWithLastErrorSource(DUTIL_SOURCE_DEFAULT, x, c, f, __VA_ARGS__)

// enums
typedef enum CONSOLE_COLOR { CONSOLE_COLOR_NORMAL, CONSOLE_COLOR_RED, CONSOLE_COLOR_YELLOW, CONSOLE_COLOR_GREEN } CONSOLE_COLOR;

// structs

// functions
HRESULT DAPI ConsoleInitialize();
void DAPI ConsoleUninitialize();

void DAPI ConsoleGreen();
void DAPI ConsoleRed();
void DAPI ConsoleYellow();
void DAPI ConsoleNormal();

HRESULT DAPI ConsoleWrite(
    CONSOLE_COLOR cc,
    __in_z __format_string LPCSTR szFormat,
    ...
    );
HRESULT DAPI ConsoleWriteLine(
    CONSOLE_COLOR cc,
    __in_z __format_string LPCSTR szFormat,
    ...
    );
HRESULT DAPI ConsoleWriteError(
    HRESULT hrError,
    CONSOLE_COLOR cc,
    __in_z __format_string LPCSTR szFormat,
    ...
    );

HRESULT DAPI ConsoleReadW(
    __deref_out_z LPWSTR* ppwzBuffer
    );

HRESULT DAPI ConsoleReadStringA(
    __deref_out_ecount_part(cchCharBuffer,*pcchNumCharReturn) LPSTR* szCharBuffer,
    CONST DWORD cchCharBuffer,
    __out DWORD* pcchNumCharReturn
    );
HRESULT DAPI ConsoleReadStringW(
    __deref_out_ecount_part(cchCharBuffer,*pcchNumCharReturn) LPWSTR* szCharBuffer,
    CONST DWORD cchCharBuffer,
    __out DWORD* pcchNumCharReturn
    );

HRESULT DAPI ConsoleReadNonBlockingW(
    __deref_out_ecount_opt(*pcchSize) LPWSTR* ppwzBuffer,
    __out DWORD* pcchSize,
    BOOL fReadLine
    );

HRESULT DAPI ConsoleSetReadHidden(void);
HRESULT DAPI ConsoleSetReadNormal(void);

#ifdef __cplusplus
}
#endif

