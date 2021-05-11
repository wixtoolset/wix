// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System.IO;

    /// <summary>
    /// Interface for BootstrapperApplicationData.xml.
    /// </summary>
    public interface IBootstrapperApplicationData
    {
        /// <summary>
        /// The BootstrapperApplicationData.xml file.
        /// </summary>
        FileInfo BADataFile { get; }

        /// <summary>
        /// The BA manifest.
        /// </summary>
        IBundleInfo Bundle { get; }
    }
}