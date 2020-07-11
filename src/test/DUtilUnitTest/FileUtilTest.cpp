// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixTest;

namespace DutilTests
{
    public ref class FileUtil
    {
    public:
        [Fact(Skip="Skipped until we have a good way to reference ANSI.txt.")]
        void FileUtilTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczTempDir = NULL;
            LPWSTR sczFileDir = NULL;

            try
            {
                hr = PathExpand(&sczTempDir, L"%TEMP%\\FileUtilTest\\", PATH_EXPAND_ENVIRONMENT);
                NativeAssert::Succeeded(hr, "Failed to get temp dir");

                hr = PathExpand(&sczFileDir, L"%WIX_ROOT%\\examples\\data\\TextEncodings\\", PATH_EXPAND_ENVIRONMENT);
                NativeAssert::Succeeded(hr, "Failed to get path to encodings file dir");

                hr = DirEnsureExists(sczTempDir, NULL);
                NativeAssert::Succeeded(hr, "Failed to ensure directory exists: {0}", sczTempDir);

                TestFile(sczFileDir, sczTempDir, L"ANSI.txt", 32, FILE_ENCODING_UTF8);
                // Big endian not supported today!
                //TestFile(sczFileDir, L"UnicodeBENoBOM.txt", 34);
                //TestFile(sczFileDir, L"UnicodeBEWithBOM.txt", 34);
                TestFile(sczFileDir, sczTempDir, L"UnicodeLENoBOM.txt", 34, FILE_ENCODING_UTF16);
                TestFile(sczFileDir, sczTempDir, L"UnicodeLEWithBOM.txt", 34, FILE_ENCODING_UTF16_WITH_BOM);
                TestFile(sczFileDir, sczTempDir, L"UTF8WithSignature.txt", 34, FILE_ENCODING_UTF8_WITH_BOM);

                hr = DirEnsureDelete(sczTempDir, TRUE, TRUE);
            }
            finally
            {
                ReleaseStr(sczTempDir);
                ReleaseStr(sczFileDir);
            }
        }

    private:
        void TestFile(LPWSTR wzDir, LPCWSTR wzTempDir, LPWSTR wzFileName, DWORD dwExpectedStringLength, FILE_ENCODING feExpectedEncoding)
        {
            HRESULT hr = S_OK;
            LPWSTR sczFullPath = NULL;
            LPWSTR sczContents = NULL;
            LPWSTR sczOutputPath = NULL;
            FILE_ENCODING feEncodingFound = FILE_ENCODING_UNSPECIFIED;
            BYTE *pbFile1 = NULL;
            DWORD cbFile1 = 0;
            BYTE *pbFile2 = NULL;
            DWORD cbFile2 = 0;

            try
            {
                hr = PathConcat(wzDir, wzFileName, &sczFullPath);
                NativeAssert::Succeeded(hr, "Failed to create path to test file: {0}", sczFullPath);

                hr = FileToString(sczFullPath, &sczContents, &feEncodingFound);
                hr = E_FAIL;
                NativeAssert::Succeeded(hr, "Failed to read text from file: {0}", sczFullPath);

                if (!sczContents)
                {
                    hr = E_FAIL;
                    NativeAssert::Succeeded(hr, "FileToString() returned NULL for file: {0}", sczFullPath);
                }

                if ((DWORD)lstrlenW(sczContents) != dwExpectedStringLength)
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "FileToString() returned wrong size for file: %ls (expected size %u, found size %u)", sczFullPath, dwExpectedStringLength, lstrlenW(sczContents));
                }

                if (feEncodingFound != feExpectedEncoding)
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "FileToString() returned unexpected encoding type for file: %ls (expected type %u, found type %u)", sczFullPath, feExpectedEncoding, feEncodingFound);
                }

                hr = PathConcat(wzTempDir, wzFileName, &sczOutputPath);
                NativeAssert::Succeeded(hr, "Failed to get output path");

                hr = FileFromString(sczOutputPath, 0, sczContents, feExpectedEncoding);
                NativeAssert::Succeeded(hr, "Failed to write contents of file back out to disk");

                hr = FileRead(&pbFile1, &cbFile1, sczFullPath);
                NativeAssert::Succeeded(hr, "Failed to read input file as binary");

                hr = FileRead(&pbFile2, &cbFile2, sczOutputPath);
                NativeAssert::Succeeded(hr, "Failed to read output file as binary");

                if (cbFile1 != cbFile2 || 0 != memcmp(pbFile1, pbFile2, cbFile1))
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "Outputted file doesn't match input file: \"%ls\" and \"%ls\"", sczFullPath, sczOutputPath);
                }
            }
            finally
            {
                ReleaseStr(sczOutputPath);
                ReleaseStr(sczFullPath);
                ReleaseStr(sczContents);
            }

        LExit:
            return;
        }
    };
}
