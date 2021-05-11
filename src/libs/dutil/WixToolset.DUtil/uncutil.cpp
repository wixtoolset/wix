// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define UncExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_UNCUTIL, x, s, __VA_ARGS__)
#define UncExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_UNCUTIL, x, s, __VA_ARGS__)
#define UncExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_UNCUTIL, x, s, __VA_ARGS__)
#define UncExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_UNCUTIL, x, s, __VA_ARGS__)
#define UncExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_UNCUTIL, x, s, __VA_ARGS__)
#define UncExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_UNCUTIL, x, s, __VA_ARGS__)
#define UncExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_UNCUTIL, p, x, e, s, __VA_ARGS__)
#define UncExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_UNCUTIL, p, x, s, __VA_ARGS__)
#define UncExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_UNCUTIL, p, x, e, s, __VA_ARGS__)
#define UncExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_UNCUTIL, p, x, s, __VA_ARGS__)
#define UncExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_UNCUTIL, e, x, s, __VA_ARGS__)
#define UncExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_UNCUTIL, g, x, s, __VA_ARGS__)

DAPI_(HRESULT) UncConvertFromMountedDrive(
    __inout LPWSTR *psczUNCPath,
    __in LPCWSTR sczMountedDrivePath
    )
{
    HRESULT hr = S_OK;
    DWORD dwLength = 0;
    DWORD er = ERROR_SUCCESS;
    LPWSTR sczDrive = NULL;

    // Only copy drive letter and colon
    hr = StrAllocString(&sczDrive, sczMountedDrivePath, 2);
    UncExitOnFailure(hr, "Failed to copy drive");

    // ERROR_NOT_CONNECTED means it's not a mapped drive
    er = ::WNetGetConnectionW(sczDrive, NULL, &dwLength);
    if (ERROR_MORE_DATA == er)
    {
        er = ERROR_SUCCESS;

        hr = StrAlloc(psczUNCPath, dwLength);
        UncExitOnFailure(hr, "Failed to allocate string to get raw UNC path of length %u", dwLength);

        er = ::WNetGetConnectionW(sczDrive, *psczUNCPath, &dwLength);
        if (ERROR_CONNECTION_UNAVAIL == er)
        {
            // This means the drive is remembered but not currently connected, this can mean the location is accessible via UNC path but not via mounted drive path
            er = ERROR_SUCCESS;
        }
        UncExitOnWin32Error(er, hr, "::WNetGetConnectionW() failed with buffer provided on drive %ls", sczDrive);

        // Skip drive letter and colon
        hr = StrAllocConcat(psczUNCPath, sczMountedDrivePath + 2, 0);
        UncExitOnFailure(hr, "Failed to copy rest of database path");
    }
    else
    {
        if (ERROR_SUCCESS == er)
        {
            er = ERROR_NO_DATA;
        }

        UncExitOnWin32Error(er, hr, "::WNetGetConnectionW() failed on drive %ls", sczDrive);
    }

LExit:
    ReleaseStr(sczDrive);

    return hr;
}
