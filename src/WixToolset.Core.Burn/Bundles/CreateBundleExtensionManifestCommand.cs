// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Tuples;

    internal class CreateBundleExtensionManifestCommand
    {
        public CreateBundleExtensionManifestCommand(IntermediateSection section, WixBundleTuple bundleTuple, IDictionary<string, IList<IntermediateTuple>> extensionSearchTuplesByExtensionId, int lastUXPayloadIndex, string intermediateFolder)
        {
            this.Section = section;
            this.BundleTuple = bundleTuple;
            this.ExtensionSearchTuplesByExtensionId = extensionSearchTuplesByExtensionId;
            this.LastUXPayloadIndex = lastUXPayloadIndex;
            this.IntermediateFolder = intermediateFolder;
        }

        private IntermediateSection Section { get; }

        private WixBundleTuple BundleTuple { get; }

        private IDictionary<string, IList<IntermediateTuple>> ExtensionSearchTuplesByExtensionId { get; }

        private int LastUXPayloadIndex { get; }

        private string IntermediateFolder { get; }

        public WixBundlePayloadTuple BundleExtensionManifestPayloadRow { get; private set; }

        public void Execute()
        {
            var bextManifestPath = this.CreateBundleExtensionManifest();

            this.BundleExtensionManifestPayloadRow = this.CreateBundleExtensionManifestPayloadRow(bextManifestPath);
        }

        private string CreateBundleExtensionManifest()
        {
            var path = Path.Combine(this.IntermediateFolder, "wix-bextdata.xml");

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var writer = new XmlTextWriter(path, Encoding.Unicode))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("BundleExtensionData", BurnCommon.BundleExtensionDataNamespace);

                foreach (var kvp in this.ExtensionSearchTuplesByExtensionId)
                {
                    this.WriteExtension(writer, kvp.Key, kvp.Value);
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return path;
        }

        private void WriteExtension(XmlTextWriter writer, string extensionId, IEnumerable<IntermediateTuple> tuples)
        {
            writer.WriteStartElement("BundleExtension");

            writer.WriteAttributeString("Id", extensionId);

            this.WriteBundleExtensionDataTuples(writer, tuples);

            writer.WriteEndElement();
        }

        private void WriteBundleExtensionDataTuples(XmlTextWriter writer, IEnumerable<IntermediateTuple> tuples)
        {
            var dataTuplesGroupedByDefinitionName = tuples.GroupBy(t => t.Definition);

            foreach (var group in dataTuplesGroupedByDefinitionName)
            {
                var definition = group.Key;

                // We simply assert that the table (and field) name is valid, because
                // this is up to the extension developer to get right. An author will
                // only affect the attribute value, and that will get properly escaped.
#if DEBUG
                Debug.Assert(Common.IsIdentifier(definition.Name));
                foreach (var fieldDef in definition.FieldDefinitions)
                {
                    Debug.Assert(Common.IsIdentifier(fieldDef.Name));
                }
#endif // DEBUG

                foreach (var tuple in group)
                {
                    writer.WriteStartElement(definition.Name);

                    if (tuple.Id != null)
                    {
                        writer.WriteAttributeString("Id", tuple.Id.Id);
                    }

                    foreach (var field in tuple.Fields)
                    {
                        if (!field.IsNull())
                        {
                            writer.WriteAttributeString(field.Definition.Name, field.AsString());
                        }
                    }

                    writer.WriteEndElement();
                }
            }
        }

        private WixBundlePayloadTuple CreateBundleExtensionManifestPayloadRow(string bextManifestPath)
        {
            var generatedId = Common.GenerateIdentifier("ux", BurnCommon.BundleExtensionDataFileName);

            var tuple = this.Section.AddTuple(new WixBundlePayloadTuple(this.BundleTuple.SourceLineNumbers, new Identifier(AccessModifier.Private, generatedId))
            {
                Name = BurnCommon.BundleExtensionDataFileName,
                SourceFile = new IntermediateFieldPathValue { Path = bextManifestPath },
                Compressed = true,
                UnresolvedSourceFile = bextManifestPath,
                ContainerRef = BurnConstants.BurnUXContainerName,
                EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, this.LastUXPayloadIndex),
                Packaging = PackagingType.Embedded,
            });

            var fileInfo = new FileInfo(bextManifestPath);

            tuple.FileSize = (int)fileInfo.Length;

            tuple.Hash = BundleHashAlgorithm.Hash(fileInfo);

            return tuple;
        }
    }
}
