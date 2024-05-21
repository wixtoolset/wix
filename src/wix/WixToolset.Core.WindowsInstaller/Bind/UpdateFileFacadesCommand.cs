// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Update file information.
    /// </summary>
    internal class UpdateFileFacadesCommand
    {
        public UpdateFileFacadesCommand(IMessaging messaging, IFileSystem fileSystem, IntermediateSection section, IEnumerable<IFileFacade> allFileFacades, IEnumerable<IFileFacade> updateFileFacades, IDictionary<string, string> variableCache, bool overwriteHash, CancellationToken cancellationToken)
        {
            this.Messaging = messaging;
            this.FileSystem = fileSystem;
            this.Section = section;
            this.AllFileFacades = allFileFacades;
            this.UpdateFileFacades = updateFileFacades;
            this.VariableCache = variableCache;
            this.OverwriteHash = overwriteHash;
            this.CancellationToken = cancellationToken;
        }

        private IMessaging Messaging { get; }

        private IFileSystem FileSystem { get; }

        private IntermediateSection Section { get; }

        private IEnumerable<IFileFacade> AllFileFacades { get; }

        private IEnumerable<IFileFacade> UpdateFileFacades { get; }

        private bool OverwriteHash { get; }

        private IDictionary<string, string> VariableCache { get; }

        private CancellationToken CancellationToken { get; }

        public void Execute()
        {
            try
            {
                this.UpdateFileFacadesInParallel(this.UpdateFileFacades.Where(f => f.SourcePath != null));
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.Flatten().InnerExceptions)
                {
                    throw ex;
                }
            }
        }

        private void UpdateFileFacadesInParallel(IEnumerable<IFileFacade> facades)
        {
            var mut = new Mutex();
            var exceptions = new ConcurrentQueue<Exception>();

            var assemblySymbols = this.Section.Symbols.OfType<AssemblySymbol>().ToDictionary(t => t.Id.Id);

            Parallel.ForEach(facades,
                new ParallelOptions{
                    CancellationToken = this.CancellationToken
                },
                () =>
                {
                    return new LocalData(
                        this.Messaging,
                        this.FileSystem,
                        this.AllFileFacades,
                        this.OverwriteHash,
                        this.Section.Symbols.OfType<MsiAssemblyNameSymbol>().ToDictionary(t => t.Id.Id),
                        useVariableCache: null != this.VariableCache
                        );
                },
                (file, loopstate, local) =>
                {
                    try
                    {
                        local.UpdateFileFacade(file, assemblySymbols);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                        loopstate.Stop();
                    }

                    return local;
                },
                (local) =>
                {
                    // Merge local variable cache back into common variable cache
                    if (null != this.VariableCache)
                    {
                        mut.WaitOne();
                        try
                        {
                            local.MergeVariableCacheWith(this.VariableCache);
                        }
                        finally
                        {
                            mut.ReleaseMutex();
                        }
                    }
                }
            );

            if (!exceptions.IsEmpty)
            {
                throw new AggregateException(exceptions);
            }
        }


        internal class LocalData
        {
            public LocalData(IMessaging messaging, IFileSystem fileSystem, IEnumerable<IFileFacade> allFileFacades, bool overwriteHash, Dictionary<string, MsiAssemblyNameSymbol> assemblyNameSymbols, bool useVariableCache)
            {
                this.Messaging = messaging;
                this.FileSystem = fileSystem;
                this.AllFileFacades = allFileFacades;
                this.OverwriteHash = overwriteHash;
                this.AssemblyNameSymbols = assemblyNameSymbols;
                this.VariableCache = useVariableCache ? new Dictionary<string, string>() : null;
            }

            private IMessaging Messaging { get; }

            private IFileSystem FileSystem { get; }

            private IEnumerable<IFileFacade> AllFileFacades { get; }

            private bool OverwriteHash { get; }

            private Dictionary<string, MsiAssemblyNameSymbol> AssemblyNameSymbols { get; }

            private Dictionary<string, string> VariableCache { get; }

            public void MergeVariableCacheWith(IDictionary<string, string> variableCache)
            {
                if (null != variableCache && null != this.VariableCache)
                {
                    foreach (var v in this.VariableCache)
                    {
                        variableCache[v.Key] = v.Value;
                    }
                }
            }

            public void UpdateFileFacade(IFileFacade facade, Dictionary<string, AssemblySymbol> assemblySymbols)
            {
                FileInfo fileInfo = null;
                try
                {
                    fileInfo = new FileInfo(facade.SourcePath);
                }
                catch (ArgumentException)
                {
                    this.Messaging.Write(ErrorMessages.InvalidFileName(facade.SourceLineNumber, facade.SourcePath));
                    return;
                }
                catch (PathTooLongException)
                {
                    this.Messaging.Write(ErrorMessages.InvalidFileName(facade.SourceLineNumber, facade.SourcePath));
                    return;
                }
                catch (NotSupportedException)
                {
                    this.Messaging.Write(ErrorMessages.InvalidFileName(facade.SourceLineNumber, facade.SourcePath));
                    return;
                }

                if (!fileInfo.Exists)
                {
                    this.Messaging.Write(ErrorMessages.CannotFindFile(facade.SourceLineNumber, facade.Id, facade.FileName, facade.SourcePath));
                    return;
                }

                using (var fileStream = this.FileSystem.OpenFile(facade.SourceLineNumber, fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (Int32.MaxValue < fileStream.Length)
                    {
                        throw new WixException(ErrorMessages.FileTooLarge(facade.SourceLineNumber, facade.SourcePath));
                    }

                    facade.FileSize = Convert.ToInt32(fileStream.Length, CultureInfo.InvariantCulture);
                }

                string version = null;
                string language = null;
                try
                {
                    Installer.GetFileVersion(fileInfo.FullName, out version, out language);
                }
                catch (Win32Exception e)
                {
                    if (0x2 == e.NativeErrorCode) // ERROR_FILE_NOT_FOUND
                    {
                        throw new WixException(ErrorMessages.FileNotFound(facade.SourceLineNumber, fileInfo.FullName));
                    }
                    else
                    {
                        throw new WixException(ErrorMessages.Win32Exception(e.NativeErrorCode, e.Message));
                    }
                }

                // If there is no version, it is assumed there is no language because it won't matter in the versioning of the install.
                if (String.IsNullOrEmpty(version)) // unversioned files have their hashes added to the MsiFileHash table
                {
                    if (!this.OverwriteHash)
                    {
                        // not overwriting hash, so don't do the rest of these options.
                    }
                    else if (null != facade.Version)
                    {
                        // Search all of the file rows available to see if the specified version is actually a companion file. Yes, this looks
                        // very expensive and you're probably thinking it would be better to create an index of some sort to do an O(1) look up.
                        // That's a reasonable thought but companion file usage is usually pretty rare so we'd be doing something expensive (indexing
                        // all the file rows) for a relatively uncommon situation. Let's not do that.
                        //
                        // Also, if we do not find a matching file identifier then the user provided a default version and is providing a version
                        // for unversioned file. That's allowed but generally a dangerous thing to do so let's point that out to the user.
                        if (!this.AllFileFacades.Any(r => facade.Version.Equals(r.Id, StringComparison.Ordinal)))
                        {
                            this.Messaging.Write(WarningMessages.DefaultVersionUsedForUnversionedFile(facade.SourceLineNumber, facade.Version, facade.Id));
                        }
                    }
                    else
                    {
                        if (null != facade.Language)
                        {
                            this.Messaging.Write(WarningMessages.DefaultLanguageUsedForUnversionedFile(facade.SourceLineNumber, facade.Language, facade.Id));
                        }

                        int[] hash;
                        try
                        {
                            Installer.GetFileHash(fileInfo.FullName, 0, out hash);
                        }
                        catch (Win32Exception e)
                        {
                            if (0x2 == e.NativeErrorCode) // ERROR_FILE_NOT_FOUND
                            {
                                throw new WixException(ErrorMessages.FileNotFound(facade.SourceLineNumber, fileInfo.FullName));
                            }
                            else
                            {
                                throw new WixException(ErrorMessages.Win32Exception(e.NativeErrorCode, fileInfo.FullName, e.Message));
                            }
                        }

                        // Remember the hash symbol for use later.
                        facade.MsiFileHashSymbol = new MsiFileHashSymbol(facade.SourceLineNumber, facade.Identifier)
                        {
                            Options = 0,
                            HashPart1 = hash[0],
                            HashPart2 = hash[1],
                            HashPart3 = hash[2],
                            HashPart4 = hash[3],
                        };
                    }
                }
                else // update the file row with the version and language information.
                {
                    // If no version was provided by the user, use the version from the file itself.
                    // This is the most common case.
                    if (String.IsNullOrEmpty(facade.Version))
                    {
                        facade.Version = version;
                    }
                    else if (!this.AllFileFacades.Any(r => facade.Version.Equals(r.Id, StringComparison.Ordinal))) // this looks expensive, but see explanation below.
                    {
                        // The user provided a default version for the file row so we looked for a companion file (a file row with Id matching
                        // the version value). We didn't find it so, we will override the default version they provided with the actual
                        // version from the file itself. Now, I know it looks expensive to search through all the file rows trying to match
                        // on the Id. However, the alternative is to build a big index of all file rows to do look ups. Since this case
                        // where the file version is already present is rare (companion files are pretty uncommon), we'll do the more
                        // CPU intensive search to save on the memory intensive index that wouldn't be used much.
                        //
                        // Also note this case can occur when the file is being updated using the WixBindUpdatedFiles extension mechanism.
                        // That's typically even more rare than companion files so again, no index, just search.
                        facade.Version = version;
                    }

                    if (!String.IsNullOrEmpty(facade.Language) && String.IsNullOrEmpty(language))
                    {
                        this.Messaging.Write(WarningMessages.DefaultLanguageUsedForVersionedFile(facade.SourceLineNumber, facade.Language, facade.Id));
                    }
                    else // override the default provided by the user (usually nothing) with the actual language from the file itself.
                    {
                        facade.Language = language;
                    }
                }

                // Populate the binder variables for this file information if requested.
                if (null != this.VariableCache)
                {
                    this.VariableCache[$"fileversion.{facade.Id}"] = facade.Version ?? String.Empty;
                    this.VariableCache[$"filelanguage.{facade.Id}"] = facade.Language ?? String.Empty;
                }

                // If there is an assembly for this file.
                if (assemblySymbols.TryGetValue(facade.Id, out var assemblySymbol))
                {
                    // If this is a CLR assembly, load the assembly and get the assembly name information
                    if (AssemblyType.DotNetAssembly == assemblySymbol.Type)
                    {
                        try
                        {
                            var assemblyName = AssemblyNameReader.ReadAssembly(this.FileSystem, facade.SourceLineNumber, fileInfo.FullName, version);

                            this.SetMsiAssemblyName(facade, assemblySymbol, "name", assemblyName.Name);
                            this.SetMsiAssemblyName(facade, assemblySymbol, "culture", assemblyName.Culture);
                            this.SetMsiAssemblyName(facade, assemblySymbol, "version", assemblyName.Version);

                            if (!String.IsNullOrEmpty(assemblyName.Architecture))
                            {
                                this.SetMsiAssemblyName(facade, assemblySymbol, "processorArchitecture", assemblyName.Architecture);
                            }
                            // TODO: WiX v3 seemed to do this but not clear it should actually be done.
                            //else if (!String.IsNullOrEmpty(file.WixFile.ProcessorArchitecture))
                            //{
                            //    this.SetMsiAssemblyName(assemblyNameSymbols, file, "processorArchitecture", file.WixFile.ProcessorArchitecture);
                            //}

                            if (assemblyName.StrongNamedSigned)
                            {
                                this.SetMsiAssemblyName(facade, assemblySymbol, "publicKeyToken", assemblyName.PublicKeyToken);
                            }
                            else if (assemblySymbol.ApplicationFileRef == null)
                            {
                                throw new WixException(ErrorMessages.GacAssemblyNoStrongName(facade.SourceLineNumber, fileInfo.FullName, facade.ComponentRef));
                            }

                            if (!String.IsNullOrEmpty(assemblyName.FileVersion))
                            {
                                this.SetMsiAssemblyName(facade, assemblySymbol, "fileVersion", assemblyName.FileVersion);
                            }

                            // add the assembly name to the information cache
                            if (null != this.VariableCache)
                            {
                                this.VariableCache[$"assemblyfullname.{facade.Id}"] = assemblyName.GetFullName();
                            }
                        }
                        catch (WixException e)
                        {
                            this.Messaging.Write(e.Error);
                        }
                    }
                    else if (AssemblyType.Win32Assembly == assemblySymbol.Type)
                    {
                        // TODO: Consider passing in the this.AllFileFacades as an indexed collection instead of searching through
                        // all files like this. Even though this is a rare case it looks like we might be able to index the
                        // file earlier.
                        var fileManifest = this.AllFileFacades.FirstOrDefault(r => r.Id.Equals(assemblySymbol.ManifestFileRef, StringComparison.Ordinal));
                        if (null == fileManifest)
                        {
                            this.Messaging.Write(ErrorMessages.MissingManifestForWin32Assembly(facade.SourceLineNumber, facade.Id, assemblySymbol.ManifestFileRef));
                        }

                        try
                        {
                            var assemblyName = AssemblyNameReader.ReadAssemblyManifest(facade.SourceLineNumber, fileManifest.SourcePath);

                            if (!String.IsNullOrEmpty(assemblyName.Name))
                            {
                                this.SetMsiAssemblyName(facade, assemblySymbol, "name", assemblyName.Name);
                            }

                            if (!String.IsNullOrEmpty(assemblyName.Version))
                            {
                                this.SetMsiAssemblyName(facade, assemblySymbol, "version", assemblyName.Version);
                            }

                            if (!String.IsNullOrEmpty(assemblyName.Type))
                            {
                                this.SetMsiAssemblyName(facade, assemblySymbol, "type", assemblyName.Type);
                            }

                            if (!String.IsNullOrEmpty(assemblyName.Architecture))
                            {
                                this.SetMsiAssemblyName(facade, assemblySymbol, "processorArchitecture", assemblyName.Architecture);
                            }

                            if (!String.IsNullOrEmpty(assemblyName.PublicKeyToken))
                            {
                                this.SetMsiAssemblyName(facade, assemblySymbol, "publicKeyToken", assemblyName.PublicKeyToken);
                            }
                        }
                        catch (WixException e)
                        {
                            this.Messaging.Write(e.Error);
                        }
                    }
                }
            }

            private void SetMsiAssemblyName(IFileFacade facade, AssemblySymbol assemblySymbol, string name, string value)
            {
                // check for null value (this can occur when grabbing the file version from an assembly without one)
                if (String.IsNullOrEmpty(value))
                {
                    this.Messaging.Write(WarningMessages.NullMsiAssemblyNameValue(facade.SourceLineNumber, facade.ComponentRef, name));
                }
                else
                {
                    // if the assembly will be GAC'd and the name in the file table doesn't match the name in the MsiAssemblyName table, error because the install will fail.
                    if ("name" == name && AssemblyType.DotNetAssembly == assemblySymbol.Type &&
                        String.IsNullOrEmpty(assemblySymbol.ApplicationFileRef) &&
                        !String.Equals(Path.GetFileNameWithoutExtension(facade.FileName), value, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Messaging.Write(ErrorMessages.GACAssemblyIdentityWarning(facade.SourceLineNumber, Path.GetFileNameWithoutExtension(facade.FileName), value));
                    }

                    // Override directly authored value, otherwise remember the gathered information on the facade for use later.
                    var lookup = String.Concat(facade.ComponentRef, "/", name);
                    if (this.AssemblyNameSymbols.TryGetValue(lookup, out var assemblyNameSymbol))
                    {
                        assemblyNameSymbol.Value = value;
                    }
                    else
                    {
                        assemblyNameSymbol = new MsiAssemblyNameSymbol(assemblySymbol.SourceLineNumbers, new Identifier(AccessModifier.Section, facade.ComponentRef, name))
                        {
                            ComponentRef = facade.ComponentRef,
                            Name = name,
                            Value = value,
                        };

                        facade.AssemblyNameSymbols.Add(assemblyNameSymbol);
                        this.AssemblyNameSymbols.Add(assemblyNameSymbol.Id.Id, assemblyNameSymbol);
                    }

                    if (this.VariableCache != null)
                    {
                        var key = String.Format(CultureInfo.InvariantCulture, "assembly{0}.{1}", name, facade.Id).ToLowerInvariant();
                        this.VariableCache[key] = value;
                    }
                }
            }
        }
    }
}
