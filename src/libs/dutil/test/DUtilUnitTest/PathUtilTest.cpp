// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;

namespace DutilTests
{
    public ref class PathUtil
    {
    public:
        [Fact]
        void PathGetDirectoryTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[18] =
            {
                L"C:\\a\\b", L"C:\\a\\",
                L"C:\\a", L"C:\\",
                L"C:\\", L"C:\\",
                L"\"C:\\a\\b\\c\"", L"\"C:\\a\\b\\",
                L"\"C:\\a\\b\\\"c", L"\"C:\\a\\b\\",
                L"\"C:\\a\\b\"\\c", L"\"C:\\a\\b\"\\",
                L"\"C:\\a\\\"b\\c", L"\"C:\\a\\\"b\\",
                L"C:\\a\"\\\"b\\c", L"C:\\a\"\\\"b\\",
                L"C:\\a\"\\b\\c\"", L"C:\\a\"\\b\\",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 2)
                {
                    hr = PathGetDirectory(rgwzPaths[i], &sczPath);
                    NativeAssert::Succeeded(hr, "PathGetDirectory: {0}", rgwzPaths[i]);
                    NativeAssert::StringEqual(rgwzPaths[i + 1], sczPath);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

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

        [Fact]
        void PathPrefixTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[12] =
            {
                L"\\\\", L"\\\\?\\UNC\\",
                L"C:\\\\foo2", L"\\\\?\\C:\\\\foo2",
                L"\\\\a\\b\\", L"\\\\?\\UNC\\a\\b\\",
                L"\\\\?\\UNC\\test\\unc\\path\\to\\something", L"\\\\?\\UNC\\test\\unc\\path\\to\\something",
                L"\\\\?\\C:\\foo\\bar.txt", L"\\\\?\\C:\\foo\\bar.txt",
                L"\\??\\C:\\foo\\bar.txt", L"\\??\\C:\\foo\\bar.txt",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths) / 2; i += 2)
                {
                    hr = StrAllocString(&sczPath, rgwzPaths[i], 0);
                    NativeAssert::Succeeded(hr, "Failed to copy string");

                    hr = PathPrefix(&sczPath);
                    NativeAssert::Succeeded(hr, "PathPrefix: {0}", rgwzPaths[i]);
                    NativeAssert::StringEqual(rgwzPaths[i + 1], sczPath);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathPrefixFailureTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[8] =
            {
                L"\\",
                L"C:",
                L"C:foo.txt",
                L"",
                L"\\?",
                L"\\dir",
                L"dir",
                L"dir\\subdir",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); ++i)
                {
                    hr = StrAllocString(&sczPath, rgwzPaths[i], 0);
                    NativeAssert::Succeeded(hr, "Failed to copy string");

                    hr = PathPrefix(&sczPath);
                    NativeAssert::SpecificReturnCode(E_INVALIDARG, hr, "PathPrefix: {0}, {1}", rgwzPaths[i], sczPath);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathIsRootedAndFullyQualifiedTest()
        {
            LPCWSTR rgwzPaths[15] =
            {
                L"\\\\",
                L"\\\\\\",
                L"C:\\",
                L"C:\\\\",
                L"C:\\foo1",
                L"C:\\\\foo2",
                L"\\\\test\\unc\\path\\to\\something",
                L"\\\\a\\b\\c\\d\\e",
                L"\\\\a\\b\\",
                L"\\\\a\\b",
                L"\\\\test\\unc",
                L"\\\\Server",
                L"\\\\Server\\Foo.txt",
                L"\\\\Server\\Share\\Foo.txt",
                L"\\\\Server\\Share\\Test\\Foo.txt",
            };

            for (DWORD i = 0; i < countof(rgwzPaths); ++i)
            {
                ValidateFullyQualifiedPath(rgwzPaths[i], TRUE, FALSE);
                ValidateRootedPath(rgwzPaths[i], TRUE);
            }
        }

        [Fact]
        void PathIsRootedAndFullyQualifiedWithPrefixTest()
        {
            LPCWSTR rgwzPaths[6] =
            {
                L"\\\\?\\UNC\\test\\unc\\path\\to\\something",
                L"\\\\?\\UNC\\test\\unc",
                L"\\\\?\\UNC\\a\\b1",
                L"\\\\?\\UNC\\a\\b2\\",
                L"\\\\?\\C:\\foo\\bar.txt",
                L"\\??\\C:\\foo\\bar.txt",
            };

            for (DWORD i = 0; i < countof(rgwzPaths); ++i)
            {
                ValidateFullyQualifiedPath(rgwzPaths[i], TRUE, TRUE);
                ValidateRootedPath(rgwzPaths[i], TRUE);
            }
        }

        [Fact]
        void PathIsRootedButNotFullyQualifiedTest()
        {
            LPCWSTR rgwzPaths[7] =
            {
                L"\\",
                L"a:",
                L"A:",
                L"z:",
                L"Z:",
                L"C:foo.txt",
                L"\\dir",
            };

            for (DWORD i = 0; i < countof(rgwzPaths); ++i)
            {
                ValidateFullyQualifiedPath(rgwzPaths[i], FALSE, FALSE);
                ValidateRootedPath(rgwzPaths[i], TRUE);
            }
        }

        [Fact]
        void PathIsNotRootedAndNotFullyQualifiedTest()
        {
            LPCWSTR rgwzPaths[9] =
            {
                NULL,
                L"",
                L"dir",
                L"dir\\subdir",
                L"@:\\foo",  // 064 = @     065 = A
                L"[:\\\\",   // 091 = [     090 = Z
                L"`:\\foo ", // 096 = `     097 = a
                L"{:\\\\",   // 123 = {     122 = z
                L"[:",
            };

            for (DWORD i = 0; i < countof(rgwzPaths); ++i)
            {
                ValidateFullyQualifiedPath(rgwzPaths[i], FALSE, FALSE);
                ValidateRootedPath(rgwzPaths[i], FALSE);
            }
        }

        void ValidateFullyQualifiedPath(LPCWSTR wzPath, BOOL fExpected, BOOL fExpectedHasPrefix)
        {
            BOOL fHasLongPathPrefix = FALSE;
            BOOL fRooted = PathIsFullyQualified(wzPath, &fHasLongPathPrefix);
            String^ message = String::Format("IsFullyQualified: {0}", gcnew String(wzPath));
            if (fExpected)
            {
                Assert::True(fRooted, message);
            }
            else
            {
                Assert::False(fRooted, message);
            }

            message = String::Format("HasLongPathPrefix: {0}", gcnew String(wzPath));
            if (fExpectedHasPrefix)
            {
                Assert::True(fHasLongPathPrefix, message);
            }
            else
            {
                Assert::False(fHasLongPathPrefix, message);
            }
        }

        void ValidateRootedPath(LPCWSTR wzPath, BOOL fExpected)
        {
            BOOL fRooted = PathIsRooted(wzPath);
            String^ message = String::Format("IsRooted: {0}", gcnew String(wzPath));
            if (fExpected)
            {
                Assert::True(fRooted, message);
            }
            else
            {
                Assert::False(fRooted, message);
            }
        }
    };
}
