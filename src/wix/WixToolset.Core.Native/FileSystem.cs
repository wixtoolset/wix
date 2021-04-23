// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;

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

            if (!allowHardlink || !CreateHardLink(destination, source, IntPtr.Zero))
            {
#if DEBUG
                var er = Marshal.GetLastWin32Error();
#endif

                File.Copy(source, destination, overwrite: true);
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

            File.Move(source, destination);
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
                new FileInfo(file).SetAccessControl(aclReset);
            }
        }

        private static void EnsureDirectoryWithoutFile(string path)
        {
            File.Delete(path);

            var directory = Path.GetDirectoryName(path);

            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
