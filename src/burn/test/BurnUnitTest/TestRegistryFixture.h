#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools
{
namespace TestSupport
{
    using namespace System;

    public ref class TestRegistryFixture : IDisposable
    {
    private:
        String^ rootPath;
        String^ hkcuPath;
        String^ hklmPath;
    public:
        TestRegistryFixture();

        ~TestRegistryFixture();

        void SetUp();

        void TearDown();

        String^ GetDirectHkcuPath(... array<String^>^ paths);

        String^ GetDirectHklmPath(... array<String^>^ paths);
    };
}
}
