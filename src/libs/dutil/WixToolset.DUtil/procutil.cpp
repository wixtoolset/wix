// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define ProcExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_PROCUTIL, x, e, s, __VA_ARGS__)
#define ProcExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_PROCUTIL, p, x, e, s, __VA_ARGS__)
#define ProcExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_PROCUTIL, p, x, s, __VA_ARGS__)
#define ProcExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_PROCUTIL, p, x, e, s, __VA_ARGS__)
#define ProcExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_PROCUTIL, p, x, s, __VA_ARGS__)
#define ProcExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_PROCUTIL, e, x, s, __VA_ARGS__)
#define ProcExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_PROCUTIL, g, x, s, __VA_ARGS__)
#define ProcExitOnWaitObjectFailure(x, b, s, ...) ExitOnWaitObjectFailureSource(DUTIL_SOURCE_PROCUTIL, x, b, s, __VA_ARGS__)


// private functions
static HRESULT CreatePipes(
    __out HANDLE *phOutRead,
    __out HANDLE *phOutWrite,
    __out HANDLE *phErrWrite,
    __out HANDLE *phInRead,
    __out HANDLE *phInWrite
    );

static BOOL CALLBACK CloseWindowEnumCallback(
    __in HWND hWnd,
    __in LPARAM lParam
    );


extern "C" HRESULT DAPI ProcElevated(
    __in HANDLE hProcess,
    __out BOOL* pfElevated
    )
{
    HRESULT hr = S_OK;
    HANDLE hToken = NULL;
    TOKEN_ELEVATION tokenElevated = { };
    DWORD cbToken = 0;

    if (!::OpenProcessToken(hProcess, TOKEN_QUERY, &hToken))
    {
        ProcExitWithLastError(hr, "Failed to open process token.");
    }

    if (::GetTokenInformation(hToken, TokenElevation, &tokenElevated, sizeof(TOKEN_ELEVATION), &cbToken))
    {
        *pfElevated = (0 != tokenElevated.TokenIsElevated);
    }
    else
    {
        DWORD er = ::GetLastError();
        hr = HRESULT_FROM_WIN32(er);

        // If it's invalid argument, this means the OS doesn't support TokenElevation, so we're not elevated.
        if (E_INVALIDARG == hr)
        {
            *pfElevated = FALSE;
            hr = S_OK;
        }
        else
        {
            ProcExitOnRootFailure(hr, "Failed to get elevation token from process.");
        }
    }

LExit:
    ReleaseHandle(hToken);

    return hr;
}

extern "C" HRESULT DAPI ProcSystem(
    __in HANDLE hProcess,
    __out BOOL* pfSystem
    )
{
    HRESULT hr = S_OK;
    TOKEN_USER* pTokenUser = NULL;

    hr = ProcGetTokenInformation(hProcess, TokenUser, reinterpret_cast<LPVOID*>(&pTokenUser));
    ProcExitOnFailure(hr, "Failed to get TokenUser from process token.");

    *pfSystem = ::IsWellKnownSid(pTokenUser->User.Sid, WinLocalSystemSid);

LExit:
    ReleaseMem(pTokenUser);

    return hr;
}

extern "C" HRESULT DAPI ProcGetTokenInformation(
    __in HANDLE hProcess,
    __in TOKEN_INFORMATION_CLASS tokenInformationClass,
    __out LPVOID* ppvTokenInformation
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HANDLE hToken = NULL;
    LPVOID pvTokenInformation = NULL;
    DWORD cbToken = 0;

    if (!::OpenProcessToken(hProcess, TOKEN_QUERY, &hToken))
    {
        ProcExitWithLastError(hr, "Failed to open process token.");
    }

    if (!::GetTokenInformation(hToken, tokenInformationClass, pvTokenInformation, 0, &cbToken))
    {
        er = ::GetLastError();
    }

    if (er != ERROR_INSUFFICIENT_BUFFER)
    {
        ProcExitOnWin32Error(er, hr, "Failed to get information from process token size.");
    }

    pvTokenInformation = MemAlloc(cbToken, TRUE);
    ProcExitOnNull(pvTokenInformation, hr, E_OUTOFMEMORY, "Failed to allocate token information.");

    if (!::GetTokenInformation(hToken, tokenInformationClass, pvTokenInformation, cbToken, &cbToken))
    {
        ProcExitWithLastError(hr, "Failed to get information from process token.");
    }

    *ppvTokenInformation = pvTokenInformation;
    pvTokenInformation = NULL;

LExit:
    ReleaseMem(pvTokenInformation);
    ReleaseHandle(hToken);

    return hr;
}

