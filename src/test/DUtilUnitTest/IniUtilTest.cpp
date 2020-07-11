// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixTest;

typedef HRESULT (__clrcall *IniFormatParameters)(
    INI_HANDLE
    );

namespace DutilTests
{
    public ref class IniUtil
    {
    public:
        [Fact]
        void IniUtilTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczTempIniFilePath = NULL;
            LPWSTR sczTempIniFileDir = NULL;
            LPWSTR wzIniContents = L"           PlainValue             =       \t      Blah               \r\n;CommentHere\r\n[Section1]\r\n     ;Another Comment With = Equal Sign\r\nSection1ValueA=Foo\r\n\r\nSection1ValueB=Bar\r\n[Section2]\r\nSection2ValueA=Cha\r\nArray[0]=Arr\r\n";
            LPWSTR wzScriptContents = L"setf ~PlainValue Blah\r\n;CommentHere\r\n\r\nsetf ~Section1\\Section1ValueA Foo\r\n\r\nsetf ~Section1\\Section1ValueB Bar\r\nsetf ~Section2\\Section2ValueA Cha\r\nsetf ~Section2\\Array[0] Arr\r\n";

            try
            {
                hr = PathExpand(&sczTempIniFilePath, L"%TEMP%\\IniUtilTest\\Test.ini", PATH_EXPAND_ENVIRONMENT);
                NativeAssert::Succeeded(hr, "Failed to get path to temp INI file");

                hr = PathGetDirectory(sczTempIniFilePath, &sczTempIniFileDir);
                NativeAssert::Succeeded(hr, "Failed to get directory to temp INI file");

                hr = DirEnsureDelete(sczTempIniFileDir, TRUE, TRUE);
                if (E_PATHNOTFOUND == hr)
                {
                    hr = S_OK;
                }
                NativeAssert::Succeeded(hr, "Failed to delete IniUtilTest directory: {0}", sczTempIniFileDir);

                hr = DirEnsureExists(sczTempIniFileDir, NULL);
                NativeAssert::Succeeded(hr, "Failed to ensure temp directory exists: {0}", sczTempIniFileDir);

                // Tests parsing, then modifying a regular INI file
                TestReadThenWrite(sczTempIniFilePath, StandardIniFormat, wzIniContents);

                // Tests programmatically creating from scratch, then parsing an INI file
                TestWriteThenRead(sczTempIniFilePath, StandardIniFormat);

                // Tests parsing, then modifying a regular INI file
                TestReadThenWrite(sczTempIniFilePath, ScriptFormat, wzScriptContents);

                // Tests programmatically creating from scratch, then parsing an INI file
                TestWriteThenRead(sczTempIniFilePath, ScriptFormat);
            }
            finally
            {
                ReleaseStr(sczTempIniFilePath);
                ReleaseStr(sczTempIniFileDir);
            }
        }

    private:
        void AssertValue(INI_HANDLE iniHandle, LPCWSTR wzValueName, LPCWSTR wzValue)
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;

