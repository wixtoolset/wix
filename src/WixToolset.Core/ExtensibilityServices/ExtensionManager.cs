// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class ExtensionManager : IExtensionManager
    {
        private const string UserWixFolderName = ".wix4";
        private const string MachineWixFolderName = "WixToolset4";
        private const string ExtensionsFolderName = "extensions";

        private readonly List<IExtensionFactory> extensionFactories = new List<IExtensionFactory>();
        private readonly Dictionary<Type, List<object>> loadedExtensionsByType = new Dictionary<Type, List<object>>();

        public ExtensionManager(IWixToolsetCoreServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IWixToolsetCoreServiceProvider ServiceProvider { get; }

        public void Add(Assembly extensionAssembly)
        {
            var types = extensionAssembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface && typeof(IExtensionFactory).IsAssignableFrom(t));
            var factories = types.Select(this.CreateExtensionFactory).ToList();

            if (!factories.Any())
            {
                var path = Path.GetFullPath(new Uri(extensionAssembly.CodeBase).LocalPath);
                throw new WixException(ErrorMessages.InvalidExtension(path, "The extension does not implement IExtensionFactory. All extensions must have at least one implementation of IExtensionFactory."));
            }

            this.extensionFactories.AddRange(factories);
        }

        public void Load(string extensionPath)
        {
            var checkPath = extensionPath;
            var checkedPaths = new List<string> { checkPath };
            try
            {
                if (!TryLoadFromPath(checkPath, out var assembly) && !Path.IsPathRooted(extensionPath))
                {
                    if (TryParseExtensionReference(extensionPath, out var extensionId, out var extensionVersion))
                    {
                        foreach (var cachePath in this.CacheLocations())
                        {
                            var extensionFolder = Path.Combine(cachePath, extensionId);

                            var versionFolder = extensionVersion;
                            if (String.IsNullOrEmpty(versionFolder) && !TryFindLatestVersionInFolder(extensionFolder, out versionFolder))
                            {
                                checkedPaths.Add(extensionFolder);
                                continue;
                            }

                            checkPath = Path.Combine(extensionFolder, versionFolder, "tools", extensionId + ".dll");
                            checkedPaths.Add(checkPath);

                            if (TryLoadFromPath(checkPath, out assembly))
                            {
                                break;
                            }
                        }
                    }
                }

                if (assembly == null)
                {
                    throw new WixException(ErrorMessages.CouldNotFindExtensionInPaths(extensionPath, checkedPaths));
                }

                this.Add(assembly);
            }
            catch (ReflectionTypeLoadException rtle)
            {
                throw new WixException(ErrorMessages.InvalidExtension(checkPath, String.Join(Environment.NewLine, rtle.LoaderExceptions.Select(le => le.ToString()))));
            }
            catch (WixException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new WixException(ErrorMessages.InvalidExtension(checkPath, e.Message), e);
            }
        }

        public IReadOnlyCollection<T> GetServices<T>() where T : class
        {
            if (!this.loadedExtensionsByType.TryGetValue(typeof(T), out var extensions))
            {
                extensions = new List<object>();

                foreach (var factory in this.extensionFactories)
                {
                    if (factory.TryCreateExtension(typeof(T), out var obj) && obj is T extension)
                    {
                        extensions.Add(extension);
                    }
                }

                this.loadedExtensionsByType.Add(typeof(T), extensions);
            }

            return extensions.Cast<T>().ToList();
        }

        private IExtensionFactory CreateExtensionFactory(Type type)
        {
            var constructor = type.GetConstructor(new[] { typeof(IWixToolsetCoreServiceProvider) });
            if (constructor != null)
            {
                return (IExtensionFactory)constructor.Invoke(new[] { this.ServiceProvider });
            }

            return (IExtensionFactory)Activator.CreateInstance(type);
        }

        private IEnumerable<string> CacheLocations()
        {
            var path = Path.Combine(Environment.CurrentDirectory, UserWixFolderName, ExtensionsFolderName);
            if (Directory.Exists(path))
            {
                yield return path;
            }

            path = Environment.GetEnvironmentVariable("WIX_EXTENSIONS") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(path, UserWixFolderName, ExtensionsFolderName);
            if (Directory.Exists(path))
            {
                yield return path;
            }

            if (Environment.Is64BitOperatingSystem)
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), MachineWixFolderName, ExtensionsFolderName);
                if (Directory.Exists(path))
                {
                    yield return path;
                }
            }

            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86), MachineWixFolderName, ExtensionsFolderName);
            if (Directory.Exists(path))
            {
                yield return path;
            }

            path = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().CodeBase).LocalPath), ExtensionsFolderName);
            if (Directory.Exists(path))
            {
                yield return path;
            }
        }

        private static bool TryParseExtensionReference(string extensionReference, out string extensionId, out string extensionVersion)
        {
            extensionId = extensionReference ?? String.Empty;
            extensionVersion = String.Empty;

            var index = extensionId.LastIndexOf('/');
            if (index > 0)
            {
                extensionVersion = extensionReference.Substring(index + 1);
                extensionId = extensionReference.Substring(0, index);

                if (!NuGet.Versioning.NuGetVersion.TryParse(extensionVersion, out _))
                {
                    return false;
                }

                if (String.IsNullOrEmpty(extensionId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryFindLatestVersionInFolder(string basePath, out string foundVersionFolder)
        {
            foundVersionFolder = null;

            try
            {
                NuGet.Versioning.NuGetVersion version = null;
                foreach (var versionPath in Directory.GetDirectories(basePath))
                {
                    var versionFolder = Path.GetFileName(versionPath);
                    if (NuGet.Versioning.NuGetVersion.TryParse(versionFolder, out var checkVersion) &&
                        (version == null || version < checkVersion))
                    {
                        foundVersionFolder = versionFolder;
                        version = checkVersion;
                    }
                }
            }
            catch (IOException)
            {
            }

            return !String.IsNullOrEmpty(foundVersionFolder);
        }

        private static bool TryLoadFromPath(string extensionPath, out Assembly assembly)
        {
            try
            {
                if (File.Exists(extensionPath))
                {
                    assembly = Assembly.LoadFrom(extensionPath);
                    return true;
                }
            }
            catch (IOException e) when (e is FileLoadException || e is FileNotFoundException)
            {
            }

            assembly = null;
            return false;
        }
    }
}
