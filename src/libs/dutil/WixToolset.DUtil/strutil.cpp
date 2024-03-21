// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define StrExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_STRUTIL, x, s, __VA_ARGS__)
#define StrExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_STRUTIL, x, s, __VA_ARGS__)
#define StrExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_STRUTIL, x, s, __VA_ARGS__)
#define StrExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_STRUTIL, x, s, __VA_ARGS__)
#define StrExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_STRUTIL, x, s, __VA_ARGS__)
#define StrExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_STRUTIL, x, s, __VA_ARGS__)
#define StrExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_STRUTIL, p, x, e, s, __VA_ARGS__)
#define StrExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_STRUTIL, p, x, s, __VA_ARGS__)
#define StrExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_STRUTIL, p, x, e, s, __VA_ARGS__)
#define StrExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_STRUTIL, p, x, s, __VA_ARGS__)
#define StrExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_STRUTIL, e, x, s, __VA_ARGS__)
#define StrExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_STRUTIL, g, x, s, __VA_ARGS__)

#define ARRAY_GROWTH_SIZE 5

// Forward declarations.
static HRESULT AllocHelper(
    __deref_out_ecount_part(cch, 0) LPWSTR* ppwz,
    __in SIZE_T cch,
    __in BOOL fZeroOnRealloc
    );
static HRESULT AllocStringHelper(
    __deref_out_ecount_z(cchSource + 1) LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource,
    __in BOOL fZeroOnRealloc
    );
static HRESULT AllocConcatHelper(
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource,
    __in BOOL fZeroOnRealloc
    );
static HRESULT AllocFormattedArgsHelper(
    __deref_out_z LPWSTR* ppwz,
    __in BOOL fZeroOnRealloc,
    __in __format_string LPCWSTR wzFormat,
    __in va_list args
    );
static HRESULT StrAllocStringMapInvariant(
    __deref_out_z LPWSTR* pscz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource,
    __in DWORD dwMapFlags
    );

/********************************************************************
StrAlloc - allocates or reuses dynamic string memory

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAlloc(
    __deref_out_ecount_part(cch, 0) LPWSTR* ppwz,
    __in SIZE_T cch
    )
{
    return AllocHelper(ppwz, cch, FALSE);
}

/********************************************************************
StrAllocSecure - allocates or reuses dynamic string memory
If the memory needs to reallocated, calls SecureZeroMemory on the
original block of memory after it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAllocSecure(
    __deref_out_ecount_part(cch, 0) LPWSTR* ppwz,
    __in SIZE_T cch
    )
{
    return AllocHelper(ppwz, cch, TRUE);
}

/********************************************************************
AllocHelper - allocates or reuses dynamic string memory
If fZeroOnRealloc is true and the memory needs to reallocated,
calls SecureZeroMemory on original block of memory after it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
static HRESULT AllocHelper(
    __deref_out_ecount_part(cch, 0) LPWSTR* ppwz,
    __in SIZE_T cch,
    __in BOOL fZeroOnRealloc
    )
{
    Assert(ppwz && cch);

    HRESULT hr = S_OK;
    LPWSTR pwz = NULL;

    if (cch >= MAXDWORD / sizeof(WCHAR))
    {
        hr = E_OUTOFMEMORY;
        StrExitOnFailure(hr, "Not enough memory to allocate string of size: %u", cch);
    }

    if (*ppwz)
    {
        if (fZeroOnRealloc)
        {
            LPVOID pvNew = NULL;
            hr = MemReAllocSecure(*ppwz, sizeof(WCHAR)* cch, FALSE, &pvNew);
            StrExitOnFailure(hr, "Failed to reallocate string");
            pwz = static_cast<LPWSTR>(pvNew);
        }
        else
        {
            pwz = static_cast<LPWSTR>(MemReAlloc(*ppwz, sizeof(WCHAR)* cch, FALSE));
        }
    }
    else
    {
        pwz = static_cast<LPWSTR>(MemAlloc(sizeof(WCHAR) * cch, TRUE));
    }

    StrExitOnNull(pwz, hr, E_OUTOFMEMORY, "failed to allocate string, len: %u", cch);

    *ppwz = pwz;
LExit:
    return hr;
}


/********************************************************************
StrTrimCapacity - Frees any unnecessary memory associated with a string.
                  Purely used for optimization, generally only when a string
                  has been changing size, and will no longer grow.

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
HRESULT DAPI StrTrimCapacity(
    __deref_out_z LPWSTR* ppwz
    )
{
    Assert(ppwz);

    HRESULT hr = S_OK;
    SIZE_T cchLen = 0;

    hr = ::StringCchLengthW(*ppwz, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchLen));
    StrExitOnRootFailure(hr, "Failed to calculate length of string");

    ++cchLen; // Add 1 for null-terminator

    hr = StrAlloc(ppwz, cchLen);
    StrExitOnFailure(hr, "Failed to reallocate string");

LExit:
    return hr;
}


/********************************************************************
StrTrimWhitespace - allocates or reuses dynamic string memory and copies
                    in an existing string, excluding whitespace

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
HRESULT DAPI StrTrimWhitespace(
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzSource
    )
{
    HRESULT hr = S_OK;
    size_t i = 0;
    LPWSTR sczResult = NULL;

    // Ignore beginning whitespace
    while (L' ' == *wzSource || L'\t' == *wzSource)
    {
        wzSource++;
    }

    hr = ::StringCchLengthW(wzSource, STRSAFE_MAX_CCH, &i);
    StrExitOnRootFailure(hr, "Failed to get length of string");

    // Overwrite ending whitespace with null characters
    if (0 < i)
    {
        // start from the last non-null-terminator character in the array
        for (i = i - 1; i > 0; --i)
        {
            if (L' ' != wzSource[i] && L'\t' != wzSource[i])
            {
                break;
            }
        }

        ++i;
    }

    hr = StrAllocString(&sczResult, wzSource, i);
    StrExitOnFailure(hr, "Failed to copy result string");

    // Output result
    *ppwz = sczResult;
    sczResult = NULL;

LExit:
    ReleaseStr(sczResult);

    return hr;
}


/********************************************************************
StrAnsiAlloc - allocates or reuses dynamic ANSI string memory

NOTE: caller is responsible for freeing ppsz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAnsiAlloc(
    __deref_out_ecount_part(cch, 0) LPSTR* ppsz,
    __in SIZE_T cch
    )
{
    Assert(ppsz && cch);

    HRESULT hr = S_OK;
    LPSTR psz = NULL;

    if (cch >= MAXDWORD / sizeof(WCHAR))
    {
        hr = E_OUTOFMEMORY;
        StrExitOnFailure(hr, "Not enough memory to allocate string of size: %u", cch);
    }

    if (*ppsz)
    {
        psz = static_cast<LPSTR>(MemReAlloc(*ppsz, sizeof(CHAR) * cch, FALSE));
    }
    else
    {
        psz = static_cast<LPSTR>(MemAlloc(sizeof(CHAR) * cch, TRUE));
    }

    StrExitOnNull(psz, hr, E_OUTOFMEMORY, "failed to allocate string, len: %u", cch);

    *ppsz = psz;
LExit:
    return hr;
}


/********************************************************************
StrAnsiTrimCapacity - Frees any unnecessary memory associated with a string.
                  Purely used for optimization, generally only when a string
                  has been changing size, and will no longer grow.

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
HRESULT DAPI StrAnsiTrimCapacity(
    __deref_out_z LPSTR* ppz
    )
{
    Assert(ppz);

    HRESULT hr = S_OK;
    SIZE_T cchLen = 0;

#pragma prefast(push)
#pragma prefast(disable:25068)
    hr = ::StringCchLengthA(*ppz, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchLen));
#pragma prefast(pop)
    StrExitOnFailure(hr, "Failed to calculate length of string");

    ++cchLen; // Add 1 for null-terminator

    hr = StrAnsiAlloc(ppz, cchLen);
    StrExitOnFailure(hr, "Failed to reallocate string");

LExit:
    return hr;
}


/********************************************************************
StrAnsiTrimWhitespace - allocates or reuses dynamic string memory and copies
                    in an existing string, excluding whitespace

NOTE: caller is responsible for freeing ppz even if function fails
********************************************************************/
HRESULT DAPI StrAnsiTrimWhitespace(
    __deref_out_z LPSTR* ppz,
    __in_z LPCSTR szSource
    )
{
    HRESULT hr = S_OK;
    size_t i = 0;
    LPSTR sczResult = NULL;

    // Ignore beginning whitespace
    while (' ' == *szSource || '\t' == *szSource)
    {
        szSource++;
    }

    hr = ::StringCchLengthA(szSource, STRSAFE_MAX_CCH, &i);
    StrExitOnRootFailure(hr, "Failed to get length of string");

    // Overwrite ending whitespace with null characters
    if (0 < i)
    {
        // start from the last non-null-terminator character in the array
        for (i = i - 1; i > 0; --i)
        {
            if (L' ' != szSource[i] && L'\t' != szSource[i])
            {
                break;
            }
        }

        ++i;
    }

    hr = StrAnsiAllocStringAnsi(&sczResult, szSource, i);
    StrExitOnFailure(hr, "Failed to copy result string");

    // Output result
    *ppz = sczResult;
    sczResult = NULL;

LExit:
    ReleaseStr(sczResult);

    return hr;
}

