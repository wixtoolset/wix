// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;

namespace DutilTests
{
    public ref class LocUtil
    {
    public:
        [Fact]
        void CanLoadStringsWxl()
        {
            HRESULT hr = S_OK;
            WIX_LOCALIZATION* pLoc = NULL;
            LOC_STRING* pLocString = NULL;
            LPWSTR sczValue = NULL;

            DutilInitialize(&DutilTestTraceError);

            try
            {
                hr = XmlInitialize();
                NativeAssert::Succeeded(hr, "Failed to initialize Xml.");

                pin_ptr<const wchar_t> wxlFilePath = PtrToStringChars(TestData::Get("TestData", "LocUtilTests", "strings.wxl"));
                hr = LocLoadFromFile(wxlFilePath, &pLoc);
                NativeAssert::Succeeded(hr, "Failed to parse strings.wxl: {0}", wxlFilePath);

                Assert::Equal(4ul, pLoc->cLocStrings);

                hr = LocGetString(pLoc, L"#(loc.Ex1)", &pLocString);
                NativeAssert::Succeeded(hr, "Failed to get loc string 'Ex1' from: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"#(loc.Ex1)", pLocString->wzId);
                NativeAssert::StringEqual(L"This is example #1", pLocString->wzText);
                NativeAssert::True(pLocString->bOverridable);

                hr = LocGetString(pLoc, L"#(loc.Ex2)", &pLocString);
                NativeAssert::Succeeded(hr, "Failed to get loc string 'Ex2' from: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"#(loc.Ex2)", pLocString->wzId);
                NativeAssert::StringEqual(L"This is example #2", pLocString->wzText);
                NativeAssert::False(pLocString->bOverridable);

                hr = LocGetString(pLoc, L"#(loc.Ex3)", &pLocString);
                NativeAssert::Succeeded(hr, "Failed to get loc string 'Ex3' from: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"#(loc.Ex3)", pLocString->wzId);
                NativeAssert::StringEqual(L"This is example #3", pLocString->wzText);
                NativeAssert::False(pLocString->bOverridable);

                hr = LocGetString(pLoc, L"#(loc.Ex4)", &pLocString);
                NativeAssert::Succeeded(hr, "Failed to get loc string 'Ex4' from: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"#(loc.Ex4)", pLocString->wzId);
                NativeAssert::StringEqual(L"", pLocString->wzText);
                NativeAssert::False(pLocString->bOverridable);

                hr = StrAllocString(&sczValue, L"Before #(loc.Ex1) After", 0);
                NativeAssert::Succeeded(hr, "Failed to create localizable Ex1 string");

                hr = LocLocalizeString(pLoc, &sczValue);
                NativeAssert::Succeeded(hr, "Failed to localize Ex1 string using: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"Before This is example #1 After", sczValue);

                hr = StrAllocString(&sczValue, L"Xxx#(loc.Ex3)yyY", 0);
                NativeAssert::Succeeded(hr, "Failed to create localizable Ex3 string");

                hr = LocLocalizeString(pLoc, &sczValue);
                NativeAssert::Succeeded(hr, "Failed to localize Ex3 string using: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"XxxThis is example #3yyY", sczValue);

                hr = StrAllocString(&sczValue, L"aaa#(loc.Ex4)bbb", 0);
                NativeAssert::Succeeded(hr, "Failed to create localizable Ex4 string");

                hr = LocLocalizeString(pLoc, &sczValue);
                NativeAssert::Succeeded(hr, "Failed to localize Ex4 string using: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"aaabbb", sczValue);
            }
            finally
            {
                ReleaseStr(sczValue);

                if (pLoc)
                {
                    LocFree(pLoc);
                }

                DutilUninitialize();
            }
        }

        [Fact]
        void CanLoadControlsWxl()
        {
            HRESULT hr = S_OK;
            WIX_LOCALIZATION* pLoc = NULL;
            LOC_CONTROL* pLocControl = NULL;

            DutilInitialize(&DutilTestTraceError);

            try
            {
                hr = XmlInitialize();
                NativeAssert::Succeeded(hr, "Failed to initialize Xml.");

                pin_ptr<const wchar_t> wxlFilePath = PtrToStringChars(TestData::Get("TestData", "LocUtilTests", "controls.wxl"));
                hr = LocLoadFromFile(wxlFilePath, &pLoc);
                NativeAssert::Succeeded(hr, "Failed to parse controls.wxl: {0}", wxlFilePath);

                Assert::Equal(3ul, pLoc->cLocControls);

                hr = LocGetControl(pLoc, L"Control1", &pLocControl);
                NativeAssert::Succeeded(hr, "Failed to get loc control 'Control1' from: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"Control1", pLocControl->wzControl);
                NativeAssert::Equal(1, pLocControl->nX);
                NativeAssert::Equal(2, pLocControl->nY);
                NativeAssert::Equal(LOC_CONTROL_NOT_SET, pLocControl->nWidth);
                NativeAssert::Equal(LOC_CONTROL_NOT_SET, pLocControl->nHeight);
                NativeAssert::StringEqual(L"This is control #1", pLocControl->wzText);

                hr = LocGetControl(pLoc, L"Control2", &pLocControl);
                NativeAssert::Succeeded(hr, "Failed to get loc control 'Control2' from: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"Control2", pLocControl->wzControl);
                NativeAssert::Equal(LOC_CONTROL_NOT_SET, pLocControl->nX);
                NativeAssert::Equal(LOC_CONTROL_NOT_SET, pLocControl->nY);
                NativeAssert::Equal(50, pLocControl->nWidth);
                NativeAssert::Equal(LOC_CONTROL_NOT_SET, pLocControl->nHeight);
                NativeAssert::StringEqual(L"This is control #2", pLocControl->wzText);

                hr = LocGetControl(pLoc, L"Control3", &pLocControl);
                NativeAssert::Succeeded(hr, "Failed to get loc control 'Control3' from: {0}", wxlFilePath);
                NativeAssert::StringEqual(L"Control3", pLocControl->wzControl);
                NativeAssert::Equal(LOC_CONTROL_NOT_SET, pLocControl->nX);
                NativeAssert::Equal(LOC_CONTROL_NOT_SET, pLocControl->nY);
                NativeAssert::Equal(LOC_CONTROL_NOT_SET, pLocControl->nWidth);
                NativeAssert::Equal(150, pLocControl->nHeight);
                NativeAssert::StringEqual(L"", pLocControl->wzText);
            }
            finally
            {
                if (pLoc)
                {
                    LocFree(pLoc);
                }

                DutilUninitialize();
            }
        }
    };
}
