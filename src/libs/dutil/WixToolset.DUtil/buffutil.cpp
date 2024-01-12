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

// Buffer read functions

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
        BuffExitOnRootFailure(hr, "Buffer too small to read number. cbAvailable: %u", cbAvailable);
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
        BuffExitOnRootFailure(hr, "Buffer too small to read 64-bit number. cbAvailable: %u", cbAvailable);
    }

    *pdw64 = *(const DWORD64*)(pbBuffer + *piBuffer);
    *piBuffer += sizeof(DWORD64);

LExit:
    return hr;
}

extern "C" HRESULT BuffReadPointer(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
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
        BuffExitOnRootFailure(hr, "Buffer too small to read pointer. cbAvailable: %u", cbAvailable);
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
    SIZE_T cb = 0;
    SIZE_T cbAvailable = 0;

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for character count.");

    // verify buffer size
    if (sizeof(DWORD) > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small to read size of string. cbAvailable: %u", cbAvailable);
    }

    // read character count
    cch = *(const DWORD*)(pbBuffer + *piBuffer);

    hr = ::SIZETMult(cch, sizeof(WCHAR), &cb);
    BuffExitOnRootFailure(hr, "Overflow while multiplying to calculate buffer size");

    hr = ::SIZETAdd(*piBuffer, sizeof(cch), piBuffer);
    BuffExitOnRootFailure(hr, "Overflow while adding to calculate buffer size");

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for character buffer.");

    // verify buffer size
    if (cb > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small to read string data. cbAvailable: %u, cb: %u", cbAvailable, cb);
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
    SIZE_T cb = 0;
    SIZE_T cbAvailable = 0;

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for character count.");

    // verify buffer size
    if (sizeof(DWORD) > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small to read size of ANSI string. cbAvailable: %u", cbAvailable);
    }

    // read character count
    cch = *(const DWORD*)(pbBuffer + *piBuffer);

    hr = ::SIZETMult(cch, sizeof(CHAR), &cb);
    BuffExitOnRootFailure(hr, "Overflow while multiplying to calculate buffer size");

    hr = ::SIZETAdd(*piBuffer, sizeof(cch), piBuffer);
    BuffExitOnRootFailure(hr, "Overflow while adding to calculate buffer size");

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for character buffer.");

    // verify buffer size
    if (cb > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small to read ANSI string data. cbAvailable: %u, cb: %u", cbAvailable, cb);
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
    SIZE_T cb = 0;
    SIZE_T cbAvailable = 0;
    errno_t err = 0;

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for stream size.");

    // verify buffer size
    if (sizeof(DWORD) > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small to read size of stream. cbAvailable: %u, cb: %u", cbAvailable, cb);
    }

    // read stream size
    cb = *(const DWORD*)(pbBuffer + *piBuffer);
    *piBuffer += sizeof(DWORD);

    // get availiable data size
    hr = ::SIZETSub(cbBuffer, *piBuffer, &cbAvailable);
    BuffExitOnRootFailure(hr, "Failed to calculate available data size for stream buffer.");

    // verify buffer size
    if (cb > cbAvailable)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Buffer too small to read stream data. cbAvailable: %u, cb: %u", cbAvailable, cb);
    }

    // allocate buffer
    *ppbStream = (BYTE*)MemAlloc(cb, TRUE);
    BuffExitOnNull(*ppbStream, hr, E_OUTOFMEMORY, "Failed to allocate stream.");

    // read stream data
    err = memcpy_s(*ppbStream, cbBuffer - *piBuffer, pbBuffer + *piBuffer, cb);
    if (err)
    {
        BuffExitOnRootFailure(hr = E_INVALIDARG, "Failed to read stream from buffer, error: %d", err);
    }

    *piBuffer += cb;

    // return stream size
    *pcbStream = cb;

LExit:
    return hr;
}


// Buffer Reader read functions

extern "C" HRESULT BuffReaderReadNumber(
    __in BUFF_READER* pReader,
    __out DWORD* pdw
    )
{
    return BuffReadNumber(pReader->pbData, pReader->cbData, &pReader->iBuffer, pdw);
}

extern "C" HRESULT BuffReaderReadNumber64(
    __in BUFF_READER* pReader,
    __out DWORD64* pdw64
    )
{
    return BuffReadNumber64(pReader->pbData, pReader->cbData, &pReader->iBuffer, pdw64);
}

extern "C" HRESULT BuffReaderReadPointer(
    __in BUFF_READER* pReader,
    __out DWORD_PTR* pdw
    )
{
    return BuffReadPointer(pReader->pbData, pReader->cbData, &pReader->iBuffer, pdw);
}

extern "C" HRESULT BuffReaderReadString(
    __in BUFF_READER* pReader,
    __deref_out_z LPWSTR* pscz
    )
{
    return BuffReadString(pReader->pbData, pReader->cbData, &pReader->iBuffer, pscz);
}

extern "C" HRESULT BuffReaderReadStringAnsi(
    __in BUFF_READER* pReader,
    __deref_out_z LPSTR* pscz
    )
{
    return BuffReadStringAnsi(pReader->pbData, pReader->cbData, &pReader->iBuffer, pscz);
}


// Buffer write functions

extern "C" HRESULT BuffWriteNumber(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in DWORD dw
    )
{
    Assert(ppbBuffer);
    Assert(piBuffer);

    HRESULT hr = S_OK;

    // make sure we have a buffer with sufficient space
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + sizeof(DWORD));
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy data to buffer
    *reinterpret_cast<DWORD*>(*ppbBuffer + *piBuffer) = dw;
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
    DWORD cch = 0; // This value *MUST* be treated as a DWORD to be marshalled over the pipe between 32-bit and 64-bit process the same.
    SIZE_T cb = 0;
    errno_t err = 0;

    if (scz)
    {
        size_t size = 0;

        hr = ::StringCchLengthW(scz, STRSAFE_MAX_CCH, &size);
        BuffExitOnRootFailure(hr, "Failed to get string size.");

        if (size > DWORD_MAX)
        {
            hr = E_INVALIDARG;
            BuffExitOnRootFailure(hr, "String too long to write to buffer.");
        }

        cch = static_cast<DWORD>(size);
    }

    cb = cch * sizeof(WCHAR);

    // make sure we have a buffer with sufficient space for the length plus the string without terminator.
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + sizeof(DWORD) + cb);
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy the character count to buffer as a DWORD
    *reinterpret_cast<DWORD*>(*ppbBuffer + *piBuffer) = cch;
    *piBuffer += sizeof(DWORD);

    // copy data to buffer
    err = memcpy_s(*ppbBuffer + *piBuffer, cb, scz, cb);
    if (err)
    {
        BuffExitOnRootFailure(hr = E_INVALIDARG, "Failed to write string to buffer: '%ls', error: %d", scz, err);
    }

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
    DWORD cch = 0; // This value *MUST* be treated as a DWORD to be marshalled over the pipe between 32-bit and 64-bit process the same.
    SIZE_T cb = 0;
    errno_t err = 0;

    if (scz)
    {
        size_t size = 0;

        hr = ::StringCchLengthA(scz, STRSAFE_MAX_CCH, &size);
        BuffExitOnRootFailure(hr, "Failed to get ANSI string size.")

        if (size > DWORD_MAX)
        {
            hr = E_INVALIDARG;
            BuffExitOnRootFailure(hr, "ANSI string too long to write to buffer.");
        }

        cch = static_cast<DWORD>(size);
    }

    cb = cch * sizeof(CHAR);

    // make sure we have a buffer with sufficient space
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + (sizeof(DWORD) + cb));
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy character count to buffer
    *reinterpret_cast<DWORD*>(*ppbBuffer + *piBuffer) = cch;
    *piBuffer += sizeof(DWORD);

    // copy data to buffer
    err = memcpy_s(*ppbBuffer + *piBuffer, cb, scz, cb);
    if (err)
    {
        BuffExitOnRootFailure(hr = E_INVALIDARG, "Failed to write string to buffer: '%hs', error: %d", scz, err);
    }

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
    DWORD cb = 0;
    errno_t err = 0;

    if (cbStream > DWORD_MAX)
    {
        hr = E_INVALIDARG;
        BuffExitOnRootFailure(hr, "Stream too large to write to buffer.");
    }

    cb = static_cast<DWORD>(cbStream);

    // make sure we have a buffer with sufficient space
    hr = EnsureBufferSize(ppbBuffer, *piBuffer + cbStream + sizeof(DWORD));
    BuffExitOnFailure(hr, "Failed to ensure buffer size.");

    // copy byte count to buffer
    *reinterpret_cast<DWORD*>(*ppbBuffer + *piBuffer) = cb;
    *piBuffer += sizeof(DWORD);

    if (cbStream)
    {
        // copy data to buffer
        err = memcpy_s(*ppbBuffer + *piBuffer, cbStream, pbStream, cbStream);
        if (err)
        {
            BuffExitOnRootFailure(hr = E_INVALIDARG, "Failed to write stream to buffer, error: %d", err);
        }

        *piBuffer += cbStream;
    }

