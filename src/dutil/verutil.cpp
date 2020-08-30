// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// Exit macros
#define VerExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_VERUTIL, x, s, __VA_ARGS__)
#define VerExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_VERUTIL, x, s, __VA_ARGS__)
#define VerExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_VERUTIL, x, s, __VA_ARGS__)
#define VerExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_VERUTIL, x, s, __VA_ARGS__)
#define VerExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_VERUTIL, x, s, __VA_ARGS__)
#define VerExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_VERUTIL, x, s, __VA_ARGS__)
#define VerExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_VERUTIL, p, x, e, s, __VA_ARGS__)
#define VerExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_VERUTIL, p, x, s, __VA_ARGS__)
#define VerExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_VERUTIL, p, x, e, s, __VA_ARGS__)
#define VerExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_VERUTIL, p, x, s, __VA_ARGS__)
#define VerExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_VERUTIL, e, x, s, __VA_ARGS__)

// constants
const DWORD GROW_RELEASE_LABELS = 3;

// Forward declarations.
static int CompareDword(
    __in const DWORD& dw1,
    __in const DWORD& dw2
    );
static HRESULT CompareReleaseLabel(
    __in const VERUTIL_VERSION_RELEASE_LABEL* p1,
    __in LPCWSTR wzVersion1,
    __in const VERUTIL_VERSION_RELEASE_LABEL* p2,
    __in LPCWSTR wzVersion2,
    __out int* pnResult
    );
static HRESULT CompareVersionSubstring(
    __in LPCWSTR wzString1,
    __in int cchCount1,
    __in LPCWSTR wzString2,
    __in int cchCount2,
    __out int* pnResult
    );


DAPI_(HRESULT) VerCompareParsedVersions(
    __in VERUTIL_VERSION* pVersion1,
    __in VERUTIL_VERSION* pVersion2,
    __out int* pnResult
    )
{
    HRESULT hr = S_OK;
    int nResult = 0;
    DWORD cMaxReleaseLabels = 0;
    BOOL fCompareMetadata = FALSE;

    if (!pVersion1 || !pVersion1->sczVersion ||
        !pVersion2 || !pVersion2->sczVersion)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    if (pVersion1 == pVersion2)
    {
        ExitFunction1(nResult = 0);
    }

    nResult = CompareDword(pVersion1->dwMajor, pVersion2->dwMajor);
    if (0 != nResult)
    {
        ExitFunction();
    }

    nResult = CompareDword(pVersion1->dwMinor, pVersion2->dwMinor);
    if (0 != nResult)
    {
        ExitFunction();
    }

    nResult = CompareDword(pVersion1->dwPatch, pVersion2->dwPatch);
    if (0 != nResult)
    {
        ExitFunction();
    }

    nResult = CompareDword(pVersion1->dwRevision, pVersion2->dwRevision);
    if (0 != nResult)
    {
        ExitFunction();
    }

    if (pVersion1->fInvalid)
    {
        if (!pVersion2->fInvalid)
        {
            ExitFunction1(nResult = -1);
        }
        else
        {
            fCompareMetadata = TRUE;
        }
    }
    else if (pVersion2->fInvalid)
    {
        ExitFunction1(nResult = 1);
    }

    if (pVersion1->cReleaseLabels)
    {
        if (pVersion2->cReleaseLabels)
        {
            cMaxReleaseLabels = max(pVersion1->cReleaseLabels, pVersion2->cReleaseLabels);
        }
        else
        {
            ExitFunction1(nResult = -1);
        }
    }
    else if (pVersion2->cReleaseLabels)
    {
        ExitFunction1(nResult = 1);
    }

    if (cMaxReleaseLabels)
    {
        for (DWORD i = 0; i < cMaxReleaseLabels; ++i)
        {
            VERUTIL_VERSION_RELEASE_LABEL* pReleaseLabel1 = pVersion1->cReleaseLabels > i ? pVersion1->rgReleaseLabels + i : NULL;
            VERUTIL_VERSION_RELEASE_LABEL* pReleaseLabel2 = pVersion2->cReleaseLabels > i ? pVersion2->rgReleaseLabels + i : NULL;

            hr = CompareReleaseLabel(pReleaseLabel1, pVersion1->sczVersion, pReleaseLabel2, pVersion2->sczVersion, &nResult);
            if (FAILED(hr) || 0 != nResult)
            {
                ExitFunction();
            }
        }
    }

    if (fCompareMetadata)
    {
        hr = CompareVersionSubstring(pVersion1->sczVersion + pVersion1->cchMetadataOffset, -1, pVersion2->sczVersion + pVersion2->cchMetadataOffset, -1, &nResult);
    }

LExit:
    *pnResult = nResult;
    return hr;
}

