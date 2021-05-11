// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msm
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Merge merge modules into an MSI file.
    /// </summary>
    [ComImport, Guid("F94985D5-29F9-4743-9805-99BC3F35B678")]
    public class MsmMerge2
    {
    }

    /// <summary>
    /// Defines the standard COM IClassFactory interface.
    /// </summary>
    [ComImport, Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IClassFactory
    {
        /// <summary>
        /// 
        /// </summary>
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object CreateInstance(IntPtr unkOuter, [MarshalAs(UnmanagedType.LPStruct)] Guid iid);
    }

    /// <summary>
    /// Contains native methods for merge operations.
    /// </summary>
    public static class MsmInterop
    {
        [DllImport("mergemod.dll", EntryPoint = "DllGetClassObject", PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.IUnknown)]
        private static extern object MergeModGetClassObject([MarshalAs(UnmanagedType.LPStruct)] Guid clsid, [MarshalAs(UnmanagedType.LPStruct)] Guid iid);

        /// <summary>
        /// Load the merge object directly from a local mergemod.dll without going through COM registration.
        /// </summary>
        /// <returns>Merge interface.</returns>
        public static IMsmMerge2 GetMsmMerge()
        {
            var classFactory = (IClassFactory)MergeModGetClassObject(typeof(MsmMerge2).GUID, typeof(IClassFactory).GUID);
            return (IMsmMerge2)classFactory.CreateInstance(IntPtr.Zero, typeof(IMsmMerge2).GUID);
        }
    }
}
