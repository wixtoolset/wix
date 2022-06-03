// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;

LPCWSTR wzBaseRegKey = L"Software\\RegUtilTest\\";
LPWSTR rgwzMultiValue[2] = { L"First", L"Second" };
LPWSTR rgwzEmptyMultiValue[2] = { L"", L"" };

HKEY hkBase;

namespace DutilTests
{
    public ref class RegUtil : IDisposable
    {
    private:

        void CreateBaseKey()
        {
            HRESULT hr = RegCreate(HKEY_CURRENT_USER, wzBaseRegKey, KEY_ALL_ACCESS, &hkBase);
            NativeAssert::Succeeded(hr, "Failed to create base key.");
        }

    public:
        RegUtil()
        {
            HRESULT hr = RegInitialize();
            NativeAssert::Succeeded(hr, "RegInitialize failed.");
        }

        ~RegUtil()
        {
            RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

            if (hkBase)
            {
                RegDelete(hkBase, NULL, REG_KEY_DEFAULT, TRUE);
            }

            ReleaseRegKey(hkBase);

            RegUninitialize();
        }

        [Fact]
        void RegUtilStringValueTest()
        {
            this->StringValueTest();
        }

        [Fact]
        void RegUtilStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->StringValueTest();
        }

        void StringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            LPCWSTR wzValue = L"Value";

            try
            {
                this->CreateBaseKey();

                hr = RegWriteString(hkBase, L"String", wzValue);
                NativeAssert::Succeeded(hr, "Failed to write string value.");

                hr = RegReadString(hkBase, L"String", &sczValue);
                NativeAssert::Succeeded(hr, "Failed to read string value.");
                NativeAssert::StringEqual(wzValue, sczValue);

                ReleaseNullStr(sczValue);
                hr = StrAllocString(&sczValue, L"e", 0);
                NativeAssert::Succeeded(hr, "Failed to reallocate string value.");

                hr = RegReadString(hkBase, L"String", &sczValue);
                NativeAssert::Succeeded(hr, "Failed to read string value.");
                NativeAssert::StringEqual(wzValue, sczValue);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        [Fact]
        void RegUtilPartialStringValueTest()
        {
            this->PartialStringValueTest();
        }

        [Fact]
        void RegUtilPartialStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->PartialStringValueTest();
        }

        void PartialStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            LPCWSTR wzValue = L"Value";
            BOOL fNeedsExpansion = FALSE;

            try
            {
                this->CreateBaseKey();

                // Use API directly to write non-null terminated string.
                hr = HRESULT_FROM_WIN32(::RegSetValueExW(hkBase, L"PartialString", 0, REG_SZ, reinterpret_cast<const BYTE*>(wzValue), 4 * sizeof(WCHAR)));
                NativeAssert::Succeeded(hr, "Failed to write partial string value.");

                hr = RegReadString(hkBase, L"PartialString", &sczValue);
                NativeAssert::Succeeded(hr, "Failed to read partial string value.");
                NativeAssert::StringEqual(L"Valu", sczValue);

                hr = RegReadUnexpandedString(hkBase, L"PartialString", &fNeedsExpansion, &sczValue);
                NativeAssert::Succeeded(hr, "Failed to read partial unexpanded string value.");
                NativeAssert::StringEqual(L"Valu", sczValue);
                Assert::False(fNeedsExpansion);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        [Fact]
        void RegUtilEmptyStringValueTest()
        {
            this->EmptyStringValueTest();
        }

        [Fact]
        void RegUtilEmptyStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->EmptyStringValueTest();
        }

        void EmptyStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;

