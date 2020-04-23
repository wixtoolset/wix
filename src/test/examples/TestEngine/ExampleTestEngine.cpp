// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

int __cdecl wmain(int argc, LPWSTR argv[])
{
    HRESULT hr = E_INVALIDARG;
    BOOL fShowUsage = FALSE;

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
    else
    {
        fShowUsage = TRUE;
    }

    if (fShowUsage)
    {
        ConsoleWriteError(hr, CONSOLE_COLOR_RED, "Usage: {reload|shutdown} Example.TestEngine.exe Bundle.exe BA.dll");
    }

    ConsoleUninitialize();
    return hr;
}
