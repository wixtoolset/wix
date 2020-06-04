// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Binds a databse.
    /// </summary>
    internal class BindDatabaseCommand
    {
        // As outlined in RFC 4122, this is our namespace for generating name-based (version 3) UUIDs.
        internal static readonly Guid WixComponentGuidNamespace = new Guid("{3064E5C6-FB63-4FE9-AC49-E446A792EFA5}");

        public BindDatabaseCommand(IBindContext context, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtension, Validator validator) : this(context, backendExtension, null, validator)
        {
        }

        public BindDatabaseCommand(IBindContext context, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtension, IEnumerable<SubStorage> subStorages, Validator validator)
        {
            this.ServiceProvider = context.ServiceProvider;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();

            this.BackendHelper = context.ServiceProvider.GetService<IBackendHelper>();
            this.WindowsInstallerBackendHelper = context.ServiceProvider.GetService<IWindowsInstallerBackendHelper>();

            this.PathResolver = this.ServiceProvider.GetService<IPathResolver>();

            this.CabbingThreadCount = context.CabbingThreadCount;
            this.CabCachePath = context.CabCachePath;
            this.Codepage = context.Codepage;
            this.DefaultCompressionLevel = context.DefaultCompressionLevel;
            this.DelayedFields = context.DelayedFields;
            this.ExpectedEmbeddedFiles = context.ExpectedEmbeddedFiles;
            this.FileSystemManager = new FileSystemManager(context.FileSystemExtensions);
            this.Intermediate = context.IntermediateRepresentation;
            this.IntermediateFolder = context.IntermediateFolder;
            this.OutputPath = context.OutputPath;
            this.OutputPdbPath = context.PdbPath;
            this.PdbType = context.PdbType;
            this.SuppressLayout = context.SuppressLayout;

            this.SubStorages = subStorages;
            this.Validator = validator;

            this.BackendExtensions = backendExtension;
        }

        public IWixToolsetServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IWindowsInstallerBackendHelper WindowsInstallerBackendHelper { get; }

        private IPathResolver PathResolver { get; }

        private int Codepage { get; }

        private int CabbingThreadCount { get; }

        private string CabCachePath { get; }

        private CompressionLevel? DefaultCompressionLevel { get; }

        public IEnumerable<IDelayedField> DelayedFields { get; }

        public IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; }

        public FileSystemManager FileSystemManager { get; }

        public bool DeltaBinaryPatch { get; set; }

        private IEnumerable<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        private IEnumerable<SubStorage> SubStorages { get; }

        private Intermediate Intermediate { get; }

        private string OutputPath { get; }

        public PdbType PdbType { get; set; }

        private string OutputPdbPath { get; }

        private bool SuppressAddingValidationRows { get; }

        private bool SuppressLayout { get; }

        private string IntermediateFolder { get; }

        private Validator Validator { get; }

        public IBindResult Execute()
        {
            if (!this.Intermediate.HasLevel(Data.IntermediateLevels.Linked) && !this.Intermediate.HasLevel(Data.IntermediateLevels.Resolved))
            {
                this.Messaging.Write(ErrorMessages.IntermediatesMustBeResolved(this.Intermediate.Id));
            }

            var section = this.Intermediate.Sections.Single();

            var fileTransfers = new List<IFileTransfer>();
            var trackedFiles = new List<ITrackedFile>();

            var containsMergeModules = false;

            // If there are any fields to resolve later, create the cache to populate during bind.
            var variableCache = this.DelayedFields.Any() ? new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) : null;

            // Load standard tables, authored custom tables, and extension custom tables.
            TableDefinitionCollection tableDefinitions;
            {
                var command = new LoadTableDefinitionsCommand(this.Messaging, section, this.BackendExtensions);
                command.Execute();

                tableDefinitions = command.TableDefinitions;
            }

            // Process the summary information table before the other tables.
            bool compressed;
            bool longNames;
            int installerVersion;
            string modularizationSuffix;
            {
                var command = new BindSummaryInfoCommand(section);
                command.Execute();

                compressed = command.Compressed;
                longNames = command.LongNames;
                installerVersion = command.InstallerVersion;
                modularizationSuffix = command.ModularizationSuffix;
            }

            // Add binder variables for all properties.
            if (SectionType.Product == section.Type || variableCache != null)
            {
                foreach (var propertyRow in section.Tuples.OfType<PropertyTuple>())
                {
                    // Set the ProductCode if it is to be generated.
                    if ("ProductCode".Equals(propertyRow.Id.Id, StringComparison.Ordinal) && "*".Equals(propertyRow.Value, StringComparison.Ordinal))
                    {
                        propertyRow.Value = Common.GenerateGuid();

#if TODO_PATCHING // Is this still necessary?

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
#endif
                    }

                    // Add the property name and value to the variableCache.
                    if (variableCache != null)
                    {
                        var key = String.Concat("property.", propertyRow.Id.Id);
                        variableCache[key] = propertyRow.Value;
                    }
                }
            }

            // Sequence all the actions.
            {
                var command = new SequenceActionsCommand(this.Messaging, section);
                command.Execute();
            }

            {
                var command = new CreateSpecialPropertiesCommand(section);
                command.Execute();
            }

