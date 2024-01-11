// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static const DWORD PIPE_64KB = 64 * 1024;
static const LPCWSTR PIPE_NAME_FORMAT_STRING = L"\\\\.\\pipe\\%ls";


// Exit macros
#define PipeExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_PIPEUTIL, x, s, __VA_ARGS__)
#define PipeExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_PIPEUTIL, x, s, __VA_ARGS__)
#define PipeExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_PIPEUTIL, x, s, __VA_ARGS__)
#define PipeExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_PIPEUTIL, x, s, __VA_ARGS__)
#define PipeExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_PIPEUTIL, x, s, __VA_ARGS__)
#define PipeExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_PIPEUTIL, x, s, __VA_ARGS__)
#define PipeExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_PIPEUTIL, p, x, e, s, __VA_ARGS__)
#define PipeExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_PIPEUTIL, p, x, s, __VA_ARGS__)
#define PipeExitOnNullDebugTrace(p, x, e, s, ...)  PipeExitOnNullDebugTraceSource(DUTIL_SOURCE_PIPEUTIL, p, x, e, s, __VA_ARGS__)
#define PipeExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_PIPEUTIL, p, x, s, __VA_ARGS__)
#define PipeExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_PIPEUTIL, e, x, s, __VA_ARGS__)
#define PipeExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_PIPEUTIL, g, x, s, __VA_ARGS__)


static HRESULT AllocatePipeMessage(
    __in DWORD dwMessageType,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in SIZE_T cbData,
    __out_bcount(cb) LPVOID* ppvMessage,
    __out SIZE_T* pcbMessage
);


DAPI_(HRESULT) PipeClientConnect(
    __in_z LPCWSTR wzPipeName,
    __out HANDLE* phPipe
)
{
    HRESULT hr = S_OK;
    LPWSTR sczPipeName = NULL;
    HANDLE hPipe = INVALID_HANDLE_VALUE;

    // Try to connect to the parent.
    hr = StrAllocFormatted(&sczPipeName, PIPE_NAME_FORMAT_STRING, wzPipeName);
    PipeExitOnFailure(hr, "Failed to allocate name of pipe.");

    hr = E_UNEXPECTED;
    for (DWORD cRetry = 0; FAILED(hr) && cRetry < PIPE_RETRY_FOR_CONNECTION; ++cRetry)
    {
        hPipe = ::CreateFileW(sczPipeName, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
        if (INVALID_HANDLE_VALUE == hPipe)
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            if (E_FILENOTFOUND == hr) // if the pipe isn't created, call it a timeout waiting on the parent.
            {
                hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
            }

            ::Sleep(PIPE_WAIT_FOR_CONNECTION);
        }
        else // we have a connection, go with it.
        {
            hr = S_OK;
        }
    }
    PipeExitOnRootFailure(hr, "Failed to open parent pipe: %ls", sczPipeName);

    *phPipe = hPipe;
    hPipe = INVALID_HANDLE_VALUE;

LExit:
    ReleaseFileHandle(hPipe);
    return hr;
}

DAPI_(HRESULT) PipeCreate(
    __in LPCWSTR wzName,
    __in_opt LPSECURITY_ATTRIBUTES psa,
    __out HANDLE* phPipe
)
{
    HRESULT hr = S_OK;
    LPWSTR sczFullPipeName = NULL;
    HANDLE hPipe = INVALID_HANDLE_VALUE;

    // Create the pipe.
    hr = StrAllocFormatted(&sczFullPipeName, PIPE_NAME_FORMAT_STRING, wzName);
    PipeExitOnFailure(hr, "Failed to allocate full name of pipe: %ls", wzName);

    // TODO: consider using overlapped IO to do waits on the pipe and still be able to cancel and such.
    hPipe = ::CreateNamedPipeW(sczFullPipeName, PIPE_ACCESS_DUPLEX | FILE_FLAG_FIRST_PIPE_INSTANCE, PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT, 1, PIPE_64KB, PIPE_64KB, 1, psa);
    if (INVALID_HANDLE_VALUE == hPipe)
    {
        PipeExitWithLastError(hr, "Failed to create pipe: %ls", sczFullPipeName);
    }

    *phPipe = hPipe;
    hPipe = INVALID_HANDLE_VALUE;

LExit:
    ReleaseFileHandle(hPipe);
    ReleaseStr(sczFullPipeName);

    return hr;
}

