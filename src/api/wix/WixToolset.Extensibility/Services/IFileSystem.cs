// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;
    using System.IO;

    /// <summary>
    /// Abstracts basic file system operations.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Copies a file.
        /// </summary>
        /// <param name="source">The file to copy.</param>
        /// <param name="destination">The destination file.</param>
        /// <param name="allowHardlink">Allow hardlinks.</param>
        void CopyFile(string source, string destination, bool allowHardlink);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="source">The file to delete.</param>
        /// <param name="throwOnError">Indicates the file must be deleted. Default is a best effort delete.</param>
        /// <param name="maxRetries">Maximum retry attempts. Default is 4.</param>
        void DeleteFile(string source, bool throwOnError = false, int maxRetries = 4);

        /// <summary>
        /// Moves a file.
        /// </summary>
        /// <param name="source">The file to move.</param>
        /// <param name="destination">The destination file.</param>
        void MoveFile(string source, string destination);

        /// <summary>
        /// Opens a file.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">A System.IO.FileMode value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
        /// <param name="access">A System.IO.FileAccess value that specifies the operations that can be performed on the file.</param>
        /// <param name="share">A System.IO.FileShare value specifying the type of access other threads have to the file.</param>
        FileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share);

        /// <summary>
        /// Executes an action and retries on any exception a few times with short pause
        /// between each attempt. Primarily intended for use with file system operations
        /// that might get interrupted by external systems (usually anti-virus).
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="maxRetries">Maximum retry attempts. Default is 4.</param>
        void ExecuteWithRetries(Action action, int maxRetries = 4);
    }
}
