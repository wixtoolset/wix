// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Host
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Contains native constants, functions, and structures for this assembly.
    /// </summary>
    internal static class NativeMethods
    {
        #region Error Constants
        internal const int E_NOTFOUND = unchecked((int)0x80070490);
        internal const int E_UNEXPECTED = unchecked((int)0x8000ffff);
        #endregion
    }
}
