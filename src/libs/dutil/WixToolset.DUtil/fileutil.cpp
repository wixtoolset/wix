// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define FileExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_FILEUTIL, x, s, __VA_ARGS__)
#define FileExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_FILEUTIL, p, x, e, s, __VA_ARGS__)
#define FileExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_FILEUTIL, p, x, s, __VA_ARGS__)
#define FileExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_FILEUTIL, p, x, e, s, __VA_ARGS__)
#define FileExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_FILEUTIL, p, x, s, __VA_ARGS__)
#define FileExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_FILEUTIL, e, x, s, __VA_ARGS__)
#define FileExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_FILEUTIL, g, x, s, __VA_ARGS__)

// constants

const BYTE UTF8BOM[] = {0xEF, 0xBB, 0xBF};
const BYTE UTF16BOM[] = {0xFF, 0xFE};


/*******************************************************************
FileStripExtension - Strip extension from filename
********************************************************************/
extern "C" HRESULT DAPI FileStripExtension(
__in_z LPCWSTR wzFileName,
__out LPWSTR *ppwzFileNameNoExtension
)
{
    Assert(wzFileName && *wzFileName);
   
    HRESULT hr = S_OK;
    size_t cchFileName = 0;
    LPWSTR pwzFileNameNoExtension = NULL;
    size_t cchFileNameNoExtension = 0;
    errno_t err = 0;

    hr = ::StringCchLengthW(wzFileName, STRSAFE_MAX_LENGTH, &cchFileName);
    FileExitOnRootFailure(hr, "failed to get length of file name: %ls", wzFileName);

    cchFileNameNoExtension = cchFileName + 1;

    hr = StrAlloc(&pwzFileNameNoExtension, cchFileNameNoExtension);
    FileExitOnFailure(hr, "failed to allocate space for File Name without extension");

    // _wsplitpath_s can handle drive/path/filename/extension
    err = _wsplitpath_s(wzFileName, NULL, NULL, NULL, NULL, pwzFileNameNoExtension, cchFileNameNoExtension, NULL, NULL);
    if (err)
    {
        hr = E_INVALIDARG;
        FileExitOnRootFailure(hr, "failed to parse filename: '%ls', error: %d", wzFileName, err);
    }

    *ppwzFileNameNoExtension = pwzFileNameNoExtension;
    pwzFileNameNoExtension = NULL;

LExit:
    ReleaseStr(pwzFileNameNoExtension);

    return hr;
}


/*******************************************************************
FileChangeExtension - Changes the extension of a filename
********************************************************************/
extern "C" HRESULT DAPI FileChangeExtension(
    __in_z LPCWSTR wzFileName,
    __in_z LPCWSTR wzNewExtension,
    __out LPWSTR *ppwzFileNameNewExtension
    )
{
    Assert(wzFileName && *wzFileName);

    HRESULT hr = S_OK;
    LPWSTR sczFileName = NULL;

    hr = FileStripExtension(wzFileName, &sczFileName);
    FileExitOnFailure(hr, "Failed to strip extension from file name: %ls", wzFileName);

    hr = StrAllocConcat(&sczFileName, wzNewExtension, 0);
    FileExitOnFailure(hr, "Failed to add new extension.");

    *ppwzFileNameNewExtension = sczFileName;
    sczFileName = NULL;

LExit:
    ReleaseStr(sczFileName);
   
    return hr;
}


/*******************************************************************
FileAddSuffixToBaseName - Adds a suffix the base portion of a file
name; e.g., file.ext to fileSuffix.ext.
********************************************************************/
extern "C" HRESULT DAPI FileAddSuffixToBaseName(
    __in_z LPCWSTR wzFileName,
    __in_z LPCWSTR wzSuffix,
    __out_z LPWSTR* psczNewFileName
    )
{
    Assert(wzFileName && *wzFileName);

    HRESULT hr = S_OK;
    LPWSTR sczNewFileName = NULL;
    size_t cchFileName = 0;

    hr = ::StringCchLengthW(wzFileName, STRSAFE_MAX_CCH, &cchFileName);
    FileExitOnRootFailure(hr, "Failed to get length of file name: %ls", wzFileName);

    LPCWSTR wzExtension = wzFileName + cchFileName;
    while (wzFileName < wzExtension && L'.' != *wzExtension)
    {
        --wzExtension;
    }

    if (wzFileName < wzExtension)
    {
        // found an extension so add the suffix before it
        hr = StrAllocFormatted(&sczNewFileName, L"%.*ls%ls%ls", static_cast<int>(wzExtension - wzFileName), wzFileName, wzSuffix, wzExtension);
    }
    else
    {
        // no extension, so add the suffix at the end of the whole name
        hr = StrAllocString(&sczNewFileName, wzFileName, 0);
        FileExitOnFailure(hr, "Failed to allocate new file name.");

        hr = StrAllocConcat(&sczNewFileName, wzSuffix, 0);
    }
    FileExitOnFailure(hr, "Failed to allocate new file name with suffix.");

    *psczNewFileName = sczNewFileName;
    sczNewFileName = NULL;

LExit:
    ReleaseStr(sczNewFileName);
   
    return hr;
}


/*******************************************************************
 FileVersion

********************************************************************/
extern "C" HRESULT DAPI FileVersion(
    __in_z LPCWSTR wzFilename,
    __out DWORD *pdwVerMajor,
    __out DWORD* pdwVerMinor
    )
{
    HRESULT hr = S_OK;

    DWORD dwHandle = 0;
    UINT cbVerBuffer = 0;
    LPVOID pVerBuffer = NULL;
    VS_FIXEDFILEINFO* pvsFileInfo = NULL;
    UINT cbFileInfo = 0;

    if (0 == (cbVerBuffer = ::GetFileVersionInfoSizeW(wzFilename, &dwHandle)))
    {
        FileExitOnLastErrorDebugTrace(hr, "failed to get version info for file: %ls", wzFilename);
    }

    pVerBuffer = ::GlobalAlloc(GMEM_FIXED, cbVerBuffer);
    FileExitOnNullDebugTrace(pVerBuffer, hr, E_OUTOFMEMORY, "failed to allocate version info for file: %ls", wzFilename);

    if (!::GetFileVersionInfoW(wzFilename, dwHandle, cbVerBuffer, pVerBuffer))
    {
        FileExitOnLastErrorDebugTrace(hr, "failed to get version info for file: %ls", wzFilename);
    }

    if (!::VerQueryValueW(pVerBuffer, L"\\", (void**)&pvsFileInfo, &cbFileInfo))
    {
        FileExitOnLastErrorDebugTrace(hr, "failed to get version value for file: %ls", wzFilename);
    }

    *pdwVerMajor = pvsFileInfo->dwFileVersionMS;
    *pdwVerMinor = pvsFileInfo->dwFileVersionLS;

LExit:
    if (pVerBuffer)
    {
        ::GlobalFree(pVerBuffer);
    }
    return hr;
}


