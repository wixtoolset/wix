// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define ProcExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_PROCUTIL, p, x, e, s, __VA_ARGS__)
#define ProcExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_PROCUTIL, p, x, s, __VA_ARGS__)
#define ProcExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_PROCUTIL, p, x, e, s, __VA_ARGS__)
#define ProcExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_PROCUTIL, p, x, s, __VA_ARGS__)
#define ProcExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_PROCUTIL, e, x, s, __VA_ARGS__)
#define ProcExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_PROCUTIL, g, x, s, __VA_ARGS__)

static HRESULT GetActiveSessionUserToken(
    __out HANDLE *phToken
    );


/********************************************************************
 ProcExecuteAsInteractiveUser() - runs process as currently logged in
                                  user.
*******************************************************************/
extern "C" HRESULT DAPI ProcExecuteAsInteractiveUser(
    __in_z LPCWSTR wzExecutablePath,
    __in_z LPCWSTR wzCommandLine,
    __out HANDLE *phProcess
    )
{
    HRESULT hr = S_OK;
    HANDLE hToken = NULL;
    LPVOID pEnvironment = NULL;
    LPWSTR sczFullCommandLine = NULL;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };

    hr = GetActiveSessionUserToken(&hToken);
    ProcExitOnFailure(hr, "Failed to get active session user token.");

    if (!::CreateEnvironmentBlock(&pEnvironment, hToken, FALSE))
    {
        ProcExitWithLastError(hr, "Failed to create environment block for UI process.");
    }

    hr = StrAllocFormatted(&sczFullCommandLine, L"\"%ls\" %ls", wzExecutablePath, wzCommandLine);
    ProcExitOnFailure(hr, "Failed to allocate full command-line.");

    si.cb = sizeof(si);
    if (!::CreateProcessAsUserW(hToken, wzExecutablePath, sczFullCommandLine, NULL, NULL, FALSE, CREATE_UNICODE_ENVIRONMENT, pEnvironment, NULL, &si, &pi))
    {
        ProcExitWithLastError(hr, "Failed to create UI process: %ls", sczFullCommandLine);
    }

    *phProcess = pi.hProcess;
    pi.hProcess = NULL;

LExit:
    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);
    ReleaseStr(sczFullCommandLine);

    if (pEnvironment)
    {
        ::DestroyEnvironmentBlock(pEnvironment);
    }

    ReleaseHandle(hToken);

    return hr;
}


static HRESULT GetActiveSessionUserToken(
    __out HANDLE *phToken
    )
{
    HRESULT hr = S_OK;
    PWTS_SESSION_INFO pSessionInfo = NULL;
    DWORD cSessions = 0;
    DWORD dwSessionId = 0;
    BOOL fSessionFound = FALSE;
    HANDLE hToken = NULL;

    // Loop through the sessions looking for the active one.
    if (!::WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, &pSessionInfo, &cSessions))
    {
        ProcExitWithLastError(hr, "Failed to enumerate sessions.");
    }

    for (DWORD i = 0; i < cSessions; ++i)
    {
        if (WTSActive == pSessionInfo[i].State)
        {
            dwSessionId = pSessionInfo[i].SessionId;
            fSessionFound = TRUE;

            break;
        }
    }

    if (!fSessionFound)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    // Get the user token from the active session.
    if (!::WTSQueryUserToken(dwSessionId, &hToken))
    {
        ProcExitWithLastError(hr, "Failed to get active session user token.");
    }

    *phToken = hToken;
    hToken = NULL;

LExit:
    ReleaseHandle(hToken);

    if (pSessionInfo)
    {
        ::WTSFreeMemory(pSessionInfo);
    }

    return hr;
}
