// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define QueExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_QUEUTIL, x, s, __VA_ARGS__)
#define QueExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_QUEUTIL, x, s, __VA_ARGS__)
#define QueExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_QUEUTIL, x, s, __VA_ARGS__)
#define QueExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_QUEUTIL, x, s, __VA_ARGS__)
#define QueExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_QUEUTIL, x, s, __VA_ARGS__)
#define QueExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_QUEUTIL, x, s, __VA_ARGS__)
#define QueExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_QUEUTIL, p, x, e, s, __VA_ARGS__)
#define QueExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_QUEUTIL, p, x, s, __VA_ARGS__)
#define QueExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_QUEUTIL, p, x, e, s, __VA_ARGS__)
#define QueExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_QUEUTIL, p, x, s, __VA_ARGS__)
#define QueExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_QUEUTIL, e, x, s, __VA_ARGS__)
#define QueExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_QUEUTIL, g, x, s, __VA_ARGS__)


struct QUEUTIL_QUEUE_ITEM
{
    QUEUTIL_QUEUE_ITEM* pNext;
    void* pvValue;
};

struct QUEUTIL_QUEUE_STRUCT
{
    QUEUTIL_QUEUE_ITEM* pFirst;
    QUEUTIL_QUEUE_ITEM* pLast;
};

const int QUEUTIL_QUEUE_HANDLE_BYTES = sizeof(QUEUTIL_QUEUE_STRUCT);

extern "C" HRESULT DAPI QueCreate(
    __out_bcount(QUEUTIL_QUEUE_HANDLE_BYTES) QUEUTIL_QUEUE_HANDLE* phQueue
    )
{
    HRESULT hr = S_OK;

    QueExitOnNull(phQueue, hr, E_INVALIDARG, "Handle not specified while creating queue.");

    *phQueue = reinterpret_cast<QUEUTIL_QUEUE_HANDLE>(MemAlloc(QUEUTIL_QUEUE_HANDLE_BYTES, TRUE));
    QueExitOnNull(*phQueue, hr, E_OUTOFMEMORY, "Failed to allocate queue object.");

LExit:
    return hr;
}

extern "C" HRESULT DAPI QueEnqueue(
    __in_bcount(QUEUTIL_QUEUE_HANDLE_BYTES) QUEUTIL_QUEUE_HANDLE hQueue,
    __in void* pvValue
    )
{
    HRESULT hr = S_OK;
    QUEUTIL_QUEUE_ITEM* pItem = NULL;
    QUEUTIL_QUEUE_STRUCT* pQueue = reinterpret_cast<QUEUTIL_QUEUE_STRUCT*>(hQueue);

    QueExitOnNull(pQueue, hr, E_INVALIDARG, "Handle not specified while enqueing value.");

    pItem = reinterpret_cast<QUEUTIL_QUEUE_ITEM*>(MemAlloc(sizeof(QUEUTIL_QUEUE_ITEM), TRUE));
    QueExitOnNull(pItem, hr, E_OUTOFMEMORY, "Failed to allocate queue item.");

    pItem->pvValue = pvValue;

    if (!pQueue->pLast)
    {
        pQueue->pFirst = pItem;
    }
    else
    {
        pQueue->pLast->pNext = pItem;
    }

    pQueue->pLast = pItem;

    pItem = NULL;

LExit:
    ReleaseMem(pItem);

    return hr;
}

extern "C" HRESULT DAPI QueDequeue(
    __in_bcount(QUEUTIL_QUEUE_HANDLE_BYTES) QUEUTIL_QUEUE_HANDLE hQueue,
    __out void** ppvValue
    )
{
    HRESULT hr = S_OK;
    QUEUTIL_QUEUE_ITEM* pItem = NULL;
    QUEUTIL_QUEUE_STRUCT* pQueue = reinterpret_cast<QUEUTIL_QUEUE_STRUCT*>(hQueue);

    QueExitOnNull(pQueue, hr, E_INVALIDARG, "Handle not specified while dequeing value.");

    if (!pQueue->pFirst)
    {
        *ppvValue = NULL;
        ExitFunction1(hr = E_NOMOREITEMS);
    }

    pItem = pQueue->pFirst;

    if (!pItem->pNext)
    {
        pQueue->pFirst = NULL;
        pQueue->pLast = NULL;
    }
    else
    {
        pQueue->pFirst = pItem->pNext;
    }

    *ppvValue = pItem->pvValue;

LExit:
    ReleaseMem(pItem);

    return hr;
}

extern "C" void DAPI QueDestroy(
    __in_bcount(QUEUTIL_QUEUE_HANDLE_BYTES) QUEUTIL_QUEUE_HANDLE hQueue,
    __in_opt PFNQUEUTIL_QUEUE_RELEASE_VALUE pfnReleaseValue,
    __in_opt void* pvContext
    )
{
    HRESULT hr = S_OK;
    void* pvValue = NULL;

    hr = hQueue ? QueDequeue(hQueue, &pvValue) : E_NOMOREITEMS;

    while (SUCCEEDED(hr))
    {
        if (pfnReleaseValue)
        {
            pfnReleaseValue(pvValue, pvContext);
        }

        hr = QueDequeue(hQueue, &pvValue);
    }

    ReleaseMem(hQueue);
}
