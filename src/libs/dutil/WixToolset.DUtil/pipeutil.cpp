// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static const DWORD PIPE_64KB = 64 * 1024;
static const LPCWSTR PIPE_NAME_FORMAT_STRING = L"\\\\.\\pipe\\%ls";
static const DWORD PIPE_MESSAGE_DISCONNECT = 0xFFFFFFFF;

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

    ZeroMemory(pMsg, sizeof(PIPE_MESSAGE));
}

DAPI_(void) PipeFreeRpcResult(
    __in PIPE_RPC_RESULT* pResult
)
{
    if (pResult->pbData)
    {
        ReleaseNullMem(pResult->pbData);
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
    DWORD rgdwMessageIdAndByteCount[2] = { };
    LPBYTE pbData = NULL;
    DWORD cbData = 0;

    hr = FileReadHandle(hPipe, reinterpret_cast<LPBYTE>(rgdwMessageIdAndByteCount), sizeof(rgdwMessageIdAndByteCount));
    if (HRESULT_FROM_WIN32(ERROR_BROKEN_PIPE) == hr)
    {
        memset(rgdwMessageIdAndByteCount, 0, sizeof(rgdwMessageIdAndByteCount));
        hr = S_FALSE;
    }
    PipeExitOnFailure(hr, "Failed to read message from pipe.");

    Trace(REPORT_STANDARD, "RPC pipe %p read message: %u recv cbData: %u", hPipe, rgdwMessageIdAndByteCount[0], rgdwMessageIdAndByteCount[1]);

    cbData = rgdwMessageIdAndByteCount[1];
    if (cbData)
    {
        pbData = reinterpret_cast<LPBYTE>(MemAlloc(cbData, FALSE));
        PipeExitOnNull(pbData, hr, E_OUTOFMEMORY, "Failed to allocate data for message.");

        hr = FileReadHandle(hPipe, pbData, cbData);
        PipeExitOnFailure(hr, "Failed to read data for message.");
    }

    pMsg->dwMessageType = rgdwMessageIdAndByteCount[0];
    pMsg->cbData = cbData;
    pMsg->pvData = pbData;
    pbData = NULL;

    if (PIPE_MESSAGE_DISCONNECT == pMsg->dwMessageType)
    {
        hr = S_FALSE;
    }

LExit:
    ReleaseMem(pbData);

    return hr;
}

DAPI_(void) PipeRpcInitialize(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in HANDLE hPipe,
    __in BOOL fTakeHandleOwnership
)
{
    phRpcPipe->hPipe = hPipe;
    if (phRpcPipe->hPipe != INVALID_HANDLE_VALUE)
    {
        ::InitializeCriticalSection(&phRpcPipe->cs);
        phRpcPipe->fOwnHandle = fTakeHandleOwnership;
        phRpcPipe->fInitialized = TRUE;
    }
}

DAPI_(BOOL) PipeRpcInitialized(
    __in PIPE_RPC_HANDLE* phRpcPipe
)
{
    return phRpcPipe->fInitialized && phRpcPipe->hPipe != INVALID_HANDLE_VALUE;
}

DAPI_(void) PipeRpcUninitiailize(
    __in PIPE_RPC_HANDLE* phRpcPipe
)
{
    if (phRpcPipe->fInitialized)
    {
        ::DeleteCriticalSection(&phRpcPipe->cs);

        if (phRpcPipe->fOwnHandle)
        {
            ::CloseHandle(phRpcPipe->hPipe);
        }

        phRpcPipe->hPipe = INVALID_HANDLE_VALUE;
        phRpcPipe->fOwnHandle = FALSE;
        phRpcPipe->fInitialized = FALSE;
    }
}

DAPI_(HRESULT) PipeRpcReadMessage(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in PIPE_MESSAGE* pMsg
)
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&phRpcPipe->cs);

    hr = PipeReadMessage(phRpcPipe->hPipe, pMsg);
    PipeExitOnFailure(hr, "Failed to read message from RPC pipe.");

LExit:
    ::LeaveCriticalSection(&phRpcPipe->cs);

    return hr;
}

