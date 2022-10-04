// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using WixToolset.Data;

    /// <summary>
    /// Interface used to track all files processed.
    /// </summary>
    public interface ITrackedFile
    {
        /// <summary>
        /// Path to tracked file.
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Optional source line numbers where the tracked file was created.
        /// </summary>
        SourceLineNumber SourceLineNumbers { get; set; }

        /// <summary>
        /// Type of tracked file.
        /// </summary>
        TrackedFileType Type { get; set; }
    }
}
