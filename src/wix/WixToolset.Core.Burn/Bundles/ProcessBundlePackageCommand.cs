// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Initializes package state from the Bundle contents.
    /// </summary>
    internal class ProcessBundlePackageCommand
    {
        public ProcessBundlePackageCommand(IServiceProvider serviceProvider, IEnumerable<IBurnBackendBinderExtension> backendExtensions, IntermediateSection section, PackageFacade facade, Dictionary<string, WixBundlePayloadSymbol> packagePayloads, string intermediateFolder)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.BackendExtensions = backendExtensions;
            this.PackagePayloads = packagePayloads;
            this.Section = section;
            this.IntermediateFolder = intermediateFolder;

            this.ChainPackage = facade.PackageSymbol;
            this.BundlePackage = (WixBundleBundlePackageSymbol)facade.SpecificPackageSymbol;
            this.BundlePackagePayload = (WixBundleBundlePackagePayloadSymbol)facade.SpecificPackagePayloadSymbol;
            this.PackagePayload = packagePayloads[this.ChainPackage.PayloadRef];
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IEnumerable<IBurnBackendBinderExtension> BackendExtensions { get; }

        private Dictionary<string, WixBundlePayloadSymbol> PackagePayloads { get; }

        private WixBundlePackageSymbol ChainPackage { get; }

        private WixBundleBundlePackageSymbol BundlePackage { get; }

        private WixBundleBundlePackagePayloadSymbol BundlePackagePayload { get; }

        private string PackageId => this.ChainPackage.Id.Id;

        private WixBundlePayloadSymbol PackagePayload { get; }

        private IntermediateSection Section { get; }

        private string IntermediateFolder { get; }

        public List<ITrackedFile> TrackedFiles { get; } = new List<ITrackedFile>();

        /// <summary>
        /// Processes the Bundle packages to add properties and payloads from the Bundle packages.
        /// </summary>
        public void Execute()
        {
            var harvestedBundlePackage = this.Section.Symbols.OfType<WixBundleHarvestedBundlePackageSymbol>()
                                                             .Where(h => h.Id.Id == this.PackagePayload.Id.Id)
                                                             .SingleOrDefault();

            if (harvestedBundlePackage == null)
            {
                harvestedBundlePackage = this.HarvestPackage();

                if (harvestedBundlePackage == null)
                {
                    return;
                }
            }

            this.ChainPackage.Win64 = harvestedBundlePackage.Win64;
            this.BundlePackage.BundleId = Guid.Parse(harvestedBundlePackage.BundleId).ToString("B").ToUpperInvariant();
            this.BundlePackage.EngineVersion = harvestedBundlePackage.EngineVersion;
            this.BundlePackage.SupportsBurnProtocol = harvestedBundlePackage.ProtocolVersion == BurnCommon.BURN_PROTOCOL_VERSION;

            var supportsArpSystemComponent = BurnCommon.BurnV3Namespace != harvestedBundlePackage.ManifestNamespace;
            if (!supportsArpSystemComponent && !this.ChainPackage.Visible)
            {
                this.Messaging.Write(BurnBackendWarnings.HiddenBundleNotSupported(this.PackagePayload.SourceLineNumbers, this.PackageId));

                this.ChainPackage.Visible = true;
            }

            this.ChainPackage.PerMachine = harvestedBundlePackage.PerMachine;
            this.PackagePayload.Version = harvestedBundlePackage.Version;
            this.BundlePackage.Version = harvestedBundlePackage.Version;
            this.ChainPackage.Version = harvestedBundlePackage.Version;

            if (String.IsNullOrEmpty(this.ChainPackage.CacheId))
            {
                this.ChainPackage.CacheId = CacheIdGenerator.GenerateLocalCacheId(this.Messaging, harvestedBundlePackage, this.PackagePayload, this.BundlePackage.SourceLineNumbers, "BundlePackage");
            }

            if (String.IsNullOrEmpty(this.ChainPackage.DisplayName))
            {
                this.ChainPackage.DisplayName = harvestedBundlePackage.DisplayName;
            }

            this.ChainPackage.InstallSize = harvestedBundlePackage.InstallSize;
        }

        private WixBundleHarvestedBundlePackageSymbol HarvestPackage()
        {
            var command = new HarvestBundlePackageCommand(this.ServiceProvider, this.BackendExtensions, this.IntermediateFolder, this.PackagePayload, this.BundlePackagePayload, this.PackagePayloads);
            command.Execute();

            this.TrackedFiles.AddRange(command.TrackedFiles);
            if (this.Messaging.EncounteredError)
            {
                return null;
            }

            this.Section.AddSymbol(command.HarvestedBundlePackage);
            this.Section.AddSymbol(command.HarvestedDependencyProvider);

            foreach (var payload in command.Payloads)
            {
                this.Section.AddSymbol(payload);
            }

            foreach (var relatedBundle in command.RelatedBundles)
            {
                this.Section.AddSymbol(relatedBundle);
            }

            return command.HarvestedBundlePackage;
        }
    }
}
