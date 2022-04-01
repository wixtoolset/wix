// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
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
            this.Facade = facade;
            this.IntermediateFolder = intermediateFolder;
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private Dictionary<string, WixBundlePayloadSymbol> PackagePayloads { get; }

        private PackageFacade Facade { get; }

        private IntermediateSection Section { get; }

        private string IntermediateFolder { get; }

        public List<ITrackedFile> TrackedFiles { get; } = new List<ITrackedFile>();

        /// <summary>
        /// Processes the Bundle packages to add properties and payloads from the Bundle packages.
        /// </summary>
        public void Execute()
        {
            var bundlePackage = (WixBundleBundlePackageSymbol)this.Facade.SpecificPackageSymbol;
            var packagePayload = this.PackagePayloads[this.Facade.PackageSymbol.PayloadRef];
            var sourcePath = packagePayload.SourceFile.Path;

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
                    this.TrackedFiles.Add(this.BackendHelper.TrackFile(filePath, TrackedFileType.Temporary, packagePayload.SourceLineNumbers));
                }

                switch (burnReader.MachineType)
                {
                    case BurnCommon.IMAGE_FILE_MACHINE_ARM:
                    case BurnCommon.IMAGE_FILE_MACHINE_ARMNT:
                    case BurnCommon.IMAGE_FILE_MACHINE_I386:
                        break;
                    case BurnCommon.IMAGE_FILE_MACHINE_AMD64:
                    case BurnCommon.IMAGE_FILE_MACHINE_ARM64:
                        bundlePackage.Win64 = true;
                        break;
                    default:
                        Debug.Assert(false, "Unknown machine type");
                        break;
                }

                bundlePackage.BundleId = burnReader.BundleId.ToString("B").ToUpperInvariant();

                // Assume that the .wixburn section version will change when the Burn protocol changes.
                // This should be a safe assumption since we will need to add the protocol version to the section to support this harvesting.
                bundlePackage.SupportsBurnProtocol = burnReader.Version == 2;

                var document = new XmlDocument();
                document.Load(Path.Combine(baFolderPath, "manifest.xml"));
                var namespaceManager = new XmlNamespaceManager(document.NameTable);
                namespaceManager.AddNamespace("burn", BurnCommon.BurnNamespace); // TODO: support v3 bundles
                var registrationElement = document.SelectSingleNode("/burn:BurnManifest/burn:Registration", namespaceManager) as XmlElement;
                var arpElement = document.SelectSingleNode("/burn:BurnManifest/burn:Registration/burn:Arp", namespaceManager) as XmlElement;

                var perMachine = registrationElement.GetAttribute("PerMachine") == "yes";
                this.Facade.PackageSymbol.PerMachine = perMachine ? YesNoDefaultType.Yes : YesNoDefaultType.No;

                var version = registrationElement.GetAttribute("Version");
                packagePayload.Version = version;
                bundlePackage.Version = version;
                this.Facade.PackageSymbol.Version = version;

                if (String.IsNullOrEmpty(this.Facade.PackageSymbol.CacheId))
                {
                    this.Facade.PackageSymbol.CacheId = String.Format("{0}v{1}", bundlePackage.BundleId, version);
                }

                var providerKey = registrationElement.GetAttribute("ProviderKey");
                var depId = new Identifier(AccessModifier.Section, this.BackendHelper.GenerateIdentifier("dep", bundlePackage.Id.Id, providerKey));
                this.Section.AddSymbol(new WixDependencyProviderSymbol(packagePayload.SourceLineNumbers, depId)
                {
                    ParentRef = bundlePackage.Id.Id,
                    ProviderKey = providerKey,
                    Version = version,
                    Attributes = WixDependencyProviderAttributes.ProvidesAttributesImported,
                });

                if (String.IsNullOrEmpty(this.Facade.PackageSymbol.DisplayName))
                {
                    this.Facade.PackageSymbol.DisplayName = arpElement.GetAttribute("DisplayName");
                }

                this.ProcessPackages(document, namespaceManager);

                this.ProcessRelatedBundles(document, namespaceManager);

                // TODO: Add payloads?
            }
        }

        private void ProcessPackages(XmlDocument document, XmlNamespaceManager namespaceManager)
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

            this.Facade.PackageSymbol.InstallSize = packageInstallSize;
        }

        private void ProcessRelatedBundles(XmlDocument document, XmlNamespaceManager namespaceManager)
        {
            foreach (XmlElement relatedBundleElement in document.SelectNodes("/burn:BurnManifest/burn:RelatedBundle", namespaceManager))
            {
                var id = relatedBundleElement.GetAttribute("Id");

                if (!Enum.TryParse(relatedBundleElement.GetAttribute("Action"), out RelatedBundleActionType action))
                {
                    // TODO: warning
                    continue;
                }

                this.Section.AddSymbol(new WixBundlePackageRelatedBundleSymbol
                {
                    PackageRef = this.Facade.PackageId,
                    BundleId = id,
                    Action = action,
                });
            }
        }
    }
}
