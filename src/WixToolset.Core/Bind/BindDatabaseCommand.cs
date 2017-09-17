// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WixToolset.Bind.Databases;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;
    using WixToolset.Msi;

    /// <summary>
    /// Binds a databse.
    /// </summary>
    internal class BindDatabaseCommand : ICommand
    {
        // As outlined in RFC 4122, this is our namespace for generating name-based (version 3) UUIDs.
        private static readonly Guid WixComponentGuidNamespace = new Guid("{3064E5C6-FB63-4FE9-AC49-E446A792EFA5}");

        public int Codepage { private get; set; }

        public int CabbingThreadCount { private get; set; }

        public CompressionLevel DefaultCompressionLevel { private get; set; }

        public bool DeltaBinaryPatch { get; set; }

        public IEnumerable<IBinderExtension> Extensions { private get; set; }

        public BinderFileManagerCore FileManagerCore { private get; set; }

        public IEnumerable<IBinderFileManager> FileManagers { private get; set; }

        public IEnumerable<InspectorExtension> InspectorExtensions { private get; set; }

        public Localizer Localizer { private get; set; }

        public string PdbFile { private get; set; }

        public Output Output { private get; set; }

        public string OutputPath { private get; set; }

        public bool SuppressAddingValidationRows { private get; set; }

        public bool SuppressLayout { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public string TempFilesLocation { private get; set; }

        public Validator Validator { private get; set; }

        public WixVariableResolver WixVariableResolver { private get; set; }

        public IEnumerable<FileTransfer> FileTransfers { get; private set; }

        public IEnumerable<string> ContentFilePaths { get; private set; }

        public void Execute()
        {
            List<FileTransfer> fileTransfers = new List<FileTransfer>();

            HashSet<string> suppressedTableNames = new HashSet<string>();

            // Localize fields, resolve wix variables, and resolve file paths.
            ExtractEmbeddedFiles filesWithEmbeddedFiles = new ExtractEmbeddedFiles();

            IEnumerable<DelayedField> delayedFields;
            {
                ResolveFieldsCommand command = new ResolveFieldsCommand();
                command.Tables = this.Output.Tables;
                command.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
                command.FileManagerCore = this.FileManagerCore;
                command.FileManagers = this.FileManagers;
                command.SupportDelayedResolution = true;
                command.TempFilesLocation = this.TempFilesLocation;
                command.WixVariableResolver = this.WixVariableResolver;
                command.Execute();

                delayedFields = command.DelayedFields;
            }

            if (OutputType.Patch == this.Output.Type)
            {
                foreach (SubStorage transform in this.Output.SubStorages)
                {
                    ResolveFieldsCommand command = new ResolveFieldsCommand();
                    command.Tables = transform.Data.Tables;
                    command.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
                    command.FileManagerCore = this.FileManagerCore;
                    command.FileManagers = this.FileManagers;
                    command.SupportDelayedResolution = false;
                    command.TempFilesLocation = this.TempFilesLocation;
                    command.WixVariableResolver = this.WixVariableResolver;
                    command.Execute();
                }
            }

            // If there are any fields to resolve later, create the cache to populate during bind.
            IDictionary<string, string> variableCache = null;
            if (delayedFields.Any())
            {
                variableCache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }

            this.LocalizeUI(this.Output.Tables);

            // Process the summary information table before the other tables.
            bool compressed;
            bool longNames;
            int installerVersion;
            string modularizationGuid;
            {
                BindSummaryInfoCommand command = new BindSummaryInfoCommand();
                command.Output = this.Output;
                command.Execute();

                compressed = command.Compressed;
                longNames = command.LongNames;
                installerVersion = command.InstallerVersion;
                modularizationGuid = command.ModularizationGuid;
            }

            // Stop processing if an error previously occurred.
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // Modularize identifiers and add tables with real streams to the import tables.
            if (OutputType.Module == this.Output.Type)
            {
                // Gather all the suppress modularization identifiers
                HashSet<string> suppressModularizationIdentifiers = null;
                Table wixSuppressModularizationTable = this.Output.Tables["WixSuppressModularization"];
                if (null != wixSuppressModularizationTable)
                {
                    suppressModularizationIdentifiers = new HashSet<string>(wixSuppressModularizationTable.Rows.Select(row => (string)row[0]));
                }

                foreach (Table table in this.Output.Tables)
                {
                    table.Modularize(modularizationGuid, suppressModularizationIdentifiers);
                }
            }

            // This must occur after all variables and source paths have been resolved and after modularization.
            List<FileFacade> fileFacades;
            {
                GetFileFacadesCommand command = new GetFileFacadesCommand();
                command.FileTable = this.Output.Tables["File"];
                command.WixFileTable = this.Output.Tables["WixFile"];
                command.WixDeltaPatchFileTable = this.Output.Tables["WixDeltaPatchFile"];
                command.WixDeltaPatchSymbolPathsTable = this.Output.Tables["WixDeltaPatchSymbolPaths"];
                command.Execute();

                fileFacades = command.FileFacades;
            }

            ////if (OutputType.Patch == this.Output.Type)
            ////{
            ////    foreach (SubStorage substorage in this.Output.SubStorages)
            ////    {
            ////        Output transform = substorage.Data;

            ////        ResolveFieldsCommand command = new ResolveFieldsCommand();
            ////        command.Tables = transform.Tables;
            ////        command.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
            ////        command.FileManagerCore = this.FileManagerCore;
            ////        command.FileManagers = this.FileManagers;
            ////        command.SupportDelayedResolution = false;
            ////        command.TempFilesLocation = this.TempFilesLocation;
            ////        command.WixVariableResolver = this.WixVariableResolver;
            ////        command.Execute();

            ////        this.MergeUnrealTables(transform.Tables);
            ////    }
            ////}

            {
                CreateSpecialPropertiesCommand command = new CreateSpecialPropertiesCommand();
                command.PropertyTable = this.Output.Tables["Property"];
                command.WixPropertyTable = this.Output.Tables["WixProperty"];
                command.Execute();
            }

            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // Add binder variables for all properties.
            Table propertyTable = this.Output.Tables["Property"];
            if (null != propertyTable)
            {
                foreach (PropertyRow propertyRow in propertyTable.Rows)
                {
                    // Set the ProductCode if it is to be generated.
                    if (OutputType.Product == this.Output.Type && "ProductCode".Equals(propertyRow.Property, StringComparison.Ordinal) && "*".Equals(propertyRow.Value, StringComparison.Ordinal))
                    {
                        propertyRow.Value = Common.GenerateGuid();

                        // Update the target ProductCode in any instance transforms.
                        foreach (SubStorage subStorage in this.Output.SubStorages)
                        {
                            Output subStorageOutput = subStorage.Data;
                            if (OutputType.Transform != subStorageOutput.Type)
                            {
                                continue;
                            }

                            Table instanceSummaryInformationTable = subStorageOutput.Tables["_SummaryInformation"];
                            foreach (Row row in instanceSummaryInformationTable.Rows)
                            {
                                if ((int)SummaryInformation.Transform.ProductCodes == row.FieldAsInteger(0))
                                {
                                    row[1] = row.FieldAsString(1).Replace("*", propertyRow.Value);
                                    break;
                                }
                            }
                        }
                    }

                    // Add the property name and value to the variableCache.
                    if (null != variableCache)
                    {
                        string key = String.Concat("property.", Demodularize(this.Output.Type, modularizationGuid, propertyRow.Property));
                        variableCache[key] = propertyRow.Value;
                    }
                }
            }

            // Extract files that come from cabinet files (this does not extract files from merge modules).
            {
                ExtractEmbeddedFilesCommand command = new ExtractEmbeddedFilesCommand();
                command.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
                command.Execute();
            }

            if (OutputType.Product == this.Output.Type)
            {
                // Retrieve files and their information from merge modules.
                Table wixMergeTable = this.Output.Tables["WixMerge"];

                if (null != wixMergeTable)
                {
                    ExtractMergeModuleFilesCommand command = new ExtractMergeModuleFilesCommand();
                    command.FileFacades = fileFacades;
                    command.FileTable = this.Output.Tables["File"];
                    command.WixFileTable = this.Output.Tables["WixFile"];
                    command.WixMergeTable = wixMergeTable;
                    command.OutputInstallerVersion = installerVersion;
                    command.SuppressLayout = this.SuppressLayout;
                    command.TempFilesLocation = this.TempFilesLocation;
                    command.Execute();

                    fileFacades.AddRange(command.MergeModulesFileFacades);
                }
            }
            else if (OutputType.Patch == this.Output.Type)
            {
                // Merge transform data into the output object.
                IEnumerable<FileFacade> filesFromTransform = this.CopyFromTransformData(this.Output);

                fileFacades.AddRange(filesFromTransform);
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            Messaging.Instance.OnMessage(WixVerboses.UpdatingFileInformation());

            // Gather information about files that did not come from merge modules (i.e. rows with a reference to the File table).
            {
                UpdateFileFacadesCommand command = new UpdateFileFacadesCommand();
                command.FileFacades = fileFacades;
                command.UpdateFileFacades = fileFacades.Where(f => !f.FromModule);
                command.ModularizationGuid = modularizationGuid;
                command.Output = this.Output;
                command.OverwriteHash = true;
                command.TableDefinitions = this.TableDefinitions;
                command.VariableCache = variableCache;
                command.Execute();
            }

            // Set generated component guids.
            this.SetComponentGuids(this.Output);

            // With the Component Guids set now we can create instance transforms.
            this.CreateInstanceTransforms(this.Output);

            this.ValidateComponentGuids(this.Output);

            this.UpdateControlText(this.Output);

            if (delayedFields.Any())
            {
                ResolveDelayedFieldsCommand command = new ResolveDelayedFieldsCommand();
                command.OutputType = this.Output.Type;
                command.DelayedFields = delayedFields;
                command.ModularizationGuid = null;
                command.VariableCache = variableCache;
                command.Execute();
            }

            // Assign files to media.
            RowDictionary<MediaRow> assignedMediaRows;
            Dictionary<MediaRow, IEnumerable<FileFacade>> filesByCabinetMedia;
            IEnumerable<FileFacade> uncompressedFiles;
            {
                AssignMediaCommand command = new AssignMediaCommand();
                command.FilesCompressed = compressed;
                command.FileFacades = fileFacades;
                command.Output = this.Output;
                command.TableDefinitions = this.TableDefinitions;
                command.Execute();

                assignedMediaRows = command.MediaRows;
                filesByCabinetMedia = command.FileFacadesByCabinetMedia;
                uncompressedFiles = command.UncompressedFileFacades;
            }

            // Update file sequence.
            this.UpdateMediaSequences(this.Output.Type, fileFacades, assignedMediaRows);

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // Extended binder extensions can be called now that fields are resolved.
            {
                Table updatedFiles = this.Output.EnsureTable(this.TableDefinitions["WixBindUpdatedFiles"]);

                foreach (BinderExtension extension in this.Extensions)
                {
                    extension.AfterResolvedFields(this.Output);
                }

                List<FileFacade> updatedFileFacades = new List<FileFacade>();

                foreach (Row updatedFile in updatedFiles.Rows)
                {
                    string updatedId = updatedFile.FieldAsString(0);

                    FileFacade updatedFacade = fileFacades.First(f => f.File.File.Equals(updatedId));

                    updatedFileFacades.Add(updatedFacade);
                }

                if (updatedFileFacades.Any())
                {
                    UpdateFileFacadesCommand command = new UpdateFileFacadesCommand();
                    command.FileFacades = fileFacades;
                    command.UpdateFileFacades = updatedFileFacades;
                    command.ModularizationGuid = modularizationGuid;
                    command.Output = this.Output;
                    command.OverwriteHash = true;
                    command.TableDefinitions = this.TableDefinitions;
                    command.VariableCache = variableCache;
                    command.Execute();
                }
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            Directory.CreateDirectory(this.TempFilesLocation);

            if (OutputType.Patch == this.Output.Type && this.DeltaBinaryPatch)
            {
                CreateDeltaPatchesCommand command = new CreateDeltaPatchesCommand();
                command.FileFacades = fileFacades;
                command.WixPatchIdTable = this.Output.Tables["WixPatchId"];
                command.TempFilesLocation = this.TempFilesLocation;
                command.Execute();
            }

            // create cabinet files and process uncompressed files
            string layoutDirectory = Path.GetDirectoryName(this.OutputPath);
            if (!this.SuppressLayout || OutputType.Module == this.Output.Type)
            {
                Messaging.Instance.OnMessage(WixVerboses.CreatingCabinetFiles());

                CreateCabinetsCommand command = new CreateCabinetsCommand();
                command.CabbingThreadCount = this.CabbingThreadCount;
                command.DefaultCompressionLevel = this.DefaultCompressionLevel;
                command.Output = this.Output;
                command.FileManagers = this.FileManagers;
                command.LayoutDirectory = layoutDirectory;
                command.Compressed = compressed;
                command.FileRowsByCabinet = filesByCabinetMedia;
                command.ResolveMedia = this.ResolveMedia;
                command.TableDefinitions = this.TableDefinitions;
                command.TempFilesLocation = this.TempFilesLocation;
                command.WixMediaTable = this.Output.Tables["WixMedia"];
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
            }

            if (OutputType.Patch == this.Output.Type)
            {
                // copy output data back into the transforms
                this.CopyToTransformData(this.Output);
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // add back suppressed tables which must be present prior to merging in modules
            if (OutputType.Product == this.Output.Type)
            {
                Table wixMergeTable = this.Output.Tables["WixMerge"];

                if (null != wixMergeTable && 0 < wixMergeTable.Rows.Count)
                {
                    foreach (SequenceTable sequence in Enum.GetValues(typeof(SequenceTable)))
                    {
                        string sequenceTableName = sequence.ToString();
                        Table sequenceTable = this.Output.Tables[sequenceTableName];

                        if (null == sequenceTable)
                        {
                            sequenceTable = this.Output.EnsureTable(this.TableDefinitions[sequenceTableName]);
                        }

                        if (0 == sequenceTable.Rows.Count)
                        {
                            suppressedTableNames.Add(sequenceTableName);
                        }
                    }
                }
            }

            foreach (BinderExtension extension in this.Extensions)
            {
                extension.Finish(this.Output);
            }

            // generate database file
            Messaging.Instance.OnMessage(WixVerboses.GeneratingDatabase());
            string tempDatabaseFile = Path.Combine(this.TempFilesLocation, Path.GetFileName(this.OutputPath));
            this.GenerateDatabase(this.Output, tempDatabaseFile, false, false);

            FileTransfer transfer;
            if (FileTransfer.TryCreate(tempDatabaseFile, this.OutputPath, true, this.Output.Type.ToString(), null, out transfer)) // note where this database needs to move in the future
            {
                transfer.Built = true;
                fileTransfers.Add(transfer);
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // Output the output to a file
            Pdb pdb = new Pdb();
            pdb.Output = this.Output;
            if (!String.IsNullOrEmpty(this.PdbFile))
            {
                pdb.Save(this.PdbFile);
            }

            // Merge modules.
            if (OutputType.Product == this.Output.Type)
            {
                Messaging.Instance.OnMessage(WixVerboses.MergingModules());

                MergeModulesCommand command = new MergeModulesCommand();
                command.FileFacades = fileFacades;
                command.Output = this.Output;
                command.OutputPath = tempDatabaseFile;
                command.SuppressedTableNames = suppressedTableNames;
                command.Execute();

                // stop processing if an error previously occurred
                if (Messaging.Instance.EncounteredError)
                {
                    return;
                }
            }

            // inspect the MSI prior to running ICEs
            InspectorCore inspectorCore = new InspectorCore();
            foreach (InspectorExtension inspectorExtension in this.InspectorExtensions)
            {
                inspectorExtension.Core = inspectorCore;
                inspectorExtension.InspectDatabase(tempDatabaseFile, pdb);

                inspectorExtension.Core = null; // reset.
            }

            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // validate the output if there is an MSI validator
            if (null != this.Validator)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                // set the output file for source line information
                this.Validator.Output = this.Output;

                Messaging.Instance.OnMessage(WixVerboses.ValidatingDatabase());

                this.Validator.Validate(tempDatabaseFile);

                stopwatch.Stop();
                Messaging.Instance.OnMessage(WixVerboses.ValidatedDatabase(stopwatch.ElapsedMilliseconds));

                // Stop processing if an error occurred.
                if (Messaging.Instance.EncounteredError)
                {
                    return;
                }
            }

            // Process uncompressed files.
            if (!Messaging.Instance.EncounteredError && !this.SuppressLayout && uncompressedFiles.Any())
            {
                ProcessUncompressedFilesCommand command = new ProcessUncompressedFilesCommand();
                command.Compressed = compressed;
                command.FileFacades = uncompressedFiles;
                command.LayoutDirectory = layoutDirectory;
                command.LongNamesInImage = longNames;
                command.MediaRows = assignedMediaRows;
                command.ResolveMedia = this.ResolveMedia;
                command.DatabasePath = tempDatabaseFile;
                command.WixMediaTable = this.Output.Tables["WixMedia"];
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
            }

            this.FileTransfers = fileTransfers;
            this.ContentFilePaths = fileFacades.Select(r => r.WixFile.Source).ToList();
        }

        /// <summary>
        /// Localize dialogs and controls.
        /// </summary>
        /// <param name="tables">The tables to localize.</param>
        private void LocalizeUI(TableIndexedCollection tables)
        {
            Table dialogTable = tables["Dialog"];
            if (null != dialogTable)
            {
                foreach (Row row in dialogTable.Rows)
                {
                    string dialog = (string)row[0];
                    LocalizedControl localizedControl = this.Localizer.GetLocalizedControl(dialog, null);
                    if (null != localizedControl)
                    {
                        if (CompilerConstants.IntegerNotSet != localizedControl.X)
                        {
                            row[1] = localizedControl.X;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Y)
                        {
                            row[2] = localizedControl.Y;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Width)
                        {
                            row[3] = localizedControl.Width;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Height)
                        {
                            row[4] = localizedControl.Height;
                        }

                        row[5] = (int)row[5] | localizedControl.Attributes;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            row[6] = localizedControl.Text;
                        }
                    }
                }
            }

            Table controlTable = tables["Control"];
            if (null != controlTable)
            {
                foreach (Row row in controlTable.Rows)
                {
                    string dialog = (string)row[0];
                    string control = (string)row[1];
                    LocalizedControl localizedControl = this.Localizer.GetLocalizedControl(dialog, control);
                    if (null != localizedControl)
                    {
                        if (CompilerConstants.IntegerNotSet != localizedControl.X)
                        {
                            row[3] = localizedControl.X.ToString();
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Y)
                        {
                            row[4] = localizedControl.Y.ToString();
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Width)
                        {
                            row[5] = localizedControl.Width.ToString();
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Height)
                        {
                            row[6] = localizedControl.Height.ToString();
                        }

                        row[7] = (int)row[7] | localizedControl.Attributes;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            row[9] = localizedControl.Text;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copy file data between transform substorages and the patch output object
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="allFileRows">True if copying from transform to patch, false the other way.</param>
        private IEnumerable<FileFacade> CopyFromTransformData(Output output)
        {
            CopyTransformDataCommand command = new CopyTransformDataCommand();
            command.CopyOutFileRows = true;
            command.FileManagerCore = this.FileManagerCore;
            command.FileManagers = this.FileManagers;
            command.Output = output;
            command.TableDefinitions = this.TableDefinitions;
            command.Execute();

            return command.FileFacades;
        }

        /// <summary>
        /// Copy file data between transform substorages and the patch output object
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="allFileRows">True if copying from transform to patch, false the other way.</param>
        private void CopyToTransformData(Output output)
        {
            CopyTransformDataCommand command = new CopyTransformDataCommand();
            command.CopyOutFileRows = false;
            command.FileManagerCore = this.FileManagerCore;
            command.FileManagers = this.FileManagers;
            command.Output = output;
            command.TableDefinitions = this.TableDefinitions;
            command.Execute();
        }

        /// <summary>
        /// Takes an id, and demodularizes it (if possible).
        /// </summary>
        /// <remarks>
        /// If the output type is a module, returns a demodularized version of an id. Otherwise, returns the id.
        /// </remarks>
        /// <param name="outputType">The type of the output to bind.</param>
        /// <param name="modularizationGuid">The modularization GUID.</param>
        /// <param name="id">The id to demodularize.</param>
        /// <returns>The demodularized id.</returns>
        internal static string Demodularize(OutputType outputType, string modularizationGuid, string id)
        {
            if (OutputType.Module == outputType && id.EndsWith(String.Concat(".", modularizationGuid), StringComparison.Ordinal))
            {
                id = id.Substring(0, id.Length - 37);
            }

            return id;
        }

        private void UpdateMediaSequences(OutputType outputType, IEnumerable<FileFacade> fileFacades, RowDictionary<MediaRow> mediaRows)
        {
            // Calculate sequence numbers and media disk id layout for all file media information objects.
            if (OutputType.Module == outputType)
            {
                int lastSequence = 0;
                foreach (FileFacade facade in fileFacades) // TODO: Sort these rows directory path and component id and maybe file size or file extension and other creative ideas to get optimal install speed out of MSI.
                {
                    facade.File.Sequence = ++lastSequence;
                }
            }
            else
            {
                int lastSequence = 0;
                MediaRow mediaRow = null;
                Dictionary<int, List<FileFacade>> patchGroups = new Dictionary<int, List<FileFacade>>();

                // sequence the non-patch-added files
                foreach (FileFacade facade in fileFacades) // TODO: Sort these rows directory path and component id and maybe file size or file extension and other creative ideas to get optimal install speed out of MSI.
                {
                    if (null == mediaRow)
                    {
                        mediaRow = mediaRows.Get(facade.WixFile.DiskId);
                        if (OutputType.Patch == outputType)
                        {
                            // patch Media cannot start at zero
                            lastSequence = mediaRow.LastSequence;
                        }
                    }
                    else if (mediaRow.DiskId != facade.WixFile.DiskId)
                    {
                        mediaRow.LastSequence = lastSequence;
                        mediaRow = mediaRows.Get(facade.WixFile.DiskId);
                    }

                    if (0 < facade.WixFile.PatchGroup)
                    {
                        List<FileFacade> patchGroup = patchGroups[facade.WixFile.PatchGroup];

                        if (null == patchGroup)
                        {
                            patchGroup = new List<FileFacade>();
                            patchGroups.Add(facade.WixFile.PatchGroup, patchGroup);
                        }

                        patchGroup.Add(facade);
                    }
                    else
                    {
                        facade.File.Sequence = ++lastSequence;
                    }
                }

                if (null != mediaRow)
                {
                    mediaRow.LastSequence = lastSequence;
                    mediaRow = null;
                }

                // sequence the patch-added files
                foreach (List<FileFacade> patchGroup in patchGroups.Values)
                {
                    foreach (FileFacade facade in patchGroup)
                    {
                        if (null == mediaRow)
                        {
                            mediaRow = mediaRows.Get(facade.WixFile.DiskId);
                        }
                        else if (mediaRow.DiskId != facade.WixFile.DiskId)
                        {
                            mediaRow.LastSequence = lastSequence;
                            mediaRow = mediaRows.Get(facade.WixFile.DiskId);
                        }

                        facade.File.Sequence = ++lastSequence;
                    }
                }

                if (null != mediaRow)
                {
                    mediaRow.LastSequence = lastSequence;
                }
            }
        }

        /// <summary>
        /// Set the guids for components with generatable guids.
        /// </summary>
        /// <param name="output">Internal representation of the database to operate on.</param>
        private void SetComponentGuids(Output output)
        {
            Table componentTable = output.Tables["Component"];
            if (null != componentTable)
            {
                Hashtable registryKeyRows = null;
                Hashtable directories = null;
                Hashtable componentIdGenSeeds = null;
                Dictionary<string, List<FileRow>> fileRows = null;

                // find components with generatable guids
                foreach (ComponentRow componentRow in componentTable.Rows)
                {
                    // component guid will be generated
                    if ("*" == componentRow.Guid)
                    {
                        if (null == componentRow.KeyPath || componentRow.IsOdbcDataSourceKeyPath)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalComponentWithAutoGeneratedGuid(componentRow.SourceLineNumbers));
                        }
                        else if (componentRow.IsRegistryKeyPath)
                        {
                            if (null == registryKeyRows)
                            {
                                Table registryTable = output.Tables["Registry"];

                                registryKeyRows = new Hashtable(registryTable.Rows.Count);

                                foreach (Row registryRow in registryTable.Rows)
                                {
                                    registryKeyRows.Add((string)registryRow[0], registryRow);
                                }
                            }

                            Row foundRow = registryKeyRows[componentRow.KeyPath] as Row;

                            string bitness = componentRow.Is64Bit ? "64" : String.Empty;
                            if (null != foundRow)
                            {
                                string regkey = String.Concat(bitness, foundRow[1], "\\", foundRow[2], "\\", foundRow[3]);
                                componentRow.Guid = Uuid.NewUuid(BindDatabaseCommand.WixComponentGuidNamespace, regkey.ToLowerInvariant()).ToString("B").ToUpperInvariant();
                            }
                        }
                        else // must be a File KeyPath
                        {
                            // if the directory table hasn't been loaded into an indexed hash
                            // of directory ids to target names do that now.
                            if (null == directories)
                            {
                                Table directoryTable = output.Tables["Directory"];

                                int numDirectoryTableRows = (null != directoryTable) ? directoryTable.Rows.Count : 0;

                                directories = new Hashtable(numDirectoryTableRows);

                                // get the target paths for all directories
                                if (null != directoryTable)
                                {
                                    foreach (Row row in directoryTable.Rows)
                                    {
                                        // if the directory Id already exists, we will skip it here since
                                        // checking for duplicate primary keys is done later when importing tables
                                        // into database
                                        if (directories.ContainsKey(row[0]))
                                        {
                                            continue;
                                        }

                                        string targetName = Installer.GetName((string)row[2], false, true);
                                        directories.Add(row[0], new ResolvedDirectory((string)row[1], targetName));
                                    }
                                }
                            }

                            // if the component id generation seeds have not been indexed
                            // from the WixDirectory table do that now.
                            if (null == componentIdGenSeeds)
                            {
                                Table wixDirectoryTable = output.Tables["WixDirectory"];

                                int numWixDirectoryRows = (null != wixDirectoryTable) ? wixDirectoryTable.Rows.Count : 0;

                                componentIdGenSeeds = new Hashtable(numWixDirectoryRows);

                                // if there are any WixDirectory rows, build up the Component Guid
                                // generation seeds indexed by Directory/@Id.
                                if (null != wixDirectoryTable)
                                {
                                    foreach (Row row in wixDirectoryTable.Rows)
                                    {
                                        componentIdGenSeeds.Add(row[0], (string)row[1]);
                                    }
                                }
                            }

                            // if the file rows have not been indexed by File.Component yet
                            // then do that now
                            if (null == fileRows)
                            {
                                Table fileTable = output.Tables["File"];

                                int numFileRows = (null != fileTable) ? fileTable.Rows.Count : 0;

                                fileRows = new Dictionary<string, List<FileRow>>(numFileRows);

                                if (null != fileTable)
                                {
                                    foreach (FileRow file in fileTable.Rows)
                                    {
                                        List<FileRow> files;
                                        if (!fileRows.TryGetValue(file.Component, out files))
                                        {
                                            files = new List<FileRow>();
                                            fileRows.Add(file.Component, files);
                                        }

                                        files.Add(file);
                                    }
                                }
                            }

                            // validate component meets all the conditions to have a generated guid
                            List<FileRow> currentComponentFiles = fileRows[componentRow.Component];
                            int numFilesInComponent = currentComponentFiles.Count;
                            string path = null;

                            foreach (FileRow fileRow in currentComponentFiles)
                            {
                                if (fileRow.File == componentRow.KeyPath)
                                {
                                    // calculate the key file's canonical target path
                                    string directoryPath = Binder.GetDirectoryPath(directories, componentIdGenSeeds, componentRow.Directory, true);
                                    string fileName = Installer.GetName(fileRow.FileName, false, true).ToLower(CultureInfo.InvariantCulture);
                                    path = Path.Combine(directoryPath, fileName);

                                    // find paths that are not canonicalized
                                    if (path.StartsWith(@"PersonalFolder\my pictures", StringComparison.Ordinal) ||
                                        path.StartsWith(@"ProgramFilesFolder\common files", StringComparison.Ordinal) ||
                                        path.StartsWith(@"ProgramMenuFolder\startup", StringComparison.Ordinal) ||
                                        path.StartsWith("TARGETDIR", StringComparison.Ordinal) ||
                                        path.StartsWith(@"StartMenuFolder\programs", StringComparison.Ordinal) ||
                                        path.StartsWith(@"WindowsFolder\fonts", StringComparison.Ordinal))
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.IllegalPathForGeneratedComponentGuid(componentRow.SourceLineNumbers, fileRow.Component, path));
                                    }

                                    // if component has more than one file, the key path must be versioned
                                    if (1 < numFilesInComponent && String.IsNullOrEmpty(fileRow.Version))
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.IllegalGeneratedGuidComponentUnversionedKeypath(componentRow.SourceLineNumbers));
                                    }
                                }
                                else
                                {
                                    // not a key path, so it must be an unversioned file if component has more than one file
                                    if (1 < numFilesInComponent && !String.IsNullOrEmpty(fileRow.Version))
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.IllegalGeneratedGuidComponentVersionedNonkeypath(componentRow.SourceLineNumbers));
                                    }
                                }
                            }

                            // if the rules were followed, reward with a generated guid
                            if (!Messaging.Instance.EncounteredError)
                            {
                                componentRow.Guid = Uuid.NewUuid(BindDatabaseCommand.WixComponentGuidNamespace, path).ToString("B").ToUpperInvariant();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates instance transform substorages in the output.
        /// </summary>
        /// <param name="output">Output containing instance transform definitions.</param>
        private void CreateInstanceTransforms(Output output)
        {
            // Create and add substorages for instance transforms.
            Table wixInstanceTransformsTable = output.Tables["WixInstanceTransforms"];
            if (null != wixInstanceTransformsTable && 0 <= wixInstanceTransformsTable.Rows.Count)
            {
                string targetProductCode = null;
                string targetUpgradeCode = null;
                string targetProductVersion = null;

                Table targetSummaryInformationTable = output.Tables["_SummaryInformation"];
                Table targetPropertyTable = output.Tables["Property"];

                // Get the data from target database
                foreach (Row propertyRow in targetPropertyTable.Rows)
                {
                    if ("ProductCode" == (string)propertyRow[0])
                    {
                        targetProductCode = (string)propertyRow[1];
                    }
                    else if ("ProductVersion" == (string)propertyRow[0])
                    {
                        targetProductVersion = (string)propertyRow[1];
                    }
                    else if ("UpgradeCode" == (string)propertyRow[0])
                    {
                        targetUpgradeCode = (string)propertyRow[1];
                    }
                }

                // Index the Instance Component Rows.
                Dictionary<string, ComponentRow> instanceComponentGuids = new Dictionary<string, ComponentRow>();
                Table targetInstanceComponentTable = output.Tables["WixInstanceComponent"];
                if (null != targetInstanceComponentTable && 0 < targetInstanceComponentTable.Rows.Count)
                {
                    foreach (Row row in targetInstanceComponentTable.Rows)
                    {
                        // Build up all the instances, we'll get the Components rows from the real Component table.
                        instanceComponentGuids.Add((string)row[0], null);
                    }

                    Table targetComponentTable = output.Tables["Component"];
                    foreach (ComponentRow componentRow in targetComponentTable.Rows)
                    {
                        string component = (string)componentRow[0];
                        if (instanceComponentGuids.ContainsKey(component))
                        {
                            instanceComponentGuids[component] = componentRow;
                        }
                    }
                }

                // Generate the instance transforms
                foreach (Row instanceRow in wixInstanceTransformsTable.Rows)
                {
                    string instanceId = (string)instanceRow[0];

                    Output instanceTransform = new Output(instanceRow.SourceLineNumbers);
                    instanceTransform.Type = OutputType.Transform;
                    instanceTransform.Codepage = output.Codepage;

                    Table instanceSummaryInformationTable = instanceTransform.EnsureTable(this.TableDefinitions["_SummaryInformation"]);
                    string targetPlatformAndLanguage = null;

                    foreach (Row summaryInformationRow in targetSummaryInformationTable.Rows)
                    {
                        if (7 == (int)summaryInformationRow[0]) // PID_TEMPLATE
                        {
                            targetPlatformAndLanguage = (string)summaryInformationRow[1];
                        }

                        // Copy the row's data to the transform.
                        Row copyOfSummaryRow = instanceSummaryInformationTable.CreateRow(null);
                        copyOfSummaryRow[0] = summaryInformationRow[0];
                        copyOfSummaryRow[1] = summaryInformationRow[1];
                    }

                    // Modify the appropriate properties.
                    Table propertyTable = instanceTransform.EnsureTable(this.TableDefinitions["Property"]);

                    // Change the ProductCode property
                    string productCode = (string)instanceRow[2];
                    if ("*" == productCode)
                    {
                        productCode = Common.GenerateGuid();
                    }

                    Row productCodeRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                    productCodeRow.Operation = RowOperation.Modify;
                    productCodeRow.Fields[1].Modified = true;
                    productCodeRow[0] = "ProductCode";
                    productCodeRow[1] = productCode;

                    // Change the instance property
                    Row instanceIdRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                    instanceIdRow.Operation = RowOperation.Modify;
                    instanceIdRow.Fields[1].Modified = true;
                    instanceIdRow[0] = (string)instanceRow[1];
                    instanceIdRow[1] = instanceId;

                    if (null != instanceRow[3])
                    {
                        // Change the ProductName property
                        Row productNameRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                        productNameRow.Operation = RowOperation.Modify;
                        productNameRow.Fields[1].Modified = true;
                        productNameRow[0] = "ProductName";
                        productNameRow[1] = (string)instanceRow[3];
                    }

                    if (null != instanceRow[4])
                    {
                        // Change the UpgradeCode property
                        Row upgradeCodeRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                        upgradeCodeRow.Operation = RowOperation.Modify;
                        upgradeCodeRow.Fields[1].Modified = true;
                        upgradeCodeRow[0] = "UpgradeCode";
                        upgradeCodeRow[1] = instanceRow[4];

                        // Change the Upgrade table
                        Table targetUpgradeTable = output.Tables["Upgrade"];
                        if (null != targetUpgradeTable && 0 <= targetUpgradeTable.Rows.Count)
                        {
                            string upgradeId = (string)instanceRow[4];
                            Table upgradeTable = instanceTransform.EnsureTable(this.TableDefinitions["Upgrade"]);
                            foreach (Row row in targetUpgradeTable.Rows)
                            {
                                // In case they are upgrading other codes to this new product, leave the ones that don't match the
                                // Product.UpgradeCode intact.
                                if (targetUpgradeCode == (string)row[0])
                                {
                                    Row upgradeRow = upgradeTable.CreateRow(null);
                                    upgradeRow.Operation = RowOperation.Add;
                                    upgradeRow.Fields[0].Modified = true;
                                    // I was hoping to be able to RowOperation.Modify, but that didn't appear to function.
                                    // upgradeRow.Fields[0].PreviousData = (string)row[0];

                                    // Inserting a new Upgrade record with the updated UpgradeCode
                                    upgradeRow[0] = upgradeId;
                                    upgradeRow[1] = row[1];
                                    upgradeRow[2] = row[2];
                                    upgradeRow[3] = row[3];
                                    upgradeRow[4] = row[4];
                                    upgradeRow[5] = row[5];
                                    upgradeRow[6] = row[6];

                                    // Delete the old row
                                    Row upgradeRemoveRow = upgradeTable.CreateRow(null);
                                    upgradeRemoveRow.Operation = RowOperation.Delete;
                                    upgradeRemoveRow[0] = row[0];
                                    upgradeRemoveRow[1] = row[1];
                                    upgradeRemoveRow[2] = row[2];
                                    upgradeRemoveRow[3] = row[3];
                                    upgradeRemoveRow[4] = row[4];
                                    upgradeRemoveRow[5] = row[5];
                                    upgradeRemoveRow[6] = row[6];
                                }
                            }
                        }
                    }

                    // If there are instance Components generate new GUIDs for them.
                    if (0 < instanceComponentGuids.Count)
                    {
                        Table componentTable = instanceTransform.EnsureTable(this.TableDefinitions["Component"]);
                        foreach (ComponentRow targetComponentRow in instanceComponentGuids.Values)
                        {
                            string guid = targetComponentRow.Guid;
                            if (!String.IsNullOrEmpty(guid))
                            {
                                Row instanceComponentRow = componentTable.CreateRow(targetComponentRow.SourceLineNumbers);
                                instanceComponentRow.Operation = RowOperation.Modify;
                                instanceComponentRow.Fields[1].Modified = true;
                                instanceComponentRow[0] = targetComponentRow[0];
                                instanceComponentRow[1] = Uuid.NewUuid(BindDatabaseCommand.WixComponentGuidNamespace, String.Concat(guid, instanceId)).ToString("B").ToUpper(CultureInfo.InvariantCulture);
                                instanceComponentRow[2] = targetComponentRow[2];
                                instanceComponentRow[3] = targetComponentRow[3];
                                instanceComponentRow[4] = targetComponentRow[4];
                                instanceComponentRow[5] = targetComponentRow[5];
                            }
                        }
                    }

                    // Update the summary information
                    Hashtable summaryRows = new Hashtable(instanceSummaryInformationTable.Rows.Count);
                    foreach (Row row in instanceSummaryInformationTable.Rows)
                    {
                        summaryRows[row[0]] = row;

                        if ((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage == (int)row[0])
                        {
                            row[1] = targetPlatformAndLanguage;
                        }
                        else if ((int)SummaryInformation.Transform.ProductCodes == (int)row[0])
                        {
                            row[1] = String.Concat(targetProductCode, targetProductVersion, ';', productCode, targetProductVersion, ';', targetUpgradeCode);
                        }
                        else if ((int)SummaryInformation.Transform.ValidationFlags == (int)row[0])
                        {
                            row[1] = 0;
                        }
                        else if ((int)SummaryInformation.Transform.Security == (int)row[0])
                        {
                            row[1] = "4";
                        }
                    }

                    if (!summaryRows.Contains((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage))
                    {
                        Row summaryRow = instanceSummaryInformationTable.CreateRow(null);
                        summaryRow[0] = (int)SummaryInformation.Transform.UpdatedPlatformAndLanguage;
                        summaryRow[1] = targetPlatformAndLanguage;
                    }
                    else if (!summaryRows.Contains((int)SummaryInformation.Transform.ValidationFlags))
                    {
                        Row summaryRow = instanceSummaryInformationTable.CreateRow(null);
                        summaryRow[0] = (int)SummaryInformation.Transform.ValidationFlags;
                        summaryRow[1] = "0";
                    }
                    else if (!summaryRows.Contains((int)SummaryInformation.Transform.Security))
                    {
                        Row summaryRow = instanceSummaryInformationTable.CreateRow(null);
                        summaryRow[0] = (int)SummaryInformation.Transform.Security;
                        summaryRow[1] = "4";
                    }

                    output.SubStorages.Add(new SubStorage(instanceId, instanceTransform));
                }
            }
        }

        /// <summary>
        /// Validate that there are no duplicate GUIDs in the output.
        /// </summary>
        /// <remarks>
        /// Duplicate GUIDs without conditions are an error condition; with conditions, it's a
        /// warning, as the conditions might be mutually exclusive.
        /// </remarks>
        private void ValidateComponentGuids(Output output)
        {
            Table componentTable = output.Tables["Component"];
            if (null != componentTable)
            {
                Dictionary<string, bool> componentGuidConditions = new Dictionary<string, bool>(componentTable.Rows.Count);

                foreach (ComponentRow row in componentTable.Rows)
                {
                    // we don't care about unmanaged components and if there's a * GUID remaining,
                    // there's already an error that prevented it from being replaced with a real GUID.
                    if (!String.IsNullOrEmpty(row.Guid) && "*" != row.Guid)
                    {
                        bool thisComponentHasCondition = !String.IsNullOrEmpty(row.Condition);
                        bool allComponentsHaveConditions = thisComponentHasCondition;

                        if (componentGuidConditions.ContainsKey(row.Guid))
                        {
                            allComponentsHaveConditions = componentGuidConditions[row.Guid] && thisComponentHasCondition;

                            if (allComponentsHaveConditions)
                            {
                                Messaging.Instance.OnMessage(WixWarnings.DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions(row.SourceLineNumbers, row.Component, row.Guid));
                            }
                            else
                            {
                                Messaging.Instance.OnMessage(WixErrors.DuplicateComponentGuids(row.SourceLineNumbers, row.Component, row.Guid));
                            }
                        }

                        componentGuidConditions[row.Guid] = allComponentsHaveConditions;
                    }
                }
            }
        }

        /// <summary>
        /// Update Control and BBControl text by reading from files when necessary.
        /// </summary>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        private void UpdateControlText(Output output)
        {
            UpdateControlTextCommand command = new UpdateControlTextCommand();
            command.BBControlTable = output.Tables["BBControl"];
            command.WixBBControlTable = output.Tables["WixBBControl"];
            command.ControlTable = output.Tables["Control"];
            command.WixControlTable = output.Tables["WixControl"];
            command.Execute();
        }

        private string ResolveMedia(MediaRow mediaRow, string mediaLayoutDirectory, string layoutDirectory)
        {
            string layout = null;

            foreach (IBinderFileManager fileManager in this.FileManagers)
            {
                layout = fileManager.ResolveMedia(mediaRow, mediaLayoutDirectory, layoutDirectory);
                if (!String.IsNullOrEmpty(layout))
                {
                    break;
                }
            }

            // If no binder file manager resolved the layout, do the default behavior.
            if (String.IsNullOrEmpty(layout))
            {
                if (String.IsNullOrEmpty(mediaLayoutDirectory))
                {
                    layout = layoutDirectory;
                }
                else if (Path.IsPathRooted(mediaLayoutDirectory))
                {
                    layout = mediaLayoutDirectory;
                }
                else
                {
                    layout = Path.Combine(layoutDirectory, mediaLayoutDirectory);
                }
            }

            return layout;
        }

        /// <summary>
        /// Creates the MSI/MSM/PCP database.
        /// </summary>
        /// <param name="output">Output to create database for.</param>
        /// <param name="databaseFile">The database file to create.</param>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        /// <param name="useSubdirectory">Whether to use a subdirectory based on the <paramref name="databaseFile"/> file name for intermediate files.</param>
        private void GenerateDatabase(Output output, string databaseFile, bool keepAddedColumns, bool useSubdirectory)
        {
            GenerateDatabaseCommand command = new GenerateDatabaseCommand();
            command.Extensions = this.Extensions;
            command.FileManagers = this.FileManagers;
            command.Output = output;
            command.OutputPath = databaseFile;
            command.KeepAddedColumns = keepAddedColumns;
            command.UseSubDirectory = useSubdirectory;
            command.SuppressAddingValidationRows = this.SuppressAddingValidationRows;
            command.TableDefinitions = this.TableDefinitions;
            command.TempFilesLocation = this.TempFilesLocation;
            command.Codepage = this.Codepage;
            command.Execute();
        }
    }
}
