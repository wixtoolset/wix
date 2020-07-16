#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{
    using namespace System;
    using namespace WixBuildTools::TestSupport;

    public ref class BurnTestFixture : IDisposable
    {
    public:
        BurnTestFixture()
        {
            HRESULT hr = XmlInitialize();
            TestThrowOnFailure(hr, L"Failed to initialize XML support.");

            hr = RegInitialize();
            TestThrowOnFailure(hr, L"Failed to initialize Regutil.");

            PlatformInitialize();

            this->testDirectory = WixBuildTools::TestSupport::TestData::Get();

            LogInitialize(::GetModuleHandleW(NULL));

            hr = LogOpen(NULL, L"BurnUnitTest", NULL, L"txt", FALSE, FALSE, NULL);
            TestThrowOnFailure(hr, L"Failed to open log.");
        }

        ~BurnTestFixture()
        {
            XmlUninitialize();
            RegUninitialize();
            LogUninitialize(FALSE);
        }

        property String^ DataDirectory
        {
            String^ get()
            {
                return this->testDirectory;
            }
        }

        property String^ TestDirectory
        {
            String^ get()
            {
                return this->testDirectory;
            }
        }

    private:
        String^ testDirectory;
    };
}
}
}
}
}