extern "C" HRESULT DAPI ProcHasPrivilege(
    __in HANDLE hProcess,
    __in LPCWSTR wzPrivilegeName,
    __out BOOL* pfHasPrivilege
    )
{
    HRESULT hr = S_OK;
    TOKEN_PRIVILEGES* pTokenPrivileges = NULL;
    LUID luidPrivilege = { };

    *pfHasPrivilege = FALSE;

    if (!::LookupPrivilegeValueW(NULL, wzPrivilegeName, &luidPrivilege))
    {
        ProcExitWithLastError(hr, "Failed to get privilege LUID: %ls", wzPrivilegeName);
    }

    hr = ProcGetTokenInformation(hProcess, TokenPrivileges, reinterpret_cast<LPVOID*>(&pTokenPrivileges));
    ProcExitOnFailure(hr, "Failed to get token privilege information.");

    for (DWORD i = 0; i < pTokenPrivileges->PrivilegeCount; ++i)
    {
        LUID* pTokenLuid = &pTokenPrivileges->Privileges[i].Luid;

        if (luidPrivilege.LowPart == pTokenLuid->LowPart && luidPrivilege.HighPart == pTokenLuid->HighPart)
        {
            *pfHasPrivilege = TRUE;
            break;
        }
    }

LExit:
    ReleaseMem(pTokenPrivileges);

    return hr;
}

extern "C" HRESULT DAPI ProcEnablePrivilege(
    __in HANDLE hProcess,
    __in LPCWSTR wzPrivilegeName
    )
{
    HRESULT hr = S_OK;
    HANDLE hToken = NULL;
    TOKEN_PRIVILEGES priv = { };

    priv.PrivilegeCount = 1;
    priv.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

    if (!::LookupPrivilegeValueW(NULL, wzPrivilegeName, &priv.Privileges[0].Luid))
    {
        ProcExitWithLastError(hr, "Failed to get privilege LUID: %ls", wzPrivilegeName);
    }

    if (!::OpenProcessToken(hProcess, TOKEN_ADJUST_PRIVILEGES, &hToken))
    {
        ProcExitWithLastError(hr, "Failed to get process token to adjust privileges.");
    }

    if (!::AdjustTokenPrivileges(hToken, FALSE, &priv, sizeof(TOKEN_PRIVILEGES), NULL, 0))
    {
        ProcExitWithLastError(hr, "Failed to adjust token to add privilege: %ls", wzPrivilegeName);
    }

    if (ERROR_NOT_ALL_ASSIGNED == ::GetLastError())
    {
        hr = S_FALSE;
    }

LExit:
    ReleaseHandle(hToken);

    return hr;
}

