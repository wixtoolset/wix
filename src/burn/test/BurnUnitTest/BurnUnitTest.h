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
    using namespace Xunit;

    [CollectionDefinition("Burn")]
    public ref class BurnCollectionDefinition : ICollectionFixture<BurnTestFixture^>
    {

    };

    [Collection("Burn")]
    public ref class BurnUnitTest
    {
    public:
        BurnUnitTest(BurnTestFixture^ fixture)
        {
            this->testContext = fixture;
        }

        property BurnTestFixture^ TestContext
        {
            BurnTestFixture^ get()
            {
                return this->testContext;
            }
        }

    private:
        BurnTestFixture^ testContext;
    }; 
}
}
}
}
}
