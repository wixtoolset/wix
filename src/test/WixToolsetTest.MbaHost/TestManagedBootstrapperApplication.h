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
    using namespace WixToolset::Mba::Core;

    public ref class TestManagedBootstrapperApplication : BootstrapperApplication
    {
    public:
        TestManagedBootstrapperApplication(WixToolset::Mba::Core::IEngine^ engine)
            : BootstrapperApplication(engine)
        {

        }

        virtual void Run() override
        {
        }

        virtual void OnShutdown(ShutdownEventArgs^ e) override
        {
            String^ message = "Shutdown," + e->Action.ToString() + "," + e->HResult.ToString();
            this->engine->Log(LogLevel::Standard, message);
        }
    };
}
}
}