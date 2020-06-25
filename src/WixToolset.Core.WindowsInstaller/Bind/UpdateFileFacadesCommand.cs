// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Update file information.
    /// </summary>
    internal class UpdateFileFacadesCommand
    {
        public UpdateFileFacadesCommand(IMessaging messaging, IntermediateSection section, IEnumerable<FileFacade> fileFacades, IEnumerable<FileFacade> updateFileFacades, IDictionary<string, string> variableCache, bool overwriteHash)
        {
            this.Messaging = messaging;
            this.Section = section;
            this.FileFacades = fileFacades;
            this.UpdateFileFacades = updateFileFacades;
            this.VariableCache = variableCache;
            this.OverwriteHash = overwriteHash;
        }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        private IEnumerable<FileFacade> FileFacades { get; }

        private IEnumerable<FileFacade> UpdateFileFacades { get; }

        private bool OverwriteHash { get; }

        private IDictionary<string, string> VariableCache { get; }

        public void Execute()
        {
            var assemblyNameSymbols = this.Section.Symbols.OfType<MsiAssemblyNameSymbol>().ToDictionary(t => t.Id.Id);

            foreach (var file in this.UpdateFileFacades)
            {
                this.UpdateFileFacade(file, assemblyNameSymbols);
            }
        }

        private void UpdateFileFacade(FileFacade facade, Dictionary<string, MsiAssemblyNameSymbol> assemblyNameSymbols)
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

            using (var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
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
                    if (!this.FileFacades.Any(r => facade.Version.Equals(r.Id, StringComparison.Ordinal)))
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

                    if (null == facade.Hash)
                    {
                        facade.Hash = this.Section.AddSymbol(new MsiFileHashSymbol(facade.SourceLineNumber, facade.Identifier));
                    }

                    facade.Hash.Options = 0;
                    facade.Hash.HashPart1 = hash[0];
                    facade.Hash.HashPart2 = hash[1];
                    facade.Hash.HashPart3 = hash[2];
                    facade.Hash.HashPart4 = hash[3];
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
                else if (!this.FileFacades.Any(r => facade.Version.Equals(r.Id, StringComparison.Ordinal))) // this looks expensive, but see explanation below.
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

                // Populate the binder variables for this file information if requested.
                if (null != this.VariableCache)
                {
                    if (!String.IsNullOrEmpty(facade.Version))
                    {
                        var key = String.Format(CultureInfo.InvariantCulture, "fileversion.{0}", facade.Id);
                        this.VariableCache[key] = facade.Version;
                    }

                    if (!String.IsNullOrEmpty(facade.Language))
                    {
                        var key = String.Format(CultureInfo.InvariantCulture, "filelanguage.{0}", facade.Id);
                        this.VariableCache[key] = facade.Language;
                    }
                }
            }

            // If this is a CLR assembly, load the assembly and get the assembly name information
            if (AssemblyType.DotNetAssembly == facade.AssemblyType)
            {
                try
                {
                    var assemblyName = AssemblyNameReader.ReadAssembly(facade.SourceLineNumber, fileInfo.FullName, version);

                    this.SetMsiAssemblyName(assemblyNameSymbols, facade, "name", assemblyName.Name);
                    this.SetMsiAssemblyName(assemblyNameSymbols, facade, "culture", assemblyName.Culture);
                    this.SetMsiAssemblyName(assemblyNameSymbols, facade, "version", assemblyName.Version);

                    if (!String.IsNullOrEmpty(assemblyName.Architecture))
                    {
                        this.SetMsiAssemblyName(assemblyNameSymbols, facade, "processorArchitecture", assemblyName.Architecture);
                    }
                    // TODO: WiX v3 seemed to do this but not clear it should actually be done.
                    //else if (!String.IsNullOrEmpty(file.WixFile.ProcessorArchitecture))
                    //{
                    //    this.SetMsiAssemblyName(assemblyNameSymbols, file, "processorArchitecture", file.WixFile.ProcessorArchitecture);
                    //}

                    if (assemblyName.StrongNamedSigned)
                    {
                        this.SetMsiAssemblyName(assemblyNameSymbols, facade, "publicKeyToken", assemblyName.PublicKeyToken);
                    }
                    else if (facade.AssemblyApplicationFileRef == null)
                    {
                        throw new WixException(ErrorMessages.GacAssemblyNoStrongName(facade.SourceLineNumber, fileInfo.FullName, facade.ComponentRef));
                    }

                    if (!String.IsNullOrEmpty(assemblyName.FileVersion))
                    {
                        this.SetMsiAssemblyName(assemblyNameSymbols, facade, "fileVersion", assemblyName.FileVersion);
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
            else if (AssemblyType.Win32Assembly == facade.AssemblyType)
            {
                // TODO: Consider passing in the this.FileFacades as an indexed collection instead of searching through
                // all files like this. Even though this is a rare case it looks like we might be able to index the
                // file earlier.
                var fileManifest = this.FileFacades.FirstOrDefault(r => r.Id.Equals(facade.AssemblyManifestFileRef, StringComparison.Ordinal));
                if (null == fileManifest)
                {
                    this.Messaging.Write(ErrorMessages.MissingManifestForWin32Assembly(facade.SourceLineNumber, facade.Id, facade.AssemblyManifestFileRef));
                }

                try
                {
                    var assemblyName = AssemblyNameReader.ReadAssemblyManifest(facade.SourceLineNumber, fileManifest.SourcePath);

                    if (!String.IsNullOrEmpty(assemblyName.Name))
                    {
                        this.SetMsiAssemblyName(assemblyNameSymbols, facade, "name", assemblyName.Name);
                    }

                    if (!String.IsNullOrEmpty(assemblyName.Version))
                    {
                        this.SetMsiAssemblyName(assemblyNameSymbols, facade, "version", assemblyName.Version);
                    }

                    if (!String.IsNullOrEmpty(assemblyName.Type))
                    {
                        this.SetMsiAssemblyName(assemblyNameSymbols, facade, "type", assemblyName.Type);
                    }

                    if (!String.IsNullOrEmpty(assemblyName.Architecture))
                    {
                        this.SetMsiAssemblyName(assemblyNameSymbols, facade, "processorArchitecture", assemblyName.Architecture);
                    }

                    if (!String.IsNullOrEmpty(assemblyName.PublicKeyToken))
                    {
                        this.SetMsiAssemblyName(assemblyNameSymbols, facade, "publicKeyToken", assemblyName.PublicKeyToken);
                    }
                }
                catch (WixException e)
                {
                    this.Messaging.Write(e.Error);
                }
            }
        }

        /// <summary>
        /// Set an MsiAssemblyName row.  If it was directly authored, override the value, otherwise
        /// create a new row.
        /// </summary>
        /// <param name="assemblyNameSymbols">MsiAssemblyName table.</param>
        /// <param name="facade">FileFacade containing the assembly read for the MsiAssemblyName row.</param>
        /// <param name="name">MsiAssemblyName name.</param>
        /// <param name="value">MsiAssemblyName value.</param>
        private void SetMsiAssemblyName(Dictionary<string, MsiAssemblyNameSymbol> assemblyNameSymbols, FileFacade facade, string name, string value)
        {
            // check for null value (this can occur when grabbing the file version from an assembly without one)
            if (String.IsNullOrEmpty(value))
            {
                this.Messaging.Write(WarningMessages.NullMsiAssemblyNameValue(facade.SourceLineNumber, facade.ComponentRef, name));
            }
            else
            {
                // if the assembly will be GAC'd and the name in the file table doesn't match the name in the MsiAssemblyName table, error because the install will fail.
                if ("name" == name && AssemblyType.DotNetAssembly == facade.AssemblyType &&
                    String.IsNullOrEmpty(facade.AssemblyApplicationFileRef) &&
                    !String.Equals(Path.GetFileNameWithoutExtension(facade.FileName), value, StringComparison.OrdinalIgnoreCase))
                {
                    this.Messaging.Write(ErrorMessages.GACAssemblyIdentityWarning(facade.SourceLineNumber, Path.GetFileNameWithoutExtension(facade.FileName), value));
                }

                // override directly authored value
                var lookup = String.Concat(facade.ComponentRef, "/", name);
                if (!assemblyNameSymbols.TryGetValue(lookup, out var assemblyNameSymbol))
                {
                    assemblyNameSymbol = this.Section.AddSymbol(new MsiAssemblyNameSymbol(facade.SourceLineNumber, new Identifier(AccessModifier.Private, facade.ComponentRef, name))
                    {
                        ComponentRef = facade.ComponentRef,
                        Name = name,
                        Value = value,
                    });

                    if (null == facade.AssemblyNames)
                    {
                        facade.AssemblyNames = new List<MsiAssemblyNameSymbol>();
                    }

                    facade.AssemblyNames.Add(assemblyNameSymbol);

                    assemblyNameSymbols.Add(assemblyNameSymbol.Id.Id, assemblyNameSymbol);
                }

                assemblyNameSymbol.Value = value;

                if (this.VariableCache != null)
                {
                    var key = String.Format(CultureInfo.InvariantCulture, "assembly{0}.{1}", name, facade.Id).ToLowerInvariant();
                    this.VariableCache[key] = value;
                }
            }
        }
    }
}