            try
            {
                this->CreateBaseKey();

                // Use API directly to write non-null terminated string.
                hr = HRESULT_FROM_WIN32(::RegSetValueExW(hkBase, L"EmptyString", 0, REG_SZ, reinterpret_cast<const BYTE*>(L""), 0));
                NativeAssert::Succeeded(hr, "Failed to write partial string value.");

                hr = RegReadString(hkBase, L"EmptyString", &sczValue);
                NativeAssert::Succeeded(hr, "Failed to read partial string value.");
                NativeAssert::StringEqual(L"", sczValue);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        [Fact]
        void RegUtilExpandStringValueTest()
        {
            this->ExpandStringValueTest();
        }

        [Fact]
        void RegUtilExpandStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->ExpandStringValueTest();
        }

        void ExpandStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            LPCWSTR wzValue = L"Value_%USERNAME%";
            String^ expandedValue = Environment::ExpandEnvironmentVariables(gcnew String(wzValue));

            try
            {
                this->CreateBaseKey();

                hr = RegWriteExpandString(hkBase, L"ExpandString", wzValue);
                NativeAssert::Succeeded(hr, "Failed to write expand string value.");

                hr = RegReadString(hkBase, L"ExpandString", &sczValue);
                NativeAssert::Succeeded(hr, "Failed to read expand string value.");
                WixAssert::StringEqual(expandedValue, gcnew String(sczValue), false);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        [Fact]
        void RegUtilExpandLongStringValueTest()
        {
            this->ExpandLongStringValueTest();
        }

        [Fact]
        void RegUtilExpandLongStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->ExpandLongStringValueTest();
        }

        void ExpandLongStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            LPCWSTR wzValue = L"%TEMP%;%PATH%;C:\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789";
            String^ expandedValue = Environment::ExpandEnvironmentVariables(gcnew String(wzValue));

            try
            {
                this->CreateBaseKey();

                hr = RegWriteExpandString(hkBase, L"ExpandString", wzValue);
                NativeAssert::Succeeded(hr, "Failed to write expand string value.");

                hr = RegReadString(hkBase, L"ExpandString", &sczValue);
                NativeAssert::Succeeded(hr, "Failed to read expand string value.");
                WixAssert::StringEqual(expandedValue, gcnew String(sczValue), false);

                ReleaseNullStr(sczValue);

                hr = RegReadString(hkBase, L"ExpandString", &sczValue);
                NativeAssert::Succeeded(hr, "Failed to read expand string value.");
                WixAssert::StringEqual(expandedValue, gcnew String(sczValue), false);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        [Fact]
        void RegUtilNotExpandStringValueTest()
        {
            this->NotExpandStringValueTest();
        }

        [Fact]
        void RegUtilNotExpandStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->NotExpandStringValueTest();
        }

        void NotExpandStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            BOOL fNeedsExpansion = FALSE;
            LPCWSTR wzValue = L"Value_%USERNAME%";

            try
            {
                this->CreateBaseKey();

                hr = RegWriteExpandString(hkBase, L"NotExpandString", wzValue);
                NativeAssert::Succeeded(hr, "Failed to write expand string value.");

                hr = RegReadUnexpandedString(hkBase, L"NotExpandString", &fNeedsExpansion, &sczValue);
                NativeAssert::Succeeded(hr, "Failed to read expand string value.");
                NativeAssert::StringEqual(wzValue, sczValue);
                Assert::True(fNeedsExpansion);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        [Fact]
        void RegUtilMultiStringValueTest()
        {
            this->MultiStringValueTest();
        }

        [Fact]
        void RegUtilMultiStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->MultiStringValueTest();
        }

        void MultiStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR* rgsczStrings = NULL;
            DWORD cStrings = 0;