DAPI_(void) PipeFreeMessage(
    __in PIPE_MESSAGE* pMsg
)
{
    if (pMsg->fAllocatedData)
    {
        ReleaseNullMem(pMsg->pvData);
        pMsg->fAllocatedData = FALSE;
    }
}


DAPI_(HRESULT) PipeOpen(
    __in_z LPCWSTR wzName,
    __out HANDLE* phPipe
)
{
    HRESULT hr = S_OK;
    LPWSTR sczPipeName = NULL;
    HANDLE hPipe = INVALID_HANDLE_VALUE;

    // Try to connect to the parent.
    hr = StrAllocFormatted(&sczPipeName, PIPE_NAME_FORMAT_STRING, wzName);
    PipeExitOnFailure(hr, "Failed to allocate name of pipe.");

    hr = E_UNEXPECTED;
    for (DWORD cRetry = 0; FAILED(hr) && cRetry < PIPE_RETRY_FOR_CONNECTION; ++cRetry)
    {
        hPipe = ::CreateFileW(sczPipeName, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
        if (INVALID_HANDLE_VALUE == hPipe)
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            if (E_FILENOTFOUND == hr) // if the pipe isn't created, call it a timeout waiting on the parent.
            {
                hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
            }

            ::Sleep(PIPE_WAIT_FOR_CONNECTION);
        }
        else // we have a connection, go with it.
        {
            hr = S_OK;
        }
    }
    PipeExitOnRootFailure(hr, "Failed to open parent pipe: %ls", sczPipeName);

    *phPipe = hPipe;
    hPipe = INVALID_HANDLE_VALUE;

LExit:
    ReleaseFileHandle(hPipe);
    ReleaseStr(sczPipeName);

    return hr;
}

DAPI_(HRESULT) PipeReadMessage(
    __in HANDLE hPipe,
    __in PIPE_MESSAGE* pMsg
)
{
    HRESULT hr = S_OK;
    BYTE pbMessageIdAndByteCount[sizeof(DWORD) + sizeof(DWORD)] = { };

    hr = FileReadHandle(hPipe, pbMessageIdAndByteCount, sizeof(pbMessageIdAndByteCount));
    if (HRESULT_FROM_WIN32(ERROR_BROKEN_PIPE) == hr)
    {
        memset(pbMessageIdAndByteCount, 0, sizeof(pbMessageIdAndByteCount));
        hr = S_FALSE;
    }
    PipeExitOnFailure(hr, "Failed to read message from pipe.");

    pMsg->dwMessageType = *(DWORD*)(pbMessageIdAndByteCount);
    pMsg->cbData = *(DWORD*)(pbMessageIdAndByteCount + sizeof(DWORD));
    if (pMsg->cbData)
    {
        pMsg->pvData = MemAlloc(pMsg->cbData, FALSE);
        PipeExitOnNull(pMsg->pvData, hr, E_OUTOFMEMORY, "Failed to allocate data for message.");

        hr = FileReadHandle(hPipe, reinterpret_cast<LPBYTE>(pMsg->pvData), pMsg->cbData);
        PipeExitOnFailure(hr, "Failed to read data for message.");

        pMsg->fAllocatedData = TRUE;
    }

LExit:
    if (!pMsg->fAllocatedData && pMsg->pvData)
    {
        MemFree(pMsg->pvData);
    }

    return hr;
}

