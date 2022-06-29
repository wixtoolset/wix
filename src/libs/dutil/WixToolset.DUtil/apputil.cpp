// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// Exit macros
#define AppExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_APPUTIL, x, e, s, __VA_ARGS__)
#define AppExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_APPUTIL, x, s, __VA_ARGS__)
#define AppExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_APPUTIL, p, x, e, s, __VA_ARGS__)
#define AppExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_APPUTIL, p, x, s, __VA_ARGS__)
#define AppExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_APPUTIL, p, x, e, s, __VA_ARGS__)
#define AppExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_APPUTIL, p, x, s, __VA_ARGS__)
#define AppExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_APPUTIL, e, x, s, __VA_ARGS__)
#define AppExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_APPUTIL, g, x, s, __VA_ARGS__)

typedef BOOL(WINAPI *LPFN_SETDEFAULTDLLDIRECTORIES)(DWORD);
typedef BOOL(WINAPI *LPFN_SETDLLDIRECTORYW)(LPCWSTR);

static BOOL vfInitialized = FALSE;
static LPFN_SETDEFAULTDLLDIRECTORIES vpfnSetDefaultDllDirectories = NULL;
static LPFN_SETDLLDIRECTORYW vpfnSetDllDirectory = NULL;

/********************************************************************
EscapeCommandLineArgument - encodes wzArgument such that
    ::CommandLineToArgv() will parse it back unaltered. If no escaping
    was required, *psczEscaped is NULL.

********************************************************************/
static HRESULT EscapeCommandLineArgument(
    __in_z LPCWSTR wzArgument,
    __out_z LPWSTR* psczEscaped
    );

static void Initialize()
{
    HRESULT hr = S_OK;
    HMODULE hKernel32 = NULL;

    if (vfInitialized)
    {
        ExitFunction();
    }

    hKernel32 = ::GetModuleHandleW(L"kernel32");
    AppExitOnNullWithLastError(hKernel32, hr, "Failed to get module handle for kernel32.");

    vpfnSetDefaultDllDirectories = (LPFN_SETDEFAULTDLLDIRECTORIES)::GetProcAddress(hKernel32, "SetDefaultDllDirectories");
    vpfnSetDllDirectory = (LPFN_SETDLLDIRECTORYW)::GetProcAddress(hKernel32, "SetDllDirectoryW");

    vfInitialized = TRUE;

LExit:
    return;
}

