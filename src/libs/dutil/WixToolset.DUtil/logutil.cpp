// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define LoguExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_LOGUTIL, x, s, __VA_ARGS__)
#define LoguExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_LOGUTIL, x, s, __VA_ARGS__)
#define LoguExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_LOGUTIL, x, s, __VA_ARGS__)
#define LoguExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_LOGUTIL, x, s, __VA_ARGS__)
#define LoguExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_LOGUTIL, x, s, __VA_ARGS__)
#define LoguExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_LOGUTIL, x, s, __VA_ARGS__)
#define LoguExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_LOGUTIL, p, x, e, s, __VA_ARGS__)
#define LoguExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_LOGUTIL, p, x, s, __VA_ARGS__)
#define LoguExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_LOGUTIL, p, x, e, s, __VA_ARGS__)
#define LoguExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_LOGUTIL, p, x, s, __VA_ARGS__)
#define LoguExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_LOGUTIL, e, x, s, __VA_ARGS__)
#define LoguExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_LOGUTIL, g, x, s, __VA_ARGS__)

// globals
static HMODULE LogUtil_hModule = NULL;
static BOOL LogUtil_fDisabled = FALSE;
static HANDLE LogUtil_hLog = INVALID_HANDLE_VALUE;
static HANDLE LogUtil_hStdOut = INVALID_HANDLE_VALUE;
static HANDLE LogUtil_hStdErr = INVALID_HANDLE_VALUE;
static LPWSTR LogUtil_sczLogPath = NULL;
static LPSTR LogUtil_sczPreInitBuffer = NULL;
static REPORT_LEVEL LogUtil_rlCurrent = REPORT_STANDARD;
static CRITICAL_SECTION LogUtil_csLog = { };
static BOOL LogUtil_fInitializedCriticalSection = FALSE;

// Customization of certain parts of the string, within a line
static LPWSTR LogUtil_sczSpecialBeginLine = NULL;
static LPWSTR LogUtil_sczSpecialEndLine = NULL;
static LPWSTR LogUtil_sczSpecialAfterTimeStamp = NULL;

static LPCSTR LOGUTIL_UNKNOWN = "unknown";
static LPCSTR LOGUTIL_WARNING = "warning";
static LPCSTR LOGUTIL_STANDARD = "standard";
static LPCSTR LOGUTIL_VERBOSE = "verbose";
static LPCSTR LOGUTIL_DEBUG = "debug";
static LPCSTR LOGUTIL_NONE = "none";

// prototypes
static HRESULT LogStringWorkRawUnsynchronized(
    __in_z LPCSTR szLogData
    );
static HRESULT LogIdWork(
    __in REPORT_LEVEL rl,
    __in_opt HMODULE hModule,
    __in DWORD dwLogId,
    __in va_list args,
    __in BOOL fLOGUTIL_NEWLINE
    );
static HRESULT LogStringWorkArgs(
    __in REPORT_LEVEL rl,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args,
    __in BOOL fLOGUTIL_NEWLINE
    );
static HRESULT LogStringWork(
    __in REPORT_LEVEL rl,
    __in DWORD dwLogId,
    __in_z LPCWSTR sczString,
    __in BOOL fLOGUTIL_NEWLINE
    );
static void LogStringToConsole(
    __in BOOL fIsError,
    __in BOOL fIsWarning,
    __in_z LPCSTR sczMultiByte
    );

// Hook to allow redirecting LogStringWorkRaw function calls
static PFN_LOGSTRINGWORKRAW s_vpfLogStringWorkRaw = NULL;
static LPVOID s_vpvLogStringWorkRawContext = NULL;


extern "C" BOOL DAPI IsLogInitialized()
{
    return LogUtil_fInitializedCriticalSection;
}

extern "C" BOOL DAPI IsLogOpen()
{
    return (INVALID_HANDLE_VALUE != LogUtil_hLog && NULL != LogUtil_sczLogPath);
}


