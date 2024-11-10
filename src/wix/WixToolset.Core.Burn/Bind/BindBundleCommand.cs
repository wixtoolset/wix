// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Burn.Bind;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Core.Burn.Interfaces;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Versioning;

    /// <summary>
    /// Binds a this.bundle.
    /// </summary>
    internal class BindBundleCommand
    {
        public BindBundleCommand(IBindContext context, IEnumerable<IBurnBackendBinderExtension> backedExtensions)
        {
            this.ServiceProvider = context.ServiceProvider;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
            this.FileSystem = context.ServiceProvider.GetService<IFileSystem>();

            this.BackendHelper = context.ServiceProvider.GetService<IBackendHelper>();
            this.InternalBurnBackendHelper = context.ServiceProvider.GetService<IInternalBurnBackendHelper>();
            this.PayloadHarvester = context.ServiceProvider.GetService<IPayloadHarvester>();

            this.DefaultCompressionLevel = context.DefaultCompressionLevel;
            this.DelayedFields = context.DelayedFields;
            this.ExpectedEmbeddedFiles = context.ExpectedEmbeddedFiles;
            this.IntermediateFolder = context.IntermediateFolder;
            this.Output = context.IntermediateRepresentation;
            this.OutputPath = context.OutputPath;
            this.OutputPdbPath = context.PdbPath;

            this.BackendExtensions = backedExtensions;
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IFileSystem FileSystem { get; }

        private IBackendHelper BackendHelper { get; }

        private IInternalBurnBackendHelper InternalBurnBackendHelper { get; }

        private IPayloadHarvester PayloadHarvester { get; }

        private CompressionLevel? DefaultCompressionLevel { get; }

        public IEnumerable<IDelayedField> DelayedFields { get; }

        public IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; }

        private IEnumerable<IBurnBackendBinderExtension> BackendExtensions { get; }

        private Intermediate Output { get; }

        private string OutputPath { get; }

        private string OutputPdbPath { get; }

        private string IntermediateFolder { get; }

        public IReadOnlyCollection<IFileTransfer> FileTransfers { get; private set; }

        public IReadOnlyCollection<ITrackedFile> TrackedFiles { get; private set; }

        public WixOutput Wixout { get; private set; }

        public void Execute()
        {
            var section = this.Output.Sections.Single();

            var fileTransfers = new List<IFileTransfer>();
            var trackedFiles = new List<ITrackedFile>();

            // First look for data we expect to find... Chain, WixGroups, etc.

            // We shouldn't really get past the linker phase if there are
            // no group items... that means that there's no UX, no Chain,
            // *and* no Containers!
            var chainPackageSymbols = this.GetRequiredSymbols<WixBundlePackageSymbol>();

            var wixGroupSymbols = this.GetRequiredSymbols<WixGroupSymbol>();

            // Ensure there is one and only one WixBundleSymbol.
            var bundleSymbol = this.GetSingleSymbol<WixBundleSymbol>("bundle");

            bundleSymbol.ProviderKey = bundleSymbol.BundleId = Guid.NewGuid().ToString("B").ToUpperInvariant();

            bundleSymbol.PerMachine = true; // default to per-machine but the first-per user package wil flip the bundle per-user.

            {
                var command = new NormalizeRelatedBundlesCommand(this.Messaging, bundleSymbol, section);
                command.Execute();
            }

            // Find the primary bootstrapper application and optional secondary.
            WixBootstrapperApplicationSymbol primaryBootstrapperApplicationSymbol = null;
            WixBootstrapperApplicationSymbol secondaryBootstrapperApplicationSymbol = null;
            {
                var command = new GetBootstrapperApplicationSymbolsCommand(this.Messaging, section);
                command.Execute();

                primaryBootstrapperApplicationSymbol = command.Primary;
                secondaryBootstrapperApplicationSymbol = command.Secondary;
            }

            // Ensure there is one and only one WixChainSymbol.
            var chainSymbol = this.GetSingleSymbol<WixChainSymbol>("package chain");

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // If there are any fields to resolve later, create the cache to populate during bind.
            var variableCache = this.DelayedFields.Any() ? new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) : null;

            IEnumerable<ISearchFacade> orderedSearches;
            IDictionary<string, IEnumerable<IntermediateSymbol>> extensionSearchSymbolsById;
            {
                var orderSearchesCommand = new OrderSearchesCommand(this.Messaging, section);
                orderSearchesCommand.Execute();

                orderedSearches = orderSearchesCommand.OrderedSearchFacades;
                extensionSearchSymbolsById = orderSearchesCommand.ExtensionSearchSymbolsByExtensionId;
            }

            // Extract files that come from binary .wixlibs and WixExtensions (this does not extract files from merge modules).
            {
                var extractedFiles = this.BackendHelper.ExtractEmbeddedFiles(this.ExpectedEmbeddedFiles);

                trackedFiles.AddRange(extractedFiles);
            }

            // Get the explicit payloads.
            var payloadSymbols = section.Symbols.OfType<WixBundlePayloadSymbol>().ToDictionary(t => t.Id.Id);
            var packagesPayloads = RecalculatePackagesPayloads(payloadSymbols, wixGroupSymbols);

            var layoutDirectory = Path.GetDirectoryName(this.OutputPath);

            // Process the explicitly authored payloads.
            ISet<string> processedPayloads;
            {
                var command = new ProcessPayloadsCommand(this.InternalBurnBackendHelper, this.PayloadHarvester, payloadSymbols.Values, bundleSymbol.DefaultPackagingType, layoutDirectory);
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
                trackedFiles.AddRange(command.TrackedFiles);

                processedPayloads = new HashSet<string>(payloadSymbols.Keys);
            }

            PackageFacades facades;
            {
                var command = new GetPackageFacadesCommand(this.Messaging, chainPackageSymbols, section);
                command.Execute();

                facades = command.PackageFacades;
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Process each package facade. Note this is likely to add payloads and other symbols so
            // note that any indexes created above may be out of date now.
            foreach (var facade in facades.Values)
            {
                switch (facade.PackageSymbol.Type)
                {
                    case WixBundlePackageType.Bundle:
                    {
                        var command = new ProcessBundlePackageCommand(this.ServiceProvider, this.BackendExtensions, section, facade, packagesPayloads[facade.PackageId], this.IntermediateFolder);
                        command.Execute();

                        trackedFiles.AddRange(command.TrackedFiles);
                    }
                    break;

                    case WixBundlePackageType.Exe:
                    {
                        var command = new ProcessExePackageCommand(this.Messaging, facade, payloadSymbols);
                        command.Execute();
                    }
                    break;

                    case WixBundlePackageType.Msi:
                    {
                        var command = new ProcessMsiPackageCommand(this.ServiceProvider, this.BackendExtensions, section, facade, packagesPayloads[facade.PackageId]);
                        command.Execute();
                    }
                    break;

                    case WixBundlePackageType.Msp:
                    {
                        var command = new ProcessMspPackageCommand(this.Messaging, section, facade, payloadSymbols);
                        command.Execute();
                    }
                    break;

                    case WixBundlePackageType.Msu:
                    {
                        var command = new ProcessMsuPackageCommand(this.Messaging, facade, payloadSymbols);
                        command.Execute();
                    }
                    break;
                }

                if (null != variableCache)
                {
                    BindBundleCommand.PopulatePackageVariableCache(facade, variableCache);
                }
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Resolve any delayed fields now that the variable cache is populated with package information.
            if (this.DelayedFields.Any())
            {
                this.BackendHelper.ResolveDelayedFields(this.DelayedFields, variableCache);
            }

            // Now that delayed variables are resolved the bundle version must be valid so ensure
            // it is correct.
            this.ProcessBundleVersion(bundleSymbol);

            // Reindex the payloads now that all the payloads (minus the manifest payloads that will be created later)
            // are present.
            payloadSymbols = section.Symbols.OfType<WixBundlePayloadSymbol>().ToDictionary(t => t.Id.Id);
            wixGroupSymbols = this.GetRequiredSymbols<WixGroupSymbol>();
            packagesPayloads = RecalculatePackagesPayloads(payloadSymbols, wixGroupSymbols);

            // Process the payloads that were added by processing the packages.
            {
                var toProcess = payloadSymbols.Values.Where(r => !processedPayloads.Contains(r.Id.Id)).ToList();

                var command = new ProcessPayloadsCommand(this.InternalBurnBackendHelper, this.PayloadHarvester, toProcess, bundleSymbol.DefaultPackagingType, layoutDirectory);
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
                trackedFiles.AddRange(command.TrackedFiles);

                processedPayloads = null;
            }

            // Set the package metadata from the payloads now that we have the complete payload information.
            {
                foreach (var facade in facades.Values)
                {
                    // Use temporary variable to avoid excessive number of PreviousValues.
                    long packageSize = 0;

                    var packagePayloads = packagesPayloads[facade.PackageId];

                    foreach (var payload in packagePayloads.Values)
                    {
                        packageSize += payload.FileSize.Value;
                    }

                    facade.PackageSymbol.Size = packageSize;

                    if (!facade.PackageSymbol.InstallSize.HasValue)
                    {
                        facade.PackageSymbol.InstallSize = facade.PackageSymbol.Size;
                    }

                    var packagePayload = payloadSymbols[facade.PackageSymbol.PayloadRef];

                    if (String.IsNullOrEmpty(facade.PackageSymbol.Description))
                    {
                        facade.PackageSymbol.Description = packagePayload.Description;
                    }

                    if (String.IsNullOrEmpty(facade.PackageSymbol.DisplayName))
                    {
                        facade.PackageSymbol.DisplayName = packagePayload.DisplayName;
                    }
                }
            }

            // Give the UX payloads their embedded IDs...
            var uxPayloadIndex = 0;
            {
                foreach (var payload in payloadSymbols.Values.Where(p => BurnConstants.BurnUXContainerName == p.ContainerRef))
                {
                    payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, uxPayloadIndex);
                    ++uxPayloadIndex;
                }

                if (0 == uxPayloadIndex)
                {
                    // If we didn't get any UX payloads, it's an error!
                    throw new WixException(ErrorMessages.MissingBundleInformation("bootstrapper application"));
                }

                // Give the embedded payloads without an embedded id yet an embedded id.
                var payloadIndex = 0;
                foreach (var payload in payloadSymbols.Values)
                {
                    Debug.Assert(PackagingType.Unknown != payload.Packaging);

                    if (PackagingType.Embedded == payload.Packaging && String.IsNullOrEmpty(payload.EmbeddedId))
                    {
                        payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnAuthoredContainerEmbeddedIdFormat, payloadIndex);
                        ++payloadIndex;
                    }
                }
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Determine patches to automatically slipstream.
            {
                var command = new AutomaticallySlipstreamPatchesCommand(this.Messaging, section, facades);
                command.Execute();
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            IEnumerable<WixBundleRollbackBoundarySymbol> boundaries;
            {
                var command = new OrderPackagesAndRollbackBoundariesCommand(this.Messaging, section, facades);
                command.Execute();

                boundaries = command.UsedRollbackBoundaries;
            }

            {
                var command = new ProcessDependencyProvidersCommand(this.ServiceProvider, section, facades);
                command.Execute();

                if (!String.IsNullOrEmpty(command.BundleProviderKey))
                {
                    bundleSymbol.ProviderKey = command.BundleProviderKey; // set the overridable bundle provider key.
                }
            }

            // Update the bundle per-machine/per-user scope based on the chained packages.
            this.ResolveBundleInstallScope(section, bundleSymbol, facades.OrderedValues);

            var softwareTags = section.Symbols.OfType<WixBundleTagSymbol>().ToList();
            if (softwareTags.Any())
            {
                var command = new ProcessBundleSoftwareTagsCommand(section, softwareTags);
                command.Execute();
            }

            this.DetectDuplicateCacheIds(facades.Values);

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Give the extension one last hook before generating the output files.
            foreach (var extension in this.BackendExtensions)
            {
                extension.SymbolsFinalized(section);
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Now that extensions can't change anything else, verify everything is still valid.
            {
                var command = new PerformBundleBackendValidationCommand(this.Messaging, this.InternalBurnBackendHelper, section, facades);
                command.Execute();
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Generate data for all manifests.
            {
                var command = new GenerateManifestDataFromIRCommand(this.Messaging, section, this.BackendExtensions, this.InternalBurnBackendHelper, extensionSearchSymbolsById);
                command.Execute();
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Generate the core-defined BA manifest tables...
            string baManifestPath;
            {
                var command = new CreateBootstrapperApplicationManifestCommand(section, bundleSymbol, boundaries, facades, uxPayloadIndex, payloadSymbols, packagesPayloads, this.IntermediateFolder, this.InternalBurnBackendHelper);
                command.Execute();

                var baManifestPayload = command.BootstrapperApplicationManifestPayloadRow;
                baManifestPath = command.OutputPath;
                payloadSymbols.Add(baManifestPayload.Id.Id, baManifestPayload);
                ++uxPayloadIndex;

                trackedFiles.Add(this.BackendHelper.TrackFile(baManifestPath, TrackedFileType.Temporary));
            }

            // Generate the bundle extension manifest...
            string bextManifestPath;
            {
                var command = new CreateBootstrapperExtensionManifestCommand(section, bundleSymbol, uxPayloadIndex, this.IntermediateFolder, this.InternalBurnBackendHelper);
                command.Execute();

                var bextManifestPayload = command.BootstrapperExtensionManifestPayloadRow;
                bextManifestPath = command.OutputPath;
                payloadSymbols.Add(bextManifestPayload.Id.Id, bextManifestPayload);
                ++uxPayloadIndex;

                trackedFiles.Add(this.BackendHelper.TrackFile(bextManifestPath, TrackedFileType.Temporary));
            }

            var containers = section.Symbols.OfType<WixBundleContainerSymbol>().ToDictionary(t => t.Id.Id);
            {
                var command = new DetectPayloadCollisionsCommand(this.Messaging, containers, facades.Values, payloadSymbols, packagesPayloads);
                command.Execute();
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Create all the containers except the UX container first so the manifest (that goes in the UX container)
            // can contain all size and hash information about the non-UX containers.
            WixBundleContainerSymbol uxContainer;
            IEnumerable<WixBundlePayloadSymbol> uxPayloads;
            {
                var command = new CreateNonUXContainers(this.BackendHelper, this.Messaging, containers.Values, payloadSymbols, this.IntermediateFolder, layoutDirectory, this.DefaultCompressionLevel);
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
                trackedFiles.AddRange(command.TrackedFiles);

                uxContainer = command.UXContainer;
                uxPayloads = command.UXContainerPayloads;
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Resolve the download URLs now that we have all of the containers and payloads calculated.
            {
                var command = new ResolveDownloadUrlsCommand(this.Messaging, this.BackendExtensions, containers.Values, payloadSymbols);
                command.Execute();
            }

            // Create the bundle manifest.
            string manifestPath;
            {
                var executableName = Path.GetFileName(this.OutputPath);

                var command = new CreateBurnManifestCommand(executableName, section, bundleSymbol, primaryBootstrapperApplicationSymbol, secondaryBootstrapperApplicationSymbol, containers.Values, chainSymbol, facades, boundaries, uxPayloads, payloadSymbols, packagesPayloads, orderedSearches, this.IntermediateFolder);
                command.Execute();

                manifestPath = command.OutputPath;
                trackedFiles.Add(this.BackendHelper.TrackFile(manifestPath, TrackedFileType.Temporary));
            }

            // Create the UX container.
            {
                var command = new CreateContainerCommand(manifestPath, uxPayloads, uxContainer.WorkingPath, this.DefaultCompressionLevel);
                command.Execute();

                uxContainer.Hash = command.Hash;
                uxContainer.Size = command.Size;

                trackedFiles.Add(this.BackendHelper.TrackFile(uxContainer.WorkingPath, TrackedFileType.Temporary, uxContainer.SourceLineNumbers));
            }

            {
                var command = new CreateBundleExeCommand(this.Messaging, this.FileSystem, this.BackendHelper, this.IntermediateFolder, this.OutputPath, bundleSymbol, uxContainer, containers.Values);
                command.Execute();

                fileTransfers.Add(command.Transfer);
                trackedFiles.Add(this.BackendHelper.TrackFile(this.OutputPath, TrackedFileType.BuiltTargetOutput));
            }

#if TODO // does this need to come back, or do they only need to be in TrackedFiles?
            this.ContentFilePaths = payloadSymbols.Values.Where(p => p.ContentFile).Select(p => p.FullFileName).ToList();
#endif
            this.FileTransfers = fileTransfers;
            this.TrackedFiles = trackedFiles;
            this.Wixout = this.CreateWixout(trackedFiles, this.Output, manifestPath, baManifestPath, bextManifestPath);
        }

        private void ProcessBundleVersion(WixBundleSymbol bundleSymbol)
        {
            if (WixVersion.TryParse(bundleSymbol.Version, out var wixVersion))
            {
                // Trim the prefix from the version if it is there.
                if (wixVersion.Prefix.HasValue)
                {
                    bundleSymbol.Version = bundleSymbol.Version.Substring(1);
                }
            }
            else
            {
                this.Messaging.Write(ErrorMessages.IllegalVersionValue(bundleSymbol.SourceLineNumbers, "Bundle", "Version", bundleSymbol.Version));
            }
        }

        private WixOutput CreateWixout(List<ITrackedFile> trackedFiles, Intermediate intermediate, string manifestPath, string baDataPath, string bextDataPath)
        {
            WixOutput wixout;

            if (String.IsNullOrEmpty(this.OutputPdbPath))
            {
                wixout = WixOutput.Create();
            }
            else
            {
                var trackPdb = this.BackendHelper.TrackFile(this.OutputPdbPath, TrackedFileType.BuiltPdbOutput);
                trackedFiles.Add(trackPdb);

                wixout = WixOutput.Create(trackPdb.Path);
            }

            intermediate.Save(wixout);

            wixout.ImportDataStream(BurnConstants.BurnManifestWixOutputStreamName, manifestPath);
            wixout.ImportDataStream(BurnConstants.BootstrapperApplicationDataWixOutputStreamName, baDataPath);
            wixout.ImportDataStream(BurnConstants.BootstrapperExtensionDataWixOutputStreamName, bextDataPath);

            wixout.Reopen();

            return wixout;
        }

        /// <summary>
        /// Populates the variable cache with specific package properties.
        /// </summary>
        /// <param name="facade">The package facade with properties to cache.</param>
        /// <param name="variableCache">The property cache.</param>
        private static void PopulatePackageVariableCache(PackageFacade facade, IDictionary<string, string> variableCache)
        {
            var package = facade.PackageSymbol;
            var id = package.Id.Id;

            variableCache.Add(String.Concat("packageDescription.", id), package.Description ?? String.Empty);
            variableCache.Add(String.Concat("packageName.", id), package.DisplayName ?? String.Empty);
            variableCache.Add(String.Concat("packageVersion.", id), package.Version);

            if (facade.SpecificPackageSymbol is WixBundleMsiPackageSymbol msiPackage)
            {
                variableCache.Add(String.Concat("packageLanguage.", id), msiPackage.ProductLanguage.ToString());
                variableCache.Add(String.Concat("packageManufacturer.", id), msiPackage.Manufacturer ?? String.Empty);
            }
            else
            {
                variableCache.Add(String.Concat("packageLanguage.", id), String.Empty);
                variableCache.Add(String.Concat("packageManufacturer.", id), String.Empty);
            }
        }

        private void ResolveBundleInstallScope(IntermediateSection section, WixBundleSymbol bundleSymbol, IEnumerable<PackageFacade> facades)
        {
            var dependencySymbolsById = section.Symbols.OfType<WixDependencyProviderSymbol>().ToDictionary(t => t.Id.Id);

            foreach (var facade in facades)
            {
                if (bundleSymbol.PerMachine && facade.PackageSymbol.PerMachine.HasValue && !facade.PackageSymbol.PerMachine.Value)
                {
                    this.Messaging.Write(VerboseMessages.SwitchingToPerUserPackage(facade.PackageSymbol.SourceLineNumbers, facade.PackageId));

                    bundleSymbol.PerMachine = false;
                    break;
                }
            }

            foreach (var facade in facades)
            {
                // Update package scope from bundle scope if default.
                if (!facade.PackageSymbol.PerMachine.HasValue)
                {
                    facade.PackageSymbol.PerMachine = bundleSymbol.PerMachine;
                }

                // We will only register packages in the same scope as the bundle. Warn if any packages with providers
                // are in a different scope and not permanent (permanents typically don't need a ref-count).
                if (!bundleSymbol.PerMachine &&
                    facade.PackageSymbol.PerMachine.Value &&
                    !facade.PackageSymbol.Permanent &&
                    dependencySymbolsById.ContainsKey(facade.PackageId))
                {
                    this.Messaging.Write(WarningMessages.NoPerMachineDependencies(facade.PackageSymbol.SourceLineNumbers, facade.PackageId));
                }
            }
        }

        private void DetectDuplicateCacheIds(IEnumerable<PackageFacade> facades)
        {
            var duplicateCacheIdDetector = new Dictionary<string, WixBundlePackageSymbol>();

            foreach (var facade in facades)
            {
                if (duplicateCacheIdDetector.TryGetValue(facade.PackageSymbol.CacheId, out var collisionPackage))
                {
                    this.Messaging.Write(BurnBackendErrors.DuplicateCacheIds(facade.PackageSymbol.SourceLineNumbers, facade.PackageSymbol.CacheId, facade.PackageId));
                    this.Messaging.Write(BurnBackendErrors.DuplicateCacheIds2(collisionPackage.SourceLineNumbers));
                }
                else
                {
                    duplicateCacheIdDetector.Add(facade.PackageSymbol.CacheId, facade.PackageSymbol);
                }
            }
        }

        private IEnumerable<T> GetRequiredSymbols<T>() where T : IntermediateSymbol
        {
            var symbols = this.Output.Sections.Single().Symbols.OfType<T>().ToList();

            if (0 == symbols.Count)
            {
                throw new WixException(ErrorMessages.MissingBundleInformation(typeof(T).Name));
            }

            return symbols;
        }

        private T GetSingleSymbol<T>(string elementName) where T : IntermediateSymbol
        {
            var symbols = this.Output.Sections.Single().Symbols.OfType<T>().ToList();

            if (0 == symbols.Count)
            {
                throw new WixException(ErrorMessages.MissingBundleInformation(elementName));
            }
            else if (1 < symbols.Count)
            {
                // We'll show the first two source line collisions. If there are more than that, the user
                // may have to build multiple times to find them all. This should be very rare.
                throw new WixException(BurnBackendErrors.MultipleSingletonSymbolsFound(symbols[0].SourceLineNumbers, elementName, symbols[1].SourceLineNumbers));
            }

            return symbols[0];
        }

        private static Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>> RecalculatePackagesPayloads(Dictionary<string, WixBundlePayloadSymbol> payloadSymbols, IEnumerable<WixGroupSymbol> wixGroupSymbols)
        {
            var packagesPayloads = new Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>>();

            foreach (var groupSymbol in wixGroupSymbols)
            {
                if (ComplexReferenceChildType.Payload == groupSymbol.ChildType)
                {
                    var payloadSymbol = payloadSymbols[groupSymbol.ChildId];

                    if (ComplexReferenceParentType.Package == groupSymbol.ParentType)
                    {
                        if (!packagesPayloads.TryGetValue(groupSymbol.ParentId, out var packagePayloadsById))
                        {
                            packagePayloadsById = new Dictionary<string, WixBundlePayloadSymbol>();
                            packagesPayloads.Add(groupSymbol.ParentId, packagePayloadsById);
                        }

                        packagePayloadsById.Add(payloadSymbol.Id.Id, payloadSymbol);
                    }
                }
            }

            return packagesPayloads;
        }
    }
}