extern "C" HRESULT DAPI ProcWow64(
    __in HANDLE hProcess,
    __out BOOL* pfWow64
    )
{
    HRESULT hr = S_OK;
    BOOL fIsWow64 = FALSE;

    typedef BOOL(WINAPI* LPFN_ISWOW64PROCESS2)(HANDLE, USHORT *, USHORT *);
    LPFN_ISWOW64PROCESS2 pfnIsWow64Process2 = (LPFN_ISWOW64PROCESS2)::GetProcAddress(::GetModuleHandleW(L"kernel32"), "IsWow64Process2");

    if (pfnIsWow64Process2)
    {
        USHORT usProcessMachine = IMAGE_FILE_MACHINE_UNKNOWN;
        if (!pfnIsWow64Process2(hProcess, &usProcessMachine, nullptr))
        {
            ProcExitWithLastError(hr, "Failed to check WOW64 process - IsWow64Process2.");
        }

        if (usProcessMachine != IMAGE_FILE_MACHINE_UNKNOWN)
        {
            fIsWow64 = TRUE;
        }
    }
    else
    {
        typedef BOOL (WINAPI *LPFN_ISWOW64PROCESS)(HANDLE, PBOOL);
        LPFN_ISWOW64PROCESS pfnIsWow64Process = (LPFN_ISWOW64PROCESS)::GetProcAddress(::GetModuleHandleW(L"kernel32"), "IsWow64Process");

        if (pfnIsWow64Process)
        {
            if (!pfnIsWow64Process(hProcess, &fIsWow64))
            {
                ProcExitWithLastError(hr, "Failed to check WOW64 process - IsWow64Process.");
            }
        }
    }

    *pfWow64 = fIsWow64;

LExit:
    return hr;
}

extern "C" HRESULT DAPI ProcNativeMachine(
    __in HANDLE hProcess,
    __out USHORT* pusNativeMachine
    )
{
    // S_FALSE will indicate that the method is not supported.
    HRESULT hr = S_FALSE;

    typedef BOOL(WINAPI* LPFN_ISWOW64PROCESS2)(HANDLE, USHORT *, USHORT *);
    LPFN_ISWOW64PROCESS2 pfnIsWow64Process2 = (LPFN_ISWOW64PROCESS2)::GetProcAddress(::GetModuleHandleW(L"kernel32"), "IsWow64Process2");

    if (pfnIsWow64Process2)
    {
        USHORT usProcessMachineUnused = IMAGE_FILE_MACHINE_UNKNOWN;
        if (!pfnIsWow64Process2(hProcess, &usProcessMachineUnused, pusNativeMachine))
        {
            ExitWithLastError(hr, "Failed to check WOW64 process - IsWow64Process2.");
        }
        hr = S_OK;
    }

LExit:
    return hr;
}

extern "C" HRESULT DAPI ProcDisableWowFileSystemRedirection(
    __in PROC_FILESYSTEMREDIRECTION* pfsr
    )
{
    AssertSz(!pfsr->fDisabled, "File system redirection was already disabled.");
    HRESULT hr = S_OK;

    typedef BOOL (WINAPI *LPFN_Wow64DisableWow64FsRedirection)(PVOID *);
    LPFN_Wow64DisableWow64FsRedirection pfnWow64DisableWow64FsRedirection = (LPFN_Wow64DisableWow64FsRedirection)::GetProcAddress(::GetModuleHandleW(L"kernel32"), "Wow64DisableWow64FsRedirection");

    if (!pfnWow64DisableWow64FsRedirection)
    {
        ExitFunction1(hr = E_NOTIMPL);
    }

    if (!pfnWow64DisableWow64FsRedirection(&pfsr->pvRevertState))
    {
        ProcExitWithLastError(hr, "Failed to disable file system redirection.");
    }

    pfsr->fDisabled = TRUE;

LExit:
    return hr;
}

extern "C" HRESULT DAPI ProcRevertWowFileSystemRedirection(
    __in PROC_FILESYSTEMREDIRECTION* pfsr
    )
{
    HRESULT hr = S_OK;

    if (pfsr->fDisabled)
    {
        typedef BOOL (WINAPI *LPFN_Wow64RevertWow64FsRedirection)(PVOID);
        LPFN_Wow64RevertWow64FsRedirection pfnWow64RevertWow64FsRedirection = (LPFN_Wow64RevertWow64FsRedirection)::GetProcAddress(::GetModuleHandleW(L"kernel32"), "Wow64RevertWow64FsRedirection");

        if (!pfnWow64RevertWow64FsRedirection(pfsr->pvRevertState))
        {
            ProcExitWithLastError(hr, "Failed to revert file system redirection.");
        }

        pfsr->fDisabled = FALSE;
        pfsr->pvRevertState = NULL;
    }

LExit:
    return hr;
}


