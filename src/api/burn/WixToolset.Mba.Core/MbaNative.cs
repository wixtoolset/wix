// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;

    internal static class MbaNative
    {
        [DllImport("mbanative.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern void BootstrapperApplicationDebuggerCheck();

        [DllImport("mbanative.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern void BootstrapperApplicationRun(
            [MarshalAs(UnmanagedType.Interface)] IBootstrapperApplication pBA
            );
    }
}
