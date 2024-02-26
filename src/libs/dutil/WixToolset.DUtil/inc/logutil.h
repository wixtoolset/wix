#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define LogExitOnFailureSource(d, x, i, f, ...) if (FAILED(x)) { LogErrorId(x, i, __VA_ARGS__); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define LogExitOnRootFailureSource(d, x, i, f, ...) if (FAILED(x)) { LogErrorId(x, i, __VA_ARGS__); Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }
#define LogExitWithRootFailureSource(d, x, e, i, f, ...) { x = FAILED(e) ? e : E_FAIL; LogErrorId(x, i, __VA_ARGS__); Dutil_RootFailure(__FILE__, __LINE__, x); ExitTraceSource(d, x, f, __VA_ARGS__); goto LExit; }

#define LogExitOnFailure(x, i, f, ...) LogExitOnFailureSource(DUTIL_SOURCE_DEFAULT, x, i, f, __VA_ARGS__)
#define LogExitOnRootFailure(x, i, f, ...) LogExitOnRootFailureSource(DUTIL_SOURCE_DEFAULT, x, i, f, __VA_ARGS__)
#define LogExitWithRootFailure(x, e, i, f, ...) LogExitWithRootFailureSource(DUTIL_SOURCE_DEFAULT, x, e, i, f, __VA_ARGS__)

typedef HRESULT (DAPI *PFN_LOGSTRINGWORKRAW)(
    __in_z LPCSTR szString,
    __in_opt LPVOID pvContext
    );

// enums

// structs

// functions
/********************************************************************
 IsLogInitialized - Checks if log is currently initialized.
********************************************************************/
BOOL DAPI IsLogInitialized();

/********************************************************************
 IsLogOpen - Checks if log is currently initialized and open.
********************************************************************/
BOOL DAPI IsLogOpen();

/********************************************************************
 LogInitialize - initializes the logutil API

********************************************************************/
void DAPI LogInitialize(
    __in_opt HMODULE hModule
    );

/********************************************************************
 LogOpen - creates an application log file

 NOTE: if wzExt is null then wzLog is path to desired log else wzLog and wzExt are used to generate log name
********************************************************************/
HRESULT DAPI LogOpen(
    __in_z_opt LPCWSTR wzDirectory,
    __in_z LPCWSTR wzLog,
    __in_z_opt LPCWSTR wzPostfix,
    __in_z_opt LPCWSTR wzExt,
    __in BOOL fAppend,
    __in BOOL fHeader,
    __out_z_opt LPWSTR* psczLogPath
    );

/********************************************************************
 LogDisable - closes any open files and disables in memory logging.

********************************************************************/
void DAPI LogDisable();

/********************************************************************
 LogEnableConsole - Log to console as well
********************************************************************/
void DAPI LogEnableConsole(
    __in BOOL fLogToConsole
    );

/********************************************************************
 LogRedirect - Redirects all logging strings to the specified
               function - or set NULL to disable the hook
********************************************************************/
void DAPI LogRedirect(
    __in_opt PFN_LOGSTRINGWORKRAW vpfLogStringWorkRaw,
    __in_opt LPVOID pvContext
    );

/********************************************************************
 LogRename - Renames a logfile, moving its contents to a new path,
             and re-opening the file for appending at the new
             location
********************************************************************/
HRESULT DAPI LogRename(
    __in_z LPCWSTR wzNewPath
    );

/********************************************************************
 LogFlush - calls ::FlushFileBuffers with the log file handle.

********************************************************************/
HRESULT DAPI LogFlush();

void DAPI LogClose(
    __in BOOL fFooter
    );

void DAPI LogUninitialize(
    __in BOOL fFooter
    );

/********************************************************************
 LogIsOpen - returns whether log file is open or note

********************************************************************/
BOOL DAPI LogIsOpen();

/********************************************************************
 LogSetSpecialParams - sets a special beginline string, endline
                       string, post-timestamp string, etc.
********************************************************************/
HRESULT DAPI LogSetSpecialParams(
    __in_z_opt LPCWSTR wzSpecialBeginLine,
    __in_z_opt LPCWSTR wzSpecialAfterTimeStamp,
    __in_z_opt LPCWSTR wzSpecialEndLine
    );

/********************************************************************
 LogSetLevel - sets the logging level

 NOTE: returns previous logging level
********************************************************************/
REPORT_LEVEL DAPI LogSetLevel(
    __in REPORT_LEVEL rl,
    __in BOOL fLogChange
    );

/********************************************************************
 LogGetLevel - gets the current logging level

********************************************************************/
REPORT_LEVEL DAPI LogGetLevel();

/********************************************************************
 LogGetPath - gets the current log path

********************************************************************/
HRESULT DAPI LogGetPath(
    __out_ecount_z(cchLogPath) LPWSTR pwzLogPath,
    __in DWORD cchLogPath
    );

