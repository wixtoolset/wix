// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// contains the file hash information returned by MsiGetFileHash and used in the MsiFileHash table.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public class MSIFILEHASHINFO
    {
        [FieldOffset(0)] public uint FileHashInfoSize;
        [FieldOffset(4)] public int Data0;
        [FieldOffset(8)] public int Data1;
        [FieldOffset(12)] public int Data2;
        [FieldOffset(16)] public int Data3;
    }
}
