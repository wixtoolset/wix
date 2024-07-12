// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Versioning;

    internal class BackendHelper : LayoutServices, IBackendHelper
    {
        public BackendHelper(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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

        public IReadOnlyList<ITrackedFile> ExtractEmbeddedFiles(IEnumerable<IExpectedExtractFile> embeddedFiles)
        {
            var command = new ExtractEmbeddedFilesCommand(this, embeddedFiles);
            command.Execute();

            return command.TrackedFiles;
        }

        public string GenerateIdentifier(string prefix, params string[] args)
        {
            return Common.GenerateIdentifier(prefix, args);
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

        public bool IsValidBinderVariable(string variable)
        {
            return Common.IsValidBinderVariable(variable);
        }

        public bool IsValidFourPartVersion(string version)
        {
            return Common.IsValidFourPartVersion(version);
        }

        public bool IsValidMsiProductVersion(string version)
        {
            return Common.IsValidMsiProductVersion(version);
        }

        public bool IsValidWixVersion(string version)
        {
            return WixVersion.TryParse(version, out _);
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

        public bool TryParseFourPartVersion(string version, out string parsedVersion)
        {
            if (WixVersion.TryParse(version, out var wixVersion) && wixVersion.HasMajor && wixVersion.Major < 65536 && wixVersion.Minor < 65536 && wixVersion.Patch < 65536 && wixVersion.Revision < 65536)
            {
                parsedVersion = $"{wixVersion.Major}.{wixVersion.Minor}.{wixVersion.Patch}.{wixVersion.Revision}";
                return true;
            }

            parsedVersion = null;
            return false;
        }

        public bool TryParseMsiProductVersion(string version, bool strict, out string parsedVersion)
        {
            if (WixVersion.TryParse(version, out var wixVersion) && wixVersion.HasMajor && wixVersion.Major < 256 && wixVersion.Minor < 256 && wixVersion.Patch < 65536 && wixVersion.Labels == null && String.IsNullOrEmpty(wixVersion.Metadata))
            {
                if (strict)
                {
                    parsedVersion = $"{wixVersion.Major}.{wixVersion.Minor}.{wixVersion.Patch}";
                }
                else
                {
                    parsedVersion = wixVersion.Prefix.HasValue ? version.Substring(1) : version;
                }

                return true;
            }

            parsedVersion = null;
            return false;
        }
    }
}