/*******************************************************************
 FileVersionFromString

*******************************************************************/
extern "C" HRESULT DAPI FileVersionFromString(
    __in_z LPCWSTR wzVersion,
    __out DWORD* pdwVerMajor,
    __out DWORD* pdwVerMinor
    )
{
    Assert(pdwVerMajor && pdwVerMinor);

    HRESULT hr = S_OK;
    LPCWSTR pwz = wzVersion;
    DWORD dw;

    *pdwVerMajor = 0;
    *pdwVerMinor = 0;

    if ((L'v' == *pwz) || (L'V' == *pwz))
    {
        ++pwz;
    }

    dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
    if (pwz && (L'.' == *pwz && dw < 0x10000) || !*pwz)
    {
        *pdwVerMajor = dw << 16;

        if (!*pwz)
        {
            ExitFunction1(hr = S_OK);
        }
        ++pwz;
    }
    else
    {
        ExitFunction1(hr = S_FALSE);
    }

    dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
    if (pwz && (L'.' == *pwz && dw < 0x10000) || !*pwz)
    {
        *pdwVerMajor |= dw;

        if (!*pwz)
        {
            ExitFunction1(hr = S_OK);
        }
        ++pwz;
    }
    else
    {
        ExitFunction1(hr = S_FALSE);
    }

    dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
    if (pwz && (L'.' == *pwz && dw < 0x10000) || !*pwz)
    {
        *pdwVerMinor = dw << 16;

        if (!*pwz)
        {
            ExitFunction1(hr = S_OK);
        }
        ++pwz;
    }
    else
    {
        ExitFunction1(hr = S_FALSE);
    }

    dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
    if (pwz && L'\0' == *pwz && dw < 0x10000)
    {
        *pdwVerMinor |= dw;
    }
    else
    {
        ExitFunction1(hr = S_FALSE);
    }

LExit:
    return hr;
}


/*******************************************************************
 FileVersionFromStringEx

*******************************************************************/
extern "C" HRESULT DAPI FileVersionFromStringEx(
    __in_z LPCWSTR wzVersion,
    __in SIZE_T cchVersion,
    __out DWORD64* pqwVersion
    )
{
    Assert(wzVersion);
    Assert(pqwVersion);

    HRESULT hr = S_OK;
    LPCWSTR wzEnd = NULL;
    LPCWSTR wzPartBegin = wzVersion;
    LPCWSTR wzPartEnd = wzVersion;
    DWORD iPart = 0;
    USHORT us = 0;
    DWORD64 qwVersion = 0;

    // get string length if not provided
    if (0 >= cchVersion)
    {
        hr = ::StringCchLengthW(wzVersion, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cchVersion));
        FileExitOnRootFailure(hr, "Failed to get length of file version string: %ls", wzVersion);

        if (0 >= cchVersion)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
    }

    if ((L'v' == *wzVersion) || (L'V' == *wzVersion))
    {
        ++wzVersion;
        --cchVersion;
        wzPartBegin = wzVersion;
        wzPartEnd = wzVersion;
    }

    // save end pointer
    wzEnd = wzVersion + cchVersion;

    // loop through parts
    for (;;)
    {
        if (4 <= iPart)
        {
            // error, too many parts
            ExitFunction1(hr = E_INVALIDARG);
        }

        // find end of part
        while (wzPartEnd < wzEnd && L'.' != *wzPartEnd)
        {
            ++wzPartEnd;
        }
        if (wzPartBegin == wzPartEnd)
        {
            // error, empty part
            ExitFunction1(hr = E_INVALIDARG);
        }

        DWORD cchPart;
        hr = ::PtrdiffTToDWord(wzPartEnd - wzPartBegin, &cchPart);
        FileExitOnFailure(hr, "Version number part was too long.");

        // parse version part
        hr = StrStringToUInt16(wzPartBegin, cchPart, &us);
        FileExitOnFailure(hr, "Failed to parse version number part.");

        // add part to qword version
        qwVersion |= (DWORD64)us << ((3 - iPart) * 16);

        if (wzPartEnd >= wzEnd)
        {
            // end of string
            break;
        }

        wzPartBegin = ++wzPartEnd; // skip over separator
        ++iPart;
    }

    *pqwVersion = qwVersion;

LExit:
    return hr;
}

/*******************************************************************
 FileVersionFromStringEx - Formats the DWORD64 as a string version.

*******************************************************************/
extern "C" HRESULT DAPI FileVersionToStringEx(
    __in DWORD64 qwVersion,
    __out LPWSTR* psczVersion
    )
{
    HRESULT hr = S_OK;
    WORD wMajor = 0;
    WORD wMinor = 0;
    WORD wBuild = 0;
    WORD wRevision = 0;

    // Mask and shift each WORD for each field.
    wMajor = (WORD)(qwVersion >> 48 & 0xffff);
    wMinor = (WORD)(qwVersion >> 32 & 0xffff);
    wBuild = (WORD)(qwVersion >> 16 & 0xffff);
    wRevision = (WORD)(qwVersion & 0xffff);

    // Format and return the version string.
    hr = StrAllocFormatted(psczVersion, L"%u.%u.%u.%u", wMajor, wMinor, wBuild, wRevision);
    FileExitOnFailure(hr, "Failed to allocate and format the version number.");

LExit:
    return hr;
}

/*******************************************************************
 FileSetPointer - sets the file pointer.

********************************************************************/
extern "C" HRESULT DAPI FileSetPointer(
    __in HANDLE hFile,
    __in DWORD64 dw64Move,
    __out_opt DWORD64* pdw64NewPosition,
    __in DWORD dwMoveMethod
    )
{
    Assert(INVALID_HANDLE_VALUE != hFile);

    HRESULT hr = S_OK;
    LARGE_INTEGER liMove = { };
    LARGE_INTEGER liNewPosition = { };

    liMove.QuadPart = dw64Move;
    if (!::SetFilePointerEx(hFile, liMove, &liNewPosition, dwMoveMethod))
    {
        FileExitWithLastError(hr, "Failed to set file pointer.");
    }

    if (pdw64NewPosition)
    {
        *pdw64NewPosition = liNewPosition.QuadPart;
    }

LExit:
    return hr;
}


/*******************************************************************
 FileSize

********************************************************************/
extern "C" HRESULT DAPI FileSize(
    __in_z LPCWSTR pwzFileName,
    __out LONGLONG* pllSize
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    FileExitOnNull(pwzFileName, hr, E_INVALIDARG, "Attempted to check filename, but no filename was provided");

    hFile = ::CreateFileW(pwzFileName, FILE_READ_ATTRIBUTES, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        er = ::GetLastError();
        if (ERROR_PATH_NOT_FOUND == er || ERROR_FILE_NOT_FOUND == er)
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(er));
        }
        FileExitWithLastError(hr, "Failed to open file %ls while checking file size", pwzFileName);
    }

    hr = FileSizeByHandle(hFile, pllSize);
    FileExitOnFailure(hr, "Failed to check size of file %ls by handle", pwzFileName);

LExit:
    ReleaseFileHandle(hFile);

    return hr;
}


/*******************************************************************
 FileSizeByHandle

********************************************************************/
extern "C" HRESULT DAPI FileSizeByHandle(
    __in HANDLE hFile,
    __out LONGLONG* pllSize
    )
{
    Assert(INVALID_HANDLE_VALUE != hFile && pllSize);
    HRESULT hr = S_OK;
    LARGE_INTEGER li;

    *pllSize = 0;

    if (!::GetFileSizeEx(hFile, &li))
    {
        FileExitWithLastError(hr, "Failed to get size of file.");
    }

    *pllSize = li.QuadPart;

LExit:
    return hr;
}


