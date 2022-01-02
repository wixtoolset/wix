// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface provided to track files for use by layout later.
    /// </summary>
    public interface ILayoutServices
    {
        /// <summary>
        /// Creates a file transfer and marks it redundant if the source and destination are identical.
        /// </summary>
        /// <param name="source">Source for the file transfer.</param>
        /// <param name="destination">Destination for the file transfer.</param>
        /// <param name="move">Indicates whether to move or copy the source file.</param>
        /// <param name="sourceLineNumbers">Optional source line numbers that requested the file transfer.</param>
        IFileTransfer CreateFileTransfer(string source, string destination, bool move, SourceLineNumber sourceLineNumbers = null);

        /// <summary>
        /// Creates a tracked file.
        /// </summary>
        /// <param name="path">Destination path for the build output.</param>
        /// <param name="type">Type of tracked file to create.</param>
        /// <param name="sourceLineNumbers">Optional source line numbers that requested the tracked file.</param>
        ITrackedFile TrackFile(string path, TrackedFileType type, SourceLineNumber sourceLineNumbers = null);
    }
}
