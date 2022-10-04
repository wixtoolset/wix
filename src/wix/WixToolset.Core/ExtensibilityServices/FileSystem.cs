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
            this.EnsureDirectoryWithoutFile(destination);

            var hardlinked = false;

            if (allowHardlink)
            {
                this.ExecuteWithRetries(() => hardlinked = CreateHardLink(destination, source, IntPtr.Zero));
            }

            if (!hardlinked)
            {
#if DEBUG
                var er = Marshal.GetLastWin32Error();
#endif

                this.ExecuteWithRetries(() => File.Copy(source, destination, overwrite: true));
            }
        }

        public void DeleteFile(string source, bool throwOnError = false, int maxRetries = 4)
        {
            try
            {
                this.ExecuteWithRetries(() => File.Delete(source), maxRetries);
            }
            catch when (!throwOnError)
            {
                // Do nothing on best-effort deletes.
            }
        }

        public void MoveFile(string source, string destination)
        {
            this.EnsureDirectoryWithoutFile(destination);

            this.ExecuteWithRetries(() => File.Move(source, destination));
        }

        public void ExecuteWithRetries(Action action, int maxRetries = 4)
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

        private void EnsureDirectoryWithoutFile(string path)
        {
            var directory = Path.GetDirectoryName(path);

            if (!String.IsNullOrEmpty(directory))
            {
                this.ExecuteWithRetries(() => Directory.CreateDirectory(directory));
            }

            this.ExecuteWithRetries(() => File.Delete(path));
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
