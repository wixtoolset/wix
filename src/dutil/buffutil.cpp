// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define BuffExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_BUFFUTIL, x, s, __VA_ARGS__)
#define BuffExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_BUFFUTIL, x, s, __VA_ARGS__)
#define BuffExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_BUFFUTIL, x, s, __VA_ARGS__)
#define BuffExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_BUFFUTIL, x, s, __VA_ARGS__)
#define BuffExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_BUFFUTIL, x, s, __VA_ARGS__)
#define BuffExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_BUFFUTIL, x, s, __VA_ARGS__)
#define BuffExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_BUFFUTIL, p, x, e, s, __VA_ARGS__)
#define BuffExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_BUFFUTIL, p, x, s, __VA_ARGS__)
#define BuffExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_BUFFUTIL, p, x, e, s, __VA_ARGS__)
#define BuffExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_BUFFUTIL, p, x, s, __VA_ARGS__)
#define BuffExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_BUFFUTIL, e, x, s, __VA_ARGS__)
#define BuffExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_BUFFUTIL, g, x, s, __VA_ARGS__)


// constants

#define BUFFER_INCREMENT 128


// helper function declarations

static HRESULT EnsureBufferSize(
    __deref_inout_bcount(cbSize) BYTE** ppbBuffer,
    __in SIZE_T cbSize
    );


// functions

extern "C" HRESULT BuffReadNumber(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __out DWORD* pdw
    )
{
    Assert(pbBuffer);
    Assert(piBuffer);
    Assert(pdw);

    HRESULT hr = S_OK;
    SIZE_T cbAvailable = 0;

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size.");

    // verify buffer size
    if (sizeof(DWORD) > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small.");
    }

    *pdw = *(const DWORD*)(pbBuffer + *piBuffer);
    *piBuffer += sizeof(DWORD);

LExit:
    return hr;
}

extern "C" HRESULT BuffReadNumber64(
    __in_bcount(cbBuffer) const BYTE * pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __out DWORD64* pdw64
)
{
    Assert(pbBuffer);
    Assert(piBuffer);
    Assert(pdw64);

    HRESULT hr = S_OK;
    SIZE_T cbAvailable = 0;

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size.");

    // verify buffer size
    if (sizeof(DWORD64) > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small.");
    }

    *pdw64 = *(const DWORD64*)(pbBuffer + *piBuffer);
    *piBuffer += sizeof(DWORD64);

LExit:
    return hr;
}

extern "C" HRESULT BuffReadPointer(
    __in_bcount(cbBuffer) const BYTE * pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __out DWORD_PTR* pdw64
)
{
    Assert(pbBuffer);
    Assert(piBuffer);
    Assert(pdw64);

    HRESULT hr = S_OK;
    SIZE_T cbAvailable = 0;

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size.");

    // verify buffer size
    if (sizeof(DWORD_PTR) > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small.");
    }

    *pdw64 = *(const DWORD_PTR*)(pbBuffer + *piBuffer);
    *piBuffer += sizeof(DWORD_PTR);

LExit:
    return hr;
}

extern "C" HRESULT BuffReadString(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __deref_out_z LPWSTR* pscz
    )
{
    Assert(pbBuffer);
    Assert(piBuffer);
    Assert(pscz);

    HRESULT hr = S_OK;
    DWORD cch = 0;
    DWORD cb = 0;
    SIZE_T cbAvailable = 0;

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for character count.");

    // verify buffer size
    if (sizeof(DWORD) > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small.");
    }

    // read character count
    cch = *(const DWORD*)(pbBuffer + *piBuffer);

    hr = ::DWordMult(cch, static_cast<DWORD>(sizeof(WCHAR)), &cb);
    BuffExitOnRootFailure(hr, "Overflow while multiplying to calculate buffer size");

    hr = ::SIZETAdd(*piBuffer, sizeof(DWORD), piBuffer);
    BuffExitOnRootFailure(hr, "Overflow while adding to calculate buffer size");

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for character buffer.");

    // verify buffer size
    if (cb > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small to hold character data.");
    }

    // copy character data
    hr = StrAllocString(pscz, cch ? (LPCWSTR)(pbBuffer + *piBuffer) : L"", cch);
    BuffExitOnFailure(hr, "Failed to copy character data.");

    *piBuffer += cb;

LExit:
    return hr;
}

extern "C" HRESULT BuffReadStringAnsi(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __deref_out_z LPSTR* pscz
    )
{
    Assert(pbBuffer);
    Assert(piBuffer);
    Assert(pscz);

    HRESULT hr = S_OK;
    DWORD cch = 0;
    DWORD cb = 0;
    SIZE_T cbAvailable = 0;

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for character count.");

    // verify buffer size
    if (sizeof(DWORD) > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small.");
    }

    // read character count
    cch = *(const DWORD*)(pbBuffer + *piBuffer);

    hr = ::DWordMult(cch, static_cast<DWORD>(sizeof(CHAR)), &cb);
    BuffExitOnRootFailure(hr, "Overflow while multiplying to calculate buffer size");

    hr = ::SIZETAdd(*piBuffer, sizeof(DWORD), piBuffer);
    BuffExitOnRootFailure(hr, "Overflow while adding to calculate buffer size");

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for character buffer.");

    // verify buffer size
    if (cb > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small to hold character count.");
    }

    // copy character data
    hr = StrAnsiAllocStringAnsi(pscz, cch ? (LPCSTR)(pbBuffer + *piBuffer) : "", cch);
    BuffExitOnFailure(hr, "Failed to copy character data.");

    *piBuffer += cb;

LExit:
    return hr;
}

