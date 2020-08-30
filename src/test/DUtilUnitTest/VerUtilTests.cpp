// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;

namespace DutilTests
{
    public ref class VerUtil
    {
    public:
        [Fact]
        void VerCompareVersionsTreatsMissingRevisionAsZero()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            VERUTIL_VERSION* pVersion3 = NULL;
            LPCWSTR wzVersion1 = L"1.2.3.4";
            LPCWSTR wzVersion2 = L"1.2.3";
            LPCWSTR wzVersion3 = L"1.2.3.0";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                hr = VerParseVersion(wzVersion3, 0, FALSE, &pVersion3);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion3);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(1, pVersion1->dwMajor);
                Assert::Equal<DWORD>(2, pVersion1->dwMinor);
                Assert::Equal<DWORD>(3, pVersion1->dwPatch);
                Assert::Equal<DWORD>(4, pVersion1->dwRevision);
                Assert::Equal<DWORD>(0, pVersion1->cReleaseLabels);
                Assert::Equal<DWORD>(7, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion2, pVersion2->sczVersion);
                Assert::Equal<DWORD>(1, pVersion2->dwMajor);
                Assert::Equal<DWORD>(2, pVersion2->dwMinor);
                Assert::Equal<DWORD>(3, pVersion2->dwPatch);
                Assert::Equal<DWORD>(0, pVersion2->dwRevision);
                Assert::Equal<DWORD>(0, pVersion2->cReleaseLabels);
                Assert::Equal<DWORD>(5, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion2->fInvalid);

                NativeAssert::StringEqual(wzVersion3, pVersion3->sczVersion);
                Assert::Equal<DWORD>(1, pVersion3->dwMajor);
                Assert::Equal<DWORD>(2, pVersion3->dwMinor);
                Assert::Equal<DWORD>(3, pVersion3->dwPatch);
                Assert::Equal<DWORD>(0, pVersion3->dwRevision);
                Assert::Equal<DWORD>(0, pVersion3->cReleaseLabels);
                Assert::Equal<DWORD>(7, pVersion3->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion3->fInvalid);

