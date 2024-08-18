// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System;
    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices;

    /// <summary>
    /// This is no longer used.
    /// </summary>
    [Obsolete("Bootstrapper applications now run out of proc and do not use a BootstrapperApplicationFactory. Remove your BootstrapperApplicationFactory class. See https://wixtoolset.org/docs/fivefour/ for more details.")]
    public interface IBootstrapperApplicationFactory
    {
        /// <summary>
        /// This is no longer used.
        /// </summary>
        /// <param name="pArgs">This is no longer used.</param>
        /// <param name="pResults">This is no longer used.</param>
        void Create(IntPtr pArgs, IntPtr pResults);
    }
}
