// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Databases
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.XPath;
    using WixToolset.Clr.Interop;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Msi;

    /// <summary>
    /// Update file information.
    /// </summary>
    internal class UpdateFileFacadesCommand : ICommand
    {
        public IEnumerable<FileFacade> FileFacades { private get; set; }

        public IEnumerable<FileFacade> UpdateFileFacades { private get; set; }

        public string ModularizationGuid { private get; set; }

        public Output Output { private get; set; }

        public bool OverwriteHash { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public IDictionary<string, string> VariableCache { private get; set; }

        public void Execute()
        {
            foreach (FileFacade file in this.UpdateFileFacades)
            {
                this.UpdateFileFacade(file);
            }
        }

        private void UpdateFileFacade(FileFacade file)
        {
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(file.WixFile.Source);
            }
            catch (ArgumentException)
            {
                Messaging.Instance.OnMessage(WixErrors.InvalidFileName(file.File.SourceLineNumbers, file.WixFile.Source));
                return;
            }
            catch (PathTooLongException)
            {
                Messaging.Instance.OnMessage(WixErrors.InvalidFileName(file.File.SourceLineNumbers, file.WixFile.Source));
                return;
            }
            catch (NotSupportedException)
            {
                Messaging.Instance.OnMessage(WixErrors.InvalidFileName(file.File.SourceLineNumbers, file.WixFile.Source));
                return;
            }

            if (!fileInfo.Exists)
            {
                Messaging.Instance.OnMessage(WixErrors.CannotFindFile(file.File.SourceLineNumbers, file.File.File, file.File.FileName, file.WixFile.Source));
                return;
            }

            using (FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (Int32.MaxValue < fileStream.Length)
                {
                    throw new WixException(WixErrors.FileTooLarge(file.File.SourceLineNumbers, file.WixFile.Source));
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
                    throw new WixException(WixErrors.FileNotFound(file.File.SourceLineNumbers, fileInfo.FullName));
                }
                else
                {
                    throw new WixException(WixErrors.Win32Exception(e.NativeErrorCode, e.Message));
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
                    if (!this.FileFacades.Any(r => file.File.Version.Equals(r.File.File, StringComparison.Ordinal)))
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DefaultVersionUsedForUnversionedFile(file.File.SourceLineNumbers, file.File.Version, file.File.File));
                    }
                }
                else
                {
                    if (null != file.File.Language)
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DefaultLanguageUsedForUnversionedFile(file.File.SourceLineNumbers, file.File.Language, file.File.File));
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
                            throw new WixException(WixErrors.FileNotFound(file.File.SourceLineNumbers, fileInfo.FullName));
                        }
                        else
                        {
                            throw new WixException(WixErrors.Win32Exception(e.NativeErrorCode, fileInfo.FullName, e.Message));
                        }
                    }

                    if (null == file.Hash)
                    {
                        Table msiFileHashTable = this.Output.EnsureTable(this.TableDefinitions["MsiFileHash"]);
                        file.Hash = msiFileHashTable.CreateRow(file.File.SourceLineNumbers);
                    }

                    file.Hash[0] = file.File.File;
                    file.Hash[1] = 0;
                    file.Hash[2] = hash[0];
                    file.Hash[3] = hash[1];
                    file.Hash[4] = hash[2];
                    file.Hash[5] = hash[3];
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
                else if (!this.FileFacades.Any(r => file.File.Version.Equals(r.File.File, StringComparison.Ordinal))) // this looks expensive, but see explanation below.
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
                    Messaging.Instance.OnMessage(WixWarnings.DefaultLanguageUsedForVersionedFile(file.File.SourceLineNumbers, file.File.Language, file.File.File));
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
                        string key = String.Format(CultureInfo.InvariantCulture, "fileversion.{0}", BindDatabaseCommand.Demodularize(this.Output.Type, this.ModularizationGuid, file.File.File));
                        this.VariableCache[key] = file.File.Version;
                    }

                    if (!String.IsNullOrEmpty(file.File.Language))
                    {
                        string key = String.Format(CultureInfo.InvariantCulture, "filelanguage.{0}", BindDatabaseCommand.Demodularize(this.Output.Type, ModularizationGuid, file.File.File));
                        this.VariableCache[key] = file.File.Language;
                    }
                }
            }

            // If this is a CLR assembly, load the assembly and get the assembly name information
            if (FileAssemblyType.DotNetAssembly == file.WixFile.AssemblyType)
            {
                bool targetNetfx1 = false;
                StringDictionary assemblyNameValues = new StringDictionary();

                ClrInterop.IReferenceIdentity referenceIdentity = null;
                Guid referenceIdentityGuid = ClrInterop.ReferenceIdentityGuid;
                uint result = ClrInterop.GetAssemblyIdentityFromFile(fileInfo.FullName, ref referenceIdentityGuid, out referenceIdentity);
                if (0 == result && null != referenceIdentity)
                {
                    string imageRuntimeVersion = referenceIdentity.GetAttribute(null, "ImageRuntimeVersion");
                    if (null != imageRuntimeVersion)
                    {
                        targetNetfx1 = imageRuntimeVersion.StartsWith("v1", StringComparison.OrdinalIgnoreCase);
                    }

                    string culture = referenceIdentity.GetAttribute(null, "Culture") ?? "neutral";
                    assemblyNameValues.Add("Culture", culture);

                    string name = referenceIdentity.GetAttribute(null, "Name");
                    if (null != name)
                    {
                        assemblyNameValues.Add("Name", name);
                    }

                    string processorArchitecture = referenceIdentity.GetAttribute(null, "ProcessorArchitecture");
                    if (null != processorArchitecture)
                    {
                        assemblyNameValues.Add("ProcessorArchitecture", processorArchitecture);
                    }

                    string publicKeyToken = referenceIdentity.GetAttribute(null, "PublicKeyToken");
                    if (null != publicKeyToken)
                    {
                        bool publicKeyIsNeutral = (String.Equals(publicKeyToken, "neutral", StringComparison.OrdinalIgnoreCase));

                        // Managed code expects "null" instead of "neutral", and
                        // this won't be installed to the GAC since it's not signed anyway.
                        assemblyNameValues.Add("publicKeyToken", publicKeyIsNeutral ? "null" : publicKeyToken.ToUpperInvariant());
                        assemblyNameValues.Add("publicKeyTokenPreservedCase", publicKeyIsNeutral ? "null" : publicKeyToken);
                    }
                    else if (file.WixFile.AssemblyApplication == null)
                    {
                        throw new WixException(WixErrors.GacAssemblyNoStrongName(file.File.SourceLineNumbers, fileInfo.FullName, file.File.Component));
                    }

                    string assemblyVersion = referenceIdentity.GetAttribute(null, "Version");
                    if (null != version)
                    {
                        assemblyNameValues.Add("Version", assemblyVersion);
                    }
                }
                else
                {
                    Messaging.Instance.OnMessage(WixErrors.InvalidAssemblyFile(file.File.SourceLineNumbers, fileInfo.FullName, String.Format(CultureInfo.InvariantCulture, "HRESULT: 0x{0:x8}", result)));
                    return;
                }

                Table assemblyNameTable = this.Output.EnsureTable(this.TableDefinitions["MsiAssemblyName"]);
                if (assemblyNameValues.ContainsKey("name"))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "name", assemblyNameValues["name"]);
                }

                if (!String.IsNullOrEmpty(version))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "fileVersion", version);
                }

                if (assemblyNameValues.ContainsKey("version"))
                {
                    string assemblyVersion = assemblyNameValues["version"];

                    if (!targetNetfx1)
                    {
                        // There is a bug in v1 fusion that requires the assembly's "version" attribute
                        // to be equal to or longer than the "fileVersion" in length when its present;
                        // the workaround is to prepend zeroes to the last version number in the assembly
                        // version.
                        if (null != version && version.Length > assemblyVersion.Length)
                        {
                            string padding = new string('0', version.Length - assemblyVersion.Length);
                            string[] assemblyVersionNumbers = assemblyVersion.Split('.');

                            if (assemblyVersionNumbers.Length > 0)
                            {
                                assemblyVersionNumbers[assemblyVersionNumbers.Length - 1] = String.Concat(padding, assemblyVersionNumbers[assemblyVersionNumbers.Length - 1]);
                                assemblyVersion = String.Join(".", assemblyVersionNumbers);
                            }
                        }
                    }

                    this.SetMsiAssemblyName(assemblyNameTable, file, "version", assemblyVersion);
                }

                if (assemblyNameValues.ContainsKey("culture"))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "culture", assemblyNameValues["culture"]);
                }

                if (assemblyNameValues.ContainsKey("publicKeyToken"))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "publicKeyToken", assemblyNameValues["publicKeyToken"]);
                }

                if (!String.IsNullOrEmpty(file.WixFile.ProcessorArchitecture))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "processorArchitecture", file.WixFile.ProcessorArchitecture);
                }

                if (assemblyNameValues.ContainsKey("processorArchitecture"))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "processorArchitecture", assemblyNameValues["processorArchitecture"]);
                }

                // add the assembly name to the information cache
                if (null != this.VariableCache)
                {
                    string fileId = BindDatabaseCommand.Demodularize(this.Output.Type, this.ModularizationGuid, file.File.File);
                    string key = String.Concat("assemblyfullname.", fileId);
                    string assemblyName = String.Concat(assemblyNameValues["name"], ", version=", assemblyNameValues["version"], ", culture=", assemblyNameValues["culture"], ", publicKeyToken=", String.IsNullOrEmpty(assemblyNameValues["publicKeyToken"]) ? "null" : assemblyNameValues["publicKeyToken"]);
                    if (assemblyNameValues.ContainsKey("processorArchitecture"))
                    {
                        assemblyName = String.Concat(assemblyName, ", processorArchitecture=", assemblyNameValues["processorArchitecture"]);
                    }

                    this.VariableCache[key] = assemblyName;

                    // Add entries with the preserved case publicKeyToken
                    string pcAssemblyNameKey = String.Concat("assemblyfullnamepreservedcase.", fileId);
                    this.VariableCache[pcAssemblyNameKey] = (assemblyNameValues["publicKeyToken"] == assemblyNameValues["publicKeyTokenPreservedCase"]) ? assemblyName : assemblyName.Replace(assemblyNameValues["publicKeyToken"], assemblyNameValues["publicKeyTokenPreservedCase"]);

                    string pcPublicKeyTokenKey = String.Concat("assemblypublickeytokenpreservedcase.", fileId);
                    this.VariableCache[pcPublicKeyTokenKey] = assemblyNameValues["publicKeyTokenPreservedCase"];
                }
            }
            else if (FileAssemblyType.Win32Assembly == file.WixFile.AssemblyType)
            {
                // TODO: Consider passing in the this.FileFacades as an indexed collection instead of searching through
                // all files like this. Even though this is a rare case it looks like we might be able to index the
                // file earlier.
                FileFacade fileManifest = this.FileFacades.SingleOrDefault(r => r.File.File.Equals(file.WixFile.AssemblyManifest, StringComparison.Ordinal));
                if (null == fileManifest)
                {
                    Messaging.Instance.OnMessage(WixErrors.MissingManifestForWin32Assembly(file.File.SourceLineNumbers, file.File.File, file.WixFile.AssemblyManifest));
                }

                string win32Type = null;
                string win32Name = null;
                string win32Version = null;
                string win32ProcessorArchitecture = null;
                string win32PublicKeyToken = null;

                // loading the dom is expensive we want more performant APIs than the DOM
                // Navigator is cheaper than dom.  Perhaps there is a cheaper API still.
                try
                {
                    XPathDocument doc = new XPathDocument(fileManifest.WixFile.Source);
                    XPathNavigator nav = doc.CreateNavigator();
                    nav.MoveToRoot();

                    // this assumes a particular schema for a win32 manifest and does not
                    // provide error checking if the file does not conform to schema.
                    // The fallback case here is that nothing is added to the MsiAssemblyName
                    // table for an out of tolerance Win32 manifest.  Perhaps warnings needed.
                    if (nav.MoveToFirstChild())
                    {
                        while (nav.NodeType != XPathNodeType.Element || nav.Name != "assembly")
                        {
                            nav.MoveToNext();
                        }

                        if (nav.MoveToFirstChild())
                        {
                            bool hasNextSibling = true;
                            while (nav.NodeType != XPathNodeType.Element || nav.Name != "assemblyIdentity" && hasNextSibling)
                            {
                                hasNextSibling = nav.MoveToNext();
                            }
                            if (!hasNextSibling)
                            {
                                Messaging.Instance.OnMessage(WixErrors.InvalidManifestContent(file.File.SourceLineNumbers, fileManifest.WixFile.Source));
                                return;
                            }

                            if (nav.MoveToAttribute("type", String.Empty))
                            {
                                win32Type = nav.Value;
                                nav.MoveToParent();
                            }

                            if (nav.MoveToAttribute("name", String.Empty))
                            {
                                win32Name = nav.Value;
                                nav.MoveToParent();
                            }

                            if (nav.MoveToAttribute("version", String.Empty))
                            {
                                win32Version = nav.Value;
                                nav.MoveToParent();
                            }

                            if (nav.MoveToAttribute("processorArchitecture", String.Empty))
                            {
                                win32ProcessorArchitecture = nav.Value;
                                nav.MoveToParent();
                            }

                            if (nav.MoveToAttribute("publicKeyToken", String.Empty))
                            {
                                win32PublicKeyToken = nav.Value;
                                nav.MoveToParent();
                            }
                        }
                    }
                }
                catch (FileNotFoundException fe)
                {
                    Messaging.Instance.OnMessage(WixErrors.FileNotFound(new SourceLineNumber(fileManifest.WixFile.Source), fe.FileName, "AssemblyManifest"));
                }
                catch (XmlException xe)
                {
                    Messaging.Instance.OnMessage(WixErrors.InvalidXml(new SourceLineNumber(fileManifest.WixFile.Source), "manifest", xe.Message));
                }

                Table assemblyNameTable = this.Output.EnsureTable(this.TableDefinitions["MsiAssemblyName"]);
                if (!String.IsNullOrEmpty(win32Name))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "name", win32Name);
                }

                if (!String.IsNullOrEmpty(win32Version))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "version", win32Version);
                }

                if (!String.IsNullOrEmpty(win32Type))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "type", win32Type);
                }

                if (!String.IsNullOrEmpty(win32ProcessorArchitecture))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "processorArchitecture", win32ProcessorArchitecture);
                }

                if (!String.IsNullOrEmpty(win32PublicKeyToken))
                {
                    this.SetMsiAssemblyName(assemblyNameTable, file, "publicKeyToken", win32PublicKeyToken);
                }
            }
        }

        /// <summary>
        /// Set an MsiAssemblyName row.  If it was directly authored, override the value, otherwise
        /// create a new row.
        /// </summary>
        /// <param name="assemblyNameTable">MsiAssemblyName table.</param>
        /// <param name="file">FileFacade containing the assembly read for the MsiAssemblyName row.</param>
        /// <param name="name">MsiAssemblyName name.</param>
        /// <param name="value">MsiAssemblyName value.</param>
        private void SetMsiAssemblyName(Table assemblyNameTable, FileFacade file, string name, string value)
        {
            // check for null value (this can occur when grabbing the file version from an assembly without one)
            if (String.IsNullOrEmpty(value))
            {
                Messaging.Instance.OnMessage(WixWarnings.NullMsiAssemblyNameValue(file.File.SourceLineNumbers, file.File.Component, name));
            }
            else
            {
                Row assemblyNameRow = null;

                // override directly authored value
                foreach (Row row in assemblyNameTable.Rows)
                {
                    if ((string)row[0] == file.File.Component && (string)row[1] == name)
                    {
                        assemblyNameRow = row;
                        break;
                    }
                }

                // if the assembly will be GAC'd and the name in the file table doesn't match the name in the MsiAssemblyName table, error because the install will fail.
                if ("name" == name && FileAssemblyType.DotNetAssembly == file.WixFile.AssemblyType &&
                    String.IsNullOrEmpty(file.WixFile.AssemblyApplication) &&
                    !String.Equals(Path.GetFileNameWithoutExtension(file.File.LongFileName), value, StringComparison.OrdinalIgnoreCase))
                {
                    Messaging.Instance.OnMessage(WixErrors.GACAssemblyIdentityWarning(file.File.SourceLineNumbers, Path.GetFileNameWithoutExtension(file.File.LongFileName), value));
                }

                if (null == assemblyNameRow)
                {
                    assemblyNameRow = assemblyNameTable.CreateRow(file.File.SourceLineNumbers);
                    assemblyNameRow[0] = file.File.Component;
                    assemblyNameRow[1] = name;
                    assemblyNameRow[2] = value;

                    // put the MsiAssemblyName row in the same section as the related File row
                    assemblyNameRow.SectionId = file.File.SectionId;

                    if (null == file.AssemblyNames)
                    {
                        file.AssemblyNames = new List<Row>();
                    }

                    file.AssemblyNames.Add(assemblyNameRow);
                }
                else
                {
                    assemblyNameRow[2] = value;
                }

                if (this.VariableCache != null)
                {
                    string key = String.Format(CultureInfo.InvariantCulture, "assembly{0}.{1}", name, BindDatabaseCommand.Demodularize(this.Output.Type, this.ModularizationGuid, file.File.File)).ToLowerInvariant();
                    this.VariableCache[key] = (string)assemblyNameRow[2];
                }
            }
        }
    }
}