extern "C" HRESULT DAPI ProcExec(
    __in_z LPCWSTR wzExecutablePath,
    __in_z_opt LPCWSTR wzCommandLine,
    __in int nCmdShow,
    __out HANDLE *phProcess
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFullCommandLine = NULL;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };

    hr = StrAllocFormatted(&sczFullCommandLine, L"\"%ls\" %ls", wzExecutablePath, wzCommandLine ? wzCommandLine : L"");
    ProcExitOnFailure(hr, "Failed to allocate full command-line.");

    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = static_cast<WORD>(nCmdShow);
    if (!::CreateProcessW(wzExecutablePath, sczFullCommandLine, NULL, NULL, FALSE, 0, 0, NULL, &si, &pi))
    {
        ProcExitWithLastError(hr, "Failed to create process: %ls", sczFullCommandLine);
    }

    *phProcess = pi.hProcess;
    pi.hProcess = NULL;

LExit:
    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);
    ReleaseStr(sczFullCommandLine);

    return hr;
}


/********************************************************************
 ProcExecute() - executes a command-line.

*******************************************************************/
extern "C" HRESULT DAPI ProcExecute(
    __in_z_opt LPCWSTR wzApplicationName,
    __in_z LPWSTR wzCommand,
    __out HANDLE *phProcess,
    __out_opt HANDLE *phChildStdIn,
    __out_opt HANDLE *phChildStdOutErr
    )
{
    HRESULT hr = S_OK;

    PROCESS_INFORMATION pi = { };
    STARTUPINFOW si = { };

    HANDLE hOutRead = INVALID_HANDLE_VALUE;
    HANDLE hOutWrite = INVALID_HANDLE_VALUE;
    HANDLE hErrWrite = INVALID_HANDLE_VALUE;
    HANDLE hInRead = INVALID_HANDLE_VALUE;
    HANDLE hInWrite = INVALID_HANDLE_VALUE;

    // Create redirect pipes.
    hr = CreatePipes(&hOutRead, &hOutWrite, &hErrWrite, &hInRead, &hInWrite);
    ProcExitOnFailure(hr, "failed to create output pipes");

    // Set up startup structure.
    si.cb = sizeof(STARTUPINFOW);
    si.dwFlags = STARTF_USESTDHANDLES;
    si.hStdInput = hInRead;
    si.hStdOutput = hOutWrite;
    si.hStdError = hErrWrite;

#pragma prefast(push)
#pragma prefast(disable:25028)
    if (::CreateProcessW(wzApplicationName,
        wzCommand, // command line
        NULL, // security info
        NULL, // thread info
        TRUE, // inherit handles
        ::GetPriorityClass(::GetCurrentProcess()) | CREATE_NO_WINDOW, // creation flags
        NULL, // environment
        NULL, // cur dir
        &si,
        &pi))
#pragma prefast(pop)
    {
        // Close child process output/input handles so child doesn't hang
        // while waiting for input from parent process.
        ::CloseHandle(hOutWrite);
        hOutWrite = INVALID_HANDLE_VALUE;

        ::CloseHandle(hErrWrite);
        hErrWrite = INVALID_HANDLE_VALUE;

        ::CloseHandle(hInRead);
        hInRead = INVALID_HANDLE_VALUE;
    }
    else
    {
        ProcExitWithLastError(hr, "Process failed to execute.");
    }

    *phProcess = pi.hProcess;
    pi.hProcess = 0;

    if (phChildStdIn)
    {
        *phChildStdIn = hInWrite;
        hInWrite = INVALID_HANDLE_VALUE;
    }

    if (phChildStdOutErr)
    {
        *phChildStdOutErr = hOutRead;
        hOutRead = INVALID_HANDLE_VALUE;
    }

LExit:
    if (pi.hThread)
    {
        ::CloseHandle(pi.hThread);
    }

    if (pi.hProcess)
    {
        ::CloseHandle(pi.hProcess);
    }

    ReleaseFileHandle(hOutRead);
    ReleaseFileHandle(hOutWrite);
    ReleaseFileHandle(hErrWrite);
    ReleaseFileHandle(hInRead);
    ReleaseFileHandle(hInWrite);

    return hr;
}