DAPI_(HRESULT) VerCompareStringVersions(
    __in_z LPCWSTR wzVersion1,
    __in_z LPCWSTR wzVersion2,
    __in BOOL fStrict,
    __out int* pnResult
    )
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pVersion1 = NULL;
    VERUTIL_VERSION* pVersion2 = NULL;
    int nResult = 0;

    hr = VerParseVersion(wzVersion1, 0, fStrict, &pVersion1);
    VerExitOnFailure(hr, "Failed to parse Verutil version '%ls'", wzVersion1);

    hr = VerParseVersion(wzVersion2, 0, fStrict, &pVersion2);
    VerExitOnFailure(hr, "Failed to parse Verutil version '%ls'", wzVersion2);

    hr = VerCompareParsedVersions(pVersion1, pVersion2, &nResult);
    VerExitOnFailure(hr, "Failed to compare parsed Verutil versions '%ls' and '%ls'.", wzVersion1, wzVersion2);

LExit:
    *pnResult = nResult;

    ReleaseVerutilVersion(pVersion1);
    ReleaseVerutilVersion(pVersion2);

    return hr;
}

DAPI_(HRESULT) VerCopyVersion(
    __in VERUTIL_VERSION* pSource,
    __out VERUTIL_VERSION** ppVersion
    )
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pCopy = NULL;

    pCopy = reinterpret_cast<VERUTIL_VERSION*>(MemAlloc(sizeof(VERUTIL_VERSION), TRUE));
    VerExitOnNull(pCopy, hr, E_OUTOFMEMORY, "Failed to allocate memory for Verutil version copy.");

    hr = StrAllocString(&pCopy->sczVersion, pSource->sczVersion, 0);
    VerExitOnFailure(hr, "Failed to copy Verutil version string '%ls'.", pSource->sczVersion);

    pCopy->dwMajor = pSource->dwMajor;
    pCopy->dwMinor = pSource->dwMinor;
    pCopy->dwPatch = pSource->dwPatch;
    pCopy->dwRevision = pSource->dwRevision;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pCopy->rgReleaseLabels), 0, sizeof(VERUTIL_VERSION_RELEASE_LABEL), pSource->cReleaseLabels);
    VerExitOnFailure(hr, "Failed to allocate memory for Verutil version release labels copies.");

    pCopy->cReleaseLabels = pSource->cReleaseLabels;

    for (DWORD i = 0; i < pCopy->cReleaseLabels; ++i)
    {
        VERUTIL_VERSION_RELEASE_LABEL* pSourceLabel = pSource->rgReleaseLabels + i;
        VERUTIL_VERSION_RELEASE_LABEL* pCopyLabel = pCopy->rgReleaseLabels + i;

        pCopyLabel->cchLabelOffset = pSourceLabel->cchLabelOffset;
        pCopyLabel->cchLabel = pSourceLabel->cchLabel;
        pCopyLabel->fNumeric = pSourceLabel->fNumeric;
        pCopyLabel->dwValue = pSourceLabel->dwValue;
    }

    pCopy->cchMetadataOffset = pSource->cchMetadataOffset;
    pCopy->fInvalid = pSource->fInvalid;

    *ppVersion = pCopy;
    pCopy = NULL;

LExit:
    ReleaseVerutilVersion(pCopy);

    return hr;
}

DAPI_(void) VerFreeVersion(
    __in VERUTIL_VERSION* pVersion
    )
{
    if (pVersion)
    {
        ReleaseStr(pVersion->sczVersion);
        ReleaseMem(pVersion->rgReleaseLabels);
        ReleaseMem(pVersion);
    }
}

