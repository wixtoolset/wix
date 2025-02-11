// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

int __cdecl wmain(
    __in int argc,
    __in LPWSTR argv[]
    )
{
    DWORD er = ERROR_SUCCESS;
    HRESULT hr = S_OK;
    LPCWSTR wzDestinationFile = argc > 1 ? argv[1] : NULL;
    LPCWSTR wzGoodFile = argc > 2 ? argv[2] : NULL;
    LPCWSTR wzBackupFile = argc > 3 ? argv[3] : NULL;

    if (!wzDestinationFile || !*wzDestinationFile || !wzGoodFile || !*wzGoodFile)
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Invalid args");
    }

    if (wzBackupFile && *wzBackupFile && !::CopyFileW(wzDestinationFile, wzBackupFile, FALSE))
    {
        er = ::GetLastError();
        if (ERROR_PATH_NOT_FOUND != er && ERROR_FILE_NOT_FOUND != er)
        {
            ExitOnWin32Error(er, hr, "Failed to copy to backup file");
        }
    }

    if (!::CopyFileW(wzGoodFile, wzDestinationFile, FALSE))
    {
        ExitWithLastError(hr, "Failed to copy in good file");
    }

LExit:
    return FAILED(hr) ? (int)hr : (int)0;
}
