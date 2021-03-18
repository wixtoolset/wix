// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

        public BindDatabaseCommand(IBindContext context, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtension, string cubeFile) : this(context, backendExtension, null, cubeFile)
        {
        }

        public BindDatabaseCommand(IBindContext context, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtension, IEnumerable<SubStorage> subStorages, string cubeFile)
        {
            this.ServiceProvider = context.ServiceProvider;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();

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

            this.SuppressValidation = context.SuppressValidation;
            this.Ices = context.Ices;
            this.SuppressedIces = context.SuppressIces;
            this.CubeFiles = String.IsNullOrEmpty(cubeFile) ? null : new[] { cubeFile };

            this.BackendExtensions = backendExtension;
        }

        public IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

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

        private bool SuppressValidation { get; }

        private IEnumerable<string> Ices { get; }

        private IEnumerable<string> SuppressedIces { get; }

        private IEnumerable<string> CubeFiles { get; }

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
                var branding = this.ServiceProvider.GetService<IWixBranding>();

                var command = new BindSummaryInfoCommand(section, this.WindowsInstallerBackendHelper, branding);
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
                        propertyRow.Value = this.WindowsInstallerBackendHelper.CreateGuid();

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

            if (section.Type == SectionType.Product || section.Type == SectionType.Module)
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
                var extractedFiles = this.WindowsInstallerBackendHelper.ExtractEmbeddedFiles(this.ExpectedEmbeddedFiles);

                trackedFiles.AddRange(extractedFiles);
            }

            // This must occur after all variables and source paths have been resolved.
            List<IFileFacade> fileFacades;
            if (SectionType.Patch == section.Type)
            {
                var command = new GetFileFacadesFromTransforms(this.Messaging, this.WindowsInstallerBackendHelper, this.FileSystemManager, this.SubStorages);
                command.Execute();

                fileFacades = command.FileFacades;
            }
            else
            {
                var command = new GetFileFacadesCommand(section, this.WindowsInstallerBackendHelper);
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

                    var command = new ExtractMergeModuleFilesCommand(this.Messaging, this.WindowsInstallerBackendHelper, wixMergeSymbols, fileFacades, installerVersion, this.IntermediateFolder, this.SuppressLayout);
                    command.Execute();

                    fileFacades.AddRange(command.MergeModulesFileFacades);
                }
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Process SoftwareTags in MSI packages.
            if (SectionType.Product == section.Type)
            {
                var softwareTags = section.Symbols.OfType<WixProductTagSymbol>().ToList();

                if (softwareTags.Any())
                {
                    var command = new ProcessPackageSoftwareTagsCommand(section, softwareTags, this.IntermediateFolder);
                    command.Execute();
                }
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
                this.WindowsInstallerBackendHelper.ResolveDelayedFields(this.DelayedFields, variableCache);
            }

            // Update symbols that reference text files on disk.
            {
                var command = new UpdateFromTextFilesCommand(this.Messaging, section);
                command.Execute();
            }

            // Add missing CreateFolder symbols to null-keypath components.
            {
                var command = new AddCreateFoldersCommand(section);
                command.Execute();
            }

            // Process dependency references.
            if (SectionType.Product == section.Type || SectionType.Module == section.Type)
            {
                var dependencyRefs = section.Symbols.OfType<WixDependencyRefSymbol>().ToList();

                if (dependencyRefs.Any())
                {
                    var command = new ProcessDependencyReferencesCommand(this.WindowsInstallerBackendHelper, section, dependencyRefs);
                    command.Execute();
                }
            }

            // If there are any backend extensions, give them the opportunity to process
            // the section now that the fields have all be resolved.
            //
            if (this.BackendExtensions.Any())
            {
                using (new IntermediateFieldContext("wix.bind.finalize"))
                {
                    foreach (var extension in this.BackendExtensions)
                    {
                        extension.SymbolsFinalized(section);
                    }

                    var reresolvedFiles = section.Symbols
                                                 .OfType<FileSymbol>()
                                                 .Where(s => s.Fields.Any(f => f?.Context == "wix.bind.finalize"))
                                                 .ToList();

                    if (reresolvedFiles.Any())
                    {
                        var updatedFacades = reresolvedFiles.Select(f => fileFacades.First(ff => ff.Id == f.Id?.Id));

                        var command = new UpdateFileFacadesCommand(this.Messaging, section, fileFacades, updatedFacades, variableCache, overwriteHash: false);
                        command.Execute();
                    }
                }

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }
            }

            // Set generated component guids.
            {
                var command = new CalculateComponentGuids(this.Messaging, this.WindowsInstallerBackendHelper, this.PathResolver, section, platform);
                command.Execute();
            }

            {
                var command = new ValidateComponentGuidsCommand(this.Messaging, section);
                command.Execute();
            }

            // Assign files to media and update file sequences.
            Dictionary<MediaSymbol, IEnumerable<IFileFacade>> filesByCabinetMedia;
            IEnumerable<IFileFacade> uncompressedFiles;
            {
                var order = new OptimizeFileFacadesOrderCommand(this.WindowsInstallerBackendHelper, this.PathResolver, section, platform, fileFacades);
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

            // Time to create the WindowsInstallerData object. Try to put as much above here as possible, updating the IR is better.
            WindowsInstallerData data;
            {
                var command = new CreateWindowsInstallerDataFromIRCommand(this.Messaging, section, tableDefinitions, this.BackendExtensions, this.WindowsInstallerBackendHelper);
                data = command.Execute();
            }

            IEnumerable<string> suppressedTableNames = null;
            if (data.Type == OutputType.Module)
            {
                // Modularize identifiers.
                var modularize = new ModularizeCommand(this.WindowsInstallerBackendHelper, data, modularizationSuffix, section.Symbols.OfType<WixSuppressModularizationSymbol>());
                modularize.Execute();

                // Ensure all sequence tables in place because, mergemod.dll requires them.
                var unsuppress = new AddBackSuppressedSequenceTablesCommand(data, tableDefinitions);
                suppressedTableNames = unsuppress.Execute();
            }
            else if (data.Type == OutputType.Patch)
            {
                foreach (var storage in this.SubStorages)
                {
                    data.SubStorages.Add(storage);
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
            if (!this.SuppressLayout || OutputType.Module == data.Type)
            {
                this.Messaging.Write(VerboseMessages.CreatingCabinetFiles());

                var mediaTemplate = section.Symbols.OfType<WixMediaTemplateSymbol>().FirstOrDefault();

                var command = new CreateCabinetsCommand(this.ServiceProvider, this.WindowsInstallerBackendHelper, mediaTemplate);
                command.CabbingThreadCount = this.CabbingThreadCount;
                command.CabCachePath = this.CabCachePath;
                command.DefaultCompressionLevel = this.DefaultCompressionLevel;
                command.Data = data;
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
            if (data.Type == OutputType.Product)
            {
                var command = new CreateInstanceTransformsCommand(section, data, tableDefinitions, this.WindowsInstallerBackendHelper);
                command.Execute();
            }
            else if (data.Type == OutputType.Patch)
            {
                // Copy output data back into the transforms.
                var command = new UpdateTransformsWithFileFacades(this.Messaging, data, this.SubStorages, tableDefinitions, fileFacades);
                command.Execute();
            }

            // Generate database file.
            this.Messaging.Write(VerboseMessages.GeneratingDatabase());

            {
                var trackMsi = this.WindowsInstallerBackendHelper.TrackFile(this.OutputPath, TrackedFileType.Final);
                trackedFiles.Add(trackMsi);

                var command = new GenerateDatabaseCommand(this.Messaging, this.WindowsInstallerBackendHelper, this.FileSystemManager, data, trackMsi.Path, tableDefinitions, this.IntermediateFolder, this.Codepage, keepAddedColumns: false, this.SuppressAddingValidationRows, useSubdirectory: false);
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

            // Validate the output if there are CUBe files and we're not explicitly suppressing validation.
            if (this.CubeFiles != null && !this.SuppressValidation)
            {
                var command = new ValidateDatabaseCommand(this.Messaging, this.WindowsInstallerBackendHelper, this.IntermediateFolder, data, this.OutputPath, this.CubeFiles, this.Ices, this.SuppressedIces);
                command.Execute();

                trackedFiles.AddRange(command.TrackedFiles);
            }

            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Process uncompressed files.
            if (!this.SuppressLayout && uncompressedFiles.Any())
            {
                var command = new ProcessUncompressedFilesCommand(section, this.WindowsInstallerBackendHelper, this.PathResolver);
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
            trackedFiles.AddRange(fileFacades.Select(f => this.WindowsInstallerBackendHelper.TrackFile(f.SourcePath, TrackedFileType.Input, f.SourceLineNumber)));

            var result = this.ServiceProvider.GetService<IBindResult>();
            result.FileTransfers = fileTransfers;
            result.TrackedFiles = trackedFiles;
            result.Wixout = this.CreateWixout(trackedFiles, this.Intermediate, data);

            return result;
        }

        private WixOutput CreateWixout(List<ITrackedFile> trackedFiles, Intermediate intermediate, WindowsInstallerData data)
        {
            WixOutput wixout;

            if (String.IsNullOrEmpty(this.OutputPdbPath))
            {
                wixout = WixOutput.Create();
            }
            else
            {
                var trackPdb = this.WindowsInstallerBackendHelper.TrackFile(this.OutputPdbPath, TrackedFileType.Final);
                trackedFiles.Add(trackPdb);

                wixout = WixOutput.Create(trackPdb.Path);
            }

            intermediate.Save(wixout);

            data.Save(wixout);

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
