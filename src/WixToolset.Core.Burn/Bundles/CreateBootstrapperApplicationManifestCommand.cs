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
        public CreateBootstrapperApplicationManifestCommand(IntermediateSection section, WixBundleSymbol bundleSymbol, IEnumerable<PackageFacade> chainPackages, int lastUXPayloadIndex, Dictionary<string, WixBundlePayloadSymbol> payloadSymbols, string intermediateFolder, IInternalBurnBackendHelper internalBurnBackendHelper)
        {
            this.Section = section;
            this.BundleSymbol = bundleSymbol;
            this.ChainPackages = chainPackages;
            this.LastUXPayloadIndex = lastUXPayloadIndex;
            this.Payloads = payloadSymbols;
            this.IntermediateFolder = intermediateFolder;
            this.InternalBurnBackendHelper = internalBurnBackendHelper;
        }

        private IntermediateSection Section { get; }

        private WixBundleSymbol BundleSymbol { get; }

        private IEnumerable<PackageFacade> ChainPackages { get; }

        private IInternalBurnBackendHelper InternalBurnBackendHelper { get; }

        private int LastUXPayloadIndex { get; }

        private Dictionary<string, WixBundlePayloadSymbol> Payloads { get; }

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
                writer.WriteStartElement("BootstrapperApplicationData", BurnCommon.BADataNamespace);

                this.WriteBundleInfo(writer);

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

            writer.WriteAttributeString("DisplayName", this.BundleSymbol.Name);
            writer.WriteAttributeString("LogPathVariable", this.BundleSymbol.LogPathVariable);
            writer.WriteAttributeString("Compressed", this.BundleSymbol.Compressed == true ? "yes" : "no");
            writer.WriteAttributeString("Id", this.BundleSymbol.BundleId.ToUpperInvariant());
            writer.WriteAttributeString("UpgradeCode", this.BundleSymbol.UpgradeCode);
            writer.WriteAttributeString("PerMachine", this.BundleSymbol.PerMachine ? "yes" : "no");

            writer.WriteEndElement();
        }

        private void WritePackageInfo(XmlTextWriter writer)
        {
            foreach (var package in this.ChainPackages)
            {
                var packagePayload = this.Payloads[package.PackageSymbol.PayloadRef];

                var size = package.PackageSymbol.Size.ToString(CultureInfo.InvariantCulture);

                writer.WriteStartElement("WixPackageProperties");

                writer.WriteAttributeString("Package", package.PackageId);
                writer.WriteAttributeString("Vital", package.PackageSymbol.Vital == true ? "yes" : "no");

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
                writer.WriteAttributeString("Compressed", packagePayload.Compressed == true ? "yes" : "no");

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

                switch (package.PackageSymbol.Cache)
                {
                    case YesNoAlwaysType.No:
                        writer.WriteAttributeString("Cache", "no");
                        break;
                    case YesNoAlwaysType.Yes:
                        writer.WriteAttributeString("Cache", "yes");
                        break;
                    case YesNoAlwaysType.Always:
                        writer.WriteAttributeString("Cache", "always");
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
                writer.WriteStartElement("WixPackageFeatureInfo");

                writer.WriteAttributeString("Package", featureSymbol.PackageRef);
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

        private void WritePayloadInfo(XmlTextWriter writer)
        {
            var payloadSymbols = this.Section.Symbols.OfType<WixBundlePayloadSymbol>();

            foreach (var payloadSymbol in payloadSymbols)
            {
                writer.WriteStartElement("WixPayloadProperties");

                writer.WriteAttributeString("Payload", payloadSymbol.Id.Id);

                if (!String.IsNullOrEmpty(payloadSymbol.PackageRef))
                {
                    writer.WriteAttributeString("Package", payloadSymbol.PackageRef);
                }

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

                writer.WriteAttributeString("LayoutOnly", payloadSymbol.LayoutOnly ? "yes" : "no");

                writer.WriteEndElement();
            }
        }

        private WixBundlePayloadSymbol CreateBootstrapperApplicationManifestPayloadRow(string baManifestPath)
        {
            var generatedId = Common.GenerateIdentifier("ux", BurnCommon.BADataFileName);

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
