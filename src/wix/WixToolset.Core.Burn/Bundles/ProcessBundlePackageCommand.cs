// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Initializes package state from the Bundle contents.
    /// </summary>
    internal class ProcessBundlePackageCommand
    {
        public ProcessBundlePackageCommand(IServiceProvider serviceProvider, IntermediateSection section, PackageFacade facade, Dictionary<string, WixBundlePayloadSymbol> packagePayloads, string intermediateFolder)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.BackendHelper = serviceProvider.GetService<IBackendHelper>();
            this.PackagePayloads = packagePayloads;
            this.Section = section;
            this.IntermediateFolder = intermediateFolder;

            this.ChainPackage = facade.PackageSymbol;
            this.BundlePackage = (WixBundleBundlePackageSymbol)facade.SpecificPackageSymbol;
            this.PackagePayload = packagePayloads[this.ChainPackage.PayloadRef];
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private Dictionary<string, WixBundlePayloadSymbol> PackagePayloads { get; }

        private WixBundlePackageSymbol ChainPackage { get; }

        private WixBundleBundlePackageSymbol BundlePackage { get; }

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
                this.ChainPackage.CacheId = String.Format("{0}v{1}", this.BundlePackage.BundleId, this.BundlePackage.Version);
            }

            if (String.IsNullOrEmpty(this.ChainPackage.DisplayName))
            {
                this.ChainPackage.DisplayName = harvestedBundlePackage.DisplayName;
            }

            this.ChainPackage.InstallSize = harvestedBundlePackage.InstallSize;
        }

        public WixBundleHarvestedBundlePackageSymbol HarvestPackage()
        {
            bool win64;
            string bundleId;
            string engineVersion;
            int protocolVersion;
            string manifestNamespace;
            bool perMachine;
            string version;
            string displayName;
            long installSize;

            var sourcePath = this.PackagePayload.SourceFile.Path;
            var sourceLineNumbers = this.PackagePayload.SourceLineNumbers;

            using (var burnReader = BurnReader.Open(this.Messaging, sourcePath))
            {
                if (burnReader.Invalid)
                {
                    return null;
                }

                var baFolderPath = Path.Combine(this.IntermediateFolder, burnReader.BundleId.ToString());

                if (!burnReader.ExtractUXContainer(baFolderPath, baFolderPath))
                {
                    return null;
                }

                foreach (var filePath in Directory.EnumerateFiles(baFolderPath, "*.*", SearchOption.AllDirectories))
                {
                    this.TrackedFiles.Add(this.BackendHelper.TrackFile(filePath, TrackedFileType.Temporary, sourceLineNumbers));
                }

                bundleId = burnReader.BundleId.ToString("B").ToUpperInvariant();

                try
                {
                    var document = new XmlDocument();
                    document.Load(Path.Combine(baFolderPath, "manifest.xml"));
                    var namespaceManager = new XmlNamespaceManager(document.NameTable);

                    if (document.DocumentElement.LocalName != "BurnManifest")
                    {
                        this.Messaging.Write(BurnBackendErrors.InvalidBundleManifest(sourceLineNumbers, sourcePath, $"Expected root element to be 'BurnManifest' but was '{document.DocumentElement.LocalName}'."));
                        return null;
                    }

                    engineVersion = document.DocumentElement.GetAttribute("EngineVersion");
                    protocolVersion = this.ProcessProtocolVersion(burnReader, document);
                    win64 = this.ProcessWin64(burnReader, document, sourceLineNumbers, sourcePath);

                    manifestNamespace = document.DocumentElement.NamespaceURI;

                    namespaceManager.AddNamespace("burn", document.DocumentElement.NamespaceURI);
                    var registrationElement = document.SelectSingleNode("/burn:BurnManifest/burn:Registration", namespaceManager) as XmlElement;
                    var arpElement = document.SelectSingleNode("/burn:BurnManifest/burn:Registration/burn:Arp", namespaceManager) as XmlElement;

                    perMachine = registrationElement.GetAttribute("PerMachine") == "yes";

                    version = registrationElement.GetAttribute("Version");

                    var providerKey = registrationElement.GetAttribute("ProviderKey");
                    var depId = new Identifier(AccessModifier.Section, this.BackendHelper.GenerateIdentifier("dep", this.PackagePayload.Id.Id, providerKey));
                    this.Section.AddSymbol(new WixBundleHarvestedDependencyProviderSymbol(sourceLineNumbers, depId)
                    {
                        PackagePayloadRef = this.PackagePayload.Id.Id,
                        ProviderKey = providerKey,
                        Version = version,
                    });

                    displayName = arpElement.GetAttribute("DisplayName");

                    installSize = this.ProcessPackages(document, namespaceManager);

                    this.ProcessRelatedBundles(document, namespaceManager, sourcePath);

                    // TODO: Add payloads?
                }
                catch (Exception e)
                {
                    this.Messaging.Write(BurnBackendErrors.InvalidBundleManifest(sourceLineNumbers, sourcePath, e.ToString()));
                    return null;
                }
            }

            return this.Section.AddSymbol(new WixBundleHarvestedBundlePackageSymbol(this.PackagePayload.SourceLineNumbers, this.PackagePayload.Id)
            {
                Win64 = win64,
                BundleId = bundleId,
                EngineVersion = engineVersion,
                ManifestNamespace = manifestNamespace,
                ProtocolVersion = protocolVersion,
                PerMachine = perMachine,
                Version = version,
                DisplayName = displayName,
                InstallSize = installSize,
            });
        }

        private int ProcessProtocolVersion(BurnReader burnReader, XmlDocument document)
        {
            var protocolVersionValue = document.DocumentElement.GetAttribute("ProtocolVersion");

            if (Int32.TryParse(protocolVersionValue, out var protocolVersion))
            {
                return protocolVersion;
            }

            // Assume that the .wixburn section version will change when the Burn protocol changes.
            // This should be a safe assumption since only old bundles should be missing the ProtocolVersion from the manifest.
            return burnReader.Version == 2 ? 1 : 0;
        }

        private bool ProcessWin64(BurnReader burnReader, XmlDocument document, SourceLineNumber sourceLineNumbers, string sourcePath)
        {
            var win64Value = document.DocumentElement.GetAttribute("Win64");

            switch (win64Value)
            {
                case "yes":
                    return true;
                case "no":
                    return false;
            }

            switch (burnReader.MachineType)
            {
                case BurnCommon.IMAGE_FILE_MACHINE_ARM:
                case BurnCommon.IMAGE_FILE_MACHINE_ARMNT:
                case BurnCommon.IMAGE_FILE_MACHINE_I386:
                case BurnCommon.IMAGE_FILE_MACHINE_LOONGARCH32:
                    return false;
                case BurnCommon.IMAGE_FILE_MACHINE_AMD64:
                case BurnCommon.IMAGE_FILE_MACHINE_ARM64:
                case BurnCommon.IMAGE_FILE_MACHINE_IA64:
                case BurnCommon.IMAGE_FILE_MACHINE_LOONGARCH64:
                    return true;
                case BurnCommon.IMAGE_FILE_MACHINE_AM33:
                case BurnCommon.IMAGE_FILE_MACHINE_EBC:
                case BurnCommon.IMAGE_FILE_MACHINE_M32R:
                case BurnCommon.IMAGE_FILE_MACHINE_MIPS16:
                case BurnCommon.IMAGE_FILE_MACHINE_MIPSFPU:
                case BurnCommon.IMAGE_FILE_MACHINE_MIPSFPU16:
                case BurnCommon.IMAGE_FILE_MACHINE_POWERPC:
                case BurnCommon.IMAGE_FILE_MACHINE_POWERPCFP:
                case BurnCommon.IMAGE_FILE_MACHINE_R4000:
                case BurnCommon.IMAGE_FILE_MACHINE_RISCV32:
                case BurnCommon.IMAGE_FILE_MACHINE_RISCV64:
                case BurnCommon.IMAGE_FILE_MACHINE_RISCV128:
                case BurnCommon.IMAGE_FILE_MACHINE_SH3:
                case BurnCommon.IMAGE_FILE_MACHINE_SH3DSP:
                case BurnCommon.IMAGE_FILE_MACHINE_SH4:
                case BurnCommon.IMAGE_FILE_MACHINE_SH5:
                case BurnCommon.IMAGE_FILE_MACHINE_THUMB:
                case BurnCommon.IMAGE_FILE_MACHINE_WCEMIPSV2:
                default:
                    this.Messaging.Write(BurnBackendWarnings.UnknownCoffMachineType(sourceLineNumbers, sourcePath, burnReader.MachineType));
                    return false;
            }
        }

        private long ProcessPackages(XmlDocument document, XmlNamespaceManager namespaceManager)
        {
            long packageInstallSize = 0;

            foreach (XmlElement packageElement in document.SelectNodes("/burn:BurnManifest/burn:Chain/*", namespaceManager))
            {
                if (!packageElement.Name.EndsWith("Package"))
                {
                    continue;
                }

                if (Int64.TryParse(packageElement.GetAttribute("InstallSize"), out var installSize))
                {
                    packageInstallSize += installSize;
                }
            }

            return packageInstallSize;
        }

        private void ProcessRelatedBundles(XmlDocument document, XmlNamespaceManager namespaceManager, string sourcePath)
        {
            var sourceLineNumbers = this.PackagePayload.SourceLineNumbers;

            foreach (XmlElement relatedBundleElement in document.SelectNodes("/burn:BurnManifest/burn:RelatedBundle", namespaceManager))
            {
                var id = relatedBundleElement.GetAttribute("Id");
                var actionValue = relatedBundleElement.GetAttribute("Action");

                if (!Enum.TryParse(actionValue, out RelatedBundleActionType action))
                {
                    this.Messaging.Write(BurnBackendWarnings.UnknownBundleRelationAction(sourceLineNumbers, sourcePath, actionValue));
                    continue;
                }

                this.Section.AddSymbol(new WixBundlePackageRelatedBundleSymbol(sourceLineNumbers)
                {
                    PackagePayloadRef = this.PackagePayload.Id.Id,
                    BundleId = id,
                    Action = action,
                });
            }
        }
    }
}
