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

        public BindDatabaseCommand(IBindContext context, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtension, IEnumerable<SubStorage> patchSubStorages = null)
        {
            this.ServiceProvider = context.ServiceProvider;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();

            this.WindowsInstallerBackendHelper = context.ServiceProvider.GetService<IWindowsInstallerBackendHelper>();
            this.FileSystem = context.ServiceProvider.GetService<IFileSystem>();
            this.PathResolver = context.ServiceProvider.GetService<IPathResolver>();

            this.CabbingThreadCount = context.CabbingThreadCount;
            this.CabCachePath = context.CabCachePath;
            this.DefaultCompressionLevel = context.DefaultCompressionLevel;
            this.DelayedFields = context.DelayedFields;
            this.ExpectedEmbeddedFiles = context.ExpectedEmbeddedFiles;
            this.FileSystemManager = new FileSystemManager(this.FileSystem, context.FileSystemExtensions);
            this.Intermediate = context.IntermediateRepresentation;
            this.IntermediateFolder = context.IntermediateFolder;
            this.OutputPath = context.OutputPath;
            this.OutputPdbPath = context.PdbPath;
            this.PdbType = context.PdbType;
            this.ResolvedCodepage = context.ResolvedCodepage;
            this.ResolvedSummaryInformationCodepage = context.ResolvedSummaryInformationCodepage;
            this.ResolvedLcid = context.ResolvedLcid;
            this.SuppressLayout = context.SuppressLayout;

            this.PatchSubStorages = patchSubStorages;

            this.BackendExtensions = backendExtension;
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IWindowsInstallerBackendHelper WindowsInstallerBackendHelper { get; }

        private  IFileSystem FileSystem { get; }

        private IPathResolver PathResolver { get; }

        private int CabbingThreadCount { get; }

        private string CabCachePath { get; }

        private CompressionLevel? DefaultCompressionLevel { get; }

        public IEnumerable<IDelayedField> DelayedFields { get; }

        public IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; }

        public FileSystemManager FileSystemManager { get; }

        public bool DeltaBinaryPatch { get; set; }

        private IEnumerable<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        private IEnumerable<SubStorage> PatchSubStorages { get; }

        private Intermediate Intermediate { get; }

        private string OutputPath { get; }

        public PdbType PdbType { get; set; }

        private string OutputPdbPath { get; }

        private int? ResolvedCodepage { get; }

        private int? ResolvedSummaryInformationCodepage { get; }

        private int? ResolvedLcid { get; }

        private bool SuppressAddingValidationRows { get; }

        private bool SuppressLayout { get; }

        private string IntermediateFolder { get; }

        public IBindResult Execute()
        {
            if (!this.Intermediate.HasLevel(Data.IntermediateLevels.Linked) || !this.Intermediate.HasLevel(Data.IntermediateLevels.Resolved))
            {
                this.Messaging.Write(ErrorMessages.IntermediatesMustBeResolved(this.Intermediate.Id));
            }

            var section = this.Intermediate.Sections.Single();

            var packageSymbol = (section.Type == SectionType.Package) ? this.GetSingleSymbol<WixPackageSymbol>(section) : null;
            var moduleSymbol = (section.Type == SectionType.Module) ? this.GetSingleSymbol<WixModuleSymbol>(section) : null;
            var patchSymbol = (section.Type == SectionType.Patch) ? this.GetSingleSymbol<WixPatchSymbol>(section) : null;

            var fileTransfers = new List<IFileTransfer>();
            var trackedFiles = new List<ITrackedFile>();

            var containsMergeModules = false;

            // Load standard tables, authored custom tables, and extension custom tables.
            TableDefinitionCollection tableDefinitions;
            {
                var command = new LoadTableDefinitionsCommand(this.Messaging, section, this.BackendExtensions);
                command.Execute();

                tableDefinitions = command.TableDefinitions;
            }

            if (section.Type == SectionType.Package)
            {
                this.ProcessProductVersion(packageSymbol, section, validate: false);
            }

            // Calculate codepage
            var codepage = this.CalculateCodepage(packageSymbol, moduleSymbol, patchSymbol);

            // Process properties and create the delayed variable cache if needed.
            Dictionary<string, string> variableCache = null;
            string productLanguage = null;
            {
                var command = new ProcessPropertiesCommand(section, packageSymbol, this.ResolvedLcid ?? 0, this.DelayedFields.Any(), this.WindowsInstallerBackendHelper);
                command.Execute();

                variableCache = command.DelayedVariablesCache;
                productLanguage = command.ProductLanguage;
            }

            // Process the summary information table after properties are processed.
            bool compressed;
            bool longNames;
            int installerVersion;
            Platform platform;
            string modularizationSuffix;
            {
                var branding = this.ServiceProvider.GetService<IWixBranding>();

                var command = new BindSummaryInfoCommand(section, this.ResolvedSummaryInformationCodepage, productLanguage, this.WindowsInstallerBackendHelper, branding);
                command.Execute();

                compressed = command.Compressed;
                longNames = command.LongNames;
                installerVersion = command.InstallerVersion;
                platform = command.Platform;
                modularizationSuffix = command.ModularizationSuffix;
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

            // Add missing CreateFolder symbols to null-keypath components.
            {
                var command = new AddCreateFoldersCommand(section);
                command.Execute();
            }

            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Process dependency references.
            if (SectionType.Package == section.Type || SectionType.Module == section.Type)
            {
                var dependencyRefs = section.Symbols.OfType<WixDependencyRefSymbol>().ToList();

                if (dependencyRefs.Any())
                {
                    var command = new ProcessDependencyReferencesCommand(this.WindowsInstallerBackendHelper, section, dependencyRefs);
                    command.Execute();
                }
            }

            // Process SoftwareTags in MSI packages.
            if (SectionType.Package == section.Type)
            {
                var softwareTags = section.Symbols.OfType<WixPackageTagSymbol>().ToList();

                if (softwareTags.Any())
                {
                    var command = new ProcessPackageSoftwareTagsCommand(section, this.WindowsInstallerBackendHelper, this.FileSystem, softwareTags, this.IntermediateFolder);
                    command.Execute();

                    trackedFiles.AddRange(command.TrackedFiles);
                }
            }

            // Extract files that come from binary .wixlibs and WixExtensions (this does not extract files from merge modules).
            {
                var extractedFiles = this.WindowsInstallerBackendHelper.ExtractEmbeddedFiles(this.ExpectedEmbeddedFiles);

                trackedFiles.AddRange(extractedFiles);
            }

            // Update symbols that reference text files on disk. Some of those files may have come from .wixlibs and WixExtensions
            // extracted above.
            {
                var command = new UpdateFromTextFilesCommand(this.Messaging, section);
                command.Execute();
            }

            this.Intermediate.UpdateLevel(Data.WindowsInstaller.IntermediateLevels.FullyBound);
            this.Messaging.Write(VerboseMessages.UpdatingFileInformation());

            // This must occur after all variables and source paths have been resolved.
            List<IFileFacade> allFileFacades;
            List<IFileFacade> fileFacadesFromIntermediate;
            List<IFileFacade> fileFacadesFromModule = null;
            if (section.Type == SectionType.Patch)
            {
                var command = new GetFileFacadesFromTransforms(this.Messaging, this.WindowsInstallerBackendHelper, this.FileSystemManager, this.PatchSubStorages);
                command.Execute();

                allFileFacades = fileFacadesFromIntermediate = command.FileFacades;
            }
            else
            {
                var command = new GetFileFacadesCommand(section, this.WindowsInstallerBackendHelper);
                command.Execute();

                allFileFacades = fileFacadesFromIntermediate = command.FileFacades;
            }

            // Retrieve file information from merge modules.
            if (SectionType.Package == section.Type)
            {
                var wixMergeSymbols = section.Symbols.OfType<WixMergeSymbol>().ToList();

                if (wixMergeSymbols.Any())
                {
                    containsMergeModules = true;

                    var command = new ExtractMergeModuleFilesCommand(this.Messaging, this.WindowsInstallerBackendHelper, wixMergeSymbols, fileFacadesFromIntermediate, installerVersion, this.IntermediateFolder, this.SuppressLayout);
                    command.Execute();

                    fileFacadesFromModule = new List<IFileFacade>(command.MergeModulesFileFacades);
                    allFileFacades.AddRange(fileFacadesFromModule);
                    trackedFiles.AddRange(command.TrackedFiles);
                }
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Gather information about files that do not come from merge modules.
            {
                var command = new UpdateFileFacadesCommand(this.Messaging, this.FileSystem, section, allFileFacades, fileFacadesFromIntermediate, variableCache, overwriteHash: true);
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

            // Now that delayed fields are processed, fixup the package version (if needed) and validate it
            // which will short circuit duplicate errors later if the ProductVersion is invalid.
            if (SectionType.Package == section.Type)
            {
                this.ProcessProductVersion(packageSymbol, section, validate: true);
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
                        var updatedFacades = reresolvedFiles.Select(f => allFileFacades.First(ff => ff.Id == f.Id?.Id));

                        var command = new UpdateFileFacadesCommand(this.Messaging, this.FileSystem, section, allFileFacades, updatedFacades, variableCache, overwriteHash: false);
                        command.Execute();
                    }
                }

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }
            }

            if (SectionType.Package == section.Type)
            {
                var command = new ValidateWindowsInstallerProductConstraints(this.Messaging, section);
                command.Execute();

                if (this.Messaging.EncounteredError)
                {
                    return null;
                }
            }

            // Assign files to media and update file sequences.
            Dictionary<MediaSymbol, IEnumerable<IFileFacade>> filesByCabinetMedia;
            IEnumerable<IFileFacade> uncompressedFiles;
            {
                var order = new OptimizeFileFacadesOrderCommand(this.WindowsInstallerBackendHelper, this.PathResolver, section, platform, allFileFacades);
                order.Execute();

                allFileFacades = order.FileFacades;

                var assign = new AssignMediaCommand(section, this.Messaging, allFileFacades, compressed);
                assign.Execute();

                filesByCabinetMedia = assign.FileFacadesByCabinetMedia;
                uncompressedFiles = assign.UncompressedFileFacades;

                var update = new UpdateMediaSequencesCommand(section, allFileFacades);
                update.Execute();
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Copy updated file facade data back into the transforms or symbols as appropriate.
            if (section.Type == SectionType.Patch)
            {
                var command = new UpdateTransformsWithFileFacades(this.Messaging, section, this.PatchSubStorages, tableDefinitions, allFileFacades);
                command.Execute();
            }
            else
            {
                var command = new UpdateSymbolsWithFileFacadesCommand(section, allFileFacades);
                command.Execute();
            }

            // Set generated component guids and validate all guids.
            {
                var command = new FinalizeComponentGuids(this.Messaging, this.WindowsInstallerBackendHelper, this.PathResolver, section, platform);
                command.Execute();
            }

            // Time to create the WindowsInstallerData object. Try to put as much above here as possible, updating the IR is better.
            WindowsInstallerData data;
            {
                var command = new CreateWindowsInstallerDataFromIRCommand(this.Messaging, section, tableDefinitions, codepage, this.BackendExtensions, this.WindowsInstallerBackendHelper);
                data = command.Execute();
            }

            IEnumerable<string> suppressedTableNames = null;
            if (data.Type == OutputType.Module)
            {
                // Modularize identifiers.
                var modularize = new ModularizeCommand(this.WindowsInstallerBackendHelper, data, modularizationSuffix, section.Symbols.OfType<WixSuppressModularizationSymbol>());
                modularize.Execute();

                // Ensure all sequence tables in place because mergemod.dll requires them.
                var unsuppress = new AddBackSuppressedSequenceTablesCommand(data, tableDefinitions);
                suppressedTableNames = unsuppress.Execute();
            }
            else if (data.Type == OutputType.Package) // we can create instance transforms since Component Guids and Outputs are created.
            {
                var command = new CreateInstanceTransformsCommand(section, data, tableDefinitions, this.WindowsInstallerBackendHelper);
                command.Execute();

                foreach (var storage in command.SubStorages)
                {
                    data.SubStorages.Add(storage);
                }
            }
            else if (data.Type == OutputType.Patch)
            {
                foreach (var storage in this.PatchSubStorages)
                {
                    data.SubStorages.Add(storage);
                }
            }

            // Stop processing if an error previously occurred.
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            if (section.Type == SectionType.Patch && this.DeltaBinaryPatch)
            {
                var command = new CreateDeltaPatchesCommand(allFileFacades, this.IntermediateFolder, section.Symbols.OfType<WixPatchSymbol>().FirstOrDefault());
                command.Execute();
            }

            // Create cabinet files.
            if (!this.SuppressLayout || OutputType.Module == data.Type)
            {
                this.Messaging.Write(VerboseMessages.CreatingCabinetFiles());

                var command = new CreateCabinetsCommand(this.ServiceProvider, this.Messaging, this.WindowsInstallerBackendHelper, this.BackendExtensions, section, this.CabCachePath, this.CabbingThreadCount, this.OutputPath, this.IntermediateFolder, this.DefaultCompressionLevel, compressed, modularizationSuffix, filesByCabinetMedia, data, tableDefinitions, this.ResolveMedia);
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
                trackedFiles.AddRange(command.TrackedFiles);
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            // Generate database file.
            {
                this.Messaging.Write(VerboseMessages.GeneratingDatabase());

                var trackMsi = this.WindowsInstallerBackendHelper.TrackFile(this.OutputPath, TrackedFileType.BuiltTargetOutput);
                trackedFiles.Add(trackMsi);

                var command = new GenerateDatabaseCommand(this.Messaging, this.WindowsInstallerBackendHelper, this.FileSystem, this.FileSystemManager, data, trackMsi.Path, tableDefinitions, this.IntermediateFolder, keepAddedColumns: false, this.SuppressAddingValidationRows, useSubdirectory: false);
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

                var command = new MergeModulesCommand(this.Messaging, this.WindowsInstallerBackendHelper, fileFacadesFromModule, section, suppressedTableNames, this.OutputPath, this.IntermediateFolder);
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
                var command = new ProcessUncompressedFilesCommand(section, this.WindowsInstallerBackendHelper, this.PathResolver, uncompressedFiles, this.OutputPath, compressed, longNames, this.ResolveMedia);
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
                trackedFiles.AddRange(command.TrackedFiles);
            }

            // Best effort check to see if the MSI file is too large for the Windows Installer.
            try
            {
                var fi = new FileInfo(this.OutputPath);
                if (fi.Length > Int32.MaxValue)
                {
                    this.Messaging.Write(WarningMessages.WindowsInstallerFileTooLarge(null, this.OutputPath, data.Type.ToString()));
                }
            }
            catch
            {
            }

            var trackedInputFiles = this.TrackInputFiles(data, trackedFiles);
            trackedFiles.AddRange(trackedInputFiles);

            var result = this.ServiceProvider.GetService<IBindResult>();
            result.FileTransfers = fileTransfers;
            result.TrackedFiles = trackedFiles;
            result.Wixout = this.CreateWixout(trackedFiles, this.Intermediate, data);

            return result;
        }

        private void ProcessProductVersion(WixPackageSymbol packageSymbol, IntermediateSection section, bool validate)
        {
            if (this.WindowsInstallerBackendHelper.TryParseMsiProductVersion(packageSymbol.Version, strict: false, out var version))
            {
                if (packageSymbol.Version != version)
                {
                    packageSymbol.Version = version;

                    var productVersionProperty = section.Symbols.OfType<PropertySymbol>().FirstOrDefault(p => p.Id.Id == "ProductVersion");
                    productVersionProperty.Value = version;
                }
            }
            else if (validate)
            {
                this.Messaging.Write(WarningMessages.InvalidMsiProductVersion(packageSymbol.SourceLineNumbers, packageSymbol.Version));
            }
        }

        private int CalculateCodepage(WixPackageSymbol packageSymbol, WixModuleSymbol moduleSymbol, WixPatchSymbol patchSymbol)
        {
            var codepage = packageSymbol?.Codepage ?? moduleSymbol?.Codepage ?? patchSymbol?.Codepage;

            if (String.IsNullOrEmpty(codepage))
            {
                codepage = this.ResolvedCodepage?.ToString() ?? "65001";

                if (packageSymbol != null)
                {
                    packageSymbol.Codepage = codepage;
                }
                else if (moduleSymbol != null)
                {
                    moduleSymbol.Codepage = codepage;
                }
                else if (patchSymbol != null)
                {
                    patchSymbol.Codepage = codepage;
                }
            }

            return this.WindowsInstallerBackendHelper.GetValidCodePage(codepage);
        }

        private T GetSingleSymbol<T>(IntermediateSection section) where T : IntermediateSymbol
        {
            var symbols = section.Symbols.OfType<T>().ToList();

            if (1 != symbols.Count)
            {
                throw new WixException($"Expected to find a single symbol of type {typeof(T).Name} but found {symbols.Count}");
            }

            return symbols[0];
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
                var trackPdb = this.WindowsInstallerBackendHelper.TrackFile(this.OutputPdbPath, TrackedFileType.BuiltPdbOutput);
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

        private IEnumerable<ITrackedFile> TrackInputFiles(WindowsInstallerData data, List<ITrackedFile> trackedFiles)
        {
            var trackedInputFiles = new List<ITrackedFile>();
            var intermediateAndTemporaryPaths = new HashSet<string>(trackedFiles.Where(t => t.Type == TrackedFileType.Intermediate || t.Type == TrackedFileType.Temporary).Select(t => t.Path), StringComparer.OrdinalIgnoreCase);

            foreach (var row in data.Tables.SelectMany(t => t.Rows))
            {
                foreach (var field in row.Fields.Where(f => f.Column.Type == ColumnType.Object))
                {
                    var path = field.AsString();

                    if (!intermediateAndTemporaryPaths.Contains(path))
                    {
                        trackedInputFiles.Add(this.WindowsInstallerBackendHelper.TrackFile(path, TrackedFileType.Input, row.SourceLineNumbers));
                    }
                }
            }

            return trackedInputFiles;
        }
    }
}
