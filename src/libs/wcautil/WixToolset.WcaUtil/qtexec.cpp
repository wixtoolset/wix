// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define OUTPUT_BUFFER 1024
#define ONEMINUTE 60000

static HRESULT CreatePipes(
    __out HANDLE *phOutRead,
    __out HANDLE *phOutWrite,
    __out HANDLE *phErrWrite,
    __out HANDLE *phInRead,
    __out HANDLE *phInWrite
    )
{
    Assert(phOutRead);
    Assert(phOutWrite);
    Assert(phErrWrite);
    Assert(phInRead);
    Assert(phInWrite);

    HRESULT hr = S_OK;
    SECURITY_ATTRIBUTES sa;
    WCHAR wzGuid[GUID_STRING_LENGTH];
    LPWSTR szStdInPipeName = NULL;
    LPWSTR szStdOutPipeName = NULL;
    BOOL fRes = TRUE;
    HANDLE hOutTemp = INVALID_HANDLE_VALUE;
    HANDLE hInTemp = INVALID_HANDLE_VALUE;

    HANDLE hOutRead = INVALID_HANDLE_VALUE;
    HANDLE hOutWrite = INVALID_HANDLE_VALUE;
    HANDLE hErrWrite = INVALID_HANDLE_VALUE;
    HANDLE hInRead = INVALID_HANDLE_VALUE;
    HANDLE hInWrite = INVALID_HANDLE_VALUE;

    // Generate unique pipe names
    hr = GuidFixedCreate(wzGuid);
    ExitOnFailure(hr, "Failed to create UUID.");

    hr = StrAllocFormatted(&szStdInPipeName, L"\\\\.\\pipe\\%ls-stdin", wzGuid);
    ExitOnFailure(hr, "Failed to create stdin pipe name.");

    hr = StrAllocFormatted(&szStdOutPipeName, L"\\\\.\\pipe\\%ls-stdout", wzGuid);
    ExitOnFailure(hr, "Failed to create stdout pipe name.");

    // Fill out security structure so we can inherit handles
    ::ZeroMemory(&sa, sizeof(SECURITY_ATTRIBUTES));
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.bInheritHandle = TRUE;
    sa.lpSecurityDescriptor = NULL;

    // Create pipes
    hOutTemp = ::CreateNamedPipeW(szStdOutPipeName, PIPE_ACCESS_INBOUND | FILE_FLAG_OVERLAPPED, PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT, 1, OUTPUT_BUFFER, OUTPUT_BUFFER, NMPWAIT_USE_DEFAULT_WAIT, &sa);
    ExitOnInvalidHandleWithLastError(hOutTemp, hr, "Failed to create named pipe for stdout reader");

    hOutWrite = ::CreateFileW(szStdOutPipeName, FILE_WRITE_DATA | SYNCHRONIZE | FILE_FLAG_OVERLAPPED, 0, &sa, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    ExitOnInvalidHandleWithLastError(hOutWrite, hr, "Failed to open named pipe for stdout writer");

    fRes = ::DuplicateHandle(::GetCurrentProcess(), hOutWrite, ::GetCurrentProcess(), &hErrWrite, 0, FALSE, DUPLICATE_SAME_ACCESS);
    ExitOnNullWithLastError(fRes, hr, "Failed to duplicate named pipe from stdout to stderr");
    ExitOnInvalidHandleWithLastError(hErrWrite, hr, "Failed to duplicate named pipe from stdout to stderr");

    fRes = ::DuplicateHandle(::GetCurrentProcess(), hOutTemp, ::GetCurrentProcess(), &hOutRead, 0, FALSE, DUPLICATE_SAME_ACCESS);
    ExitOnNullWithLastError(fRes, hr, "Failed to duplicate named pipe for stdout reader");
    ExitOnInvalidHandleWithLastError(hOutRead, hr, "Failed to duplicate named pipe for stdout reader");
    ::CloseHandle(hOutTemp);
    hOutTemp = INVALID_HANDLE_VALUE;

    hInTemp = ::CreateNamedPipeW(szStdInPipeName, PIPE_ACCESS_OUTBOUND, PIPE_TYPE_BYTE | PIPE_WAIT, 1, OUTPUT_BUFFER, OUTPUT_BUFFER, NMPWAIT_USE_DEFAULT_WAIT, &sa);
    ExitOnInvalidHandleWithLastError(hInTemp, hr, "Failed to create named pipe for stdin writer");

    hInRead = ::CreateFileW(szStdInPipeName, FILE_READ_DATA, 0, &sa, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    ExitOnInvalidHandleWithLastError(hInRead, hr, "Failed to open named pipe for stdin reader");

    fRes = ::DuplicateHandle(::GetCurrentProcess(), hInTemp, ::GetCurrentProcess(), &hInWrite, 0, FALSE, DUPLICATE_SAME_ACCESS);
    ExitOnNullWithLastError(fRes, hr, "Failed to duplicate named pipe for stdin writer");
    ExitOnInvalidHandleWithLastError(hInWrite, hr, "Failed to duplicate named pipe for stdin writer");
    ::CloseHandle(hInTemp);
    hInTemp = INVALID_HANDLE_VALUE;

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
    ReleaseStr(szStdInPipeName);
    ReleaseStr(szStdOutPipeName);

    return hr;
}

static HRESULT HandleOutput(
    __in BOOL fLogOutput,
    __in HANDLE hProcess,
    __in HANDLE hRead,
    __out_z_opt LPWSTR* psczOutput
    )
{
    BYTE* pBuffer = NULL;
    LPWSTR szLog = NULL;
    LPWSTR szTemp = NULL;
    LPWSTR pEnd = NULL;
    LPWSTR pNext = NULL;
    LPWSTR sczEscaped = NULL;
    LPSTR szWrite = NULL;
    DWORD dwBytes = OUTPUT_BUFFER;
    BOOL bFirst = TRUE;
    BOOL bUnicode = TRUE;
    HRESULT hr = S_OK;
    OVERLAPPED overlapped = { 0 };
    HANDLE rghHandles[2] = { 0 };
    BOOL fRes = TRUE;
    DWORD dwRes = ERROR_SUCCESS;
    BOOL fLast = FALSE;

    ::ZeroMemory(&overlapped, sizeof(overlapped));

    // Get buffer for output
    pBuffer = static_cast<BYTE *>(MemAlloc(OUTPUT_BUFFER, FALSE));
    ExitOnNull(pBuffer, hr, E_OUTOFMEMORY, "Failed to allocate buffer for output.");

    overlapped.hEvent = ::CreateEvent(nullptr, TRUE, FALSE, nullptr);
    ExitOnNullWithLastError((overlapped.hEvent && (overlapped.hEvent != INVALID_HANDLE_VALUE)), hr, "Failed to create event");

    rghHandles[0] = hProcess;
    rghHandles[1] = overlapped.hEvent;

    fRes = ::ConnectNamedPipe(hRead, &overlapped);
    if (!fRes)
    {
        dwRes = ::GetLastError();
        if (dwRes == ERROR_IO_PENDING)
        {
            hr = AppWaitForSingleObject(overlapped.hEvent, INFINITE);
            ExitOnFailure(hr, "Failed to wait for process to connect to stdout");
            fRes = TRUE;
        }
        else if (dwRes == ERROR_PIPE_CONNECTED)
        {
            fRes = TRUE;
        }
        ExitOnNullWithLastError(fRes, hr, "Failed to connect to stdout");
    }

    while (!fLast)
    {
        ::ZeroMemory(pBuffer, OUTPUT_BUFFER);
        fRes = ::ResetEvent(overlapped.hEvent);
        ExitOnNullWithLastError(fRes, hr, "Failed to reset event");

        fRes = ::ReadFile(hRead, pBuffer, OUTPUT_BUFFER - 1, nullptr, &overlapped);
        if (!fRes)
        {
            dwRes = ::GetLastError();
            if (dwRes == ERROR_BROKEN_PIPE)
            {
                break;
            }
            ExitOnNullWithLastError((dwRes == ERROR_IO_PENDING), hr, "Failed to wait for stdout data");
        }

        dwRes = ::WaitForMultipleObjects(ARRAYSIZE(rghHandles), rghHandles, FALSE, INFINITE);
        // Process terminated, or pipe abandoned- read any remains from the pipe
        if ((dwRes == WAIT_OBJECT_0) || (dwRes == WAIT_ABANDONED_0) || (dwRes == (WAIT_ABANDONED_0 + 1)))
        {
            fLast = TRUE;
            if (!::PeekNamedPipe(hRead, pBuffer, OUTPUT_BUFFER - 1, &dwBytes, nullptr, nullptr))
            {
                break;
            }
        }
        else
        {
            ExitOnNullWithLastError((dwRes != WAIT_FAILED), hr, "Failed to wait for process to terminate or write to stdout");
            if (dwRes != (WAIT_OBJECT_0 + 1))
            {
                ExitOnWin32Error(dwRes, hr, "Failed to wait for process to terminate or write to stdout.");
            }

            fRes = ::GetOverlappedResult(hRead, &overlapped, &dwBytes, FALSE);
            if (!fRes)
            {
                dwRes = ::GetLastError();
                if (dwRes == ERROR_BROKEN_PIPE)
                {
                    break;
                }
                ExitOnWin32Error(dwRes, hr, "Failed to read stdout data");
            }
        }

        if (fLogOutput && dwBytes)
        {
            // Check for UNICODE or ANSI output
            if (bFirst)
            {
                if ((isgraph(pBuffer[0]) && isgraph(pBuffer[1])) ||
                    (isgraph(pBuffer[0]) && isspace(pBuffer[1])) ||
                    (isspace(pBuffer[0]) && isgraph(pBuffer[1])) ||
                    (isspace(pBuffer[0]) && isspace(pBuffer[1])))
                {
                    bUnicode = FALSE;
                }

                bFirst = FALSE;
            }

            // Keep track of output
            if (bUnicode)
            {
                hr = StrAllocConcat(&szLog, (LPCWSTR)pBuffer, 0);
                ExitOnFailure(hr, "Failed to concatenate output strings.");

                if (psczOutput)
                {
                    hr = StrAllocConcat(psczOutput, (LPCWSTR)pBuffer, 0);
                    ExitOnFailure(hr, "Failed to concatenate output to return string.");
                }
            }
            else
            {
                hr = StrAllocStringAnsi(&szTemp, (LPCSTR)pBuffer, 0, CP_OEMCP);
                ExitOnFailure(hr, "Failed to allocate output string.");
                hr = StrAllocConcat(&szLog, szTemp, 0);
                ExitOnFailure(hr, "Failed to concatenate output strings.");

                if (psczOutput)
                {
                    hr = StrAllocConcat(psczOutput, szTemp, 0);
                    ExitOnFailure(hr, "Failed to concatenate output to return string.");
                }
            }

            // Log each line of the output
            pNext = szLog;
            pEnd = wcschr(szLog, L'\r');
            if (NULL == pEnd)
            {
                pEnd = wcschr(szLog, L'\n');
            }
            while (pEnd && *pEnd)
            {
                // Find beginning of next line
                pEnd[0] = 0;
                ++pEnd;
                if ((pEnd[0] == L'\r') || (pEnd[0] == L'\n'))
                {
                    ++pEnd;
                }

                // Log output
                hr = StrAllocString(&sczEscaped, pNext, 0);
                ExitOnFailure(hr, "Failed to allocate copy of string");

                hr = StrReplaceStringAll(&sczEscaped, L"%", L"%%");
                ExitOnFailure(hr, "Failed to escape percent signs in string");

                hr = StrAnsiAllocString(&szWrite, sczEscaped, 0, CP_OEMCP);
                ExitOnFailure(hr, "Failed to convert output to ANSI");
                WcaLog(LOGMSG_STANDARD, szWrite);

                // Next line
                pNext = pEnd;
                pEnd = wcschr(pNext, L'\r');
                if (NULL == pEnd)
                {
                    pEnd = wcschr(pNext, L'\n');
                }
            }

            hr = StrAllocString(&szTemp, pNext, 0);
            ExitOnFailure(hr, "Failed to allocate string");

            hr = StrAllocString(&szLog, szTemp, 0);
            ExitOnFailure(hr, "Failed to allocate string");
        }
    }

    // Print any text that didn't end with a new line
    if (szLog && *szLog)
    {
        if (psczOutput)
        {
            hr = StrAllocConcat(psczOutput, szLog, 0);
            ExitOnFailure(hr, "Failed to concatenate output to return string.");
        }

        hr = StrReplaceStringAll(&szLog, L"%", L"%%");
        ExitOnFailure(hr, "Failed to escape percent signs in string");

        hr = StrAnsiAllocString(&szWrite, szLog, 0, CP_OEMCP);
        ExitOnFailure(hr, "Failed to convert output to ANSI");

        WcaLog(LOGMSG_VERBOSE, szWrite);
    }

LExit:
    ReleaseMem(pBuffer);

    ReleaseStr(szLog);
    ReleaseStr(szTemp);
    ReleaseStr(szWrite);
    ReleaseStr(sczEscaped);
    ReleaseFileHandle(overlapped.hEvent);

    return hr;
}

static HRESULT QuietExecImpl(
    __inout_z LPWSTR wzCommand,
    __in DWORD dwTimeout,
    __in BOOL fLogCommand,
    __in BOOL fLogOutput,
    __out_z_opt LPWSTR* psczOutput
    )
{
    HRESULT hr = S_OK;
    PROCESS_INFORMATION oProcInfo;
    STARTUPINFOW oStartInfo;
    DWORD dwExitCode = ERROR_SUCCESS;
    HANDLE hOutRead = INVALID_HANDLE_VALUE;
    HANDLE hOutWrite = INVALID_HANDLE_VALUE;
    HANDLE hErrWrite = INVALID_HANDLE_VALUE;
    HANDLE hInRead = INVALID_HANDLE_VALUE;
    HANDLE hInWrite = INVALID_HANDLE_VALUE;

    memset(&oProcInfo, 0, sizeof(oProcInfo));
    memset(&oStartInfo, 0, sizeof(oStartInfo));

    // Create output redirect pipes
    hr = CreatePipes(&hOutRead, &hOutWrite, &hErrWrite, &hInRead, &hInWrite);
    ExitOnFailure(hr, "Failed to create output pipes");

    // Set up startup structure
    oStartInfo.cb = sizeof(STARTUPINFOW);
    oStartInfo.dwFlags = STARTF_USESTDHANDLES;
    oStartInfo.hStdInput = hInRead;
    oStartInfo.hStdOutput = hOutWrite;
    oStartInfo.hStdError = hErrWrite;

    // Log command if we were asked to do so
    if (fLogCommand)
    {
        WcaLog(LOGMSG_VERBOSE, "%ls", wzCommand);
    }

#pragma prefast(suppress:25028)
    if (::CreateProcessW(NULL,
        wzCommand, // command line
        NULL, // security info
        NULL, // thread info
        TRUE, // inherit handles
        ::GetPriorityClass(::GetCurrentProcess()) | CREATE_NO_WINDOW, // creation flags
        NULL, // environment
        NULL, // cur dir
        &oStartInfo,
        &oProcInfo))
    {
        ReleaseFile(oProcInfo.hThread);

        // Close child output/input handles so it doesn't hang
        ReleaseFile(hOutWrite);
        ReleaseFile(hErrWrite);
        ReleaseFile(hInRead);

        // Log output if we were asked to do so; otherwise just read the output handle
        HandleOutput(fLogOutput, oProcInfo.hProcess, hOutRead, psczOutput);

        // Wait for everything to finish
        ::WaitForSingleObject(oProcInfo.hProcess, dwTimeout);
        if (!::GetExitCodeProcess(oProcInfo.hProcess, &dwExitCode))
        {
            dwExitCode = ERROR_SEM_IS_SET;
        }

        ReleaseFile(hOutRead);
        ReleaseFile(hInWrite);
        ReleaseFile(oProcInfo.hProcess);
    }
    else
    {
        ExitOnLastError(hr, "Command failed to execute.");
    }

    ExitOnWin32Error(dwExitCode, hr, "Command line returned an error.");

LExit:
    return hr;
}


HRESULT WIXAPI QuietExec(
    __inout_z LPWSTR wzCommand,
    __in DWORD dwTimeout,
    __in BOOL fLogCommand,
    __in BOOL fLogOutput
    )
{
    return QuietExecImpl(wzCommand, dwTimeout, fLogCommand, fLogOutput, NULL);
}

HRESULT WIXAPI QuietExecCapture(
    __inout_z LPWSTR wzCommand,
    __in DWORD dwTimeout,
    __in BOOL fLogCommand,
    __in BOOL fLogOutput,
    __out_z_opt LPWSTR* psczOutput
    )
{
    return QuietExecImpl(wzCommand, dwTimeout, fLogCommand, fLogOutput, psczOutput);
}