/********************************************************************
StrAllocString - allocates or reuses dynamic string memory and copies in an existing string

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource does not have to equal the length of wzSource
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocString(
    __deref_out_ecount_z(cchSource+1) LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource
    )
{
    return AllocStringHelper(ppwz, wzSource, cchSource, FALSE);
}

/********************************************************************
StrAllocStringSecure - allocates or reuses dynamic string memory and
copies in an existing string. If the memory needs to reallocated,
calls SecureZeroMemory on original block of memory after it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource does not have to equal the length of wzSource
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocStringSecure(
    __deref_out_ecount_z(cchSource + 1) LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource
    )
{
    return AllocStringHelper(ppwz, wzSource, cchSource, TRUE);
}

/********************************************************************
AllocStringHelper - allocates or reuses dynamic string memory and copies in an existing string
If fZeroOnRealloc is true and the memory needs to reallocated,
calls SecureZeroMemory on original block of memory after it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource does not have to equal the length of wzSource
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
static HRESULT AllocStringHelper(
    __deref_out_ecount_z(cchSource + 1) LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource,
    __in BOOL fZeroOnRealloc
    )
{
    Assert(ppwz && wzSource); // && *wzSource);

    HRESULT hr = S_OK;
    SIZE_T cch = 0;

    if (*ppwz)
    {
        hr = StrMaxLength(*ppwz, &cch);
        StrExitOnFailure(hr, "failed to get size of destination string");
    }

    if (0 == cchSource && wzSource)
    {
        hr = ::StringCchLengthW(wzSource, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cchSource));
        StrExitOnRootFailure(hr, "failed to get length of source string");
    }

    SIZE_T cchNeeded;
    hr = ::ULongPtrAdd(cchSource, 1, &cchNeeded); // add one for the null terminator
    StrExitOnRootFailure(hr, "source string is too long");

    if (cch < cchNeeded)
    {
        cch = cchNeeded;
        hr = AllocHelper(ppwz, cch, fZeroOnRealloc);
        StrExitOnFailure(hr, "failed to allocate string from string.");
    }

    // copy everything (the NULL terminator will be included)
    hr = ::StringCchCopyNExW(*ppwz, cch, wzSource, cchSource, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);

LExit:
    return hr;
}


/********************************************************************
StrAnsiAllocString - allocates or reuses dynamic ANSI string memory and copies in an existing string

NOTE: caller is responsible for freeing ppsz even if function fails
NOTE: cchSource must equal the length of wzSource (not including the NULL terminator)
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAnsiAllocString(
    __deref_out_ecount_z(cchSource+1) LPSTR* ppsz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource,
    __in UINT uiCodepage
    )
{
    Assert(ppsz && wzSource);

    HRESULT hr = S_OK;
    LPSTR psz = NULL;
    SIZE_T cch = 0;
    SIZE_T cchDest = cchSource; // at least enough

    if (*ppsz)
    {
        hr = StrMaxLengthAnsi(*ppsz, &cch);
        StrExitOnFailure(hr, "failed to get size of destination string");
    }

    if (0 == cchSource)
    {
        cchDest = ::WideCharToMultiByte(uiCodepage, 0, wzSource, -1, NULL, 0, NULL, NULL);
        if (0 == cchDest)
        {
            StrExitWithLastError(hr, "failed to get required size for conversion to ANSI: %ls", wzSource);
        }

        --cchDest; // subtract one because WideChageToMultiByte includes space for the NULL terminator that we track below
    }
    else if (L'\0' == wzSource[cchSource - 1]) // if the source already had a null terminator, don't count that in the character count because we track it below
    {
        cchDest = cchSource - 1;
    }

    if (cch < cchDest + 1)
    {
        cch = cchDest + 1;   // add one for the NULL terminator
        if (cch >= MAXDWORD / sizeof(WCHAR))
        {
            hr = E_OUTOFMEMORY;
            StrExitOnFailure(hr, "Not enough memory to allocate string of size: %u", cch);
        }

        if (*ppsz)
        {
            psz = static_cast<LPSTR>(MemReAlloc(*ppsz, sizeof(CHAR) * cch, TRUE));
        }
        else
        {
            psz = static_cast<LPSTR>(MemAlloc(sizeof(CHAR) * cch, TRUE));
        }
        StrExitOnNull(psz, hr, E_OUTOFMEMORY, "failed to allocate string, len: %u", cch);

        *ppsz = psz;
    }

    if (0 == ::WideCharToMultiByte(uiCodepage, 0, wzSource, 0 == cchSource ? -1 : (int)cchSource, *ppsz, (int)cch, NULL, NULL))
    {
        StrExitWithLastError(hr, "failed to convert to ansi: %ls", wzSource);
    }
    (*ppsz)[cchDest] = L'\0';

LExit:
    return hr;
}


/********************************************************************
StrAllocStringAnsi - allocates or reuses dynamic string memory and copies in an existing ANSI string

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource must equal the length of wzSource (not including the NULL terminator)
NOTE: if cchSource == 0, length of szSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocStringAnsi(
    __deref_out_ecount_z(cchSource+1) LPWSTR* ppwz,
    __in_z LPCSTR szSource,
    __in SIZE_T cchSource,
    __in UINT uiCodepage
    )
{
    Assert(ppwz && szSource);

    HRESULT hr = S_OK;
    LPWSTR pwz = NULL;
    SIZE_T cch = 0;
    SIZE_T cchDest = cchSource;  // at least enough

    if (*ppwz)
    {
        hr = StrMaxLength(*ppwz, &cch);
        StrExitOnFailure(hr, "failed to get size of destination string");
    }

    if (0 == cchSource)
    {
        cchDest = ::MultiByteToWideChar(uiCodepage, 0, szSource, -1, NULL, 0);
        if (0 == cchDest)
        {
            StrExitWithLastError(hr, "failed to get required size for conversion to unicode: %s", szSource);
        }

        --cchDest; //subtract one because MultiByteToWideChar includes space for the NULL terminator that we track below
    }
    else if (L'\0' == szSource[cchSource - 1]) // if the source already had a null terminator, don't count that in the character count because we track it below
    {
        cchDest = cchSource - 1;
    }

    if (cch < cchDest + 1)
    {
        cch = cchDest + 1;
        if (cch >= MAXDWORD / sizeof(WCHAR))
        {
            hr = E_OUTOFMEMORY;
            StrExitOnFailure(hr, "Not enough memory to allocate string of size: %u", cch);
        }

        if (*ppwz)
        {
            pwz = static_cast<LPWSTR>(MemReAlloc(*ppwz, sizeof(WCHAR) * cch, TRUE));
        }
        else
        {
            pwz = static_cast<LPWSTR>(MemAlloc(sizeof(WCHAR) * cch, TRUE));
        }

        StrExitOnNull(pwz, hr, E_OUTOFMEMORY, "failed to allocate string, len: %u", cch);

        *ppwz = pwz;
    }

    if (0 == ::MultiByteToWideChar(uiCodepage, 0, szSource, 0 == cchSource ? -1 : (int)cchSource, *ppwz, (int)cch))
    {
        StrExitWithLastError(hr, "failed to convert to unicode: %s", szSource);
    }
    (*ppwz)[cchDest] = L'\0';

LExit:
    return hr;
}


/********************************************************************
StrAnsiAllocStringAnsi - allocates or reuses dynamic string memory and copies in an existing string

NOTE: caller is responsible for freeing ppsz even if function fails
NOTE: cchSource does not have to equal the length of wzSource
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
HRESULT DAPI StrAnsiAllocStringAnsi(
    __deref_out_ecount_z(cchSource+1) LPSTR* ppsz,
    __in_z LPCSTR szSource,
    __in SIZE_T cchSource
    )
{
    Assert(ppsz && szSource); // && *szSource);

    HRESULT hr = S_OK;
    SIZE_T cch = 0;

    if (*ppsz)
    {
        hr = StrMaxLengthAnsi(*ppsz, &cch);
        StrExitOnRootFailure(hr, "failed to get size of destination string");
    }

    if (0 == cchSource && szSource)
    {
        hr = ::StringCchLengthA(szSource, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cchSource));
        StrExitOnRootFailure(hr, "failed to get length of source string");
    }

    SIZE_T cchNeeded;
    hr = ::ULongPtrAdd(cchSource, 1, &cchNeeded); // add one for the null terminator
    StrExitOnRootFailure(hr, "source string is too long");

    if (cch < cchNeeded)
    {
        cch = cchNeeded;
        hr = StrAnsiAlloc(ppsz, cch);
        StrExitOnFailure(hr, "failed to allocate string from string.");
    }

    // copy everything (the NULL terminator will be included)
#pragma prefast(push)
#pragma prefast(disable:25068)
    hr = ::StringCchCopyNExA(*ppsz, cch, szSource, cchSource, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
#pragma prefast(pop)

LExit:
    return hr;
}


/********************************************************************
StrAllocPrefix - allocates or reuses dynamic string memory and
                 prefixes a string

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchPrefix does not have to equal the length of wzPrefix
NOTE: if cchPrefix == 0, length of wzPrefix is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocPrefix(
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzPrefix,
    __in SIZE_T cchPrefix
    )
{
    Assert(ppwz && wzPrefix);

    HRESULT hr = S_OK;
    SIZE_T cch = 0;
    SIZE_T cchLen = 0;

    if (*ppwz)
    {
        hr = StrMaxLength(*ppwz, &cch);
        StrExitOnFailure(hr, "failed to get size of destination string");

        hr = ::StringCchLengthW(*ppwz, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchLen));
        StrExitOnFailure(hr, "Failed to calculate length of string");
    }

    Assert(cchLen <= cch);

    if (0 == cchPrefix)
    {
        hr = ::StringCchLengthW(wzPrefix, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchPrefix));
        StrExitOnFailure(hr, "Failed to calculate length of string");
    }

    if (cch - cchLen < cchPrefix + 1)
    {
        cch = cchPrefix + cchLen + 1;
        hr = StrAlloc(ppwz, cch);
        StrExitOnFailure(hr, "failed to allocate string from string: %ls", wzPrefix);
    }

    if (*ppwz)
    {
        SIZE_T cb = cch * sizeof(WCHAR);
        SIZE_T cbPrefix = cchPrefix * sizeof(WCHAR);

        memmove(*ppwz + cchPrefix, *ppwz, cb - cbPrefix);
        memcpy(*ppwz, wzPrefix, cbPrefix);
    }
    else
    {
        hr = E_UNEXPECTED;
        StrExitOnFailure(hr, "for some reason our buffer is still null");
    }

LExit:
    return hr;
}


/********************************************************************
StrAllocConcat - allocates or reuses dynamic string memory and adds an existing string

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource does not have to equal the length of wzSource
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocConcat(
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource
    )
{
    return AllocConcatHelper(ppwz, wzSource, cchSource, FALSE);
}


/********************************************************************
StrAllocConcatSecure - allocates or reuses dynamic string memory and
adds an existing string. If the memory needs to reallocated, calls
SecureZeroMemory on the original block of memory after it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource does not have to equal the length of wzSource
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAllocConcatSecure(
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource
    )
{
    return AllocConcatHelper(ppwz, wzSource, cchSource, TRUE);
}


/********************************************************************
AllocConcatHelper - allocates or reuses dynamic string memory and adds an existing string
If fZeroOnRealloc is true and the memory needs to reallocated,
calls SecureZeroMemory on original block of memory after it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
NOTE: cchSource does not have to equal the length of wzSource
NOTE: if cchSource == 0, length of wzSource is used instead
********************************************************************/
static HRESULT AllocConcatHelper(
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource,
    __in BOOL fZeroOnRealloc
    )
{
    Assert(ppwz && wzSource); // && *wzSource);

    HRESULT hr = S_OK;
    SIZE_T cch = 0;
    SIZE_T cchLen = 0;

    if (*ppwz)
    {
        hr = StrMaxLength(*ppwz, &cch);
        StrExitOnFailure(hr, "failed to get size of destination string");

        hr = ::StringCchLengthW(*ppwz, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchLen));
        StrExitOnFailure(hr, "Failed to calculate length of string");
    }

    Assert(cchLen <= cch);

    if (0 == cchSource)
    {
        hr = ::StringCchLengthW(wzSource, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchSource));
        StrExitOnFailure(hr, "Failed to calculate length of string");
    }

    if (cch - cchLen < cchSource + 1)
    {
        cch = (cchSource + cchLen + 1) * 2;
        hr = AllocHelper(ppwz, cch, fZeroOnRealloc);
        StrExitOnFailure(hr, "failed to allocate string from string: %ls", wzSource);
    }

    if (*ppwz)
    {
        hr = ::StringCchCatNExW(*ppwz, cch, wzSource, cchSource, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
    }
    else
    {
        hr = E_UNEXPECTED;
        StrExitOnFailure(hr, "for some reason our buffer is still null");
    }

LExit:
    return hr;
}


