// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Bind.Bundles;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    /// <summary>
    /// Binds a this.bundle.
    /// </summary>
    internal class BindBundleCommand : ICommand
    {
        public CompressionLevel DefaultCompressionLevel { private get; set; }

        public IEnumerable<IBinderExtension> Extensions { private get; set; }

        public BinderFileManagerCore FileManagerCore { private get; set; }

        public IEnumerable<IBinderFileManager> FileManagers { private get; set; }

        public Output Output { private get; set; }

        public string OutputPath { private get; set; }

        public string PdbFile { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public string TempFilesLocation { private get; set; }

        public WixVariableResolver WixVariableResolver { private get; set; }

        public IEnumerable<FileTransfer> FileTransfers { get; private set; }

        public IEnumerable<string> ContentFilePaths { get; private set; }

        public void Execute()
        {
            this.FileTransfers = Enumerable.Empty<FileTransfer>();
            this.ContentFilePaths = Enumerable.Empty<string>();

            // First look for data we expect to find... Chain, WixGroups, etc.

            // We shouldn't really get past the linker phase if there are
            // no group items... that means that there's no UX, no Chain,
            // *and* no Containers!
            Table chainPackageTable = this.GetRequiredTable("WixBundlePackage");

            Table wixGroupTable = this.GetRequiredTable("WixGroup");

            // Ensure there is one and only one row in the WixBundle table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            WixBundleRow bundleRow = (WixBundleRow)this.GetSingleRowTable("WixBundle");

            bundleRow.PerMachine = true; // default to per-machine but the first-per user package wil flip the bundle per-user.

            // Ensure there is one and only one row in the WixBootstrapperApplication table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            Row baRow = this.GetSingleRowTable("WixBootstrapperApplication");

            // Ensure there is one and only one row in the WixChain table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            WixChainRow chainRow = (WixChainRow)this.GetSingleRowTable("WixChain");

            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

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

            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // If there are any fields to resolve later, create the cache to populate during bind.
            IDictionary<string, string> variableCache = null;
            if (delayedFields.Any())
            {
                variableCache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }

            // TODO: Although the WixSearch tables are defined in the Util extension,
            // the Bundle Binder has to know all about them. We hope to revisit all
            // of this in the 4.0 timeframe.
            IEnumerable<WixSearchInfo> orderedSearches = this.OrderSearches();

            // Extract files that come from cabinet files (this does not extract files from merge modules).
            {
                ExtractEmbeddedFilesCommand extractEmbeddedFilesCommand = new ExtractEmbeddedFilesCommand();
                extractEmbeddedFilesCommand.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
                extractEmbeddedFilesCommand.Execute();
            }

            // Get the explicit payloads.
            RowDictionary<WixBundlePayloadRow> payloads = new RowDictionary<WixBundlePayloadRow>(this.Output.Tables["WixBundlePayload"]);

            // Update explicitly authored payloads with their parent package and container (as appropriate)
            // to make it easier to gather the payloads later.
            foreach (WixGroupRow row in wixGroupTable.RowsAs<WixGroupRow>())
            {
                if (ComplexReferenceChildType.Payload == row.ChildType)
                {
                    WixBundlePayloadRow payload = payloads.Get(row.ChildId);

                    if (ComplexReferenceParentType.Package == row.ParentType)
                    {
                        Debug.Assert(String.IsNullOrEmpty(payload.Package));
                        payload.Package = row.ParentId;
                    }
                    else if (ComplexReferenceParentType.Container == row.ParentType)
                    {
                        Debug.Assert(String.IsNullOrEmpty(payload.Container));
                        payload.Container = row.ParentId;
                    }
                    else if (ComplexReferenceParentType.Layout == row.ParentType)
                    {
                        payload.LayoutOnly = true;
                    }
                }
            }

            List<FileTransfer> fileTransfers = new List<FileTransfer>();
            string layoutDirectory = Path.GetDirectoryName(this.OutputPath);

            // Process the explicitly authored payloads.
            ISet<string> processedPayloads;
            {
                ProcessPayloadsCommand command = new ProcessPayloadsCommand();
                command.Payloads = payloads.Values;
                command.DefaultPackaging = bundleRow.DefaultPackagingType;
                command.LayoutDirectory = layoutDirectory;
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);

                processedPayloads = new HashSet<string>(payloads.Keys);
            }

            IDictionary<string, PackageFacade> facades;
            {
                GetPackageFacadesCommand command = new GetPackageFacadesCommand();
                command.PackageTable = chainPackageTable;
                command.ExePackageTable = this.Output.Tables["WixBundleExePackage"];
                command.MsiPackageTable = this.Output.Tables["WixBundleMsiPackage"];
                command.MspPackageTable = this.Output.Tables["WixBundleMspPackage"];
                command.MsuPackageTable = this.Output.Tables["WixBundleMsuPackage"];
                command.Execute();

                facades = command.PackageFacades;
            }

            // Process each package facade. Note this is likely to add payloads and other rows to tables so
            // note that any indexes created above may be out of date now.
            foreach (PackageFacade facade in facades.Values)
            {
                switch (facade.Package.Type)
                {
                    case WixBundlePackageType.Exe:
                        {
                            ProcessExePackageCommand command = new ProcessExePackageCommand();
                            command.AuthoredPayloads = payloads;
                            command.Facade = facade;
                            command.Execute();

                           // ? variableCache.Add(String.Concat("packageManufacturer.", facade.Package.WixChainItemId), facade.ExePackage.Manufacturer);
                        }
                        break;

                    case WixBundlePackageType.Msi:
                        {
                            ProcessMsiPackageCommand command = new ProcessMsiPackageCommand();
                            command.AuthoredPayloads = payloads;
                            command.Facade = facade;
                            command.FileManager = this.FileManagers.First();
                            command.MsiFeatureTable = this.Output.EnsureTable(this.TableDefinitions["WixBundleMsiFeature"]);
                            command.MsiPropertyTable = this.Output.EnsureTable(this.TableDefinitions["WixBundleMsiProperty"]);
                            command.PayloadTable = this.Output.Tables["WixBundlePayload"];
                            command.RelatedPackageTable = this.Output.EnsureTable(this.TableDefinitions["WixBundleRelatedPackage"]);
                            command.Execute();

                            if (null != variableCache)
                            {
                                variableCache.Add(String.Concat("packageLanguage.", facade.Package.WixChainItemId), facade.MsiPackage.ProductLanguage.ToString());

                                if (null != facade.MsiPackage.Manufacturer)
                                {
                                    variableCache.Add(String.Concat("packageManufacturer.", facade.Package.WixChainItemId), facade.MsiPackage.Manufacturer);
                                }
                            }

                        }
                        break;

                    case WixBundlePackageType.Msp:
                        {
                            ProcessMspPackageCommand command = new ProcessMspPackageCommand();
                            command.AuthoredPayloads = payloads;
                            command.Facade = facade;
                            command.WixBundlePatchTargetCodeTable = this.Output.EnsureTable(this.TableDefinitions["WixBundlePatchTargetCode"]);
                            command.Execute();
                        }
                        break;

                    case WixBundlePackageType.Msu:
                        {
                            ProcessMsuPackageCommand command = new ProcessMsuPackageCommand();
                            command.Facade = facade;
                            command.Execute();
                        }
                        break;
                }

                if (null != variableCache)
                {
                    BindBundleCommand.PopulatePackageVariableCache(facade.Package, variableCache);
                }
            }

            // Reindex the payloads now that all the payloads (minus the manifest payloads that will be created later)
            // are present.
            payloads = new RowDictionary<WixBundlePayloadRow>(this.Output.Tables["WixBundlePayload"]);

            // Process the payloads that were added by processing the packages.
            {
                ProcessPayloadsCommand command = new ProcessPayloadsCommand();
                command.Payloads = payloads.Values.Where(r => !processedPayloads.Contains(r.Id)).ToList();
                command.DefaultPackaging = bundleRow.DefaultPackagingType;
                command.LayoutDirectory = layoutDirectory;
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);

                processedPayloads = null;
            }

            // Set the package metadata from the payloads now that we have the complete payload information.
            ILookup<string, WixBundlePayloadRow> payloadsByPackage = payloads.Values.ToLookup(p => p.Package);

            {
                foreach (PackageFacade facade in facades.Values)
                {
                    facade.Package.Size = 0;

                    IEnumerable<WixBundlePayloadRow> packagePayloads = payloadsByPackage[facade.Package.WixChainItemId];

                    foreach (WixBundlePayloadRow payload in packagePayloads)
                    {
                        facade.Package.Size += payload.FileSize;
                    }

                    if (!facade.Package.InstallSize.HasValue)
                    {
                        facade.Package.InstallSize = facade.Package.Size;

                    }

                    WixBundlePayloadRow packagePayload = payloads[facade.Package.PackagePayload];

                    if (String.IsNullOrEmpty(facade.Package.Description))
                    {
                        facade.Package.Description = packagePayload.Description;
                    }

                    if (String.IsNullOrEmpty(facade.Package.DisplayName))
                    {
                        facade.Package.DisplayName = packagePayload.DisplayName;
                    }
                }
            }


            // Give the UX payloads their embedded IDs...
            int uxPayloadIndex = 0;
            {
                foreach (WixBundlePayloadRow payload in payloads.Values.Where(p => Compiler.BurnUXContainerId == p.Container))
                {
                    // In theory, UX payloads could be embedded in the UX CAB, external to the bundle EXE, or even
                    // downloaded. The current engine requires the UX to be fully present before any downloading starts,
                    // so that rules out downloading. Also, the burn engine does not currently copy external UX payloads
                    // into the temporary UX directory correctly, so we don't allow external either.
                    if (PackagingType.Embedded != payload.Packaging)
                    {
                        Messaging.Instance.OnMessage(WixWarnings.UxPayloadsOnlySupportEmbedding(payload.SourceLineNumbers, payload.FullFileName));
                        payload.Packaging = PackagingType.Embedded;
                    }

                    payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, uxPayloadIndex);
                    ++uxPayloadIndex;
                }

                if (0 == uxPayloadIndex)
                {
                    // If we didn't get any UX payloads, it's an error!
                    throw new WixException(WixErrors.MissingBundleInformation("BootstrapperApplication"));
                }

                // Give the embedded payloads without an embedded id yet an embedded id.
                int payloadIndex = 0;
                foreach (WixBundlePayloadRow payload in payloads.Values)
                {
                    Debug.Assert(PackagingType.Unknown != payload.Packaging);

                    if (PackagingType.Embedded == payload.Packaging && String.IsNullOrEmpty(payload.EmbeddedId))
                    {
                        payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnAttachedContainerEmbeddedIdFormat, payloadIndex);
                        ++payloadIndex;
                    }
                }
            }

            // Determine patches to automatically slipstream.
            {
                AutomaticallySlipstreamPatchesCommand command = new AutomaticallySlipstreamPatchesCommand();
                command.PackageFacades = facades.Values;
                command.SlipstreamMspTable = this.Output.EnsureTable(this.TableDefinitions["WixBundleSlipstreamMsp"]);
                command.WixBundlePatchTargetCodeTable = this.Output.EnsureTable(this.TableDefinitions["WixBundlePatchTargetCode"]);
                command.Execute();
            }

            // If catalog files exist, non-embedded payloads should validate with the catalogs.
            IEnumerable<WixBundleCatalogRow> catalogs = this.Output.Tables["WixBundleCatalog"].RowsAs<WixBundleCatalogRow>();

            if (catalogs.Any())
            {
                VerifyPayloadsWithCatalogCommand command = new VerifyPayloadsWithCatalogCommand();
                command.Catalogs = catalogs;
                command.Payloads = payloads.Values;
                command.Execute();
            }

            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            IEnumerable<PackageFacade> orderedFacades;
            IEnumerable<WixBundleRollbackBoundaryRow> boundaries;
            {
                OrderPackagesAndRollbackBoundariesCommand command = new OrderPackagesAndRollbackBoundariesCommand();
                command.Boundaries = new RowDictionary<WixBundleRollbackBoundaryRow>(this.Output.Tables["WixBundleRollbackBoundary"]);
                command.PackageFacades = facades;
                command.WixGroupTable = wixGroupTable;
                command.Execute();

                orderedFacades = command.OrderedPackageFacades;
                boundaries = command.UsedRollbackBoundaries;
            }

            // Resolve any delayed fields before generating the manifest.
            if (delayedFields.Any())
            {
                ResolveDelayedFieldsCommand resolveDelayedFieldsCommand = new ResolveDelayedFieldsCommand();
                resolveDelayedFieldsCommand.OutputType = this.Output.Type;
                resolveDelayedFieldsCommand.DelayedFields = delayedFields;
                resolveDelayedFieldsCommand.ModularizationGuid = null;
                resolveDelayedFieldsCommand.VariableCache = variableCache;
                resolveDelayedFieldsCommand.Execute();
            }

            // Set the overridable bundle provider key.
            this.SetBundleProviderKey(this.Output, bundleRow);

            // Import or generate dependency providers for packages in the manifest.
            this.ProcessDependencyProviders(this.Output, facades);

            // Update the bundle per-machine/per-user scope based on the chained packages.
            this.ResolveBundleInstallScope(bundleRow, orderedFacades);

            // Generate the core-defined BA manifest tables...
            {
                CreateBootstrapperApplicationManifestCommand command = new CreateBootstrapperApplicationManifestCommand();
                command.BundleRow = bundleRow;
                command.ChainPackages = orderedFacades;
                command.LastUXPayloadIndex = uxPayloadIndex;
                command.MsiFeatures = this.Output.Tables["WixBundleMsiFeature"].RowsAs<WixBundleMsiFeatureRow>();
                command.Output = this.Output;
                command.Payloads = payloads;
                command.TableDefinitions = this.TableDefinitions;
                command.TempFilesLocation = this.TempFilesLocation;
                command.Execute();

                WixBundlePayloadRow baManifestPayload = command.BootstrapperApplicationManifestPayloadRow;
                payloads.Add(baManifestPayload);
            }

            foreach (BinderExtension extension in this.Extensions)
            {
                extension.Finish(Output);
            }

            // Create all the containers except the UX container first so the manifest (that goes in the UX container)
            // can contain all size and hash information about the non-UX containers.
            RowDictionary<WixBundleContainerRow> containers = new RowDictionary<WixBundleContainerRow>(this.Output.Tables["WixBundleContainer"]);

            ILookup<string, WixBundlePayloadRow> payloadsByContainer = payloads.Values.ToLookup(p => p.Container);

            int attachedContainerIndex = 1; // count starts at one because UX container is "0".

            IEnumerable<WixBundlePayloadRow> uxContainerPayloads = Enumerable.Empty<WixBundlePayloadRow>();

            foreach (WixBundleContainerRow container in containers.Values)
            {
                IEnumerable<WixBundlePayloadRow> containerPayloads = payloadsByContainer[container.Id];

                if (!containerPayloads.Any())
                {
                    if (container.Id != Compiler.BurnDefaultAttachedContainerId)
                    {
                        // TODO: display warning that we're ignoring container that ended up with no paylods in it.
                    }
                }
                else if (Compiler.BurnUXContainerId == container.Id)
                {
                    container.WorkingPath = Path.Combine(this.TempFilesLocation, container.Name);
                    container.AttachedContainerIndex = 0;

                    // Gather the list of UX payloads but ensure the BootstrapperApplication Payload is the first
                    // in the list since that is the Payload that Burn attempts to load.
                    List<WixBundlePayloadRow> uxPayloads = new List<WixBundlePayloadRow>();

                    string baPayloadId = baRow.FieldAsString(0);

                    foreach (WixBundlePayloadRow uxPayload in containerPayloads)
                    {
                        if (uxPayload.Id == baPayloadId)
                        {
                            uxPayloads.Insert(0, uxPayload);
                        }
                        else
                        {
                            uxPayloads.Add(uxPayload);
                        }
                    }

                    uxContainerPayloads = uxPayloads;
                }
                else
                {
                    container.WorkingPath = Path.Combine(this.TempFilesLocation, container.Name);

                    // Add detached containers to the list of file transfers.
                    if (ContainerType.Detached == container.Type)
                    {
                        FileTransfer transfer;
                        if (FileTransfer.TryCreate(container.WorkingPath, Path.Combine(layoutDirectory, container.Name), true, "Container", container.SourceLineNumbers, out transfer))
                        {
                            transfer.Built = true;
                            fileTransfers.Add(transfer);
                        }
                    }
                    else // update the attached container index.
                    {
                        Debug.Assert(ContainerType.Attached == container.Type);

                        container.AttachedContainerIndex = attachedContainerIndex;
                        ++attachedContainerIndex;
                    }

                    this.CreateContainer(container, containerPayloads, null);
                }
            }

            // Create the bundle manifest then UX container.
            string manifestPath = Path.Combine(this.TempFilesLocation, "bundle-manifest.xml");
            {
                CreateBurnManifestCommand command = new CreateBurnManifestCommand();
                command.FileManagers = this.FileManagers;
                command.Output = this.Output;

                command.BundleInfo = bundleRow;
                command.Chain = chainRow;
                command.Containers = containers;
                command.Catalogs = catalogs;
                command.ExecutableName = Path.GetFileName(this.OutputPath);
                command.OrderedPackages = orderedFacades;
                command.OutputPath = manifestPath;
                command.RollbackBoundaries = boundaries;
                command.OrderedSearches = orderedSearches;
                command.Payloads = payloads;
                command.UXContainerPayloads = uxContainerPayloads;
                command.Execute();
            }

            WixBundleContainerRow uxContainer = containers[Compiler.BurnUXContainerId];
            this.CreateContainer(uxContainer, uxContainerPayloads, manifestPath);

            // Copy the burn.exe to a writable location then mark it to be moved to its final build location. Note
            // that today, the x64 Burn uses the x86 stub.
            string stubPlatform = (Platform.X64 == bundleRow.Platform) ? "x86" : bundleRow.Platform.ToString();

            string stubFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), stubPlatform, "burn.exe");
            string bundleTempPath = Path.Combine(this.TempFilesLocation, Path.GetFileName(this.OutputPath));

            Messaging.Instance.OnMessage(WixVerboses.GeneratingBundle(bundleTempPath, stubFile));

            string bundleFilename = Path.GetFileName(this.OutputPath);
            if ("setup.exe".Equals(bundleFilename, StringComparison.OrdinalIgnoreCase))
            {
                Messaging.Instance.OnMessage(WixErrors.InsecureBundleFilename(bundleFilename));
            }

            FileTransfer bundleTransfer;
            if (FileTransfer.TryCreate(bundleTempPath, this.OutputPath, true, "Bundle", bundleRow.SourceLineNumbers, out bundleTransfer))
            {
                bundleTransfer.Built = true;
                fileTransfers.Add(bundleTransfer);
            }

            File.Copy(stubFile, bundleTempPath, true);
            File.SetAttributes(bundleTempPath, FileAttributes.Normal);

            this.UpdateBurnResources(bundleTempPath, this.OutputPath, bundleRow);

            // Update the .wixburn section to point to at the UX and attached container(s) then attach the containers
            // if they should be attached.
            using (BurnWriter writer = BurnWriter.Open(bundleTempPath))
            {
                FileInfo burnStubFile = new FileInfo(bundleTempPath);
                writer.InitializeBundleSectionData(burnStubFile.Length, bundleRow.BundleId);

                // Always attach the UX container first
                writer.AppendContainer(uxContainer.WorkingPath, BurnWriter.Container.UX);

                // Now append all other attached containers
                foreach (WixBundleContainerRow container in containers.Values)
                {
                    if (ContainerType.Attached == container.Type)
                    {
                        // The container was only created if it had payloads.
                        if (!String.IsNullOrEmpty(container.WorkingPath) && Compiler.BurnUXContainerId != container.Id)
                        {
                            writer.AppendContainer(container.WorkingPath, BurnWriter.Container.Attached);
                        }
                    }
                }
            }

            if (null != this.PdbFile)
            {
                Pdb pdb = new Pdb();
                pdb.Output = Output;
                pdb.Save(this.PdbFile);
            }

            this.FileTransfers = fileTransfers;
            this.ContentFilePaths = payloads.Values.Where(p => p.ContentFile).Select(p => p.FullFileName).ToList();
        }

        private Table GetRequiredTable(string tableName)
        {
            Table table = this.Output.Tables[tableName];
            if (null == table || 0 == table.Rows.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation(tableName));
            }

            return table;
        }

        private Row GetSingleRowTable(string tableName)
        {
            Table table = this.Output.Tables[tableName];
            if (null == table || 1 != table.Rows.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation(tableName));
            }

            return table.Rows[0];
        }

        private List<WixSearchInfo> OrderSearches()
        {
            Dictionary<string, WixSearchInfo> allSearches = new Dictionary<string, WixSearchInfo>();
            Table wixFileSearchTable = this.Output.Tables["WixFileSearch"];
            if (null != wixFileSearchTable && 0 < wixFileSearchTable.Rows.Count)
            {
                foreach (Row row in wixFileSearchTable.Rows)
                {
                    WixFileSearchInfo fileSearchInfo = new WixFileSearchInfo(row);
                    allSearches.Add(fileSearchInfo.Id, fileSearchInfo);
                }
            }

            Table wixRegistrySearchTable = this.Output.Tables["WixRegistrySearch"];
            if (null != wixRegistrySearchTable && 0 < wixRegistrySearchTable.Rows.Count)
            {
                foreach (Row row in wixRegistrySearchTable.Rows)
                {
                    WixRegistrySearchInfo registrySearchInfo = new WixRegistrySearchInfo(row);
                    allSearches.Add(registrySearchInfo.Id, registrySearchInfo);
                }
            }

            Table wixComponentSearchTable = this.Output.Tables["WixComponentSearch"];
            if (null != wixComponentSearchTable && 0 < wixComponentSearchTable.Rows.Count)
            {
                foreach (Row row in wixComponentSearchTable.Rows)
                {
                    WixComponentSearchInfo componentSearchInfo = new WixComponentSearchInfo(row);
                    allSearches.Add(componentSearchInfo.Id, componentSearchInfo);
                }
            }

            Table wixProductSearchTable = this.Output.Tables["WixProductSearch"];
            if (null != wixProductSearchTable && 0 < wixProductSearchTable.Rows.Count)
            {
                foreach (Row row in wixProductSearchTable.Rows)
                {
                    WixProductSearchInfo productSearchInfo = new WixProductSearchInfo(row);
                    allSearches.Add(productSearchInfo.Id, productSearchInfo);
                }
            }

            // Merge in the variable/condition info and get the canonical ordering for
            // the searches.
            List<WixSearchInfo> orderedSearches = new List<WixSearchInfo>();
            Table wixSearchTable = this.Output.Tables["WixSearch"];
            if (null != wixSearchTable && 0 < wixSearchTable.Rows.Count)
            {
                orderedSearches.Capacity = wixSearchTable.Rows.Count;
                foreach (Row row in wixSearchTable.Rows)
                {
                    WixSearchInfo searchInfo = allSearches[(string)row[0]];
                    searchInfo.AddWixSearchRowInfo(row);
                    orderedSearches.Add(searchInfo);
                }
            }

            return orderedSearches;
        }

        /// <summary>
        /// Populates the variable cache with specific package properties.
        /// </summary>
        /// <param name="package">The package with properties to cache.</param>
        /// <param name="variableCache">The property cache.</param>
        private static void PopulatePackageVariableCache(WixBundlePackageRow package, IDictionary<string, string> variableCache)
        {
            string id = package.WixChainItemId;

            variableCache.Add(String.Concat("packageDescription.", id), package.Description);
            //variableCache.Add(String.Concat("packageLanguage.", id), package.Language);
            //variableCache.Add(String.Concat("packageManufacturer.", id), package.Manufacturer);
            variableCache.Add(String.Concat("packageName.", id), package.DisplayName);
            variableCache.Add(String.Concat("packageVersion.", id), package.Version);
        }

        private void CreateContainer(WixBundleContainerRow container, IEnumerable<WixBundlePayloadRow> containerPayloads, string manifestFile)
        {
            CreateContainerCommand command = new CreateContainerCommand();
            command.DefaultCompressionLevel = this.DefaultCompressionLevel;
            command.Payloads = containerPayloads;
            command.ManifestFile = manifestFile;
            command.OutputPath = container.WorkingPath;
            command.Execute();

            container.Hash = command.Hash;
            container.Size = command.Size;
        }

        private void ResolveBundleInstallScope(WixBundleRow bundleInfo, IEnumerable<PackageFacade> facades)
        {
            foreach (PackageFacade facade in facades)
            {
                if (bundleInfo.PerMachine && YesNoDefaultType.No == facade.Package.PerMachine)
                {
                    Messaging.Instance.OnMessage(WixVerboses.SwitchingToPerUserPackage(facade.Package.SourceLineNumbers, facade.Package.WixChainItemId));

                    bundleInfo.PerMachine = false;
                    break;
                }
            }

            foreach (PackageFacade facade in facades)
            {
                // Update package scope from bundle scope if default.
                if (YesNoDefaultType.Default == facade.Package.PerMachine)
                {
                    facade.Package.PerMachine = bundleInfo.PerMachine ? YesNoDefaultType.Yes : YesNoDefaultType.No;
                }

                // We will only register packages in the same scope as the bundle. Warn if any packages with providers
                // are in a different scope and not permanent (permanents typically don't need a ref-count).
                if (!bundleInfo.PerMachine && YesNoDefaultType.Yes == facade.Package.PerMachine && !facade.Package.Permanent && 0 < facade.Provides.Count)
                {
                    Messaging.Instance.OnMessage(WixWarnings.NoPerMachineDependencies(facade.Package.SourceLineNumbers, facade.Package.WixChainItemId));
                }
            }
        }

        private void UpdateBurnResources(string bundleTempPath, string outputPath, WixBundleRow bundleInfo)
        {
            WixToolset.Dtf.Resources.ResourceCollection resources = new WixToolset.Dtf.Resources.ResourceCollection();
            WixToolset.Dtf.Resources.VersionResource version = new WixToolset.Dtf.Resources.VersionResource("#1", 1033);

            version.Load(bundleTempPath);
            resources.Add(version);

            // Ensure the bundle info provides a full four part version.
            Version fourPartVersion = new Version(bundleInfo.Version);
            int major = (fourPartVersion.Major < 0) ? 0 : fourPartVersion.Major;
            int minor = (fourPartVersion.Minor < 0) ? 0 : fourPartVersion.Minor;
            int build = (fourPartVersion.Build < 0) ? 0 : fourPartVersion.Build;
            int revision = (fourPartVersion.Revision < 0) ? 0 : fourPartVersion.Revision;

            if (UInt16.MaxValue < major || UInt16.MaxValue < minor || UInt16.MaxValue < build || UInt16.MaxValue < revision)
            {
                throw new WixException(WixErrors.InvalidModuleOrBundleVersion(bundleInfo.SourceLineNumbers, "Bundle", bundleInfo.Version));
            }

            fourPartVersion = new Version(major, minor, build, revision);
            version.FileVersion = fourPartVersion;
            version.ProductVersion = fourPartVersion;

            WixToolset.Dtf.Resources.VersionStringTable strings = version[1033];
            strings["LegalCopyright"] = bundleInfo.Copyright;
            strings["OriginalFilename"] = Path.GetFileName(outputPath);
            strings["FileVersion"] = bundleInfo.Version;    // string versions do not have to be four parts.
            strings["ProductVersion"] = bundleInfo.Version; // string versions do not have to be four parts.

            if (!String.IsNullOrEmpty(bundleInfo.Name))
            {
                strings["ProductName"] = bundleInfo.Name;
                strings["FileDescription"] = bundleInfo.Name;
            }

            if (!String.IsNullOrEmpty(bundleInfo.Publisher))
            {
                strings["CompanyName"] = bundleInfo.Publisher;
            }
            else
            {
                strings["CompanyName"] = String.Empty;
            }

            if (!String.IsNullOrEmpty(bundleInfo.IconPath))
            {
                Dtf.Resources.GroupIconResource iconGroup = new Dtf.Resources.GroupIconResource("#1", 1033);
                iconGroup.ReadFromFile(bundleInfo.IconPath);
                resources.Add(iconGroup);

                foreach (Dtf.Resources.Resource icon in iconGroup.Icons)
                {
                    resources.Add(icon);
                }
            }

            if (!String.IsNullOrEmpty(bundleInfo.SplashScreenBitmapPath))
            {
                Dtf.Resources.BitmapResource bitmap = new Dtf.Resources.BitmapResource("#1", 1033);
                bitmap.ReadFromFile(bundleInfo.SplashScreenBitmapPath);
                resources.Add(bitmap);
            }

            resources.Save(bundleTempPath);
        }

        #region DependencyExtension
        /// <summary>
        /// Imports authored dependency providers for each package in the manifest,
        /// and generates dependency providers for certain package types that do not
        /// have a provider defined.
        /// </summary>
        /// <param name="bundle">The <see cref="Output"/> object for the bundle.</param>
        /// <param name="facades">An indexed collection of chained packages.</param>
        private void ProcessDependencyProviders(Output bundle, IDictionary<string, PackageFacade> facades)
        {
            // First import any authored dependencies. These may merge with imported provides from MSI packages.
            Table wixDependencyProviderTable = bundle.Tables["WixDependencyProvider"];
            if (null != wixDependencyProviderTable && 0 < wixDependencyProviderTable.Rows.Count)
            {
                // Add package information for each dependency provider authored into the manifest.
                foreach (Row wixDependencyProviderRow in wixDependencyProviderTable.Rows)
                {
                    string packageId = (string)wixDependencyProviderRow[1];

                    PackageFacade facade = null;
                    if (facades.TryGetValue(packageId, out facade))
                    {
                        ProvidesDependency dependency = new ProvidesDependency(wixDependencyProviderRow);

                        if (String.IsNullOrEmpty(dependency.Key))
                        {
                            switch (facade.Package.Type)
                            {
                                // The WixDependencyExtension allows an empty Key for MSIs and MSPs.
                                case WixBundlePackageType.Msi:
                                    dependency.Key = facade.MsiPackage.ProductCode;
                                    break;
                                case WixBundlePackageType.Msp:
                                    dependency.Key = facade.MspPackage.PatchCode;
                                    break;
                            }
                        }

                        if (String.IsNullOrEmpty(dependency.Version))
                        {
                            dependency.Version = facade.Package.Version;
                        }

                        // If the version is still missing, a version could not be harvested from the package and was not authored.
                        if (String.IsNullOrEmpty(dependency.Version))
                        {
                            Messaging.Instance.OnMessage(WixErrors.MissingDependencyVersion(facade.Package.WixChainItemId));
                        }

                        if (String.IsNullOrEmpty(dependency.DisplayName))
                        {
                            dependency.DisplayName = facade.Package.DisplayName;
                        }

                        if (!facade.Provides.Merge(dependency))
                        {
                            Messaging.Instance.OnMessage(WixErrors.DuplicateProviderDependencyKey(dependency.Key, facade.Package.WixChainItemId));
                        }
                    }
                }
            }

            // Generate providers for MSI packages that still do not have providers.
            foreach (PackageFacade facade in facades.Values)
            {
                string key = null;

                if (WixBundlePackageType.Msi == facade.Package.Type && 0 == facade.Provides.Count)
                {
                    key = facade.MsiPackage.ProductCode;
                }
                else if (WixBundlePackageType.Msp == facade.Package.Type && 0 == facade.Provides.Count)
                {
                    key = facade.MspPackage.PatchCode;
                }

                if (!String.IsNullOrEmpty(key))
                {
                    ProvidesDependency dependency = new ProvidesDependency(key, facade.Package.Version, facade.Package.DisplayName, 0);

                    if (!facade.Provides.Merge(dependency))
                    {
                        Messaging.Instance.OnMessage(WixErrors.DuplicateProviderDependencyKey(dependency.Key, facade.Package.WixChainItemId));
                    }
                }
            }
        }

        /// <summary>
        /// Sets the provider key for the bundle.
        /// </summary>
        /// <param name="bundle">The <see cref="Output"/> object for the bundle.</param>
        /// <param name="bundleInfo">The <see cref="BundleInfo"/> containing the provider key and other information for the bundle.</param>
        private void SetBundleProviderKey(Output bundle, WixBundleRow bundleInfo)
        {
            // From DependencyCommon.cs in the WixDependencyExtension.
            const int ProvidesAttributesBundle = 0x10000;

            Table wixDependencyProviderTable = bundle.Tables["WixDependencyProvider"];
            if (null != wixDependencyProviderTable && 0 < wixDependencyProviderTable.Rows.Count)
            {
                // Search the WixDependencyProvider table for the single bundle provider key.
                foreach (Row wixDependencyProviderRow in wixDependencyProviderTable.Rows)
                {
                    object attributes = wixDependencyProviderRow[5];
                    if (null != attributes && 0 != (ProvidesAttributesBundle & (int)attributes))
                    {
                        bundleInfo.ProviderKey = (string)wixDependencyProviderRow[2];
                        break;
                    }
                }
            }

            // Defaults to the bundle ID as the provider key.
        }
        #endregion
    }
}
