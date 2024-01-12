#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

// macro definitions

#define ReleasePipeHandle(h) if (h != INVALID_HANDLE_VALUE) { ::CloseHandle(h); h = INVALID_HANDLE_VALUE; }
#define ReleasePipeMessage(pMsg) if (pMsg) { PipeFreeMessage(pMsg); }


// constants

static const DWORD PIPE_WAIT_FOR_CONNECTION = 100;   // wait a 10th of a second,
static const DWORD PIPE_RETRY_FOR_CONNECTION = 1800; // for up to 3 minutes.


// structs

typedef struct _PIPE_MESSAGE
{
    DWORD dwMessageType;
    DWORD cbData;

    BOOL fAllocatedData;
    LPVOID pvData;
} PIPE_MESSAGE;

typedef struct _PIPE_RPC_HANDLE
{
    HANDLE hPipe;
    CRITICAL_SECTION cs;

    BOOL fInitialized;
    BOOL fOwnHandle;
} PIPE_RPC_HANDLE;

typedef struct _PIPE_RPC_RESULT
{
    HRESULT hr;

    DWORD cbData;
    LPBYTE pbData;
} PIPE_RPC_RESULT;


// functions

/*******************************************************************
 PipeClientConnect - Called from the client process to connect back
    to the pipe provided by the server process.

*******************************************************************/
DAPI_(HRESULT) PipeClientConnect(
    __in_z LPCWSTR wzPipeName,
    __out HANDLE* phPipe
);

/*******************************************************************
 PipeCreate - create a duplex byte-mode named pipe compatible for use
    with the other pipeutil functions.

*******************************************************************/
DAPI_(HRESULT) PipeCreate(
    __in LPCWSTR wzName,
    __in_opt LPSECURITY_ATTRIBUTES psa,
    __out HANDLE* phPipe
);

/*******************************************************************
 PipeOpen - opens an exist named pipe compatible for use with the other
    pipeutil functions.

*******************************************************************/
DAPI_(HRESULT) PipeOpen(
    __in_z LPCWSTR wzName,
    __out HANDLE* phPipe
);

/*******************************************************************
 PipeReadMessage - reads a message from the pipe. Free with
    PipeFreeMessage().

*******************************************************************/
DAPI_(HRESULT) PipeReadMessage(
    __in HANDLE hPipe,
    __in PIPE_MESSAGE* pMsg
);

/*******************************************************************
 PipeRpcInitiailize - initializes a RPC pipe handle from a pipe handle.

*******************************************************************/
DAPI_(void) PipeRpcInitialize(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in HANDLE hPipe,
    __in BOOL fTakeHandleOwnership
);

/*******************************************************************
 PipeRpcInitialized - checks if a RPC pipe handle is initialized.

*******************************************************************/
DAPI_(BOOL) PipeRpcInitialized(
    __in PIPE_RPC_HANDLE* phRpcPipe
);

/*******************************************************************
 PipeRpcUninitiailize - uninitializes a RPC pipe handle.

*******************************************************************/
DAPI_(void) PipeRpcUninitiailize(
    __in PIPE_RPC_HANDLE* phRpcPipe
);

/*******************************************************************
 PipeRpcReadMessage - reads a message from the pipe. Free with
    PipeFreeMessage().

*******************************************************************/
DAPI_(HRESULT) PipeRpcReadMessage(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in PIPE_MESSAGE* pMsg
);

/*******************************************************************
 PipeRpcRequest - sends message and reads a response over the pipe.
    Free with PipeFreeRpcResult().

*******************************************************************/
DAPI_(HRESULT) PipeRpcRequest(
    __in PIPE_RPC_HANDLE* phRpcPipe,
    __in DWORD dwMessageType,
    __in_bcount(cbArgs) LPVOID pbArgs,
    __in SIZE_T cbArgs,
    __in PIPE_RPC_RESULT* pResult
);

/*******************************************************************
 PipeRpcResponse - sends response over the pipe.

*******************************************************************/
DAPI_(HRESULT) PipeRpcResponse(
    __in PIPE_RPC_HANDLE* phPipe,
    __in DWORD dwMessageType,
    __in HRESULT hrResult,
    __in_bcount(cbResult) LPVOID pvResult,
    __in SIZE_T cbResult
    );

/*******************************************************************
 PipeRpcWriteMessage - writes a message to the pipe.

*******************************************************************/
DAPI_(HRESULT) PipeRpcWriteMessage(
    __in PIPE_RPC_HANDLE* phPipe,
    __in DWORD dwMessageType,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in SIZE_T cbData
);

/*******************************************************************
 PipeWriteDisconnect - writes a message to the pipe indicating the
    client should disconnect.

*******************************************************************/
DAPI_(HRESULT) PipeWriteDisconnect(
    __in HANDLE hPipe
    );

/*******************************************************************
 PipeFreeMessage - frees any memory allocated in PipeReadMessage.

*******************************************************************/
DAPI_(void) PipeFreeMessage(
    __in PIPE_MESSAGE* pMsg
);

/*******************************************************************
 PipeFreeRpcResult - frees any memory allocated in PipeRpcRequest.

*******************************************************************/
DAPI_(void) PipeFreeRpcResult(
    __in PIPE_RPC_RESULT* pResult
);

/*******************************************************************
 PipeServerWaitForClientConnect - Called from the server process to
    wait for a client to connect back to the provided pipe.

*******************************************************************/
DAPI_(HRESULT) PipeServerWaitForClientConnect(
    __in HANDLE hClientProcess,
    __in HANDLE hPipe
);

/*******************************************************************
 PipeWriteMessage - writes a message to the pipe.

*******************************************************************/
DAPI_(HRESULT) PipeWriteMessage(
    __in HANDLE hPipe,
    __in DWORD dwMessageType,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in SIZE_T cbData
);

#ifdef __cplusplus
}
#endif
