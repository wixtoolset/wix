// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// Exit macros
#define AppExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_APPUTIL, p, x, e, s, __VA_ARGS__)
#define AppExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_APPUTIL, p, x, s, __VA_ARGS__)
#define AppExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_APPUTIL, p, x, e, s, __VA_ARGS__)
#define AppExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_APPUTIL, p, x, s, __VA_ARGS__)
#define AppExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_APPUTIL, e, x, s, __VA_ARGS__)
#define AppExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_APPUTIL, g, x, s, __VA_ARGS__)

const DWORD PRIVATE_LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
typedef BOOL(WINAPI *LPFN_SETDEFAULTDLLDIRECTORIES)(DWORD);
typedef BOOL(WINAPI *LPFN_SETDLLDIRECTORYW)(LPCWSTR);

/********************************************************************
EscapeCommandLineArgument - encodes wzArgument such that
    ::CommandLineToArgv() will parse it back unaltered. If no escaping
    was required, *psczEscaped is NULL.

********************************************************************/
static HRESULT EscapeCommandLineArgument(
    __in_z LPCWSTR wzArgument,
    __out_z LPWSTR* psczEscaped
    );

DAPI_(void) AppFreeCommandLineArgs(
    __in LPWSTR* argv
    )
{
    // The "ignored" hack in AppParseCommandLine requires an adjustment.
    LPWSTR* argvOriginal = argv - 1;
    ::LocalFree(argvOriginal);
}

/********************************************************************
AppInitialize - initializes the standard safety precautions for an
                installation application.

********************************************************************/
DAPI_(void) AppInitialize(
    __in_ecount(cSafelyLoadSystemDlls) LPCWSTR rgsczSafelyLoadSystemDlls[],
    __in DWORD cSafelyLoadSystemDlls
    )
{
    HRESULT hr = S_OK;
    HMODULE hIgnored = NULL;
    BOOL fSetDefaultDllDirectories = FALSE;

    ::HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);

    // Best effort call to initialize default DLL directories to system only.
    HMODULE hKernel32 = ::GetModuleHandleW(L"kernel32");
    Assert(hKernel32);
    LPFN_SETDEFAULTDLLDIRECTORIES pfnSetDefaultDllDirectories = (LPFN_SETDEFAULTDLLDIRECTORIES)::GetProcAddress(hKernel32, "SetDefaultDllDirectories");
    if (pfnSetDefaultDllDirectories)
    {
        if (pfnSetDefaultDllDirectories(PRIVATE_LOAD_LIBRARY_SEARCH_SYSTEM32))
        {
            fSetDefaultDllDirectories = TRUE;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            TraceError(hr, "Failed to call SetDefaultDllDirectories.");
        }
    }

    // Only need to safely load if the default DLL directories was not
    // able to be set.
    if (!fSetDefaultDllDirectories)
    {
        // Remove current working directory from search order.
        LPFN_SETDLLDIRECTORYW pfnSetDllDirectory = (LPFN_SETDLLDIRECTORYW)::GetProcAddress(hKernel32, "SetDllDirectoryW");
        if (!pfnSetDllDirectory || !pfnSetDllDirectory(L""))
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            TraceError(hr, "Failed to call SetDllDirectory.");
        }

        for (DWORD i = 0; i < cSafelyLoadSystemDlls; ++i)
        {
            hr = LoadSystemLibrary(rgsczSafelyLoadSystemDlls[i], &hIgnored);
            if (FAILED(hr))
            {
                TraceError(hr, "Failed to safety load: %ls", rgsczSafelyLoadSystemDlls[i]);
            }
        }
    }
}

DAPI_(void) AppInitializeUnsafe()
{
    ::HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);
}

DAPI_(HRESULT) AppParseCommandLine(
    __in LPCWSTR wzCommandLine,
    __in int* pArgc,
    __in LPWSTR** pArgv
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCommandLine = NULL;
    LPWSTR* argv = NULL;
    int argc = 0;

    // CommandLineToArgvW tries to treat the first argument as the path to the process,
    // which fails pretty miserably if your first argument is something like
    // FOO="C:\Program Files\My Company". So give it something harmless to play with.
    hr = StrAllocConcat(&sczCommandLine, L"ignored ", 0);
    AppExitOnFailure(hr, "Failed to initialize command line.");

    hr = StrAllocConcat(&sczCommandLine, wzCommandLine, 0);
    AppExitOnFailure(hr, "Failed to copy command line.");

    argv = ::CommandLineToArgvW(sczCommandLine, &argc);
    AppExitOnNullWithLastError(argv, hr, "Failed to parse command line.");

    // Skip "ignored" argument/hack.
    *pArgv = argv + 1;
    *pArgc = argc - 1;

LExit:
    ReleaseStr(sczCommandLine);

    return hr;
}

DAPI_(HRESULT) AppAppendCommandLineArgument(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in_z LPCWSTR wzArgument
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczQuotedArg = NULL;

    hr = EscapeCommandLineArgument(wzArgument, &sczQuotedArg);
    AppExitOnFailure(hr, "Failed to escape command line argument.");

    // If there is already data in the command line,
    // append a space before appending the argument.
    if (*psczCommandLine && **psczCommandLine)
    {
        hr = StrAllocConcatSecure(psczCommandLine, L" ", 0);
        AppExitOnFailure(hr, "Failed to append space to command line with existing data.");
    }

    hr = StrAllocConcatSecure(psczCommandLine, sczQuotedArg ? sczQuotedArg : wzArgument, 0);
    AppExitOnFailure(hr, "Failed to copy command line argument.");

LExit:
    ReleaseStr(sczQuotedArg);

    return hr;
}

