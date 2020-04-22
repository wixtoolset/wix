// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Core.Burn.Bind;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Binds a this.bundle.
    /// </summary>
    internal class BindBundleCommand
    {
        public BindBundleCommand(IBindContext context, IEnumerable<IBurnBackendExtension> backedExtensions)
        {
            this.ServiceProvider = context.ServiceProvider;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();

            this.BackendHelper = context.ServiceProvider.GetService<IBackendHelper>();

            this.BurnStubPath = context.BurnStubPath;
            this.DefaultCompressionLevel = context.DefaultCompressionLevel;
            this.DelayedFields = context.DelayedFields;
            this.ExpectedEmbeddedFiles = context.ExpectedEmbeddedFiles;
            this.IntermediateFolder = context.IntermediateFolder;
            this.Output = context.IntermediateRepresentation;
            this.OutputPath = context.OutputPath;
            this.OutputPdbPath = context.PdbPath;
            //this.VariableResolver = context.VariableResolver;

            this.BackendExtensions = backedExtensions;
        }

        private IWixToolsetServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private string BurnStubPath { get; }

        private CompressionLevel? DefaultCompressionLevel { get; }

        public IEnumerable<IDelayedField> DelayedFields { get; }

        public IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; }

        private IEnumerable<IBurnBackendExtension> BackendExtensions { get; }

        private Intermediate Output {  get; }

        private string OutputPath { get; }

        private string OutputPdbPath { get; }

        private string IntermediateFolder { get; }

        private IVariableResolver VariableResolver { get; }

        public IEnumerable<IFileTransfer> FileTransfers { get; private set; }

        public IEnumerable<ITrackedFile> TrackedFiles { get; private set; }

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
            var chainPackageTuples = this.GetRequiredTuples<WixBundlePackageTuple>();

            var wixGroupTuples = this.GetRequiredTuples<WixGroupTuple>();

            // Ensure there is one and only one row in the WixBundle table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            var bundleTuple = this.GetSingleTuple<WixBundleTuple>();

            bundleTuple.BundleId = Guid.NewGuid().ToString("B").ToUpperInvariant();

            bundleTuple.Attributes |= WixBundleAttributes.PerMachine; // default to per-machine but the first-per user package wil flip the bundle per-user.

            // Ensure there is one and only one row in the WixBootstrapperApplication table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            var bundleApplicationTuple = this.GetSingleTuple<WixBootstrapperApplicationTuple>();

            // Ensure there is one and only one row in the WixChain table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            var chainTuple = this.GetSingleTuple<WixChainTuple>();

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // If there are any fields to resolve later, create the cache to populate during bind.
            var variableCache = this.DelayedFields.Any() ? new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) : null;

            var orderSearchesCommand = new OrderSearchesCommand(this.Messaging, section);
            orderSearchesCommand.Execute();
            var orderedSearches = orderSearchesCommand.OrderedSearchFacades;
            var extensionSearchTuplesById = orderSearchesCommand.ExtensionSearchTuplesByExtensionId;

            // Extract files that come from binary .wixlibs and WixExtensions (this does not extract files from merge modules).
            {
                var command = new ExtractEmbeddedFilesCommand(this.ExpectedEmbeddedFiles);
                command.Execute();
            }

            // Get the explicit payloads.
            var payloadTuples = section.Tuples.OfType<WixBundlePayloadTuple>().ToDictionary(t => t.Id.Id);

            // Update explicitly authored payloads with their parent package and container (as appropriate)
            // to make it easier to gather the payloads later.
            foreach (var groupTuple in wixGroupTuples)
            {
                if (ComplexReferenceChildType.Payload == groupTuple.ChildType)
                {
                    var payloadTuple = payloadTuples[groupTuple.ChildId];

                    if (ComplexReferenceParentType.Package == groupTuple.ParentType)
                    {
                        Debug.Assert(String.IsNullOrEmpty(payloadTuple.PackageRef));
                        payloadTuple.PackageRef = groupTuple.ParentId;
                    }
                    else if (ComplexReferenceParentType.Container == groupTuple.ParentType)
                    {
                        Debug.Assert(String.IsNullOrEmpty(payloadTuple.ContainerRef));
                        payloadTuple.ContainerRef = groupTuple.ParentId;
                    }
                    else if (ComplexReferenceParentType.Layout == groupTuple.ParentType)
                    {
                        payloadTuple.LayoutOnly = true;
                    }
                }
            }

            var layoutDirectory = Path.GetDirectoryName(this.OutputPath);

            // Process the explicitly authored payloads.
            ISet<string> processedPayloads;
            {
                var command = new ProcessPayloadsCommand(this.ServiceProvider, this.BackendHelper, payloadTuples.Values, bundleTuple.DefaultPackagingType, layoutDirectory);
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);

                processedPayloads = new HashSet<string>(payloadTuples.Keys);
            }

            IDictionary<string, PackageFacade> facades;
            {
                var command = new GetPackageFacadesCommand(chainPackageTuples, section);
                command.Execute();

                facades = command.PackageFacades;
            }

            // Process each package facade. Note this is likely to add payloads and other tuples so
            // note that any indexes created above may be out of date now.
            foreach (var facade in facades.Values)
            {
                switch (facade.PackageTuple.Type)
                {
                case WixBundlePackageType.Exe:
                {
                    var command = new ProcessExePackageCommand(facade, payloadTuples);
                    command.Execute();
                }
                break;

                case WixBundlePackageType.Msi:
                {
                    var command = new ProcessMsiPackageCommand(this.ServiceProvider, this.BackendExtensions, section, facade, payloadTuples);
                    command.Execute();

                    if (null != variableCache)
                    {
                        var msiPackage = (WixBundleMsiPackageTuple)facade.SpecificPackageTuple;
                        variableCache.Add(String.Concat("packageLanguage.", facade.PackageId), msiPackage.ProductLanguage.ToString());

                        if (null != msiPackage.Manufacturer)
                        {
                            variableCache.Add(String.Concat("packageManufacturer.", facade.PackageId), msiPackage.Manufacturer);
                        }
                    }
                }
                break;

                case WixBundlePackageType.Msp:
                {
                    var command = new ProcessMspPackageCommand(this.Messaging, section, facade, payloadTuples);
                    command.Execute();
                }
                break;

                case WixBundlePackageType.Msu:
                {
                    var command = new ProcessMsuPackageCommand(facade, payloadTuples);
                    command.Execute();
                }
                break;
                }

                if (null != variableCache)
                {
                    BindBundleCommand.PopulatePackageVariableCache(facade.PackageTuple, variableCache);
                }
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Reindex the payloads now that all the payloads (minus the manifest payloads that will be created later)
            // are present.
            payloadTuples = section.Tuples.OfType<WixBundlePayloadTuple>().ToDictionary(t => t.Id.Id);

            // Process the payloads that were added by processing the packages.
            {
                var toProcess = payloadTuples.Values.Where(r => !processedPayloads.Contains(r.Id.Id)).ToList();

                var command = new ProcessPayloadsCommand(this.ServiceProvider, this.BackendHelper, toProcess, bundleTuple.DefaultPackagingType, layoutDirectory);
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);

                processedPayloads = null;
            }

            // Set the package metadata from the payloads now that we have the complete payload information.
            {
                var payloadsByPackageId = payloadTuples.Values.ToLookup(p => p.PackageRef);

                foreach (var facade in facades.Values)
                {
                    facade.PackageTuple.Size = 0;

                    var packagePayloads = payloadsByPackageId[facade.PackageId];

                    foreach (var payload in packagePayloads)
                    {
                        facade.PackageTuple.Size += payload.FileSize.Value;
                    }

                    if (!facade.PackageTuple.InstallSize.HasValue)
                    {
                        facade.PackageTuple.InstallSize = facade.PackageTuple.Size;
                    }

                    var packagePayload = payloadTuples[facade.PackageTuple.PayloadRef];

                    if (String.IsNullOrEmpty(facade.PackageTuple.Description))
                    {
                        facade.PackageTuple.Description = packagePayload.Description;
                    }

                    if (String.IsNullOrEmpty(facade.PackageTuple.DisplayName))
                    {
                        facade.PackageTuple.DisplayName = packagePayload.DisplayName;
                    }
                }
            }

            // Give the UX payloads their embedded IDs...
            var uxPayloadIndex = 0;
            {
                foreach (var payload in payloadTuples.Values.Where(p => BurnConstants.BurnUXContainerName == p.ContainerRef))
                {
                    // In theory, UX payloads could be embedded in the UX CAB, external to the bundle EXE, or even
                    // downloaded. The current engine requires the UX to be fully present before any downloading starts,
                    // so that rules out downloading. Also, the burn engine does not currently copy external UX payloads
                    // into the temporary UX directory correctly, so we don't allow external either.
                    if (PackagingType.Embedded != payload.Packaging)
                    {
                        this.Messaging.Write(WarningMessages.UxPayloadsOnlySupportEmbedding(payload.SourceLineNumbers, payload.SourceFile.Path));
                        payload.Packaging = PackagingType.Embedded;
                    }

                    payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, uxPayloadIndex);
                    ++uxPayloadIndex;
                }

                if (0 == uxPayloadIndex)
                {
                    // If we didn't get any UX payloads, it's an error!
                    throw new WixException(ErrorMessages.MissingBundleInformation("BootstrapperApplication"));
                }

                // Give the embedded payloads without an embedded id yet an embedded id.
                var payloadIndex = 0;
                foreach (var payload in payloadTuples.Values)
                {
                    Debug.Assert(PackagingType.Unknown != payload.Packaging);

                    if (PackagingType.Embedded == payload.Packaging && String.IsNullOrEmpty(payload.EmbeddedId))
                    {
                        payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnAttachedContainerEmbeddedIdFormat, payloadIndex);
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
                var command = new AutomaticallySlipstreamPatchesCommand(section, facades.Values);
                command.Execute();
            }

            // If catalog files exist, non-embedded payloads should validate with the catalogs.
            var catalogs = section.Tuples.OfType<WixBundleCatalogTuple>().ToList();

            if (catalogs.Count > 0)
            {
                var command = new VerifyPayloadsWithCatalogCommand(this.Messaging, catalogs, payloadTuples.Values);
                command.Execute();
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            IEnumerable<PackageFacade> orderedFacades;
            IEnumerable<WixBundleRollbackBoundaryTuple> boundaries;
            {
                var groupTuples = section.Tuples.OfType<WixGroupTuple>();
                var boundaryTuplesById = section.Tuples.OfType<WixBundleRollbackBoundaryTuple>().ToDictionary(b => b.Id.Id);

                var command = new OrderPackagesAndRollbackBoundariesCommand(this.Messaging, groupTuples, boundaryTuplesById, facades);
                command.Execute();

                orderedFacades = command.OrderedPackageFacades;
                boundaries = command.UsedRollbackBoundaries;
            }

            // Resolve any delayed fields before generating the manifest.
            if (this.DelayedFields.Any())
            {
                var resolveDelayedFieldsCommand = new ResolveDelayedFieldsCommand(this.Messaging, this.DelayedFields, variableCache);
                resolveDelayedFieldsCommand.Execute();
            }

            Dictionary<string, ProvidesDependencyTuple> dependencyTuplesByKey;
            {
                var command = new ProcessDependencyProvidersCommand(this.Messaging, section, facades);
                command.Execute();

                bundleTuple.ProviderKey = command.BundleProviderKey; // set the overridable bundle provider key.
                dependencyTuplesByKey = command.DependencyTuplesByKey;
            }

            // Update the bundle per-machine/per-user scope based on the chained packages.
            this.ResolveBundleInstallScope(section, bundleTuple, orderedFacades);

            // Generate the core-defined BA manifest tables...
            string baManifestPath;
            {
                var command = new CreateBootstrapperApplicationManifestCommand(section, bundleTuple, orderedFacades, uxPayloadIndex, payloadTuples, this.IntermediateFolder);
                command.Execute();

                var baManifestPayload = command.BootstrapperApplicationManifestPayloadRow;
                baManifestPath = command.OutputPath;
                payloadTuples.Add(baManifestPayload.Id.Id, baManifestPayload);
                ++uxPayloadIndex;
            }

            // Generate the bundle extension manifest...
            string bextManifestPath;
            {
                var command = new CreateBundleExtensionManifestCommand(section, bundleTuple, extensionSearchTuplesById, uxPayloadIndex, this.IntermediateFolder);
                command.Execute();

                var bextManifestPayload = command.BundleExtensionManifestPayloadRow;
                bextManifestPath = command.OutputPath;
                payloadTuples.Add(bextManifestPayload.Id.Id, bextManifestPayload);
                ++uxPayloadIndex;
            }

            foreach (var extension in this.BackendExtensions)
            {
                extension.BundleFinalize();
            }

            if (this.Messaging.EncounteredError)
            {
                return;
            }

            // Create all the containers except the UX container first so the manifest (that goes in the UX container)
            // can contain all size and hash information about the non-UX containers.
            WixBundleContainerTuple uxContainer;
            IEnumerable<WixBundlePayloadTuple> uxPayloads;
            IEnumerable<WixBundleContainerTuple> containers;
            {
                var command = new CreateNonUXContainers(this.BackendHelper, section, bundleApplicationTuple, payloadTuples, this.IntermediateFolder, layoutDirectory, this.DefaultCompressionLevel);
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);

                uxContainer = command.UXContainer;
                uxPayloads = command.UXContainerPayloads;
                containers = command.Containers;
            }
            
            // Create the bundle manifest.
            string manifestPath;
            {
                var executableName = Path.GetFileName(this.OutputPath);

                var command = new CreateBurnManifestCommand(this.Messaging, this.BackendExtensions, executableName, section, bundleTuple, containers, chainTuple, orderedFacades, boundaries, uxPayloads, payloadTuples, orderedSearches, catalogs, this.IntermediateFolder);
                command.Execute();

                manifestPath = command.OutputPath;
            }

            // Create the UX container.
            {
                var command = new CreateContainerCommand(manifestPath, uxPayloads, uxContainer.WorkingPath, this.DefaultCompressionLevel);
                command.Execute();

                uxContainer.Hash = command.Hash;
                uxContainer.Size = command.Size;
            }

            {
                var command = new CreateBundleExeCommand(this.Messaging, this.BackendHelper, this.IntermediateFolder, this.OutputPath, bundleTuple, uxContainer, containers, this.BurnStubPath);
                command.Execute();

                fileTransfers.Add(command.Transfer);
            }

