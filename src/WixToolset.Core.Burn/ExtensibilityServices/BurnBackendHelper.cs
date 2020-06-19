// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Data;

    internal class BurnBackendHelper : IInternalBurnBackendHelper
    {
        public static readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
        public static readonly XmlWriterSettings WriterSettings = new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment };

        private ManifestData BootstrapperApplicationManifestData { get; } = new ManifestData();

        private Dictionary<string, ManifestData> BundleExtensionDataById { get; } = new Dictionary<string, ManifestData>();

        public void AddBootstrapperApplicationData(string xml)
        {
            this.BootstrapperApplicationManifestData.AddXml(xml);
        }

        public void AddBootstrapperApplicationData(IntermediateTuple tuple, bool tupleIdIsIdAttribute = false)
        {
            this.BootstrapperApplicationManifestData.AddTuple(tuple, tupleIdIsIdAttribute, BurnCommon.BADataNamespace);
        }

        public void AddBundleExtensionData(string extensionId, string xml)
        {
            var manifestData = this.GetBundleExtensionManifestData(extensionId);
            manifestData.AddXml(xml);
        }

        public void AddBundleExtensionData(string extensionId, IntermediateTuple tuple, bool tupleIdIsIdAttribute = false)
        {
            var manifestData = this.GetBundleExtensionManifestData(extensionId);
            manifestData.AddTuple(tuple, tupleIdIsIdAttribute, BurnCommon.BundleExtensionDataNamespace);
        }

        public void WriteBootstrapperApplicationData(XmlWriter writer)
        {
            this.BootstrapperApplicationManifestData.Write(writer);
        }

        public void WriteBundleExtensionData(XmlWriter writer)
        {
            foreach (var kvp in this.BundleExtensionDataById)
            {
                this.WriteExtension(writer, kvp.Key, kvp.Value);
            }
        }

        private ManifestData GetBundleExtensionManifestData(string extensionId)
        {
            if (!Common.IsIdentifier(extensionId))
            {
                throw new ArgumentException($"'{extensionId}' is not a valid extensionId");
            }

            if (!this.BundleExtensionDataById.TryGetValue(extensionId, out var manifestData))
            {
                manifestData = new ManifestData();
                this.BundleExtensionDataById.Add(extensionId, manifestData);
            }

            return manifestData;
        }

        private void WriteExtension(XmlWriter writer, string extensionId, ManifestData manifestData)
        {
            writer.WriteStartElement("BundleExtension");

            writer.WriteAttributeString("Id", extensionId);

            manifestData.Write(writer);

            writer.WriteEndElement();
        }

        private class ManifestData
        {
            public ManifestData()
            {
                this.Builder = new StringBuilder();
            }

            private StringBuilder Builder { get; }

            public void AddTuple(IntermediateTuple tuple, bool tupleIdIsIdAttribute, string ns)
            {
                // There might be a more efficient way to do this,
                // but this is an easy way to ensure we're creating valid XML.
                var sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb, WriterSettings))
                {
                    writer.WriteStartElement(tuple.Definition.Name, ns);

                    if (tupleIdIsIdAttribute && tuple.Id != null)
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

                this.AddXml(sb.ToString());
            }

            public void AddXml(string xml)
            {
                // There might be a more efficient way to do this,
                // but this is an easy way to ensure we're given valid XML.
                var sb = new StringBuilder();
                using (var xmlWriter = XmlWriter.Create(sb, WriterSettings))
                {
                    AddManifestDataFromString(xmlWriter, xml);
                }
                this.Builder.Append(sb.ToString());
            }

            public void Write(XmlWriter writer)
            {
                AddManifestDataFromString(writer, this.Builder.ToString());
            }

            private static void AddManifestDataFromString(XmlWriter xmlWriter, string xml)
            {
                using (var stringReader = new StringReader(xml))
                using (var xmlReader = XmlReader.Create(stringReader, ReaderSettings))
                {
                    while (xmlReader.MoveToContent() != XmlNodeType.None)
                    {
                        xmlWriter.WriteNode(xmlReader, false);
                    }
                }
            }
        }
    }
}
