// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

int __cdecl wmain(int argc, LPWSTR argv[])
{
    HRESULT hr = E_INVALIDARG;

    ConsoleInitialize();

    if (argc != 3)
    {
        ConsoleWriteError(hr, CONSOLE_COLOR_RED, "Usage: Example.TestEngine.exe Bundle.exe BA.dll");
    }
    else
    {
        hr = RunShutdownEngine(argv[1], argv[2]);
    }

    ConsoleUninitialize();
    return hr;
}