/*******************************************************************
 FileExistsEx

********************************************************************/
extern "C" BOOL DAPI FileExistsEx(
    __in_z LPCWSTR wzPath,
    __out_opt DWORD *pdwAttributes
    )
{
    Assert(wzPath && *wzPath);
    BOOL fExists = FALSE;

    WIN32_FIND_DATAW fd = { };
    HANDLE hff;

    if (INVALID_HANDLE_VALUE != (hff = ::FindFirstFileW(wzPath, &fd)))
    {
        ::FindClose(hff);
        if (!(fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
        {
            if (pdwAttributes)
            {
                *pdwAttributes = fd.dwFileAttributes;
            }

            fExists = TRUE;
        }
    }

    return fExists;
}


/*******************************************************************
 FileRead - read a file into memory

********************************************************************/
extern "C" HRESULT DAPI FileRead(
    __deref_out_bcount_full(*pcbDest) LPBYTE* ppbDest,
    __out SIZE_T* pcbDest,
    __in_z LPCWSTR wzSrcPath
    )
{
    HRESULT hr = FileReadPartial(ppbDest, pcbDest, wzSrcPath, FALSE, 0, 0xFFFFFFFF, FALSE);
    return hr;
}

/*******************************************************************
 FileRead - read a file into memory with specified share mode

********************************************************************/
extern "C" HRESULT DAPI FileReadEx(
    __deref_out_bcount_full(*pcbDest) LPBYTE* ppbDest,
    __out SIZE_T* pcbDest,
    __in_z LPCWSTR wzSrcPath,
    __in DWORD dwShareMode
    )
{
    HRESULT hr = FileReadPartialEx(ppbDest, pcbDest, wzSrcPath, FALSE, 0, 0xFFFFFFFF, FALSE, dwShareMode);
    return hr;
}

/*******************************************************************
 FileReadUntil - read a file into memory with a maximum size

********************************************************************/
extern "C" HRESULT DAPI FileReadUntil(
    __deref_out_bcount_full(*pcbDest) LPBYTE* ppbDest,
    __out_range(<=, cbMaxRead) SIZE_T* pcbDest,
    __in_z LPCWSTR wzSrcPath,
    __in DWORD cbMaxRead
    )
{
    HRESULT hr = FileReadPartial(ppbDest, pcbDest, wzSrcPath, FALSE, 0, cbMaxRead, FALSE);
    return hr;
}


/*******************************************************************
 FileReadPartial - read a portion of a file into memory

********************************************************************/
extern "C" HRESULT DAPI FileReadPartial(
    __deref_out_bcount_full(*pcbDest) LPBYTE* ppbDest,
    __out_range(<=, cbMaxRead) SIZE_T* pcbDest,
    __in_z LPCWSTR wzSrcPath,
    __in BOOL fSeek,
    __in DWORD cbStartPosition,
    __in DWORD cbMaxRead,
    __in BOOL fPartialOK
    )
{
    return FileReadPartialEx(ppbDest, pcbDest, wzSrcPath, fSeek, cbStartPosition, cbMaxRead, fPartialOK, FILE_SHARE_READ | FILE_SHARE_DELETE);
}

/*******************************************************************
 FileReadPartial - read a portion of a file into memory
                   (with specified share mode)
********************************************************************/
extern "C" HRESULT DAPI FileReadPartialEx(
    __deref_inout_bcount_full(*pcbDest) LPBYTE* ppbDest,
    __out_range(<=, cbMaxRead) SIZE_T* pcbDest,
    __in_z LPCWSTR wzSrcPath,
    __in BOOL fSeek,
    __in DWORD cbStartPosition,
    __in DWORD cbMaxRead,
    __in BOOL fPartialOK,
    __in DWORD dwShareMode
    )
{
    HRESULT hr = S_OK;

    UINT er = ERROR_SUCCESS;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    LARGE_INTEGER liFileSize = { };
    DWORD cbData = 0;
    BYTE* pbData = NULL;

    FileExitOnNull(pcbDest, hr, E_INVALIDARG, "Invalid argument pcbDest");
    FileExitOnNull(ppbDest, hr, E_INVALIDARG, "Invalid argument ppbDest");
    FileExitOnNull(wzSrcPath, hr, E_INVALIDARG, "Invalid argument wzSrcPath");
    FileExitOnNull(*wzSrcPath, hr, E_INVALIDARG, "*wzSrcPath is null");

    hFile = ::CreateFileW(wzSrcPath, GENERIC_READ, dwShareMode, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        er = ::GetLastError();
        if (ERROR_PATH_NOT_FOUND == er || ERROR_FILE_NOT_FOUND == er)
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(er));
        }
        FileExitWithLastError(hr, "Failed to open file: %ls", wzSrcPath);
    }

    if (!::GetFileSizeEx(hFile, &liFileSize))
    {
        FileExitWithLastError(hr, "Failed to get size of file: %ls", wzSrcPath);
    }

    if (fSeek)
    {
        if (cbStartPosition > liFileSize.QuadPart)
        {
            hr = E_INVALIDARG;
            FileExitOnFailure(hr, "Start position %d bigger than file '%ls' size %llu", cbStartPosition, wzSrcPath, liFileSize.QuadPart);
        }

        DWORD dwErr = ::SetFilePointer(hFile, cbStartPosition, NULL, FILE_CURRENT);
        if (INVALID_SET_FILE_POINTER == dwErr)
        {
            FileExitOnLastError(hr, "Failed to seek position %d", cbStartPosition);
        }
    }
    else
    {
        cbStartPosition = 0;
    }

    if (fPartialOK)
    {
        cbData = cbMaxRead;
    }
    else
    {
        cbData = liFileSize.LowPart - cbStartPosition; // should only need the low part because we cap at DWORD
        if (cbMaxRead < liFileSize.QuadPart - cbStartPosition)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            FileExitOnRootFailure(hr, "Failed to load file: %ls, too large.", wzSrcPath);
        }
    }

    if (*ppbDest)
    {
        if (0 == cbData)
        {
            ReleaseNullMem(*ppbDest);
            *pcbDest = 0;
            ExitFunction1(hr = S_OK);
        }

        LPVOID pv = MemReAlloc(*ppbDest, cbData, TRUE);
        FileExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to re-allocate memory to read in file: %ls", wzSrcPath);

        pbData = static_cast<BYTE*>(pv);
    }
    else
    {
        if (0 == cbData)
        {
            *pcbDest = 0;
            ExitFunction1(hr = S_OK);
        }

        pbData = static_cast<BYTE*>(MemAlloc(cbData, TRUE));
        FileExitOnNull(pbData, hr, E_OUTOFMEMORY, "Failed to allocate memory to read in file: %ls", wzSrcPath);
    }

    DWORD cbTotalRead = 0;
    DWORD cbRead = 0;
    do
    {
        DWORD cbRemaining = 0;
        hr = ::ULongSub(cbData, cbTotalRead, &cbRemaining);
        FileExitOnFailure(hr, "Underflow calculating remaining buffer size.");

        if (!::ReadFile(hFile, pbData + cbTotalRead, cbRemaining, &cbRead, NULL))
        {
            FileExitWithLastError(hr, "Failed to read from file: %ls", wzSrcPath);
        }

        cbTotalRead += cbRead;
    } while (cbRead);

    if (cbTotalRead != cbData)
    {
        hr = E_UNEXPECTED;
        FileExitOnFailure(hr, "Failed to completely read file: %ls", wzSrcPath);
    }

    *ppbDest = pbData;
    pbData = NULL;
    *pcbDest = cbData;

LExit:
    ReleaseMem(pbData);
    ReleaseFile(hFile);

    return hr;
}

