// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A release label from a <see cref="VerUtilVersion"/>.
    /// </summary>
    public sealed class VerUtilVersionReleaseLabel
    {
        internal VerUtilVersionReleaseLabel(IntPtr pReleaseLabel, IntPtr wzVersion)
        {
            var releaseLabel = (VerUtil.VersionReleaseLabelStruct)Marshal.PtrToStructure(pReleaseLabel, typeof(VerUtil.VersionReleaseLabelStruct));
            this.IsNumeric = releaseLabel.fNumeric;
            this.Value = releaseLabel.dwValue;
            this.Label = VerUtil.VersionStringFromOffset(wzVersion, releaseLabel.cchLabelOffset, releaseLabel.cchLabel);
        }

        /// <summary>
        /// Whether the label was parsed as a number.
        /// </summary>
        public bool IsNumeric { get; private set; }

        /// <summary>
        /// If <see cref="IsNumeric"/> then the value that was parsed.
        /// </summary>
        public uint Value { get; private set; }

        /// <summary>
        /// The string version of the label.
        /// </summary>
        public string Label { get; private set; }
    }
}
