// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT ProgressCallback(BOOL fBeginFile, LPCWSTR wzFileId, LPVOID pvContext);


HRESULT ExtractCabCommand(
    __in int argc,
    __in LPWSTR argv[]
)
{
    HRESULT hr = E_INVALIDARG;
    LPCWSTR wzCabPath = NULL;
    LPCWSTR wzOutputFolder = NULL;

    if (argc < 2)
    {
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Must specify: cabPath outputFolder");
    }

    wzCabPath = argv[0];
    wzOutputFolder = argv[1];

    hr = CabInitialize(FALSE);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to initialize cabinet: %ls", wzCabPath);

    hr = CabExtract(wzCabPath, L"*", wzOutputFolder, ProgressCallback, NULL, 0);
    ExitOnFailure(hr, "failed to compress files into cabinet: %ls", wzCabPath);

LExit:
    CabUninitialize();

    return hr;
}


static HRESULT ProgressCallback(
    __in BOOL fBeginFile,
    __in LPCWSTR wzFileId,
    __in LPVOID /*pvContext*/
)
{
    if (fBeginFile)
    {
        ConsoleWriteLine(CONSOLE_COLOR_NORMAL, "%ls", wzFileId);
    }

    return S_OK;
}