DAPI_(HRESULT) VerParseVersion(
    __in_z LPCWSTR wzVersion,
    __in DWORD cchVersion,
    __in BOOL fStrict,
    __out VERUTIL_VERSION** ppVersion
    )
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pVersion = NULL;
    LPCWSTR wzEnd = NULL;
    LPCWSTR wzPartBegin = NULL;
    LPCWSTR wzPartEnd = NULL;
    BOOL fInvalid = FALSE;
    BOOL fLastPart = FALSE;
    BOOL fTrailingDot = FALSE;
    BOOL fParsedVersionNumber = FALSE;
    BOOL fExpectedReleaseLabels = FALSE;
    DWORD iPart = 0;

    if (!wzVersion || !ppVersion)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    // Get string length if not provided.
    if (0 == cchVersion)
    {
        cchVersion = lstrlenW(wzVersion);
    }

    if (L'v' == *wzVersion || L'V' == *wzVersion)
    {
        ++wzVersion;
        --cchVersion;
    }

    pVersion = reinterpret_cast<VERUTIL_VERSION*>(MemAlloc(sizeof(VERUTIL_VERSION), TRUE));
    VerExitOnNull(pVersion, hr, E_OUTOFMEMORY, "Failed to allocate memory for Verutil version '%ls'.", wzVersion);

    hr = StrAllocString(&pVersion->sczVersion, wzVersion, cchVersion);
    VerExitOnFailure(hr, "Failed to copy Verutil version string '%ls'.", wzVersion);

    wzVersion = wzPartBegin = wzPartEnd = pVersion->sczVersion;

    // Save end pointer.
    wzEnd = wzVersion + cchVersion;

    // Parse version number
    while (wzPartBegin < wzEnd)
    {
        fTrailingDot = FALSE;

        // Find end of part.
        for (;;)
        {
            if (wzPartEnd >= wzEnd)
            {
                fLastPart = TRUE;
                break;
            }

            switch (*wzPartEnd)
            {
            case L'0':
            case L'1':
            case L'2':
            case L'3':
            case L'4':
            case L'5':
            case L'6':
            case L'7':
            case L'8':
            case L'9':
                ++wzPartEnd;
                continue;
            case L'.':
                fTrailingDot = TRUE;
                break;
            case L'-':
            case L'+':
                fLastPart = TRUE;
                break;
            default:
                fInvalid = TRUE;
                break;
            }

            break;
        }

        if (wzPartBegin == wzPartEnd)
        {
            fInvalid = TRUE;
        }

        if (fInvalid)
        {
            break;
        }

        DWORD cchPart = 0;
        hr = ::PtrdiffTToDWord(wzPartEnd - wzPartBegin, &cchPart);
        if (FAILED(hr))
        {
            fInvalid = TRUE;
            break;
        }

        // Parse version part.
        UINT uPart = 0;
        hr = StrStringToUInt32(wzPartBegin, cchPart, &uPart);
        if (FAILED(hr))
        {
            fInvalid = TRUE;
            break;
        }

        switch (iPart)
        {
        case 0:
            pVersion->dwMajor = uPart;
            break;
        case 1:
            pVersion->dwMinor = uPart;
            break;
        case 2:
            pVersion->dwPatch = uPart;
            break;
        case 3:
            pVersion->dwRevision = uPart;
            break;
        }

        if (fTrailingDot)
        {
            ++wzPartEnd;
        }
        wzPartBegin = wzPartEnd;
        ++iPart;

        if (4 <= iPart || fLastPart)
        {
            fParsedVersionNumber = TRUE;
            break;
        }
    }

    fInvalid |= !fParsedVersionNumber || fTrailingDot;

    if (!fInvalid && wzPartBegin < wzEnd && *wzPartBegin == L'-')
    {
        wzPartBegin = wzPartEnd = wzPartBegin + 1;
        fExpectedReleaseLabels = TRUE;
        fLastPart = FALSE;
    }

    while (fExpectedReleaseLabels && wzPartBegin < wzEnd)
    {
        fTrailingDot = FALSE;

        // Find end of part.
        for (;;)
        {
            if (wzPartEnd >= wzEnd)
            {
                fLastPart = TRUE;
                break;
            }

            if (*wzPartEnd >= L'0' && *wzPartEnd <= L'9' ||
                *wzPartEnd >= L'A' && *wzPartEnd <= L'Z' ||
                *wzPartEnd >= L'a' && *wzPartEnd <= L'z' ||
                *wzPartEnd == L'-')
            {
                ++wzPartEnd;
                continue;
            }
            else if (*wzPartEnd == L'+')
            {
                fLastPart = TRUE;
            }
            else if (*wzPartEnd == L'.')
            {
                fTrailingDot = TRUE;
            }
            else
            {
                fInvalid = TRUE;
            }

            break;
        }

        if (wzPartBegin == wzPartEnd)
        {
            fInvalid = TRUE;
        }

        if (fInvalid)
        {
            break;
        }

        int cchLabel = 0;
        hr = ::PtrdiffTToInt32(wzPartEnd - wzPartBegin, &cchLabel);
        if (FAILED(hr) || 0 > cchLabel)
        {
            fInvalid = TRUE;
            break;
        }

        hr = MemReAllocArray(reinterpret_cast<LPVOID*>(&pVersion->rgReleaseLabels), pVersion->cReleaseLabels, sizeof(VERUTIL_VERSION_RELEASE_LABEL), GROW_RELEASE_LABELS - (pVersion->cReleaseLabels % GROW_RELEASE_LABELS));
        VerExitOnFailure(hr, "Failed to allocate memory for Verutil version release labels '%ls'", wzVersion);

        VERUTIL_VERSION_RELEASE_LABEL* pReleaseLabel = pVersion->rgReleaseLabels + pVersion->cReleaseLabels;
        ++pVersion->cReleaseLabels;

        // Try to parse as number.
        UINT uLabel = 0;
        hr = StrStringToUInt32(wzPartBegin, cchLabel, &uLabel);
        if (SUCCEEDED(hr))
        {
            pReleaseLabel->fNumeric = TRUE;
            pReleaseLabel->dwValue = uLabel;
        }

        pReleaseLabel->cchLabelOffset = wzPartBegin - pVersion->sczVersion;
        pReleaseLabel->cchLabel = cchLabel;

        if (fTrailingDot)
        {
            ++wzPartEnd;
        }
        wzPartBegin = wzPartEnd;

        if (fLastPart)
        {
            break;
        }
    }

    fInvalid |= fExpectedReleaseLabels && (!pVersion->cReleaseLabels || fTrailingDot);

    if (!fInvalid && wzPartBegin < wzEnd)
    {
        if (*wzPartBegin == L'+')
        {
            wzPartBegin = wzPartEnd = wzPartBegin + 1;
        }
        else
        {
            fInvalid = TRUE;
        }
    }

    if (fInvalid && fStrict)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    pVersion->cchMetadataOffset = min(wzPartBegin, wzEnd) - pVersion->sczVersion;
    pVersion->fInvalid = fInvalid;

    *ppVersion = pVersion;
    pVersion = NULL;
    hr = S_OK;