extern "C" void DAPI LogInitialize(
    __in_opt HMODULE hModule
    )
{
    AssertSz(INVALID_HANDLE_VALUE == LogUtil_hLog && !LogUtil_sczLogPath, "LogInitialize() or LogOpen() - already called.");

    LogUtil_hModule = hModule;
    LogUtil_fDisabled = FALSE;

    ::InitializeCriticalSection(&LogUtil_csLog);
    LogUtil_fInitializedCriticalSection = TRUE;
}


extern "C" HRESULT DAPI LogOpen(
    __in_z_opt LPCWSTR wzDirectory,
    __in_z LPCWSTR wzLog,
    __in_z_opt LPCWSTR wzPostfix,
    __in_z_opt LPCWSTR wzExt,
    __in BOOL fAppend,
    __in BOOL fHeader,
    __out_z_opt LPWSTR* psczLogPath
    )
{
    HRESULT hr = S_OK;
    BOOL fEnteredCriticalSection = FALSE;
    LPWSTR sczCombined = NULL;
    LPWSTR sczLogDirectory = NULL;

    ::EnterCriticalSection(&LogUtil_csLog);
    fEnteredCriticalSection = TRUE;

    if (wzExt && *wzExt)
    {
        hr = PathCreateTimeBasedTempFile(wzDirectory, wzLog, wzPostfix, wzExt, &LogUtil_sczLogPath, &LogUtil_hLog);
        LoguExitOnFailure(hr, "Failed to create log based on current system time.");
    }
    else
    {
        hr = PathConcat(wzDirectory, wzLog, &sczCombined);
        LoguExitOnFailure(hr, "Failed to combine the log path.");

        if (!PathIsFullyQualified(sczCombined))
        {
            hr = PathExpand(&LogUtil_sczLogPath, sczCombined, PATH_EXPAND_FULLPATH);
            LoguExitOnFailure(hr, "Failed to expand the log path.");
        }
        else
        {
            LogUtil_sczLogPath = sczCombined;
            sczCombined = NULL;
        }

        hr = PathGetDirectory(LogUtil_sczLogPath, &sczLogDirectory);
        LoguExitOnFailure(hr, "Failed to get log directory.");

        hr = DirEnsureExists(sczLogDirectory, NULL);
        LoguExitOnFailure(hr, "Failed to ensure log file directory exists: %ls", sczLogDirectory);

        LogUtil_hLog = ::CreateFileW(LogUtil_sczLogPath, GENERIC_WRITE, FILE_SHARE_READ, NULL, (fAppend) ? OPEN_ALWAYS : CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
        if (INVALID_HANDLE_VALUE == LogUtil_hLog)
        {
            LoguExitOnLastError(hr, "failed to create log file: %ls", LogUtil_sczLogPath);
        }

        if (fAppend)
        {
            ::SetFilePointer(LogUtil_hLog, 0, 0, FILE_END);
        }
    }

    LogUtil_fDisabled = FALSE;

    if (fHeader)
    {
        LogHeader();
    }

    if (NULL != LogUtil_sczPreInitBuffer)
    {
        // Log anything that was logged before LogOpen() was called.
        LogStringWorkRaw(LogUtil_sczPreInitBuffer);
        ReleaseNullStr(LogUtil_sczPreInitBuffer);
    }

    if (psczLogPath)
    {
        hr = StrAllocString(psczLogPath, LogUtil_sczLogPath, 0);
        LoguExitOnFailure(hr, "Failed to copy log path.");
    }

LExit:
    if (fEnteredCriticalSection)
    {
        ::LeaveCriticalSection(&LogUtil_csLog);
    }

    ReleaseStr(sczCombined);
    ReleaseStr(sczLogDirectory);

    return hr;
}


void DAPI LogDisable()
{
    ::EnterCriticalSection(&LogUtil_csLog);

    LogUtil_fDisabled = TRUE;

    ReleaseFileHandle(LogUtil_hLog);
    ReleaseFileHandle(LogUtil_hStdOut);
    ReleaseFileHandle(LogUtil_hStdErr);
    ReleaseNullStr(LogUtil_sczLogPath);
    ReleaseNullStr(LogUtil_sczPreInitBuffer);

    ::LeaveCriticalSection(&LogUtil_csLog);
}


void DAPI LogEnableConsole(
    __in BOOL fLogToConsole
    )
{
    if (fLogToConsole)
    {
        if (LogUtil_hStdOut == INVALID_HANDLE_VALUE)
        {
            // Attempt to attach to parent console
            if (!::AttachConsole(ATTACH_PARENT_PROCESS))
            {
                LogErrorString(HRESULT_FROM_WIN32(::GetLastError()), "Failed to attach parent console");
            }

            LogUtil_hStdErr = ::GetStdHandle(STD_ERROR_HANDLE);
            LogUtil_hStdOut = ::GetStdHandle(STD_OUTPUT_HANDLE);
            if (LogUtil_hStdOut == INVALID_HANDLE_VALUE)
            {
                LogErrorString(HRESULT_FROM_WIN32(::GetLastError()), "Failed to get stdout handle. Attempting to use 'CONOUT$'");
                SECURITY_ATTRIBUTES sa;

                ::ZeroMemory(&sa, sizeof(SECURITY_ATTRIBUTES));
                sa.nLength = sizeof(SECURITY_ATTRIBUTES);
                sa.bInheritHandle = TRUE;
                sa.lpSecurityDescriptor = nullptr;

                LogUtil_hStdOut = ::CreateFileA("CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, &sa, OPEN_EXISTING, 0, NULL);
                if (LogUtil_hStdOut == INVALID_HANDLE_VALUE)
                {
                    LogErrorString(HRESULT_FROM_WIN32(::GetLastError()), "Failed to get console or stdout handle");
                }
            }
        }
    }
    else
    {
        ReleaseFileHandle(LogUtil_hStdOut);
        ReleaseFileHandle(LogUtil_hStdErr);
    }
}


void DAPI LogRedirect(
    __in_opt PFN_LOGSTRINGWORKRAW vpfLogStringWorkRaw,
    __in_opt LPVOID pvContext
    )
{
    ::EnterCriticalSection(&LogUtil_csLog);

    s_vpfLogStringWorkRaw = vpfLogStringWorkRaw;
    s_vpvLogStringWorkRawContext = pvContext;

    ::LeaveCriticalSection(&LogUtil_csLog);
}


HRESULT DAPI LogRename(
    __in_z LPCWSTR wzNewPath
    )
{
    HRESULT hr = S_OK;
    BOOL fEnteredCriticalSection = FALSE;

    ::EnterCriticalSection(&LogUtil_csLog);
    fEnteredCriticalSection = TRUE;

    ReleaseFileHandle(LogUtil_hLog);

    hr = FileEnsureMove(LogUtil_sczLogPath, wzNewPath, TRUE, TRUE);
    LoguExitOnFailure(hr, "Failed to move logfile to new location: %ls", wzNewPath);

    hr = StrAllocString(&LogUtil_sczLogPath, wzNewPath, 0);
    LoguExitOnFailure(hr, "Failed to store new logfile path: %ls", wzNewPath);

    LogUtil_hLog = ::CreateFileW(LogUtil_sczLogPath, GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (INVALID_HANDLE_VALUE == LogUtil_hLog)
    {
        LoguExitOnLastError(hr, "failed to create log file: %ls", LogUtil_sczLogPath);
    }

    // Enable "append" mode by moving file pointer to the end
    ::SetFilePointer(LogUtil_hLog, 0, 0, FILE_END);

LExit:
    if (fEnteredCriticalSection)
    {
        ::LeaveCriticalSection(&LogUtil_csLog);
    }

    return hr;
}


extern "C" HRESULT DAPI LogFlush()
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&LogUtil_csLog);

    if (INVALID_HANDLE_VALUE == LogUtil_hLog)
    {
        ExitFunction1(hr = S_FALSE);
    }

    if (!::FlushFileBuffers(LogUtil_hLog))
    {
        LoguExitWithLastError(hr, "Failed to flush log file buffers.");
    }

LExit:
    ::LeaveCriticalSection(&LogUtil_csLog);

    return hr;
}


extern "C" void DAPI LogClose(
    __in BOOL fFooter
    )
{
    if (INVALID_HANDLE_VALUE != LogUtil_hLog && fFooter)
    {
        LogFooter();
    }

    ReleaseFileHandle(LogUtil_hLog);
    ReleaseFileHandle(LogUtil_hStdOut);
    ReleaseFileHandle(LogUtil_hStdErr);
    ReleaseNullStr(LogUtil_sczLogPath);
    ReleaseNullStr(LogUtil_sczPreInitBuffer);
}


extern "C" void DAPI LogUninitialize(
    __in BOOL fFooter
    )
{
    LogClose(fFooter);

    if (LogUtil_fInitializedCriticalSection)
    {
        ::DeleteCriticalSection(&LogUtil_csLog);
        LogUtil_fInitializedCriticalSection = FALSE;
    }

    LogUtil_hModule = NULL;
    LogUtil_fDisabled = FALSE;

    ReleaseNullStr(LogUtil_sczSpecialBeginLine);
    ReleaseNullStr(LogUtil_sczSpecialAfterTimeStamp);
    ReleaseNullStr(LogUtil_sczSpecialEndLine);
}


extern "C" BOOL DAPI LogIsOpen()
{
    return INVALID_HANDLE_VALUE != LogUtil_hLog;
}


HRESULT DAPI LogSetSpecialParams(
    __in_z_opt LPCWSTR wzSpecialBeginLine,
    __in_z_opt LPCWSTR wzSpecialAfterTimeStamp,
    __in_z_opt LPCWSTR wzSpecialEndLine
    )
{
    HRESULT hr = S_OK;

    // Handle special string to be prepended before every full line
    if (NULL == wzSpecialBeginLine)
    {
        ReleaseNullStr(LogUtil_sczSpecialBeginLine);
    }
    else
    {
        hr = StrAllocConcat(&LogUtil_sczSpecialBeginLine, wzSpecialBeginLine, 0);
        LoguExitOnFailure(hr, "Failed to allocate copy of special beginline string");
    }

    // Handle special string to be appended to every time stamp
    if (NULL == wzSpecialAfterTimeStamp)
    {
        ReleaseNullStr(LogUtil_sczSpecialAfterTimeStamp);
    }
    else
    {
        hr = StrAllocConcat(&LogUtil_sczSpecialAfterTimeStamp, wzSpecialAfterTimeStamp, 0);
        LoguExitOnFailure(hr, "Failed to allocate copy of special post-timestamp string");
    }

    // Handle special string to be appended before every full line
    if (NULL == wzSpecialEndLine)
    {
        ReleaseNullStr(LogUtil_sczSpecialEndLine);
    }
    else
    {
        hr = StrAllocConcat(&LogUtil_sczSpecialEndLine, wzSpecialEndLine, 0);
        LoguExitOnFailure(hr, "Failed to allocate copy of special endline string");
    }

LExit:
    return hr;
}

extern "C" REPORT_LEVEL DAPI LogSetLevel(
    __in REPORT_LEVEL rl,
    __in BOOL fLogChange
    )
{
    AssertSz(REPORT_ERROR != rl, "REPORT_ERROR is not a valid logging level to set");

    REPORT_LEVEL rlPrev = LogUtil_rlCurrent;

    if (LogUtil_rlCurrent != rl)
    {
        LogUtil_rlCurrent = rl;

        if (fLogChange)
        {
            LPCSTR szLevel = LOGUTIL_UNKNOWN;
            switch (LogUtil_rlCurrent)
            {
            case REPORT_WARNING:
                szLevel = LOGUTIL_WARNING;
                break;
            case REPORT_STANDARD:
                szLevel = LOGUTIL_STANDARD;
                break;
            case REPORT_VERBOSE:
                szLevel = LOGUTIL_VERBOSE;
                break;
            case REPORT_DEBUG:
                szLevel = LOGUTIL_DEBUG;
                break;
            case REPORT_NONE:
                szLevel = LOGUTIL_NONE;
                break;
            }

            LogStringLine(REPORT_STANDARD, "--- logging level: %hs ---", szLevel);
        }
    }

    return rlPrev;
}


extern "C" REPORT_LEVEL DAPI LogGetLevel()
{
    return LogUtil_rlCurrent;
}


extern "C" HRESULT DAPI LogGetPath(
    __out_ecount_z(cchLogPath) LPWSTR pwzLogPath,
    __in DWORD cchLogPath
    )
{
    Assert(pwzLogPath);

    HRESULT hr = S_OK;

    if (NULL == LogUtil_sczLogPath)        // they can't have a path if there isn't one!
    {
        ExitFunction1(hr = E_UNEXPECTED);
    }

    hr = ::StringCchCopyW(pwzLogPath, cchLogPath, LogUtil_sczLogPath);

LExit:
    return hr;
}


extern "C" HANDLE DAPI LogGetHandle()
{
    return LogUtil_hLog;
}


extern "C" HRESULT DAPI LogStringArgs(
    __in REPORT_LEVEL rl,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    AssertSz(REPORT_NONE != rl, "REPORT_NONE is not a valid logging level");
    HRESULT hr = S_OK;

    if (REPORT_ERROR != rl && LogUtil_rlCurrent < rl)
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = LogStringWorkArgs(rl, szFormat, args, FALSE);

LExit:
    return hr;
}

extern "C" HRESULT DAPI LogStringLineArgs(
    __in REPORT_LEVEL rl,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    AssertSz(REPORT_NONE != rl, "REPORT_NONE is not a valid logging level");
    HRESULT hr = S_OK;

    if (REPORT_ERROR != rl && LogUtil_rlCurrent < rl)
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = LogStringWorkArgs(rl, szFormat, args, TRUE);

LExit:
    return hr;
}


extern "C" HRESULT DAPI LogIdModuleArgs(
    __in REPORT_LEVEL rl,
    __in DWORD dwLogId,
    __in_opt HMODULE hModule,
    __in va_list args
    )
{
    AssertSz(REPORT_NONE != rl, "REPORT_NONE is not a valid logging level");
    HRESULT hr = S_OK;

    if (REPORT_ERROR != rl && LogUtil_rlCurrent < rl)
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = LogIdWork(rl, (hModule) ? hModule : LogUtil_hModule, dwLogId, args, TRUE);

LExit:
    return hr;
}


extern "C" HRESULT DAPI LogErrorStringArgs(
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    HRESULT hr  = S_OK;
    LPWSTR sczFormat = NULL;
    LPWSTR sczMessage = NULL;

    hr = StrAllocStringAnsi(&sczFormat, szFormat, 0, CP_ACP);
    LoguExitOnFailure(hr, "Failed to convert format string to wide character string");

    // format the string as a unicode string - this is necessary to be able to include
    // international characters in our output string. This does have the counterintuitive effect
    // that the caller's "%s" is interpreted differently
    // (so callers should use %hs for LPSTR and %ls for LPWSTR)
    hr = StrAllocFormattedArgs(&sczMessage, sczFormat, args);
    LoguExitOnFailure(hr, "Failed to format error message: \"%ls\"", sczFormat);

    hr = LogStringLine(REPORT_ERROR, "Error 0x%x: %ls", hrError, sczMessage);

LExit:
    ReleaseStr(sczFormat);
    ReleaseStr(sczMessage);

    return hr;
}


extern "C" HRESULT DAPI LogErrorIdModule(
    __in HRESULT hrError,
    __in DWORD dwLogId,
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzString1 = NULL,
    __in_z_opt LPCWSTR wzString2 = NULL,
    __in_z_opt LPCWSTR wzString3 = NULL
    )
{
    HRESULT hr = S_OK;
    WCHAR wzError[11];
    WORD cStrings = 1; // guaranteed wzError is in the list

    hr = ::StringCchPrintfW(wzError, countof(wzError), L"0x%08x", hrError);
    LoguExitOnFailure(hr, "failed to format error code: \"0%08x\"", hrError);

    cStrings += wzString1 ? 1 : 0;
    cStrings += wzString2 ? 1 : 0;
    cStrings += wzString3 ? 1 : 0;

    hr = LogIdModule(REPORT_ERROR, dwLogId, hModule, wzError, wzString1, wzString2, wzString3);
    LoguExitOnFailure(hr, "Failed to log id module.");

LExit:
    return hr;
}

extern "C" HRESULT DAPI LogHeader()
{
    HRESULT hr = S_OK;
    WCHAR wzComputerName[MAX_COMPUTERNAME_LENGTH + 1] = { };
    DWORD cchComputerName = countof(wzComputerName);
    LPWSTR sczPath = NULL;
    LPCWSTR wzPath = NULL;
    DWORD dwMajorVersion = 0;
    DWORD dwMinorVersion = 0;
    LPCSTR szLevel = LOGUTIL_UNKNOWN;
    LPWSTR sczCurrentDateTime = NULL;

    //
    // get the interesting data
    //

    hr = PathForCurrentProcess(&sczPath, NULL);
    if (FAILED(hr))
    {
        wzPath = L"";
    }
    else
    {
        wzPath = sczPath;

        hr = FileVersion(wzPath, &dwMajorVersion, &dwMinorVersion);
    }

    if (FAILED(hr))
    {
        dwMajorVersion = 0;
        dwMinorVersion = 0;
    }

    if (!::GetComputerNameW(wzComputerName, &cchComputerName))
    {
        ::SecureZeroMemory(wzComputerName, sizeof(wzComputerName));
    }

    TimeCurrentDateTime(&sczCurrentDateTime, FALSE);

    //
    // write data to the log
    //
    LogStringLine(REPORT_STANDARD, "=== Logging started: %ls ===", sczCurrentDateTime);
    LogStringLine(REPORT_STANDARD, "Executable: %ls v%d.%d.%d.%d", wzPath, dwMajorVersion >> 16, dwMajorVersion & 0xFFFF, dwMinorVersion >> 16, dwMinorVersion & 0xFFFF);
    LogStringLine(REPORT_STANDARD, "Computer  : %ls", wzComputerName);
    switch (LogUtil_rlCurrent)
    {
    case REPORT_WARNING:
        szLevel = LOGUTIL_WARNING;
        break;
    case REPORT_STANDARD:
        szLevel = LOGUTIL_STANDARD;
        break;
    case REPORT_VERBOSE:
        szLevel = LOGUTIL_VERBOSE;
        break;
    case REPORT_DEBUG:
        szLevel = LOGUTIL_DEBUG;
        break;
    case REPORT_NONE:
        szLevel = LOGUTIL_NONE;
        break;
    }
    LogStringLine(REPORT_STANDARD, "--- logging level: %hs ---", szLevel);

    hr = S_OK;

    ReleaseStr(sczCurrentDateTime);
    ReleaseStr(sczPath);

    return hr;
}



static HRESULT LogFooterWork(
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;

    va_list args;
    va_start(args, szFormat);
    hr = LogStringWorkArgs(REPORT_STANDARD, szFormat, args, TRUE);
    va_end(args);

    return hr;
}

extern "C" HRESULT DAPI LogFooter()
{
    HRESULT hr = S_OK;
    LPWSTR sczCurrentDateTime = NULL;
    TimeCurrentDateTime(&sczCurrentDateTime, FALSE);
    hr = LogFooterWork("=== Logging stopped: %ls ===", sczCurrentDateTime);
    ReleaseStr(sczCurrentDateTime);
    return hr;
}

extern "C" HRESULT DAPI LogStringWorkRaw(
    __in_z LPCSTR szLogData
    )
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&LogUtil_csLog);

    hr = LogStringWorkRawUnsynchronized(szLogData);

    ::LeaveCriticalSection(&LogUtil_csLog);

    return hr;
}

