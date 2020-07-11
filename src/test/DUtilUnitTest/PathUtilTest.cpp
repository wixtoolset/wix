// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixTest;

namespace DutilTests
{
    public ref class PathUtil
    {
    public:
        [Fact]
        void PathGetHierarchyArrayTest()
        {
            HRESULT hr = S_OK;
            LPWSTR *rgsczPaths = NULL;
            UINT cPaths = 0;

            try
            {
                hr = PathGetHierarchyArray(L"c:\\foo\\bar\\bas\\a.txt", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for regular file path");
                Assert::Equal<DWORD>(5, cPaths);
                NativeAssert::StringEqual(L"c:\\", rgsczPaths[0]);
                NativeAssert::StringEqual(L"c:\\foo\\", rgsczPaths[1]);
                NativeAssert::StringEqual(L"c:\\foo\\bar\\", rgsczPaths[2]);
                NativeAssert::StringEqual(L"c:\\foo\\bar\\bas\\", rgsczPaths[3]);
                NativeAssert::StringEqual(L"c:\\foo\\bar\\bas\\a.txt", rgsczPaths[4]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"c:\\foo\\bar\\bas\\", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for regular directory path");
                Assert::Equal<DWORD>(4, cPaths);
                NativeAssert::StringEqual(L"c:\\", rgsczPaths[0]);
                NativeAssert::StringEqual(L"c:\\foo\\", rgsczPaths[1]);
                NativeAssert::StringEqual(L"c:\\foo\\bar\\", rgsczPaths[2]);
                NativeAssert::StringEqual(L"c:\\foo\\bar\\bas\\", rgsczPaths[3]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"\\\\server\\share\\subdir\\file.txt", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for UNC file path");
                Assert::Equal<DWORD>(3, cPaths);
                NativeAssert::StringEqual(L"\\\\server\\share\\", rgsczPaths[0]);
                NativeAssert::StringEqual(L"\\\\server\\share\\subdir\\", rgsczPaths[1]);
                NativeAssert::StringEqual(L"\\\\server\\share\\subdir\\file.txt", rgsczPaths[2]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"\\\\server\\share\\subdir\\", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for UNC directory path");
                Assert::Equal<DWORD>(2, cPaths);
                NativeAssert::StringEqual(L"\\\\server\\share\\", rgsczPaths[0]);
                NativeAssert::StringEqual(L"\\\\server\\share\\subdir\\", rgsczPaths[1]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"Software\\Microsoft\\Windows\\ValueName", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for UNC directory path");
                Assert::Equal<DWORD>(4, cPaths);
                NativeAssert::StringEqual(L"Software\\", rgsczPaths[0]);
                NativeAssert::StringEqual(L"Software\\Microsoft\\", rgsczPaths[1]);
                NativeAssert::StringEqual(L"Software\\Microsoft\\Windows\\", rgsczPaths[2]);
                NativeAssert::StringEqual(L"Software\\Microsoft\\Windows\\ValueName", rgsczPaths[3]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"Software\\Microsoft\\Windows\\", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for UNC directory path");
                Assert::Equal<DWORD>(3, cPaths);
                NativeAssert::StringEqual(L"Software\\", rgsczPaths[0]);
                NativeAssert::StringEqual(L"Software\\Microsoft\\", rgsczPaths[1]);
                NativeAssert::StringEqual(L"Software\\Microsoft\\Windows\\", rgsczPaths[2]);
                ReleaseNullStrArray(rgsczPaths, cPaths);
            }
            finally
            {
                ReleaseStrArray(rgsczPaths, cPaths);
            }
        }
    };
}
