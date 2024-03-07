// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;

    internal class CreateBootstrapperExtensionManifestCommand
    {
        public CreateBootstrapperExtensionManifestCommand(IntermediateSection section, WixBundleSymbol bundleSymbol, int lastUXPayloadIndex, string intermediateFolder, IInternalBurnBackendHelper internalBurnBackendHelper)
        {
            this.Section = section;
            this.BundleSymbol = bundleSymbol;
            this.LastUXPayloadIndex = lastUXPayloadIndex;
            this.IntermediateFolder = intermediateFolder;
            this.InternalBurnBackendHelper = internalBurnBackendHelper;
        }

        private IntermediateSection Section { get; }

        private WixBundleSymbol BundleSymbol { get; }

        private IInternalBurnBackendHelper InternalBurnBackendHelper { get; }

        private int LastUXPayloadIndex { get; }

        private string IntermediateFolder { get; }

        public WixBundlePayloadSymbol BootstrapperExtensionManifestPayloadRow { get; private set; }

        public string OutputPath { get; private set; }

        public void Execute()
        {
            this.OutputPath = this.CreateBootstrapperExtensionManifest();

            this.BootstrapperExtensionManifestPayloadRow = this.CreateBootstrapperExtensionManifestPayloadRow(this.OutputPath);
        }

        private string CreateBootstrapperExtensionManifest()
        {
            var path = Path.Combine(this.IntermediateFolder, "wix-bextdata.xml");

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var writer = new XmlTextWriter(path, Encoding.Unicode))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("BootstrapperExtensionData", BurnConstants.BootstrapperExtensionDataNamespace);

                this.InternalBurnBackendHelper.WriteBootstrapperExtensionData(writer);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return path;
        }

        private WixBundlePayloadSymbol CreateBootstrapperExtensionManifestPayloadRow(string bextManifestPath)
        {
            var generatedId = this.InternalBurnBackendHelper.GenerateIdentifier("ux", BurnCommon.BootstrapperExtensionDataFileName);

            this.Section.AddSymbol(new WixGroupSymbol(this.BundleSymbol.SourceLineNumbers)
            {
                ParentType = ComplexReferenceParentType.Container,
                ParentId = BurnConstants.BurnUXContainerName,
                ChildType = ComplexReferenceChildType.Payload,
                ChildId = generatedId
            });

            var symbol = this.Section.AddSymbol(new WixBundlePayloadSymbol(this.BundleSymbol.SourceLineNumbers, new Identifier(AccessModifier.Section, generatedId))
            {
                Name = BurnCommon.BootstrapperExtensionDataFileName,
                SourceFile = new IntermediateFieldPathValue { Path = bextManifestPath },
                Compressed = true,
                UnresolvedSourceFile = bextManifestPath,
                ContainerRef = BurnConstants.BurnUXContainerName,
                EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, this.LastUXPayloadIndex),
                Packaging = PackagingType.Embedded,
            });

            var fileInfo = new FileInfo(bextManifestPath);

            symbol.FileSize = (int)fileInfo.Length;

            symbol.Hash = BundleHashAlgorithm.Hash(fileInfo);

            return symbol;
        }
    }
}
