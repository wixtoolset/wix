// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// An enhanced implementation of SemVer 2.0.
    /// </summary>
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

        /// <summary>
        /// String version, which would have stripped the leading 'v'.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// For version A.B.C.D, Major is A. It is 0 if not specified.
        /// </summary>
        public uint Major { get; private set; }

        /// <summary>
        /// For version A.B.C.D, Minor is B. It is 0 if not specified.
        /// </summary>
        public uint Minor { get; private set; }

        /// <summary>
        /// For version A.B.C.D, Patch is C. It is 0 if not specified.
        /// </summary>
        public uint Patch { get; private set; }

        /// <summary>
        /// For version A.B.C.D, Revision is D. It is 0 if not specified.
        /// </summary>
        public uint Revision { get; private set; }

        /// <summary>
        /// For version X.Y.Z-releaselabels+metadata, ReleaseLabels is the parsed information for releaselabels.
        /// </summary>
        public VerUtilVersionReleaseLabel[] ReleaseLabels { get; private set; }

        /// <summary>
        /// For version X.Y.Z-releaselabels+metadata, Metadata is the rest of the string after +.
        /// For invalid versions, it is all of the string after the point where it was an invalid string.
        /// </summary>
        public string Metadata { get; private set; }

        /// <summary>
        /// Whether the version conformed to the spec.
        /// </summary>
        public bool IsInvalid { get; private set; }

        /// <inheritdoc/>
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