#if TODO_PATCHING
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
#endif

            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            this.Intermediate.UpdateLevel(Data.WindowsInstaller.IntermediateLevels.FullyBound);
            this.Messaging.Write(VerboseMessages.UpdatingFileInformation());

            // Extract files that come from binary .wixlibs and WixExtensions (this does not extract files from merge modules).
            {
                var command = new ExtractEmbeddedFilesCommand(this.BackendHelper, this.ExpectedEmbeddedFiles);
                command.Execute();

                trackedFiles.AddRange(command.TrackedFiles);
            }

            // This must occur after all variables and source paths have been resolved.
            List<FileFacade> fileFacades;
            {
                var command = new GetFileFacadesCommand(section);
                command.Execute();

                fileFacades = command.FileFacades;
            }

            // Retrieve file information from merge modules.
            if (SectionType.Product == section.Type)
            {
                var wixMergeTuples = section.Tuples.OfType<WixMergeTuple>().ToList();

                if (wixMergeTuples.Any())
                {
                    containsMergeModules = true;

                    var command = new ExtractMergeModuleFilesCommand(this.Messaging, section, wixMergeTuples);
                    command.FileFacades = fileFacades;
                    command.OutputInstallerVersion = installerVersion;
                    command.SuppressLayout = this.SuppressLayout;
                    command.IntermediateFolder = this.IntermediateFolder;
                    command.Execute();

                    fileFacades.AddRange(command.MergeModulesFileFacades);
                }
            }
            else if (SectionType.Patch == section.Type)
            {
                var command = new GetFileFacadesFromTransforms(this.Messaging, this.FileSystemManager, this.SubStorages);
                command.Execute();
                var filesFromTransforms = command.FileFacades;

                fileFacades.AddRange(filesFromTransforms);
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Gather information about files that do not come from merge modules.
            {
                var command = new UpdateFileFacadesCommand(this.Messaging, section);
                command.FileFacades = fileFacades;
                command.UpdateFileFacades = fileFacades.Where(f => !f.FromModule);
                command.OverwriteHash = true;
                command.VariableCache = variableCache;
                command.Execute();
            }

            // Assign files to media.
            Dictionary<int, MediaTuple> assignedMediaRows;
            Dictionary<MediaTuple, IEnumerable<FileFacade>> filesByCabinetMedia;
            IEnumerable<FileFacade> uncompressedFiles;
            {
                var command = new AssignMediaCommand(section, this.Messaging);
                command.FileFacades = fileFacades;
                command.FilesCompressed = compressed;
                command.Execute();

                assignedMediaRows = command.MediaRows;
                filesByCabinetMedia = command.FileFacadesByCabinetMedia;
                uncompressedFiles = command.UncompressedFileFacades;
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Now that the variable cache is populated, resolve any delayed fields.
            if (this.DelayedFields.Any())
            {
                var command = new ResolveDelayedFieldsCommand(this.Messaging, this.DelayedFields, variableCache);
                command.Execute();
            }

            // Set generated component guids.
            {
                var command = new CalculateComponentGuids(this.Messaging, this.BackendHelper, this.PathResolver, section);
                command.Execute();
            }

            // Add missing CreateFolder tuples to null-keypath components.
            {
                var command = new AddCreateFoldersCommand(section);
                command.Execute();
            }

            // Update file sequence.
            {
                var command = new UpdateMediaSequencesCommand(section, fileFacades);
                command.Execute();
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Time to create the output object. Try to put as much above here as possible, updating the IR is better.
            WindowsInstallerData output;
            {
                var command = new CreateOutputFromIRCommand(this.Messaging, section, tableDefinitions, this.BackendExtensions, this.WindowsInstallerBackendHelper);
                command.Execute();

                output = command.Output;
            }

            IEnumerable<string> suppressedTableNames = null;
            if (output.Type == OutputType.Module)
            {
                // Modularize identifiers.
                var modularize = new ModularizeCommand(output, modularizationSuffix, section.Tuples.OfType<WixSuppressModularizationTuple>());
                modularize.Execute();

                // Ensure all sequence tables in place because, mergemod.dll requires them.
                var unsuppress = new AddBackSuppresedSequenceTablesCommand(output, tableDefinitions);
                suppressedTableNames = unsuppress.Execute();
            }
            else if (output.Type == OutputType.Patch)
            {
                foreach (var storage in this.SubStorages)
                {
                    output.SubStorages.Add(storage);
                }
            }
            else // we can create instance transforms since Component Guids are set.
            {
                var command = new CreateInstanceTransformsCommand(section, output, tableDefinitions, this.BackendHelper);
                command.Execute();
            }

#if TODO_FINISH_UPDATE
            // Extended binder extensions can be called now that fields are resolved.
            {
                Table updatedFiles = this.Output.EnsureTable(this.TableDefinitions["WixBindUpdatedFiles"]);

                foreach (IBinderExtension extension in this.Extensions)
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
#endif

            this.ValidateComponentGuids(output);

            // Stop processing if an error previously occurred.
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Ensure the intermediate folder is created since delta patches will be
            // created there.
            Directory.CreateDirectory(this.IntermediateFolder);

            if (SectionType.Patch == section.Type && this.DeltaBinaryPatch)
            {
                var command = new CreateDeltaPatchesCommand(fileFacades, this.IntermediateFolder, section.Tuples.OfType<WixPatchIdTuple>().FirstOrDefault());
                command.Execute();
            }

            // create cabinet files and process uncompressed files
            var layoutDirectory = Path.GetDirectoryName(this.OutputPath);
            if (!this.SuppressLayout || OutputType.Module == output.Type)
            {
                this.Messaging.Write(VerboseMessages.CreatingCabinetFiles());

                var command = new CreateCabinetsCommand(this.ServiceProvider, this.BackendHelper);
                command.CabbingThreadCount = this.CabbingThreadCount;
                command.CabCachePath = this.CabCachePath;
                command.DefaultCompressionLevel = this.DefaultCompressionLevel;
                command.Output = output;
                command.Messaging = this.Messaging;
                command.BackendExtensions = this.BackendExtensions;
                command.LayoutDirectory = layoutDirectory;
                command.Compressed = compressed;
                command.ModularizationSuffix = modularizationSuffix;
                command.FileRowsByCabinet = filesByCabinetMedia;
                command.ResolveMedia = this.ResolveMedia;
                command.TableDefinitions = tableDefinitions;
                command.IntermediateFolder = this.IntermediateFolder;
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
                trackedFiles.AddRange(command.TrackedFiles);
            }

            if (output.Type == OutputType.Patch)
            {
                // Copy output data back into the transforms.
                var command = new UpdateTransformsWithFileFacades(this.Messaging, output, this.SubStorages, tableDefinitions, fileFacades);
                command.Execute();
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Generate database file.
            this.Messaging.Write(VerboseMessages.GeneratingDatabase());

            {
                var trackMsi = this.BackendHelper.TrackFile(this.OutputPath, TrackedFileType.Final);
                trackedFiles.Add(trackMsi);

                var command = new GenerateDatabaseCommand(this.Messaging, this.BackendHelper, this.FileSystemManager, output, trackMsi.Path, tableDefinitions, this.IntermediateFolder, this.Codepage, keepAddedColumns: false, this.SuppressAddingValidationRows, useSubdirectory: false);
                command.Execute();

                trackedFiles.AddRange(command.GeneratedTemporaryFiles);
            }

            // Stop processing if an error previously occurred.
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Merge modules.
            if (containsMergeModules)
            {
                this.Messaging.Write(VerboseMessages.MergingModules());

                var command = new MergeModulesCommand(this.Messaging, fileFacades, section, suppressedTableNames, this.OutputPath, this.IntermediateFolder);
                command.Execute();
            }

            if (this.Messaging.EncounteredError)
            {
                return null;
            }

#if TODO_FINISH_VALIDATION
            // Validate the output if there is an MSI validator.
            if (null != this.Validator)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                // set the output file for source line information
                this.Validator.Output = this.Output;

                Messaging.Instance.Write(WixVerboses.ValidatingDatabase());

                this.Validator.Validate(this.OutputPath);

                stopwatch.Stop();
                Messaging.Instance.Write(WixVerboses.ValidatedDatabase(stopwatch.ElapsedMilliseconds));

                // Stop processing if an error occurred.
                if (Messaging.Instance.EncounteredError)
                {
                    return;
                }
            }
#endif

            // Process uncompressed files.
            if (!this.Messaging.EncounteredError && !this.SuppressLayout && uncompressedFiles.Any())
            {
                var command = new ProcessUncompressedFilesCommand(section, this.BackendHelper, this.PathResolver);
                command.Compressed = compressed;
                command.FileFacades = uncompressedFiles;
                command.LayoutDirectory = layoutDirectory;
                command.LongNamesInImage = longNames;
                command.ResolveMedia = this.ResolveMedia;
                command.DatabasePath = this.OutputPath;
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
                trackedFiles.AddRange(command.TrackedFiles);
            }

            // TODO: this is not sufficient to collect all Input files (for example, it misses Binary and Icon tables).
            trackedFiles.AddRange(fileFacades.Select(f => this.BackendHelper.TrackFile(f.SourcePath, TrackedFileType.Input, f.SourceLineNumber)));

            var result = this.ServiceProvider.GetService<IBindResult>();
            result.FileTransfers = fileTransfers;
            result.TrackedFiles = trackedFiles;
            result.Wixout = this.CreateWixout(trackedFiles, this.Intermediate, output);

            return result;
        }

        private WixOutput CreateWixout(List<ITrackedFile> trackedFiles, Intermediate intermediate, WindowsInstallerData output)
        {
            WixOutput wixout;

            if (String.IsNullOrEmpty(this.OutputPdbPath))
            {
                wixout = WixOutput.Create();
            }
            else
            {
                var trackPdb = this.BackendHelper.TrackFile(this.OutputPdbPath, TrackedFileType.Final);
                trackedFiles.Add(trackPdb);

                wixout = WixOutput.Create(trackPdb.Path);
            }

            intermediate.Save(wixout);

            output.Save(wixout);

            wixout.Reopen();

            return wixout;
        }

        /// <summary>
        /// Validate that there are no duplicate GUIDs in the output.
        /// </summary>
        /// <remarks>
        /// Duplicate GUIDs without conditions are an error condition; with conditions, it's a
        /// warning, as the conditions might be mutually exclusive.
        /// </remarks>
        private void ValidateComponentGuids(WindowsInstallerData output)
        {
            if (output.TryGetTable("Component", out var componentTable))
            {
                var componentGuidConditions = new Dictionary<string, bool>(componentTable.Rows.Count);

                foreach (Data.WindowsInstaller.Rows.ComponentRow row in componentTable.Rows)
                {
                    // We don't care about unmanaged components and if there's a * GUID remaining,
                    // there's already an error that prevented it from being replaced with a real GUID.
                    if (!String.IsNullOrEmpty(row.Guid) && "*" != row.Guid)
                    {
                        var thisComponentHasCondition = !String.IsNullOrEmpty(row.Condition);
                        var allComponentsHaveConditions = thisComponentHasCondition;

                        if (componentGuidConditions.ContainsKey(row.Guid))
                        {
                            allComponentsHaveConditions = thisComponentHasCondition && componentGuidConditions[row.Guid];

                            if (allComponentsHaveConditions)
                            {
                                this.Messaging.Write(WarningMessages.DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions(row.SourceLineNumbers, row.Component, row.Guid));
                            }
                            else
                            {
                                this.Messaging.Write(ErrorMessages.DuplicateComponentGuids(row.SourceLineNumbers, row.Component, row.Guid));
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
        private void UpdateControlText(WindowsInstallerData output)
        {
            var command = new UpdateControlTextCommand();
            command.Messaging = this.Messaging;
            command.BBControlTable = output.Tables["BBControl"];
            command.WixBBControlTable = output.Tables["WixBBControl"];
            command.ControlTable = output.Tables["Control"];
            command.WixControlTable = output.Tables["WixControl"];
            command.Execute();
        }

        private string ResolveMedia(MediaTuple media, string mediaLayoutDirectory, string layoutDirectory)
        {
            string layout = null;

            foreach (var extension in this.BackendExtensions)
            {
                layout = extension.ResolveMedia(media, mediaLayoutDirectory, layoutDirectory);
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
    }
}