DAPI_(HRESULT) PipeServerWaitForClientConnect(
    __in HANDLE hPipe
)
{
    HRESULT hr = S_OK;
    DWORD dwPipeState = PIPE_READMODE_BYTE | PIPE_NOWAIT;

    // Temporarily make the pipe non-blocking so we will not get stuck in ::ConnectNamedPipe() forever
    // if the child decides not to show up.
    if (!::SetNamedPipeHandleState(hPipe, &dwPipeState, NULL, NULL))
    {
        PipeExitWithLastError(hr, "Failed to set pipe to non-blocking.");
    }

    // Loop for a while waiting for a connection from child process.
    DWORD cRetry = 0;
    do
    {
        if (!::ConnectNamedPipe(hPipe, NULL))
        {
            DWORD er = ::GetLastError();
            if (ERROR_PIPE_CONNECTED == er)
            {
                hr = S_OK;
                break;
            }
            else if (ERROR_PIPE_LISTENING == er)
            {
                if (cRetry < PIPE_RETRY_FOR_CONNECTION)
                {
                    hr = HRESULT_FROM_WIN32(er);
                }
                else
                {
                    hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
                    break;
                }

                ++cRetry;
                ::Sleep(PIPE_WAIT_FOR_CONNECTION);
            }
            else
            {
                hr = HRESULT_FROM_WIN32(er);
                break;
            }
        }
    } while (HRESULT_FROM_WIN32(ERROR_PIPE_LISTENING) == hr);
    PipeExitOnRootFailure(hr, "Failed to wait for child to connect to pipe.");

    // Put the pipe back in blocking mode.
    dwPipeState = PIPE_READMODE_BYTE | PIPE_WAIT;
    if (!::SetNamedPipeHandleState(hPipe, &dwPipeState, NULL, NULL))
    {
        PipeExitWithLastError(hr, "Failed to reset pipe to blocking.");
    }

LExit:
    return hr;
}

DAPI_(HRESULT) PipeWriteMessage(
    __in HANDLE hPipe,
    __in DWORD dwMessageType,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in SIZE_T cbData
)
{
//    HRESULT hr = S_OK;
//
//    hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(&dwMessageType), sizeof(dwMessageType));
//    PipeExitOnFailure(hr, "Failed to write message id to pipe.");
//
//    hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(&cbData), sizeof(cbData));
//    PipeExitOnFailure(hr, "Failed to write message data size to pipe.");
//
//    if (pvData && cbData)
//    {
//        hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(pvData), cbData);
//        PipeExitOnFailure(hr, "Failed to write message data to pipe.");
//    }
//
//LExit:
//    return hr;
    HRESULT hr = S_OK;
    LPVOID pv = NULL;
    SIZE_T cb = 0;

    hr = AllocatePipeMessage(dwMessageType, pvData, cbData, &pv, &cb);
    ExitOnFailure(hr, "Failed to allocate message to write.");

    // Write the message.
    hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(pv), cb);
    ExitOnFailure(hr, "Failed to write message type to pipe.");

LExit:
    ReleaseMem(pv);
    return hr;
}

static HRESULT AllocatePipeMessage(
    __in DWORD dwMessageType,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in SIZE_T cbData,
    __out_bcount(cb) LPVOID* ppvMessage,
    __out SIZE_T* pcbMessage
)
{
    HRESULT hr = S_OK;
    LPVOID pv = NULL;
    size_t cb = 0;
    DWORD dwcbData = 0;

    // If no data was provided, ensure the count of bytes is zero.
    if (!pvData)
    {
        cbData = 0;
    }
    else if (MAXDWORD < cbData)
    {
        ExitWithRootFailure(hr, E_INVALIDDATA, "Pipe message is too large.");
    }

    hr = ::SizeTAdd(sizeof(dwMessageType) + sizeof(dwcbData), cbData, &cb);
    ExitOnRootFailure(hr, "Failed to calculate total pipe message size");

    dwcbData = (DWORD)cbData;

    // Allocate the message.
    pv = MemAlloc(cb, FALSE);
    ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to allocate memory for message.");

    memcpy_s(pv, cb, &dwMessageType, sizeof(dwMessageType));
    memcpy_s(static_cast<BYTE*>(pv) + sizeof(dwMessageType), cb - sizeof(dwMessageType), &dwcbData, sizeof(dwcbData));
    if (dwcbData)
    {
        memcpy_s(static_cast<BYTE*>(pv) + sizeof(dwMessageType) + sizeof(dwcbData), cb - sizeof(dwMessageType) - sizeof(dwcbData), pvData, dwcbData);
    }

    *pcbMessage = cb;
    *ppvMessage = pv;
    pv = NULL;

LExit:
    ReleaseMem(pv);
    return hr;
}
