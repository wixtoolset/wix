// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using WixToolset.Extensibility.Services;

    internal class FileSystem : IFileSystem
    {
        public void CopyFile(string source, string destination, bool allowHardlink)
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

        public void MoveFile(string source, string destination)
        {
            EnsureDirectoryWithoutFile(destination);

            ActionWithRetries(() => File.Move(source, destination));
        }

        /// <summary>
        /// Executes an action and retries on any exception up to a few times. Primarily
        /// intended for use with file system operations that might get interrupted by
        /// external systems (usually anti-virus).
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="maxRetries">Maximum retry attempts.</param>
        internal static void ActionWithRetries(Action action, int maxRetries = 3)
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
