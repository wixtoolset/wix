#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseVerutilVersion(p) if (p) { VerFreeVersion(p); p = NULL; }

typedef struct _VERUTIL_VERSION_RELEASE_LABEL
{
    BOOL fNumeric;
    DWORD dwValue;
    SIZE_T cchLabelOffset;
    int cchLabel;
} VERUTIL_VERSION_RELEASE_LABEL;

typedef struct _VERUTIL_VERSION
{
    LPWSTR sczVersion;
    DWORD dwMajor;
    DWORD dwMinor;
    DWORD dwPatch;
    DWORD dwRevision;
    DWORD cReleaseLabels;
    VERUTIL_VERSION_RELEASE_LABEL* rgReleaseLabels;
    SIZE_T cchMetadataOffset;
    BOOL fInvalid;
} VERUTIL_VERSION;

/*******************************************************************
 VerCompareParsedVersions - compares the Verutil versions.

*******************************************************************/
HRESULT DAPI VerCompareParsedVersions(
    __in VERUTIL_VERSION* pVersion1,
    __in VERUTIL_VERSION* pVersion2,
    __out int* pnResult
    );

/*******************************************************************
 VerCompareStringVersions - parses the strings with VerParseVersion and then
                            compares the Verutil versions with VerCompareParsedVersions.

*******************************************************************/
HRESULT DAPI VerCompareStringVersions(
    __in_z LPCWSTR wzVersion1,
    __in_z LPCWSTR wzVersion2,
    __in BOOL fStrict,
    __out int* pnResult
    );

/********************************************************************
 VerCopyVersion - copies the given Verutil version.

*******************************************************************/
HRESULT DAPI VerCopyVersion(
    __in VERUTIL_VERSION* pSource,
    __out VERUTIL_VERSION** ppVersion
    );

/********************************************************************
 VerFreeVersion - frees any memory associated with a Verutil version.

*******************************************************************/
void DAPI VerFreeVersion(
    __in VERUTIL_VERSION* pVersion
    );

/*******************************************************************
 VerParseVersion - parses the string into a Verutil version.

*******************************************************************/
HRESULT DAPI VerParseVersion(
    __in_z LPCWSTR wzVersion,
    __in DWORD cchVersion,
    __in BOOL fStrict,
    __out VERUTIL_VERSION** ppVersion
    );

/*******************************************************************
 VerParseVersion - parses the QWORD into a Verutil version.

*******************************************************************/
HRESULT DAPI VerVersionFromQword(
    __in DWORD64 qwVersion,
    __out VERUTIL_VERSION** ppVersion
    );

#ifdef __cplusplus
}
#endif