extern "C" HRESULT DAPI FileReadHandle(
    __in HANDLE hFile,
    __in_bcount(cbDest) LPBYTE pbDest,
    __in SIZE_T cbDest
    )
{
    HRESULT hr = 0;
    DWORD cbDataRead = 0;
    SIZE_T cbRemaining = cbDest;
    SIZE_T cbTotal = 0;

    while (0 < cbRemaining)
    {
        if (!::ReadFile(hFile, pbDest + cbTotal, (DWORD)min(DWORD_MAX, cbRemaining), &cbDataRead, NULL))
        {
            DWORD er = ::GetLastError();
            if (ERROR_MORE_DATA == er)
            {
                hr = S_OK;
            }
            else
            {
                hr = HRESULT_FROM_WIN32(er);
            }
            FileExitOnRootFailure(hr, "Failed to read data from file handle.");
        }

        cbRemaining -= cbDataRead;
        cbTotal += cbDataRead;
    }

LExit:
    return hr;
}


/*******************************************************************
 FileWrite - write a file from memory

********************************************************************/
extern "C" HRESULT DAPI FileWrite(
    __in_z LPCWSTR pwzFileName,
    __in DWORD dwFlagsAndAttributes,
    __in_bcount_opt(cbData) LPCBYTE pbData,
    __in SIZE_T cbData,
    __out_opt HANDLE* pHandle
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // Open the file
    hFile = ::CreateFileW(pwzFileName, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, dwFlagsAndAttributes, NULL);
    FileExitOnInvalidHandleWithLastError(hFile, hr, "Failed to open file: %ls", pwzFileName);

    hr = FileWriteHandle(hFile, pbData, cbData);
    FileExitOnFailure(hr, "Failed to write to file: %ls", pwzFileName);

    if (pHandle)
    {
        *pHandle = hFile;
        hFile = INVALID_HANDLE_VALUE;
    }

LExit:
    ReleaseFile(hFile);

    return hr;
}


/*******************************************************************
 FileWriteHandle - write to a file handle from memory

********************************************************************/
extern "C" HRESULT DAPI FileWriteHandle(
    __in HANDLE hFile,
    __in_bcount_opt(cbData) LPCBYTE pbData,
    __in SIZE_T cbData
    )
{
    HRESULT hr = S_OK;
    DWORD cbDataWritten = 0;
    SIZE_T cbTotal = 0;
    SIZE_T cbRemaining = cbData;

    // Write out all of the data.
    while (0 < cbRemaining)
    {
        if (!::WriteFile(hFile, pbData + cbTotal, (DWORD)min(DWORD_MAX, cbRemaining), &cbDataWritten, NULL))
        {
            FileExitOnLastError(hr, "Failed to write data to file handle.");
        }

        cbRemaining -= cbDataWritten;
        cbTotal += cbDataWritten;
    }

LExit:
    return hr;
}


/*******************************************************************
 FileCopyUsingHandles

*******************************************************************/
extern "C" HRESULT DAPI FileCopyUsingHandles(
    __in HANDLE hSource,
    __in HANDLE hTarget,
    __in DWORD64 cbCopy,
    __out_opt DWORD64* pcbCopied
    )
{
    HRESULT hr = S_OK;
    DWORD64 cbTotalCopied = 0;
    BYTE rgbData[4 * 1024] = { };
    DWORD cbRead = 0;

    do
    {
        cbRead = static_cast<DWORD>((0 == cbCopy) ? countof(rgbData) : min(countof(rgbData), cbCopy - cbTotalCopied));
        if (!::ReadFile(hSource, rgbData, cbRead, &cbRead, NULL))
        {
            FileExitWithLastError(hr, "Failed to read from source.");
        }

        if (cbRead)
        {
            hr = FileWriteHandle(hTarget, rgbData, cbRead);
            FileExitOnFailure(hr, "Failed to write to target.");
        }

        cbTotalCopied += cbRead;
    } while (cbTotalCopied < cbCopy && 0 != cbRead);

    if (pcbCopied)
    {
        *pcbCopied = cbTotalCopied;
    }

LExit:
    return hr;
}


/*******************************************************************
 FileCopyUsingHandlesWithProgress

*******************************************************************/
extern "C" HRESULT DAPI FileCopyUsingHandlesWithProgress(
    __in HANDLE hSource,
    __in HANDLE hTarget,
    __in DWORD64 cbCopy,
    __in_opt LPPROGRESS_ROUTINE lpProgressRoutine,
    __in_opt LPVOID lpData
    )
{
    HRESULT hr = S_OK;
    DWORD64 cbTotalCopied = 0;
    BYTE rgbData[64 * 1024] = { };
    DWORD cbRead = 0;

    LARGE_INTEGER liSourceSize = { };
    LARGE_INTEGER liTotalCopied = { };
    LARGE_INTEGER liZero = { };
    DWORD dwResult = 0;

    hr = FileSizeByHandle(hSource, &liSourceSize.QuadPart);
    FileExitOnFailure(hr, "Failed to get size of source.");

    if (0 < cbCopy && cbCopy < (DWORD64)liSourceSize.QuadPart)
    {
        liSourceSize.QuadPart = cbCopy;
    }

    if (lpProgressRoutine)
    {
        dwResult = lpProgressRoutine(liSourceSize, liTotalCopied, liZero, liZero, 0, CALLBACK_STREAM_SWITCH, hSource, hTarget, lpData);
        switch (dwResult)
        {
        case PROGRESS_CONTINUE:
            break;

        case PROGRESS_CANCEL:
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_REQUEST_ABORTED));

        case PROGRESS_STOP:
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_REQUEST_ABORTED));

        case PROGRESS_QUIET:
            lpProgressRoutine = NULL;
            break;
        }
    }

    // Set size of the target file.
    ::SetFilePointerEx(hTarget, liSourceSize, NULL, FILE_BEGIN);

    if (!::SetEndOfFile(hTarget))
    {
        FileExitWithLastError(hr, "Failed to set end of target file.");
    }

    if (!::SetFilePointerEx(hTarget, liZero, NULL, FILE_BEGIN))
    {
        FileExitWithLastError(hr, "Failed to reset target file pointer.");
    }

    // Copy with progress.
    while (0 == cbCopy || cbTotalCopied < cbCopy)
    {
        cbRead = static_cast<DWORD>((0 == cbCopy) ? countof(rgbData) : min(countof(rgbData), cbCopy - cbTotalCopied));
        if (!::ReadFile(hSource, rgbData, cbRead, &cbRead, NULL))
        {
            FileExitWithLastError(hr, "Failed to read from source.");
        }

        if (cbRead)
        {
            hr = FileWriteHandle(hTarget, rgbData, cbRead);
            FileExitOnFailure(hr, "Failed to write to target.");

            cbTotalCopied += cbRead;

            if (lpProgressRoutine)
            {
                liTotalCopied.QuadPart = cbTotalCopied;
                dwResult = lpProgressRoutine(liSourceSize, liTotalCopied, liZero, liZero, 0, CALLBACK_CHUNK_FINISHED, hSource, hTarget, lpData);
                switch (dwResult)
                {
                case PROGRESS_CONTINUE:
                    break;

                case PROGRESS_CANCEL:
                    ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_REQUEST_ABORTED));

                case PROGRESS_STOP:
                    ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_REQUEST_ABORTED));

                case PROGRESS_QUIET:
                    lpProgressRoutine = NULL;
                    break;
                }
            }
        }
        else
        {
            break;
        }
    }

LExit:
    return hr;
}