            try
            {
                this->CreateBaseKey();

                hr = RegWriteStringArray(hkBase, L"MultiString", rgwzMultiValue, 2);
                NativeAssert::Succeeded(hr, "Failed to write multi string value.");

                hr = RegReadStringArray(hkBase, L"MultiString", &rgsczStrings, &cStrings);
                NativeAssert::Succeeded(hr, "Failed to read multi string value.");
                Assert::Equal<DWORD>(2, cStrings);
                NativeAssert::StringEqual(L"First", rgsczStrings[0]);
                NativeAssert::StringEqual(L"Second", rgsczStrings[1]);
            }
            finally
            {
                ReleaseStrArray(rgsczStrings, cStrings);
            }
        }

        [Fact]
        void RegUtilPartialMultiStringValueTest()
        {
            this->PartialMultiStringValueTest();
        }

        [Fact]
        void RegUtilPartialMultiStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->PartialMultiStringValueTest();
        }

        void PartialMultiStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR* rgsczStrings = NULL;
            DWORD cStrings = 0;

            try
            {
                this->CreateBaseKey();

                // Use API directly to write non-double-null terminated string.
                hr = HRESULT_FROM_WIN32(::RegSetValueExW(hkBase, L"PartialMultiString", 0, REG_MULTI_SZ, reinterpret_cast<const BYTE*>(L"First\0Second"), 13 * sizeof(WCHAR)));
                NativeAssert::Succeeded(hr, "Failed to write partial multi string value.");

                hr = RegReadStringArray(hkBase, L"PartialMultiString", &rgsczStrings, &cStrings);
                NativeAssert::Succeeded(hr, "Failed to read partial multi string value.");
                Assert::Equal<DWORD>(2, cStrings);
                NativeAssert::StringEqual(L"First", rgsczStrings[0]);
                NativeAssert::StringEqual(L"Second", rgsczStrings[1]);
            }
            finally
            {
                ReleaseStrArray(rgsczStrings, cStrings);
            }
        }

        [Fact]
        void RegUtilEmptyMultiStringValueTest()
        {
            this->EmptyMultiStringValueTest();
        }

        [Fact]
        void RegUtilEmptyMultiStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->EmptyMultiStringValueTest();
        }

        void EmptyMultiStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR* rgsczStrings = NULL;
            DWORD cStrings = 0;

            try
            {
                this->CreateBaseKey();

                hr = RegWriteStringArray(hkBase, L"EmptyMultiString", rgwzMultiValue, 0);
                NativeAssert::Succeeded(hr, "Failed to write empty multi string value.");

                hr = RegReadStringArray(hkBase, L"EmptyMultiString", &rgsczStrings, &cStrings);
                NativeAssert::Succeeded(hr, "Failed to read empty multi string value.");
                Assert::Equal<DWORD>(0, cStrings);
            }
            finally
            {
                ReleaseStrArray(rgsczStrings, cStrings);
            }
        }

        [Fact]
        void RegUtilOneEmptyMultiStringValueTest()
        {
            this->OneEmptyMultiStringValueTest();
        }

        [Fact]
        void RegUtilOneEmptyMultiStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->OneEmptyMultiStringValueTest();
        }

        void OneEmptyMultiStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR* rgsczStrings = NULL;
            DWORD cStrings = 0;

            try
            {
                this->CreateBaseKey();

                hr = RegWriteStringArray(hkBase, L"OneEmptyMultiString", rgwzEmptyMultiValue, 1);
                NativeAssert::Succeeded(hr, "Failed to write one empty multi string value.");

                hr = RegReadStringArray(hkBase, L"OneEmptyMultiString", &rgsczStrings, &cStrings);
                NativeAssert::Succeeded(hr, "Failed to read one empty multi string value.");
                Assert::Equal<DWORD>(0, cStrings);
            }
            finally
            {
                ReleaseStrArray(rgsczStrings, cStrings);
            }
        }

        [Fact]
        void RegUtilTwoEmptyMultiStringValueTest()
        {
            this->TwoEmptyMultiStringValueTest();
        }

        [Fact]
        void RegUtilTwoEmptyMultiStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->TwoEmptyMultiStringValueTest();
        }

        void TwoEmptyMultiStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR* rgsczStrings = NULL;
            DWORD cStrings = 0;

            try
            {
                this->CreateBaseKey();

                hr = RegWriteStringArray(hkBase, L"OneEmptyMultiString", rgwzEmptyMultiValue, 2);
                NativeAssert::Succeeded(hr, "Failed to write one empty multi string value.");

                hr = RegReadStringArray(hkBase, L"OneEmptyMultiString", &rgsczStrings, &cStrings);
                NativeAssert::Succeeded(hr, "Failed to read one empty multi string value.");
                Assert::Equal<DWORD>(2, cStrings);
                NativeAssert::StringEqual(L"", rgsczStrings[0]);
                NativeAssert::StringEqual(L"", rgsczStrings[1]);
            }
            finally
            {
                ReleaseStrArray(rgsczStrings, cStrings);
            }
        }

        [Fact]
        void RegUtilOnePartialEmptyMultiStringValueTest()
        {
            this->OnePartialEmptyMultiStringValueTest();
        }

        [Fact]
        void RegUtilOnePartialEmptyMultiStringValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->OnePartialEmptyMultiStringValueTest();
        }

        void OnePartialEmptyMultiStringValueTest()
        {
            HRESULT hr = S_OK;
            LPWSTR* rgsczStrings = NULL;
            DWORD cStrings = 0;

            try
            {
                this->CreateBaseKey();

                // Use API directly to write non-double-null terminated string.
                hr = HRESULT_FROM_WIN32(::RegSetValueExW(hkBase, L"OnePartialEmptyMultiString", 0, REG_MULTI_SZ, reinterpret_cast<const BYTE*>(L""), 1 * sizeof(WCHAR)));
                NativeAssert::Succeeded(hr, "Failed to write partial empty multi string value.");

                hr = RegReadStringArray(hkBase, L"OnePartialEmptyMultiString", &rgsczStrings, &cStrings);
                NativeAssert::Succeeded(hr, "Failed to read partial empty multi string value.");
                Assert::Equal<DWORD>(0, cStrings);
            }
            finally
            {
                ReleaseStrArray(rgsczStrings, cStrings);
            }
        }

        [Fact]
        void RegUtilBinaryValueTest()
        {
            this->BinaryValueTest();
        }

        [Fact]
        void RegUtilBinaryValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->BinaryValueTest();
        }

        void BinaryValueTest()
        {
            HRESULT hr = S_OK;
            BYTE pbSource[4] = { 1, 2, 3, 4 };
            BYTE* pbBuffer = NULL;
            SIZE_T cbBuffer = 0;

            try
            {
                this->CreateBaseKey();

                hr = RegWriteBinary(hkBase, L"Binary", pbSource, 4);
                NativeAssert::Succeeded(hr, "Failed to write binary value.");

                hr = RegReadBinary(hkBase, L"Binary", &pbBuffer, &cbBuffer);
                NativeAssert::Succeeded(hr, "Failed to read binary value.");
                Assert::Equal<DWORD>(4, cbBuffer);
                Assert::Equal<BYTE>(1, pbBuffer[0]);
                Assert::Equal<BYTE>(2, pbBuffer[1]);
                Assert::Equal<BYTE>(3, pbBuffer[2]);
                Assert::Equal<BYTE>(4, pbBuffer[3]);
            }
            finally
            {
                ReleaseMem(pbBuffer);
            }
        }

        [Fact]
        void RegUtilEmptyBinaryValueTest()
        {
            this->EmptyBinaryValueTest();
        }

        [Fact]
        void RegUtilEmptyBinaryValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->EmptyBinaryValueTest();
        }

        void EmptyBinaryValueTest()
        {
            HRESULT hr = S_OK;
            BYTE* pbBuffer = NULL;
            SIZE_T cbBuffer = 0;

            try
            {
                this->CreateBaseKey();

                hr = RegWriteBinary(hkBase, L"Binary", NULL, 0);
                NativeAssert::Succeeded(hr, "Failed to write binary value.");

                hr = RegReadBinary(hkBase, L"Binary", &pbBuffer, &cbBuffer);
                NativeAssert::Succeeded(hr, "Failed to read binary value.");
                Assert::Equal<DWORD>(0, cbBuffer);
            }
            finally
            {
                ReleaseMem(pbBuffer);
            }
        }

        [Fact]
        void RegUtilQwordVersionValueTest()
        {
            this->QwordVersionValueTest();
        }

        [Fact]
        void RegUtilQwordVersionValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->QwordVersionValueTest();
        }

        void QwordVersionValueTest()
        {
            HRESULT hr = S_OK;
            DWORD64 qwVersion = FILEMAKEVERSION(1, 2, 3, 4);
            DWORD64 qwValue = 0;

            this->CreateBaseKey();

            hr = RegWriteQword(hkBase, L"QwordVersion", qwVersion);
            NativeAssert::Succeeded(hr, "Failed to write qword version value.");

            hr = RegReadVersion(hkBase, L"QwordVersion", &qwValue);
            NativeAssert::Succeeded(hr, "Failed to read qword version value.");
            Assert::Equal<DWORD64>(qwVersion, qwValue);
        }

        [Fact]
        void RegUtilStringVersionValueTest()
        {
            this->StringVersionValueTest();
        }

        [Fact]
        void RegUtilStringVersionValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->StringVersionValueTest();
        }

        void StringVersionValueTest()
        {
            HRESULT hr = S_OK;
            LPCWSTR wzVersion = L"65535.65535.65535.65535";
            DWORD64 qwValue = 0;

            this->CreateBaseKey();

            hr = RegWriteString(hkBase, L"StringVersion", wzVersion);
            NativeAssert::Succeeded(hr, "Failed to write string version value.");

            hr = RegReadVersion(hkBase, L"StringVersion", &qwValue);
            NativeAssert::Succeeded(hr, "Failed to read string version value.");
            Assert::Equal<DWORD64>(MAXDWORD64, qwValue);
        }

        [Fact]
        void RegUtilQwordWixVersionValueTest()
        {
            this->QwordWixVersionValueTest();
        }

        [Fact]
        void RegUtilQwordWixVersionValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->QwordWixVersionValueTest();
        }

        void QwordWixVersionValueTest()
        {
            HRESULT hr = S_OK;
            DWORD64 qwVersion = FILEMAKEVERSION(1, 2, 3, 4);
            VERUTIL_VERSION* pVersion = NULL;

            try
            {
                this->CreateBaseKey();

                hr = RegWriteQword(hkBase, L"QwordWixVersion", qwVersion);
                NativeAssert::Succeeded(hr, "Failed to write qword wix version value.");

                hr = RegReadWixVersion(hkBase, L"QwordWixVersion", &pVersion);
                NativeAssert::Succeeded(hr, "Failed to read qword wix version value.");
                NativeAssert::StringEqual(L"1.2.3.4", pVersion->sczVersion);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion);
            }
        }

        [Fact]
        void RegUtilStringWixVersionValueTest()
        {
            this->StringWixVersionValueTest();
        }

        [Fact]
        void RegUtilStringWixVersionValueFallbackTest()
        {
            RegFunctionForceFallback();
            this->StringWixVersionValueTest();
        }

        void StringWixVersionValueTest()
        {
            HRESULT hr = S_OK;
            LPCWSTR wzVersion = L"65535.65535.65535.65535-abc+def";
            VERUTIL_VERSION* pVersion = NULL;

            try
            {
                this->CreateBaseKey();

                hr = RegWriteString(hkBase, L"StringWixVersion", wzVersion);
                NativeAssert::Succeeded(hr, "Failed to write string wix version value.");

                hr = RegReadWixVersion(hkBase, L"StringWixVersion", &pVersion);
                NativeAssert::Succeeded(hr, "Failed to read string wix version value.");
                NativeAssert::StringEqual(wzVersion, pVersion->sczVersion);
            }
            finally
            {
                ReleaseVerutilVersion(pVersion);
            }
        }
    };
}
