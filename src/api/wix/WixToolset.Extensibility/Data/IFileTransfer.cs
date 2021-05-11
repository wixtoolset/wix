// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using WixToolset.Data;

    /// <summary>
    /// Structure used for all file transfer information.
    /// </summary>
    public interface IFileTransfer
    {
        /// <summary>Destination path for file.</summary>
        string Destination { get; set; }

        /// <summary>Flag if file should be moved (optimal).</summary>
        bool Move { get; set; }

        /// <summary>Set during layout of media when the file transfer when the source and target resolve to the same path.</summary>
        bool Redundant { get; set; }

        /// <summary>Source path to file.</summary>
        string Source { get; set; }

        /// <summary>Optional source line numbers where this file transfer orginated.</summary>
        SourceLineNumber SourceLineNumbers { get; set; }
    }
}
