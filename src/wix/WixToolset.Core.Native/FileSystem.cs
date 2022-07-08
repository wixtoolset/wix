// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Threading;

    /// <summary>
    /// File system helpers.
    /// </summary>
    public static class FileSystem
    {
        /// <summary>
        /// Copies a file.
        /// </summary>
        /// <param name="source">The file to copy.</param>
        /// <param name="destination">The destination file.</param>
        /// <param name="allowHardlink">Allow hardlinks.</param>
        public static void CopyFile(string source, string destination, bool allowHardlink)
        {
            EnsureDirectoryWithoutFile(destination);

            var hardlinked = false;

            if (allowHardlink)
            {
                ActionWithRetries(() => hardlinked = CreateHardLink(destination, source, IntPtr.Zero));
            }

            if (!hardlinked)
            {
#if DEBUG
                var er = Marshal.GetLastWin32Error();
#endif

                ActionWithRetries(() => File.Copy(source, destination, overwrite: true));
            }
        }

        /// <summary>
        /// Moves a file.
        /// </summary>
        /// <param name="source">The file to move.</param>
        /// <param name="destination">The destination file.</param>
        public static void MoveFile(string source, string destination)
        {
            EnsureDirectoryWithoutFile(destination);

            ActionWithRetries(() => File.Move(source, destination));
        }

        /// <summary>
        /// Reset the ACLs on a set of files.
        /// </summary>
        /// <param name="files">The list of file paths to set ACLs.</param>
        public static void ResetAcls(IEnumerable<string> files)
        {
            var aclReset = new FileSecurity();
            aclReset.SetAccessRuleProtection(false, false);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                ActionWithRetries(() => fileInfo.SetAccessControl(aclReset));
            }
        }

        /// <summary>
        /// Executes an action and retries on any exception up to a few times. Primarily
        /// intended for use with file system operations that might get interrupted by
        /// external systems (usually anti-virus).
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="maxRetries">Maximum retry attempts.</param>
        public static void ActionWithRetries(Action action, int maxRetries = 3)
        {
            for (var attempt = 1; attempt <= maxRetries; ++attempt)
            {
                try
                {
                    action();
                    break;
                }
                catch when (attempt < maxRetries)
                {
                    Thread.Sleep(250);
                }
            }
        }

        private static void EnsureDirectoryWithoutFile(string path)
        {
            var directory = Path.GetDirectoryName(path);

            if (!String.IsNullOrEmpty(directory))
            {
                ActionWithRetries(() => Directory.CreateDirectory(directory));
            }

            ActionWithRetries(() => File.Delete(path));
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
