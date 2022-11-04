// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;

    /// <summary>
    /// Add referenced standard directory symbols, if not already present.
    /// </summary>
    internal class AddRequiredStandardDirectories
    {
        public AddRequiredStandardDirectories(IntermediateSection section, List<WixSimpleReferenceSymbol> references)
        {
            this.Section = section;
            this.References = references;
        }

        private IntermediateSection Section { get; }

        private List<WixSimpleReferenceSymbol> References { get; }

        public void Execute()
        {
            var platform = this.GetPlatformFromSection();

            var directories = this.Section.Symbols.OfType<DirectorySymbol>().ToList();
            var directoryIds = new SortedSet<string>(directories.Select(d => d.Id.Id));

            // Ensure any standard directory references symbols are added.
            foreach (var directoryReference in this.References.Where(r => r.Table == "Directory"))
            {
                this.EnsureStandardDirectoryAdded(directoryIds, directoryReference.PrimaryKeys, directoryReference.SourceLineNumbers, platform);
            }

            foreach (var directory in directories)
            {
                var parentDirectoryId = directory.ParentDirectoryRef;

                if (String.IsNullOrEmpty(parentDirectoryId))
                {
                    if (directory.Id.Id != "TARGETDIR")
                    {
                        directory.ParentDirectoryRef = "TARGETDIR";
                    }
                }
                else
                {
                    this.EnsureStandardDirectoryAdded(directoryIds, parentDirectoryId, directory.SourceLineNumbers, platform);
                }
            }

            if (!directoryIds.Contains("TARGETDIR") && WindowsInstallerStandard.TryGetStandardDirectory("TARGETDIR", out var targetDir))
            {
                directoryIds.Add(targetDir.Id.Id);
                this.Section.AddSymbol(targetDir);
            }
        }

        private void EnsureStandardDirectoryAdded(ISet<string> directoryIds, string directoryId, SourceLineNumber sourceLineNumbers, Platform platform)
        {
            if (!directoryIds.Contains(directoryId) && WindowsInstallerStandard.TryGetStandardDirectory(directoryId, out var standardDirectory))
            {
                var parentDirectoryId = this.GetStandardDirectoryParent(directoryId, platform);

                var directory = new DirectorySymbol(sourceLineNumbers, standardDirectory.Id)
                {
                    Name = standardDirectory.Name,
                    ParentDirectoryRef = parentDirectoryId,
                };

                directoryIds.Add(directory.Id.Id);
                this.Section.AddSymbol(directory);

                if (!String.IsNullOrEmpty(parentDirectoryId))
                {
                    this.EnsureStandardDirectoryAdded(directoryIds, parentDirectoryId, sourceLineNumbers, platform);
                }
            }
        }

        private string GetStandardDirectoryParent(string directoryId, Platform platform)
        {
            switch (directoryId)
            {
                case "TARGETDIR":
                    return null;

                case "CommonFiles6432Folder":
                case "ProgramFiles6432Folder":
                case "System6432Folder":
                    return WindowsInstallerStandard.GetPlatformSpecificDirectoryId(directoryId, platform);

                default:
                    return "TARGETDIR";
            }
        }

        private Platform GetPlatformFromSection()
        {
            var symbol = this.Section.Symbols.OfType<SummaryInformationSymbol>().First(p => p.PropertyId == SummaryInformationType.PlatformAndLanguage);

            var value = symbol.Value;
            var separatorIndex = value.IndexOf(';');
            var platformValue = separatorIndex > 0 ? value.Substring(0, separatorIndex) : value;

            switch (platformValue)
            {
                case "x64":
                    return Platform.X64;

                case "Arm64":
                    return Platform.ARM64;

                case "Intel":
                default:
                    return Platform.X86;
            }
        }
    }
}
