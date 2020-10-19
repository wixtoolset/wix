// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;

    public sealed class VerUtilVersionReleaseLabel
    {
        internal VerUtilVersionReleaseLabel(IntPtr pReleaseLabel, IntPtr wzVersion)
        {
            var releaseLabel = (VerUtil.VersionReleaseLabelStruct)Marshal.PtrToStructure(pReleaseLabel, typeof(VerUtil.VersionReleaseLabelStruct));
            this.IsNumeric = releaseLabel.fNumeric;
            this.Value = releaseLabel.dwValue;
            this.Label = VerUtil.VersionStringFromOffset(wzVersion, releaseLabel.cchLabelOffset, releaseLabel.cchLabel);
        }

        public bool IsNumeric { get; private set; }
        public uint Value { get; private set; }
        public string Label { get; private set; }
    }
}
