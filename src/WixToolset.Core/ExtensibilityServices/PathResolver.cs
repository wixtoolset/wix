// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class PathResolver : IPathResolver
    {
        public string GetDirectoryPath(Dictionary<string, IResolvedDirectory> directories, Dictionary<string, string> componentIdGenSeeds, string directory, bool canonicalize)
        {
            if (!directories.TryGetValue(directory, out var resolvedDirectory))
            {
                throw new WixException(ErrorMessages.ExpectedDirectory(directory));
            }

            if (null == resolvedDirectory.Path)
            {
                if (null != componentIdGenSeeds && componentIdGenSeeds.ContainsKey(directory))
                {
                    resolvedDirectory.Path = componentIdGenSeeds[directory];
                }
                else if (canonicalize && WindowsInstallerStandard.IsStandardDirectory(directory))
                {
                    // when canonicalization is on, standard directories are treated equally
                    resolvedDirectory.Path = directory;
                }
                else
                {
                    string name = resolvedDirectory.Name;

                    if (canonicalize)
                    {
                        name = name?.ToLowerInvariant();
                    }

                    if (String.IsNullOrEmpty(resolvedDirectory.DirectoryParent))
                    {
                        resolvedDirectory.Path = name;
                    }
                    else
                    {
                        var parentPath = this.GetDirectoryPath(directories, componentIdGenSeeds, resolvedDirectory.DirectoryParent, canonicalize);

                        if (null != resolvedDirectory.Name)
                        {
                            resolvedDirectory.Path = Path.Combine(parentPath, name);
                        }
                        else
                        {
                            resolvedDirectory.Path = parentPath;
                        }
                    }
                }
            }

            return resolvedDirectory.Path;
        }

        public string GetFileSourcePath(Dictionary<string, IResolvedDirectory> directories, string directoryId, string fileName, bool compressed, bool useLongName)
        {
            var fileSourcePath = Common.GetName(fileName, true, useLongName);

            if (compressed)
            {
                // Use just the file name of the file since all uncompressed files must appear
                // in the root of the image in a compressed package.
            }
            else
            {
                // Get the relative path of where we want the file to be layed out as specified
                // in the Directory table.
                var directoryPath = this.GetDirectoryPath(directories, null, directoryId, false);
                fileSourcePath = Path.Combine(directoryPath, fileSourcePath);
            }

            // Strip off "SourceDir" if it's still on there.
            if (fileSourcePath.StartsWith("SourceDir\\", StringComparison.Ordinal))
            {
                fileSourcePath = fileSourcePath.Substring(10);
            }

            return fileSourcePath;
        }
    }
}
