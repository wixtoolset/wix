// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

int __cdecl wmain(
    __in int argc,
    __in LPWSTR argv[]
    )
{
    HRESULT hr = S_OK;
    DWORD dwExitCode = 0;
    LPCWSTR wzDestinationFile = argc > 1 ? argv[1] : NULL;
    LPCWSTR wzGoodFile = argc > 2 ? argv[2] : NULL;
    LPCWSTR wzBadFile = argc > 3 ? argv[3] : NULL;

    if (argc != 4)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Invalid args");
    }

    if (!::MoveFileW(wzDestinationFile, wzBadFile))
    {
        ExitWithLastError(hr, "Failed to move bad file");
    }

    if (!::MoveFileW(wzGoodFile, wzDestinationFile))
    {
        ExitWithLastError(hr, "Failed to move good file");
    }

LExit:
    return FAILED(hr) ? (int)hr : (int)dwExitCode;
}