//
// private worker functions
//

static HRESULT LogStringWorkRawUnsynchronized(
    __in_z LPCSTR szLogData
    )
{
    Assert(szLogData && *szLogData);

    HRESULT hr = S_OK;
    size_t cchLogData = 0;
    DWORD cbLogData = 0;
    DWORD cbTotal = 0;
    DWORD cbWrote = 0;

    hr = ::StringCchLengthA(szLogData, STRSAFE_MAX_CCH, &cchLogData);
    LoguExitOnRootFailure(hr, "Failed to get length of raw string");

    cbLogData = (DWORD)cchLogData;

    // If the log hasn't been initialized yet, store it in a buffer
    if (INVALID_HANDLE_VALUE == LogUtil_hLog)
    {
        hr = StrAnsiAllocConcat(&LogUtil_sczPreInitBuffer, szLogData, 0);
        LoguExitOnFailure(hr, "Failed to concatenate string to pre-init buffer");

        ExitFunction1(hr = S_OK);
    }

    // write the string
    while (cbTotal < cbLogData)
    {
        if (!::WriteFile(LogUtil_hLog, reinterpret_cast<const BYTE*>(szLogData) + cbTotal, cbLogData - cbTotal, &cbWrote, NULL))
        {
            LoguExitOnLastError(hr, "Failed to write output to log: %ls - %hs", LogUtil_sczLogPath, szLogData);
        }

        cbTotal += cbWrote;
    }

LExit:
    return hr;
}