DAPI_(HRESULT) PipeRpcRequest(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in DWORD dwMessageType,
    __in_bcount(cbArgs) LPVOID pvArgs,
    __in SIZE_T cbArgs,
    __in PIPE_RPC_RESULT* pResult
)
{
    HRESULT hr = S_OK;
    HANDLE hPipe = phRpcPipe->hPipe;
    BOOL fLocked = FALSE;
    DWORD rgResultAndDataSize[2] = { };
    DWORD cbData = 0;
    LPBYTE pbData = NULL;

    if (hPipe == INVALID_HANDLE_VALUE)
    {
        ExitFunction();
    }

    Trace(REPORT_STANDARD, "RPC pipe %p request message: %d send cbArgs: %u", hPipe, dwMessageType, cbArgs);

    ::EnterCriticalSection(&phRpcPipe->cs);
    fLocked = TRUE;

    // Send the message.
    hr = PipeRpcWriteMessage(phRpcPipe, dwMessageType, pvArgs, cbArgs);
    PipeExitOnFailure(hr, "Failed to send RPC pipe request.");

    // Read the result and size of response data.
    hr = FileReadHandle(hPipe, reinterpret_cast<LPBYTE>(rgResultAndDataSize), sizeof(rgResultAndDataSize));
    PipeExitOnFailure(hr, "Failed to read result and size of message.");

    pResult->hr = rgResultAndDataSize[0];
    cbData = rgResultAndDataSize[1];

    Trace(REPORT_STANDARD, "RPC pipe %p request message: %d returned hr: 0x%x, cbData: %u", hPipe, dwMessageType, pResult->hr, cbData);
    AssertSz(FAILED(pResult->hr) || pResult->hr == S_OK || pResult->hr == S_FALSE, "Unexpected HRESULT from RPC pipe request.");

    if (cbData)
    {
        pbData = reinterpret_cast<LPBYTE>(MemAlloc(cbData, TRUE));
        PipeExitOnNull(pbData, hr, E_OUTOFMEMORY, "Failed to allocate memory for RPC pipe results.");

        hr = FileReadHandle(hPipe, pbData, cbData);
        PipeExitOnFailure(hr, "Failed to read result data.");
    }

    pResult->cbData = cbData;
    pResult->pbData = pbData;
    pbData = NULL;

    hr = pResult->hr;
    PipeExitOnFailure(hr, "RPC pipe client reported failure.");

LExit:
    ReleaseMem(pbData);

    if (fLocked)
    {
        ::LeaveCriticalSection(&phRpcPipe->cs);
    }

    return hr;
}

DAPI_(HRESULT) PipeRpcResponse(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in DWORD
#if DEBUG
     dwMessageType
#endif
    ,
    __in HRESULT hrResult,
    __in_bcount(cbResult) LPVOID pvResult,
    __in SIZE_T cbResult
    )
{
    HRESULT hr = S_OK;
    HANDLE hPipe = phRpcPipe->hPipe;
    DWORD dwcbResult = 0;

    hr = DutilSizetToDword(pvResult ? cbResult : 0, &dwcbResult);
    PipeExitOnFailure(hr, "Pipe message is too large.");

    Trace(REPORT_STANDARD, "RPC pipe %p response message: %d returned hr: 0x%x, cbResult: %u", hPipe, dwMessageType, hrResult, dwcbResult);

    ::EnterCriticalSection(&phRpcPipe->cs);

    hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(&hrResult), sizeof(hrResult));
    PipeExitOnFailure(hr, "Failed to write RPC result code to pipe.");

    hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(&dwcbResult), sizeof(dwcbResult));
    PipeExitOnFailure(hr, "Failed to write RPC result size to pipe.");

    if (dwcbResult)
    {
        hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(pvResult), dwcbResult);
        PipeExitOnFailure(hr, "Failed to write RPC result data to pipe.");
    }

LExit:
    ::LeaveCriticalSection(&phRpcPipe->cs);

    return hr;
}

DAPI_(HRESULT) PipeRpcWriteMessage(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in DWORD dwMessageType,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in SIZE_T cbData
)
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&phRpcPipe->cs);

    hr = PipeWriteMessage(phRpcPipe->hPipe, dwMessageType, pvData, cbData);
    PipeExitOnFailure(hr, "Failed to write message type to RPC pipe.");

LExit:
    ::LeaveCriticalSection(&phRpcPipe->cs);

    return hr;
}

