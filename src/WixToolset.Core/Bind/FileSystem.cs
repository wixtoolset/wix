// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using WixToolset.Extensibility;

    internal class FileSystem
    {
        public FileSystem(IEnumerable<ILayoutExtension> extensions)
        {
            this.Extensions = extensions ?? Array.Empty<ILayoutExtension>();
        }

        private IEnumerable<ILayoutExtension> Extensions { get; }

        /// <summary>
        /// Copies a file.
        /// </summary>
        /// <param name="source">The file to copy.</param>
        /// <param name="destination">The destination file.</param>
        public bool CopyFile(string source, string destination)
        {
            foreach (var extension in this.Extensions)
            {
                if (extension.CopyFile(source, destination))
                {
                    return true;
                }
            }

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            if (!CreateHardLink(destination, source, IntPtr.Zero))
            {
#if DEBUG
                int er = Marshal.GetLastWin32Error();
#endif

                File.Copy(source, destination, true);
            }

            return true;
        }

        /// <summary>
        /// Moves a file.
        /// </summary>
        /// <param name="source">The file to move.</param>
        /// <param name="destination">The destination file.</param>
        public bool MoveFile(string source, string destination)
        {
            foreach (var extension in this.Extensions)
            {
                if (extension.MoveFile(source, destination))
                {
                    return true;
                }
            }

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            var directory = Path.GetDirectoryName(destination);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Move(source, destination);

            return true;
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
