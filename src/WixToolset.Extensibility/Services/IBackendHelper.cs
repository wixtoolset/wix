// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
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
        /// <param name="destination">Destiation for the file transfer.</param>
        /// <param name="move">Indicates whether to move or copy the source file.</param>
        /// <param name="type">Type of file transfer to create.</param>
        /// <param name="sourceLineNumbers">Optional source line numbers that requested the file transfer.</param>
        IFileTransfer CreateFileTransfer(string source, string destination, bool move, FileTransferType type, SourceLineNumber sourceLineNumbers = null);
    }
}
