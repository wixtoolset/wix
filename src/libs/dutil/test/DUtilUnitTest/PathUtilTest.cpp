// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace System::IO;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;

namespace DutilTests
{
    public ref class PathUtil
    {
    public:
        [Fact]
        void PathBackslashFixedTerminateTest()
        {
            HRESULT hr = S_OK;
            WCHAR wzEmpty[1] = { L'\0' };
            WCHAR wzSingleLetter[1] = { L'a' };
            WCHAR wzSingleBackslash[1] = { L'\\' };
            WCHAR wzSingleForwardSlash[1] = { L'/' };
            WCHAR wzSingleLetterNullTerminated[2] = { L'a', L'\0' };
            WCHAR wzSingleBackslashNullTerminated[2] = { L'\\', L'\0' };
            WCHAR wzSingleForwardSlashNullTerminated[2] = { L'/', L'\0' };
            WCHAR wzExtraSpaceLetterNullTerminated[3] = { L'a', L'\0', L'\0' };
            WCHAR wzExtraSpaceBackslashNullTerminated[3] = { L'\\', L'\0', L'\0' };
            WCHAR wzExtraSpaceForwardSlashNullTerminated[3] = { L'/', L'\0', L'\0' };

            hr = PathFixedBackslashTerminate(wzEmpty, 0);
            NativeAssert::SpecificReturnCode(E_INSUFFICIENT_BUFFER, hr, "PathFixedBackslashTerminate: zero-length, {0}", wzEmpty);

            hr = PathFixedBackslashTerminate(wzEmpty, countof(wzEmpty));
            NativeAssert::SpecificReturnCode(E_INSUFFICIENT_BUFFER, hr, "PathFixedBackslashTerminate: '' (length 1), {0}", wzEmpty);

            hr = PathFixedBackslashTerminate(wzSingleLetter, countof(wzSingleLetter));
            NativeAssert::SpecificReturnCode(E_INVALIDARG, hr, "PathFixedBackslashTerminate: 'a' (length 1)");

            hr = PathFixedBackslashTerminate(wzSingleBackslash, countof(wzSingleBackslash));
            NativeAssert::SpecificReturnCode(E_INVALIDARG, hr, "PathFixedBackslashTerminate: '\\' (length 1)");

            hr = PathFixedBackslashTerminate(wzSingleForwardSlash, countof(wzSingleForwardSlash));
            NativeAssert::SpecificReturnCode(E_INVALIDARG, hr, "PathFixedBackslashTerminate: '/' (length 1)");

            hr = PathFixedBackslashTerminate(wzSingleLetterNullTerminated, countof(wzSingleLetterNullTerminated));
            NativeAssert::SpecificReturnCode(E_INSUFFICIENT_BUFFER, hr, "PathFixedBackslashTerminate: 'a' (length 2)");

            hr = PathFixedBackslashTerminate(wzSingleBackslashNullTerminated, countof(wzSingleBackslashNullTerminated));
            NativeAssert::Succeeded(hr, "PathFixedBackslashTerminate: '\\' (length 2)");
            NativeAssert::StringEqual(L"\\", wzSingleBackslashNullTerminated);

            hr = PathFixedBackslashTerminate(wzSingleForwardSlashNullTerminated, countof(wzSingleForwardSlashNullTerminated));
            NativeAssert::Succeeded(hr, "PathFixedBackslashTerminate: '/' (length 2)");
            NativeAssert::StringEqual(L"\\", wzSingleForwardSlashNullTerminated);

            hr = PathFixedBackslashTerminate(wzExtraSpaceLetterNullTerminated, countof(wzExtraSpaceLetterNullTerminated));
            NativeAssert::Succeeded(hr, "PathFixedBackslashTerminate: 'a' (length 3)");
            NativeAssert::StringEqual(L"a\\", wzExtraSpaceLetterNullTerminated);

            hr = PathFixedBackslashTerminate(wzExtraSpaceBackslashNullTerminated, countof(wzExtraSpaceBackslashNullTerminated));
            NativeAssert::Succeeded(hr, "PathFixedBackslashTerminate: '\\' (length 3)");
            NativeAssert::StringEqual(L"\\", wzExtraSpaceBackslashNullTerminated);

            hr = PathFixedBackslashTerminate(wzExtraSpaceForwardSlashNullTerminated, countof(wzExtraSpaceForwardSlashNullTerminated));
            NativeAssert::Succeeded(hr, "PathFixedBackslashTerminate: '/' (length 3)");
            NativeAssert::StringEqual(L"\\", wzExtraSpaceForwardSlashNullTerminated);
        }

        [Fact]
        void PathBackslashTerminateTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[16] =
            {
                L"", L"\\",
                L"a", L"a\\",
                L"\\", L"\\",
                L"a\\", L"a\\",
                L"/", L"\\",
                L"a/", L"a\\",
                L"\\\\", L"\\\\",
                L"//", L"/\\",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 2)
                {
                    hr = StrAllocString(&sczPath, rgwzPaths[i], 0);
                    NativeAssert::Succeeded(hr, "Failed to copy string");

                    hr = PathBackslashTerminate(&sczPath);
                    NativeAssert::Succeeded(hr, "PathBackslashTerminate: {0}", rgwzPaths[i]);
                    NativeAssert::StringEqual(rgwzPaths[i + 1], sczPath);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathCanonicalizeForComparisonTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczCanonicalized = NULL;

            try
            {
                hr = PathCanonicalizeForComparison(L"C:\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789", 0, &sczCanonicalized);
                Assert::Equal<HRESULT>(HRESULT_FROM_WIN32(ERROR_FILENAME_EXCED_RANGE), hr);

                hr = PathCanonicalizeForComparison(L"\\\\?\\C:\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789", 0, &sczCanonicalized);
                Assert::Equal<HRESULT>(HRESULT_FROM_WIN32(ERROR_FILENAME_EXCED_RANGE), hr);

                hr = PathCanonicalizeForComparison(L"\\\\server", PATH_CANONICALIZE_KEEP_UNC_ROOT, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\server", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\server", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\server", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\server\\", PATH_CANONICALIZE_KEEP_UNC_ROOT, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\server\\", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\server\\share", PATH_CANONICALIZE_KEEP_UNC_ROOT, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\server\\share", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\server\\share\\", PATH_CANONICALIZE_KEEP_UNC_ROOT, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\server\\share\\", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\.\\UNC\\server\\share\\..\\unc.exe", PATH_CANONICALIZE_KEEP_UNC_ROOT, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\?\\UNC\\server\\share\\unc.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\..\\share\\otherdir\\unc.exe", PATH_CANONICALIZE_KEEP_UNC_ROOT, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\..\\share\\otherdir\\unc.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\..\\share\\otherdir\\unc.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\share\\otherdir\\unc.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\.\\UNC\\share\\otherdir\\unc.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\UNC\\share\\otherdir\\unc.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\server\\share\\..\\..\\otherdir\\unc.exe", PATH_CANONICALIZE_KEEP_UNC_ROOT, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\server\\share\\otherdir\\unc.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\server\\share\\..\\..\\otherdir\\unc.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\otherdir\\unc.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\??\\UNC\\server\\share\\dir", PATH_CANONICALIZE_KEEP_UNC_ROOT, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\?\\UNC\\server\\share\\dir", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\?\\UNC\\server\\share\\..\\..\\otherdir\\unc.exe", PATH_CANONICALIZE_KEEP_UNC_ROOT, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\?\\UNC\\server\\share\\otherdir\\unc.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\?\\UNC\\server\\share\\..\\..\\otherdir\\unc.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\otherdir\\unc.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"C:\\dir\\subdir\\..\\..\\..\\otherdir\\pastroot.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"C:\\otherdir\\pastroot.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\?\\C:\\dir\\subdir\\..\\..\\..\\otherdir\\pastroot.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"C:\\otherdir\\pastroot.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\?\\C:dir\\subdir\\..\\..\\..\\otherdir\\pastroot.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\otherdir\\pastroot.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"C:dir\\subdir\\..\\..\\..\\otherdir\\pastrelativeroot.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\otherdir\\pastrelativeroot.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"A:dir\\subdir\\..\\..\\otherdir\\relativeroot.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\otherdir\\relativeroot.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"C:dir\\subdir\\otherdir\\relativeroot.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"C:dir\\subdir\\otherdir\\relativeroot.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"C:\\dir\\subdir\\..\\..\\otherdir\\backslashes.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"C:\\otherdir\\backslashes.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"C:\\dir\\subdir\\..\\..\\otherdir\\\\consecutivebackslashes.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"C:\\otherdir\\consecutivebackslashes.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"C:/dir/subdir/../../otherdir/forwardslashes.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"C:\\otherdir\\forwardslashes.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\?\\C:\\test\\..\\validlongpath.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"C:\\validlongpath.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\\\?\\test\\..\\invalidlongpath.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\?\\invalidlongpath.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"\\??\\test\\..\\invalidlongpath.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\?\\invalidlongpath.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"C:\\.\\invalid:pathchars?.exe", 0, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"C:\\invalid:pathchars?.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"C:\\addprefix.exe", PATH_CANONICALIZE_APPEND_EXTENDED_PATH_PREFIX, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"\\\\?\\C:\\addprefix.exe", sczCanonicalized);

                hr = PathCanonicalizeForComparison(L"C:\\addbackslash.exe", PATH_CANONICALIZE_BACKSLASH_TERMINATE, &sczCanonicalized);
                NativeAssert::Succeeded(hr, "Failed to canonicalize path");
                NativeAssert::StringEqual(L"C:\\addbackslash.exe\\", sczCanonicalized);
            }
            finally
            {
                ReleaseStr(sczCanonicalized);
            }
        }

        [Fact]
        void PathConcatTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[54] =
            {
                L"a", NULL, L"a",
                L"a", L"", L"a",
                L"C:\\", L"a", L"C:\\a",
                L"\\a", L"b", L"\\a\\b",
                L"a", L"b", L"a\\b",
                L"C:\\", L"..\\a", L"C:\\..\\a",
                L"C:\\a", L"..\\b", L"C:\\a\\..\\b",
                L"\\\\server\\share", L"..\\a", L"\\\\server\\share\\..\\a",
                L"\\\\server\\share\\a", L"..\\b", L"\\\\server\\share\\a\\..\\b",
                NULL, L"b", L"b",
                L"", L"b", L"b",
                L"a", L"\\b", L"\\b",
                L"a", L"b:", L"b:",
                L"a", L"b:\\", L"b:\\",
                L"a", L"\\\\?\\b", L"\\\\?\\b",
                L"a", L"\\\\?\\UNC\\b", L"\\\\?\\UNC\\b",
                L"a", L"\\b", L"\\b",
                L"a", L"\\\\", L"\\\\",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 3)
                {
                    hr = PathConcat(rgwzPaths[i], rgwzPaths[i + 1], &sczPath);
                    NativeAssert::Succeeded(hr, "PathConcat: {0}, {1}", rgwzPaths[i], rgwzPaths[i + 1]);
                    NativeAssert::StringEqual(rgwzPaths[i + 2], sczPath);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathCompareCanonicalizeEqualTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            BOOL fEqual = FALSE;
            LPCWSTR rgwzPaths[14] =
            {
                L"C:\\simplepath", L"C:\\simplepath",
                L"\\\\server\\share\\dir\\dir2\\..\\otherdir\\unc.exe", L"\\\\server\\share\\dir\\otherdir\\unc.exe",
                L"\\\\server\\share\\..\\..\\otherdir\\unc.exe", L"\\\\server\\share\\otherdir\\unc.exe",
                L"\\\\?\\UNC\\server\\share\\..\\..\\otherdir\\unc.exe", L"\\\\?\\UNC\\server\\share\\otherdir\\unc.exe",
                L"C:\\dir\\subdir\\..\\..\\..\\otherdir\\pastroot.exe", L"C:\\otherdir\\pastroot.exe",
                L"\\\\?\\C:\\dir\\subdir\\..\\..\\..\\otherdir\\pastroot.exe", L"C:\\..\\otherdir\\pastroot.exe",
                L"\\??\\C:\\dir", L"\\\\?\\C:\\dir",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 2)
                {
                    hr = PathCompareCanonicalized(rgwzPaths[i], rgwzPaths[i + 1], &fEqual);
                    NativeAssert::Succeeded(hr, "PathCompareCanonicalized: {0}, {1}", rgwzPaths[i], rgwzPaths[i + 1]);
                    Assert::True(fEqual, String::Format("PathCompareCanonicalized: {0}, {1}", gcnew String(rgwzPaths[i]), gcnew String(rgwzPaths[i + 1])));
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathCompareCanonicalizeNotEqualTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            BOOL fEqual = FALSE;
            LPCWSTR rgwzPaths[8] =
            {
                L"C:\\simplepath", L"D:\\simplepath",
                L"\\\\..\\share\\otherdir\\unc.exe", L"\\\\share\\otherdir\\unc.exe",
                L"\\\\server\\.\\otherdir\\unc.exe", L"\\\\server\\otherdir\\unc.exe",
                L"\\\\server\\\\otherdir\\unc.exe", L"\\\\server\\otherdir\\unc.exe",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 2)
                {
                    hr = PathCompareCanonicalized(rgwzPaths[i], rgwzPaths[i + 1], &fEqual);
                    NativeAssert::Succeeded(hr, "PathCompareCanonicalized: {0}, {1}", rgwzPaths[i], rgwzPaths[i + 1]);
                    Assert::False(fEqual, String::Format("PathCompareCanonicalized: {0}, {1}", gcnew String(rgwzPaths[i]), gcnew String(rgwzPaths[i + 1])));
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathConcatRelativeToBaseTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[27] =
            {
                L"a", NULL, L"a",
                L"a", L"", L"a",
                L"C:\\", L"a", L"C:\\a",
                L"\\a", L"b", L"\\a\\b",
                L"a", L"b", L"a\\b",
                L"C:\\", L"..\\a", L"C:\\a",
                L"C:\\a", L"..\\b", L"C:\\a\\b",
                L"\\\\server\\share", L"..\\a", L"\\\\server\\share\\a",
                L"\\\\server\\share\\a", L"..\\b", L"\\\\server\\share\\a\\b",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 3)
                {
                    hr = PathConcatRelativeToBase(rgwzPaths[i], rgwzPaths[i + 1], &sczPath);
                    NativeAssert::Succeeded(hr, "PathConcatRelativeToBase: {0}, {1}", rgwzPaths[i], rgwzPaths[i + 1]);
                    NativeAssert::StringEqual(rgwzPaths[i + 2], sczPath);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathConcatRelativeToBaseFailureTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[18] =
            {
                NULL, L"b",
                L"", L"b",
                L"a", L"\\b",
                L"a", L"b:",
                L"a", L"b:\\",
                L"a", L"\\\\?\\b",
                L"a", L"\\\\?\\UNC\\b",
                L"a", L"\\b",
                L"a", L"\\\\",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 2)
                {
                    hr = PathConcatRelativeToBase(rgwzPaths[i], rgwzPaths[i + 1], &sczPath);
                    NativeAssert::SpecificReturnCode(hr, E_INVALIDARG, "PathConcatRelativeToBase: {0}, {1}", rgwzPaths[i], rgwzPaths[i + 1]);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathDirectoryContainsPathTest()
        {
            HRESULT hr = S_OK;

            hr = PathDirectoryContainsPath(L"", L"");
            Assert::Equal<HRESULT>(E_INVALIDARG, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory", L"");
            Assert::Equal<HRESULT>(E_INVALIDARG, hr);

            hr = PathDirectoryContainsPath(L"", L"C:\\Directory");
            Assert::Equal<HRESULT>(E_INVALIDARG, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory", L"C:\\Directory");
            Assert::Equal<HRESULT>(S_FALSE, hr);

            hr = PathDirectoryContainsPath(L"C:\\Dir", L"C:\\Directory");
            Assert::Equal<HRESULT>(S_FALSE, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory", L"C:\\");
            Assert::Equal<HRESULT>(S_FALSE, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory", L"C:\\DirectoryPlus");
            Assert::Equal<HRESULT>(S_FALSE, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory\\", L"C:\\DirectoryPlus");
            Assert::Equal<HRESULT>(S_FALSE, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory\\", L"C:\\Directory\\../Plus");
            Assert::Equal<HRESULT>(S_FALSE, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory\\", L"C:\\Directory/../Plus");
            Assert::Equal<HRESULT>(S_FALSE, hr);

            hr = PathDirectoryContainsPath(L"\\\\server\\share\\Directory", L"\\\\server\\share\\DirectoryPlus");
            Assert::Equal<HRESULT>(S_FALSE, hr);

            hr = PathDirectoryContainsPath(L"\\\\server\\share\\Directory", L"\\\\discarded\\..\\server\\share\\Directory\\Plus");
            Assert::Equal<HRESULT>(S_FALSE, hr);

            hr = PathDirectoryContainsPath(L"..\\..", L"..\\..\\plus");
            Assert::Equal<HRESULT>(E_INVALIDARG, hr);

            hr = PathDirectoryContainsPath(L"..\\..", L"\\..\\..\\plus");
            Assert::Equal<HRESULT>(E_INVALIDARG, hr);

            hr = PathDirectoryContainsPath(L"\\..\\..", L"\\..\\..\\plus");
            Assert::Equal<HRESULT>(E_INVALIDARG, hr);

            hr = PathDirectoryContainsPath(L"C:..\\..", L"C:..\\..\\plus");
            Assert::Equal<HRESULT>(E_INVALIDARG, hr);

            hr = PathDirectoryContainsPath(L"\\\\server\\share\\Directory", L"\\\\server\\share\\Directory\\Plus");
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory", L"C:\\directory\\plus");
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory\\", L"C:\\Directory\\Plus");
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory", L"C:\\.\\Directory\\Plus");
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory", L"C:\\Directory/Plus");
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathDirectoryContainsPath(L"C:\\Directory\\", L"C:\\Directory/Plus");
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathDirectoryContainsPath(L"\\\\?\\C:\\Directory", L"C:\\Directory\\Plus");
            Assert::Equal<HRESULT>(S_OK, hr);
        }

        [Fact]
        void PathExpandEnvironmentVariablesTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczExpanded = NULL;
            LPCWSTR rgwzPaths[4] =
            {
                L"", L"",
                L"\\\\?\\", L"%TEMP%;%PATH%;C:\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789\\abcdefghijklomnopqrstuvwxyz0123456789",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 2)
                {
                    hr = PathExpand(&sczExpanded, rgwzPaths[i + 1], PATH_EXPAND_ENVIRONMENT);
                    NativeAssert::Succeeded(hr, "PathExpand: {0}", rgwzPaths[i + 1]);
                    WixAssert::StringEqual((gcnew String(rgwzPaths[i])) + Environment::ExpandEnvironmentVariables(gcnew String(rgwzPaths[i + 1])), gcnew String(sczExpanded), false);
                }
            }
            finally
            {
                ReleaseStr(sczExpanded);
            }
        }

        [Fact]
        void PathExpandFullPathTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczExpanded = NULL;
            LPCWSTR wzGreaterThanMaxPathString = L"abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz\\a.txt";

            try
            {
                hr = PathExpand(&sczExpanded, wzGreaterThanMaxPathString, PATH_EXPAND_FULLPATH);
                NativeAssert::Succeeded(hr, "Failed to expand greater than MAX_PATH string.");
                WixAssert::StringEqual((gcnew String("\\\\?\\")) + Path::Combine(Environment::CurrentDirectory, gcnew String(wzGreaterThanMaxPathString)), gcnew String(sczExpanded), false);
            }
            finally
            {
                ReleaseStr(sczExpanded);
            }
        }

        [Fact]
        void PathExpandAllTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczExpanded = NULL;
            LPCWSTR wzRelativeEnvironmentVariableString = L"%USERNAME%\\abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz\\a.txt";

            try
            {
                hr = PathExpand(&sczExpanded, wzRelativeEnvironmentVariableString, PATH_EXPAND_ENVIRONMENT | PATH_EXPAND_FULLPATH);
                NativeAssert::Succeeded(hr, "Failed to expand path.");
                WixAssert::StringEqual((gcnew String("\\\\?\\")) + Path::Combine(Environment::CurrentDirectory, Environment::ExpandEnvironmentVariables(gcnew String(wzRelativeEnvironmentVariableString))), gcnew String(sczExpanded), false);
            }
            finally
            {
                ReleaseStr(sczExpanded);
            }
        }

        [Fact]
        void PathGetDirectoryTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[20] =
            {
                L"C:\\a\\b", L"C:\\a\\",
                L"C:\\a\\b\\", L"C:\\a\\b\\",
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
        void PathGetParentPathTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[20] =
            {
                L"C:\\a\\b", L"C:\\a\\",
                L"C:\\a\\b\\", L"C:\\a\\",
                L"C:\\a", L"C:\\",
                L"C:\\", NULL,
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
                    hr = PathGetParentPath(rgwzPaths[i], &sczPath, NULL);
                    NativeAssert::Succeeded(hr, "PathGetParentPath: {0}", rgwzPaths[i]);
                    NativeAssert::StringEqual(rgwzPaths[i + 1], sczPath);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathGetFullPathNameTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR wzFileName = NULL;
            LPCWSTR rgwzPaths[33] =
            {
                L"C:\\", L"C:\\", NULL,
                L"C:\\file", L"C:\\file", L"file",
                L"C:\\..\\file", L"C:\\file", L"file",
                L"C:\\dir\\..\\file.txt", L"C:\\file.txt", L"file.txt",
                L"C:\\dir\\\\file.txt.txt", L"C:\\dir\\file.txt.txt", L"file.txt.txt",
                L"C:\\dir/.file", L"C:\\dir\\.file", L".file",
                L"\\\\?\\C:\\file", L"\\\\?\\C:\\file", L"file",
                L"\\\\server\\share\\file", L"\\\\server\\share\\file", L"file",
                L"\\\\server\\share\\..\\file", L"\\\\server\\share\\file", L"file",
                L"\\\\?\\UNC\\server\\share\\file", L"\\\\?\\UNC\\server\\share\\file", L"file",
                L"C:\\abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz\\a.txt", L"C:\\abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz\\a.txt", L"a.txt",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 3)
                {
                    hr = PathGetFullPathName(rgwzPaths[i], &sczPath, &wzFileName, NULL);
                    NativeAssert::Succeeded(hr, "PathGetFullPathName: {0}", rgwzPaths[i]);
                    NativeAssert::StringEqual(rgwzPaths[i + 1], sczPath);
                    NativeAssert::StringEqual(rgwzPaths[i + 2], wzFileName);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathGetFullPathNameRelativeTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[4] =
            {
                L"",
                L"a.txt",
                L"abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz\\a.txt",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); ++i)
                {
                    hr = PathGetFullPathName(rgwzPaths[i], &sczPath, NULL, NULL);
                    NativeAssert::Succeeded(hr, "PathGetFullPathName: {0}", rgwzPaths[i]);
                    WixAssert::StringEqual(Path::Combine(Environment::CurrentDirectory, gcnew String(rgwzPaths[i])), gcnew String(sczPath), false);
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

                hr = PathGetHierarchyArray(L"Software", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for relative path");
                Assert::Equal<DWORD>(1, cPaths);
                NativeAssert::StringEqual(L"Software", rgsczPaths[0]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"c:/foo/bar/bas/a.txt", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for regular file path");
                Assert::Equal<DWORD>(5, cPaths);
                NativeAssert::StringEqual(L"c:/", rgsczPaths[0]);
                NativeAssert::StringEqual(L"c:/foo/", rgsczPaths[1]);
                NativeAssert::StringEqual(L"c:/foo/bar/", rgsczPaths[2]);
                NativeAssert::StringEqual(L"c:/foo/bar/bas/", rgsczPaths[3]);
                NativeAssert::StringEqual(L"c:/foo/bar/bas/a.txt", rgsczPaths[4]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"c:/foo/bar/bas/", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for regular directory path");
                Assert::Equal<DWORD>(4, cPaths);
                NativeAssert::StringEqual(L"c:/", rgsczPaths[0]);
                NativeAssert::StringEqual(L"c:/foo/", rgsczPaths[1]);
                NativeAssert::StringEqual(L"c:/foo/bar/", rgsczPaths[2]);
                NativeAssert::StringEqual(L"c:/foo/bar/bas/", rgsczPaths[3]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"//server/share/subdir/file.txt", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for UNC file path");
                Assert::Equal<DWORD>(3, cPaths);
                NativeAssert::StringEqual(L"//server/share/", rgsczPaths[0]);
                NativeAssert::StringEqual(L"//server/share/subdir/", rgsczPaths[1]);
                NativeAssert::StringEqual(L"//server/share/subdir/file.txt", rgsczPaths[2]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"//server/share/subdir/", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for UNC directory path");
                Assert::Equal<DWORD>(2, cPaths);
                NativeAssert::StringEqual(L"//server/share/", rgsczPaths[0]);
                NativeAssert::StringEqual(L"//server/share/subdir/", rgsczPaths[1]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"Software/Microsoft/Windows/ValueName", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for UNC directory path");
                Assert::Equal<DWORD>(4, cPaths);
                NativeAssert::StringEqual(L"Software/", rgsczPaths[0]);
                NativeAssert::StringEqual(L"Software/Microsoft/", rgsczPaths[1]);
                NativeAssert::StringEqual(L"Software/Microsoft/Windows/", rgsczPaths[2]);
                NativeAssert::StringEqual(L"Software/Microsoft/Windows/ValueName", rgsczPaths[3]);
                ReleaseNullStrArray(rgsczPaths, cPaths);

                hr = PathGetHierarchyArray(L"Software/Microsoft/Windows/", &rgsczPaths, &cPaths);
                NativeAssert::Succeeded(hr, "Failed to get parent directories array for UNC directory path");
                Assert::Equal<DWORD>(3, cPaths);
                NativeAssert::StringEqual(L"Software/", rgsczPaths[0]);
                NativeAssert::StringEqual(L"Software/Microsoft/", rgsczPaths[1]);
                NativeAssert::StringEqual(L"Software/Microsoft/Windows/", rgsczPaths[2]);
                ReleaseNullStrArray(rgsczPaths, cPaths);
            }
            finally
            {
                ReleaseStrArray(rgsczPaths, cPaths);
            }
        }

        [Fact]
        void PathNormalizeSlashesFixedTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[54] =
            {
                L"", L"",
                L"\\", L"\\",
                L"\\\\", L"\\\\",
                L"\\\\\\", L"\\\\\\",
                L"\\\\?\\UNC\\", L"\\\\?\\UNC\\",
                L"C:\\\\foo2", L"C:\\foo2",
                L"\\\\?\\C:\\\\foo2", L"\\\\?\\C:\\foo2",
                L"\\\\a\\b\\", L"\\\\a\\b\\",
                L"\\\\?\\UNC\\a\\b\\\\c\\", L"\\\\?\\UNC\\a\\b\\c\\",
                L"\\\\?\\UNC\\a\\b\\\\", L"\\\\?\\UNC\\a\\b\\",
                L"\\\\?\\UNC\\test\\unc\\path\\to\\\\something", L"\\\\?\\UNC\\test\\unc\\path\\to\\something",
                L"\\\\?\\C:\\\\foo\\\\bar.txt", L"\\\\?\\C:\\foo\\bar.txt",
                L"\\??\\C:\\\\foo\\bar.txt", L"\\??\\C:\\foo\\bar.txt",
                L"\\??\\\\C:\\\\foo\\bar.txt", L"\\??\\\\C:\\foo\\bar.txt",
                L"/", L"\\",
                L"//", L"\\\\",
                L"///", L"\\\\\\",
                L"//?/UNC/", L"\\\\?\\UNC\\",
                L"C://foo2", L"C:\\foo2",
                L"//?/C://foo2", L"\\\\?\\C:\\foo2",
                L"//a/b/", L"\\\\a\\b\\",
                L"//?/UNC/a/b//c/", L"\\\\?\\UNC\\a\\b\\c\\",
                L"//?/UNC/a/b//", L"\\\\?\\UNC\\a\\b\\",
                L"//?/UNC/test/unc/path/to//something", L"\\\\?\\UNC\\test\\unc\\path\\to\\something",
                L"//?/C://foo//bar.txt", L"\\\\?\\C:\\foo\\bar.txt",
                L"/??/C://foo/bar.txt", L"\\??\\C:\\foo\\bar.txt",
                L"/??//C://foo/bar.txt", L"\\??\\\\C:\\foo\\bar.txt",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 2)
                {
                    hr = StrAllocString(&sczPath, rgwzPaths[i], 0);
                    NativeAssert::Succeeded(hr, "Failed to copy string");

                    hr = PathFixedNormalizeSlashes(sczPath);
                    NativeAssert::Succeeded(hr, "PathNormalizeSlashes: {0}", rgwzPaths[i]);
                    NativeAssert::StringEqual(rgwzPaths[i + 1], sczPath);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
            }
        }

        [Fact]
        void PathPrefixTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPCWSTR rgwzPaths[24] =
            {
                L"\\\\", L"\\\\?\\UNC\\",
                L"C:\\\\foo2", L"\\\\?\\C:\\\\foo2",
                L"\\\\a\\b\\", L"\\\\?\\UNC\\a\\b\\",
                L"\\\\?\\UNC\\test\\unc\\path\\to\\something", L"\\\\?\\UNC\\test\\unc\\path\\to\\something",
                L"\\\\?\\C:\\foo\\bar.txt", L"\\\\?\\C:\\foo\\bar.txt",
                L"\\??\\C:\\foo\\bar.txt", L"\\??\\C:\\foo\\bar.txt",
                L"//", L"\\\\?\\UNC\\",
                L"C://foo2", L"\\\\?\\C://foo2",
                L"//a/b/", L"\\\\?\\UNC\\a/b/",
                L"//?/UNC/test/unc/path/to/something", L"//?/UNC/test/unc/path/to/something",
                L"//?/C:/foo/bar.txt", L"//?/C:/foo/bar.txt",
                L"/??/C:/foo/bar.txt", L"/??/C:/foo/bar.txt",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); i += 2)
                {
                    hr = StrAllocString(&sczPath, rgwzPaths[i], 0);
                    NativeAssert::Succeeded(hr, "Failed to copy string");

                    hr = PathPrefix(&sczPath, 0, 0);
                    NativeAssert::Succeeded(hr, "PathPrefix: {0}", rgwzPaths[i]);
                    NativeAssert::StringEqual(rgwzPaths[i], sczPath);

                    hr = PathPrefix(&sczPath, 0, PATH_PREFIX_SHORT_PATHS);
                    NativeAssert::Succeeded(hr, "PathPrefix (SHORT_PATHS): {0}", rgwzPaths[i]);
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
            LPCWSTR rgwzPaths[12] =
            {
                L"\\",
                L"/",
                L"C:",
                L"C:foo.txt",
                L"",
                L"\\?",
                L"/?",
                L"\\dir",
                L"/dir",
                L"dir",
                L"dir\\subdir",
                L"dir/subdir",
            };

            try
            {
                for (DWORD i = 0; i < countof(rgwzPaths); ++i)
                {
                    hr = StrAllocString(&sczPath, rgwzPaths[i], 0);
                    NativeAssert::Succeeded(hr, "Failed to copy string");

                    hr = PathPrefix(&sczPath, 0, PATH_PREFIX_EXPECT_FULLY_QUALIFIED);
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
            LPCWSTR rgwzPaths[30] =
            {
                L"//", L"",
                L"///", L"",
                L"C:/", L"",
                L"C://", L"/",
                L"C:/foo1", L"foo1",
                L"C://foo2", L"/foo2",
                L"//test/unc/path/to/something", L"path/to/something",
                L"//a/b/c/d/e", L"c/d/e",
                L"//a/b/", L"",
                L"//a/b", L"",
                L"//test/unc", L"",
                L"//Server", L"",
                L"//Server/Foo.txt", L"",
                L"//Server/Share/Foo.txt", L"Foo.txt",
                L"//Server/Share/Test/Foo.txt", L"Test/Foo.txt",
            };

            ValidateSkipPastRoot(rgwzPaths, countof(rgwzPaths), FALSE, TRUE, TRUE);
        }

        [Fact]
        void PathIsRootedAndFullyQualifiedWithPrefixTest()
        {
            LPCWSTR rgwzPaths[12] =
            {
                L"//?/UNC/test/unc/path/to/something", L"path/to/something",
                L"//?/UNC/test/unc", L"",
                L"//?/UNC/a/b1", L"",
                L"//?/UNC/a/b2/", L"",
                L"//?/C:/foo/bar.txt", L"foo/bar.txt",
                L"/??/C:/foo/bar.txt", L"foo/bar.txt",
            };

            ValidateSkipPastRoot(rgwzPaths, countof(rgwzPaths), TRUE, TRUE, TRUE);
        }

        [Fact]
        void PathIsRootedButNotFullyQualifiedTest()
        {
            LPCWSTR rgwzPaths[14] =
            {
                L"/", L"",
                L"a:", L"",
                L"A:", L"",
                L"z:", L"",
                L"Z:", L"",
                L"C:foo.txt", L"foo.txt",
                L"/dir", L"dir",
            };

            ValidateSkipPastRoot(rgwzPaths, countof(rgwzPaths), FALSE, FALSE, TRUE);
        }

        [Fact]
        void PathIsNotRootedAndNotFullyQualifiedTest()
        {
            LPCWSTR rgwzPaths[18] =
            {
                NULL, NULL,
                L"", NULL,
                L"dir", NULL,
                L"dir/subdir", NULL,
                L"@:/foo", NULL,  // 064 = @     065 = A
                L"[://", NULL,   // 091 = [     090 = Z
                L"`:/foo ", NULL, // 096 = `     097 = a
                L"{://", NULL,   // 123 = {     122 = z
                L"[:", NULL,
            };

            ValidateSkipPastRoot(rgwzPaths, countof(rgwzPaths), FALSE, FALSE, FALSE);
        }

        void ValidateSkipPastRoot(LPCWSTR* rgwzPaths, DWORD cPaths, BOOL fExpectedPrefix, BOOL fExpectedFullyQualified, BOOL fExpectedRooted)
        {
            HRESULT hr = S_OK;
            LPWSTR sczPath = NULL;
            LPWSTR sczSkipRootPath = NULL;
            LPCWSTR wzSkipRootPath = NULL;
            BOOL fHasPrefix = FALSE;

            try
            {
                for (DWORD i = 0; i < cPaths; i += 2)
                {
                    wzSkipRootPath = PathSkipPastRoot(rgwzPaths[i], &fHasPrefix, NULL, NULL);
                    NativeAssert::StringEqual(rgwzPaths[i + 1], wzSkipRootPath);
                    ValidateExtendedPrefixPath(rgwzPaths[i], fExpectedPrefix, fHasPrefix);
                    ValidateFullyQualifiedPath(rgwzPaths[i], fExpectedFullyQualified);
                    ValidateRootedPath(rgwzPaths[i], fExpectedRooted);

                    if (rgwzPaths[i])
                    {
                        hr = StrAllocString(&sczPath, rgwzPaths[i], 0);
                        NativeAssert::Succeeded(hr, "Failed to copy string");

                        PathFixedReplaceForwardSlashes(sczPath);
                    }

                    if (rgwzPaths[i + 1])
                    {
                        hr = StrAllocString(&sczSkipRootPath, rgwzPaths[i + 1], 0);
                        NativeAssert::Succeeded(hr, "Failed to copy string");

                        PathFixedReplaceForwardSlashes(sczSkipRootPath);
                    }

                    wzSkipRootPath = PathSkipPastRoot(sczPath, &fHasPrefix, NULL, NULL);
                    NativeAssert::StringEqual(sczSkipRootPath, wzSkipRootPath);
                    ValidateExtendedPrefixPath(sczPath, fExpectedPrefix, fHasPrefix);
                    ValidateFullyQualifiedPath(sczPath, fExpectedFullyQualified);
                    ValidateRootedPath(sczPath, fExpectedRooted);
                }
            }
            finally
            {
                ReleaseStr(sczPath);
                ReleaseStr(sczSkipRootPath);
            }
        }

        void ValidateExtendedPrefixPath(LPCWSTR wzPath, BOOL fExpected, BOOL fHasExtendedPrefix)
        {
            String^ message = String::Format("HasExtendedPrefix: {0}", gcnew String(wzPath));
            if (fExpected)
            {
                Assert::True(fHasExtendedPrefix, message);
            }
            else
            {
                Assert::False(fHasExtendedPrefix, message);
            }
        }

        void ValidateFullyQualifiedPath(LPCWSTR wzPath, BOOL fExpected)
        {
            BOOL fRooted = PathIsFullyQualified(wzPath);
            String^ message = String::Format("IsFullyQualified: {0}", gcnew String(wzPath));
            if (fExpected)
            {
                Assert::True(fRooted, message);
            }
            else
            {
                Assert::False(fRooted, message);
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
