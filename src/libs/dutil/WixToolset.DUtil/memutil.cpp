// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define MemExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_MEMUTIL, x, s, __VA_ARGS__)
#define MemExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_MEMUTIL, x, s, __VA_ARGS__)
#define MemExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_MEMUTIL, x, s, __VA_ARGS__)
#define MemExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_MEMUTIL, x, s, __VA_ARGS__)
#define MemExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_MEMUTIL, x, s, __VA_ARGS__)
#define MemExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_MEMUTIL, x, e, s, __VA_ARGS__)
#define MemExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_MEMUTIL, x, s, __VA_ARGS__)
#define MemExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_MEMUTIL, p, x, e, s, __VA_ARGS__)
#define MemExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_MEMUTIL, p, x, s, __VA_ARGS__)
#define MemExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_MEMUTIL, p, x, e, s, __VA_ARGS__)
#define MemExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_MEMUTIL, p, x, s, __VA_ARGS__)
#define MemExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_MEMUTIL, e, x, s, __VA_ARGS__)
#define MemExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_MEMUTIL, g, x, s, __VA_ARGS__)


#if DEBUG
static BOOL vfMemInitialized = FALSE;
#endif

extern "C" HRESULT DAPI MemInitialize()
{
#if DEBUG
    vfMemInitialized = TRUE;
#endif
    return S_OK;
}

extern "C" void DAPI MemUninitialize()
{
#if DEBUG
    vfMemInitialized = FALSE;
#endif
}

extern "C" LPVOID DAPI MemAlloc(
    __in SIZE_T cbSize,
    __in BOOL fZero
    )
{
//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    AssertSz(0 < cbSize, "MemAlloc() called with invalid size");
    return ::HeapAlloc(::GetProcessHeap(), fZero ? HEAP_ZERO_MEMORY : 0, cbSize);
}


extern "C" LPVOID DAPI MemReAlloc(
    __in LPVOID pv,
    __in SIZE_T cbSize,
    __in BOOL fZero
    )
{
//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    AssertSz(0 < cbSize, "MemReAlloc() called with invalid size");
    return ::HeapReAlloc(::GetProcessHeap(), fZero ? HEAP_ZERO_MEMORY : 0, pv, cbSize);
}


extern "C" HRESULT DAPI MemReAllocSecure(
    __in LPVOID pv,
    __in SIZE_T cbSize,
    __in BOOL fZero,
    __deref_out LPVOID* ppvNew
    )
{
//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    AssertSz(ppvNew, "MemReAllocSecure() called with uninitialized pointer");
    AssertSz(0 < cbSize, "MemReAllocSecure() called with invalid size");

    HRESULT hr = S_OK;
    DWORD dwFlags = HEAP_REALLOC_IN_PLACE_ONLY;
    LPVOID pvNew = NULL;
    SIZE_T cb = 0;

    dwFlags |= fZero ? HEAP_ZERO_MEMORY : 0;
    pvNew = ::HeapReAlloc(::GetProcessHeap(), dwFlags, pv, cbSize);
    if (!pvNew)
    {
        pvNew = MemAlloc(cbSize, fZero);
        if (pvNew)
        {
            hr = MemSizeChecked(pv, &cb);
            MemExitOnFailure(hr, "Failed to get current memory size.");

            const SIZE_T cbCurrent = cb;

            // HeapReAlloc may allocate more memory than requested.
            hr = MemSizeChecked(pvNew, &cb);
            MemExitOnFailure(hr, "Failed to get new memory size.");

            const SIZE_T cbNew = cb;

            cbSize = cbNew;
            if (cbSize > cbCurrent)
            {
                cbSize = cbCurrent;
            }

            memcpy_s(pvNew, cbNew, pv, cbSize);

            SecureZeroMemory(pv, cbCurrent);
            MemFree(pv);
        }
    }
    MemExitOnNull(pvNew, hr, E_OUTOFMEMORY, "Failed to reallocate memory");

    *ppvNew = pvNew;
    pvNew = NULL;

LExit:
    ReleaseMem(pvNew);

    return hr;
}


extern "C" HRESULT DAPI MemAllocArray(
    __inout LPVOID* ppvArray,
    __in SIZE_T cbArrayType,
    __in DWORD dwItemCount
    )
{
    return MemReAllocArray(ppvArray, 0, cbArrayType, dwItemCount);
}


