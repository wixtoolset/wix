// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    internal class FileSystem : IFileSystem
    {
        public void CopyFile(SourceLineNumber sourceLineNumbers, string source, string destination, bool allowHardlink)
        {
            try
            {
                this.EnsureDirectoryWithoutFile(destination);
            }
            catch (Exception e)
            {
                throw new WixException(CoreErrors.UnableToCopyFile(sourceLineNumbers, source, destination, e.Message), e);
            }

            var hardlinked = false;

            if (allowHardlink)
            {
                try
                {
                    this.ExecuteWithRetries(() => hardlinked = CreateHardLink(destination, source, IntPtr.Zero));
                }
                catch
                {
                    // Catch hard-link failures and fall back to copy file.
                }
            }

            if (!hardlinked)
            {
#if DEBUG
                var er = Marshal.GetLastWin32Error();
#endif

                try
                {
                    this.ExecuteWithRetries(() => File.Copy(source, destination, overwrite: true));
                }
                catch (Exception e)
                {
                    throw new WixException(CoreErrors.UnableToCopyFile(sourceLineNumbers, source, destination, e.Message), e);
                }
            }
        }

        public void DeleteFile(SourceLineNumber sourceLineNumbers, string source, bool throwOnError = false, int maxRetries = 4)
        {
            try
            {
                this.ExecuteWithRetries(() => File.Delete(source), maxRetries);
            }
            catch (Exception e)
            {
                if (throwOnError)
                {
                    throw new WixException(CoreErrors.UnableToDeleteFile(sourceLineNumbers, source, e.Message), e);
                }
                // else do nothing on best-effort deletes.
            }
        }

        public void MoveFile(SourceLineNumber sourceLineNumbers, string source, string destination)
        {
            try
            {
                this.EnsureDirectoryWithoutFile(destination);

                this.ExecuteWithRetries(() => File.Move(source, destination));
            }
            catch (Exception e)
            {
                throw new WixException(CoreErrors.UnableToMoveFile(sourceLineNumbers, source, destination, e.Message), e);
            }
        }

        public FileStream OpenFile(SourceLineNumber sourceLineNumbers, string path, FileMode mode, FileAccess access, FileShare share)
        {
            const int maxRetries = 4;

            for (var attempt = 1; attempt <= maxRetries; ++attempt)
            {
                try
                {
                    return File.Open(path, mode, access, share);
                }
                catch (Exception e) when (e is IOException || e is SystemException || e is Win32Exception)
                {
                    if (attempt < maxRetries)
                    {
                        Thread.Sleep(250);
                    }
                    else
                    {
                        throw new WixException(CoreErrors.UnableToOpenFile(sourceLineNumbers, path, e.Message), e);
                    }
                }
            }

            throw new InvalidOperationException("Cannot reach this code");
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
                catch (Exception e) when (attempt < maxRetries && (e is IOException || e is SystemException || e is Win32Exception))
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
