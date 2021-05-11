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
        public string GetCanonicalDirectoryPath(Dictionary<string, IResolvedDirectory> directories, Dictionary<string, string> componentIdGenSeeds, string directory, Platform platform)
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
                else if (WindowsInstallerStandard.IsStandardDirectory(directory))
                {
                    resolvedDirectory.Path = WindowsInstallerStandard.GetPlatformSpecificDirectoryId(directory, platform);
                }
                else
                {
                    var name = resolvedDirectory.Name?.ToLowerInvariant();

                    if (String.IsNullOrEmpty(resolvedDirectory.DirectoryParent))
                    {
                        resolvedDirectory.Path = name;
                    }
                    else
                    {
                        var parentPath = this.GetCanonicalDirectoryPath(directories, componentIdGenSeeds, resolvedDirectory.DirectoryParent, platform);

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

        public string GetDirectoryPath(Dictionary<string, IResolvedDirectory> directories, string directory)
        {
            if (!directories.TryGetValue(directory, out var resolvedDirectory))
            {
                throw new WixException(ErrorMessages.ExpectedDirectory(directory));
            }

            if (null == resolvedDirectory.Path)
            {
                var name = resolvedDirectory.Name;

                if (String.IsNullOrEmpty(resolvedDirectory.DirectoryParent))
                {
                    resolvedDirectory.Path = name;
                }
                else
                {
                    var parentPath = this.GetDirectoryPath(directories, resolvedDirectory.DirectoryParent);

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
                var directoryPath = this.GetDirectoryPath(directories, directoryId);
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
