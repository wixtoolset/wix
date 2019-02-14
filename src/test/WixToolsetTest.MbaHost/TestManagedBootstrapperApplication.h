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

    public ref class TestManagedBootstrapperApplication : BootstrapperApplication
    {
    public:
        virtual void Run() override
        {
        }

        virtual void OnShutdown(ShutdownEventArgs^ e) override
        {
            String^ message = "Shutdown," + e->Action.ToString() + "," + e->HResult.ToString();
            this->Engine->Log(LogLevel::Standard, message);
        }
    };

    [assembly:BootstrapperApplication(TestManagedBootstrapperApplication::typeid)];
}
}
}