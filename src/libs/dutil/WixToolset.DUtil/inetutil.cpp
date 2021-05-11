// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define InetExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_INETUTIL, x, s, __VA_ARGS__)
#define InetExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_INETUTIL, x, s, __VA_ARGS__)
#define InetExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_INETUTIL, x, s, __VA_ARGS__)
#define InetExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_INETUTIL, x, s, __VA_ARGS__)
#define InetExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_INETUTIL, x, s, __VA_ARGS__)
#define InetExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_INETUTIL, x, s, __VA_ARGS__)
#define InetExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_INETUTIL, p, x, e, s, __VA_ARGS__)
#define InetExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_INETUTIL, p, x, s, __VA_ARGS__)
#define InetExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_INETUTIL, p, x, e, s, __VA_ARGS__)
#define InetExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_INETUTIL, p, x, s, __VA_ARGS__)
#define InetExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_INETUTIL, e, x, s, __VA_ARGS__)
#define InetExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_INETUTIL, g, x, s, __VA_ARGS__)


/*******************************************************************
 InternetGetSizeByHandle - returns size of file by url handle

*******************************************************************/
extern "C" HRESULT DAPI InternetGetSizeByHandle(
    __in HINTERNET hiFile,
    __out LONGLONG* pllSize
    )
{
    Assert(pllSize);

    HRESULT hr = S_OK;
    DWORD dwSize = 0;
    DWORD cb = 0;

    cb = sizeof(dwSize);
    if (!::HttpQueryInfoW(hiFile, HTTP_QUERY_CONTENT_LENGTH | HTTP_QUERY_FLAG_NUMBER, reinterpret_cast<LPVOID>(&dwSize), &cb, NULL))
    {
        InetExitOnLastError(hr, "Failed to get size for internet file handle");
    }

    *pllSize = dwSize;
LExit:
    return hr;
}


/*******************************************************************
 InetGetCreateTimeByHandle - returns url creation time

******************************************************************/
extern "C" HRESULT DAPI InternetGetCreateTimeByHandle(
    __in HINTERNET hiFile,
    __out LPFILETIME pft
    )
{
    Assert(pft);

    HRESULT hr = S_OK;
    SYSTEMTIME st = {0 };
    DWORD cb = sizeof(SYSTEMTIME);

    if (!::HttpQueryInfoW(hiFile, HTTP_QUERY_LAST_MODIFIED | HTTP_QUERY_FLAG_SYSTEMTIME, reinterpret_cast<LPVOID>(&st), &cb, NULL))
    {
        InetExitWithLastError(hr, "failed to get create time for internet file handle");
    }

    if (!::SystemTimeToFileTime(&st, pft))
    {
        InetExitWithLastError(hr, "failed to convert system time to file time");
    }

LExit:
    return hr;
}


/*******************************************************************
 InternetQueryInfoString - query info string

*******************************************************************/
extern "C" HRESULT DAPI InternetQueryInfoString(
    __in HINTERNET hRequest,
    __in DWORD dwInfo,
    __deref_out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    SIZE_T cbOriginal = 0;
    DWORD cbValue = 0;
    DWORD dwIndex = 0;

    // If nothing was provided start off with some arbitrary size.
    if (!*psczValue)
    {
        hr = StrAlloc(psczValue, 64);
        InetExitOnFailure(hr, "Failed to allocate memory for value.");
    }

    hr = StrSize(*psczValue, &cbOriginal);
    InetExitOnFailure(hr, "Failed to get size of value.");

    cbValue = (DWORD)min(DWORD_MAX, cbOriginal);

    if (!::HttpQueryInfoW(hRequest, dwInfo, static_cast<void*>(*psczValue), &cbValue, &dwIndex))
    {
        DWORD er = ::GetLastError();
        if (ERROR_INSUFFICIENT_BUFFER == er)
        {
            cbValue += sizeof(WCHAR); // add one character for the null terminator.

            hr = StrAlloc(psczValue, cbValue / sizeof(WCHAR));
            InetExitOnFailure(hr, "Failed to allocate value.");

            if (!::HttpQueryInfoW(hRequest, dwInfo, static_cast<void*>(*psczValue), &cbValue, &dwIndex))
            {
                er = ::GetLastError();
            }
            else
            {
                er = ERROR_SUCCESS;
            }
        }

        hr = HRESULT_FROM_WIN32(er);
        InetExitOnRootFailure(hr, "Failed to get query information.");
    }

LExit:
    return hr;
}


/*******************************************************************
 InternetQueryInfoNumber - query info number

*******************************************************************/
extern "C" HRESULT DAPI InternetQueryInfoNumber(
    __in HINTERNET hRequest,
    __in DWORD dwInfo,
    __inout LONG* plInfo
    )
{
    HRESULT hr = S_OK;
    DWORD cbCode = sizeof(LONG);
    DWORD dwIndex = 0;

    if (!::HttpQueryInfoW(hRequest, dwInfo | HTTP_QUERY_FLAG_NUMBER, static_cast<void*>(plInfo), &cbCode, &dwIndex))
    {
        InetExitWithLastError(hr, "Failed to get query information.");
    }

LExit:
    return hr;
}
