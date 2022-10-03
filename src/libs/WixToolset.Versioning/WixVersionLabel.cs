// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Versioning
{
    /// <summary>
    /// Label in a <c>WixVersion</c>.
    /// </summary>
    public class WixVersionLabel
    {
        /// <summary>
        /// Creates a string only version label.
        /// </summary>
        /// <param name="label">String value for version label.</param>
        public WixVersionLabel(string label)
        {
            this.Label = label;
        }

        /// <summary>
        /// Creates a string version label with numeric value.
        /// </summary>
        /// <param name="label">String value for version label.</param>
        /// <param name="numeric">Numeric value for the version label.</param>
        public WixVersionLabel(string label, uint? numeric)
        {
            this.Label = label;
            this.Numeric = numeric;
        }

        /// <summary>
        /// Gets the string label value.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets the optional numeric label value.
        /// </summary>
        public uint? Numeric { get; set; }
    }
}