/********************************************************************
StrAnsiAllocConcat - allocates or reuses dynamic string memory and adds an existing string

NOTE: caller is responsible for freeing ppz even if function fails
NOTE: cchSource does not have to equal the length of pzSource
NOTE: if cchSource == 0, length of pzSource is used instead
********************************************************************/
extern "C" HRESULT DAPI StrAnsiAllocConcat(
    __deref_out_z LPSTR* ppz,
    __in_z LPCSTR pzSource,
    __in SIZE_T cchSource
    )
{
    Assert(ppz && pzSource); // && *pzSource);

    HRESULT hr = S_OK;
    SIZE_T cch = 0;
    SIZE_T cchLen = 0;

    if (*ppz)
    {
        hr = StrMaxLengthAnsi(*ppz, &cch);
        StrExitOnFailure(hr, "failed to get size of destination string");

#pragma prefast(push)
#pragma prefast(disable:25068)
        hr = ::StringCchLengthA(*ppz, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchLen));
#pragma prefast(pop)
        StrExitOnFailure(hr, "Failed to calculate length of string");
    }

    Assert(cchLen <= cch);

    if (0 == cchSource)
    {
#pragma prefast(push)
#pragma prefast(disable:25068)
        hr = ::StringCchLengthA(pzSource, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchSource));
#pragma prefast(pop)
        StrExitOnFailure(hr, "Failed to calculate length of string");
    }

    if (cch - cchLen < cchSource + 1)
    {
        cch = (cchSource + cchLen + 1) * 2;
        hr = StrAnsiAlloc(ppz, cch);
        StrExitOnFailure(hr, "failed to allocate string from string: %hs", pzSource);
    }

    if (*ppz)
    {
#pragma prefast(push)
#pragma prefast(disable:25068)
        hr = ::StringCchCatNExA(*ppz, cch, pzSource, cchSource, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
#pragma prefast(pop)
    }
    else
    {
        hr = E_UNEXPECTED;
        StrExitOnFailure(hr, "for some reason our buffer is still null");
    }

LExit:
    return hr;
}


/********************************************************************
StrAllocFormatted - allocates or reuses dynamic string memory and formats it

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT __cdecl StrAllocFormatted(
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    ...
    )
{
    Assert(ppwz && wzFormat && *wzFormat);

    HRESULT hr = S_OK;
    va_list args;

    va_start(args, wzFormat);
    hr = StrAllocFormattedArgs(ppwz, wzFormat, args);
    va_end(args);

    return hr;
}


/********************************************************************
StrAllocConcatFormatted - allocates or reuses dynamic string memory
and adds a formatted string

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT __cdecl StrAllocConcatFormatted(
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    ...
    )
{
    Assert(ppwz && wzFormat && *wzFormat);

    HRESULT hr = S_OK;
    LPWSTR sczFormatted = NULL;
    va_list args;

    va_start(args, wzFormat);
    hr = StrAllocFormattedArgs(&sczFormatted, wzFormat, args);
    va_end(args);
    StrExitOnFailure(hr, "Failed to allocate formatted string");

    hr = StrAllocConcat(ppwz, sczFormatted, 0);

LExit:
    ReleaseStr(sczFormatted);

    return hr;
}


/********************************************************************
StrAllocConcatFormattedSecure - allocates or reuses dynamic string
memory and adds a formatted string. If the memory needs to be
reallocated, calls SecureZeroMemory on original block of memory after
it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT __cdecl StrAllocConcatFormattedSecure(
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    ...
    )
{
    Assert(ppwz && wzFormat && *wzFormat);

    HRESULT hr = S_OK;
    LPWSTR sczFormatted = NULL;
    va_list args;

    va_start(args, wzFormat);
    hr = StrAllocFormattedArgsSecure(&sczFormatted, wzFormat, args);
    va_end(args);
    StrExitOnFailure(hr, "Failed to allocate formatted string");

    hr = StrAllocConcatSecure(ppwz, sczFormatted, 0);

LExit:
    ReleaseStr(sczFormatted);

    return hr;
}


/********************************************************************
StrAllocFormattedSecure - allocates or reuses dynamic string memory
and formats it. If the memory needs to be reallocated,
calls SecureZeroMemory on original block of memory after it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT __cdecl StrAllocFormattedSecure(
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    ...
    )
{
    Assert(ppwz && wzFormat && *wzFormat);

    HRESULT hr = S_OK;
    va_list args;

    va_start(args, wzFormat);
    hr = StrAllocFormattedArgsSecure(ppwz, wzFormat, args);
    va_end(args);

    return hr;
}


/********************************************************************
StrAnsiAllocFormatted - allocates or reuses dynamic ANSI string memory and formats it

NOTE: caller is responsible for freeing ppsz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAnsiAllocFormatted(
    __deref_out_z LPSTR* ppsz,
    __in __format_string LPCSTR szFormat,
    ...
    )
{
    Assert(ppsz && szFormat && *szFormat);

    HRESULT hr = S_OK;
    va_list args;

    va_start(args, szFormat);
    hr = StrAnsiAllocFormattedArgs(ppsz, szFormat, args);
    va_end(args);

    return hr;
}


/********************************************************************
StrAllocFormattedArgs - allocates or reuses dynamic string memory
and formats it with the passed in args

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAllocFormattedArgs(
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    __in va_list args
    )
{
    return AllocFormattedArgsHelper(ppwz, FALSE, wzFormat, args);
}


/********************************************************************
StrAllocFormattedArgsSecure - allocates or reuses dynamic string memory
and formats it with the passed in args.

If the memory needs to reallocated, calls SecureZeroMemory on the
original block of memory after it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAllocFormattedArgsSecure(
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    __in va_list args
    )
{
    return AllocFormattedArgsHelper(ppwz, TRUE, wzFormat, args);
}


/********************************************************************
AllocFormattedArgsHelper - allocates or reuses dynamic string memory
and formats it with the passed in args.

If fZeroOnRealloc is true and the memory needs to reallocated,
calls SecureZeroMemory on original block of memory after it is moved.

NOTE: caller is responsible for freeing ppwz even if function fails
********************************************************************/
static HRESULT AllocFormattedArgsHelper(
    __deref_out_z LPWSTR* ppwz,
    __in BOOL fZeroOnRealloc,
    __in __format_string LPCWSTR wzFormat,
    __in va_list args
    )
{
    Assert(ppwz && wzFormat && *wzFormat);

    HRESULT hr = S_OK;
    SIZE_T cch = 0;
    LPWSTR pwzOriginal = NULL;
    SIZE_T cbOriginal = 0;
    size_t cchOriginal = 0;

    if (*ppwz)
    {
        hr = StrSize(*ppwz, &cbOriginal);
        StrExitOnFailure(hr, "failed to get size of destination string");

        cch = cbOriginal / sizeof(WCHAR);  //convert the count in bytes to count in characters

        hr = ::StringCchLengthW(*ppwz, STRSAFE_MAX_CCH, &cchOriginal);
        StrExitOnRootFailure(hr, "failed to get length of original string");
    }

    if (0 == cch)   // if there is no space in the string buffer
    {
        cch = 256;

        hr = AllocHelper(ppwz, cch, fZeroOnRealloc);
        StrExitOnFailure(hr, "failed to allocate string to format: %ls", wzFormat);
    }

    // format the message (grow until it fits or there is a failure)
    do
    {
        hr = ::StringCchVPrintfW(*ppwz, cch, wzFormat, args);
        if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
        {
            if (!pwzOriginal)
            {
                // this allows you to pass the original string as a formatting argument and not crash
                // save the original string and free it after the printf is complete
                pwzOriginal = *ppwz;
                *ppwz = NULL;

                // StringCchVPrintfW starts writing to the string...
                // NOTE: this hack only works with sprintf(&pwz, "%s ...", pwz, ...);
                pwzOriginal[cchOriginal] = 0;
            }

            cch *= 2;

            hr = AllocHelper(ppwz, cch, fZeroOnRealloc);
            StrExitOnFailure(hr, "failed to allocate string to format: %ls", wzFormat);

            hr = S_FALSE;
        }
    } while (S_FALSE == hr);
    StrExitOnRootFailure(hr, "failed to format string");

LExit:
    if (pwzOriginal && fZeroOnRealloc)
    {
        SecureZeroMemory(pwzOriginal, cbOriginal);
    }

    ReleaseStr(pwzOriginal);

    return hr;
}


