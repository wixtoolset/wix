#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest
{
namespace MbaHost
{
namespace Native
{
    using namespace System;
    using namespace WixToolset::BootstrapperCore;

    public ref class TestManagedBootstrapperApplicationFactory : public BaseBootstrapperApplicationFactory
    {
    protected:
        virtual IBootstrapperApplication^ Create(IEngine^ engine, IBootstrapperCommand^ /*command*/) override
        {
            return gcnew TestManagedBootstrapperApplication(engine);
        }
    };

    [assembly:BootstrapperApplicationFactory(TestManagedBootstrapperApplicationFactory::typeid)];
}
}
}