LExit:
    return hr;
}

// Buffer-based write functions

extern "C" HRESULT BuffWriteNumberToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __out DWORD dw
    )
{
    return BuffWriteNumber(&pBuffer->pbData, &pBuffer->cbData, dw);
}

extern "C" HRESULT BuffWriteNumber64ToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __out DWORD64 dw64
    )
{
    return BuffWriteNumber64(&pBuffer->pbData, &pBuffer->cbData, dw64);
}

extern "C" HRESULT BuffWritePointerToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __out DWORD_PTR dw
    )
{
    return BuffWritePointer(&pBuffer->pbData, &pBuffer->cbData, dw);
}

extern "C" HRESULT BuffWriteStringToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __in_z_opt LPCWSTR scz
    )
{
    return BuffWriteString(&pBuffer->pbData, &pBuffer->cbData, scz);
}

extern "C" HRESULT BuffWriteStringAnsiToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __in_z_opt LPCSTR scz
    )
{
    return BuffWriteStringAnsi(&pBuffer->pbData, &pBuffer->cbData, scz);
}

extern "C" HRESULT BuffWriteStreamToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __in_bcount(cbStream) const BYTE* pbStream,
    __in SIZE_T cbStream
    )
{
    return BuffWriteStream(&pBuffer->pbData, &pBuffer->cbData, pbStream, cbStream);
}

