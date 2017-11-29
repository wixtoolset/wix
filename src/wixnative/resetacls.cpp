// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT ResetAclsCommand(int argc, LPWSTR argv[])
{
    Unused(argc);
    Unused(argv);

    HRESULT hr = S_OK;
    ACL* pacl = NULL;
    DWORD cbAcl = sizeof(ACL);
    LPWSTR sczFilePath = NULL;

    // create an empty (not NULL!) ACL to use on all the files
    pacl = static_cast<ACL*>(MemAlloc(cbAcl, FALSE));
    ConsoleExitOnNull(pacl, hr, E_OUTOFMEMORY, CONSOLE_COLOR_RED, "failed to allocate ACL");

#pragma prefast(push)
#pragma prefast(disable:25029)
    if (!::InitializeAcl(pacl, cbAcl, ACL_REVISION))
#pragma prefast(op)
    {
        ConsoleExitOnLastError(hr, CONSOLE_COLOR_RED, "failed to initialize ACL");
    }

    // Reset the existing security permissions on each provided file.
    for (;;)
    {
        hr = ConsoleReadW(&sczFilePath);
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to read file path from stdin");

        if (!*sczFilePath)
        {
            break;
        }

        hr = ::SetNamedSecurityInfoW(sczFilePath, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION | UNPROTECTED_DACL_SECURITY_INFORMATION, NULL, NULL, pacl, NULL);
        if (ERROR_FILE_NOT_FOUND != hr && ERROR_PATH_NOT_FOUND != hr)
        {
            ConsoleExitOnFailure(hr = HRESULT_FROM_WIN32(hr), CONSOLE_COLOR_RED, "failed to set security descriptor for file: %ls", sczFilePath);
        }
    }

    AssertSz(::IsValidAcl(pacl), "ResetAcls() - created invalid ACL");

LExit:
    ReleaseStr(sczFilePath);
    ReleaseMem(pacl);
    return hr;
}