/*******************************************************************
 FileEnsureCopy

*******************************************************************/
extern "C" HRESULT DAPI FileEnsureCopy(
    __in_z LPCWSTR wzSource,
    __in_z LPCWSTR wzTarget,
    __in BOOL fOverwrite
    )
{
    HRESULT hr = S_OK;
    DWORD er;

    // try to copy the file first
    if (::CopyFileW(wzSource, wzTarget, !fOverwrite))
    {
        ExitFunction();  // we're done
    }

    er = ::GetLastError();  // check the error and do the right thing below
    if (!fOverwrite && (ERROR_FILE_EXISTS == er || ERROR_ALREADY_EXISTS == er))
    {
        // if not overwriting this is an expected error
        ExitFunction1(hr = S_FALSE);
    }
    else if (ERROR_PATH_NOT_FOUND == er)  // if the path doesn't exist
    {
        // try to create the directory then do the copy
        LPWSTR pwzLastSlash = NULL;
        for (LPWSTR pwz = const_cast<LPWSTR>(wzTarget); *pwz; ++pwz)
        {
            if (*pwz == L'\\')
            {
                pwzLastSlash = pwz;
            }
        }

        if (pwzLastSlash)
        {
            *pwzLastSlash = L'\0'; // null terminate
            hr = DirEnsureExists(wzTarget, NULL);
            *pwzLastSlash = L'\\'; // now put the slash back
            FileExitOnFailureDebugTrace(hr, "failed to create directory while copying file: '%ls' to: '%ls'", wzSource, wzTarget);

            // try to copy again
            if (!::CopyFileW(wzSource, wzTarget, fOverwrite))
            {
                FileExitOnLastErrorDebugTrace(hr, "failed to copy file: '%ls' to: '%ls'", wzSource, wzTarget);
            }
        }
        else // no path was specified so just return the error
        {
            hr = HRESULT_FROM_WIN32(er);
        }
    }
    else // unexpected error
    {
        hr = HRESULT_FROM_WIN32(er);
    }

LExit:
    return hr;
}


/*******************************************************************
 FileEnsureCopyWithRetry

*******************************************************************/
extern "C" HRESULT DAPI FileEnsureCopyWithRetry(
    __in LPCWSTR wzSource,
    __in LPCWSTR wzTarget,
    __in BOOL fOverwrite,
    __in DWORD cRetry,
    __in DWORD dwWaitMilliseconds
    )
{
    AssertSz(cRetry != DWORD_MAX, "Cannot pass DWORD_MAX for retry.");

    HRESULT hr = E_FAIL;
    DWORD i = 0;

    for (i = 0; FAILED(hr) && i <= cRetry; ++i)
    {
        if (0 < i)
        {
            ::Sleep(dwWaitMilliseconds);
        }

        hr = FileEnsureCopy(wzSource, wzTarget, fOverwrite);
        if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr
            || HRESULT_FROM_WIN32(ERROR_FILE_EXISTS) == hr || HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS) == hr)
        {
            break; // no reason to retry these errors.
        }
    }
    FileExitOnFailure(hr, "Failed to copy file: '%ls' to: '%ls' after %u retries.", wzSource, wzTarget, i);

LExit:
    return hr;
}


/*******************************************************************
 FileEnsureMove

*******************************************************************/
extern "C" HRESULT DAPI FileEnsureMove(
    __in_z LPCWSTR wzSource,
    __in_z LPCWSTR wzTarget,
    __in BOOL fOverwrite,
    __in BOOL fAllowCopy
    )
{
    HRESULT hr = S_OK;
    DWORD er;

    DWORD dwFlags = 0;

    if (fOverwrite)
    {
        dwFlags |= MOVEFILE_REPLACE_EXISTING;
    }
    if (fAllowCopy)
    {
        dwFlags |= MOVEFILE_COPY_ALLOWED;
    }

    // try to move the file first
    if (::MoveFileExW(wzSource, wzTarget, dwFlags))
    {
        ExitFunction();  // we're done
    }

    er = ::GetLastError();  // check the error and do the right thing below
    if (!fOverwrite && (ERROR_FILE_EXISTS == er || ERROR_ALREADY_EXISTS == er))
    {
        // if not overwriting this is an expected error
        ExitFunction1(hr = S_FALSE);
    }
    else if (ERROR_FILE_NOT_FOUND == er)
    {
        // We are seeing some cases where ::MoveFileEx() says a file was not found
        // but the source file is actually present. In that case, return path not
        // found so we try to create the target path since that is most likely
        // what is missing. Otherwise, the source file is missing and we're obviously
        // not going to be recovering from that.
        if (FileExistsEx(wzSource, NULL))
        {
            er = ERROR_PATH_NOT_FOUND;
        }
    }

    // If the path doesn't exist, try to create the directory tree then do the move.
    if (ERROR_PATH_NOT_FOUND == er)
    {
        LPWSTR pwzLastSlash = NULL;
        for (LPWSTR pwz = const_cast<LPWSTR>(wzTarget); *pwz; ++pwz)
        {
            if (*pwz == L'\\')
            {
                pwzLastSlash = pwz;
            }
        }

        if (pwzLastSlash)
        {
            *pwzLastSlash = L'\0'; // null terminate
            hr = DirEnsureExists(wzTarget, NULL);
            *pwzLastSlash = L'\\'; // now put the slash back
            FileExitOnFailureDebugTrace(hr, "failed to create directory while moving file: '%ls' to: '%ls'", wzSource, wzTarget);

            // try to move again
            if (!::MoveFileExW(wzSource, wzTarget, dwFlags))
            {
                FileExitOnLastErrorDebugTrace(hr, "failed to move file: '%ls' to: '%ls'", wzSource, wzTarget);
            }
        }
        else // no path was specified so just return the error
        {
            hr = HRESULT_FROM_WIN32(er);
        }
    }
    else // unexpected error
    {
        hr = HRESULT_FROM_WIN32(er);
    }

LExit:
    return hr;
}


/*******************************************************************
 FileEnsureMoveWithRetry

*******************************************************************/
extern "C" HRESULT DAPI FileEnsureMoveWithRetry(
    __in LPCWSTR wzSource,
    __in LPCWSTR wzTarget,
    __in BOOL fOverwrite,
    __in BOOL fAllowCopy,
    __in DWORD cRetry,
    __in DWORD dwWaitMilliseconds
    )
{
    AssertSz(cRetry != DWORD_MAX, "Cannot pass DWORD_MAX for retry.");

    HRESULT hr = E_FAIL;
    DWORD i = 0;

    for (i = 0; FAILED(hr) && i < cRetry + 1; ++i)
    {
        if (0 < i)
        {
            ::Sleep(dwWaitMilliseconds);
        }

        hr = FileEnsureMove(wzSource, wzTarget, fOverwrite, fAllowCopy);
    }
    FileExitOnFailure(hr, "Failed to move file: '%ls' to: '%ls' after %u retries.", wzSource, wzTarget, i);

LExit:
    return hr;
}


