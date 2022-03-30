// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;

    /// <summary>
    /// The result from decompiling a Windows Installer database.
    /// </summary>
    public interface IWindowsInstallerDecompileResult
    {
        /// <summary>
        /// Decompiled document.
        /// </summary>
        XDocument Document { get; set; }

        /// <summary>
        /// Extracted paths.
        /// </summary>
        IList<string> ExtractedFilePaths { get; set; }

        /// <summary>
        /// Decompiled platform.
        /// </summary>
        Platform? Platform { get; set; }
    }
}