extern "C" HRESULT BuffReadStream(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __deref_inout_bcount(*pcbStream) BYTE** ppbStream,
    __out SIZE_T* pcbStream
    )
{
    Assert(pbBuffer);
    Assert(piBuffer);
    Assert(ppbStream);
    Assert(pcbStream);

    HRESULT hr = S_OK;
    DWORD64 cb = 0;
    SIZE_T cbAvailable = 0;

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for stream size.");

    // verify buffer size
    if (sizeof(DWORD64) > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small.");
    }

    // read stream size
    cb = *(const DWORD64*)(pbBuffer + *piBuffer);
    *piBuffer += sizeof(DWORD64);

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for stream buffer.");

    // verify buffer size
    if (cb > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small to hold byte count.");
    }

    // allocate buffer
    *ppbStream = (BYTE*)MemAlloc((SIZE_T)cb, TRUE);
    BuffExitOnNull(*ppbStream, hr, E_OUTOFMEMORY, "Failed to allocate stream.");

    // read stream data
    memcpy_s(*ppbStream, cbBuffer - *piBuffer, pbBuffer + *piBuffer, (SIZE_T)cb);
    *piBuffer += (SIZE_T)cb;

    // return stream size
    *pcbStream = (SIZE_T)cb;

LExit:
    return hr;
}

extern "C" HRESULT BuffWriteNumber(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in DWORD_PTR dw
    )
{
    Assert(ppbBuffer);
    Assert(piBuffer);

    HRESULT hr = S_OK;

    // make sure we have a buffer with sufficient space
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + sizeof(DWORD));
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy data to buffer
    *(DWORD_PTR*)(*ppbBuffer + *piBuffer) = dw;
    *piBuffer += sizeof(DWORD);

LExit:
    return hr;
}

extern "C" HRESULT BuffWriteNumber64(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in DWORD64 dw64
    )
{
    Assert(ppbBuffer);
    Assert(piBuffer);

    HRESULT hr = S_OK;

    // make sure we have a buffer with sufficient space
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + sizeof(DWORD64));
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy data to buffer
    *(DWORD64*)(*ppbBuffer + *piBuffer) = dw64;
    *piBuffer += sizeof(DWORD64);

LExit:
    return hr;
}

extern "C" HRESULT BuffWritePointer(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in DWORD_PTR dw
)
{
    Assert(ppbBuffer);
    Assert(piBuffer);

    HRESULT hr = S_OK;

    // make sure we have a buffer with sufficient space
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + sizeof(DWORD_PTR));
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy data to buffer
    *(DWORD_PTR*)(*ppbBuffer + *piBuffer) = dw;
    *piBuffer += sizeof(DWORD_PTR);

LExit:
    return hr;
}

extern "C" HRESULT BuffWriteString(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in_z_opt LPCWSTR scz
    )
{
    Assert(ppbBuffer);
    Assert(piBuffer);

    HRESULT hr = S_OK;
    DWORD cch = (DWORD)lstrlenW(scz);
    SIZE_T cb = cch * sizeof(WCHAR);

    // make sure we have a buffer with sufficient space
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + (sizeof(DWORD) + cb));
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy character count to buffer
    *(DWORD*)(*ppbBuffer + *piBuffer) = cch;
    *piBuffer += sizeof(DWORD);

    // copy data to buffer
    memcpy_s(*ppbBuffer + *piBuffer, cb, scz, cb);
    *piBuffer += cb;

LExit:
    return hr;
}

extern "C" HRESULT BuffWriteStringAnsi(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in_z_opt LPCSTR scz
    )
{
    Assert(ppbBuffer);
    Assert(piBuffer);

    HRESULT hr = S_OK;
    DWORD cch = (DWORD)lstrlenA(scz);
    SIZE_T cb = cch * sizeof(CHAR);

    // make sure we have a buffer with sufficient space
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + (sizeof(DWORD) + cb));
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy character count to buffer
    *(DWORD*)(*ppbBuffer + *piBuffer) = cch;
    *piBuffer += sizeof(DWORD);

    // copy data to buffer
    memcpy_s(*ppbBuffer + *piBuffer, cb, scz, cb);
    *piBuffer += cb;

LExit:
    return hr;
}

extern "C" HRESULT BuffWriteStream(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in_bcount(cbStream) const BYTE* pbStream,
    __in SIZE_T cbStream
    )
{
    Assert(ppbBuffer);
    Assert(piBuffer);
    Assert(pbStream);

    HRESULT hr = S_OK;
    DWORD64 cb = cbStream;

    // make sure we have a buffer with sufficient space
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + cbStream + sizeof(DWORD64));
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy byte count to buffer
    *(DWORD64*)(*ppbBuffer + *piBuffer) = cb;
    *piBuffer += sizeof(DWORD64);

    // copy data to buffer
    memcpy_s(*ppbBuffer + *piBuffer, cbStream, pbStream, cbStream);
    *piBuffer += cbStream;

LExit:
    return hr;
}


// helper functions

static HRESULT EnsureBufferSize(
    __deref_inout_bcount(cbSize) BYTE** ppbBuffer,
    __in SIZE_T cbSize
    )
{
    HRESULT hr = S_OK;
    SIZE_T cbTarget = ((cbSize / BUFFER_INCREMENT) + 1) * BUFFER_INCREMENT;

    if (*ppbBuffer)
    {
        if (MemSize(*ppbBuffer) < cbTarget)
        {
            LPVOID pv = MemReAlloc(*ppbBuffer, cbTarget, TRUE);
            BuffExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to reallocate buffer.");
            *ppbBuffer = (BYTE*)pv;
        }
    }
    else
    {
        *ppbBuffer = (BYTE*)MemAlloc(cbTarget, TRUE);
        BuffExitOnNull(*ppbBuffer, hr, E_OUTOFMEMORY, "Failed to allocate buffer.");
    }

LExit:
    return hr;
}
