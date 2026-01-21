// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixInternal::TestSupport;

namespace DutilTests
{
    public ref class StrUtil
    {
    public:
        [Fact]
        void StrUtilFormattedTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczText = NULL;

            try
            {
                hr = StrAllocFormatted(&sczText, L"%hs - %ls - %u", "ansi string", L"unicode string", 1234);
                NativeAssert::Succeeded(hr, "Failed to format string.");
                NativeAssert::StringEqual(L"ansi string - unicode string - 1234", sczText);

                ReleaseNullStr(sczText);

                hr = StrAllocString(&sczText, L"repeat", 0);
                NativeAssert::Succeeded(hr, "Failed to allocate string.");

                hr = StrAllocFormatted(&sczText, L"%ls and %ls", sczText, sczText);
                NativeAssert::Succeeded(hr, "Failed to format string unto itself.");
                NativeAssert::StringEqual(L"repeat and repeat", sczText);
            }
            finally
            {
                ReleaseStr(sczText);
            }
        }

        [Fact]
        void StrUtilTrimTest()
        {
            TestTrim(L"", L"");
            TestTrim(L"Blah", L"Blah");
            TestTrim(L"\t\t\tBlah", L"Blah");
            TestTrim(L"\t    Blah     ", L"Blah");
            TestTrim(L"Blah     ", L"Blah");
            TestTrim(L"\t  Spaces  \t   Between   \t", L"Spaces  \t   Between");
            TestTrim(L"    \t\t\t    ", L"");

            TestTrimAnsi("", "");
            TestTrimAnsi("Blah", "Blah");
            TestTrimAnsi("\t\t\tBlah", "Blah");
            TestTrimAnsi("    Blah     ", "Blah");
            TestTrimAnsi("Blah     ", "Blah");
            TestTrimAnsi("\t  Spaces  \t   Between   \t", "Spaces  \t   Between");
            TestTrimAnsi("    \t\t\t    ", "");
        }

        [Fact]
        void StrUtilConvertTest()
        {
            char a[] = { 'a', 'b', 'C', 'd', '\0', '\0' };

            TestStrAllocStringAnsi(a, 5, L"abCd");
            TestStrAllocStringAnsi(a, 4, L"abCd");
            TestStrAllocStringAnsi(a, 3, L"abC");
            TestStrAllocStringAnsi(a, 2, L"ab");
            TestStrAllocStringAnsi(a, 1, L"a");
            TestStrAllocStringAnsi(a, 0, L"abCd");

            wchar_t b[] = { L'a', L'b', L'C', L'd', L'\0', L'\0' };

            TestStrAnsiAllocString(b, 5, "abCd");
            TestStrAnsiAllocString(b, 4, "abCd");
            TestStrAnsiAllocString(b, 3, "abC");
            TestStrAnsiAllocString(b, 2, "ab");
            TestStrAnsiAllocString(b, 1, "a");
            TestStrAnsiAllocString(b, 0, "abCd");
        }

        [Fact]
        void StrUtilMultiSzLenNullTest()
        {
            HRESULT hr = S_OK;
            SIZE_T cchMultiSz = 7;

            DutilInitialize(&DutilTestTraceError);

            try
            {
                hr = MultiSzLen(NULL, &cchMultiSz);
                NativeAssert::Succeeded(hr, "Failed to get MULTISZ length for null.");
                NativeAssert::Equal<SIZE_T>(0, cchMultiSz);
            }
            finally
            {
                DutilUninitialize();
            }
        }

        [Fact]
        void StrUtilMultiSzPrependEmptyTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczMultiSz = NULL;
            SIZE_T cchMultiSz = 0;
            const WCHAR expected[] = { L'f', L'i', L'r', L's', L't', L'\0', L'\0' };

            DutilInitialize(&DutilTestTraceError);