static HRESULT LogIdWork(
    __in REPORT_LEVEL rl,
    __in_opt HMODULE hModule,
    __in DWORD dwLogId,
    __in va_list args,
    __in BOOL fLOGUTIL_NEWLINE
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwz = NULL;
    DWORD cch = 0;

    // get the string for the id
#pragma prefast(push)
#pragma prefast(disable:25028)
#pragma prefast(disable:25068)
    cch = ::FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_HMODULE,
                           static_cast<LPCVOID>(hModule), dwLogId, 0, reinterpret_cast<LPWSTR>(&pwz), 0, &args);
#pragma prefast(pop)

    if (0 == cch)
    {
        LoguExitOnLastError(hr, "failed to log id: %d", dwLogId);
    }

    if (2 <= cch && L'\r' == pwz[cch-2] && L'\n' == pwz[cch-1])
    {
        pwz[cch-2] = L'\0'; // remove newline from message table
    }

    LogStringWork(rl, dwLogId, pwz, fLOGUTIL_NEWLINE);

LExit:
    if (pwz)
    {
        ::LocalFree(pwz);
    }

    return hr;
}


static HRESULT LogStringWorkArgs(
    __in REPORT_LEVEL rl,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args,
    __in BOOL fLOGUTIL_NEWLINE
    )
{
    Assert(szFormat && *szFormat);

    HRESULT hr = S_OK;
    LPWSTR sczFormat = NULL;
    LPWSTR sczMessage = NULL;

    hr = StrAllocStringAnsi(&sczFormat, szFormat, 0, CP_ACP);
    LoguExitOnFailure(hr, "Failed to convert format string to wide character string");

    // format the string as a unicode string
    hr = StrAllocFormattedArgs(&sczMessage, sczFormat, args);
    LoguExitOnFailure(hr, "Failed to format message: \"%ls\"", sczFormat);

    hr = LogStringWork(rl, 0, sczMessage, fLOGUTIL_NEWLINE);
    LoguExitOnFailure(hr, "Failed to write formatted string to log:%ls", sczMessage);

LExit:
    ReleaseStr(sczFormat);
    ReleaseStr(sczMessage);

    return hr;
}


