// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT WixNativeReadStdinPreamble();


int __cdecl wmain(int argc, LPWSTR argv[])
{
    HRESULT hr = E_INVALIDARG;

    ConsoleInitialize();

    if (argc < 2)
    {
        ConsoleWriteError(hr, CONSOLE_COLOR_RED, "Must specify a command");

        ExitFunction();
    }

    hr = WixNativeReadStdinPreamble();
    ExitOnFailure(hr, "failed to read stdin preamble");

    if (CSTR_EQUAL == ::CompareStringOrdinal(argv[1], -1, L"smartcab", -1, TRUE))
    {
        hr = SmartCabCommand(argc - 2, argv + 2);
    }
    else if (CSTR_EQUAL == ::CompareStringOrdinal(argv[1], -1, L"extractcab", -1, TRUE))
    {
        hr = ExtractCabCommand(argc - 2, argv + 2);
    }
    else if (CSTR_EQUAL == ::CompareStringOrdinal(argv[1], -1, L"enumcab", -1, TRUE))
    {
        hr = EnumCabCommand(argc - 2, argv + 2);
    }
    else if (CSTR_EQUAL == ::CompareStringOrdinal(argv[1], -1, L"certhashes", -1, TRUE))
    {
        hr = CertificateHashesCommand(argc - 2, argv + 2);
    }
    else
    {
        ConsoleWriteError(hr, CONSOLE_COLOR_RED, "Unknown command: %ls", argv[1]);
    }

LExit:
    ConsoleUninitialize();
    return HRESULT_CODE(hr);
}

static HRESULT WixNativeReadStdinPreamble()
{
    HRESULT hr = S_OK;
    LPWSTR sczLine = NULL;
    size_t cchPreamble = 0;

    // Read the first line to determine if a byte-order-mark was prepended to stdin.
    // A byte-order-mark is not normally expected but has been seen in some CI/CD systems.
    // The preable is a single line with ":".
    hr = ConsoleReadW(&sczLine);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to read preamble from stdin");

    hr = ::StringCchLengthW(sczLine, STRSAFE_MAX_CCH, &cchPreamble);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to get length of stdin preamble");

    // Ensure the preamble ends with ":" and ignore anything before that (since it may be a BOM).
    if (!cchPreamble || sczLine[cchPreamble - 1] != L':')
    {
        hr = E_INVALIDDATA;
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "expected ':' as preamble on first line of stdin");
    }

LExit:
    ReleaseStr(sczLine);

    return hr;
}
