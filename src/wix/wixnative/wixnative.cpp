// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

int __cdecl wmain(int argc, LPWSTR argv[])
{
    HRESULT hr = E_INVALIDARG;

    ConsoleInitialize();

    if (argc < 2)
    {
        ConsoleWriteError(hr, CONSOLE_COLOR_RED, "Must specify a command");
    }
    else if (CSTR_EQUAL == ::CompareString(LOCALE_INVARIANT, NORM_IGNORECASE, argv[1], -1, L"smartcab", -1))
    {
        hr = SmartCabCommand(argc - 2, argv + 2);
    }
    else if (CSTR_EQUAL == ::CompareString(LOCALE_INVARIANT, NORM_IGNORECASE, argv[1], -1, L"extractcab", -1))
    {
        hr = ExtractCabCommand(argc - 2, argv + 2);
    }
    else if (CSTR_EQUAL == ::CompareString(LOCALE_INVARIANT, NORM_IGNORECASE, argv[1], -1, L"enumcab", -1))
    {
        hr = EnumCabCommand(argc - 2, argv + 2);
    }
    else if (CSTR_EQUAL == ::CompareString(LOCALE_INVARIANT, NORM_IGNORECASE, argv[1], -1, L"resetacls", -1))
    {
        hr = ResetAclsCommand(argc - 2, argv + 2);
    }
    else
    {
        ConsoleWriteError(hr, CONSOLE_COLOR_RED, "Unknown command: %ls", argv[1]);
    }

    ConsoleUninitialize();
    return HRESULT_CODE(hr);
}
