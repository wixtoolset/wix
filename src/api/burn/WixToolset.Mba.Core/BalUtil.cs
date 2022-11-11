// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;

    internal static class BalUtil
    {
        [DllImport("mbanative.dll", ExactSpelling = true)]
        internal static extern int BalEscapeStringFromEngine(
            [MarshalAs(UnmanagedType.Interface)] IBootstrapperEngine pEngine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzIn,
            ref StrUtil.StrHandle psczOut
            );

        [DllImport("mbanative.dll", ExactSpelling = true)]
        internal static extern int BalFormatStringFromEngine(
            [MarshalAs(UnmanagedType.Interface)] IBootstrapperEngine pEngine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzFormat,
            ref StrUtil.StrHandle psczOut
            );

        [DllImport("mbanative.dll", ExactSpelling = true)]
        internal static extern int BalGetStringVariableFromEngine(
            [MarshalAs(UnmanagedType.Interface)] IBootstrapperEngine pEngine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            ref StrUtil.StrHandle psczOut
            );

        [DllImport("mbanative.dll", ExactSpelling = true)]
        internal static extern int BalGetVersionVariableFromEngine(
            [MarshalAs(UnmanagedType.Interface)] IBootstrapperEngine pEngine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            ref StrUtil.StrHandle psczOut
            );

        [DllImport("mbanative.dll", ExactSpelling = true)]
        internal static extern int BalGetRelatedBundleVariableFromEngine(
            [MarshalAs(UnmanagedType.Interface)] IBootstrapperEngine pEngine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            ref StrUtil.StrHandle psczOut
            );

        [DllImport("mbanative.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BalVariableExistsFromEngine(
            [MarshalAs(UnmanagedType.Interface)] IBootstrapperEngine pEngine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable
            );
    }
}