LExit:
    ReleaseVerutilVersion(pVersion);

    return hr;
}

DAPI_(HRESULT) VerVersionFromQword(
    __in DWORD64 qwVersion,
    __out VERUTIL_VERSION** ppVersion
    )
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pVersion = NULL;

    pVersion = reinterpret_cast<VERUTIL_VERSION*>(MemAlloc(sizeof(VERUTIL_VERSION), TRUE));
    VerExitOnNull(pVersion, hr, E_OUTOFMEMORY, "Failed to allocate memory for Verutil version from QWORD.");

    pVersion->dwMajor = (WORD)(qwVersion >> 48 & 0xffff);
    pVersion->dwMinor = (WORD)(qwVersion >> 32 & 0xffff);
    pVersion->dwPatch = (WORD)(qwVersion >> 16 & 0xffff);
    pVersion->dwRevision = (WORD)(qwVersion & 0xffff);

    hr = StrAllocFormatted(&pVersion->sczVersion, L"%lu.%lu.%lu.%lu", pVersion->dwMajor, pVersion->dwMinor, pVersion->dwPatch, pVersion->dwRevision);
    ExitOnFailure(hr, "Failed to allocate and format the version string.");

    pVersion->cchMetadataOffset = lstrlenW(pVersion->sczVersion);

    *ppVersion = pVersion;
    pVersion = NULL;

LExit:
    ReleaseVerutilVersion(pVersion);

    return hr;
}


static int CompareDword(
    __in const DWORD& dw1,
    __in const DWORD& dw2
    )
{
    int nResult = 0;

    if (dw1 > dw2)
    {
        nResult = 1;
    }
    else if (dw1 < dw2)
    {
        nResult = -1;
    }

    return nResult;
}

static HRESULT CompareReleaseLabel(
    __in const VERUTIL_VERSION_RELEASE_LABEL* p1,
    __in LPCWSTR wzVersion1,
    __in const VERUTIL_VERSION_RELEASE_LABEL* p2,
    __in LPCWSTR wzVersion2,
    __out int* pnResult
    )
{
    HRESULT hr = S_OK;
    int nResult = 0;

    if (p1 == p2)
    {
        ExitFunction();
    }
    else if (p1 && !p2)
    {
        ExitFunction1(nResult = 1);
    }
    else if (!p1 && p2)
    {
        ExitFunction1(nResult = -1);
    }

    if (p1->fNumeric)
    {
        if (p2->fNumeric)
        {
            nResult = CompareDword(p1->dwValue, p2->dwValue);
        }
        else
        {
            nResult = -1;
        }
    }
    else
    {
        if (p2->fNumeric)
        {
            nResult = 1;
        }
        else
        {
            hr = CompareVersionSubstring(wzVersion1 + p1->cchLabelOffset, p1->cchLabel, wzVersion2 + p2->cchLabelOffset, p2->cchLabel, &nResult);
        }
    }

LExit:
    *pnResult = nResult;

    return hr;
}

static HRESULT CompareVersionSubstring(
    __in LPCWSTR wzString1,
    __in int cchCount1,
    __in LPCWSTR wzString2,
    __in int cchCount2,
    __out int* pnResult
    )
{
    HRESULT hr = S_OK;
    int nResult = 0;

    nResult = ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzString1, cchCount1, wzString2, cchCount2);
    if (!nResult)
    {
        VerExitOnLastError(hr, "Failed to compare version substrings");
    }

LExit:
    *pnResult = nResult - 2;

    return hr;
}
