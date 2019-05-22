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
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Update file information.
    /// </summary>
    internal class UpdateFileFacadesCommand
    {
        public UpdateFileFacadesCommand(IMessaging messaging, IntermediateSection section)
        {
            this.Messaging = messaging;
            this.Section = section;
        }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        public IEnumerable<FileFacade> FileFacades { private get; set; }

        public IEnumerable<FileFacade> UpdateFileFacades { private get; set; }

        public bool OverwriteHash { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public IDictionary<string, string> VariableCache { private get; set; }

        public void Execute()
        {
            foreach (var file in this.UpdateFileFacades)
            {
                this.UpdateFileFacade(file);
            }
        }

        private void UpdateFileFacade(FileFacade file)
        {
            var assemblyNameTuples = new Dictionary<string, MsiAssemblyNameTuple>();
            foreach (var assemblyTuple in this.Section.Tuples.OfType<MsiAssemblyNameTuple>())
            {
                assemblyNameTuples.Add(assemblyTuple.Component_ + "/" + assemblyTuple.Name, assemblyTuple);
            }

            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(file.WixFile.Source.Path);
            }
            catch (ArgumentException)
            {
                this.Messaging.Write(ErrorMessages.InvalidFileName(file.File.SourceLineNumbers, file.WixFile.Source.Path));
                return;
            }
            catch (PathTooLongException)
            {
                this.Messaging.Write(ErrorMessages.InvalidFileName(file.File.SourceLineNumbers, file.WixFile.Source.Path));
                return;
            }
            catch (NotSupportedException)
            {
                this.Messaging.Write(ErrorMessages.InvalidFileName(file.File.SourceLineNumbers, file.WixFile.Source.Path));
                return;
            }

            if (!fileInfo.Exists)
            {
                this.Messaging.Write(ErrorMessages.CannotFindFile(file.File.SourceLineNumbers, file.File.Id.Id, file.File.LongFileName, file.WixFile.Source.Path));
                return;
            }

            using (var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (Int32.MaxValue < fileStream.Length)
                {
                    throw new WixException(ErrorMessages.FileTooLarge(file.File.SourceLineNumbers, file.WixFile.Source.Path));
                }

                file.File.FileSize = Convert.ToInt32(fileStream.Length, CultureInfo.InvariantCulture);
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
                    throw new WixException(ErrorMessages.FileNotFound(file.File.SourceLineNumbers, fileInfo.FullName));
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
                else if (null != file.File.Version)
                {
                    // Search all of the file rows available to see if the specified version is actually a companion file. Yes, this looks
                    // very expensive and you're probably thinking it would be better to create an index of some sort to do an O(1) look up.
                    // That's a reasonable thought but companion file usage is usually pretty rare so we'd be doing something expensive (indexing
                    // all the file rows) for a relatively uncommon situation. Let's not do that.
                    //
                    // Also, if we do not find a matching file identifier then the user provided a default version and is providing a version
                    // for unversioned file. That's allowed but generally a dangerous thing to do so let's point that out to the user.
                    if (!this.FileFacades.Any(r => file.File.Version.Equals(r.File.Id.Id, StringComparison.Ordinal)))
                    {
                        this.Messaging.Write(WarningMessages.DefaultVersionUsedForUnversionedFile(file.File.SourceLineNumbers, file.File.Version, file.File.Id.Id));
                    }
                }
                else
                {
                    if (null != file.File.Language)
                    {
                        this.Messaging.Write(WarningMessages.DefaultLanguageUsedForUnversionedFile(file.File.SourceLineNumbers, file.File.Language, file.File.Id.Id));
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
                            throw new WixException(ErrorMessages.FileNotFound(file.File.SourceLineNumbers, fileInfo.FullName));
                        }
                        else
                        {
                            throw new WixException(ErrorMessages.Win32Exception(e.NativeErrorCode, fileInfo.FullName, e.Message));
                        }
                    }

                    if (null == file.Hash)
                    {
                        file.Hash = new MsiFileHashTuple(file.File.SourceLineNumbers, file.File.Id);
                        this.Section.Tuples.Add(file.Hash);
                    }

                    file.Hash.File_ = file.File.Id.Id;
                    file.Hash.Options = 0;
                    file.Hash.HashPart1 = hash[0];
                    file.Hash.HashPart2 = hash[1];
                    file.Hash.HashPart3 = hash[2];
                    file.Hash.HashPart4 = hash[3];
                }
            }
            else // update the file row with the version and language information.
            {
                // If no version was provided by the user, use the version from the file itself.
                // This is the most common case.
                if (String.IsNullOrEmpty(file.File.Version))
                {
                    file.File.Version = version;
                }
                else if (!this.FileFacades.Any(r => file.File.Version.Equals(r.File.Id.Id, StringComparison.Ordinal))) // this looks expensive, but see explanation below.
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
                    file.File.Version = version;
                }

                if (!String.IsNullOrEmpty(file.File.Language) && String.IsNullOrEmpty(language))
                {
                    this.Messaging.Write(WarningMessages.DefaultLanguageUsedForVersionedFile(file.File.SourceLineNumbers, file.File.Language, file.File.Id.Id));
                }
                else // override the default provided by the user (usually nothing) with the actual language from the file itself.
                {
                    file.File.Language = language;
                }

                // Populate the binder variables for this file information if requested.
                if (null != this.VariableCache)
                {
                    if (!String.IsNullOrEmpty(file.File.Version))
                    {
                        var key = String.Format(CultureInfo.InvariantCulture, "fileversion.{0}", file.File.Id.Id);
                        this.VariableCache[key] = file.File.Version;
                    }

                    if (!String.IsNullOrEmpty(file.File.Language))
                    {
                        var key = String.Format(CultureInfo.InvariantCulture, "filelanguage.{0}", file.File.Id.Id);
                        this.VariableCache[key] = file.File.Language;
                    }
                }
            }

            // If this is a CLR assembly, load the assembly and get the assembly name information
            if (FileAssemblyType.DotNetAssembly == file.WixFile.AssemblyType)
            {
                try
                {
                    var assemblyName = AssemblyNameReader.ReadAssembly(file.File.SourceLineNumbers, fileInfo.FullName, version);

                    this.SetMsiAssemblyName(assemblyNameTuples, file, "name", assemblyName.Name);
                    this.SetMsiAssemblyName(assemblyNameTuples, file, "culture", assemblyName.Culture);
                    this.SetMsiAssemblyName(assemblyNameTuples, file, "version", assemblyName.Version);

                    if (!String.IsNullOrEmpty(assemblyName.Architecture))
                    {
                        this.SetMsiAssemblyName(assemblyNameTuples, file, "processorArchitecture", assemblyName.Architecture);
                    }
                    // TODO: WiX v3 seemed to do this but not clear it should actually be done.
                    //else if (!String.IsNullOrEmpty(file.WixFile.ProcessorArchitecture))
                    //{
                    //    this.SetMsiAssemblyName(assemblyNameTuples, file, "processorArchitecture", file.WixFile.ProcessorArchitecture);
                    //}

                    if (assemblyName.StrongNamedSigned)
                    {
                        this.SetMsiAssemblyName(assemblyNameTuples, file, "publicKeyToken", assemblyName.PublicKeyToken);
                    }
                    else if (file.WixFile.File_AssemblyApplication == null)
                    {
                        throw new WixException(ErrorMessages.GacAssemblyNoStrongName(file.File.SourceLineNumbers, fileInfo.FullName, file.File.Component_));
                    }

                    if (!String.IsNullOrEmpty(assemblyName.FileVersion))
                    {
                        this.SetMsiAssemblyName(assemblyNameTuples, file, "fileVersion", assemblyName.FileVersion);
                    }

                    // add the assembly name to the information cache
                    if (null != this.VariableCache)
                    {
                        this.VariableCache[$"assemblyfullname.{file.File.Id.Id}"] = assemblyName.GetFullName();
                    }
                }
                catch (WixException e)
                {
                    this.Messaging.Write(e.Error);
                }
            }
            else if (FileAssemblyType.Win32Assembly == file.WixFile.AssemblyType)
            {
                // TODO: Consider passing in the this.FileFacades as an indexed collection instead of searching through
                // all files like this. Even though this is a rare case it looks like we might be able to index the
                // file earlier.
                var fileManifest = this.FileFacades.FirstOrDefault(r => r.File.Id.Id.Equals(file.WixFile.File_AssemblyManifest, StringComparison.Ordinal));
                if (null == fileManifest)
                {
                    this.Messaging.Write(ErrorMessages.MissingManifestForWin32Assembly(file.File.SourceLineNumbers, file.File.Id.Id, file.WixFile.File_AssemblyManifest));
                }

                try
                {
                    var assemblyName = AssemblyNameReader.ReadAssemblyManifest(file.File.SourceLineNumbers, fileManifest.WixFile.Source.Path);

                    if (!String.IsNullOrEmpty(assemblyName.Name))
                    {
                        this.SetMsiAssemblyName(assemblyNameTuples, file, "name", assemblyName.Name);
                    }

                    if (!String.IsNullOrEmpty(assemblyName.Version))
                    {
                        this.SetMsiAssemblyName(assemblyNameTuples, file, "version", assemblyName.Version);
                    }

                    if (!String.IsNullOrEmpty(assemblyName.Type))
                    {
                        this.SetMsiAssemblyName(assemblyNameTuples, file, "type", assemblyName.Type);
                    }

                    if (!String.IsNullOrEmpty(assemblyName.Architecture))
                    {
                        this.SetMsiAssemblyName(assemblyNameTuples, file, "processorArchitecture", assemblyName.Architecture);
                    }

                    if (!String.IsNullOrEmpty(assemblyName.PublicKeyToken))
                    {
                        this.SetMsiAssemblyName(assemblyNameTuples, file, "publicKeyToken", assemblyName.PublicKeyToken);
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
        /// <param name="assemblyNameTuples">MsiAssemblyName table.</param>
        /// <param name="file">FileFacade containing the assembly read for the MsiAssemblyName row.</param>
        /// <param name="name">MsiAssemblyName name.</param>
        /// <param name="value">MsiAssemblyName value.</param>
        private void SetMsiAssemblyName(Dictionary<string, MsiAssemblyNameTuple> assemblyNameTuples, FileFacade file, string name, string value)
        {
            // check for null value (this can occur when grabbing the file version from an assembly without one)
            if (String.IsNullOrEmpty(value))
            {
                this.Messaging.Write(WarningMessages.NullMsiAssemblyNameValue(file.File.SourceLineNumbers, file.File.Component_, name));
            }
            else
            {
                // if the assembly will be GAC'd and the name in the file table doesn't match the name in the MsiAssemblyName table, error because the install will fail.
                if ("name" == name && FileAssemblyType.DotNetAssembly == file.WixFile.AssemblyType &&
                    String.IsNullOrEmpty(file.WixFile.File_AssemblyApplication) &&
                    !String.Equals(Path.GetFileNameWithoutExtension(file.File.LongFileName), value, StringComparison.OrdinalIgnoreCase))
                {
                    this.Messaging.Write(ErrorMessages.GACAssemblyIdentityWarning(file.File.SourceLineNumbers, Path.GetFileNameWithoutExtension(file.File.LongFileName), value));
                }

                // override directly authored value
                var lookup = String.Concat(file.File.Component_, "/", name);
                if (!assemblyNameTuples.TryGetValue(lookup, out var assemblyNameRow))
                {
                    assemblyNameRow = new MsiAssemblyNameTuple(file.File.SourceLineNumbers);
                    assemblyNameRow.Component_ = file.File.Component_;
                    assemblyNameRow.Name = name;
                    assemblyNameRow.Value = value;

                    if (null == file.AssemblyNames)
                    {
                        file.AssemblyNames = new List<MsiAssemblyNameTuple>();
                    }

                    file.AssemblyNames.Add(assemblyNameRow);
                    this.Section.Tuples.Add(assemblyNameRow);
                }

                assemblyNameRow.Value = value;

                if (this.VariableCache != null)
                {
                    var key = String.Format(CultureInfo.InvariantCulture, "assembly{0}.{1}", name, file.File.Id.Id).ToLowerInvariant();
                    this.VariableCache[key] = value;
                }
            }
        }
    }
}