#if TODO // does this need to come back, or do they only need to be in TrackedFiles?
            this.ContentFilePaths = payloadTuples.Values.Where(p => p.ContentFile).Select(p => p.FullFileName).ToList();
#endif
            this.FileTransfers = fileTransfers;
            this.TrackedFiles = trackedFiles;
            this.Wixout = this.CreateWixout(trackedFiles, this.Output, manifestPath, baManifestPath, bextManifestPath);
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
                var trackPdb = this.BackendHelper.TrackFile(this.OutputPdbPath, TrackedFileType.Final);
                trackedFiles.Add(trackPdb);

                wixout = WixOutput.Create(trackPdb.Path);
            }

            intermediate.Save(wixout);

            wixout.ImportDataStream(BurnConstants.BurnManifestWixOutputStreamName, manifestPath);
            wixout.ImportDataStream(BurnConstants.BootstrapperApplicationDataWixOutputStreamName, baDataPath);
            wixout.ImportDataStream(BurnConstants.BundleExtensionDataWixOutputStreamName, bextDataPath);

            wixout.Reopen();

            return wixout;
        }

        /// <summary>
        /// Populates the variable cache with specific package properties.
        /// </summary>
        /// <param name="package">The package with properties to cache.</param>
        /// <param name="variableCache">The property cache.</param>
        private static void PopulatePackageVariableCache(WixBundlePackageTuple package, IDictionary<string, string> variableCache)
        {
            var id = package.Id.Id;

            variableCache.Add(String.Concat("packageDescription.", id), package.Description);
            //variableCache.Add(String.Concat("packageLanguage.", id), package.Language);
            //variableCache.Add(String.Concat("packageManufacturer.", id), package.Manufacturer);
            variableCache.Add(String.Concat("packageName.", id), package.DisplayName);
            variableCache.Add(String.Concat("packageVersion.", id), package.Version);
        }

        private void ResolveBundleInstallScope(IntermediateSection section, WixBundleTuple bundleTuple, IEnumerable<PackageFacade> facades)
        {
            var dependencyTuplesById = section.Tuples.OfType<ProvidesDependencyTuple>().ToDictionary(t => t.Id.Id);

            foreach (var facade in facades)
            {
                if (bundleTuple.PerMachine && YesNoDefaultType.No == facade.PackageTuple.PerMachine)
                {
                    this.Messaging.Write(VerboseMessages.SwitchingToPerUserPackage(facade.PackageTuple.SourceLineNumbers, facade.PackageId));

                    bundleTuple.Attributes &= ~WixBundleAttributes.PerMachine;
                    break;
                }
            }

            foreach (var facade in facades)
            {
                // Update package scope from bundle scope if default.
                if (YesNoDefaultType.Default == facade.PackageTuple.PerMachine)
                {
                    facade.PackageTuple.PerMachine = bundleTuple.PerMachine ? YesNoDefaultType.Yes : YesNoDefaultType.No;
                }

                // We will only register packages in the same scope as the bundle. Warn if any packages with providers
                // are in a different scope and not permanent (permanents typically don't need a ref-count).
                if (!bundleTuple.PerMachine &&
                    YesNoDefaultType.Yes == facade.PackageTuple.PerMachine &&
                    !facade.PackageTuple.Permanent &&
                    dependencyTuplesById.ContainsKey(facade.PackageId))
                {
                    this.Messaging.Write(WarningMessages.NoPerMachineDependencies(facade.PackageTuple.SourceLineNumbers, facade.PackageId));
                }
            }
        }

        private IEnumerable<T> GetRequiredTuples<T>() where T : IntermediateTuple
        {
            var tuples = this.Output.Sections.Single().Tuples.OfType<T>().ToList();

            if (0 == tuples.Count)
            {
                throw new WixException(ErrorMessages.MissingBundleInformation(nameof(T)));
            }

            return tuples;
        }

        private T GetSingleTuple<T>() where T : IntermediateTuple
        {
            var tuples = this.Output.Sections.Single().Tuples.OfType<T>().ToList();

            if (1 != tuples.Count)
            {
                throw new WixException(ErrorMessages.MissingBundleInformation(nameof(T)));
            }

            return tuples[0];
        }
    }
}