DAPI_(HRESULT) PipeRpcWriteMessageReadResponse(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in DWORD dwMessageType,
    __in_bcount_opt(cbData) LPBYTE pbArgData,
    __in SIZE_T cbArgData,
    __in PIPE_RPC_RESULT* pResult
)
{
    HRESULT hr = S_OK;
    DWORD rgResultAndSize[2] = { };
    LPBYTE pbResultData = NULL;
    DWORD cbResultData = 0;

    hr = PipeWriteMessage(phRpcPipe->hPipe, dwMessageType, pbArgData, cbArgData);
    PipeExitOnFailure(hr, "Failed to write message type to RPC pipe.");

    // Read the result and size of response.
    hr = FileReadHandle(phRpcPipe->hPipe, reinterpret_cast<LPBYTE>(rgResultAndSize), sizeof(rgResultAndSize));
    ExitOnFailure(hr, "Failed to read result and size of message.");

    pResult->hr = rgResultAndSize[0];
    cbResultData = rgResultAndSize[1];

    if (cbResultData)
    {
        pbResultData = reinterpret_cast<LPBYTE>(MemAlloc(cbResultData, TRUE));
        ExitOnNull(pbResultData, hr, E_OUTOFMEMORY, "Failed to allocate memory for BA results.");

        hr = FileReadHandle(phRpcPipe->hPipe, pbResultData, cbResultData);
        ExitOnFailure(hr, "Failed to read result and size of message.");
    }

    pResult->cbData = cbResultData;
    pResult->pbData = pbResultData;
    pbResultData = NULL;

    hr = pResult->hr;
    ExitOnFailure(hr, "BA reported failure.");

LExit:
    ReleaseMem(pbResultData);

    ::LeaveCriticalSection(&phRpcPipe->cs);

    return hr;
}

DAPI_(HRESULT) PipeServerWaitForClientConnect(
    __in HANDLE hClientProcess,
    __in HANDLE hPipe
)
{
    HRESULT hr = S_OK;
    DWORD dwPipeState = PIPE_READMODE_BYTE | PIPE_NOWAIT;

    // Temporarily make the pipe non-blocking so we will not get stuck in ::ConnectNamedPipe() forever
    // if the client decides not to show up.
    if (!::SetNamedPipeHandleState(hPipe, &dwPipeState, NULL, NULL))
    {
        PipeExitWithLastError(hr, "Failed to set pipe to non-blocking.");
    }

    // Loop for a while waiting for a connection from client process.
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

                // Ensure the client is still around.
                hr = ::AppWaitForSingleObject(hClientProcess, PIPE_WAIT_FOR_CONNECTION);
                if (HRESULT_FROM_WIN32(WAIT_TIMEOUT) == hr)
                {
                    // Timeout out means the process is still there, that's good.
                    hr = HRESULT_FROM_WIN32(ERROR_PIPE_LISTENING);
                }
                else if (SUCCEEDED(hr))
                {
                    // Success means the process is gone, that's bad.
                    hr = HRESULT_FROM_WIN32(WAIT_ABANDONED);
                }
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

DAPI_(HRESULT) PipeWriteDisconnect(
    __in HANDLE hPipe
    )
{
    HRESULT hr = S_OK;
    LPVOID pv = NULL;
    SIZE_T cb = 0;

    hr = AllocatePipeMessage(PIPE_MESSAGE_DISCONNECT, NULL, 0, &pv, &cb);
    ExitOnFailure(hr, "Failed to allocate message to write.");

    // Write the message.
    hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(pv), cb);
    ExitOnFailure(hr, "Failed to write message type to pipe.");

LExit:
    ReleaseMem(pv);
    return hr;
}

DAPI_(HRESULT) PipeWriteMessage(
    __in HANDLE hPipe,
    __in DWORD dwMessageType,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in SIZE_T cbData
)
{
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
    __out_bcount(*pcbMessage) LPVOID* ppvMessage,
    __out SIZE_T* pcbMessage
)
{
    HRESULT hr = S_OK;
    LPVOID pv = NULL;
    size_t cb = 0;
    DWORD dwcbData = 0;

    hr = DutilSizetToDword(pvData ? cbData : 0, &dwcbData);
    PipeExitOnFailure(hr, "Pipe message is too large.");

    hr = ::SizeTAdd(sizeof(dwMessageType) + sizeof(dwcbData), dwcbData, &cb);
    ExitOnRootFailure(hr, "Failed to calculate total pipe message size");

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
