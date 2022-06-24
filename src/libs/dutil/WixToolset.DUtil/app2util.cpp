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

DAPI_(void) AppFreeCommandLineArgs(
    __in LPWSTR* argv
    )
{
    // The "ignored" hack in AppParseCommandLine requires an adjustment.
    LPWSTR* argvOriginal = argv - 1;
    ::LocalFree(argvOriginal);
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