/********************************************************************
 ProcWaitForCompletion() - waits for process to complete and gets return code.

*******************************************************************/
extern "C" HRESULT DAPI ProcWaitForCompletion(
    __in HANDLE hProcess,
    __in DWORD dwTimeout,
    __out_opt DWORD* pdwReturnCode
    )
{
    HRESULT hr = S_OK;
    BOOL fTimedOut = FALSE;

    // Wait for everything to finish.
    hr = AppWaitForSingleObject(hProcess, dwTimeout);
    ProcExitOnWaitObjectFailure(hr, fTimedOut, "Failed to wait for process to complete.");

    if (fTimedOut)
    {
        hr = HRESULT_FROM_WIN32(WAIT_TIMEOUT);
    }
    else if (pdwReturnCode && !::GetExitCodeProcess(hProcess, pdwReturnCode))
    {
        ProcExitWithLastError(hr, "Failed to get process return code.");
    }

LExit:
    return hr;
}

/********************************************************************
 ProcWaitForIds() - waits for multiple processes to complete.

*******************************************************************/
extern "C" HRESULT DAPI ProcWaitForIds(
    __in_ecount(cProcessIds) const DWORD rgdwProcessIds[],
    __in DWORD cProcessIds,
    __in DWORD dwMilliseconds
    )
{
    HRESULT hr = S_OK;
    HANDLE hProcess = NULL;
    HANDLE* rghProcesses = NULL;
    DWORD cProcesses = 0;
    BOOL fTimedOut = FALSE;

    rghProcesses =  static_cast<HANDLE*>(MemAlloc(sizeof(HANDLE) * cProcessIds, TRUE));
    ProcExitOnNull(rgdwProcessIds, hr, E_OUTOFMEMORY, "Failed to allocate array for process ID Handles.");

    for (DWORD i = 0; i < cProcessIds; ++i)
    {
        hProcess = ::OpenProcess(SYNCHRONIZE, FALSE, rgdwProcessIds[i]);
        if (hProcess != NULL)
        {
            rghProcesses[cProcesses++] = hProcess;
        }
    }

    hr = AppWaitForMultipleObjects(cProcesses, rghProcesses, TRUE, dwMilliseconds, NULL);
    ProcExitOnWaitObjectFailure(hr, fTimedOut, "Failed to wait for processes to complete.");

    if (fTimedOut)
    {
        ProcExitWithRootFailure(hr, HRESULT_FROM_WIN32(WAIT_TIMEOUT), "Timed out while waiting for processes to complete.");
    }

LExit:
    if (rghProcesses)
    {
        for (DWORD i = 0; i < cProcesses; ++i)
        {
            if (NULL != rghProcesses[i])
            {
                ::CloseHandle(rghProcesses[i]);
            }
        }
        
        MemFree(rghProcesses);
    }

    return hr;
}

/********************************************************************
 ProcCloseIds() - sends WM_CLOSE messages to all process ids.

*******************************************************************/
extern "C" HRESULT DAPI ProcCloseIds(
    __in_ecount(cProcessIds) const DWORD* pdwProcessIds,
    __in DWORD cProcessIds
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < cProcessIds; ++i)
    {
        if (!::EnumWindows(&CloseWindowEnumCallback, pdwProcessIds[i]))
        {
            ProcExitWithLastError(hr, "Failed to enumerate windows.");
        }
    }

LExit:
    return hr;
}