/********************************************************************
StrAnsiAllocFormattedArgs - allocates or reuses dynamic ANSI string memory
and formats it with the passed in args

NOTE: caller is responsible for freeing ppsz even if function fails
********************************************************************/
extern "C" HRESULT DAPI StrAnsiAllocFormattedArgs(
    __deref_out_z LPSTR* ppsz,
    __in __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    Assert(ppsz && szFormat && *szFormat);

    HRESULT hr = S_OK;
    SIZE_T cch = 0;
    LPSTR pszOriginal = NULL;
    size_t cchOriginal = 0;

    if (*ppsz)
    {
        hr = StrMaxLengthAnsi(*ppsz, &cch);
        StrExitOnFailure(hr, "failed to get size of destination string");

        hr = ::StringCchLengthA(*ppsz, STRSAFE_MAX_CCH, &cchOriginal);
        StrExitOnRootFailure(hr, "failed to get length of original string");
    }

    if (0 == cch)   // if there is no space in the string buffer
    {
        cch = 256;
        hr = StrAnsiAlloc(ppsz, cch);
        StrExitOnFailure(hr, "failed to allocate string to format: %s", szFormat);
    }

    // format the message (grow until it fits or there is a failure)
    do
    {
#pragma prefast(push)
#pragma prefast(disable:25068) // We intentionally don't use the unicode API here
        hr = ::StringCchVPrintfA(*ppsz, cch, szFormat, args);
#pragma prefast(pop)
        if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
        {
            if (!pszOriginal)
            {
                // this allows you to pass the original string as a formatting argument and not crash
                // save the original string and free it after the printf is complete
                pszOriginal = *ppsz;
                *ppsz = NULL;
                // StringCchVPrintfW starts writing to the string...
                // NOTE: this hack only works with sprintf(&pwz, "%s ...", pwz, ...);
                pszOriginal[cchOriginal] = 0;
            }
            cch *= 2;
            hr = StrAnsiAlloc(ppsz, cch);
            StrExitOnFailure(hr, "failed to allocate string to format: %hs", szFormat);
            hr = S_FALSE;
        }
    } while (S_FALSE == hr);
    StrExitOnRootFailure(hr, "failed to format string");

LExit:
    ReleaseStr(pszOriginal);

    return hr;
}


/********************************************************************
StrAllocFromError - returns the string for a particular error.

********************************************************************/
extern "C" HRESULT DAPI StrAllocFromError(
    __inout LPWSTR *ppwzMessage,
    __in HRESULT hrError,
    __in_opt HMODULE hModule,
    ...
    )
{
    HRESULT hr = S_OK;
    DWORD dwFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_MAX_WIDTH_MASK | FORMAT_MESSAGE_FROM_SYSTEM;
    LPVOID pvMessage = NULL;
    DWORD cchMessage = 0;

    if (hModule)
    {
        dwFlags |= FORMAT_MESSAGE_FROM_HMODULE;
    }

    va_list args;
    va_start(args, hModule);
    cchMessage = ::FormatMessageW(dwFlags, static_cast<LPCVOID>(hModule), hrError, 0, reinterpret_cast<LPWSTR>(&pvMessage), 0, &args);
    va_end(args);

    if (0 == cchMessage)
    {
        StrExitWithLastError(hr, "Failed to format message for error: 0x%x", hrError);
    }

    hr = StrAllocString(ppwzMessage, reinterpret_cast<LPCWSTR>(pvMessage), cchMessage);
    StrExitOnFailure(hr, "Failed to allocate string for message.");

LExit:
    if (pvMessage)
    {
        ::LocalFree(pvMessage);
    }

    return hr;
}


/********************************************************************
StrMaxLength - returns maximum number of characters that can be stored in dynamic string p

NOTE:  assumes Unicode string
********************************************************************/
extern "C" HRESULT DAPI StrMaxLength(
    __in LPCVOID p,
    __out SIZE_T* pcch
    )
{
    Assert(pcch);

    HRESULT hr = S_OK;

    if (p)
    {
        hr = StrSize(p, pcch);
        StrExitOnFailure(hr, "Failed to get size of string buffer.");

        *pcch /= sizeof(WCHAR);   // reduce to count of characters
    }
    else
    {
        *pcch = 0;
    }
    Assert(S_OK == hr);

LExit:
    return hr;
}


/********************************************************************
StrMaxLengthAnsi - returns maximum number of characters that can be stored in dynamic string p

NOTE:  assumes non-Unicode string
********************************************************************/
extern "C" HRESULT DAPI StrMaxLengthAnsi(
    __in LPCVOID p,
    __out SIZE_T* pcch
    )
{
    Assert(pcch);

    HRESULT hr = S_OK;

    if (p)
    {
        hr = StrSize(p, pcch);
        StrExitOnFailure(hr, "Failed to get size of string buffer.");

        *pcch /= sizeof(CHAR);   // reduce to count of characters
    }
    else
    {
        *pcch = 0;
    }
    Assert(S_OK == hr);

LExit:
    return hr;
}


/********************************************************************
StrSize - returns count of bytes in dynamic string p

********************************************************************/
extern "C" HRESULT DAPI StrSize(
    __in LPCVOID p,
    __out SIZE_T* pcb
    )
{
    Assert(p && pcb);

    return MemSizeChecked(p, pcb);
}

/********************************************************************
StrFree - releases dynamic string memory allocated by any StrAlloc*() functions

********************************************************************/
extern "C" HRESULT DAPI StrFree(
    __in LPVOID p
    )
{
    Assert(p);

    HRESULT hr = MemFree(p);
    StrExitOnFailure(hr, "failed to free string");

LExit:
    return hr;
}


/****************************************************************************
StrReplaceStringAll - Replaces wzOldSubString in ppOriginal with a wzNewSubString.
Replaces all instances.

****************************************************************************/
extern "C" HRESULT DAPI StrReplaceStringAll(
    __inout LPWSTR* ppwzOriginal,
    __in_z LPCWSTR wzOldSubString,
    __in_z LPCWSTR wzNewSubString
    )
{
    HRESULT hr = S_OK;
    DWORD dwStartIndex = 0;

    do
    {
        hr = StrReplaceString(ppwzOriginal, &dwStartIndex, wzOldSubString, wzNewSubString);
        StrExitOnFailure(hr, "Failed to replace substring");
    }
    while (S_OK == hr);

    hr = (0 == dwStartIndex) ? S_FALSE : S_OK;

LExit:
    return hr;
}


/****************************************************************************
StrReplaceString - Replaces wzOldSubString in ppOriginal with a wzNewSubString.
Search for old substring starts at dwStartIndex.  Does only 1 replace.

****************************************************************************/
extern "C" HRESULT DAPI StrReplaceString(
    __inout LPWSTR* ppwzOriginal,
    __inout DWORD* pdwStartIndex,
    __in_z LPCWSTR wzOldSubString,
    __in_z LPCWSTR wzNewSubString
    )
{
    Assert(ppwzOriginal && wzOldSubString && wzNewSubString);

    HRESULT hr = S_FALSE;
    LPCWSTR wzSubLocation = NULL;
    LPWSTR pwzBuffer = NULL;
    size_t cchOldSubString = 0;
    size_t cchNewSubString = 0;

    if (!*ppwzOriginal)
    {
        ExitFunction();
    }

    wzSubLocation = wcsstr(*ppwzOriginal + *pdwStartIndex, wzOldSubString);
    if (!wzSubLocation)
    {
        ExitFunction();
    }

    if (wzOldSubString)
    {
        hr = ::StringCchLengthW(wzOldSubString, STRSAFE_MAX_CCH, &cchOldSubString);
        StrExitOnRootFailure(hr, "Failed to get old string length.");
    }

    if (wzNewSubString)
    {
        hr = ::StringCchLengthW(wzNewSubString, STRSAFE_MAX_CCH, &cchNewSubString);
        StrExitOnRootFailure(hr, "Failed to get new string length.");
    }

    hr = ::PtrdiffTToDWord(wzSubLocation - *ppwzOriginal, pdwStartIndex);
    StrExitOnRootFailure(hr, "Failed to diff pointers.");

    hr = StrAllocString(&pwzBuffer, *ppwzOriginal, wzSubLocation - *ppwzOriginal);
    StrExitOnFailure(hr, "Failed to duplicate string.");

    pwzBuffer[wzSubLocation - *ppwzOriginal] = '\0';

    hr = StrAllocConcat(&pwzBuffer, wzNewSubString, 0);
    StrExitOnFailure(hr, "Failed to append new string.");

    hr = StrAllocConcat(&pwzBuffer, wzSubLocation + cchOldSubString, 0);
    StrExitOnFailure(hr, "Failed to append post string.");

    hr = StrFree(*ppwzOriginal);
    StrExitOnFailure(hr, "Failed to free original string.");

    *ppwzOriginal = pwzBuffer;
    *pdwStartIndex = *pdwStartIndex + static_cast<DWORD>(cchNewSubString);
    hr = S_OK;

LExit:
    return hr;
}


