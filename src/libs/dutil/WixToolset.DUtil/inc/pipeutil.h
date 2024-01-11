#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define ReleasePipeHandle(h) if (h != INVALID_HANDLE_VALUE) { ::CloseHandle(h); }
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
 PipeWriteMessage - writes a message to the pipe.

*******************************************************************/
DAPI_(HRESULT) PipeWriteMessage(
    __in HANDLE hPipe,
    __in DWORD dwMessageType,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in SIZE_T cbData
);

/*******************************************************************
 PipeFreeMessage - frees any memory allocated in PipeReadMessage.

*******************************************************************/
DAPI_(void) PipeFreeMessage(
    __in PIPE_MESSAGE* pMsg
);

/*******************************************************************
 PipeServerWaitForClientConnect - Called from the server process to
    wait for a client to connect back to the provided pipe.

*******************************************************************/
DAPI_(HRESULT) PipeServerWaitForClientConnect(
    __in HANDLE hPipe
);

#ifdef __cplusplus
}
#endif
