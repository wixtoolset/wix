// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Extensibility;

    internal class FileSystemManager
    {
        public FileSystemManager(IEnumerable<IFileSystemExtension> fileSystemExtensions)
        {
            this.Extensions = fileSystemExtensions;
        }

        private IEnumerable<IFileSystemExtension> Extensions { get; }

        public bool CompareFiles(string firstPath, string secondPath)
        {
            foreach (var extension in this.Extensions)
            {
                var compared = extension.CompareFiles(firstPath, secondPath);
                if (compared.HasValue)
                {
                    return compared.Value;
                }
            }

            return BuiltinCompareFiles(firstPath, secondPath);
        }

        private static bool BuiltinCompareFiles(string firstPath, string secondPath)
        {
            if (String.Equals(firstPath, secondPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            using (var firstStream = File.OpenRead(firstPath))
            using (var secondStream = File.OpenRead(secondPath))
            {
                if (firstStream.Length != secondStream.Length)
                {
                    return false;
                }

                // Using a larger buffer than the default buffer of 4 * 1024 used by FileStream.ReadByte improves performance.
                // The buffer size is based on user feedback. Based on performance results, a better buffer size may be determined.
                var firstBuffer = new byte[16 * 1024];
                var secondBuffer = new byte[16 * 1024];

                var firstReadLength = 0;
                do
                {
                    firstReadLength = firstStream.Read(firstBuffer, 0, firstBuffer.Length);
                    var secondReadLength = secondStream.Read(secondBuffer, 0, secondBuffer.Length);

                    if (firstReadLength != secondReadLength)
                    {
                        return false;
                    }

                    for (var i = 0; i < firstReadLength; ++i)
                    {
                        if (firstBuffer[i] != secondBuffer[i])
                        {
                            return false;
                        }
                    }
                } while (0 < firstReadLength);
            }

            return true;
        }
    }
}
