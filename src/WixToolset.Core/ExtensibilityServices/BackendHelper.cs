// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class BackendHelper : IBackendHelper
    {
        private static readonly string[] ReservedFileNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        public BackendHelper(IWixToolsetServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ParseHelper = serviceProvider.GetService<IParseHelper>();
        }

        private IMessaging Messaging { get; }

        private IParseHelper ParseHelper { get; }

        public IFileTransfer CreateFileTransfer(string source, string destination, bool move, SourceLineNumber sourceLineNumbers = null)
        {
            var sourceFullPath = this.GetValidatedFullPath(sourceLineNumbers, source);

            var destinationFullPath = this.GetValidatedFullPath(sourceLineNumbers, destination);

            return (String.IsNullOrEmpty(sourceFullPath) || String.IsNullOrEmpty(destinationFullPath)) ? null : new FileTransfer
            {
                Source = sourceFullPath,
                Destination = destinationFullPath,
                Move = move,
                SourceLineNumbers = sourceLineNumbers,
                Redundant = String.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase)
            };
        }

        public string CreateGuid(Guid namespaceGuid, string value)
        {
            return Uuid.NewUuid(namespaceGuid, value).ToString("B").ToUpperInvariant();
        }

        public IResolvedDirectory CreateResolvedDirectory(string directoryParent, string name)
        {
            return new ResolvedDirectory
            {
                DirectoryParent = directoryParent,
                Name = name
            };
        }

        public string GetCanonicalRelativePath(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string relativePath)
        {
            return this.ParseHelper.GetCanonicalRelativePath(sourceLineNumbers, elementName, attributeName, relativePath);
        }

        public ITrackedFile TrackFile(string path, TrackedFileType type, SourceLineNumber sourceLineNumbers = null)
        {
            return new TrackedFile(path, type, sourceLineNumbers);
        }

        private string GetValidatedFullPath(SourceLineNumber sourceLineNumbers, string path)
        {
            try
            {
                var result = Path.GetFullPath(path);

                var filename = Path.GetFileName(result);

                foreach (var reservedName in ReservedFileNames)
                {
                    if (reservedName.Equals(filename, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Messaging.Write(ErrorMessages.InvalidFileName(sourceLineNumbers, path));
                        return null;
                    }
                }

                return result;
            }
            catch (ArgumentException)
            {
                this.Messaging.Write(ErrorMessages.InvalidFileName(sourceLineNumbers, path));
            }
            catch (PathTooLongException)
            {
                this.Messaging.Write(ErrorMessages.PathTooLong(sourceLineNumbers, path));
            }

            return null;
        }
    }
}
