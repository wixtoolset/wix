// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
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
        internal AddRequiredStandardDirectories(IntermediateSection section, Platform platform)
        {
            this.Section = section;
            this.Platform = platform;
        }

        private IntermediateSection Section { get; }

        private Platform Platform { get; }

        public void Execute()
        {
            var directories = this.Section.Symbols.OfType<DirectorySymbol>().ToList();
            var directoryIds = new SortedSet<string>(directories.Select(d => d.Id.Id));

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
                    this.EnsureStandardDirectoryAdded(directoryIds, parentDirectoryId, directory.SourceLineNumbers);
                }
            }

            if (!directoryIds.Contains("TARGETDIR") && WindowsInstallerStandard.TryGetStandardDirectory("TARGETDIR", out var targetDir))
            {
                directoryIds.Add(targetDir.Id.Id);
                this.Section.AddSymbol(targetDir);
            }
        }

        private void EnsureStandardDirectoryAdded(ISet<string> directoryIds, string directoryId, SourceLineNumber sourceLineNumbers)
        {
            if (!directoryIds.Contains(directoryId) && WindowsInstallerStandard.TryGetStandardDirectory(directoryId, out var standardDirectory))
            {
                var parentDirectoryId = this.GetStandardDirectoryParent(directoryId);

                var directory = new DirectorySymbol(sourceLineNumbers, standardDirectory.Id)
                {
                    Name = standardDirectory.Name,
                    ParentDirectoryRef = parentDirectoryId,
                };

                directoryIds.Add(directory.Id.Id);
                this.Section.AddSymbol(directory);

                if (!String.IsNullOrEmpty(parentDirectoryId))
                {
                    this.EnsureStandardDirectoryAdded(directoryIds, parentDirectoryId, sourceLineNumbers);
                }
            }
        }

        private string GetStandardDirectoryParent(string directoryId)
        {
            switch (directoryId)
            {
                case "TARGETDIR":
                    return null;

                case "CommonFiles6432Folder":
                case "ProgramFiles6432Folder":
                case "System6432Folder":
                    return WindowsInstallerStandard.GetPlatformSpecificDirectoryId(directoryId, this.Platform);

                default:
                    return "TARGETDIR";
            }
        }
    }
}
