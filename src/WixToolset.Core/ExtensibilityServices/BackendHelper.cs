// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class BackendHelper : IBackendHelper
    {
        private static readonly string[] ReservedFileNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        public BackendHelper(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IMessaging Messaging { get; }

        public IFileFacade CreateFileFacade(FileSymbol file, AssemblySymbol assembly)
        {
            return new FileFacade(file, assembly);
        }

        public IFileFacade CreateFileFacade(FileRow fileRow)
        {
            return new FileFacade(fileRow);
        }

        public IFileFacade CreateFileFacadeFromMergeModule(FileSymbol fileSymbol)
        {
            return new FileFacade(true, fileSymbol);
        }

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

        public string CreateGuid()
        {
            return Common.GenerateGuid();
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

        public IEnumerable<ITrackedFile> ExtractEmbeddedFiles(IEnumerable<IExpectedExtractFile> embeddedFiles)
        {
            var command = new ExtractEmbeddedFilesCommand(this, embeddedFiles);
            command.Execute();

            return command.TrackedFiles;
        }

        public string GenerateIdentifier(string prefix, params string[] args)
        {
            return Common.GenerateIdentifier(prefix, args);
        }

        public string GetCanonicalRelativePath(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string relativePath)
        {
            return Common.GetCanonicalRelativePath(sourceLineNumbers, elementName, attributeName, relativePath, this.Messaging);
        }

        public int GetValidCodePage(string value, bool allowNoChange = false, bool onlyAnsi = false, SourceLineNumber sourceLineNumbers = null)
        {
            return Common.GetValidCodePage(value, allowNoChange, onlyAnsi, sourceLineNumbers);
        }

        public string GetMsiFileName(string value, bool source, bool longName)
        {
            return Common.GetName(value, source, longName);
        }

        public void ResolveDelayedFields(IEnumerable<IDelayedField> delayedFields, Dictionary<string, string> variableCache)
        {
            var command = new ResolveDelayedFieldsCommand(this.Messaging, delayedFields, variableCache);
            command.Execute();
        }

        public string[] SplitMsiFileName(string value)
        {
            return Common.GetNames(value);
        }

        public ITrackedFile TrackFile(string path, TrackedFileType type, SourceLineNumber sourceLineNumbers = null)
        {
            return new TrackedFile(path, type, sourceLineNumbers);
        }

        public bool IsValidBinderVariable(string variable)
        {
            return Common.IsValidBinderVariable(variable);
        }

        public bool IsValidFourPartVersion(string version)
        {
            return Common.IsValidFourPartVersion(version);
        }

        public bool IsValidIdentifier(string id)
        {
            return Common.IsIdentifier(id);
        }

        public bool IsValidLongFilename(string filename, bool allowWildcards, bool allowRelative)
        {
            return Common.IsValidLongFilename(filename, allowWildcards, allowRelative);
        }

        public bool IsValidShortFilename(string filename, bool allowWildcards)
        {
            return Common.IsValidShortFilename(filename, allowWildcards);
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