static inline BYTE HexCharToByte(
    __in WCHAR wc
    )
{
    Assert(L'0' <= wc && wc <= L'9' || L'a' <= wc && wc <= L'f' || L'A' <= wc && wc <= L'F');  // make sure wc is a hex character

    BYTE b;
    if (L'0' <= wc && wc <= L'9')
    {
        b = (BYTE)(wc - L'0');
    }
    else if ('a' <= wc && wc <= 'f')
    {
        b = (BYTE)(wc - L'0' - (L'a' - L'9' - 1));
    }
    else  // must be (L'A' <= wc && wc <= L'F')
    {
        b = (BYTE)(wc - L'0' - (L'A' - L'9' - 1));
    }

    Assert(0 <= b && b <= 15);
    return b;
}


/****************************************************************************
StrHexEncode - converts an array of bytes to a text string

NOTE: wzDest must have space for cbSource * 2 + 1 characters
****************************************************************************/
extern "C" HRESULT DAPI StrHexEncode(
    __in_ecount(cbSource) const BYTE* pbSource,
    __in SIZE_T cbSource,
    __out_ecount(cchDest) LPWSTR wzDest,
    __in SIZE_T cchDest
    )
{
    Assert(pbSource && wzDest);

    HRESULT hr = S_OK;
    DWORD i;
    BYTE b;

    if (cchDest < 2 * cbSource + 1)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
    }

    for (i = 0; i < cbSource; ++i)
    {
        b = (*pbSource) >> 4;
        *(wzDest++) = (WCHAR)(L'0' + b + ((b < 10) ? 0 : L'A'-L'9'-1));
        b = (*pbSource) & 0xF;
        *(wzDest++) = (WCHAR)(L'0' + b + ((b < 10) ? 0 : L'A'-L'9'-1));

        ++pbSource;
    }

    *wzDest = 0;

LExit:
    return hr;
}


/****************************************************************************
StrAllocHexEncode - converts an array of bytes to an allocated text string

****************************************************************************/
HRESULT DAPI StrAllocHexEncode(
    __in_ecount(cbSource) const BYTE* pbSource,
    __in SIZE_T cbSource,
    __deref_out_ecount_z(2*(cbSource+1)) LPWSTR* ppwzDest
    )
{
    HRESULT hr = S_OK;
    SIZE_T cchSource = sizeof(WCHAR) * (cbSource + 1);

    hr = StrAlloc(ppwzDest, cchSource);
    StrExitOnFailure(hr, "Failed to allocate hex string.");

    hr = StrHexEncode(pbSource, cbSource, *ppwzDest, cchSource);
    StrExitOnFailure(hr, "Failed to encode hex string.");

LExit:
    return hr;
}


/****************************************************************************
StrHexDecode - converts a string of text to array of bytes

NOTE: wzSource must contain even number of characters
****************************************************************************/
extern "C" HRESULT DAPI StrHexDecode(
    __in_z LPCWSTR wzSource,
    __out_bcount(cbDest) BYTE* pbDest,
    __in SIZE_T cbDest
    )
{
    Assert(wzSource && pbDest);

    HRESULT hr = S_OK;
    size_t cchSource = 0;
    size_t i = 0;
    BYTE b = 0;

    hr = ::StringCchLengthW(wzSource, STRSAFE_MAX_CCH, &cchSource);
    StrExitOnRootFailure(hr, "Failed to get length of hex string: %ls", wzSource);

    Assert(0 == cchSource % 2);
    if (cbDest < cchSource / 2)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        StrExitOnRootFailure(hr, "Insufficient buffer to decode string '%ls' len: %Iu into %Iu bytes.", wzSource, cchSource, cbDest);
    }

    for (i = 0; i < cchSource / 2; ++i)
    {
        b = HexCharToByte(*wzSource++);
        (*pbDest) = b << 4;

        b = HexCharToByte(*wzSource++);
        (*pbDest) |= b & 0xF;

        ++pbDest;
    }

LExit:
    return hr;
}


/****************************************************************************
StrAllocHexDecode - allocates a byte array hex-converted from string of text

NOTE: wzSource must contain even number of characters
****************************************************************************/
extern "C" HRESULT DAPI StrAllocHexDecode(
    __in_z LPCWSTR wzSource,
    __out_bcount(*pcbDest) BYTE** ppbDest,
    __out_opt DWORD* pcbDest
    )
{
    Assert(wzSource && *wzSource && ppbDest);

    HRESULT hr = S_OK;
    size_t cch = 0;
    BYTE* pb = NULL;
    DWORD cb = 0;

    hr = ::StringCchLengthW(wzSource, STRSAFE_MAX_CCH, &cch);
    StrExitOnFailure(hr, "Failed to calculate length of source string.");

    if (cch % 2)
    {
        hr = E_INVALIDARG;
        StrExitOnFailure(hr, "Invalid source parameter, string must be even length or it cannot be decoded.");
    }

    cb = static_cast<DWORD>(cch / 2);
    pb = static_cast<BYTE*>(MemAlloc(cb, TRUE));
    StrExitOnNull(pb, hr, E_OUTOFMEMORY, "Failed to allocate memory for hex decode.");

    hr = StrHexDecode(wzSource, pb, cb);
    StrExitOnFailure(hr, "Failed to decode source string.");

    *ppbDest = pb;
    pb = NULL;

    if (pcbDest)
    {
        *pcbDest = cb;
    }

LExit:
    ReleaseMem(pb);

    return hr;
}


/****************************************************************************
Base85 encoding/decoding data

****************************************************************************/
const WCHAR Base85EncodeTable[] = L"!%'()*+,-./0123456789:;?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_abcdefghijklmnopqrstuvwxyz{|}~";

const BYTE Base85DecodeTable[256] =
{
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
    85,  0, 85, 85, 85,  1, 85,  2,  3,  4,  5,  6,  7,  8,  9, 10,
    11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 85, 85, 85, 23,
    24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
    40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 85, 52, 53, 54,
    85, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
    70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85,
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85,
    85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85
};

const UINT Base85PowerTable[4] = { 1, 85, 85*85, 85*85*85 };


/****************************************************************************
StrAllocBase85Encode - converts an array of bytes into an XML compatible string

****************************************************************************/
extern "C" HRESULT DAPI StrAllocBase85Encode(
    __in_bcount_opt(cbSource) const BYTE* pbSource,
    __in SIZE_T cbSource,
    __deref_out_z LPWSTR* pwzDest
    )
{
    HRESULT hr = S_OK;
    SIZE_T cchDest = 0;
    LPWSTR wzDest;
    DWORD_PTR iSource = 0;
    DWORD_PTR iDest = 0;

    if (!pwzDest || !pbSource)
    {
        return E_INVALIDARG;
    }

    // calc actual size of output
    cchDest = cbSource / 4;
    cchDest += cchDest * 4;
    if (cbSource & 3)
    {
        cchDest += (cbSource & 3) + 1;
    }
    ++cchDest; // add room for null terminator

    hr = StrAlloc(pwzDest, cchDest);
    StrExitOnFailure(hr, "failed to allocate destination string");

    wzDest = *pwzDest;

    // first, encode full words
    for (iSource = 0, iDest = 0; (iSource + 4 < cbSource) && (iDest + 5 < cchDest); iSource += 4, iDest += 5)
    {
        DWORD n = pbSource[iSource] + (pbSource[iSource + 1] << 8) + (pbSource[iSource + 2] << 16) + (pbSource[iSource + 3] << 24);
        DWORD k = n / 85;

        //Assert(0 <= (n - k * 85) && (n - k * 85) < countof(Base85EncodeTable));
        wzDest[iDest] = Base85EncodeTable[n - k * 85];
        n = k / 85;

        //Assert(0 <= (k - n * 85) && (k - n * 85) < countof(Base85EncodeTable));
        wzDest[iDest + 1] = Base85EncodeTable[k - n * 85];
        k = n / 85;

        //Assert(0 <= (n - k * 85) && (n - k * 85) < countof(Base85EncodeTable));
        wzDest[iDest + 2] = Base85EncodeTable[n - k * 85];
        n = k / 85;

        //Assert(0 <= (k - n * 85) && (k - n * 85) < countof(Base85EncodeTable));
        wzDest[iDest + 3] = Base85EncodeTable[k - n * 85];

        __assume(n <= DWORD_MAX / 85 / 85 / 85 / 85);

        //Assert(0 <= n && n < countof(Base85EncodeTable));
        wzDest[iDest + 4] = Base85EncodeTable[n];
    }

    // encode any remaining bytes
    if (iSource < cbSource)
    {
        DWORD n = 0;
        for (DWORD i = 0; iSource + i < cbSource; ++i)
        {
            n += pbSource[iSource + i] << (i << 3);
        }

        for (/* iSource already initialized */; iSource < cbSource && iDest < cchDest; ++iSource, ++iDest)
        {
            DWORD k = n / 85;

            //Assert(0 <= (n - k * 85) && (n - k * 85) < countof(Base85EncodeTable));
            wzDest[iDest] = Base85EncodeTable[n - k * 85];

            n = k;
        }

        wzDest[iDest] = Base85EncodeTable[n];
        ++iDest;
    }
    Assert(iSource == cbSource);
    Assert(iDest == cchDest - 1);

    wzDest[iDest] = L'\0';
    hr = S_OK;

LExit:
    return hr;
}