DAPIV_(HRESULT) AppAppendCommandLineArgumentFormatted(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in __format_string LPCWSTR wzFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    va_start(args, wzFormat);
    hr = AppAppendCommandLineArgumentFormattedArgs(psczCommandLine, wzFormat, args);
    va_end(args);

    return hr;
}

DAPI_(HRESULT) AppAppendCommandLineArgumentFormattedArgs(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in __format_string LPCWSTR wzFormat,
    __in va_list args
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczQuotedArg = NULL;

    hr = AppEscapeCommandLineArgumentFormattedArgs(&sczQuotedArg, wzFormat, args);
    AppExitOnFailure(hr, "Failed to escape command line argument.");

    // If there is already data in the command line,
    // append a space before appending the argument.
    if (*psczCommandLine && **psczCommandLine)
    {
        hr = StrAllocConcatSecure(psczCommandLine, L" ", 0);
        AppExitOnFailure(hr, "Failed to append space to command line with existing data.");
    }

    hr = StrAllocConcatSecure(psczCommandLine, sczQuotedArg, 0);
    AppExitOnFailure(hr, "Failed to copy command line argument.");

LExit:
    ReleaseStr(sczQuotedArg);

    return hr;
}

DAPIV_(HRESULT) AppEscapeCommandLineArgumentFormatted(
    __deref_inout_z LPWSTR* psczEscapedArgument,
    __in __format_string LPCWSTR wzFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    va_start(args, wzFormat);
    hr = AppEscapeCommandLineArgumentFormattedArgs(psczEscapedArgument, wzFormat, args);
    va_end(args);

    return hr;
}

DAPI_(HRESULT) AppEscapeCommandLineArgumentFormattedArgs(
    __deref_inout_z LPWSTR* psczEscapedArgument,
    __in __format_string LPCWSTR wzFormat,
    __in va_list args
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFormattedArg = NULL;
    LPWSTR sczQuotedArg = NULL;

    hr = StrAllocFormattedArgsSecure(&sczFormattedArg, wzFormat, args);
    AppExitOnFailure(hr, "Failed to format command line argument.");

    hr = EscapeCommandLineArgument(sczFormattedArg, &sczQuotedArg);
    AppExitOnFailure(hr, "Failed to escape command line argument.");

    if (sczQuotedArg)
    {
        *psczEscapedArgument = sczQuotedArg;
        sczQuotedArg = NULL;
    }
    else
    {
        *psczEscapedArgument = sczFormattedArg;
        sczFormattedArg = NULL;
    }

LExit:
    ReleaseStr(sczFormattedArg);
    ReleaseStr(sczQuotedArg);

    return hr;
}

static HRESULT EscapeCommandLineArgument(
    __in_z LPCWSTR wzArgument,
    __out_z LPWSTR* psczEscaped
    )
{
    HRESULT hr = S_OK;
    BOOL fRequiresQuoting = FALSE;
    SIZE_T cMaxEscapedSize = 0;

    *psczEscaped = NULL;

    // Loop through the argument determining if it needs to be quoted and what the maximum
    // size would be if there are escape characters required.
    for (LPCWSTR pwz = wzArgument; *pwz; ++pwz)
    {
        // Arguments with whitespace need quoting.
        if (L' ' == *pwz || L'\t' == *pwz || L'\n' == *pwz || L'\v' == *pwz)
        {
            fRequiresQuoting = TRUE;
        }
        else if (L'"' == *pwz) // quotes need quoting and sometimes escaping.
        {
            fRequiresQuoting = TRUE;
            ++cMaxEscapedSize;
        }
        else if (L'\\' == *pwz) // some backslashes need escaping, so we'll count them all to make sure there is room.
        {
            ++cMaxEscapedSize;
        }

        ++cMaxEscapedSize;
    }

    // If we found anything in the argument that requires our argument to be quoted
    if (fRequiresQuoting)
    {
        hr = StrAlloc(psczEscaped, cMaxEscapedSize + 3); // plus three for the start and end quote plus null terminator.
        AppExitOnFailure(hr, "Failed to allocate argument to be quoted.");

        LPCWSTR pwz = wzArgument;
        LPWSTR pwzQuoted = *psczEscaped;

        *pwzQuoted = L'"';
        ++pwzQuoted;
        while (*pwz)
        {
            DWORD dwBackslashes = 0;
            while (L'\\' == *pwz)
            {
                ++dwBackslashes;
                ++pwz;
            }

            // Escape all backslashes at the end of the string.
            if (!*pwz)
            {
                dwBackslashes *= 2;
            }
            else if (L'"' == *pwz) // escape all backslashes before the quote and escape the quote itself.
            {
                dwBackslashes = dwBackslashes * 2 + 1;
            }
            // the backslashes don't have to be escaped.

            // Add the appropriate number of backslashes
            for (DWORD i = 0; i < dwBackslashes; ++i)
            {
                *pwzQuoted = L'\\';
                ++pwzQuoted;
            }

            // If there is a character, add it after all the escaped backslashes
            if (*pwz)
            {
                *pwzQuoted = *pwz;
                ++pwz;
                ++pwzQuoted;
            }
        }

        *pwzQuoted = L'"';
        ++pwzQuoted;
        *pwzQuoted = L'\0'; // ensure the arg is null terminated.
    }

LExit:
    return hr;
}
