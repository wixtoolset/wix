// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Managed wrapper for verutil.
    /// </summary>
    public static class VerUtil
    {
        [DllImport("mbanative.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern int VerCompareParsedVersions(
            VersionHandle pVersion1,
            VersionHandle pVersion2
            );

        [DllImport("mbanative.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern int VerCompareStringVersions(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion1,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion2,
            [MarshalAs(UnmanagedType.Bool)] bool fStrict
            );

        [DllImport("mbanative.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern VersionHandle VerCopyVersion(
            VersionHandle pSource
            );

        [DllImport("mbanative.dll", ExactSpelling = true)]
        internal static extern void VerFreeVersion(
            IntPtr pVersion
            );

        [DllImport("mbanative.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern VersionHandle VerParseVersion(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion,
            [MarshalAs(UnmanagedType.U4)] uint cchValue,
            [MarshalAs(UnmanagedType.Bool)] bool fStrict
            );

        [DllImport("mbanative.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern VersionHandle VerVersionFromQword(
            [MarshalAs(UnmanagedType.I8)] long qwVersion
            );

        [StructLayout(LayoutKind.Sequential)]
        internal struct VersionReleaseLabelStruct
        {
            public bool fNumeric;
            public uint dwValue;
            public IntPtr cchLabelOffset;
            public int cchLabel;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct VersionStruct
        {
            public IntPtr sczVersion;
            public uint dwMajor;
            public uint dwMinor;
            public uint dwPatch;
            public uint dwRevision;
            public int cReleaseLabels;
            public IntPtr rgReleaseLabels;
            public IntPtr cchMetadataOffset;
            public bool fInvalid;
        }

        internal static string VersionStringFromOffset(IntPtr wzVersion, IntPtr cchOffset, int? cchLength = null)
        {
            var offset = cchOffset.ToInt64() * UnicodeEncoding.CharSize;
            var wz = new IntPtr(wzVersion.ToInt64() + offset);
            if (cchLength.HasValue)
            {
                return Marshal.PtrToStringUni(wz, (int)cchLength);
            }
            else
            {
                return Marshal.PtrToStringUni(wz);
            }
        }

        internal sealed class VersionHandle : SafeHandle
        {
            public VersionHandle() : base(IntPtr.Zero, true) { }

            public override bool IsInvalid => false;

            protected override bool ReleaseHandle()
            {
                VerFreeVersion(this.handle);
                return true;
            }
        }

        /// <returns>0 if equal, 1 if version1 &gt; version2, -1 if version1 &lt; version2</returns>
        public static int CompareParsedVersions(VerUtilVersion version1, VerUtilVersion version2)
        {
            return VerCompareParsedVersions(version1.GetHandle(), version2.GetHandle());
        }

        /// <returns>0 if equal, 1 if version1 &gt; version2, -1 if version1 &lt; version2</returns>
        public static int CompareStringVersions(string version1, string version2, bool strict)
        {
            return VerCompareStringVersions(version1, version2, strict);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static VerUtilVersion CopyVersion(VerUtilVersion version)
        {
            var handle = VerCopyVersion(version.GetHandle());
            return new VerUtilVersion(handle);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        /// <param name="strict">Whether to throw exception on invalid version.</param>
        /// <returns></returns>
        public static VerUtilVersion ParseVersion(string version, bool strict)
        {
            var handle = VerParseVersion(version, 0, strict);
            return new VerUtilVersion(handle);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static VerUtilVersion VersionFromQword(long version)
        {
            var handle = VerVersionFromQword(version);
            return new VerUtilVersion(handle);
        }
    }
}
