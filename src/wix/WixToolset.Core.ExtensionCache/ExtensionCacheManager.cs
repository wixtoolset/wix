// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensionCache
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NuGet.Common;
    using NuGet.Configuration;
    using NuGet.Credentials;
    using NuGet.Packaging;
    using NuGet.Protocol;
    using NuGet.Protocol.Core.Types;
    using NuGet.Versioning;

    /// <summary>
    /// Extension cache manager.
    /// </summary>
    internal class ExtensionCacheManager
    {
        public string CacheFolder(bool global) => global ? this.GlobalCacheFolder() : this.LocalCacheFolder();

        public string LocalCacheFolder() => Path.Combine(Environment.CurrentDirectory, ".wix", "extensions");

        public string GlobalCacheFolder()
        {
            var baseFolder = Environment.GetEnvironmentVariable("WIX_EXTENSIONS") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(baseFolder, ".wix", "extensions");
        }

        public async Task<bool> AddAsync(bool global, string extension, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(extension))
            {
                throw new ArgumentNullException(nameof(extension));
            }

            (var extensionId, var extensionVersion) = ParseExtensionReference(extension);

            var result = await this.DownloadAndExtractAsync(global, extensionId, extensionVersion, cancellationToken);

            return result;
        }

        public Task<bool> RemoveAsync(bool global, string extension, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(extension))
            {
                throw new ArgumentNullException(nameof(extension));
            }

            (var extensionId, var extensionVersion) = ParseExtensionReference(extension);

            var cacheFolder = this.CacheFolder(global);

            cacheFolder = Path.Combine(cacheFolder, extensionId, extensionVersion);

            if (Directory.Exists(cacheFolder))
            {
                cancellationToken.ThrowIfCancellationRequested();

                Directory.Delete(cacheFolder, true);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<IEnumerable<CachedExtension>> ListAsync(bool global, string extension, CancellationToken cancellationToken)
        {
            var found = new List<CachedExtension>();

            (var extensionId, var extensionVersion) = ParseExtensionReference(extension);

            var cacheFolder = this.CacheFolder(global);

            var searchFolder = Path.Combine(cacheFolder, extensionId, extensionVersion);

            if (!Directory.Exists(searchFolder))
            {
            }
            else if (!String.IsNullOrEmpty(extensionVersion)) // looking for an explicit version of an extension.
            {
                var present = ExtensionFileExists(cacheFolder, extensionId, extensionVersion);
                found.Add(new CachedExtension(extensionId, extensionVersion, !present));
            }
            else // looking for all versions of an extension or all versions of all extensions.
            {
                IEnumerable<string> foundExtensionIds;

                if (String.IsNullOrEmpty(extensionId))
                {
                    // Looking for all versions of all extensions.
                    foundExtensionIds = Directory.GetDirectories(cacheFolder).Select(folder => Path.GetFileName(folder)).ToList();
                }
                else
                {
                    // Looking for all versions of a single extension.
                    var extensionFolder = Path.Combine(cacheFolder, extensionId);
                    foundExtensionIds = Directory.Exists(extensionFolder) ? new[] { extensionId } : Array.Empty<string>();
                }

                foreach (var foundExtensionId in foundExtensionIds)
                {
                    var extensionFolder = Path.Combine(cacheFolder, foundExtensionId);

                    foreach (var folder in Directory.GetDirectories(extensionFolder))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var foundExtensionVersion = Path.GetFileName(folder);

                        if (!NuGetVersion.TryParse(foundExtensionVersion, out _))
                        {
                            continue;
                        }

                        var present = ExtensionFileExists(cacheFolder, foundExtensionId, foundExtensionVersion);
                        found.Add(new CachedExtension(foundExtensionId, foundExtensionVersion, !present));
                    }
                }
            }

            return Task.FromResult((IEnumerable<CachedExtension>)found);
        }

        private async Task<bool> DownloadAndExtractAsync(bool global, string id, string version, CancellationToken cancellationToken)
        {
            var logger = NullLogger.Instance;

            DefaultCredentialServiceUtility.SetupDefaultCredentialService(logger, nonInteractive: false);

            var settings = Settings.LoadDefaultSettings(root: Environment.CurrentDirectory);
            var sources = PackageSourceProvider.LoadPackageSources(settings).Where(s => s.IsEnabled);

            using (var cache = new SourceCacheContext())
            {
                PackageSource versionSource = null;

                var nugetVersion = String.IsNullOrEmpty(version) ? null : new NuGetVersion(version);

                if (nugetVersion is null)
                {
                    foreach (var source in sources)
                    {
                        var repository = Repository.Factory.GetCoreV3(source.Source);
                        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

                        var availableVersions = await resource.GetAllVersionsAsync(id, cache, logger, cancellationToken);
                        foreach (var availableVersion in availableVersions)
                        {
                            if (nugetVersion is null || nugetVersion < availableVersion)
                            {
                                nugetVersion = availableVersion;
                                versionSource = source;
                            }
                        }
                    }

                    if (nugetVersion is null)
                    {
                        return false;
                    }
                }

                var searchSources = versionSource is null ? sources : new[] { versionSource };

                var extensionFolder = Path.Combine(this.CacheFolder(global), id, nugetVersion.ToString());

                foreach (var source in searchSources)
                {
                    var repository = Repository.Factory.GetCoreV3(source.Source);
                    var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

                    using (var stream = new MemoryStream())
                    {
                        var downloaded = await resource.CopyNupkgToStreamAsync(id, nugetVersion, stream, cache, logger, cancellationToken);

                        if (downloaded)
                        {
                            stream.Position = 0;

                            using (var archive = new PackageArchiveReader(stream))
                            {
                                var files = PackagingConstants.Folders.Known.SelectMany(folder => archive.GetFiles(folder)).Distinct(StringComparer.OrdinalIgnoreCase);
                                await archive.CopyFilesAsync(extensionFolder, files, this.ExtractProgress, logger, cancellationToken);
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private string ExtractProgress(string sourceFile, string targetPath, Stream fileStream) => fileStream.CopyToFile(targetPath);

        private static (string extensionId, string extensionVersion) ParseExtensionReference(string extensionReference)
        {
            var extensionId = extensionReference ?? String.Empty;
            var extensionVersion = String.Empty;

            var index = extensionId.LastIndexOf('/');
            if (index > 0)
            {
                extensionVersion = extensionReference.Substring(index + 1);
                extensionId = extensionReference.Substring(0, index);

                if (!NuGetVersion.TryParse(extensionVersion, out _))
                {
                    throw new ArgumentException($"Invalid extension version in {extensionReference}");
                }

                if (String.IsNullOrEmpty(extensionId))
                {
                    throw new ArgumentException($"Invalid extension id in {extensionReference}");
                }
            }

            return (extensionId, extensionVersion);
        }

        private static bool ExtensionFileExists(string baseFolder, string extensionId, string extensionVersion)
        {
            var toolsFolder = Path.Combine(baseFolder, extensionId, extensionVersion, "tools");
            if (!Directory.Exists(toolsFolder))
            {
                return false;
            }

            var extensionAssembly = Path.Combine(toolsFolder, extensionId + ".dll");

            var present = File.Exists(extensionAssembly);
            if (!present)
            {
                extensionAssembly = Path.Combine(toolsFolder, extensionId + ".exe");
                present = File.Exists(extensionAssembly);
            }

            return present;
        }
    }
}
