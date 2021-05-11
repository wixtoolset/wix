// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static INT_PTR __stdcall EnumCallback(FDINOTIFICATIONTYPE fdint, PFDINOTIFICATION pfdin);


HRESULT EnumCabCommand(
    __in int argc,
    __in LPWSTR argv[]
)
{
    HRESULT hr = E_INVALIDARG;
    LPCWSTR wzCabPath = NULL;

    if (argc < 1)
    {
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Must specify: cabPath outputFolder");
    }

    wzCabPath = argv[0];

    hr = CabInitialize(FALSE);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to initialize cabinet: %ls", wzCabPath);

    hr = CabEnumerate(wzCabPath, L"*", EnumCallback, 0);
    ExitOnFailure(hr, "failed to compress files into cabinet: %ls", wzCabPath);

LExit:
    CabUninitialize();

    return hr;
}


static INT_PTR __stdcall EnumCallback(
    __in FDINOTIFICATIONTYPE fdint,
    __in PFDINOTIFICATION pfdin
)
{
    if (fdint == fdintCOPY_FILE)
    {
        ConsoleWriteLine(CONSOLE_COLOR_NORMAL, "%s\t%d\t%u\t%u", pfdin->psz1, pfdin->cb, pfdin->date, pfdin->time);
    }

    return 0;
}