/*******************************************************************
 FileCreateTemp - creates an empty temp file

 NOTE: uses ANSI functions internally so it is Win9x safe
********************************************************************/
extern "C" HRESULT DAPI FileCreateTemp(
    __in_z LPCWSTR wzPrefix,
    __in_z LPCWSTR wzExtension,
    __deref_opt_out_z LPWSTR* ppwzTempFile,
    __out_opt HANDLE* phTempFile
    )
{
    Assert(wzPrefix && *wzPrefix);
    HRESULT hr = S_OK;
    LPSTR pszTempPath = NULL;
    DWORD cchTempPath = MAX_PATH;

    HANDLE hTempFile = INVALID_HANDLE_VALUE;
    LPSTR pszTempFile = NULL;

    int i = 0;

    hr = StrAnsiAlloc(&pszTempPath, cchTempPath);
    FileExitOnFailure(hr, "failed to allocate memory for the temp path");
    ::GetTempPathA(cchTempPath, pszTempPath);

    for (i = 0; i < 1000 && INVALID_HANDLE_VALUE == hTempFile; ++i)
    {
        hr = StrAnsiAllocFormatted(&pszTempFile, "%s%ls%05d.%ls", pszTempPath, wzPrefix, i, wzExtension);
        FileExitOnFailure(hr, "failed to allocate memory for log file");

        hTempFile = ::CreateFileA(pszTempFile, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
        if (INVALID_HANDLE_VALUE == hTempFile)
        {
            // if the file already exists, just try again
            hr = HRESULT_FROM_WIN32(::GetLastError());
            if (HRESULT_FROM_WIN32(ERROR_FILE_EXISTS) == hr)
            {
                hr = S_OK;
                continue;
            }
            FileExitOnFailureDebugTrace(hr, "failed to create file: %hs", pszTempFile);
        }
    }

    if (ppwzTempFile)
    {
        hr = StrAllocStringAnsi(ppwzTempFile, pszTempFile, 0, CP_UTF8);
    }

    if (phTempFile)
    {
        *phTempFile = hTempFile;
        hTempFile = INVALID_HANDLE_VALUE;
    }

LExit:
    ReleaseFile(hTempFile);
    ReleaseStr(pszTempFile);
    ReleaseStr(pszTempPath);

    return hr;
}


/*******************************************************************
 FileCreateTempW - creates an empty temp file

*******************************************************************/
extern "C" HRESULT DAPI FileCreateTempW(
    __in_z LPCWSTR wzPrefix,
    __in_z LPCWSTR wzExtension,
    __deref_opt_out_z LPWSTR* ppwzTempFile,
    __out_opt HANDLE* phTempFile
    )
{
    Assert(wzPrefix && *wzPrefix);
    HRESULT hr = E_FAIL;

    LPWSTR pwzTempPath = NULL;
    LPWSTR pwzTempFile = NULL;

    HANDLE hTempFile = INVALID_HANDLE_VALUE;
    int i = 0;

    hr = PathGetTempPath(&pwzTempPath, NULL);
    FileExitOnFailure(hr, "failed to get temp path");

    for (i = 0; i < 1000 && INVALID_HANDLE_VALUE == hTempFile; ++i)
    {
        hr = StrAllocFormatted(&pwzTempFile, L"%s%s%05d.%s", pwzTempPath, wzPrefix, i, wzExtension);
        FileExitOnFailure(hr, "failed to allocate memory for temp filename");

        hTempFile = ::CreateFileW(pwzTempFile, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
        if (INVALID_HANDLE_VALUE == hTempFile)
        {
            // if the file already exists, just try again
            hr = HRESULT_FROM_WIN32(::GetLastError());
            if (HRESULT_FROM_WIN32(ERROR_FILE_EXISTS) == hr)
            {
                hr = S_OK;
                continue;
            }
            FileExitOnFailureDebugTrace(hr, "failed to create file: %ls", pwzTempFile);
        }
    }

    if (phTempFile)
    {
        *phTempFile = hTempFile;
        hTempFile = INVALID_HANDLE_VALUE;
    }

    if (ppwzTempFile)
    {
        *ppwzTempFile = pwzTempFile;
        pwzTempFile = NULL;
    }

LExit:
    ReleaseFile(hTempFile);
    ReleaseStr(pwzTempFile);
    ReleaseStr(pwzTempPath);

    return hr;
}


/*******************************************************************
 FileIsSame

********************************************************************/
extern "C" HRESULT DAPI FileIsSame(
    __in_z LPCWSTR wzFile1,
    __in_z LPCWSTR wzFile2,
    __out LPBOOL lpfSameFile
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile1 = NULL;
    HANDLE hFile2 = NULL;
    BY_HANDLE_FILE_INFORMATION fileInfo1 = { };
    BY_HANDLE_FILE_INFORMATION fileInfo2 = { };

    hFile1 = ::CreateFileW(wzFile1, FILE_READ_ATTRIBUTES, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
    FileExitOnInvalidHandleWithLastError(hFile1, hr, "Failed to open file 1. File = '%ls'", wzFile1);

    hFile2 = ::CreateFileW(wzFile2, FILE_READ_ATTRIBUTES, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
    FileExitOnInvalidHandleWithLastError(hFile2, hr, "Failed to open file 2. File = '%ls'", wzFile2);

    if (!::GetFileInformationByHandle(hFile1, &fileInfo1))
    {
        FileExitWithLastError(hr, "Failed to get information for file 1. File = '%ls'", wzFile1);
    }

    if (!::GetFileInformationByHandle(hFile2, &fileInfo2))
    {
        FileExitWithLastError(hr, "Failed to get information for file 2. File = '%ls'", wzFile2);
    }

    *lpfSameFile = fileInfo1.dwVolumeSerialNumber == fileInfo2.dwVolumeSerialNumber &&
        fileInfo1.nFileIndexHigh == fileInfo2.nFileIndexHigh &&
        fileInfo1.nFileIndexLow == fileInfo2.nFileIndexLow ? TRUE : FALSE;

LExit:
    ReleaseFile(hFile1);
    ReleaseFile(hFile2);

    return hr;
}

/*******************************************************************
 FileEnsureDelete - deletes a file, first removing read-only,
    hidden, or system attributes if necessary.
********************************************************************/
extern "C" HRESULT DAPI FileEnsureDelete(
    __in_z LPCWSTR wzFile
    )
{
    HRESULT hr = S_OK;

    DWORD dwAttrib = INVALID_FILE_ATTRIBUTES;
    if (FileExistsEx(wzFile, &dwAttrib))
    {
        if (dwAttrib & FILE_ATTRIBUTE_READONLY || dwAttrib & FILE_ATTRIBUTE_HIDDEN || dwAttrib & FILE_ATTRIBUTE_SYSTEM)
        {
            if (!::SetFileAttributesW(wzFile, FILE_ATTRIBUTE_NORMAL))
            {
                FileExitOnLastError(hr, "Failed to remove attributes from file: %ls", wzFile);
            }
        }

        if (!::DeleteFileW(wzFile))
        {
            FileExitOnLastError(hr, "Failed to delete file: %ls", wzFile);
        }
    }

LExit:
    return hr;
}

/*******************************************************************
 FileGetTime - Gets the file time of a specified file
********************************************************************/
extern "C" HRESULT DAPI FileGetTime(
    __in_z LPCWSTR wzFile,
    __out_opt  LPFILETIME lpCreationTime,
    __out_opt  LPFILETIME lpLastAccessTime,
    __out_opt  LPFILETIME lpLastWriteTime
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = NULL;

    hFile = ::CreateFileW(wzFile, FILE_READ_ATTRIBUTES, FILE_SHARE_WRITE | FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, 0, NULL);
    FileExitOnInvalidHandleWithLastError(hFile, hr, "Failed to open file. File = '%ls'", wzFile);

    if (!::GetFileTime(hFile, lpCreationTime, lpLastAccessTime, lpLastWriteTime))
    {
        FileExitWithLastError(hr, "Failed to get file time for file. File = '%ls'", wzFile);
    }

LExit:
    ReleaseFile(hFile);
    return hr;
}

/*******************************************************************
 FileSetTime - Sets the file time of a specified file
********************************************************************/
extern "C" HRESULT DAPI FileSetTime(
    __in_z LPCWSTR wzFile,
    __in_opt  const FILETIME *lpCreationTime,
    __in_opt  const FILETIME *lpLastAccessTime,
    __in_opt  const FILETIME *lpLastWriteTime
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = NULL;

    hFile = ::CreateFileW(wzFile, FILE_WRITE_ATTRIBUTES, FILE_SHARE_WRITE | FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, 0, NULL);
    FileExitOnInvalidHandleWithLastError(hFile, hr, "Failed to open file. File = '%ls'", wzFile);

    if (!::SetFileTime(hFile, lpCreationTime, lpLastAccessTime, lpLastWriteTime))
    {
        FileExitWithLastError(hr, "Failed to set file time for file. File = '%ls'", wzFile);
    }

LExit:
    ReleaseFile(hFile);
    return hr;
}

/*******************************************************************
 FileReSetTime - ReSets a file's last acess and modified time to the
 creation time of the file
********************************************************************/
extern "C" HRESULT DAPI FileResetTime(
    __in_z LPCWSTR wzFile
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = NULL;
    FILETIME ftCreateTime;

    hFile = ::CreateFileW(wzFile, FILE_WRITE_ATTRIBUTES | FILE_READ_ATTRIBUTES, FILE_SHARE_WRITE | FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    FileExitOnInvalidHandleWithLastError(hFile, hr, "Failed to open file. File = '%ls'", wzFile);
    
    if (!::GetFileTime(hFile, &ftCreateTime, NULL, NULL))
    {
        FileExitWithLastError(hr, "Failed to get file time for file. File = '%ls'", wzFile);
    }

    if (!::SetFileTime(hFile, NULL, NULL, &ftCreateTime))
    {
        FileExitWithLastError(hr, "Failed to reset file time for file. File = '%ls'", wzFile);
    }

LExit:
    ReleaseFile(hFile);
    return hr;
}


/*******************************************************************
 FileExecutableArchitecture

*******************************************************************/
extern "C" HRESULT DAPI FileExecutableArchitecture(
    __in_z LPCWSTR wzFile,
    __out FILE_ARCHITECTURE *pArchitecture
    )
{
    HRESULT hr = S_OK;

    HANDLE hFile = INVALID_HANDLE_VALUE;
    DWORD cbRead = 0;
    IMAGE_DOS_HEADER DosImageHeader = { };
    IMAGE_NT_HEADERS NtImageHeader = { };

    hFile = ::CreateFileW(wzFile, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        FileExitWithLastError(hr, "Failed to open file: %ls", wzFile);
    }

    if (!::ReadFile(hFile, &DosImageHeader, sizeof(DosImageHeader), &cbRead, NULL))
    {
        FileExitWithLastError(hr, "Failed to read DOS header from file: %ls", wzFile);
    }

    if (DosImageHeader.e_magic != IMAGE_DOS_SIGNATURE)
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        FileExitOnRootFailure(hr, "Read invalid DOS header from file: %ls", wzFile);
    }

    if (INVALID_SET_FILE_POINTER == ::SetFilePointer(hFile, DosImageHeader.e_lfanew, NULL, FILE_BEGIN))
    {
        FileExitWithLastError(hr, "Failed to seek the NT header in file: %ls", wzFile);
    }

    if (!::ReadFile(hFile, &NtImageHeader, sizeof(NtImageHeader), &cbRead, NULL))
    {
        FileExitWithLastError(hr, "Failed to read NT header from file: %ls", wzFile);
    }

    if (NtImageHeader.Signature != IMAGE_NT_SIGNATURE)
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        FileExitOnRootFailure(hr, "Read invalid NT header from file: %ls", wzFile);
    }

    if (IMAGE_SUBSYSTEM_NATIVE == NtImageHeader.OptionalHeader.Subsystem ||
        IMAGE_SUBSYSTEM_WINDOWS_GUI == NtImageHeader.OptionalHeader.Subsystem ||
        IMAGE_SUBSYSTEM_WINDOWS_CUI == NtImageHeader.OptionalHeader.Subsystem)
    {
        switch (NtImageHeader.FileHeader.Machine)
        {
        case IMAGE_FILE_MACHINE_I386:
            *pArchitecture = FILE_ARCHITECTURE_X86;
            break;
        case IMAGE_FILE_MACHINE_IA64:
            *pArchitecture = FILE_ARCHITECTURE_IA64;
            break;
        case IMAGE_FILE_MACHINE_AMD64:
            *pArchitecture = FILE_ARCHITECTURE_X64;
            break;
        default:
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            break;
        }
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
    }
    FileExitOnFailure(hr, "Unexpected subsystem: %d machine type: %d specified in NT header from file: %ls", NtImageHeader.OptionalHeader.Subsystem, NtImageHeader.FileHeader.Machine, wzFile);

LExit:
    if (hFile != INVALID_HANDLE_VALUE)
    {
        ::CloseHandle(hFile);
    }

    return hr;
}

/*******************************************************************
 FileToString

*******************************************************************/
extern "C" HRESULT DAPI FileToString(
    __in_z LPCWSTR wzFile,
    __out LPWSTR *psczString,
    __out_opt FILE_ENCODING *pfeEncoding
    )
{
    HRESULT hr = S_OK;
    BYTE *pbFullFileBuffer = NULL;
    SIZE_T cbFullFileBuffer = 0;
    BOOL fNullCharFound = FALSE;
    LPWSTR sczFileText = NULL;

    // Check if the file is ANSI
    hr = FileRead(&pbFullFileBuffer, &cbFullFileBuffer, wzFile);
    FileExitOnFailure(hr, "Failed to read file: %ls", wzFile);

    if (0 == cbFullFileBuffer)
    {
        *psczString = NULL;
        ExitFunction1(hr = S_OK);
    }

    // UTF-8 BOM
    if (cbFullFileBuffer > sizeof(UTF8BOM) && 0 == memcmp(pbFullFileBuffer, UTF8BOM, sizeof(UTF8BOM)))
    {
        if (pfeEncoding)
        {
            *pfeEncoding = FILE_ENCODING_UTF8_WITH_BOM;
        }

        hr = StrAllocStringAnsi(&sczFileText, reinterpret_cast<LPCSTR>(pbFullFileBuffer + 3), cbFullFileBuffer - 3, CP_UTF8);
        FileExitOnFailure(hr, "Failed to convert file %ls from UTF-8 as its BOM indicated", wzFile);

        *psczString = sczFileText;
        sczFileText = NULL;
    }
    // UTF-16 BOM, little endian (windows regular UTF-16)
    else if (cbFullFileBuffer > sizeof(UTF16BOM) && 0 == memcmp(pbFullFileBuffer, UTF16BOM, sizeof(UTF16BOM)))
    {
        if (pfeEncoding)
        {
            *pfeEncoding = FILE_ENCODING_UTF16_WITH_BOM;
        }

        hr = StrAllocString(psczString, reinterpret_cast<LPWSTR>(pbFullFileBuffer + 2), (cbFullFileBuffer - 2) / sizeof(WCHAR));
        FileExitOnFailure(hr, "Failed to allocate copy of string");
    }
    // No BOM, let's try to detect
    else
    {
        for (DWORD i = 0; i < cbFullFileBuffer; ++i)
        {
            if (pbFullFileBuffer[i] == '\0')
            {
                fNullCharFound = TRUE;
                break;
            }
        }

        if (!fNullCharFound)
        {
            if (pfeEncoding)
            {
                *pfeEncoding = FILE_ENCODING_UTF8;
            }

            hr = StrAllocStringAnsi(&sczFileText, reinterpret_cast<LPCSTR>(pbFullFileBuffer), cbFullFileBuffer, CP_UTF8);
            if (FAILED(hr))
            {
                if (E_OUTOFMEMORY == hr)
                {
                    FileExitOnFailure(hr, "Failed to convert file %ls from UTF-8", wzFile);
                }
            }
            else
            {
                *psczString = sczFileText;
                sczFileText = NULL;
            }
        }
        else if (NULL == *psczString)
        {
            if (pfeEncoding)
            {
                *pfeEncoding = FILE_ENCODING_UTF16;
            }

            hr = StrAllocString(psczString, reinterpret_cast<LPWSTR>(pbFullFileBuffer), cbFullFileBuffer / sizeof(WCHAR));
            FileExitOnFailure(hr, "Failed to allocate copy of string");
        }
    }

LExit:
    ReleaseStr(sczFileText);
    ReleaseMem(pbFullFileBuffer);

    return hr;
}

/*******************************************************************
 FileFromString

*******************************************************************/
extern "C" HRESULT DAPI FileFromString(
    __in_z LPCWSTR wzFile,
    __in DWORD dwFlagsAndAttributes,
    __in_z LPCWSTR sczString,
    __in FILE_ENCODING feEncoding
    )
{
    HRESULT hr = S_OK;
    LPSTR sczUtf8String = NULL;
    BYTE *pbFullFileBuffer = NULL;
    const BYTE *pcbFullFileBuffer = NULL;
    SIZE_T cbFullFileBuffer = 0;
    SIZE_T cbStrLen = 0;

    switch (feEncoding)
    {
    case FILE_ENCODING_UTF8:
        hr = StrAnsiAllocString(&sczUtf8String, sczString, 0, CP_UTF8);
        FileExitOnFailure(hr, "Failed to convert string to UTF-8 to write UTF-8 file");

        hr = ::StringCchLengthA(sczUtf8String, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cbFullFileBuffer));
        FileExitOnRootFailure(hr, "Failed to get length of UTF-8 string");

        pcbFullFileBuffer = reinterpret_cast<BYTE *>(sczUtf8String);
        break;
    case FILE_ENCODING_UTF8_WITH_BOM:
        hr = StrAnsiAllocString(&sczUtf8String, sczString, 0, CP_UTF8);
        FileExitOnFailure(hr, "Failed to convert string to UTF-8 to write UTF-8 file");

        hr = ::StringCchLengthA(sczUtf8String, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cbStrLen));
        FileExitOnRootFailure(hr, "Failed to get length of UTF-8 string");

        cbFullFileBuffer = sizeof(UTF8BOM) + cbStrLen;

        pbFullFileBuffer = reinterpret_cast<BYTE *>(MemAlloc(cbFullFileBuffer, TRUE));
        FileExitOnNull(pbFullFileBuffer, hr, E_OUTOFMEMORY, "Failed to allocate memory for output file buffer");

        memcpy_s(pbFullFileBuffer, sizeof(UTF8BOM), UTF8BOM, sizeof(UTF8BOM));
        memcpy_s(pbFullFileBuffer + sizeof(UTF8BOM), cbStrLen, sczUtf8String, cbStrLen);
        pcbFullFileBuffer = pbFullFileBuffer;
        break;
    case FILE_ENCODING_UTF16:
        hr = ::StringCchLengthW(sczString, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cbStrLen));
        FileExitOnRootFailure(hr, "Failed to get length of string");

        cbFullFileBuffer = cbStrLen * sizeof(WCHAR);
        pcbFullFileBuffer = reinterpret_cast<const BYTE *>(sczString);
        break;
    case FILE_ENCODING_UTF16_WITH_BOM:
        hr = ::StringCchLengthW(sczString, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cbStrLen));
        FileExitOnRootFailure(hr, "Failed to get length of string");

        cbStrLen *= sizeof(WCHAR);
        cbFullFileBuffer = sizeof(UTF16BOM) + cbStrLen;

        pbFullFileBuffer = reinterpret_cast<BYTE *>(MemAlloc(cbFullFileBuffer, TRUE));
        FileExitOnNull(pbFullFileBuffer, hr, E_OUTOFMEMORY, "Failed to allocate memory for output file buffer");

        memcpy_s(pbFullFileBuffer, sizeof(UTF16BOM), UTF16BOM, sizeof(UTF16BOM));
        memcpy_s(pbFullFileBuffer + sizeof(UTF16BOM), cbStrLen, sczString, cbStrLen);
        pcbFullFileBuffer = pbFullFileBuffer;
        break;
    }

    hr = FileWrite(wzFile, dwFlagsAndAttributes, pcbFullFileBuffer, cbFullFileBuffer, NULL);
    FileExitOnFailure(hr, "Failed to write file from string to: %ls", wzFile);