/****************************************************************************
StrAllocBase85Decode - converts a string of text to array of bytes

NOTE: Use MemFree() to release the allocated stream of bytes
****************************************************************************/
extern "C" HRESULT DAPI StrAllocBase85Decode(
    __in_z LPCWSTR wzSource,
    __deref_out_bcount(*pcbDest) BYTE** ppbDest,
    __out SIZE_T* pcbDest
    )
{
    HRESULT hr = S_OK;
    size_t cchSource = 0;
    DWORD_PTR i, n, k;

    BYTE* pbDest = 0;
    SIZE_T cbDest = 0;

    if (!wzSource || !ppbDest || !pcbDest)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    hr = ::StringCchLengthW(wzSource, STRSAFE_MAX_CCH, &cchSource);
    StrExitOnRootFailure(hr, "failed to get length of base 85 string: %ls", wzSource);

    // evaluate size of output and check it
    k = cchSource / 5;
    cbDest = k << 2;
    k = cchSource - k * 5;
    if (k)
    {
        if (1 == k)
        {
            // decode error -- encoded size cannot equal 1 mod 5
            return E_UNEXPECTED;
        }

        cbDest += k - 1;
    }

    *ppbDest = static_cast<BYTE*>(MemAlloc(cbDest, FALSE));
    StrExitOnNull(*ppbDest, hr, E_OUTOFMEMORY, "failed allocate memory to decode the string");

    pbDest = *ppbDest;
    *pcbDest = cbDest;

    // decode full words first
    while (5 <= cchSource)
    {
        k = Base85DecodeTable[wzSource[0]];
        if (85 == k)
        {
            // illegal symbol
            return E_UNEXPECTED;
        }
        n = k;

        k = Base85DecodeTable[wzSource[1]];
        if (85 == k)
        {
            // illegal symbol
            return E_UNEXPECTED;
        }
        n += k * 85;

        k = Base85DecodeTable[wzSource[2]];
        if (85 == k)
        {
            // illegal symbol
            return E_UNEXPECTED;
        }
        n += k * (85 * 85);

        k = Base85DecodeTable[wzSource[3]];
        if (85 == k)
        {
            // illegal symbol
            return E_UNEXPECTED;
        }
        n += k * (85 * 85 * 85);

        k = Base85DecodeTable[wzSource[4]];
        if (85 == k)
        {
            // illegal symbol
            return E_UNEXPECTED;
        }
        k *= (85 * 85 * 85 * 85);

        // if (k + n > (1u << 32)) <=> (k > ~n) then decode error
        if (k > ~n)
        {
            // overflow
            return E_UNEXPECTED;
        }

        n += k;

        pbDest[0] = (BYTE) n;
        pbDest[1] = (BYTE) (n >> 8);
        pbDest[2] = (BYTE) (n >> 16);
        pbDest[3] = (BYTE) (n >> 24);

        wzSource += 5;
        pbDest += 4;
        cchSource -= 5;
    }

    if (cchSource)
    {
        n = 0;
        for (i = 0; i < cchSource; ++i)
        {
            k = Base85DecodeTable[wzSource[i]];
            if (85 == k)
            {
                // illegal symbol
                return E_UNEXPECTED;
            }

            n += k * Base85PowerTable[i];
        }

        for (i = 1; i < cchSource; ++i)
        {
            *pbDest++ = (BYTE)n;
            n >>= 8;
        }

        if (0 != n)
        {
            // decode error
            return E_UNEXPECTED;
        }
    }

    hr = S_OK;

LExit:
    return hr;
}


/****************************************************************************
MultiSzLen - calculates the length of a MULTISZ string including all nulls
including the double null terminator at the end of the MULTISZ.

NOTE: returns 0 if the multisz in not properly terminated with two nulls
****************************************************************************/
extern "C" HRESULT DAPI MultiSzLen(
    __in_ecount(*pcch) __nullnullterminated LPCWSTR pwzMultiSz,
    __out SIZE_T* pcch
    )
{
    Assert(pcch);
    if (!pwzMultiSz)
    {
        *pcch = 0;
        return S_OK;
    }

    HRESULT hr = S_OK;
    LPCWSTR wz = pwzMultiSz;
    DWORD_PTR dwMaxSize = 0;

    hr = StrMaxLength(pwzMultiSz, &dwMaxSize);
    StrExitOnFailure(hr, "failed to get the max size of a string while calculating MULTISZ length");

    *pcch = 0;
    while (*pcch < dwMaxSize)
    {
        if (L'\0' == *wz && L'\0' == *(wz + 1))
        {
            break;
        }

        ++wz;
        *pcch = *pcch + 1;
    }

    // Add two for the last 2 NULLs (that we looked ahead at)
    *pcch = *pcch + 2;

    // If we've walked off the end then the length is 0
    if (*pcch > dwMaxSize)
    {
        *pcch = 0;
    }

LExit:
    return hr;
}


/****************************************************************************
MultiSzPrepend - prepends a string onto the front of a MUTLISZ

****************************************************************************/
extern "C" HRESULT DAPI MultiSzPrepend(
    __deref_inout_ecount(*pcchMultiSz) __nullnullterminated LPWSTR* ppwzMultiSz,
    __inout_opt SIZE_T* pcchMultiSz,
    __in __nullnullterminated LPCWSTR pwzInsert
    )
{
    Assert(ppwzMultiSz && pwzInsert && *pwzInsert);

    HRESULT hr =S_OK;
    LPWSTR pwzResult = NULL;
    SIZE_T cchResult = 0;
    SIZE_T cchInsert = 0;
    SIZE_T cchMultiSz = 0;

    // Get the lengths of the MULTISZ (and prime it if it's not initialized)
    if (pcchMultiSz && 0 != *pcchMultiSz)
    {
        cchMultiSz = *pcchMultiSz;
    }
    else
    {
        hr = MultiSzLen(*ppwzMultiSz, &cchMultiSz);
        StrExitOnFailure(hr, "failed to get length of multisz");
    }

    hr = ::StringCchLengthW(pwzInsert, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cchInsert));
    StrExitOnRootFailure(hr, "failed to get length of insert string");

    cchResult = cchInsert + (cchMultiSz ? cchMultiSz : 1) + 1;

    // Allocate the result buffer
    hr = StrAlloc(&pwzResult, cchResult);
    StrExitOnFailure(hr, "failed to allocate result string");

    // Prepend
    hr = ::StringCchCopyW(pwzResult, cchResult, pwzInsert);
    StrExitOnRootFailure(hr, "failed to copy prepend string: %ls", pwzInsert);

    // If there was no MULTISZ, double null terminate our result, otherwise, copy the MULTISZ in
    if (0 == cchMultiSz)
    {
        pwzResult[cchResult - 2] = L'\0';
    }
    else
    {
        // Copy the rest
        ::CopyMemory(pwzResult + cchInsert + 1, *ppwzMultiSz, cchMultiSz * sizeof(WCHAR));

        // Free the old buffer
        ReleaseNullStr(*ppwzMultiSz);
    }

    // Set the result
    pwzResult[cchResult - 1] = L'\0';
    *ppwzMultiSz = pwzResult;

    if (pcchMultiSz)
    {
        *pcchMultiSz = cchResult;
    }

    pwzResult = NULL;

LExit:
    ReleaseNullStr(pwzResult);

    return hr;
}

/****************************************************************************
MultiSzFindSubstring - case insensitive find of a string in a MULTISZ that contains the
specified sub string and returns the index of the
string in the MULTISZ, the address, neither, or both

NOTE: returns S_FALSE if the string is not found
****************************************************************************/
extern "C" HRESULT DAPI MultiSzFindSubstring(
    __in __nullnullterminated LPCWSTR pwzMultiSz,
    __in __nullnullterminated LPCWSTR pwzSubstring,
    __out_opt DWORD_PTR* pdwIndex,
    __deref_opt_out __nullnullterminated LPCWSTR* ppwzFoundIn
    )
{
    Assert(pwzMultiSz && *pwzMultiSz && pwzSubstring && *pwzSubstring);

    HRESULT hr = S_FALSE; // Assume we won't find it (the glass is half empty)
    LPCWSTR wz = pwzMultiSz;
    DWORD_PTR dwIndex = 0;
    SIZE_T cchMultiSz = 0;
    SIZE_T cchProgress = 0;

    hr = MultiSzLen(pwzMultiSz, &cchMultiSz);
    StrExitOnFailure(hr, "failed to get the length of a MULTISZ string");

    // Find the string containing the sub string
    hr = S_OK;
    while (NULL == wcsistr(wz, pwzSubstring))
    {
        // Slide through to the end of the current string
        while (L'\0' != *wz && cchProgress < cchMultiSz)
        {
            ++wz;
            ++cchProgress;
        }

        // If we're done, we're done
        if (L'\0' == *(wz + 1) || cchProgress >= cchMultiSz)
        {
            hr = S_FALSE;
            break;
        }

        // Move on to the next string
        ++wz;
        ++dwIndex;
    }
    Assert(S_OK == hr || S_FALSE == hr);

    // If we found it give them what they want
    if (S_OK == hr)
    {
        if (pdwIndex)
        {
            *pdwIndex = dwIndex;
        }

        if (ppwzFoundIn)
        {
            *ppwzFoundIn = wz;
        }
    }

LExit:
    return hr;
}

/****************************************************************************
MultiSzFindString - finds a string in a MULTISZ and returns the index of
the string in the MULTISZ, the address or both

NOTE: returns S_FALSE if the string is not found
****************************************************************************/
extern "C" HRESULT DAPI MultiSzFindString(
    __in __nullnullterminated LPCWSTR pwzMultiSz,
    __in __nullnullterminated LPCWSTR pwzString,
    __out_opt DWORD_PTR* pdwIndex,
    __deref_opt_out __nullnullterminated LPCWSTR* ppwzFound
    )
{
    Assert(pwzMultiSz && *pwzMultiSz && pwzString && *pwzString && (pdwIndex || ppwzFound));

    HRESULT hr = S_FALSE; // Assume we won't find it
    LPCWSTR wz = pwzMultiSz;
    DWORD_PTR dwIndex = 0;
    SIZE_T cchMutliSz = 0;
    SIZE_T cchProgress = 0;

    hr = MultiSzLen(pwzMultiSz, &cchMutliSz);
    StrExitOnFailure(hr, "failed to get the length of a MULTISZ string");

    // Find the string
    hr = S_OK;
    while (0 != lstrcmpW(wz, pwzString))
    {
        // Slide through to the end of the current string
        while (L'\0' != *wz && cchProgress < cchMutliSz)
        {
            ++wz;
            ++cchProgress;
        }

        // If we're done, we're done
        if (L'\0' == *(wz + 1) || cchProgress >= cchMutliSz)
        {
            hr = S_FALSE;
            break;
        }

        // Move on to the next string
        ++wz;
        ++dwIndex;
    }
    Assert(S_OK == hr || S_FALSE == hr);

    // If we found it give them what they want
    if (S_OK == hr)
    {
        if (pdwIndex)
        {
            *pdwIndex = dwIndex;
        }

        if (ppwzFound)
        {
            *ppwzFound = wz;
        }
    }

LExit:
    return hr;
}

