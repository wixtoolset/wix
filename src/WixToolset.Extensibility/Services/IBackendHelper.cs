// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface provided to help backend extensions.
    /// </summary>
    public interface IBackendHelper
    {
        /// <summary>
        /// Creates a file transfer and marks it redundant if the source and destination are identical.
        /// </summary>
        /// <param name="source">Source for the file transfer.</param>
        /// <param name="destination">Destination for the file transfer.</param>
        /// <param name="move">Indicates whether to move or copy the source file.</param>
        IFileTransfer CreateFileTransfer(string source, string destination, bool move, SourceLineNumber sourceLineNumbers = null);

        /// <summary>
        /// Creates a version 3 name-based UUID.
        /// </summary>
        /// <param name="namespaceGuid">The namespace UUID.</param>
        /// <param name="value">The value.</param>
        /// <returns>The generated GUID for the given namespace and value.</returns>
        string CreateGuid(Guid namespaceGuid, string value);

        /// <summary>
        /// Creates a resolved directory.
        /// </summary>
        /// <param name="directoryParent">Directory parent identifier.</param>
        /// <param name="name">Name of directory.</param>
        /// <returns>Resolved directory.</returns>
        IResolvedDirectory CreateResolvedDirectory(string directoryParent, string name);

        /// <summary>
        /// Validates path is relative and canonicalizes it.
        /// For example, "a\..\c\.\d.exe" => "c\d.exe".
        /// </summary>
        /// <param name="sourceLineNumbers"></param>
        /// <param name="elementName"></param>
        /// <param name="attributeName"></param>
        /// <param name="relativePath"></param>
        /// <returns>The original value if not relative, otherwise the canonicalized relative path.</returns>
        string GetCanonicalRelativePath(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string relativePath);

        /// <summary>
        /// Creates a tracked file.
        /// </summary>
        /// <param name="path">Destination path for the build output.</param>
        /// <param name="type">Type of tracked file to create.</param>
        /// <param name="sourceLineNumbers">Optional source line numbers that requested the tracked file.</param>
        ITrackedFile TrackFile(string path, TrackedFileType type, SourceLineNumber sourceLineNumbers = null);
    }
}