            try
            {
                hr = MultiSzPrepend(&sczMultiSz, &cchMultiSz, L"first");
                NativeAssert::Succeeded(hr, "Failed to prepend into empty MULTISZ.");
                VerifyMultiSz(sczMultiSz, expected, sizeof(expected) / sizeof(expected[0]));
                NativeAssert::Equal<SIZE_T>(sizeof(expected) / sizeof(expected[0]), cchMultiSz);
            }
            finally
            {
                ReleaseNullStr(sczMultiSz);
                DutilUninitialize();
            }
        }

        [Fact]
        void StrUtilMultiSzInsertEmptyTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczMultiSz = NULL;
            SIZE_T cchMultiSz = 0;
            const WCHAR expected[] = { L'i', L'n', L's', L'e', L'r', L't', L'\0', L'\0' };

            DutilInitialize(&DutilTestTraceError);

            try
            {
                hr = MultiSzInsertString(&sczMultiSz, &cchMultiSz, 0, L"insert");
                NativeAssert::Succeeded(hr, "Failed to insert into empty MULTISZ.");
                VerifyMultiSz(sczMultiSz, expected, sizeof(expected) / sizeof(expected[0]));
                NativeAssert::Equal<SIZE_T>(sizeof(expected) / sizeof(expected[0]), cchMultiSz);
            }
            finally
            {
                ReleaseNullStr(sczMultiSz);
                DutilUninitialize();
            }
        }

        [Fact]
        void StrUtilMultiSzFindTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczMultiSz = NULL;
            DWORD_PTR dwIndex = 0;
            LPCWSTR wzFound = NULL;
            const WCHAR source[] = { L'o', L'n', L'e', L'\0', L't', L'w', L'o', L'\0', L't', L'h', L'r', L'e', L'e', L'\0', L'\0' };

            DutilInitialize(&DutilTestTraceError);

            try
            {
                CreateMultiSz(&sczMultiSz, source, sizeof(source) / sizeof(source[0]));

                hr = MultiSzFindString(sczMultiSz, L"two", &dwIndex, &wzFound);
                NativeAssert::Succeeded(hr, "Failed to find string in MULTISZ.");
                NativeAssert::Equal<DWORD_PTR>(1, dwIndex);
                NativeAssert::StringEqual(L"two", wzFound);

                hr = MultiSzFindSubstring(sczMultiSz, L"re", &dwIndex, &wzFound);
                NativeAssert::Succeeded(hr, "Failed to find substring in MULTISZ.");
                NativeAssert::Equal<DWORD_PTR>(2, dwIndex);
                NativeAssert::StringEqual(L"three", wzFound);

                hr = MultiSzFindString(sczMultiSz, L"missing", &dwIndex, &wzFound);
                NativeAssert::SpecificReturnCode(S_FALSE, hr, "Expected not found for missing string.");

                hr = MultiSzFindSubstring(sczMultiSz, L"zzz", &dwIndex, &wzFound);
                NativeAssert::SpecificReturnCode(S_FALSE, hr, "Expected not found for missing substring.");
            }
            finally
            {
                ReleaseNullStr(sczMultiSz);
                DutilUninitialize();
            }
        }

        [Fact]
        void StrUtilMultiSzRemoveTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczMultiSz = NULL;
            const WCHAR source[] = { L'o', L'n', L'e', L'\0', L't', L'w', L'o', L'\0', L't', L'h', L'r', L'e', L'e', L'\0', L'\0' };
            const WCHAR expected[] = { L'o', L'n', L'e', L'\0', L't', L'h', L'r', L'e', L'e', L'\0', L'\0' };

            DutilInitialize(&DutilTestTraceError);

            try
            {
                CreateMultiSz(&sczMultiSz, source, sizeof(source) / sizeof(source[0]));

                hr = MultiSzRemoveString(&sczMultiSz, 1);
                NativeAssert::Succeeded(hr, "Failed to remove string from MULTISZ.");
                VerifyMultiSz(sczMultiSz, expected, sizeof(expected) / sizeof(expected[0]));

                hr = MultiSzRemoveString(&sczMultiSz, 10);
                NativeAssert::SpecificReturnCode(S_FALSE, hr, "Expected S_FALSE when removing out of range.");
            }
            finally
            {
                ReleaseNullStr(sczMultiSz);
                DutilUninitialize();
            }
        }

        [Fact]
        void StrUtilMultiSzInsertTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczMultiSz = NULL;
            SIZE_T cchMultiSz = 0;
            const WCHAR source[] = { L'o', L'n', L'e', L'\0', L't', L'w', L'o', L'\0', L'\0' };
            const WCHAR expected[] = { L'o', L'n', L'e', L'\0', L't', L'h', L'r', L'e', L'e', L'\0', L't', L'w', L'o', L'\0', L'\0' };

            DutilInitialize(&DutilTestTraceError);

            try
            {
                CreateMultiSz(&sczMultiSz, source, sizeof(source) / sizeof(source[0]));
                cchMultiSz = sizeof(source) / sizeof(source[0]);

                hr = MultiSzInsertString(&sczMultiSz, &cchMultiSz, 1, L"three");
                NativeAssert::Succeeded(hr, "Failed to insert string into MULTISZ.");
                VerifyMultiSz(sczMultiSz, expected, sizeof(expected) / sizeof(expected[0]));
                NativeAssert::Equal<SIZE_T>(sizeof(expected) / sizeof(expected[0]), cchMultiSz);

                hr = MultiSzInsertString(&sczMultiSz, &cchMultiSz, 10, L"bad");
                NativeAssert::SpecificReturnCode(HRESULT_FROM_WIN32(ERROR_OBJECT_NOT_FOUND), hr, "Expected insert to fail for invalid index.");
            }
            finally
            {
                ReleaseNullStr(sczMultiSz);
                DutilUninitialize();
            }
        }

        [Fact]
        void StrUtilMultiSzReplaceTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczMultiSz = NULL;
            const WCHAR source[] = { L'o', L'n', L'e', L'\0', L't', L'w', L'o', L'\0', L't', L'h', L'r', L'e', L'e', L'\0', L'\0' };
            const WCHAR expected[] = { L'o', L'n', L'e', L'\0', L't', L'w', L'o', L'\0', L'f', L'o', L'u', L'r', L'\0', L'\0' };

            DutilInitialize(&DutilTestTraceError);

            try
            {
                CreateMultiSz(&sczMultiSz, source, sizeof(source) / sizeof(source[0]));

                hr = MultiSzReplaceString(&sczMultiSz, 2, L"four");
                NativeAssert::Succeeded(hr, "Failed to replace string in MULTISZ.");
                VerifyMultiSz(sczMultiSz, expected, sizeof(expected) / sizeof(expected[0]));
            }
            finally
            {
                ReleaseNullStr(sczMultiSz);
                DutilUninitialize();
            }
        }

    private:
        void CreateMultiSz(LPWSTR* ppwzMultiSz, const WCHAR* pwzSource, SIZE_T cchSource)
        {
            HRESULT hr = S_OK;

            hr = StrAlloc(ppwzMultiSz, cchSource);
            NativeAssert::Succeeded(hr, "Failed to allocate MULTISZ.");
            ::CopyMemory(*ppwzMultiSz, pwzSource, cchSource * sizeof(WCHAR));
        }

        void VerifyMultiSz(LPCWSTR wzMultiSz, const WCHAR* pwzExpected, SIZE_T cchExpected)
        {
            HRESULT hr = S_OK;
            SIZE_T cchMultiSz = 0;

            hr = MultiSzLen(wzMultiSz, &cchMultiSz);
            NativeAssert::Succeeded(hr, "Failed to get MULTISZ length.");
            NativeAssert::Equal<SIZE_T>(cchExpected, cchMultiSz);
            NativeAssert::True(0 == ::memcmp(wzMultiSz, pwzExpected, cchExpected * sizeof(WCHAR)));
        }

        void TestTrim(LPCWSTR wzInput, LPCWSTR wzExpectedResult)
        {
            HRESULT hr = S_OK;
            LPWSTR sczOutput = NULL;

            DutilInitialize(&DutilTestTraceError);

            try
            {
                hr = StrTrimWhitespace(&sczOutput, wzInput);
                NativeAssert::Succeeded(hr, "Failed to trim whitespace from string: {0}", wzInput);

                if (0 != wcscmp(wzExpectedResult, sczOutput))
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "Trimmed string \"%ls\", expected result \"%ls\", actual result \"%ls\"", wzInput, wzExpectedResult, sczOutput);
                }
            }
            finally
            {
                ReleaseStr(sczOutput);
            }

        LExit:
            DutilUninitialize();
        }

        void TestTrimAnsi(LPCSTR szInput, LPCSTR szExpectedResult)
        {
            HRESULT hr = S_OK;
            LPSTR sczOutput = NULL;

            DutilInitialize(&DutilTestTraceError);

            try
            {
                hr = StrAnsiTrimWhitespace(&sczOutput, szInput);
                NativeAssert::Succeeded(hr, "Failed to trim whitespace from string: \"{0}\"", szInput);

                if (0 != strcmp(szExpectedResult, sczOutput))
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "Trimmed string \"%hs\", expected result \"%hs\", actual result \"%hs\"", szInput, szExpectedResult, sczOutput);
                }
            }
            finally
            {
                ReleaseStr(sczOutput);
            }

        LExit:
            DutilUninitialize();
        }

        void TestStrAllocStringAnsi(LPCSTR szSource, DWORD cchSource, LPCWSTR wzExpectedResult)
        {
            HRESULT hr = S_OK;
            LPWSTR sczOutput = NULL;

            DutilInitialize(&DutilTestTraceError);

            try
            {
                hr = StrAllocStringAnsi(&sczOutput, szSource, cchSource, CP_UTF8);
                NativeAssert::Succeeded(hr, "Failed to call StrAllocStringAnsi on string: \"{0}\"", szSource);

                if (0 != wcscmp(sczOutput, wzExpectedResult))
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "String doesn't match, expected result \"%ls\", actual result \"%ls\"", wzExpectedResult, sczOutput);
                }
            }
            finally
            {
                ReleaseStr(sczOutput);
            }

        LExit:
            DutilUninitialize();
        }

        void TestStrAnsiAllocString(LPWSTR wzSource, DWORD cchSource, LPCSTR szExpectedResult)
        {
            HRESULT hr = S_OK;
            LPSTR sczOutput = NULL;

            DutilInitialize(&DutilTestTraceError);

            try
            {
                hr = StrAnsiAllocString(&sczOutput, wzSource, cchSource, CP_UTF8);
                NativeAssert::Succeeded(hr, "Failed to call StrAllocStringAnsi on string: \"{0}\"", wzSource);

                if (0 != strcmp(sczOutput, szExpectedResult))
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "String doesn't match, expected result \"%hs\", actual result \"%hs\"", szExpectedResult, sczOutput);
                }
            }
            finally
            {
                ReleaseStr(sczOutput);
            }

        LExit:
            DutilUninitialize();
        }
    };
}