/****************************************************************************
MultiSzRemoveString - removes string from a MULTISZ at the specified
index

NOTE: does an in place removal without shrinking the memory allocation

NOTE: returns S_FALSE if the MULTISZ has fewer strings than dwIndex
****************************************************************************/
extern "C" HRESULT DAPI MultiSzRemoveString(
    __deref_inout __nullnullterminated LPWSTR* ppwzMultiSz,
    __in DWORD_PTR dwIndex
    )
{
    Assert(ppwzMultiSz && *ppwzMultiSz);

    HRESULT hr = S_OK;
    LPCWSTR wz = *ppwzMultiSz;
    LPCWSTR wzNext = NULL;
    DWORD_PTR dwCurrentIndex = 0;
    SIZE_T cchMultiSz = 0;
    SIZE_T cchProgress = 0;

    hr = MultiSzLen(*ppwzMultiSz, &cchMultiSz);
    StrExitOnFailure(hr, "failed to get the length of a MULTISZ string");

    // Find the index we want to remove
    hr = S_OK;
    while (dwCurrentIndex < dwIndex)
    {
        // Slide through to the end of the current string
        while (L'\0' != *wz && cchProgress < cchMultiSz)
        {
            ++wz;
            ++cchProgress;
        }

        // If we're done, we're done
        if (L'\0' == *(wz + 1) || cchProgress >= cchMultiSz)
        {
            hr = S_FALSE;
            break;
        }

        // Move on to the next string
        ++wz;
        ++cchProgress;
        ++dwCurrentIndex;
    }
    Assert(S_OK == hr || S_FALSE == hr);

    // If we found the index to be removed
    if (S_OK == hr)
    {
        wzNext = wz;

        // Slide through to the end of the current string
        while (L'\0' != *wzNext && cchProgress < cchMultiSz)
        {
            ++wzNext;
            ++cchProgress;
        }

        // Something weird has happened if we're past the end of the MULTISZ
        if (cchProgress > cchMultiSz)
        {
            hr = E_UNEXPECTED;
            StrExitOnFailure(hr, "failed to move past the string to be removed from MULTISZ");
        }

        // Move on to the next character
        ++wzNext;
        ++cchProgress;

        ::MoveMemory((LPVOID)wz, (LPVOID)wzNext, (cchMultiSz - cchProgress) * sizeof(WCHAR));
    }

LExit:
    return hr;
}

/****************************************************************************
MultiSzInsertString - inserts new string at the specified index

****************************************************************************/
extern "C" HRESULT DAPI MultiSzInsertString(
    __deref_inout __nullnullterminated LPWSTR* ppwzMultiSz,
    __inout_opt SIZE_T* pcchMultiSz,
    __in DWORD_PTR dwIndex,
    __in_z LPCWSTR pwzInsert
    )
{
    Assert(ppwzMultiSz && pwzInsert && *pwzInsert);

    HRESULT hr = S_OK;
    LPCWSTR wz = *ppwzMultiSz;
    DWORD_PTR dwCurrentIndex = 0;
    SIZE_T cchProgress = 0;
    LPWSTR pwzResult = NULL;
    SIZE_T cchResult = 0;
    SIZE_T cchString = 0;
    SIZE_T cchMultiSz = 0;

    hr = ::StringCchLengthW(pwzInsert, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cchString));
    StrExitOnRootFailure(hr, "failed to get length of insert string");

    if (pcchMultiSz && 0 != *pcchMultiSz)
    {
        cchMultiSz = *pcchMultiSz;
    }
    else
    {
        hr = MultiSzLen(*ppwzMultiSz, &cchMultiSz);
        StrExitOnFailure(hr, "failed to get the length of a MULTISZ string");
    }

    // Find the index we want to insert at
    hr = S_OK;
    while (dwCurrentIndex < dwIndex)
    {
        // Slide through to the end of the current string
        while (L'\0' != *wz && cchProgress < cchMultiSz)
        {
            ++wz;
            ++cchProgress;
        }

        // If we're done, we're done
        if ((dwCurrentIndex + 1 != dwIndex && L'\0' == *(wz + 1)) || cchProgress >= cchMultiSz)
        {
            hr = HRESULT_FROM_WIN32(ERROR_OBJECT_NOT_FOUND);
            StrExitOnRootFailure(hr, "requested to insert into an invalid index: %u in a MULTISZ", dwIndex);
        }

        // Move on to the next string
        ++wz;
        ++cchProgress;
        ++dwCurrentIndex;
    }

    //
    // Insert the string
    //
    cchResult = (cchMultiSz ? cchMultiSz : 1) + cchString + 1;

    hr = StrAlloc(&pwzResult, cchResult);
    StrExitOnFailure(hr, "failed to allocate result string for MULTISZ insert");

    // Copy the part before the insert
    if (cchProgress)
    {
        ::CopyMemory(pwzResult, *ppwzMultiSz, cchProgress * sizeof(WCHAR));
    }

    // Copy the insert part
    ::CopyMemory(pwzResult + cchProgress, pwzInsert, (cchString + 1) * sizeof(WCHAR));

    // Copy the part after the insert
    if (cchMultiSz > cchProgress)
    {
        ::CopyMemory(pwzResult + cchProgress + cchString + 1, wz, (cchMultiSz - cchProgress) * sizeof(WCHAR));
    }

    // Ensure double-null termination
    pwzResult[cchResult-1] = NULL;
    pwzResult[cchResult-2] = NULL;

    // Free the old buffer
    ReleaseNullStr(*ppwzMultiSz);

    // Set the result
    *ppwzMultiSz = pwzResult;

    // If they wanted the resulting length, let 'em have it
    if (pcchMultiSz)
    {
        *pcchMultiSz = cchResult;
    }

    pwzResult = NULL;

LExit:
    ReleaseStr(pwzResult);

    return hr;
}

/****************************************************************************
MultiSzReplaceString - replaces string at the specified index with a new one

****************************************************************************/
extern "C" HRESULT DAPI MultiSzReplaceString(
    __deref_inout __nullnullterminated LPWSTR* ppwzMultiSz,
    __in DWORD_PTR dwIndex,
    __in_z LPCWSTR pwzString
    )
{
    Assert(ppwzMultiSz && pwzString && *pwzString);

    HRESULT hr = S_OK;

    hr = MultiSzRemoveString(ppwzMultiSz, dwIndex);
    StrExitOnFailure(hr, "failed to remove string from MULTISZ at the specified index: %u", dwIndex);

    hr = MultiSzInsertString(ppwzMultiSz, NULL, dwIndex, pwzString);
    StrExitOnFailure(hr, "failed to insert string into MULTISZ at the specified index: %u", dwIndex);

LExit:
    return hr;
}


/****************************************************************************
wcsistr - case insensitive find a substring

****************************************************************************/
extern "C" LPCWSTR DAPI wcsistr(
    __in_z LPCWSTR wzString,
    __in_z LPCWSTR wzCharSet
    )
{
    LPCWSTR wzSource = wzString;
    LPCWSTR wzSearch = NULL;
    SIZE_T cchSourceIndex = 0;

    // Walk through wzString (the source string) one character at a time
    while (*wzSource)
    {
        cchSourceIndex = 0;
        wzSearch = wzCharSet;

        // Look ahead in the source string until we get a full match or we hit the end of the source
        while (L'\0' != wzSource[cchSourceIndex] && L'\0' != *wzSearch && towlower(wzSource[cchSourceIndex]) == towlower(*wzSearch))
        {
            ++cchSourceIndex;
            ++wzSearch;
        }

        // If we found it, return the point that we found it at
        if (L'\0' == *wzSearch)
        {
            return wzSource;
        }

        // Walk ahead one character
        ++wzSource;
    }

    return NULL;
}

/****************************************************************************
StrStringToInt16 - converts a string to a signed 16-bit integer.

****************************************************************************/
extern "C" HRESULT DAPI StrStringToInt16(
    __in_z LPCWSTR wzIn,
    __in DWORD cchIn,
    __out SHORT* psOut
    )
{
    HRESULT hr = S_OK;
    LONGLONG ll = 0;

    hr = StrStringToInt64(wzIn, cchIn, &ll);
    StrExitOnFailure(hr, "Failed to parse int64.");

    if (SHORT_MAX < ll || SHORT_MIN > ll)
    {
        ExitFunction1(hr = DISP_E_OVERFLOW);
    }
    *psOut = (SHORT)ll;

LExit:
    return hr;
}

/****************************************************************************
StrStringToUInt16 - converts a string to an unsigned 16-bit integer.

****************************************************************************/
extern "C" HRESULT DAPI StrStringToUInt16(
    __in_z LPCWSTR wzIn,
    __in DWORD cchIn,
    __out USHORT* pusOut
    )
{
    HRESULT hr = S_OK;
    ULONGLONG ull = 0;

    hr = StrStringToUInt64(wzIn, cchIn, &ull);
    StrExitOnFailure(hr, "Failed to parse uint64 to convert to uint16.");

    if (USHORT_MAX < ull)
    {
        ExitFunction1(hr = DISP_E_OVERFLOW);
    }
    *pusOut = (USHORT)ull;

LExit:
    return hr;
}

/****************************************************************************
StrStringToInt32 - converts a string to a signed 32-bit integer.

****************************************************************************/
extern "C" HRESULT DAPI StrStringToInt32(
    __in_z LPCWSTR wzIn,
    __in DWORD cchIn,
    __out INT* piOut
    )
{
    HRESULT hr = S_OK;
    LONGLONG ll = 0;

    hr = StrStringToInt64(wzIn, cchIn, &ll);
    StrExitOnFailure(hr, "Failed to parse int64.");

    if (INT_MAX < ll || INT_MIN > ll)
    {
        ExitFunction1(hr = DISP_E_OVERFLOW);
    }
    *piOut = (INT)ll;

LExit:
    return hr;
}

