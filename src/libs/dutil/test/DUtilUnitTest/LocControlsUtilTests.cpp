// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixInternal::TestSupport;

namespace DutilTests
{
    public ref class LocControlsUtil
    {
    public:
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