// Buffer Writer write functions

extern "C" HRESULT BuffWriterWriteNumber(
    __in BUFF_WRITER* pWriter,
    __out DWORD dw
    )
{
    return BuffWriteNumber(pWriter->ppbData, pWriter->pcbData, dw);
}

extern "C" HRESULT BuffWriterWriteNumber64(
    __in BUFF_WRITER* pWriter,
    __out DWORD64 dw64
    )
{
    return BuffWriteNumber64(pWriter->ppbData, pWriter->pcbData, dw64);
}

extern "C" HRESULT BuffWriterWritePointer(
    __in BUFF_WRITER* pWriter,
    __out DWORD_PTR dw
    )
{
    return BuffWritePointer(pWriter->ppbData, pWriter->pcbData, dw);
}

extern "C" HRESULT BuffWriterWriteString(
    __in BUFF_WRITER* pWriter,
    __in_z_opt LPCWSTR scz
    )
{
    return BuffWriteString(pWriter->ppbData, pWriter->pcbData, scz);
}

extern "C" HRESULT BuffWriterWriteStringAnsi(
    __in BUFF_WRITER* pWriter,
    __in_z_opt LPCSTR scz
    )
{
    return BuffWriteStringAnsi(pWriter->ppbData, pWriter->pcbData, scz);
}

extern "C" HRESULT BuffWriterWriteStream(
    __in BUFF_WRITER* pWriter,
    __in_bcount(cbStream) const BYTE* pbStream,
    __in SIZE_T cbStream
    )
{
    return BuffWriteStream(pWriter->ppbData, pWriter->pcbData, pbStream, cbStream);
}


// helper functions

static HRESULT EnsureBufferSize(
    __deref_inout_bcount(cbSize) BYTE** ppbBuffer,
    __in SIZE_T cbSize
    )
{
    HRESULT hr = S_OK;
    SIZE_T cbTarget = ((cbSize / BUFFER_INCREMENT) + 1) * BUFFER_INCREMENT;
    SIZE_T cbCurrent = 0;

    if (*ppbBuffer)
    {
        hr = MemSizeChecked(*ppbBuffer, &cbCurrent);
        BuffExitOnFailure(hr, "Failed to get current buffer size.");

        if (cbCurrent < cbTarget)
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