DAPI_(HRESULT) LoadSystemLibrary(
    __in_z LPCWSTR wzModuleName,
    __out HMODULE* phModule
    )
{
    HRESULT hr = S_OK;

    Initialize();

    if (vpfnSetDefaultDllDirectories) // LOAD_LIBRARY_SEARCH_SYSTEM32 was added at same time as SetDefaultDllDirectories.
    {
        *phModule = ::LoadLibraryExW(wzModuleName, NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
        AppExitOnNullWithLastError(*phModule, hr, "Failed to get load library with LOAD_LIBRARY_SEARCH_SYSTEM32.");
    }
    else
    {
        hr = LoadSystemLibraryWithPath(wzModuleName, phModule, NULL);
    }

LExit:
    return hr;
}

DAPI_(HRESULT) LoadSystemLibraryWithPath(
    __in_z LPCWSTR wzModuleName,
    __out HMODULE* phModule,
    __deref_out_z_opt LPWSTR* psczPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDirectory = NULL;
    LPWSTR sczPath = NULL;

    hr = PathGetSystemDirectory(&sczDirectory);
    AppExitOnFailure(hr, "Failed to get the Windows system directory.");

    hr = StrAllocFormatted(&sczPath, L"%ls%ls", sczDirectory, wzModuleName);
    AppExitOnFailure(hr, "Failed to create the fully-qualified path to %ls.", wzModuleName);

    *phModule = ::LoadLibraryExW(sczPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    AppExitOnNullWithLastError(*phModule, hr, "Failed to load the library %ls.", sczPath);

    if (psczPath)
    {
        *psczPath = sczPath;
        sczPath = NULL;
    }

LExit:
    ReleaseStr(sczDirectory);

    return hr;
}

DAPI_(HRESULT) LoadSystemApiSet(
    __in_z LPCWSTR wzApiSet,
    __out HMODULE* phModule
    )
{
    HRESULT hr = S_OK;

    Initialize();

    if (!vpfnSetDefaultDllDirectories)
    {
        // For many API sets, the .dll does not actually exist on disk so there's no point on even trying if SetDefaultDllDirectories is not available.
        // On OS's where API sets are implemented, the loader requires just the API set name with .dll.
        // It is not safe to pass such strings to LoadLibraryEx without LOAD_LIBRARY_SEARCH_SYSTEM32, which isn't available on old OS's.
        AppExitWithRootFailure(hr, E_MODNOTFOUND, "OS doesn't support API sets.");
    }
    else
    {
        hr = LoadSystemLibrary(wzApiSet, phModule);
    }

LExit:
    return hr;
}

DAPI_(void) AppInitialize(
    __in_ecount(cSafelyLoadSystemDlls) LPCWSTR rgsczSafelyLoadSystemDlls[],
    __in DWORD cSafelyLoadSystemDlls
    )
{
    HRESULT hr = S_OK;
    HMODULE hIgnored = NULL;
    BOOL fSetDefaultDllDirectories = FALSE;

    ::HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);

    Initialize();

    // Best effort call to initialize default DLL directories to system only.
    if (vpfnSetDefaultDllDirectories)
    {
        if (vpfnSetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_SYSTEM32))
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
        if (!vpfnSetDllDirectory || !vpfnSetDllDirectory(L""))
        {
            hr = vpfnSetDllDirectory ? HRESULT_FROM_WIN32(::GetLastError()) : HRESULT_FROM_WIN32(ERROR_PROC_NOT_FOUND);
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

DAPI_(HRESULT) AppWaitForSingleObject(
    __in HANDLE hHandle,
    __in DWORD dwMilliseconds
    )
{
    HRESULT hr = S_OK;
    DWORD dwResult = 0;

    dwResult = ::WaitForSingleObject(hHandle, dwMilliseconds);
    if (WAIT_TIMEOUT == dwResult)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(dwResult));
    }
    else if (WAIT_ABANDONED == dwResult)
    {
        AppExitOnWin32Error(dwResult, hr, "Abandoned wait for single object.");
    }
    else if (WAIT_OBJECT_0 != dwResult)
    {
        AssertSz(WAIT_FAILED == dwResult, "Unexpected return code from WaitForSingleObject.");
        AppExitWithLastError(hr, "Failed to wait for single object.");
    }

LExit:
    return hr;
}

DAPI_(HRESULT) AppWaitForMultipleObjects(
    __in DWORD dwCount,
    __in const HANDLE* rghHandles,
    __in BOOL fWaitAll,
    __in DWORD dwMilliseconds,
    __out_opt DWORD* pdwSignaledIndex
    )
{
    HRESULT hr = S_OK;
    DWORD dwResult = 0;
    DWORD dwSignaledIndex = dwCount;

    dwResult = ::WaitForMultipleObjects(dwCount, rghHandles, fWaitAll, dwMilliseconds);
    if (WAIT_TIMEOUT == dwResult)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(dwResult));
    }
    else if (WAIT_ABANDONED_0 <= dwResult && (WAIT_ABANDONED_0 + dwCount) > dwResult)
    {
        dwSignaledIndex = dwResult - WAIT_ABANDONED_0;
        AppExitOnWin32Error(dwResult, hr, "Abandoned wait for multiple objects, index: %u.", dwSignaledIndex);
    }
    else if (WAIT_OBJECT_0 <= dwResult && (WAIT_OBJECT_0 + dwCount) > dwResult)
    {
        dwSignaledIndex = dwResult - WAIT_OBJECT_0;
    }
    else
    {
        AssertSz(WAIT_FAILED == dwResult, "Unexpected return code from WaitForMultipleObjects.");
        AppExitWithLastError(hr, "Failed to wait for multiple objects.");
    }

LExit:
    if (pdwSignaledIndex)
    {
        *pdwSignaledIndex = dwSignaledIndex;
    }

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
