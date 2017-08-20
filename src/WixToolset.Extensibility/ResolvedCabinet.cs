// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Data returned from build file manager ResolveCabinet callback.
    /// </summary>
    public class ResolvedCabinet
    {
        /// <summary>
        /// Gets or sets the build option for the resolved cabinet.
        /// </summary>
        public CabinetBuildOption BuildOption { get; set; }

        /// <summary>
        /// Gets or sets the path for the resolved cabinet.
        /// </summary>
        public string Path { get; set; }
    }
}
