// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;

    internal class CreateBootstrapperApplicationManifestCommand
    {
        public CreateBootstrapperApplicationManifestCommand(IntermediateSection section, WixBundleSymbol bundleSymbol, IEnumerable<WixBundleRollbackBoundarySymbol> boundaries, PackageFacades packageFacades, int lastUXPayloadIndex, Dictionary<string, WixBundlePayloadSymbol> payloadSymbols, Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>> packagesPayloads, string intermediateFolder, IInternalBurnBackendHelper internalBurnBackendHelper)
        {
            this.Section = section;
            this.BundleSymbol = bundleSymbol;
            this.RollbackBoundaries = boundaries;
            this.PackagesFacades = packageFacades;
            this.LastUXPayloadIndex = lastUXPayloadIndex;
            this.Payloads = payloadSymbols;
            this.PackagesPayloads = packagesPayloads;
            this.IntermediateFolder = intermediateFolder;
            this.InternalBurnBackendHelper = internalBurnBackendHelper;
        }

        private IntermediateSection Section { get; }

        private WixBundleSymbol BundleSymbol { get; }

        private IEnumerable<WixBundleRollbackBoundarySymbol> RollbackBoundaries { get; }

        private PackageFacades PackagesFacades { get; }

        private IInternalBurnBackendHelper InternalBurnBackendHelper { get; }

        private int LastUXPayloadIndex { get; }

        private Dictionary<string, WixBundlePayloadSymbol> Payloads { get; }

        private Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>> PackagesPayloads { get; }

        private string IntermediateFolder { get; }

        public WixBundlePayloadSymbol BootstrapperApplicationManifestPayloadRow { get; private set; }

        public string OutputPath { get; private set; }

        public void Execute()
        {
            this.OutputPath = this.CreateBootstrapperApplicationManifest();

            this.BootstrapperApplicationManifestPayloadRow = this.CreateBootstrapperApplicationManifestPayloadRow(this.OutputPath);
        }

        private string CreateBootstrapperApplicationManifest()
        {
            var path = Path.Combine(this.IntermediateFolder, "wix-badata.xml");

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var writer = new XmlTextWriter(path, Encoding.Unicode))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("BootstrapperApplicationData", BurnConstants.BootstrapperApplicationDataNamespace);

                this.WriteBundleInfo(writer);

                this.WriteRollbackBoundaryInfo(writer);

                this.WritePackageInfo(writer);

                this.WriteFeatureInfo(writer);

                this.WritePayloadInfo(writer);

                this.InternalBurnBackendHelper.WriteBootstrapperApplicationData(writer);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return path;
        }

        private void WriteBundleInfo(XmlTextWriter writer)
        {
            writer.WriteStartElement("WixBundleProperties");

            if (this.BundleSymbol.Id != null)
            {
                writer.WriteAttributeString("BundleId", this.BundleSymbol.Id.Id);
            }

            writer.WriteAttributeString("Code", this.BundleSymbol.BundleCode.ToUpperInvariant());
            writer.WriteAttributeString("DisplayName", this.BundleSymbol.Name);
            writer.WriteAttributeString("LogPathVariable", this.BundleSymbol.LogPathVariable);
            writer.WriteAttributeString("Compressed", this.BundleSymbol.Compressed == true ? "yes" : "no");
            writer.WriteAttributeString("UpgradeCode", this.BundleSymbol.UpgradeCode);
            writer.WriteAttributeString("PerMachine", this.BundleSymbol.PerMachine ? "yes" : "no");

            writer.WriteEndElement();
        }

        private void WriteRollbackBoundaryInfo(XmlTextWriter writer)
        {
            foreach (var rollbackBoundary in this.RollbackBoundaries)
            {
                writer.WriteStartElement("WixRollbackBoundary");
                writer.WriteAttributeString("Id", rollbackBoundary.Id.Id);
                writer.WriteAttributeString("Vital", rollbackBoundary.Vital ? "yes" : "no");
                writer.WriteAttributeString("Transaction", rollbackBoundary.Transaction ? "yes" : "no");

                if (!String.IsNullOrEmpty(rollbackBoundary.LogPathVariable))
                {
                    writer.WriteAttributeString("LogPathVariable", rollbackBoundary.LogPathVariable);
                }

                writer.WriteEndElement();
            }
        }

        private void WritePackageInfo(XmlTextWriter writer)
        {
            foreach (var package in this.PackagesFacades.OrderedValues)
            {
                if (!this.PackagesPayloads.TryGetValue(package.PackageId, out var payloads))
                {
                    continue;
                }

                var packagePayload = payloads[package.PackageSymbol.PayloadRef];

                var size = package.PackageSymbol.Size.ToString(CultureInfo.InvariantCulture);

                writer.WriteStartElement("WixPackageProperties");

                writer.WriteAttributeString("Package", package.PackageId);
                writer.WriteAttributeString("Vital", package.PackageSymbol.Vital ? "yes" : "no");

                if (!String.IsNullOrEmpty(package.PackageSymbol.DisplayName))
                {
                    writer.WriteAttributeString("DisplayName", package.PackageSymbol.DisplayName);
                }

                if (!String.IsNullOrEmpty(package.PackageSymbol.Description))
                {
                    writer.WriteAttributeString("Description", package.PackageSymbol.Description);
                }

                writer.WriteAttributeString("DownloadSize", size);
                writer.WriteAttributeString("PackageSize", size);
                writer.WriteAttributeString("InstalledSize", package.PackageSymbol.InstallSize?.ToString(CultureInfo.InvariantCulture) ?? size);
                writer.WriteAttributeString("PackageType", package.PackageSymbol.Type.ToString());
                writer.WriteAttributeString("Permanent", package.PackageSymbol.Permanent ? "yes" : "no");
                writer.WriteAttributeString("LogPathVariable", package.PackageSymbol.LogPathVariable);
                writer.WriteAttributeString("RollbackLogPathVariable", package.PackageSymbol.RollbackLogPathVariable);
                writer.WriteAttributeString("Compressed", packagePayload.Packaging == PackagingType.Embedded ? "yes" : "no");

                if (package.SpecificPackageSymbol is WixBundleMsiPackageSymbol msiPackage)
                {
                    if (!String.IsNullOrEmpty(msiPackage.ProductCode))
                    {
                        writer.WriteAttributeString("ProductCode", msiPackage.ProductCode);
                    }

                    if (!String.IsNullOrEmpty(msiPackage.UpgradeCode))
                    {
                        writer.WriteAttributeString("UpgradeCode", msiPackage.UpgradeCode);
                    }
                }
                else if (package.SpecificPackageSymbol is WixBundleMspPackageSymbol mspPackage)
                {
                    if (!String.IsNullOrEmpty(mspPackage.PatchCode))
                    {
                        writer.WriteAttributeString("ProductCode", mspPackage.PatchCode);
                    }
                }

                if (!String.IsNullOrEmpty(package.PackageSymbol.Version))
                {
                    writer.WriteAttributeString("Version", package.PackageSymbol.Version);
                }

                if (!String.IsNullOrEmpty(package.PackageSymbol.InstallCondition))
                {
                    writer.WriteAttributeString("InstallCondition", package.PackageSymbol.InstallCondition);
                }

                if (!String.IsNullOrEmpty(package.PackageSymbol.RepairCondition))
                {
                    writer.WriteAttributeString("RepairCondition", package.PackageSymbol.RepairCondition);
                }

                switch (package.PackageSymbol.Cache)
                {
                    case BundleCacheType.Remove:
                        writer.WriteAttributeString("Cache", "remove");
                        break;
                    case BundleCacheType.Keep:
                        writer.WriteAttributeString("Cache", "keep");
                        break;
                    case BundleCacheType.Force:
                        writer.WriteAttributeString("Cache", "force");
                        break;
                }

                writer.WriteEndElement();
            }
        }

        private void WriteFeatureInfo(XmlTextWriter writer)
        {
            var featureSymbols = this.Section.Symbols.OfType<WixBundleMsiFeatureSymbol>();

            foreach (var featureSymbol in featureSymbols)
            {
                if (!this.PackagesFacades.TryGetFacadesByPackagePayloadId(featureSymbol.PackagePayloadRef, out var facades))
                {
                    continue;
                }

                foreach (var facade in facades)
                {
                    if (!(facade.SpecificPackageSymbol is WixBundleMsiPackageSymbol msiPackage) || !msiPackage.EnableFeatureSelection)
                    {
                        continue;
                    }

                    writer.WriteStartElement("WixPackageFeatureInfo");

                    writer.WriteAttributeString("Package", facade.PackageId);
                    writer.WriteAttributeString("Feature", featureSymbol.Name);
                    writer.WriteAttributeString("Size", featureSymbol.Size.ToString(CultureInfo.InvariantCulture));

                    if (!String.IsNullOrEmpty(featureSymbol.Parent))
                    {
                        writer.WriteAttributeString("Parent", featureSymbol.Parent);
                    }

                    if (!String.IsNullOrEmpty(featureSymbol.Title))
                    {
                        writer.WriteAttributeString("Title", featureSymbol.Title);
                    }

                    if (!String.IsNullOrEmpty(featureSymbol.Description))
                    {
                        writer.WriteAttributeString("Description", featureSymbol.Description);
                    }

                    writer.WriteAttributeString("Display", featureSymbol.Display.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("Level", featureSymbol.Level.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("Directory", featureSymbol.Directory);
                    writer.WriteAttributeString("Attributes", featureSymbol.Attributes.ToString(CultureInfo.InvariantCulture));

                    writer.WriteEndElement();
                }
            }
        }

        private void WritePayloadInfo(XmlTextWriter writer)
        {
            foreach (var kvp in this.PackagesPayloads.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                var packageId = kvp.Key;
                var payloadsById = kvp.Value;

                foreach (var payloadSymbol in payloadsById.Values.OrderBy(p => p.Id.Id, StringComparer.Ordinal))
                {
                    this.WritePayloadInfo(writer, payloadSymbol, packageId);
                }
            }

            foreach (var payloadSymbol in this.Payloads.Values.Where(p => p.LayoutOnly).OrderBy(p => p.Id.Id, StringComparer.Ordinal))
            {
                this.WritePayloadInfo(writer, payloadSymbol, null);
            }
        }

        private void WritePayloadInfo(XmlTextWriter writer, WixBundlePayloadSymbol payloadSymbol, string packageId)
        {
            writer.WriteStartElement("WixPayloadProperties");

            if (!String.IsNullOrEmpty(packageId))
            {
                writer.WriteAttributeString("Package", packageId);
            }

            writer.WriteAttributeString("Payload", payloadSymbol.Id.Id);

            if (!String.IsNullOrEmpty(payloadSymbol.ContainerRef))
            {
                writer.WriteAttributeString("Container", payloadSymbol.ContainerRef);
            }

            writer.WriteAttributeString("Name", payloadSymbol.Name);
            writer.WriteAttributeString("Size", payloadSymbol.FileSize.Value.ToString(CultureInfo.InvariantCulture));

            if (!String.IsNullOrEmpty(payloadSymbol.DownloadUrl))
            {
                writer.WriteAttributeString("DownloadUrl", payloadSymbol.DownloadUrl);
            }

            writer.WriteEndElement();
        }

        private WixBundlePayloadSymbol CreateBootstrapperApplicationManifestPayloadRow(string baManifestPath)
        {
            var generatedId = this.InternalBurnBackendHelper.GenerateIdentifier("ux", BurnCommon.BADataFileName);

            var symbol = this.Section.AddSymbol(new WixBundlePayloadSymbol(this.BundleSymbol.SourceLineNumbers, new Identifier(AccessModifier.Section, generatedId))
            {
                Name = BurnCommon.BADataFileName,
                SourceFile = new IntermediateFieldPathValue { Path = baManifestPath },
                Compressed = true,
                UnresolvedSourceFile = baManifestPath,
                ContainerRef = BurnConstants.BurnUXContainerName,
                EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, this.LastUXPayloadIndex),
                Packaging = PackagingType.Embedded,
            });

            var fileInfo = new FileInfo(baManifestPath);

            symbol.FileSize = (int)fileInfo.Length;

            symbol.Hash = BundleHashAlgorithm.Hash(fileInfo);

            return symbol;
        }
    }
}