static HRESULT CreatePipes(
    __out HANDLE *phOutRead,
    __out HANDLE *phOutWrite,
    __out HANDLE *phErrWrite,
    __out HANDLE *phInRead,
    __out HANDLE *phInWrite
    )
{
    HRESULT hr = S_OK;
    SECURITY_ATTRIBUTES sa;
    HANDLE hOutTemp = INVALID_HANDLE_VALUE;
    HANDLE hInTemp = INVALID_HANDLE_VALUE;

    HANDLE hOutRead = INVALID_HANDLE_VALUE;
    HANDLE hOutWrite = INVALID_HANDLE_VALUE;
    HANDLE hErrWrite = INVALID_HANDLE_VALUE;
    HANDLE hInRead = INVALID_HANDLE_VALUE;
    HANDLE hInWrite = INVALID_HANDLE_VALUE;

    // Fill out security structure so we can inherit handles
    ZeroMemory(&sa, sizeof(SECURITY_ATTRIBUTES));
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.bInheritHandle = TRUE;
    sa.lpSecurityDescriptor = NULL;

    // Create pipes
    if (!::CreatePipe(&hOutTemp, &hOutWrite, &sa, 0))
    {
        ProcExitWithLastError(hr, "failed to create output pipe");
    }

    if (!::CreatePipe(&hInRead, &hInTemp, &sa, 0))
    {
        ProcExitWithLastError(hr, "failed to create input pipe");
    }

    // Duplicate output pipe so standard error and standard output write to the same pipe.
    if (!::DuplicateHandle(::GetCurrentProcess(), hOutWrite, ::GetCurrentProcess(), &hErrWrite, 0, TRUE, DUPLICATE_SAME_ACCESS))
    {
        ProcExitWithLastError(hr, "failed to duplicate write handle");
    }

    // We need to create new "output read" and "input write" handles that are non inheritable.  Otherwise CreateProcess will creates handles in 
    // the child process that can't be closed.
    if (!::DuplicateHandle(::GetCurrentProcess(), hOutTemp, ::GetCurrentProcess(), &hOutRead, 0, FALSE, DUPLICATE_SAME_ACCESS))
    {
        ProcExitWithLastError(hr, "failed to duplicate output pipe");
    }

    if (!::DuplicateHandle(::GetCurrentProcess(), hInTemp, ::GetCurrentProcess(), &hInWrite, 0, FALSE, DUPLICATE_SAME_ACCESS))
    {
        ProcExitWithLastError(hr, "failed to duplicate input pipe");
    }

    // now that everything has succeeded, assign to the outputs
    *phOutRead = hOutRead;
    hOutRead = INVALID_HANDLE_VALUE;

    *phOutWrite = hOutWrite;
    hOutWrite = INVALID_HANDLE_VALUE;

    *phErrWrite = hErrWrite;
    hErrWrite = INVALID_HANDLE_VALUE;

    *phInRead = hInRead;
    hInRead = INVALID_HANDLE_VALUE;

    *phInWrite = hInWrite;
    hInWrite = INVALID_HANDLE_VALUE;

LExit:
    ReleaseFileHandle(hOutRead);
    ReleaseFileHandle(hOutWrite);
    ReleaseFileHandle(hErrWrite);
    ReleaseFileHandle(hInRead);
    ReleaseFileHandle(hInWrite);
    ReleaseFileHandle(hOutTemp);
    ReleaseFileHandle(hInTemp);

    return hr;
}


/********************************************************************
 CloseWindowEnumCallback() - outputs trace and log info

*******************************************************************/
static BOOL CALLBACK CloseWindowEnumCallback(
    __in HWND hWnd,
    __in LPARAM lParam
    )
{
    DWORD dwPid = static_cast<DWORD>(lParam);
    DWORD dwProcessId = 0;

    ::GetWindowThreadProcessId(hWnd, &dwProcessId);
    if (dwPid == dwProcessId)
    {
        ::SendMessageW(hWnd, WM_CLOSE, 0, 0);
    }

    return TRUE;
}
