// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

int __cdecl wmain(int argc, LPWSTR argv[])
{
    HRESULT hr = S_OK;
    BOOL fComInitialized = FALSE;
    BOOL fShowUsage = FALSE;

    // initialize COM
    hr = ::CoInitializeEx(NULL, COINIT_MULTITHREADED);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    ConsoleInitialize();

    if (argc != 4)
    {
        fShowUsage = TRUE;
    }
    else if (CSTR_EQUAL == ::CompareString(LOCALE_INVARIANT, NORM_IGNORECASE, argv[1], -1, L"reload", -1))
    {
        hr = RunReloadEngine(argv[2], argv[3]);
    }
    else if (CSTR_EQUAL == ::CompareString(LOCALE_INVARIANT, NORM_IGNORECASE, argv[1], -1, L"shutdown", -1))
    {
        hr = RunShutdownEngine(argv[2], argv[3]);
    }
    else if (CSTR_EQUAL == ::CompareString(LOCALE_INVARIANT, NORM_IGNORECASE, argv[1], -1, L"waitforquit", -1))
    {
        hr = RunWaitForQuitEngine(argv[2], argv[3]);
    }
    else
    {
        fShowUsage = TRUE;
    }

    if (fShowUsage)
    {
        ConsoleWriteError(hr = E_INVALIDARG, CONSOLE_COLOR_RED, "Usage: Example.TestEngine.exe {reload|shutdown|waitforquit} Bundle.exe BA.dll");
    }

    ConsoleUninitialize();

LExit:
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return hr;
}