/********************************************************************
 LogGetHandle - gets the current log file handle

********************************************************************/
HANDLE DAPI LogGetHandle();

/********************************************************************
 LogStringArgs - implementation of LogString

********************************************************************/
HRESULT DAPI LogStringArgs(
    __in REPORT_LEVEL rl,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

/********************************************************************
 LogString - write a string to the log

 NOTE: use printf formatting ("%ls", "%d", etc.)
********************************************************************/
inline HRESULT LogString(
    __in REPORT_LEVEL rl,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    va_start(args, szFormat);
    hr = LogStringArgs(rl, szFormat, args);
    va_end(args);

    return hr;
}

/********************************************************************
 LogStringLineArgs - implementation of LogStringLine

********************************************************************/
HRESULT DAPI LogStringLineArgs(
    __in REPORT_LEVEL rl,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

/********************************************************************
 LogStringLine - write a string plus LOGUTIL_NEWLINE to the log

 NOTE: use printf formatting ("%ls", "%d", etc.)
********************************************************************/
inline HRESULT LogStringLine(
    __in REPORT_LEVEL rl,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    va_start(args, szFormat);
    hr = LogStringLineArgs(rl, szFormat, args);
    va_end(args);

    return hr;
}

/********************************************************************
 LogIdModuleArgs - implementation of LogIdModule

********************************************************************/
HRESULT DAPI LogIdModuleArgs(
    __in REPORT_LEVEL rl,
    __in DWORD dwLogId,
    __in_opt HMODULE hModule,
    __in va_list args
    );

/********************************************************************
 LogIdModule - write a string embedded in a MESSAGETABLE in the specified module to the log

 NOTE: uses format string from MESSAGETABLE resource
********************************************************************/
inline HRESULT LogIdModule(
    __in REPORT_LEVEL rl,
    __in DWORD dwLogId,
    __in_opt HMODULE hModule,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    va_start(args, hModule);
    hr = LogIdModuleArgs(rl, dwLogId, hModule, args);
    va_end(args);

    return hr;
}

/********************************************************************
 LogIdArgs - inline wrapper for LogIdModuleArgs, passing NULL for hModule

********************************************************************/
inline HRESULT LogIdArgs(
    __in REPORT_LEVEL rl,
    __in DWORD dwLogId,
    __in va_list args
    )
{
    return LogIdModuleArgs(rl, dwLogId, NULL, args);
}

/********************************************************************
 LogId - write a string embedded in a MESSAGETABLE in the default module to the log

 NOTE: uses format string from MESSAGETABLE resource
********************************************************************/
inline HRESULT LogId(
    __in REPORT_LEVEL rl,
    __in DWORD dwLogId,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    va_start(args, dwLogId);
    hr = LogIdArgs(rl, dwLogId, args);
    va_end(args);

    return hr;
}

/********************************************************************
 LogErrorStringArgs - implementation of LogErrorString

********************************************************************/
HRESULT DAPI LogErrorStringArgs(
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

/********************************************************************
 LogErrorString - write an error to the log

 NOTE: use printf formatting ("%ls", "%d", etc.)
********************************************************************/
inline HRESULT LogErrorString(
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr  = S_OK;

    va_list args;
    va_start(args, szFormat);
    hr = LogErrorStringArgs(hrError, szFormat, args);
    va_end(args);

    return hr;
}

/********************************************************************
 LogErrorIdModule - write an error string embedded in the specified module in a MESSAGETABLE to the log

 NOTE:  uses format string from MESSAGETABLE resource
        can log no more than three strings in the error message
********************************************************************/
HRESULT DAPI LogErrorIdModule(
    __in HRESULT hrError,
    __in DWORD dwLogId,
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzString1,
    __in_z_opt LPCWSTR wzString2,
    __in_z_opt LPCWSTR wzString3
    );

/********************************************************************
 LogErrorId - write an error string embedded in the default module in a MESSAGETABLE to the log

 NOTE:  uses format string from MESSAGETABLE resource
        can log no more than three strings in the error message
********************************************************************/
inline HRESULT LogErrorId(
    __in HRESULT hrError,
    __in DWORD dwLogId,
    __in_z_opt LPCWSTR wzString1 = NULL,
    __in_z_opt LPCWSTR wzString2 = NULL,
    __in_z_opt LPCWSTR wzString3 = NULL
    )
{
    return LogErrorIdModule(hrError, dwLogId, NULL, wzString1, wzString2, wzString3);
}

/********************************************************************
 LogHeader - write a standard header to the log

********************************************************************/
HRESULT DAPI LogHeader();

/********************************************************************
 LogFooter - write a standard footer to the log

********************************************************************/
HRESULT DAPI LogFooter();

/********************************************************************
 LogStringWorkRaw - Write a raw, unformatted string to the log

********************************************************************/
HRESULT DAPI LogStringWorkRaw(
    __in_z LPCSTR szLogData
    );

#ifdef __cplusplus
}
#endif

