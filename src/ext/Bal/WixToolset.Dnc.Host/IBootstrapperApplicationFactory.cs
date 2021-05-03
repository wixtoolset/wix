// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dnc.Host
{
    using System;
    using System.Runtime.InteropServices;

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IBootstrapperApplicationFactory
    {
        void Create(
            IntPtr pArgs,
            IntPtr pResults
            );
    }
}