            try
            {
                hr = IniGetValue(iniHandle, wzValueName, &sczValue);
                NativeAssert::Succeeded(hr, "Failed to get ini value: {0}", wzValueName);

                if (0 != wcscmp(sczValue, wzValue))
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "Expected to find value in INI: '%ls'='%ls' - but found value '%ls' instead", wzValueName, wzValue, sczValue);
                }
            }
            finally
            {
                ReleaseStr(sczValue);
            }

        LExit:
            return;
        }

        void AssertNoValue(INI_HANDLE iniHandle, LPCWSTR wzValueName)
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;

            try
            {
                hr = IniGetValue(iniHandle, wzValueName, &sczValue);
                if (E_NOTFOUND != hr)
                {
                    if (SUCCEEDED(hr))
                    {
                        hr = E_FAIL;
                    }
                    ExitOnFailure(hr, "INI value shouldn't have been found: %ls", wzValueName);
                }
            }
            finally
            {
                ReleaseStr(sczValue);
            }

        LExit:
            return;
        }

        static HRESULT StandardIniFormat(__inout INI_HANDLE iniHandle)
        {
            HRESULT hr = S_OK;

            hr = IniSetOpenTag(iniHandle, L"[", L"]");
            NativeAssert::Succeeded(hr, "Failed to set open tag settings on ini handle");

            hr = IniSetValueStyle(iniHandle, NULL, L"=");
            NativeAssert::Succeeded(hr, "Failed to set value separator setting on ini handle");

            hr = IniSetCommentStyle(iniHandle, L";");
            NativeAssert::Succeeded(hr, "Failed to set comment style setting on ini handle");

            return hr;
        }

        static HRESULT ScriptFormat(__inout INI_HANDLE iniHandle)
        {
            HRESULT hr = S_OK;

            hr = IniSetValueStyle(iniHandle, L"setf ~", L" ");
            NativeAssert::Succeeded(hr, "Failed to set value separator setting on ini handle");

            return hr;
        }

        void TestReadThenWrite(LPWSTR wzIniFilePath, IniFormatParameters SetFormat, LPCWSTR wzContents)
        {
            HRESULT hr = S_OK;
            INI_HANDLE iniHandle = NULL;
            INI_HANDLE iniHandle2 = NULL;
            INI_VALUE *rgValues = NULL;
            DWORD cValues = 0;

            try
            {
                hr = FileWrite(wzIniFilePath, 0, reinterpret_cast<LPCBYTE>(wzContents), lstrlenW(wzContents) * sizeof(WCHAR), NULL);
                NativeAssert::Succeeded(hr, "Failed to write out INI file");

                hr = IniInitialize(&iniHandle);
                NativeAssert::Succeeded(hr, "Failed to initialize INI object");

                hr = SetFormat(iniHandle);
                NativeAssert::Succeeded(hr, "Failed to set parameters for INI file");

                hr = IniParse(iniHandle, wzIniFilePath, NULL);
                NativeAssert::Succeeded(hr, "Failed to parse INI file");

                hr = IniGetValueList(iniHandle, &rgValues, &cValues);
                NativeAssert::Succeeded(hr, "Failed to get list of values in INI");

                NativeAssert::Equal<DWORD>(5, cValues);

                AssertValue(iniHandle, L"PlainValue", L"Blah");
                AssertNoValue(iniHandle, L"PlainValue2");
                AssertValue(iniHandle, L"Section1\\Section1ValueA", L"Foo");
                AssertValue(iniHandle, L"Section1\\Section1ValueB", L"Bar");
                AssertValue(iniHandle, L"Section2\\Section2ValueA", L"Cha");
                AssertNoValue(iniHandle, L"Section1\\ValueDoesntExist");
                AssertValue(iniHandle, L"Section2\\Array[0]", L"Arr");

                hr = IniSetValue(iniHandle, L"PlainValue2", L"Blah2");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Section1\\CreatedValue", L"Woo");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Section2\\Array[0]", L"Arrmod");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniGetValueList(iniHandle, &rgValues, &cValues);
                NativeAssert::Succeeded(hr, "Failed to get list of values in INI");

                NativeAssert::Equal<DWORD>(7, cValues);

                AssertValue(iniHandle, L"PlainValue", L"Blah");
                AssertValue(iniHandle, L"PlainValue2", L"Blah2");
                AssertValue(iniHandle, L"Section1\\Section1ValueA", L"Foo");
                AssertValue(iniHandle, L"Section1\\Section1ValueB", L"Bar");
                AssertValue(iniHandle, L"Section2\\Section2ValueA", L"Cha");
                AssertNoValue(iniHandle, L"Section1\\ValueDoesntExist");
                AssertValue(iniHandle, L"Section1\\CreatedValue", L"Woo");
                AssertValue(iniHandle, L"Section2\\Array[0]", L"Arrmod");

                // Try deleting a value as well
                hr = IniSetValue(iniHandle, L"Section1\\Section1ValueB", NULL);
                NativeAssert::Succeeded(hr, "Failed to kill value in INI");

                hr = IniWriteFile(iniHandle, NULL, FILE_ENCODING_UNSPECIFIED);
                NativeAssert::Succeeded(hr, "Failed to write ini file back out to disk");

                ReleaseNullIni(iniHandle);
                // Now re-parse the INI we just wrote and make sure it matches the values we expect
                hr = IniInitialize(&iniHandle2);
                NativeAssert::Succeeded(hr, "Failed to initialize INI object");

                hr = SetFormat(iniHandle2);
                NativeAssert::Succeeded(hr, "Failed to set parameters for INI file");

                hr = IniParse(iniHandle2, wzIniFilePath, NULL);
                NativeAssert::Succeeded(hr, "Failed to parse INI file");

                hr = IniGetValueList(iniHandle2, &rgValues, &cValues);
                NativeAssert::Succeeded(hr, "Failed to get list of values in INI");

                NativeAssert::Equal<DWORD>(6, cValues);

                AssertValue(iniHandle2, L"PlainValue", L"Blah");
                AssertValue(iniHandle2, L"PlainValue2", L"Blah2");
                AssertValue(iniHandle2, L"Section1\\Section1ValueA", L"Foo");
                AssertNoValue(iniHandle2, L"Section1\\Section1ValueB");
                AssertValue(iniHandle2, L"Section2\\Section2ValueA", L"Cha");
                AssertNoValue(iniHandle2, L"Section1\\ValueDoesntExist");
                AssertValue(iniHandle2, L"Section1\\CreatedValue", L"Woo");
                AssertValue(iniHandle2, L"Section2\\Array[0]", L"Arrmod");
            }
            finally
            {
                ReleaseIni(iniHandle);
                ReleaseIni(iniHandle2);
            }
        }

        void TestWriteThenRead(LPWSTR wzIniFilePath, IniFormatParameters SetFormat)
        {
            HRESULT hr = S_OK;
            INI_HANDLE iniHandle = NULL;
            INI_HANDLE iniHandle2 = NULL;
            INI_VALUE *rgValues = NULL;
            DWORD cValues = 0;

            try
            {
                hr = FileEnsureDelete(wzIniFilePath);
                NativeAssert::Succeeded(hr, "Failed to ensure file is deleted");

                hr = IniInitialize(&iniHandle);
                NativeAssert::Succeeded(hr, "Failed to initialize INI object");

                hr = SetFormat(iniHandle);
                NativeAssert::Succeeded(hr, "Failed to set parameters for INI file");

                hr = IniGetValueList(iniHandle, &rgValues, &cValues);
                NativeAssert::Succeeded(hr, "Failed to get list of values in INI");
                
                NativeAssert::Equal<DWORD>(0, cValues);

                hr = IniSetValue(iniHandle, L"Value1", L"BlahTypo");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Value2", L"Blah2");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Section1\\Value1", L"Section1Value1");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Section1\\Value2", L"Section1Value2");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Section2\\Value1", L"Section2Value1");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Section2\\Array[0]", L"Arr");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Value3", L"Blah3");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Value4", L"Blah4");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Value4", NULL);
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniSetValue(iniHandle, L"Value1", L"Blah1");
                NativeAssert::Succeeded(hr, "Failed to set value in INI");

                hr = IniGetValueList(iniHandle, &rgValues, &cValues);
                NativeAssert::Succeeded(hr, "Failed to get list of values in INI");

                NativeAssert::Equal<DWORD>(8, cValues);

                AssertValue(iniHandle, L"Value1", L"Blah1");
                AssertValue(iniHandle, L"Value2", L"Blah2");
                AssertValue(iniHandle, L"Value3", L"Blah3");
                AssertNoValue(iniHandle, L"Value4");
                AssertValue(iniHandle, L"Section1\\Value1", L"Section1Value1");
                AssertValue(iniHandle, L"Section1\\Value2", L"Section1Value2");
                AssertValue(iniHandle, L"Section2\\Value1", L"Section2Value1");
                AssertValue(iniHandle, L"Section2\\Array[0]", L"Arr");

                hr = IniWriteFile(iniHandle, wzIniFilePath, FILE_ENCODING_UNSPECIFIED);
                NativeAssert::Succeeded(hr, "Failed to write ini file back out to disk");

                ReleaseNullIni(iniHandle);
                // Now re-parse the INI we just wrote and make sure it matches the values we expect
                hr = IniInitialize(&iniHandle2);
                NativeAssert::Succeeded(hr, "Failed to initialize INI object");

                hr = SetFormat(iniHandle2);
                NativeAssert::Succeeded(hr, "Failed to set parameters for INI file");

                hr = IniParse(iniHandle2, wzIniFilePath, NULL);
                NativeAssert::Succeeded(hr, "Failed to parse INI file");

                hr = IniGetValueList(iniHandle2, &rgValues, &cValues);
                NativeAssert::Succeeded(hr, "Failed to get list of values in INI");

                NativeAssert::Equal<DWORD>(7, cValues);

                AssertValue(iniHandle2, L"Value1", L"Blah1");
                AssertValue(iniHandle2, L"Value2", L"Blah2");
                AssertValue(iniHandle2, L"Value3", L"Blah3");
                AssertNoValue(iniHandle2, L"Value4");
                AssertValue(iniHandle2, L"Section1\\Value1", L"Section1Value1");
                AssertValue(iniHandle2, L"Section1\\Value2", L"Section1Value2");
                AssertValue(iniHandle2, L"Section2\\Value1", L"Section2Value1");
                AssertValue(iniHandle2, L"Section2\\Array[0]", L"Arr");
            }
            finally
            {
                ReleaseIni(iniHandle);
                ReleaseIni(iniHandle2);
            }
        }
    };
}
