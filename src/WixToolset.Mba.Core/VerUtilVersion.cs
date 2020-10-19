// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;

    public sealed class VerUtilVersion : IDisposable
    {
        internal VerUtilVersion(VerUtil.VersionHandle handle)
        {
            this.Handle = handle;

            var pVersion = handle.DangerousGetHandle();
            var version = (VerUtil.VersionStruct)Marshal.PtrToStructure(pVersion, typeof(VerUtil.VersionStruct));
            this.Version = Marshal.PtrToStringUni(version.sczVersion);
            this.Major = version.dwMajor;
            this.Minor = version.dwMinor;
            this.Patch = version.dwPatch;
            this.Revision = version.dwRevision;
            this.ReleaseLabels = new VerUtilVersionReleaseLabel[version.cReleaseLabels];
            this.Metadata = VerUtil.VersionStringFromOffset(version.sczVersion, version.cchMetadataOffset);
            this.IsInvalid = version.fInvalid;

            for (var i = 0; i < version.cReleaseLabels; ++i)
            {
                var offset = i * Marshal.SizeOf(typeof(VerUtil.VersionReleaseLabelStruct));
                var pReleaseLabel = new IntPtr(version.rgReleaseLabels.ToInt64() + offset);
                this.ReleaseLabels[i] = new VerUtilVersionReleaseLabel(pReleaseLabel, version.sczVersion);
            }
        }

        public string Version { get; private set; }
        public uint Major { get; private set; }
        public uint Minor { get; private set; }
        public uint Patch { get; private set; }
        public uint Revision { get; private set; }
        public VerUtilVersionReleaseLabel[] ReleaseLabels { get; private set; }
        public string Metadata { get; private set; }
        public bool IsInvalid { get; private set; }

        public void Dispose()
        {
            if (this.Handle != null)
            {
                this.Handle.Dispose();
                this.Handle = null;
            }
        }

        private VerUtil.VersionHandle Handle { get; set; }

        internal VerUtil.VersionHandle GetHandle()
        {
            if (this.Handle == null)
            {
                throw new ObjectDisposedException(this.Version);
            }

            return this.Handle;
        }
    }
}