                TestVerutilCompareParsedVersions(pVersion1, pVersion2, 1);
                TestVerutilCompareParsedVersions(pVersion3, pVersion2, 0);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
                ReleaseVerutilVersion(pVersion3);
            }
        }

        [Fact]
        void VerCompareVersionsTreatsNumericReleaseLabelsAsNumbers()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            LPCWSTR wzVersion1 = L"1.0-2.0";
            LPCWSTR wzVersion2 = L"1.0-19";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(1, pVersion1->dwMajor);
                Assert::Equal<DWORD>(0, pVersion1->dwMinor);
                Assert::Equal<DWORD>(0, pVersion1->dwPatch);
                Assert::Equal<DWORD>(0, pVersion1->dwRevision);
                Assert::Equal<DWORD>(2, pVersion1->cReleaseLabels);

                Assert::Equal<BOOL>(TRUE, pVersion1->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(2, pVersion1->rgReleaseLabels[0].dwValue);
                Assert::Equal<DWORD>(1, pVersion1->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(4, pVersion1->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<BOOL>(TRUE, pVersion1->rgReleaseLabels[1].fNumeric);
                Assert::Equal<DWORD>(0, pVersion1->rgReleaseLabels[1].dwValue);
                Assert::Equal<DWORD>(1, pVersion1->rgReleaseLabels[1].cchLabel);
                Assert::Equal<DWORD>(6, pVersion1->rgReleaseLabels[1].cchLabelOffset);

                Assert::Equal<DWORD>(7, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion2, pVersion2->sczVersion);
                Assert::Equal<DWORD>(1, pVersion2->dwMajor);
                Assert::Equal<DWORD>(0, pVersion2->dwMinor);
                Assert::Equal<DWORD>(0, pVersion2->dwPatch);
                Assert::Equal<DWORD>(0, pVersion2->dwRevision);
                Assert::Equal<DWORD>(1, pVersion2->cReleaseLabels);

                Assert::Equal<BOOL>(TRUE, pVersion2->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(19, pVersion2->rgReleaseLabels[0].dwValue);
                Assert::Equal<DWORD>(2, pVersion2->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(4, pVersion2->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<DWORD>(6, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion2->fInvalid);

                TestVerutilCompareParsedVersions(pVersion1, pVersion2, -1);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
            }
        }

        [Fact]
        void VerCompareVersionsHandlesNormallyInvalidVersions()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            VERUTIL_VERSION* pVersion3 = NULL;
            VERUTIL_VERSION* pVersion4 = NULL;
            VERUTIL_VERSION* pVersion5 = NULL;
            VERUTIL_VERSION* pVersion6 = NULL;
            LPCWSTR wzVersion1 = L"10.-4.0";
            LPCWSTR wzVersion2 = L"10.-2.0";
            LPCWSTR wzVersion3 = L"0";
            LPCWSTR wzVersion4 = L"";
            LPCWSTR wzVersion5 = L"10-2";
            LPCWSTR wzVersion6 = L"10-4.@";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                hr = VerParseVersion(wzVersion3, 0, FALSE, &pVersion3);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion3);

                hr = VerParseVersion(wzVersion4, 0, FALSE, &pVersion4);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion4);

                hr = VerParseVersion(wzVersion5, 0, FALSE, &pVersion5);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion5);

                hr = VerParseVersion(wzVersion6, 0, FALSE, &pVersion6);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion6);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(10, pVersion1->dwMajor);
                Assert::Equal<DWORD>(0, pVersion1->dwMinor);
                Assert::Equal<DWORD>(0, pVersion1->dwPatch);
                Assert::Equal<DWORD>(0, pVersion1->dwRevision);
                Assert::Equal<DWORD>(0, pVersion1->cReleaseLabels);
                Assert::Equal<DWORD>(3, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion2, pVersion2->sczVersion);
                Assert::Equal<DWORD>(10, pVersion2->dwMajor);
                Assert::Equal<DWORD>(0, pVersion2->dwMinor);
                Assert::Equal<DWORD>(0, pVersion2->dwPatch);
                Assert::Equal<DWORD>(0, pVersion2->dwRevision);
                Assert::Equal<DWORD>(0, pVersion2->cReleaseLabels);
                Assert::Equal<DWORD>(3, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion2->fInvalid);

                NativeAssert::StringEqual(wzVersion3, pVersion3->sczVersion);
                Assert::Equal<DWORD>(0, pVersion3->dwMajor);
                Assert::Equal<DWORD>(0, pVersion3->dwMinor);
                Assert::Equal<DWORD>(0, pVersion3->dwPatch);
                Assert::Equal<DWORD>(0, pVersion3->dwRevision);
                Assert::Equal<DWORD>(0, pVersion3->cReleaseLabels);
                Assert::Equal<DWORD>(1, pVersion3->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion3->fInvalid);

                NativeAssert::StringEqual(wzVersion4, pVersion4->sczVersion);
                Assert::Equal<DWORD>(0, pVersion4->dwMajor);
                Assert::Equal<DWORD>(0, pVersion4->dwMinor);
                Assert::Equal<DWORD>(0, pVersion4->dwPatch);
                Assert::Equal<DWORD>(0, pVersion4->dwRevision);
                Assert::Equal<DWORD>(0, pVersion4->cReleaseLabels);
                Assert::Equal<DWORD>(0, pVersion4->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion4->fInvalid);

                NativeAssert::StringEqual(wzVersion5, pVersion5->sczVersion);
                Assert::Equal<DWORD>(10, pVersion5->dwMajor);
                Assert::Equal<DWORD>(0, pVersion5->dwMinor);
                Assert::Equal<DWORD>(0, pVersion5->dwPatch);
                Assert::Equal<DWORD>(0, pVersion5->dwRevision);
                Assert::Equal<DWORD>(1, pVersion5->cReleaseLabels);

                Assert::Equal<BOOL>(TRUE, pVersion5->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(2, pVersion5->rgReleaseLabels[0].dwValue);
                Assert::Equal<DWORD>(1, pVersion5->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(3, pVersion5->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<DWORD>(4, pVersion5->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion5->fInvalid);

                NativeAssert::StringEqual(wzVersion6, pVersion6->sczVersion);
                Assert::Equal<DWORD>(10, pVersion6->dwMajor);
                Assert::Equal<DWORD>(0, pVersion6->dwMinor);
                Assert::Equal<DWORD>(0, pVersion6->dwPatch);
                Assert::Equal<DWORD>(0, pVersion6->dwRevision);
                Assert::Equal<DWORD>(1, pVersion6->cReleaseLabels);

                Assert::Equal<BOOL>(TRUE, pVersion6->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(4, pVersion6->rgReleaseLabels[0].dwValue);
                Assert::Equal<DWORD>(1, pVersion6->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(3, pVersion6->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<DWORD>(5, pVersion6->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion6->fInvalid);

                TestVerutilCompareParsedVersions(pVersion1, pVersion2, 1);
                TestVerutilCompareParsedVersions(pVersion3, pVersion4, 1);
                TestVerutilCompareParsedVersions(pVersion5, pVersion6, 1);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
                ReleaseVerutilVersion(pVersion3);
                ReleaseVerutilVersion(pVersion4);
                ReleaseVerutilVersion(pVersion5);
                ReleaseVerutilVersion(pVersion6);
            }
        }

        [Fact]
        void VerCompareVersionsTreatsHyphenAsVersionSeparator()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            VERUTIL_VERSION* pVersion3 = NULL;
            LPCWSTR wzVersion1 = L"0.0.1-a";
            LPCWSTR wzVersion2 = L"0-2";
            LPCWSTR wzVersion3 = L"1-2";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                hr = VerParseVersion(wzVersion3, 0, FALSE, &pVersion3);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion3);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(0, pVersion1->dwMajor);
                Assert::Equal<DWORD>(0, pVersion1->dwMinor);
                Assert::Equal<DWORD>(1, pVersion1->dwPatch);
                Assert::Equal<DWORD>(0, pVersion1->dwRevision);
                Assert::Equal<DWORD>(1, pVersion1->cReleaseLabels);

                Assert::Equal<BOOL>(FALSE, pVersion1->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(1, pVersion1->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(6, pVersion1->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<DWORD>(7, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion2, pVersion2->sczVersion);
                Assert::Equal<DWORD>(0, pVersion2->dwMajor);
                Assert::Equal<DWORD>(0, pVersion2->dwMinor);
                Assert::Equal<DWORD>(0, pVersion2->dwPatch);
                Assert::Equal<DWORD>(0, pVersion2->dwRevision);
                Assert::Equal<DWORD>(1, pVersion2->cReleaseLabels);

                Assert::Equal<BOOL>(TRUE, pVersion2->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(2, pVersion2->rgReleaseLabels[0].dwValue);
                Assert::Equal<DWORD>(1, pVersion2->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(2, pVersion2->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<DWORD>(3, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion2->fInvalid);

                NativeAssert::StringEqual(wzVersion3, pVersion3->sczVersion);
                Assert::Equal<DWORD>(1, pVersion3->dwMajor);
                Assert::Equal<DWORD>(0, pVersion3->dwMinor);
                Assert::Equal<DWORD>(0, pVersion3->dwPatch);
                Assert::Equal<DWORD>(0, pVersion3->dwRevision);
                Assert::Equal<DWORD>(1, pVersion3->cReleaseLabels);

                Assert::Equal<BOOL>(TRUE, pVersion3->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(2, pVersion3->rgReleaseLabels[0].dwValue);
                Assert::Equal<DWORD>(1, pVersion3->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(2, pVersion3->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<DWORD>(3, pVersion3->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion3->fInvalid);

                TestVerutilCompareParsedVersions(pVersion1, pVersion2, 1);
                TestVerutilCompareParsedVersions(pVersion1, pVersion3, -1);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
                ReleaseVerutilVersion(pVersion3);
            }
        }

        [Fact]
        void VerCompareVersionsIgnoresLeadingZeroes()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            VERUTIL_VERSION* pVersion3 = NULL;
            VERUTIL_VERSION* pVersion4 = NULL;
            LPCWSTR wzVersion1 = L"0.01-a.1";
            LPCWSTR wzVersion2 = L"0.1.0-a.1";
            LPCWSTR wzVersion3 = L"0.1-a.b.0";
            LPCWSTR wzVersion4 = L"0.1.0-a.b.000";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                hr = VerParseVersion(wzVersion3, 0, FALSE, &pVersion3);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion3);

                hr = VerParseVersion(wzVersion4, 0, FALSE, &pVersion4);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion4);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(0, pVersion1->dwMajor);
                Assert::Equal<DWORD>(1, pVersion1->dwMinor);
                Assert::Equal<DWORD>(0, pVersion1->dwPatch);
                Assert::Equal<DWORD>(0, pVersion1->dwRevision);
                Assert::Equal<DWORD>(2, pVersion1->cReleaseLabels);

                Assert::Equal<BOOL>(FALSE, pVersion1->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(1, pVersion1->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(5, pVersion1->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<BOOL>(TRUE, pVersion1->rgReleaseLabels[1].fNumeric);
                Assert::Equal<DWORD>(1, pVersion1->rgReleaseLabels[1].dwValue);
                Assert::Equal<DWORD>(1, pVersion1->rgReleaseLabels[1].cchLabel);
                Assert::Equal<DWORD>(7, pVersion1->rgReleaseLabels[1].cchLabelOffset);

                Assert::Equal<DWORD>(8, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion2, pVersion2->sczVersion);
                Assert::Equal<DWORD>(0, pVersion2->dwMajor);
                Assert::Equal<DWORD>(1, pVersion2->dwMinor);
                Assert::Equal<DWORD>(0, pVersion2->dwPatch);
                Assert::Equal<DWORD>(0, pVersion2->dwRevision);
                Assert::Equal<DWORD>(2, pVersion2->cReleaseLabels);

                Assert::Equal<BOOL>(FALSE, pVersion2->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(1, pVersion2->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(6, pVersion2->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<BOOL>(TRUE, pVersion2->rgReleaseLabels[1].fNumeric);
                Assert::Equal<DWORD>(1, pVersion2->rgReleaseLabels[1].dwValue);
                Assert::Equal<DWORD>(1, pVersion2->rgReleaseLabels[1].cchLabel);
                Assert::Equal<DWORD>(8, pVersion2->rgReleaseLabels[1].cchLabelOffset);

                Assert::Equal<DWORD>(9, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion2->fInvalid);

                NativeAssert::StringEqual(wzVersion3, pVersion3->sczVersion);
                Assert::Equal<DWORD>(0, pVersion3->dwMajor);
                Assert::Equal<DWORD>(1, pVersion3->dwMinor);
                Assert::Equal<DWORD>(0, pVersion3->dwPatch);
                Assert::Equal<DWORD>(0, pVersion3->dwRevision);
                Assert::Equal<DWORD>(3, pVersion3->cReleaseLabels);

                Assert::Equal<BOOL>(FALSE, pVersion3->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(1, pVersion3->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(4, pVersion3->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<BOOL>(FALSE, pVersion3->rgReleaseLabels[1].fNumeric);
                Assert::Equal<DWORD>(1, pVersion3->rgReleaseLabels[1].cchLabel);
                Assert::Equal<DWORD>(6, pVersion3->rgReleaseLabels[1].cchLabelOffset);

                Assert::Equal<BOOL>(TRUE, pVersion3->rgReleaseLabels[2].fNumeric);
                Assert::Equal<DWORD>(0, pVersion3->rgReleaseLabels[2].dwValue);
                Assert::Equal<DWORD>(1, pVersion3->rgReleaseLabels[2].cchLabel);
                Assert::Equal<DWORD>(8, pVersion3->rgReleaseLabels[2].cchLabelOffset);

                Assert::Equal<DWORD>(9, pVersion3->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion3->fInvalid);

                NativeAssert::StringEqual(wzVersion4, pVersion4->sczVersion);
                Assert::Equal<DWORD>(0, pVersion4->dwMajor);
                Assert::Equal<DWORD>(1, pVersion4->dwMinor);
                Assert::Equal<DWORD>(0, pVersion4->dwPatch);
                Assert::Equal<DWORD>(0, pVersion4->dwRevision);
                Assert::Equal<DWORD>(3, pVersion4->cReleaseLabels);

                Assert::Equal<BOOL>(FALSE, pVersion4->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(1, pVersion4->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(6, pVersion4->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<BOOL>(FALSE, pVersion4->rgReleaseLabels[1].fNumeric);
                Assert::Equal<DWORD>(1, pVersion4->rgReleaseLabels[1].cchLabel);
                Assert::Equal<DWORD>(8, pVersion4->rgReleaseLabels[1].cchLabelOffset);

                Assert::Equal<BOOL>(TRUE, pVersion4->rgReleaseLabels[2].fNumeric);
                Assert::Equal<DWORD>(0, pVersion4->rgReleaseLabels[2].dwValue);
                Assert::Equal<DWORD>(3, pVersion4->rgReleaseLabels[2].cchLabel);
                Assert::Equal<DWORD>(10, pVersion4->rgReleaseLabels[2].cchLabelOffset);

                Assert::Equal<DWORD>(13, pVersion4->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion4->fInvalid);

                TestVerutilCompareParsedVersions(pVersion1, pVersion2, 0);
                TestVerutilCompareParsedVersions(pVersion3, pVersion4, 0);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
                ReleaseVerutilVersion(pVersion3);
                ReleaseVerutilVersion(pVersion4);
            }
        }

        [Fact]
        void VerCompareVersionsTreatsUnexpectedContentAsMetadata()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            VERUTIL_VERSION* pVersion3 = NULL;
            LPCWSTR wzVersion1 = L"1.2.3+abcd";
            LPCWSTR wzVersion2 = L"1.2.3.abcd";
            LPCWSTR wzVersion3 = L"1.2.3.-abcd";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                hr = VerParseVersion(wzVersion3, 0, FALSE, &pVersion3);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion3);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(1, pVersion1->dwMajor);
                Assert::Equal<DWORD>(2, pVersion1->dwMinor);
                Assert::Equal<DWORD>(3, pVersion1->dwPatch);
                Assert::Equal<DWORD>(0, pVersion1->dwRevision);
                Assert::Equal<DWORD>(0, pVersion1->cReleaseLabels);
                Assert::Equal<DWORD>(6, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion2, pVersion2->sczVersion);
                Assert::Equal<DWORD>(1, pVersion2->dwMajor);
                Assert::Equal<DWORD>(2, pVersion2->dwMinor);
                Assert::Equal<DWORD>(3, pVersion2->dwPatch);
                Assert::Equal<DWORD>(0, pVersion2->dwRevision);
                Assert::Equal<DWORD>(0, pVersion2->cReleaseLabels);
                Assert::Equal<DWORD>(6, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion2->fInvalid);

                NativeAssert::StringEqual(wzVersion3, pVersion3->sczVersion);
                Assert::Equal<DWORD>(1, pVersion3->dwMajor);
                Assert::Equal<DWORD>(2, pVersion3->dwMinor);
                Assert::Equal<DWORD>(3, pVersion3->dwPatch);
                Assert::Equal<DWORD>(0, pVersion3->dwRevision);
                Assert::Equal<DWORD>(0, pVersion3->cReleaseLabels);
                Assert::Equal<DWORD>(6, pVersion3->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion3->fInvalid);

                TestVerutilCompareParsedVersions(pVersion1, pVersion2, 1);
                TestVerutilCompareParsedVersions(pVersion1, pVersion3, 1);
                TestVerutilCompareParsedVersions(pVersion2, pVersion3, -1);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
                ReleaseVerutilVersion(pVersion3);
            }
        }

        [Fact]
        void VerCompareVersionsIgnoresLeadingV()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            VERUTIL_VERSION* pVersion3 = NULL;
            LPCWSTR wzVersion1 = L"10.20.30.40";
            LPCWSTR wzVersion2 = L"v10.20.30.40";
            LPCWSTR wzVersion3 = L"V10.20.30.40";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                hr = VerParseVersion(wzVersion3, 0, FALSE, &pVersion3);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion3);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(10, pVersion1->dwMajor);
                Assert::Equal<DWORD>(20, pVersion1->dwMinor);
                Assert::Equal<DWORD>(30, pVersion1->dwPatch);
                Assert::Equal<DWORD>(40, pVersion1->dwRevision);
                Assert::Equal<DWORD>(0, pVersion1->cReleaseLabels);
                Assert::Equal<DWORD>(11, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion1, pVersion2->sczVersion);
                Assert::Equal<DWORD>(10, pVersion2->dwMajor);
                Assert::Equal<DWORD>(20, pVersion2->dwMinor);
                Assert::Equal<DWORD>(30, pVersion2->dwPatch);
                Assert::Equal<DWORD>(40, pVersion2->dwRevision);
                Assert::Equal<DWORD>(0, pVersion2->cReleaseLabels);
                Assert::Equal<DWORD>(11, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion2->fInvalid);

                NativeAssert::StringEqual(wzVersion1, pVersion3->sczVersion);
                Assert::Equal<DWORD>(10, pVersion3->dwMajor);
                Assert::Equal<DWORD>(20, pVersion3->dwMinor);
                Assert::Equal<DWORD>(30, pVersion3->dwPatch);
                Assert::Equal<DWORD>(40, pVersion3->dwRevision);
                Assert::Equal<DWORD>(0, pVersion3->cReleaseLabels);
                Assert::Equal<DWORD>(11, pVersion3->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion3->fInvalid);

                TestVerutilCompareParsedVersions(pVersion1, pVersion2, 0);
                TestVerutilCompareParsedVersions(pVersion1, pVersion3, 0);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
                ReleaseVerutilVersion(pVersion3);
            }
        }

        [Fact]
        void VerCompareVersionsHandlesTooLargeNumbers()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            LPCWSTR wzVersion1 = L"4294967295.4294967295.4294967295.4294967295";
            LPCWSTR wzVersion2 = L"4294967296.4294967296.4294967296.4294967296";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(4294967295, pVersion1->dwMajor);
                Assert::Equal<DWORD>(4294967295, pVersion1->dwMinor);
                Assert::Equal<DWORD>(4294967295, pVersion1->dwPatch);
                Assert::Equal<DWORD>(4294967295, pVersion1->dwRevision);
                Assert::Equal<DWORD>(0, pVersion1->cReleaseLabels);
                Assert::Equal<DWORD>(43, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion2, pVersion2->sczVersion);
                Assert::Equal<DWORD>(0, pVersion2->dwMajor);
                Assert::Equal<DWORD>(0, pVersion2->dwMinor);
                Assert::Equal<DWORD>(0, pVersion2->dwPatch);
                Assert::Equal<DWORD>(0, pVersion2->dwRevision);
                Assert::Equal<DWORD>(0, pVersion2->cReleaseLabels);
                Assert::Equal<DWORD>(0, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion2->fInvalid);

                TestVerutilCompareParsedVersions(pVersion1, pVersion2, 1);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
            }
        }

        [Fact]
        void VerCompareVersionsIgnoresMetadataForValidVersions()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            LPCWSTR wzVersion1 = L"1.2.3+abc";
            LPCWSTR wzVersion2 = L"1.2.3+xyz";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(1, pVersion1->dwMajor);
                Assert::Equal<DWORD>(2, pVersion1->dwMinor);
                Assert::Equal<DWORD>(3, pVersion1->dwPatch);
                Assert::Equal<DWORD>(0, pVersion1->dwRevision);
                Assert::Equal<DWORD>(0, pVersion1->cReleaseLabels);
                Assert::Equal<DWORD>(6, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion2, pVersion2->sczVersion);
                Assert::Equal<DWORD>(1, pVersion2->dwMajor);
                Assert::Equal<DWORD>(2, pVersion2->dwMinor);
                Assert::Equal<DWORD>(3, pVersion2->dwPatch);
                Assert::Equal<DWORD>(0, pVersion2->dwRevision);
                Assert::Equal<DWORD>(0, pVersion2->cReleaseLabels);
                Assert::Equal<DWORD>(6, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion2->fInvalid);

                TestVerutilCompareParsedVersions(pVersion1, pVersion2, 0);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
            }
        }

        [Fact]
        void VerCopyVersionCopiesVersion()
        {
            HRESULT hr = S_OK;
            LPCWSTR wzVersion = L"1.2.3.4-a.b.c.d.5.+abc123";
            VERUTIL_VERSION* pSource = NULL;
            VERUTIL_VERSION* pCopy = NULL;
            int nResult = 0;

            try
            {
                hr = VerParseVersion(wzVersion, 0, FALSE, &pSource);
                NativeAssert::Succeeded(hr, "VerParseVersion failed");

                NativeAssert::StringEqual(wzVersion, pSource->sczVersion);
                Assert::Equal<DWORD>(1, pSource->dwMajor);
                Assert::Equal<DWORD>(2, pSource->dwMinor);
                Assert::Equal<DWORD>(3, pSource->dwPatch);
                Assert::Equal<DWORD>(4, pSource->dwRevision);
                Assert::Equal<DWORD>(5, pSource->cReleaseLabels);

                Assert::Equal<BOOL>(FALSE, pSource->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(1, pSource->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(8, pSource->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<BOOL>(FALSE, pSource->rgReleaseLabels[1].fNumeric);
                Assert::Equal<DWORD>(1, pSource->rgReleaseLabels[1].cchLabel);
                Assert::Equal<DWORD>(10, pSource->rgReleaseLabels[1].cchLabelOffset);

                Assert::Equal<BOOL>(FALSE, pSource->rgReleaseLabels[2].fNumeric);
                Assert::Equal<DWORD>(1, pSource->rgReleaseLabels[2].cchLabel);
                Assert::Equal<DWORD>(12, pSource->rgReleaseLabels[2].cchLabelOffset);

                Assert::Equal<BOOL>(FALSE, pSource->rgReleaseLabels[3].fNumeric);
                Assert::Equal<DWORD>(1, pSource->rgReleaseLabels[3].cchLabel);
                Assert::Equal<DWORD>(14, pSource->rgReleaseLabels[3].cchLabelOffset);

                Assert::Equal<BOOL>(TRUE, pSource->rgReleaseLabels[4].fNumeric);
                Assert::Equal<DWORD>(5, pSource->rgReleaseLabels[4].dwValue);
                Assert::Equal<DWORD>(1, pSource->rgReleaseLabels[4].cchLabel);
                Assert::Equal<DWORD>(16, pSource->rgReleaseLabels[4].cchLabelOffset);

                Assert::Equal<DWORD>(18, pSource->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pSource->fInvalid);

                hr = VerCopyVersion(pSource, &pCopy);
                NativeAssert::Succeeded(hr, "VerCopyVersion failed");

                Assert::False(pSource == pCopy);
                Assert::False(pSource->sczVersion == pCopy->sczVersion);
                Assert::False(pSource->rgReleaseLabels == pCopy->rgReleaseLabels);

                hr = VerCompareParsedVersions(pSource, pCopy, &nResult);
                NativeAssert::Succeeded(hr, "VerCompareParsedVersions failed");

                Assert::Equal<int>(nResult, 0);
            }
            finally
            {
                ReleaseVerutilVersion(pCopy);
                ReleaseVerutilVersion(pSource);
            }
        }

        [Fact]
        void VerParseVersionTreatsTrailingDotsAsInvalid()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;
            VERUTIL_VERSION* pVersion2 = NULL;
            VERUTIL_VERSION* pVersion3 = NULL;
            VERUTIL_VERSION* pVersion4 = NULL;
            VERUTIL_VERSION* pVersion5 = NULL;
            VERUTIL_VERSION* pVersion6 = NULL;
            VERUTIL_VERSION* pVersion7 = NULL;
            LPCWSTR wzVersion1 = L".";
            LPCWSTR wzVersion2 = L"1.";
            LPCWSTR wzVersion3 = L"2.1.";
            LPCWSTR wzVersion4 = L"3.2.1.";
            LPCWSTR wzVersion5 = L"4.3.2.1.";
            LPCWSTR wzVersion6 = L"5-.";
            LPCWSTR wzVersion7 = L"6-a.";

            try
            {
                hr = VerParseVersion(wzVersion1, 0, FALSE, &pVersion1);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion1);

                hr = VerParseVersion(wzVersion2, 0, FALSE, &pVersion2);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion2);

                hr = VerParseVersion(wzVersion3, 0, FALSE, &pVersion3);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion3);

                hr = VerParseVersion(wzVersion4, 0, FALSE, &pVersion4);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion4);

                hr = VerParseVersion(wzVersion5, 0, FALSE, &pVersion5);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion5);

                hr = VerParseVersion(wzVersion6, 0, FALSE, &pVersion6);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion6);

                hr = VerParseVersion(wzVersion7, 0, FALSE, &pVersion7);
                NativeAssert::Succeeded(hr, "Failed to parse version '{0}'", wzVersion7);

                NativeAssert::StringEqual(wzVersion1, pVersion1->sczVersion);
                Assert::Equal<DWORD>(0, pVersion1->dwMajor);
                Assert::Equal<DWORD>(0, pVersion1->dwMinor);
                Assert::Equal<DWORD>(0, pVersion1->dwPatch);
                Assert::Equal<DWORD>(0, pVersion1->dwRevision);
                Assert::Equal<DWORD>(0, pVersion1->cReleaseLabels);
                Assert::Equal<DWORD>(0, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion1->fInvalid);

                NativeAssert::StringEqual(wzVersion2, pVersion2->sczVersion);
                Assert::Equal<DWORD>(1, pVersion2->dwMajor);
                Assert::Equal<DWORD>(0, pVersion2->dwMinor);
                Assert::Equal<DWORD>(0, pVersion2->dwPatch);
                Assert::Equal<DWORD>(0, pVersion2->dwRevision);
                Assert::Equal<DWORD>(0, pVersion2->cReleaseLabels);
                Assert::Equal<DWORD>(2, pVersion2->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion2->fInvalid);

                NativeAssert::StringEqual(wzVersion3, pVersion3->sczVersion);
                Assert::Equal<DWORD>(2, pVersion3->dwMajor);
                Assert::Equal<DWORD>(1, pVersion3->dwMinor);
                Assert::Equal<DWORD>(0, pVersion3->dwPatch);
                Assert::Equal<DWORD>(0, pVersion3->dwRevision);
                Assert::Equal<DWORD>(0, pVersion3->cReleaseLabels);
                Assert::Equal<DWORD>(4, pVersion3->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion3->fInvalid);

                NativeAssert::StringEqual(wzVersion4, pVersion4->sczVersion);
                Assert::Equal<DWORD>(3, pVersion4->dwMajor);
                Assert::Equal<DWORD>(2, pVersion4->dwMinor);
                Assert::Equal<DWORD>(1, pVersion4->dwPatch);
                Assert::Equal<DWORD>(0, pVersion4->dwRevision);
                Assert::Equal<DWORD>(0, pVersion4->cReleaseLabels);
                Assert::Equal<DWORD>(6, pVersion4->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion4->fInvalid);

                NativeAssert::StringEqual(wzVersion5, pVersion5->sczVersion);
                Assert::Equal<DWORD>(4, pVersion5->dwMajor);
                Assert::Equal<DWORD>(3, pVersion5->dwMinor);
                Assert::Equal<DWORD>(2, pVersion5->dwPatch);
                Assert::Equal<DWORD>(1, pVersion5->dwRevision);
                Assert::Equal<DWORD>(0, pVersion5->cReleaseLabels);
                Assert::Equal<DWORD>(8, pVersion5->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion5->fInvalid);

                NativeAssert::StringEqual(wzVersion6, pVersion6->sczVersion);
                Assert::Equal<DWORD>(5, pVersion6->dwMajor);
                Assert::Equal<DWORD>(0, pVersion6->dwMinor);
                Assert::Equal<DWORD>(0, pVersion6->dwPatch);
                Assert::Equal<DWORD>(0, pVersion6->dwRevision);
                Assert::Equal<DWORD>(0, pVersion6->cReleaseLabels);
                Assert::Equal<DWORD>(2, pVersion6->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion6->fInvalid);

                NativeAssert::StringEqual(wzVersion7, pVersion7->sczVersion);
                Assert::Equal<DWORD>(6, pVersion7->dwMajor);
                Assert::Equal<DWORD>(0, pVersion7->dwMinor);
                Assert::Equal<DWORD>(0, pVersion7->dwPatch);
                Assert::Equal<DWORD>(0, pVersion7->dwRevision);
                Assert::Equal<DWORD>(1, pVersion7->cReleaseLabels);

                Assert::Equal<BOOL>(FALSE, pVersion7->rgReleaseLabels[0].fNumeric);
                Assert::Equal<DWORD>(1, pVersion7->rgReleaseLabels[0].cchLabel);
                Assert::Equal<DWORD>(2, pVersion7->rgReleaseLabels[0].cchLabelOffset);

                Assert::Equal<DWORD>(4, pVersion7->cchMetadataOffset);
                Assert::Equal<BOOL>(TRUE, pVersion7->fInvalid);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
                ReleaseVerutilVersion(pVersion2);
                ReleaseVerutilVersion(pVersion3);
                ReleaseVerutilVersion(pVersion4);
                ReleaseVerutilVersion(pVersion5);
                ReleaseVerutilVersion(pVersion6);
                ReleaseVerutilVersion(pVersion7);
            }
        }

        [Fact]
        void VerVersionFromQwordCreatesVersion()
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion1 = NULL;

            try
            {
                hr = VerVersionFromQword(MAKEQWORDVERSION(1, 2, 3, 4), &pVersion1);
                NativeAssert::Succeeded(hr, "VerVersionFromQword failed");

                NativeAssert::StringEqual(L"1.2.3.4", pVersion1->sczVersion);
                Assert::Equal<DWORD>(1, pVersion1->dwMajor);
                Assert::Equal<DWORD>(2, pVersion1->dwMinor);
                Assert::Equal<DWORD>(3, pVersion1->dwPatch);
                Assert::Equal<DWORD>(4, pVersion1->dwRevision);
                Assert::Equal<DWORD>(0, pVersion1->cReleaseLabels);
                Assert::Equal<DWORD>(7, pVersion1->cchMetadataOffset);
                Assert::Equal<BOOL>(FALSE, pVersion1->fInvalid);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion1);
            }
        }

    private:
        void TestVerutilCompareParsedVersions(VERUTIL_VERSION* pVersion1, VERUTIL_VERSION* pVersion2, int nExpectedResult)
        {
            HRESULT hr = S_OK;
            int nResult = 0;

            hr = VerCompareParsedVersions(pVersion1, pVersion2, &nResult);
            NativeAssert::Succeeded(hr, "Failed to compare versions '{0}' and '{1}'", pVersion1->sczVersion, pVersion2->sczVersion);

            Assert::Equal(nExpectedResult, nResult);

            hr = VerCompareParsedVersions(pVersion2, pVersion1, &nResult);
            NativeAssert::Succeeded(hr, "Failed to compare versions '{0}' and '{1}'", pVersion1->sczVersion, pVersion2->sczVersion);

            Assert::Equal(nExpectedResult, -nResult);
        }
    };
}