static HRESULT LogStringWork(
    __in REPORT_LEVEL rl,
    __in DWORD dwLogId,
    __in_z LPCWSTR sczString,
    __in BOOL fLOGUTIL_NEWLINE
    )
{
    Assert(sczString && *sczString);

    HRESULT hr = S_OK;
    BOOL fEnteredCriticalSection = FALSE;
    LPWSTR scz = NULL;
    LPCWSTR wzLogData = NULL;
    LPSTR sczMultiByte = NULL;
    BOOL fIsError = FALSE;
    BOOL fIsWarning = FALSE;

    // If logging is disabled, just bail.
    if (LogUtil_fDisabled)
    {
        ExitFunction();
    }

    ::EnterCriticalSection(&LogUtil_csLog);
    fEnteredCriticalSection = TRUE;

    if (fLOGUTIL_NEWLINE)
    {
        // get the process and thread id.
        DWORD dwProcessId = ::GetCurrentProcessId();
        DWORD dwThreadId = ::GetCurrentThreadId();

        // get the time relative to GMT.
        SYSTEMTIME st = { };
        ::GetLocalTime(&st);

        DWORD dwId = dwLogId & 0xFFFFFFF;
        DWORD dwType = dwLogId & 0xF0000000;
        fIsError = (0xE0000000 == dwType || REPORT_ERROR == rl);
        fIsWarning = (0xA0000000 == dwType || REPORT_WARNING == rl);
        LPSTR szType = fIsError ? "e" : fIsWarning ? "w" : "i";

        // add line prefix and trailing newline
        hr = StrAllocFormatted(&scz, L"%ls[%04X:%04X][%04hu-%02hu-%02huT%02hu:%02hu:%02hu]%hs%03d:%ls %ls%ls", LogUtil_sczSpecialBeginLine ? LogUtil_sczSpecialBeginLine : L"",
            dwProcessId, dwThreadId, st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, szType, dwId,
            LogUtil_sczSpecialAfterTimeStamp ? LogUtil_sczSpecialAfterTimeStamp : L"", sczString, LogUtil_sczSpecialEndLine ? LogUtil_sczSpecialEndLine : L"\r\n");
        LoguExitOnFailure(hr, "Failed to format line prefix.");
    }

    wzLogData = scz ? scz : sczString;

    // Convert to UTF-8 before writing out to the log file
    hr = StrAnsiAllocString(&sczMultiByte, wzLogData, 0, CP_UTF8);
    LoguExitOnFailure(hr, "Failed to convert log string to UTF-8");

    if (s_vpfLogStringWorkRaw)
    {
        hr = s_vpfLogStringWorkRaw(sczMultiByte, s_vpvLogStringWorkRawContext);
        LoguExitOnFailure(hr, "Failed to write string to log using redirected function: %ls", sczString);
    }
    else
    {
        if (LogUtil_hStdOut != INVALID_HANDLE_VALUE)
        {
            LogStringToConsole(fIsError, fIsWarning, sczMultiByte);
        }

        hr = LogStringWorkRaw(sczMultiByte);
        LoguExitOnFailure(hr, "Failed to write string to log using default function: %ls", sczString);
    }

LExit:
    if (fEnteredCriticalSection)
    {
        ::LeaveCriticalSection(&LogUtil_csLog);
    }

    ReleaseStr(scz);
    ReleaseStr(sczMultiByte);

    return hr;
}

static void LogStringToConsole(
    __in BOOL fIsError,
    __in BOOL fIsWarning,
    __in_z LPCSTR sczMultiByte
    )
{
    DWORD cbLogData = lstrlenA(sczMultiByte);
    DWORD cbTotal = 0;
    HANDLE hStd = LogUtil_hStdOut;

    if (fIsError || fIsWarning)
    {
        if (LogUtil_hStdErr != INVALID_HANDLE_VALUE)
        {
            hStd = LogUtil_hStdErr;
        }
        ::SetConsoleTextAttribute(hStd, fIsError ? FOREGROUND_RED : FOREGROUND_RED | FOREGROUND_GREEN);
    }
    while (cbTotal < cbLogData)
    {
        DWORD cbWrote = 0;

        if (!::WriteFile(hStd, sczMultiByte + cbTotal, cbLogData - cbTotal, &cbWrote, NULL))
        {
            break;
        }

        cbTotal += cbWrote;
    }
    if (fIsError || fIsWarning)
    {
        ::SetConsoleTextAttribute(hStd, FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE);
    }
}
