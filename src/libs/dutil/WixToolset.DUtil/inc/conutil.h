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
/********************************************************************
 ConsoleInitialize - initialize console for UTF-8

*********************************************************************/
HRESULT DAPI ConsoleInitialize();
void DAPI ConsoleUninitialize();

void DAPI ConsoleGreen();
void DAPI ConsoleRed();
void DAPI ConsoleYellow();
void DAPI ConsoleNormal();

/********************************************************************
 ConsoleWrite - full color printfA without libc

 NOTE: only supports ANSI characters
       assumes already in normal color and resets the screen to normal color
********************************************************************/
HRESULT DAPI ConsoleWrite(
    CONSOLE_COLOR cc,
    __in_z __format_string LPCSTR szFormat,
    ...
    );

/********************************************************************
 ConsoleWriteW - sends UTF-8 characters to console out in color.

 NOTE: assumes already in normal color and resets the screen to normal color
********************************************************************/
HRESULT DAPI ConsoleWriteW(
    __in CONSOLE_COLOR cc,
    __in_z LPCWSTR wzData
    );

/********************************************************************
 ConsoleWriteLine - full color printfA plus newline without libc

 NOTE: only supports ANSI characters
       assumes already in normal color and resets the screen to normal color
********************************************************************/
HRESULT DAPI ConsoleWriteLine(
    CONSOLE_COLOR cc,
    __in_z __format_string LPCSTR szFormat,
    ...
    );

/********************************************************************
 ConsoleWriteError - display an error to the console out

 NOTE: only supports ANSI characters
       does not write to stderr
********************************************************************/
HRESULT DAPI ConsoleWriteError(
    HRESULT hrError,
    CONSOLE_COLOR cc,
    __in_z __format_string LPCSTR szFormat,
    ...
    );

/********************************************************************
 ConsoleReadW - reads a line from console in as UTF-8 to populate Unicode buffer

********************************************************************/
HRESULT DAPI ConsoleReadW(
    __deref_out_z LPWSTR* ppwzBuffer
    );

/********************************************************************
 ConsoleReadNonBlockingW - Read from the console without blocking
 Won't work for redirected files (exe < txtfile), but will work for stdin redirected to
 an anonymous or named pipe

 if (fReadLine), stop reading immediately when \r\n is found
*********************************************************************/
HRESULT DAPI ConsoleReadNonBlockingW(
    __deref_out_ecount_opt(*pcchSize) LPWSTR* ppwzBuffer,
    __out DWORD* pcchSize,
    BOOL fReadLine
    );

/********************************************************************
 ConsoleReadStringA - get console input without libc

 NOTE: only supports ANSI characters
*********************************************************************/
HRESULT DAPI ConsoleReadStringA(
    __deref_inout_ecount_part(cchCharBuffer,*pcchNumCharReturn) LPSTR* szCharBuffer,
    CONST DWORD cchCharBuffer,
    __out DWORD* pcchNumCharReturn
    );

/********************************************************************
 ConsoleReadStringW - get console input without libc

*********************************************************************/
HRESULT DAPI ConsoleReadStringW(
    __deref_inout_ecount_part(cchCharBuffer,*pcchNumCharReturn) LPWSTR* szCharBuffer,
    CONST DWORD cchCharBuffer,
    __out DWORD* pcchNumCharReturn
    );

/********************************************************************
 ConsoleSetReadHidden - set console input no echo

*********************************************************************/
HRESULT DAPI ConsoleSetReadHidden(void);

/********************************************************************
 ConsoleSetReadNormal - reset to echo

*********************************************************************/
HRESULT DAPI ConsoleSetReadNormal(void);

#ifdef __cplusplus
}
#endif

