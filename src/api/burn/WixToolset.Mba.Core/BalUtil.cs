// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;

    internal static class BalUtil
    {
        [DllImport("mbanative.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern IBootstrapperEngine InitializeFromCreateArgs(
            IntPtr pArgs,
            ref Command pCommand
            );

        [DllImport("mbanative.dll", ExactSpelling = true)]
        internal static extern void StoreBAInCreateResults(
            IntPtr pResults,
            [MarshalAs(UnmanagedType.Interface)] IBootstrapperApplication pBA
            );
    }
}
