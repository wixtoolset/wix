#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal
{
namespace TestSupport
{
    using namespace System;

    public ref class TestRegistryFixture : IDisposable
    {
    private:
        String^ rootPath;
    public:
        TestRegistryFixture();

        ~TestRegistryFixture();

        void SetUp();

        void TearDown();

        String^ GetDirectHkcuPath(REG_KEY_BITNESS bitness, ... array<String^>^ paths);

        String^ GetDirectHklmPath(REG_KEY_BITNESS bitness, ... array<String^>^ paths);
    };
}
}
