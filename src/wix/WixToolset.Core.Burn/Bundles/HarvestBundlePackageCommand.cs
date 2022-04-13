// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class HarvestBundlePackageCommand
    {
        public HarvestBundlePackageCommand(IServiceProvider serviceProvider, string intermediateFolder, WixBundlePayloadSymbol payloadSymbol)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.BackendHelper = serviceProvider.GetService<IBackendHelper>();
            this.IntermediateFolder = intermediateFolder;

            this.PackagePayload = payloadSymbol;
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private string IntermediateFolder { get; }

        private WixBundlePayloadSymbol PackagePayload { get; }

        public WixBundleHarvestedBundlePackageSymbol HarvestedBundlePackage { get; private set; }

        public WixBundleHarvestedDependencyProviderSymbol HarvestedDependencyProvider { get; private set; }

        public List<WixBundlePackageRelatedBundleSymbol> RelatedBundles { get; } = new List<WixBundlePackageRelatedBundleSymbol>();

        public List<ITrackedFile> TrackedFiles { get; } = new List<ITrackedFile>();

        public void Execute()
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
                    return;
                }

                var baFolderPath = Path.Combine(this.IntermediateFolder, burnReader.BundleId.ToString());

                if (!burnReader.ExtractUXContainer(baFolderPath, baFolderPath))
                {
                    return;
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
                        return;
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
                    this.HarvestedDependencyProvider = new WixBundleHarvestedDependencyProviderSymbol(sourceLineNumbers, depId)
                    {
                        PackagePayloadRef = this.PackagePayload.Id.Id,
                        ProviderKey = providerKey,
                        Version = version,
                    };

                    displayName = arpElement.GetAttribute("DisplayName");

                    installSize = this.ProcessPackages(document, namespaceManager);

                    this.ProcessRelatedBundles(document, namespaceManager, sourcePath);

                    // TODO: Add payloads?
                }
                catch (Exception e)
                {
                    this.Messaging.Write(BurnBackendErrors.InvalidBundleManifest(sourceLineNumbers, sourcePath, e.ToString()));
                    return;
                }
            }

            this.HarvestedBundlePackage = new WixBundleHarvestedBundlePackageSymbol(this.PackagePayload.SourceLineNumbers, this.PackagePayload.Id)
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
            };
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

                this.RelatedBundles.Add(new WixBundlePackageRelatedBundleSymbol(sourceLineNumbers)
                {
                    PackagePayloadRef = this.PackagePayload.Id.Id,
                    BundleId = id,
                    Action = action,
                });
            }
        }
    }
}
