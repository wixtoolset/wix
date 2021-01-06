// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
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
            Platform platform;
            string modularizationSuffix;
            {
                var command = new BindSummaryInfoCommand(section);
                command.Execute();

                compressed = command.Compressed;
                longNames = command.LongNames;
                installerVersion = command.InstallerVersion;
                platform = command.Platform;
                modularizationSuffix = command.ModularizationSuffix;
            }

            // Add binder variables for all properties.
            if (SectionType.Product == section.Type || variableCache != null)
            {
                foreach (var propertyRow in section.Symbols.OfType<PropertySymbol>())
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
                var command = new AddRequiredStandardDirectories(section, platform);
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
            if (SectionType.Patch == section.Type)
            {
                var command = new GetFileFacadesFromTransforms(this.Messaging, this.FileSystemManager, this.SubStorages);
                command.Execute();

                fileFacades = command.FileFacades;
            }
            else
            {
                var command = new GetFileFacadesCommand(section);
                command.Execute();

                fileFacades = command.FileFacades;
            }

            // Retrieve file information from merge modules.
            if (SectionType.Product == section.Type)
            {
                var wixMergeSymbols = section.Symbols.OfType<WixMergeSymbol>().ToList();

                if (wixMergeSymbols.Any())
                {
                    containsMergeModules = true;

                    var command = new ExtractMergeModuleFilesCommand(this.Messaging, wixMergeSymbols, fileFacades, installerVersion, this.IntermediateFolder, this.SuppressLayout);
                    command.Execute();

                    fileFacades.AddRange(command.MergeModulesFileFacades);
                }
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Gather information about files that do not come from merge modules.
            {
                var command = new UpdateFileFacadesCommand(this.Messaging, section, fileFacades, fileFacades.Where(f => !f.FromModule), variableCache, overwriteHash: true);
                command.Execute();
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

#if TODO_FINISH_UPDATE // use symbols instead of rows
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
                    UpdateFileFacadesCommand command = new UpdateFileFacadesCommand(this.Messaging, section, fileFacades, updateFileFacades, variableCache, overwriteHash: false);
                    //command.FileFacades = fileFacades;
                    //command.UpdateFileFacades = updatedFileFacades;
                    //command.ModularizationGuid = modularizationGuid;
                    //command.Output = this.Output;
                    //command.OverwriteHash = true;
                    //command.TableDefinitions = this.TableDefinitions;
                    //command.VariableCache = variableCache;
                    command.Execute();
                }
            }
#endif

            // Set generated component guids.
            {
                var command = new CalculateComponentGuids(this.Messaging, this.BackendHelper, this.PathResolver, section, platform);
                command.Execute();
            }

            {
                var command = new ValidateComponentGuidsCommand(this.Messaging, section);
                command.Execute();
            }

            // Add missing CreateFolder symbols to null-keypath components.
            {
                var command = new AddCreateFoldersCommand(section);
                command.Execute();
            }

            // Update symbols that reference text files on disk.
            {
                var command = new UpdateFromTextFilesCommand(this.Messaging, section);
                command.Execute();
            }

            // Assign files to media and update file sequences.
            Dictionary<MediaSymbol, IEnumerable<FileFacade>> filesByCabinetMedia;
            IEnumerable<FileFacade> uncompressedFiles;
            {
                var order = new OptimizeFileFacadesOrderCommand(this.BackendHelper, this.PathResolver, section, platform, fileFacades);
                order.Execute();

                fileFacades = order.FileFacades;

                var assign = new AssignMediaCommand(section, this.Messaging, fileFacades, compressed);
                assign.Execute();

                filesByCabinetMedia = assign.FileFacadesByCabinetMedia;
                uncompressedFiles = assign.UncompressedFileFacades;

                var update = new UpdateMediaSequencesCommand(section, fileFacades);
                update.Execute();
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
                var modularize = new ModularizeCommand(output, modularizationSuffix, section.Symbols.OfType<WixSuppressModularizationSymbol>());
                modularize.Execute();

                // Ensure all sequence tables in place because, mergemod.dll requires them.
                var unsuppress = new AddBackSuppressedSequenceTablesCommand(output, tableDefinitions);
                suppressedTableNames = unsuppress.Execute();
            }
            else if (output.Type == OutputType.Patch)
            {
                foreach (var storage in this.SubStorages)
                {
                    output.SubStorages.Add(storage);
                }
            }

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
                var command = new CreateDeltaPatchesCommand(fileFacades, this.IntermediateFolder, section.Symbols.OfType<WixPatchIdSymbol>().FirstOrDefault());
                command.Execute();
            }

            // create cabinet files and process uncompressed files
            var layoutDirectory = Path.GetDirectoryName(this.OutputPath);
            if (!this.SuppressLayout || OutputType.Module == output.Type)
            {
                this.Messaging.Write(VerboseMessages.CreatingCabinetFiles());

                var mediaTemplate = section.Symbols.OfType<WixMediaTemplateSymbol>().FirstOrDefault();

                var command = new CreateCabinetsCommand(this.ServiceProvider, this.BackendHelper, mediaTemplate);
                command.CabbingThreadCount = this.CabbingThreadCount;
                command.CabCachePath = this.CabCachePath;
                command.DefaultCompressionLevel = this.DefaultCompressionLevel;
                command.Output = output;
                command.Messaging = this.Messaging;
                command.BackendExtensions = this.BackendExtensions;
                command.LayoutDirectory = layoutDirectory;
                command.Compressed = compressed;
                command.ModularizationSuffix = modularizationSuffix;
                command.FileFacadesByCabinet = filesByCabinetMedia;
                command.ResolveMedia = this.ResolveMedia;
                command.TableDefinitions = tableDefinitions;
                command.IntermediateFolder = this.IntermediateFolder;
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
                trackedFiles.AddRange(command.TrackedFiles);
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // We can create instance transforms since Component Guids and Outputs are created.
            if (output.Type == OutputType.Product)
            {
                var command = new CreateInstanceTransformsCommand(section, output, tableDefinitions, this.BackendHelper);
                command.Execute();
            }
            else if (output.Type == OutputType.Patch)
            {
                // Copy output data back into the transforms.
                var command = new UpdateTransformsWithFileFacades(this.Messaging, output, this.SubStorages, tableDefinitions, fileFacades);
                command.Execute();
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

        private string ResolveMedia(MediaSymbol media, string mediaLayoutDirectory, string layoutDirectory)
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