extern "C" HRESULT DAPI MemReAllocArray(
    __inout LPVOID* ppvArray,
    __in DWORD cArray,
    __in SIZE_T cbArrayType,
    __in DWORD dwNewItemCount
    )
{
    HRESULT hr = S_OK;
    DWORD cNew = 0;
    LPVOID pvNew = NULL;
    SIZE_T cbNew = 0;

    hr = ::DWordAdd(cArray, dwNewItemCount, &cNew);
    MemExitOnFailure(hr, "Integer overflow when calculating new element count.");

    hr = ::SIZETMult(cNew, cbArrayType, &cbNew);
    MemExitOnFailure(hr, "Integer overflow when calculating new block size.");

    if (*ppvArray)
    {
        SIZE_T cbCurrent = 0;
        hr = MemSizeChecked(*ppvArray, &cbCurrent);
        MemExitOnFailure(hr, "Failed to get current memory size.");

        if (cbCurrent < cbNew)
        {
            pvNew = MemReAlloc(*ppvArray, cbNew, TRUE);
            MemExitOnNull(pvNew, hr, E_OUTOFMEMORY, "Failed to allocate larger array.");

            *ppvArray = pvNew;
        }
    }
    else
    {
        pvNew = MemAlloc(cbNew, TRUE);
        MemExitOnNull(pvNew, hr, E_OUTOFMEMORY, "Failed to allocate new array.");

        *ppvArray = pvNew;
    }

LExit:
    return hr;
}


extern "C" HRESULT DAPI MemEnsureArraySize(
    __deref_inout_bcount(cArray * cbArrayType) LPVOID* ppvArray,
    __in DWORD cArray,
    __in SIZE_T cbArrayType,
    __in DWORD dwGrowthCount
    )
{
    HRESULT hr = S_OK;
    DWORD cNew = 0;
    LPVOID pvNew = NULL;
    SIZE_T cbNew = 0;

    hr = ::DWordAdd(cArray, dwGrowthCount, &cNew);
    MemExitOnFailure(hr, "Integer overflow when calculating new element count.");

    hr = ::SIZETMult(cNew, cbArrayType, &cbNew);
    MemExitOnFailure(hr, "Integer overflow when calculating new block size.");

    if (*ppvArray)
    {
        SIZE_T cbUsed = cArray * cbArrayType;
        SIZE_T cbCurrent = 0;

        hr = MemSizeChecked(*ppvArray, &cbCurrent);
        MemExitOnFailure(hr, "Failed to get current memory size.");

        if (cbCurrent < cbUsed)
        {
            pvNew = MemReAlloc(*ppvArray, cbNew, TRUE);
            MemExitOnNull(pvNew, hr, E_OUTOFMEMORY, "Failed to allocate array larger.");

            *ppvArray = pvNew;
        }
    }
    else
    {
        pvNew = MemAlloc(cbNew, TRUE);
        MemExitOnNull(pvNew, hr, E_OUTOFMEMORY, "Failed to allocate new array.");

        *ppvArray = pvNew;
    }

LExit:
    return hr;
}


extern "C" HRESULT DAPI MemEnsureArraySizeForNewItems(
    __inout LPVOID* ppvArray,
    __in DWORD cArray,
    __in DWORD cNewItems,
    __in SIZE_T cbArrayType,
    __in DWORD dwGrowthCount
    )
{
    HRESULT hr = S_OK;
    DWORD cNew = 0;

    hr = ::DWordAdd(cArray, cNewItems, &cNew);
    MemExitOnFailure(hr, "Integer overflow when calculating new element count.");

    hr = MemEnsureArraySize(ppvArray, cNew, cbArrayType, dwGrowthCount);

LExit:
    return hr;
}


extern "C" HRESULT DAPI MemInsertIntoArray(
    __deref_inout_bcount((cExistingArray + cInsertItems) * cbArrayType) LPVOID* ppvArray,
    __in DWORD dwInsertIndex,
    __in DWORD cInsertItems,
    __in DWORD cExistingArray,
    __in SIZE_T cbArrayType,
    __in DWORD dwGrowthCount
    )
{
    HRESULT hr = S_OK;
    DWORD i;
    BYTE *pbArray = NULL;

    if (0 == cInsertItems)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = MemEnsureArraySizeForNewItems(ppvArray, cExistingArray, cInsertItems, cbArrayType, dwGrowthCount);
    MemExitOnFailure(hr, "Failed to resize array while inserting items");

    pbArray = reinterpret_cast<BYTE *>(*ppvArray);
    for (i = cExistingArray + cInsertItems - 1; i > dwInsertIndex; --i)
    {
        memcpy_s(pbArray + i * cbArrayType, cbArrayType, pbArray + (i - 1) * cbArrayType, cbArrayType);
    }

    // Zero out the newly-inserted items
    memset(pbArray + dwInsertIndex * cbArrayType, 0, cInsertItems * cbArrayType);

LExit:
    return hr;
}

