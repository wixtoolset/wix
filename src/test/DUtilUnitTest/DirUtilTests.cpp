// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixTest;

namespace DutilTests
{
    public ref class DirUtil
    {
    public:
        [Fact]
        void DirUtilTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczCurrentDir = NULL;
            LPWSTR sczGuid = NULL;
            LPWSTR sczFolder = NULL;
            LPWSTR sczSubFolder = NULL;

            try
            {
                hr = GuidCreate(&sczGuid);
                NativeAssert::Succeeded(hr, "Failed to create guid.");

                hr = DirGetCurrent(&sczCurrentDir);
                NativeAssert::Succeeded(hr, "Failed to get current directory.");

                hr = PathConcat(sczCurrentDir, sczGuid, &sczFolder);
                NativeAssert::Succeeded(hr, "Failed to combine current directory: '{0}' with Guid: '{1}'", sczCurrentDir, sczGuid);

                BOOL fExists = DirExists(sczFolder, NULL);
                Assert::False(fExists);

                hr = PathConcat(sczFolder, L"foo", &sczSubFolder);
                NativeAssert::Succeeded(hr, "Failed to combine folder: '%ls' with subfolder: 'foo'", sczFolder);

                hr = DirEnsureExists(sczSubFolder, NULL);
                NativeAssert::Succeeded(hr, "Failed to create multiple directories: %ls", sczSubFolder);

                // Test failure to delete non-empty folder.
                hr = DirEnsureDelete(sczFolder, FALSE, FALSE);
                Assert::Equal(HRESULT_FROM_WIN32(ERROR_DIR_NOT_EMPTY), hr);

                hr = DirEnsureDelete(sczSubFolder, FALSE, FALSE);
                NativeAssert::Succeeded(hr, "Failed to delete single directory: %ls", sczSubFolder);

                // Put the directory back and we'll test deleting tree.
                hr = DirEnsureExists(sczSubFolder, NULL);
                NativeAssert::Succeeded(hr, "Failed to create single directory: %ls", sczSubFolder);

                hr = DirEnsureDelete(sczFolder, FALSE, TRUE);
                NativeAssert::Succeeded(hr, "Failed to delete directory tree: %ls", sczFolder);

                // Finally, try to create "C:\" which would normally fail, but we want success
                hr = DirEnsureExists(L"C:\\", NULL);
                NativeAssert::Succeeded(hr, "Failed to create C:\\");
            }
            finally
            {
                ReleaseStr(sczSubFolder);
                ReleaseStr(sczFolder);
                ReleaseStr(sczGuid);
                ReleaseStr(sczCurrentDir);
            }
        }
    };
}
