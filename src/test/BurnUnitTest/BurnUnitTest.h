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
    using namespace WixTest;
    using namespace Xunit;

    public ref class BurnUnitTest : WixTestBase, IUseFixture<BurnTestFixture^>
    {
    public:
        BurnUnitTest()
        {
        }

        virtual void TestInitialize() override
        {
            WixTestBase::TestInitialize();

            HRESULT hr = S_OK;

            LogInitialize(::GetModuleHandleW(NULL));

            hr = LogOpen(NULL, L"BurnUnitTest", NULL, L"txt", FALSE, FALSE, NULL);
            TestThrowOnFailure(hr, L"Failed to open log.");
        }

        virtual void TestUninitialize() override
        {
            LogUninitialize(FALSE);

            WixTestBase::TestUninitialize();
        }

        virtual void SetFixture(BurnTestFixture^ fixture)
        {
            // Don't care about the fixture, just need it to be created and disposed.
            UNREFERENCED_PARAMETER(fixture);
        }
    }; 
}
}
}
}
}
