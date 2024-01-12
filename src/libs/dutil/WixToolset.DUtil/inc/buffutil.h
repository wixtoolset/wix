#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif


// macro definitions

#define ReleaseBuffer(b) BuffFree(b)
#define ReleaseNullBuffer(b) BuffFree(b)
#define BuffFree(b) if (b.pbData) { MemFree(b.pbData); b.pbData = NULL; } b.cbData = 0


// structs

// A buffer that owns its data and must be freed with BuffFree().
typedef struct _BUFF_BUFFER
{
    LPBYTE pbData;
    SIZE_T cbData;
} BUFF_BUFFER;

// A read-only buffer with internal pointer that can be advanced for multiple reads.
typedef struct _BUFF_READER
{
    LPCBYTE pbData;
    SIZE_T cbData;

    SIZE_T iBuffer;
} BUFF_READER;

// A write buffer that does not own its data.
typedef struct _BUFF_WRITER
{
    LPBYTE *ppbData;
    SIZE_T *pcbData;
} BUFF_WRITER;


// function declarations

HRESULT BuffReadNumber(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __out DWORD* pdw
    );
HRESULT BuffReadNumber64(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __out DWORD64* pdw64
    );
HRESULT BuffReadPointer(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __out DWORD_PTR* pdw
);
HRESULT BuffReadString(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __deref_out_z LPWSTR* pscz
    );
HRESULT BuffReadStringAnsi(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __deref_out_z LPSTR* pscz
    );
HRESULT BuffReadStream(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer,
    __deref_inout_bcount(*pcbStream) BYTE** ppbStream,
    __out SIZE_T* pcbStream
    );
HRESULT BuffSkipExtraData(
    __in SIZE_T cbExpectedSize,
    __in SIZE_T cbActualSize,
    __inout SIZE_T* piBuffer
    );

HRESULT BuffReaderReadNumber(
    __in BUFF_READER* pReader,
    __out DWORD* pdw
    );
HRESULT BuffReaderReadNumber64(
    __in BUFF_READER* pReader,
    __out DWORD64* pdw64
    );
HRESULT BuffReaderReadPointer(
    __in BUFF_READER* pReader,
    __out DWORD_PTR* pdw
);
HRESULT BuffReaderReadString(
    __in BUFF_READER* pReader,
    __deref_out_z LPWSTR* pscz
    );
HRESULT BuffReaderReadStringAnsi(
    __in BUFF_READER* pReader,
    __deref_out_z LPSTR* pscz
    );
HRESULT BuffReaderReadStream(
    __in BUFF_READER* pReader,
    __deref_inout_bcount(*pcbStream) BYTE** ppbStream,
    __out SIZE_T* pcbStream
    );

HRESULT BuffWriteNumber(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in DWORD dw
    );
HRESULT BuffWriteNumber64(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in DWORD64 dw64
    );
HRESULT BuffWritePointer(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in DWORD_PTR dw
);
HRESULT BuffWriteString(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in_z_opt LPCWSTR scz
    );
HRESULT BuffWriteStringAnsi(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in_z_opt LPCSTR scz
    );
HRESULT BuffWriteStream(
    __deref_inout_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer,
    __in_bcount(cbStream) const BYTE* pbStream,
    __in SIZE_T cbStream
    );

HRESULT BuffWriteNumberToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __in DWORD dw
    );
HRESULT BuffWriteNumber64ToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __in DWORD64 dw64
    );
HRESULT BuffWritePointerToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __in DWORD_PTR dw
    );
HRESULT BuffWriteStringToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __in_z_opt LPCWSTR scz
    );
HRESULT BuffWriteStringAnsiToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __in_z_opt LPCSTR scz
    );
HRESULT BuffWriteStreamToBuffer(
    __in BUFF_BUFFER* pBuffer,
    __in_bcount(cbStream) const BYTE* pbStream,
    __in SIZE_T cbStream
    );

HRESULT BuffWriterWriteNumber(
    __in BUFF_WRITER* pWriter,
    __in DWORD dw
    );
HRESULT BuffWriterWriteNumber64(
    __in BUFF_WRITER* pWriter,
    __in DWORD64 dw64
    );
HRESULT BuffWriterWritePointer(
    __in BUFF_WRITER* pWriter,
    __in DWORD_PTR dw
    );
HRESULT BuffWriterWriteString(
    __in BUFF_WRITER* pWriter,
    __in_z_opt LPCWSTR scz
    );
HRESULT BuffWriterWriteStringAnsi(
    __in BUFF_WRITER* pWriter,
    __in_z_opt LPCSTR scz
    );
HRESULT BuffWriterWriteStream(
    __in BUFF_WRITER* pWriter,
    __in_bcount(cbStream) const BYTE* pbStream,
    __in SIZE_T cbStream
    );

#ifdef __cplusplus
}
#endif
