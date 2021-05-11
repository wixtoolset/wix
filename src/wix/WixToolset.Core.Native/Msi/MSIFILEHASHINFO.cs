// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// contains the file hash information returned by MsiGetFileHash and used in the MsiFileHash table.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public class MSIFILEHASHINFO
    {
        /// <summary>
        /// 
        /// </summary>
        [FieldOffset(0)] public uint FileHashInfoSize;
        /// <summary>
        /// 
        /// </summary>
        [FieldOffset(4)] public int Data0;
        /// <summary>
        /// 
        /// </summary>
        [FieldOffset(8)] public int Data1;
        /// <summary>
        /// 
        /// </summary>
        [FieldOffset(12)] public int Data2;
        /// <summary>
        /// 
        /// </summary>
        [FieldOffset(16)] public int Data3;
    }
}
