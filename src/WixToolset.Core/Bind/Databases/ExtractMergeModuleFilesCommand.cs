// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Databases
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using WixToolset.Cab;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.MergeMod;
    using WixToolset.Msi;
    using WixToolset.Core.Native;

    /// <summary>
    /// Retrieve files information and extract them from merge modules.
    /// </summary>
    internal class ExtractMergeModuleFilesCommand : ICommand
    {
        public IEnumerable<FileFacade> FileFacades { private get; set; }

        public Table FileTable { private get; set; }

        public Table WixFileTable { private get; set; }

        public Table WixMergeTable { private get; set; }

        public int OutputInstallerVersion { private get; set; }

        public bool SuppressLayout { private get; set; }

        public string TempFilesLocation { private get; set; }

        public IEnumerable<FileFacade> MergeModulesFileFacades { get; private set; }

        public void Execute()
        {
            List<FileFacade> mergeModulesFileFacades = new List<FileFacade>();

            IMsmMerge2 merge = MsmInterop.GetMsmMerge();

            // Index all of the file rows to be able to detect collisions with files in the Merge Modules.
            // It may seem a bit expensive to build up this index solely for the purpose of checking collisions
            // and you may be thinking, "Surely, we must need the file rows indexed elsewhere." It turns out
            // there are other cases where we need all the file rows indexed, however they are not common cases.
            // Now since Merge Modules are already slow and generally less desirable than .wixlibs we'll let
            // this case be slightly more expensive because the cost of maintaining an indexed file row collection
            // is a lot more costly for the common cases.
            Dictionary<string, FileFacade> indexedFileFacades = this.FileFacades.ToDictionary(f => f.File.File, StringComparer.Ordinal);

            foreach (WixMergeRow wixMergeRow in this.WixMergeTable.Rows)
            {
                bool containsFiles = this.CreateFacadesForMergeModuleFiles(wixMergeRow, mergeModulesFileFacades, indexedFileFacades);

                // If the module has files and creating layout
                if (containsFiles && !this.SuppressLayout)
                {
                    this.ExtractFilesFromMergeModule(merge, wixMergeRow);
                }
            }

            this.MergeModulesFileFacades = mergeModulesFileFacades;
        }

        private bool CreateFacadesForMergeModuleFiles(WixMergeRow wixMergeRow, List<FileFacade> mergeModulesFileFacades, Dictionary<string, FileFacade> indexedFileFacades)
        {
            bool containsFiles = false;

            try
            {
                // read the module's File table to get its FileMediaInformation entries and gather any other information needed from the module.
                using (Database db = new Database(wixMergeRow.SourceFile, OpenDatabase.ReadOnly))
                {
                    if (db.TableExists("File") && db.TableExists("Component"))
                    {
                        Dictionary<string, FileFacade> uniqueModuleFileIdentifiers = new Dictionary<string, FileFacade>(StringComparer.OrdinalIgnoreCase);

                        using (View view = db.OpenExecuteView("SELECT `File`, `Directory_` FROM `File`, `Component` WHERE `Component_`=`Component`"))
                        {
                            // add each file row from the merge module into the file row collection (check for errors along the way)
                            while (true)
                            {
                                using (Record record = view.Fetch())
                                {
                                    if (null == record)
                                    {
                                        break;
                                    }

                                    // NOTE: this is very tricky - the merge module file rows are not added to the
                                    // file table because they should not be created via idt import.  Instead, these
                                    // rows are created by merging in the actual modules.
                                    FileRow fileRow = (FileRow)this.FileTable.CreateRow(wixMergeRow.SourceLineNumbers, false);
                                    fileRow.File = record[1];
                                    fileRow.Compressed = wixMergeRow.FileCompression;

                                    WixFileRow wixFileRow = (WixFileRow)this.WixFileTable.CreateRow(wixMergeRow.SourceLineNumbers, false);
                                    wixFileRow.Directory = record[2];
                                    wixFileRow.DiskId = wixMergeRow.DiskId;
                                    wixFileRow.PatchGroup = -1;
                                    wixFileRow.Source = String.Concat(this.TempFilesLocation, Path.DirectorySeparatorChar, "MergeId.", wixMergeRow.Number.ToString(CultureInfo.InvariantCulture), Path.DirectorySeparatorChar, record[1]);

                                    FileFacade mergeModuleFileFacade = new FileFacade(true, fileRow, wixFileRow);

                                    FileFacade collidingFacade;

                                    // If case-sensitive collision with another merge module or a user-authored file identifier.
                                    if (indexedFileFacades.TryGetValue(mergeModuleFileFacade.File.File, out collidingFacade))
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.DuplicateModuleFileIdentifier(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, collidingFacade.File.File));
                                    }
                                    else if (uniqueModuleFileIdentifiers.TryGetValue(mergeModuleFileFacade.File.File, out collidingFacade)) // case-insensitive collision with another file identifier in the same merge module
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.DuplicateModuleCaseInsensitiveFileIdentifier(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, mergeModuleFileFacade.File.File, collidingFacade.File.File));
                                    }
                                    else // no collision
                                    {
                                        mergeModulesFileFacades.Add(mergeModuleFileFacade);

                                        // Keep updating the indexes as new rows are added.
                                        indexedFileFacades.Add(mergeModuleFileFacade.File.File, mergeModuleFileFacade);
                                        uniqueModuleFileIdentifiers.Add(mergeModuleFileFacade.File.File, mergeModuleFileFacade);
                                    }

                                    containsFiles = true;
                                }
                            }
                        }
                    }

                    // Get the summary information to detect the Schema
                    using (SummaryInformation summaryInformation = new SummaryInformation(db))
                    {
                        string moduleInstallerVersionString = summaryInformation.GetProperty(14);

                        try
                        {
                            int moduleInstallerVersion = Convert.ToInt32(moduleInstallerVersionString, CultureInfo.InvariantCulture);
                            if (moduleInstallerVersion > this.OutputInstallerVersion)
                            {
                                Messaging.Instance.OnMessage(WixWarnings.InvalidHigherInstallerVersionInModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, moduleInstallerVersion, this.OutputInstallerVersion));
                            }
                        }
                        catch (FormatException)
                        {
                            throw new WixException(WixErrors.MissingOrInvalidModuleInstallerVersion(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.SourceFile, moduleInstallerVersionString));
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                throw new WixException(WixErrors.FileNotFound(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile));
            }
            catch (Win32Exception)
            {
                throw new WixException(WixErrors.CannotOpenMergeModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.SourceFile));
            }

            return containsFiles;
        }

        private void ExtractFilesFromMergeModule(IMsmMerge2 merge, WixMergeRow wixMergeRow)
        {
            bool moduleOpen = false;
            short mergeLanguage;

            try
            {
                mergeLanguage = Convert.ToInt16(wixMergeRow.Language, CultureInfo.InvariantCulture);
            }
            catch (System.FormatException)
            {
                Messaging.Instance.OnMessage(WixErrors.InvalidMergeLanguage(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.Language));
                return;
            }

            try
            {
                merge.OpenModule(wixMergeRow.SourceFile, mergeLanguage);
                moduleOpen = true;

                string safeMergeId = wixMergeRow.Number.ToString(CultureInfo.InvariantCulture.NumberFormat);

                // extract the module cabinet, then explode all of the files to a temp directory
                string moduleCabPath = String.Concat(this.TempFilesLocation, Path.DirectorySeparatorChar, safeMergeId, ".module.cab");
                merge.ExtractCAB(moduleCabPath);

                string mergeIdPath = String.Concat(this.TempFilesLocation, Path.DirectorySeparatorChar, "MergeId.", safeMergeId);
                Directory.CreateDirectory(mergeIdPath);

                using (WixExtractCab extractCab = new WixExtractCab())
                {
                    try
                    {
                        extractCab.Extract(moduleCabPath, mergeIdPath);
                    }
                    catch (FileNotFoundException)
                    {
                        throw new WixException(WixErrors.CabFileDoesNotExist(moduleCabPath, wixMergeRow.SourceFile, mergeIdPath));
                    }
                    catch
                    {
                        throw new WixException(WixErrors.CabExtractionFailed(moduleCabPath, wixMergeRow.SourceFile, mergeIdPath));
                    }
                }
            }
            catch (COMException ce)
            {
                throw new WixException(WixErrors.UnableToOpenModule(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile, ce.Message));
            }
            finally
            {
                if (moduleOpen)
                {
                    merge.CloseModule();
                }
            }
        }
    }
}