/****************************************************************************
StrStringToUInt32 - converts a string to an unsigned 32-bit integer.

****************************************************************************/
extern "C" HRESULT DAPI StrStringToUInt32(
    __in_z LPCWSTR wzIn,
    __in DWORD cchIn,
    __out UINT* puiOut
    )
{
    HRESULT hr = S_OK;
    ULONGLONG ull = 0;

    hr = StrStringToUInt64(wzIn, cchIn, &ull);
    StrExitOnFailure(hr, "Failed to parse uint64 to convert to uint32.");

    if (UINT_MAX < ull)
    {
        ExitFunction1(hr = DISP_E_OVERFLOW);
    }
    *puiOut = (UINT)ull;

LExit:
    return hr;
}

/****************************************************************************
StrStringToInt64 - converts a string to a signed 64-bit integer.

****************************************************************************/
extern "C" HRESULT DAPI StrStringToInt64(
    __in_z LPCWSTR wzIn,
    __in DWORD cchIn,
    __out LONGLONG* pllOut
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    INT iSign = 1;
    INT nDigit = 0;
    LARGE_INTEGER liValue = { };
    size_t cchString = 0;

    // get string length if not provided
    if (0 >= cchIn)
    {
        hr = ::StringCchLengthW(wzIn, STRSAFE_MAX_CCH, &cchString);
        StrExitOnRootFailure(hr, "Failed to get length of string.");

        cchIn = (DWORD)cchString;
        if (0 >= cchIn)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
    }

    // check sign
    if (L'-' == wzIn[0])
    {
        if (1 >= cchIn)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
        i = 1;
        iSign = -1;
    }

    // read digits
    while (i < cchIn)
    {
        nDigit = wzIn[i] - L'0';
        if (0 > nDigit || 9 < nDigit)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
        liValue.QuadPart = liValue.QuadPart * 10 + nDigit * iSign;

        if ((liValue.HighPart ^ iSign) & INT_MIN)
        {
            ExitFunction1(hr = DISP_E_OVERFLOW);
        }
        ++i;
    }

    *pllOut = liValue.QuadPart;

LExit:
    return hr;
}

/****************************************************************************
StrStringToUInt64 - converts a string to an unsigned 64-bit integer.

****************************************************************************/
extern "C" HRESULT DAPI StrStringToUInt64(
    __in_z LPCWSTR wzIn,
    __in DWORD cchIn,
    __out ULONGLONG* pullOut
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    DWORD nDigit = 0;
    ULONGLONG ullValue = 0;
    ULONGLONG ull = 0;
    size_t cchString = 0;

    // get string length if not provided
    if (0 >= cchIn)
    {
        hr = ::StringCchLengthW(wzIn, STRSAFE_MAX_CCH, &cchString);
        StrExitOnRootFailure(hr, "Failed to get length of string.");

        cchIn = (DWORD)cchString;
        if (0 >= cchIn)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
    }

    // read digits
    while (i < cchIn)
    {
        nDigit = wzIn[i] - L'0';
        if (0 > nDigit || 9 < nDigit)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
        ull = (ULONGLONG)(ullValue * 10 + nDigit);

        if (ull < ullValue)
        {
            ExitFunction1(hr = DISP_E_OVERFLOW);
        }
        ullValue = ull;
        ++i;
    }

    *pullOut = ullValue;

LExit:
    return hr;
}

/****************************************************************************
StrStringToUpper - alters the given string in-place to be entirely uppercase

****************************************************************************/
void DAPI StrStringToUpper(
    __inout_z LPWSTR wzIn
    )
{
    ::CharUpperBuffW(wzIn, lstrlenW(wzIn));
}

/****************************************************************************
StrStringToLower - alters the given string in-place to be entirely lowercase

****************************************************************************/
void DAPI StrStringToLower(
    __inout_z LPWSTR wzIn
    )
{
    ::CharLowerBuffW(wzIn, lstrlenW(wzIn));
}

/****************************************************************************
StrAllocStringToUpperInvariant - creates an upper-case copy of a string.

****************************************************************************/
extern "C" HRESULT DAPI StrAllocStringToUpperInvariant(
    __deref_out_z LPWSTR* pscz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource
    )
{
    return StrAllocStringMapInvariant(pscz, wzSource, cchSource, LCMAP_UPPERCASE);
}

/****************************************************************************
StrAllocStringToLowerInvariant - creates an lower-case copy of a string.

****************************************************************************/
extern "C" HRESULT DAPI StrAllocStringToLowerInvariant(
    __deref_out_z LPWSTR* pscz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource
    )
{
    return StrAllocStringMapInvariant(pscz, wzSource, cchSource, LCMAP_LOWERCASE);
}

/****************************************************************************
StrArrayAllocString - Allocates a string array.

****************************************************************************/
extern "C" HRESULT DAPI StrArrayAllocString(
    __deref_inout_ecount_opt(*pcStrArray) LPWSTR **prgsczStrArray,
    __inout LPUINT pcStrArray,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource
    )
{
    HRESULT hr = S_OK;
    UINT cNewStrArray;

    hr = ::UIntAdd(*pcStrArray, 1, &cNewStrArray);
    StrExitOnFailure(hr, "Failed to increment the string array element count.");

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgsczStrArray), cNewStrArray, sizeof(LPWSTR), ARRAY_GROWTH_SIZE);
    StrExitOnFailure(hr, "Failed to allocate memory for the string array.");

    hr = StrAllocString(&(*prgsczStrArray)[*pcStrArray], wzSource, cchSource);
    StrExitOnFailure(hr, "Failed to allocate and assign the string.");

    *pcStrArray = cNewStrArray;

LExit:
    return hr;
}

/****************************************************************************
StrArrayFree - Frees a string array.

Use ReleaseNullStrArray to nullify the arguments.

****************************************************************************/
extern "C" HRESULT DAPI StrArrayFree(
    __in_ecount(cStrArray) LPWSTR *rgsczStrArray,
    __in UINT cStrArray
    )
{
    HRESULT hr = S_OK;

    for (UINT i = 0; i < cStrArray; ++i)
    {
        if (NULL != rgsczStrArray[i])
        {
            hr = StrFree(rgsczStrArray[i]);
            StrExitOnFailure(hr, "Failed to free the string at index %u.", i);
        }
    }

    hr = MemFree(rgsczStrArray);
    StrExitOnFailure(hr, "Failed to free memory for the string array.");

LExit:
    return hr;
}

/****************************************************************************
StrSplitAllocArray - Splits a string into an array.

****************************************************************************/
extern "C" HRESULT DAPI StrSplitAllocArray(
    __deref_inout_ecount_opt(*pcStrArray) LPWSTR **prgsczStrArray,
    __inout LPUINT pcStrArray,
    __in_z LPCWSTR wzSource,
    __in_z LPCWSTR wzDelim
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCopy = NULL;
    LPWSTR wzContext = NULL;

    // Copy wzSource so it is not modified.
    hr = StrAllocString(&sczCopy, wzSource, 0);
    StrExitOnFailure(hr, "Failed to copy the source string.");

    for (LPCWSTR wzToken = ::wcstok_s(sczCopy, wzDelim, &wzContext); wzToken; wzToken = ::wcstok_s(NULL, wzDelim, &wzContext))
    {
        hr = StrArrayAllocString(prgsczStrArray, pcStrArray, wzToken, 0);
        StrExitOnFailure(hr, "Failed to add the string to the string array.");
    }

LExit:
    ReleaseStr(sczCopy);

    return hr;
}

/****************************************************************************
StrAllocStringMapInvariant - helper function for the ToUpper and ToLower.

Note: Assumes source and destination buffers will be the same.
****************************************************************************/
static HRESULT StrAllocStringMapInvariant(
    __deref_out_z LPWSTR* pscz,
    __in_z LPCWSTR wzSource,
    __in SIZE_T cchSource,
    __in DWORD dwMapFlags
    )
{
    HRESULT hr = S_OK;

    hr = StrAllocString(pscz, wzSource, cchSource);
    StrExitOnFailure(hr, "Failed to allocate a copy of the source string.");

    if (0 == cchSource)
    {
        // Need the actual string size for LCMapString. This includes the null-terminator
        // but LCMapString doesn't care either way.
        hr = ::StringCchLengthW(*pscz, INT_MAX, reinterpret_cast<size_t*>(&cchSource));
        StrExitOnRootFailure(hr, "Failed to get the length of the string.");
    }
    else if (INT_MAX < cchSource)
    {
        StrExitOnRootFailure(hr = E_INVALIDARG, "Source string is too long: %Iu", cchSource);
    }

    // Convert the copy of the string to upper or lower case in-place.
    if (0 == ::LCMapStringW(LOCALE_INVARIANT, dwMapFlags, *pscz, static_cast<int>(cchSource), *pscz, static_cast<int>(cchSource)))
    {
        StrExitWithLastError(hr, "Failed to convert the string case.");
    }

LExit:
    return hr;
}

/****************************************************************************
StrSecureZeroString - zeroes out string to the make sure the contents
don't remain in memory.

****************************************************************************/
extern "C" DAPI_(HRESULT) StrSecureZeroString(
    __in LPWSTR pwz
    )
{
    HRESULT hr = S_OK;
    SIZE_T cb = 0;

    if (pwz)
    {
        hr = StrSize(pwz, &cb);
        StrExitOnFailure(hr, "Failed to get size of string");

        SecureZeroMemory(pwz, cb);
    }

LExit:
    return hr;
}

/****************************************************************************
StrSecureZeroFreeString - zeroes out string to the make sure the contents
don't remain in memory, then frees the string.

****************************************************************************/
extern "C" DAPI_(HRESULT) StrSecureZeroFreeString(
    __in LPWSTR pwz
    )
{
    HRESULT hr = S_OK;

    hr = StrSecureZeroString(pwz);
    ReleaseStr(pwz);

    return hr;
}
