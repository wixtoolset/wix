// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixTest;

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

    private:
        void TestTrim(LPCWSTR wzInput, LPCWSTR wzExpectedResult)
        {
            HRESULT hr = S_OK;
            LPWSTR sczOutput = NULL;

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
            return;
        }

        void TestTrimAnsi(LPCSTR szInput, LPCSTR szExpectedResult)
        {
            HRESULT hr = S_OK;
            LPSTR sczOutput = NULL;

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
            return;
        }

        void TestStrAllocStringAnsi(LPCSTR szSource, DWORD cchSource, LPCWSTR wzExpectedResult)
        {
            HRESULT hr = S_OK;
            LPWSTR sczOutput = NULL;

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
            return;
        }

        void TestStrAnsiAllocString(LPWSTR wzSource, DWORD cchSource, LPCSTR szExpectedResult)
        {
            HRESULT hr = S_OK;
            LPSTR sczOutput = NULL;

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
            return;
        }
    };
}
