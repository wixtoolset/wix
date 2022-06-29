// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define ThrdExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_THRDUTIL, x, s, __VA_ARGS__)
#define ThrdExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_THRDUTIL, x, s, __VA_ARGS__)
#define ThrdExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_THRDUTIL, x, s, __VA_ARGS__)
#define ThrdExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_THRDUTIL, x, s, __VA_ARGS__)
#define ThrdExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_THRDUTIL, x, s, __VA_ARGS__)
#define ThrdExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_THRDUTIL, x, e, s, __VA_ARGS__)
#define ThrdExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_THRDUTIL, x, s, __VA_ARGS__)
#define ThrdExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_THRDUTIL, p, x, e, s, __VA_ARGS__)
#define ThrdExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_THRDUTIL, p, x, s, __VA_ARGS__)
#define ThrdExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_THRDUTIL, p, x, e, s, __VA_ARGS__)
#define ThrdExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_THRDUTIL, p, x, s, __VA_ARGS__)
#define ThrdExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_THRDUTIL, e, x, s, __VA_ARGS__)
#define ThrdExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_THRDUTIL, g, x, s, __VA_ARGS__)
#define ThrdExitOnWaitObjectFailure(x, b, s, ...) ExitOnWaitObjectFailureSource(DUTIL_SOURCE_THRDUTIL, x, b, s, __VA_ARGS__)

DAPI_(HRESULT) ThrdWaitForCompletion(
    __in HANDLE hThread,
    __in DWORD dwTimeout,
    __out_opt DWORD *pdwReturnCode
    )
{
    HRESULT hr = S_OK;
    BOOL fTimedOut = FALSE;

    // Wait for everything to finish.
    hr = AppWaitForSingleObject(hThread, dwTimeout);
    ThrdExitOnWaitObjectFailure(hr, fTimedOut, "Failed to wait for thread to complete.");

    if (fTimedOut)
    {
        hr = HRESULT_FROM_WIN32(WAIT_TIMEOUT);
    }
    else if (pdwReturnCode && !::GetExitCodeThread(hThread, pdwReturnCode))
    {
        ThrdExitWithLastError(hr, "Failed to get thread return code.");
    }

LExit:
    return hr;
}
