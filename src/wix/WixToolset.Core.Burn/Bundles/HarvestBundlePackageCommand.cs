// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class HarvestBundlePackageCommand
    {
        public HarvestBundlePackageCommand(IServiceProvider serviceProvider, IEnumerable<IBurnBackendBinderExtension> backendExtensions, string intermediateFolder, WixBundlePayloadSymbol payloadSymbol, WixBundleBundlePackagePayloadSymbol packagePayloadSymbol, Dictionary<string, WixBundlePayloadSymbol> packagePayloadsById)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.BackendHelper = serviceProvider.GetService<IBackendHelper>();
            this.BackendExtensions = backendExtensions;
            this.IntermediateFolder = intermediateFolder;

            this.PackagePayload = payloadSymbol;
            this.BundlePackagePayload = packagePayloadSymbol;
            this.PackagePayloadsById = packagePayloadsById;
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IEnumerable<IBurnBackendBinderExtension> BackendExtensions { get; }

        private string IntermediateFolder { get; }

        private WixBundlePayloadSymbol PackagePayload { get; }

        private WixBundleBundlePackagePayloadSymbol BundlePackagePayload { get; }

        private Dictionary<string, WixBundlePayloadSymbol> PackagePayloadsById { get; }

        public WixBundleHarvestedBundlePackageSymbol HarvestedBundlePackage { get; private set; }

        public WixBundleHarvestedDependencyProviderSymbol HarvestedDependencyProvider { get; private set; }

        public List<WixBundlePayloadSymbol> Payloads { get; } = new List<WixBundlePayloadSymbol>();

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

                    this.ProcessPayloads(document, namespaceManager, this.BundlePackagePayload.PayloadGeneration);

                    this.ProcessRelatedBundles(document, namespaceManager, sourcePath);
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

        private void ProcessPayloads(XmlDocument document, XmlNamespaceManager namespaceManager, BundlePackagePayloadGenerationType payloadGenerationType)
        {
            if (payloadGenerationType == BundlePackagePayloadGenerationType.None)
            {
                return;
            }

            var payloadNames = new HashSet<string>(this.PackagePayloadsById.Values.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

            var containersById = new Dictionary<string, ManifestContainer>();

            foreach (XmlElement containerElement in document.SelectNodes("/burn:BurnManifest/burn:Container", namespaceManager))
            {
                var container = new ManifestContainer();
                container.Attached = containerElement.GetAttribute("Attached") == "yes";
                container.DownloadUrl = containerElement.GetAttribute("DownloadUrl");
                container.FilePath = containerElement.GetAttribute("FilePath");
                container.Id = containerElement.GetAttribute("Id");
                containersById.Add(container.Id, container);

                if (container.Attached)
                {
                    continue;
                }

                switch (payloadGenerationType)
                {
                    case BundlePackagePayloadGenerationType.ExternalWithoutDownloadUrl:
                        if (!String.IsNullOrEmpty(container.DownloadUrl))
                        {
                            continue;
                        }
                        break;
                }

                // If we didn't find the Payload as an existing child of the package, we need to
                // add it.  We expect the file to exist on-disk in the same relative location as
                // the bundle expects to find it...
                container.IncludedAsPayload = true;
                var containerName = container.FilePath;
                var containerFullName = Path.Combine(Path.GetDirectoryName(this.PackagePayload.Name), containerName);

                if (!payloadNames.Contains(containerFullName))
                {
                    var generatedId = this.BackendHelper.GenerateIdentifier("hcp", this.PackagePayload.Id.Id, containerName);
                    var payloadSourceFile = this.ResolveRelatedFile(this.PackagePayload.SourceFile.Path, this.PackagePayload.UnresolvedSourceFile, containerName, "Container", this.PackagePayload.SourceLineNumbers);

                    this.Payloads.Add(new WixBundlePayloadSymbol(this.PackagePayload.SourceLineNumbers, new Identifier(AccessModifier.Section, generatedId))
                    {
                        Name = containerFullName,
                        SourceFile = new IntermediateFieldPathValue { Path = payloadSourceFile },
                        Compressed = this.PackagePayload.Compressed,
                        UnresolvedSourceFile = containerFullName,
                        ContainerRef = this.PackagePayload.ContainerRef,
                        DownloadUrl = this.PackagePayload.DownloadUrl,
                        Packaging = this.PackagePayload.Packaging,
                        ParentPackagePayloadRef = this.PackagePayload.Id.Id,
                    });
                }
            }

            foreach (XmlElement payloadElement in document.SelectNodes("/burn:BurnManifest/burn:Payload", namespaceManager))
            {
                var payload = new ManifestPayload();
                payload.Container = payloadElement.GetAttribute("Container");
                payload.DownloadUrl = payloadElement.GetAttribute("DownloadUrl");
                payload.FilePath = payloadElement.GetAttribute("FilePath");
                payload.Id = payloadElement.GetAttribute("Id");

                if (payload.Container == null || !containersById.TryGetValue(payload.Container, out var container))
                {
                    container = null;
                }

                if (container != null && container.IncludedAsPayload)
                {
                    // Don't include payload if it's in a container that's already included.
                    continue;
                }

                switch (payloadGenerationType)
                {
                    case BundlePackagePayloadGenerationType.ExternalWithoutDownloadUrl:
                        if (container != null || !String.IsNullOrEmpty(payload.DownloadUrl))
                        {
                            continue;
                        }
                        break;
                    case BundlePackagePayloadGenerationType.External:
                        if (container != null)
                        {
                            continue;
                        }
                        break;
                }

                // If we didn't find the Payload as an existing child of the package, we need to
                // add it.  We expect the file to exist on-disk in the same relative location as
                // the bundle expects to find it...
                var payloadName = payload.FilePath;
                var payloadFullName = Path.Combine(Path.GetDirectoryName(this.PackagePayload.Name), payloadName);

                if (!payloadNames.Contains(payloadFullName))
                {
                    var generatedId = this.BackendHelper.GenerateIdentifier("hpp", this.PackagePayload.Id.Id, payloadName);
                    var payloadSourceFile = this.ResolveRelatedFile(this.PackagePayload.SourceFile.Path, this.PackagePayload.UnresolvedSourceFile, payloadName, "Payload", this.PackagePayload.SourceLineNumbers);

                    this.Payloads.Add(new WixBundlePayloadSymbol(this.PackagePayload.SourceLineNumbers, new Identifier(AccessModifier.Section, generatedId))
                    {
                        Name = payloadFullName,
                        SourceFile = new IntermediateFieldPathValue { Path = payloadSourceFile },
                        Compressed = this.PackagePayload.Compressed,
                        UnresolvedSourceFile = payloadFullName,
                        ContainerRef = this.PackagePayload.ContainerRef,
                        DownloadUrl = this.PackagePayload.DownloadUrl,
                        Packaging = this.PackagePayload.Packaging,
                        ParentPackagePayloadRef = this.PackagePayload.Id.Id,
                    });
                }
            }
        }

        private string ResolveRelatedFile(string resolvedSource, string unresolvedSource, string relatedSource, string type, SourceLineNumber sourceLineNumbers)
        {
            var checkedPaths = new List<string>();

            foreach (var extension in this.BackendExtensions)
            {
                var resolved = extension.ResolveRelatedFile(unresolvedSource, relatedSource, type, sourceLineNumbers);

                if (resolved?.CheckedPaths != null)
                {
                    checkedPaths.AddRange(resolved.CheckedPaths);
                }

                if (!String.IsNullOrEmpty(resolved?.Path))
                {
                    return resolved?.Path;
                }
            }

            var resolvedPath = Path.Combine(Path.GetDirectoryName(resolvedSource), relatedSource);

            if (!File.Exists(resolvedPath))
            {
                checkedPaths.Add(resolvedPath);
                this.Messaging.Write(ErrorMessages.FileNotFound(sourceLineNumbers, resolvedPath, type, checkedPaths));
            }

            return resolvedPath;
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

        private class ManifestContainer
        {
            public bool Attached { get; set; }
            public string DownloadUrl { get; set; }
            public string FilePath { get; set; }
            public string Id { get; set; }

            public bool IncludedAsPayload { get; set; }
        }

        private class ManifestPayload
        {
            public string Container { get; set; }
            public string DownloadUrl { get; set; }
            public string FilePath { get; set; }
            public string Id { get; set; }
        }
    }
}