LExit:
    ReleaseStr(sczUtf8String);
    ReleaseMem(pbFullFileBuffer);

    return hr;
}

HRESULT DAPI FileCopyPartial(
    __in HANDLE hSource,
    __in DWORD64 cbStart,
    __in DWORD64 cbCopy,
    __in_z LPCWSTR wzTarget
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    DWORD64 cbCurrPointer = 0;
    LARGE_INTEGER liMaxRead = {};
    LARGE_INTEGER liRead = {};
    LARGE_INTEGER cbData = {};
    BYTE* pbData = NULL;

    FileExitOnNull(hSource && (hSource != INVALID_HANDLE_VALUE), hr, E_INVALIDARG, "Invalid argument hSource");
    FileExitOnNull(wzTarget && *wzTarget, hr, E_INVALIDARG, "wzTarget is null");

    hr = FileSetPointer(hSource, 0, &cbCurrPointer, FILE_CURRENT);
    FileExitOnFailure(hr, "Failed to get current file pointer");

    hr = FileSetPointer(hSource, cbStart, NULL, FILE_BEGIN);
    FileExitOnFailure(hr, "Failed to set file pointer");

    if (!::GetFileSizeEx(hSource, &liMaxRead))
    {
        FileExitWithLastError(hr, "Failed to get size of file");
    }
    liMaxRead.QuadPart = min(liMaxRead.QuadPart - cbStart, cbCopy);

    hFile = ::CreateFileW(wzTarget, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    FileExitOnInvalidHandleWithLastError(hFile, hr, "Failed to open file: %ls", wzTarget);

    cbData.QuadPart = min(liMaxRead.QuadPart, 1024 * 1024 * 50); // Max 50MB chunks
    pbData = (BYTE*)MemAlloc((SIZE_T)cbData.QuadPart, FALSE);
    FileExitOnNull(pbData, hr, E_OUTOFMEMORY, "Failed to allocate memory");

    while (liRead.QuadPart < liMaxRead.QuadPart)
    {
        LARGE_INTEGER cbRead = {};

        cbData.QuadPart = min(cbData.QuadPart, liMaxRead.QuadPart - liRead.QuadPart);
        if (!::ReadFile(hSource, pbData, cbData.LowPart, &cbRead.LowPart, NULL))
        {
            FileExitWithLastError(hr, "Failed to read from file");
        }
        FileExitOnNull((cbRead.LowPart == cbData.LowPart), hr, E_FAIL, "Failed to read data from file");

        hr = FileWriteHandle(hFile, pbData, cbData.LowPart);
        FileExitOnFailure(hr, "Failed to write to file");

        liRead.QuadPart += cbRead.LowPart;
    }

LExit:
    FileSetPointer(hSource, cbCurrPointer, NULL, FILE_BEGIN); // Recover initial position

    ReleaseMem(pbData);
    ReleaseFile(hFile);

    return hr;
}