extern "C" void DAPI MemRemoveFromArray(
    __inout_bcount((cExistingArray) * cbArrayType) LPVOID pvArray,
    __in DWORD dwRemoveIndex,
    __in DWORD cRemoveItems,
    __in DWORD cExistingArray,
    __in SIZE_T cbArrayType,
    __in BOOL fPreserveOrder
    )
{
    BYTE *pbArray = static_cast<BYTE *>(pvArray);
    DWORD cItemsLeftAfterRemoveIndex = (cExistingArray - cRemoveItems - dwRemoveIndex);

    if (fPreserveOrder)
    {
        memmove(pbArray + dwRemoveIndex * cbArrayType, pbArray + (dwRemoveIndex + cRemoveItems) * cbArrayType, cItemsLeftAfterRemoveIndex * cbArrayType);
    }
    else
    {
        DWORD cItemsToMove = (cRemoveItems > cItemsLeftAfterRemoveIndex ? cItemsLeftAfterRemoveIndex : cRemoveItems);
        memmove(pbArray + dwRemoveIndex * cbArrayType, pbArray + (cExistingArray - cItemsToMove) * cbArrayType, cItemsToMove * cbArrayType);
    }

    ZeroMemory(pbArray + (cExistingArray - cRemoveItems) * cbArrayType, cRemoveItems * cbArrayType);
}

extern "C" void DAPI MemArraySwapItems(
    __inout_bcount(cbArrayType) LPVOID pvArray,
    __in DWORD dwIndex1,
    __in DWORD dwIndex2,
    __in SIZE_T cbArrayType
    )
{
    BYTE *pbArrayItem1 = static_cast<BYTE *>(pvArray) + dwIndex1 * cbArrayType;
    BYTE *pbArrayItem2 = static_cast<BYTE *>(pvArray) + dwIndex2 * cbArrayType;
    DWORD dwByteIndex = 0;

    if (dwIndex1 == dwIndex2)
    {
        return;
    }

    // Use XOR swapping to avoid the need for a temporary item
    while (dwByteIndex < cbArrayType)
    {
        // Try to do many bytes at a time in most cases
        if (cbArrayType - dwByteIndex > sizeof(DWORD64))
        {
            // x: X xor Y
            *(reinterpret_cast<DWORD64 *>(pbArrayItem1 + dwByteIndex)) ^= *(reinterpret_cast<DWORD64 *>(pbArrayItem2 + dwByteIndex));
            // y: X xor Y
            *(reinterpret_cast<DWORD64 *>(pbArrayItem2 + dwByteIndex)) = *(reinterpret_cast<DWORD64 *>(pbArrayItem1 + dwByteIndex)) ^ *(reinterpret_cast<DWORD64 *>(pbArrayItem2 + dwByteIndex));
            // x: X xor Y
            *(reinterpret_cast<DWORD64 *>(pbArrayItem1 + dwByteIndex)) ^= *(reinterpret_cast<DWORD64 *>(pbArrayItem2 + dwByteIndex));

            dwByteIndex += sizeof(DWORD64);
        }
        else
        {
            // x: X xor Y
            *(reinterpret_cast<unsigned char *>(pbArrayItem1 + dwByteIndex)) ^= *(reinterpret_cast<unsigned char *>(pbArrayItem2 + dwByteIndex));
            // y: X xor Y
            *(reinterpret_cast<unsigned char *>(pbArrayItem2 + dwByteIndex)) = *(reinterpret_cast<unsigned char *>(pbArrayItem1 + dwByteIndex)) ^ *(reinterpret_cast<unsigned char *>(pbArrayItem2 + dwByteIndex));
            // x: X xor Y
            *(reinterpret_cast<unsigned char *>(pbArrayItem1 + dwByteIndex)) ^= *(reinterpret_cast<unsigned char *>(pbArrayItem2 + dwByteIndex));

            dwByteIndex += sizeof(unsigned char);
        }
    }
}

extern "C" HRESULT DAPI MemFree(
    __in LPVOID pv
    )
{
//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    return ::HeapFree(::GetProcessHeap(), 0, pv) ? S_OK : HRESULT_FROM_WIN32(::GetLastError());
}


extern "C" SIZE_T DAPI MemSize(
    __in LPCVOID pv
    )
{
//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    return ::HeapSize(::GetProcessHeap(), 0, pv);
}


extern "C" HRESULT DAPI MemSizeChecked(
    __in LPCVOID pv,
    __out SIZE_T* pcb
    )
{
    HRESULT hr = S_OK;

//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    *pcb = MemSize(pv);

    if (-1 == *pcb)
    {
        MemExitWithRootFailure(hr, E_INVALIDARG, "Failed to get memory size");
    }

LExit:
    return hr;
}
