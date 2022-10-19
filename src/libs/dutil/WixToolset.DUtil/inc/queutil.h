#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseQueue(qh, pfn, pv) if (qh) { QueDestroy(qh, pfn, pv); }
#define ReleaseNullQue(qh, pfv, pv) if (qh) { QueDestroy(qh, pfn, pv); qh = NULL; }

typedef void* QUEUTIL_QUEUE_HANDLE;

typedef void(CALLBACK* PFNQUEUTIL_QUEUE_RELEASE_VALUE)(
    __in void* pvValue,
    __in void* pvContext
    );

extern const int QUEUTIL_QUEUE_HANDLE_BYTES;

/********************************************************************
QueCreate - Creates a simple queue. It is not thread safe.

********************************************************************/
HRESULT DAPI QueCreate(
    __out_bcount(QUEUTIL_QUEUE_HANDLE_BYTES) QUEUTIL_QUEUE_HANDLE* phQueue
    );

HRESULT DAPI QueEnqueue(
    __in_bcount(QUEUTIL_QUEUE_HANDLE_BYTES) QUEUTIL_QUEUE_HANDLE hQueue,
    __in void* pvValue
    );

/********************************************************************
QueDequeue - Returns the value from the beginning of the queue,
             or E_NOMOREITEMS if the queue is empty.

********************************************************************/
HRESULT DAPI QueDequeue(
    __in_bcount(QUEUTIL_QUEUE_HANDLE_BYTES) QUEUTIL_QUEUE_HANDLE hQueue,
    __out void** ppvValue
    );

void DAPI QueDestroy(
    __in_bcount(QUEUTIL_QUEUE_HANDLE_BYTES) QUEUTIL_QUEUE_HANDLE hQueue,
    __in_opt PFNQUEUTIL_QUEUE_RELEASE_VALUE pfnReleaseValue,
    __in_opt void* pvContext
    );

#ifdef __cplusplus
}
#endif
