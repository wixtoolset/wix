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

    internal class CreateBootstrapperApplicationManifestCommand
    {
        private static readonly char[] ColonCharacter = new[] { ':' };

        public CreateBootstrapperApplicationManifestCommand(IntermediateSection section, WixBundleTuple bundleTuple, IEnumerable<PackageFacade> chainPackages, int lastUXPayloadIndex, Dictionary<string, WixBundlePayloadTuple> payloadTuples, string intermediateFolder)
        {
            this.Section = section;
            this.BundleTuple = bundleTuple;
            this.ChainPackages = chainPackages;
            this.LastUXPayloadIndex = lastUXPayloadIndex;
            this.Payloads = payloadTuples;
            this.IntermediateFolder = intermediateFolder;
        }

        private IntermediateSection Section { get; }

        private WixBundleTuple BundleTuple { get; }

        private IEnumerable<PackageFacade> ChainPackages { get; }

        private int LastUXPayloadIndex { get; }

        private Dictionary<string, WixBundlePayloadTuple> Payloads { get; }

        private string IntermediateFolder { get; }

        public WixBundlePayloadTuple BootstrapperApplicationManifestPayloadRow { get; private set; }

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

                this.WriteCustomBootstrapperApplicationData(writer);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return path;
        }

        private void WriteBundleInfo(XmlTextWriter writer)
        {
            writer.WriteStartElement("WixBundleProperties");

            writer.WriteAttributeString("DisplayName", this.BundleTuple.Name);
            writer.WriteAttributeString("LogPathVariable", this.BundleTuple.LogPathVariable);
            writer.WriteAttributeString("Compressed", this.BundleTuple.Compressed == true ? "yes" : "no");
            writer.WriteAttributeString("Id", this.BundleTuple.BundleId.ToUpperInvariant());
            writer.WriteAttributeString("UpgradeCode", this.BundleTuple.UpgradeCode);
            writer.WriteAttributeString("PerMachine", this.BundleTuple.PerMachine ? "yes" : "no");

            writer.WriteEndElement();
        }

        private void WritePackageInfo(XmlTextWriter writer)
        {
            foreach (var package in this.ChainPackages)
            {
                var packagePayload = this.Payloads[package.PackageTuple.PayloadRef];

                var size = package.PackageTuple.Size.ToString(CultureInfo.InvariantCulture);

                writer.WriteStartElement("WixPackageProperties");

                writer.WriteAttributeString("Package", package.PackageId);
                writer.WriteAttributeString("Vital", package.PackageTuple.Vital == true ? "yes" : "no");

                if (!String.IsNullOrEmpty(package.PackageTuple.DisplayName))
                {
                    writer.WriteAttributeString("DisplayName", package.PackageTuple.DisplayName);
                }

                if (!String.IsNullOrEmpty(package.PackageTuple.Description))
                {
                    writer.WriteAttributeString("Description", package.PackageTuple.Description);
                }

                writer.WriteAttributeString("DownloadSize", size);
                writer.WriteAttributeString("PackageSize", size);
                writer.WriteAttributeString("InstalledSize", package.PackageTuple.InstallSize?.ToString(CultureInfo.InvariantCulture) ?? size);
                writer.WriteAttributeString("PackageType", package.PackageTuple.Type.ToString());
                writer.WriteAttributeString("Permanent", package.PackageTuple.Permanent ? "yes" : "no");
                writer.WriteAttributeString("LogPathVariable", package.PackageTuple.LogPathVariable);
                writer.WriteAttributeString("RollbackLogPathVariable", package.PackageTuple.RollbackLogPathVariable);
                writer.WriteAttributeString("Compressed", packagePayload.Compressed == true ? "yes" : "no");

                if (package.SpecificPackageTuple is WixBundleMsiPackageTuple msiPackage)
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
                else if (package.SpecificPackageTuple is WixBundleMspPackageTuple mspPackage)
                {
                    if (!String.IsNullOrEmpty(mspPackage.PatchCode))
                    {
                        writer.WriteAttributeString("ProductCode", mspPackage.PatchCode);
                    }
                }

                if (!String.IsNullOrEmpty(package.PackageTuple.Version))
                {
                    writer.WriteAttributeString("Version", package.PackageTuple.Version);
                }

                if (!String.IsNullOrEmpty(package.PackageTuple.InstallCondition))
                {
                    writer.WriteAttributeString("InstallCondition", package.PackageTuple.InstallCondition);
                }

                switch (package.PackageTuple.Cache)
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
            var featureTuples = this.Section.Tuples.OfType<WixBundleMsiFeatureTuple>();

            foreach (var featureTuple in featureTuples)
            {
                writer.WriteStartElement("WixPackageFeatureInfo");

                writer.WriteAttributeString("Package", featureTuple.PackageRef);
                writer.WriteAttributeString("Feature", featureTuple.Name);
                writer.WriteAttributeString("Size", featureTuple.Size.ToString(CultureInfo.InvariantCulture));

                if (!String.IsNullOrEmpty(featureTuple.Parent))
                {
                    writer.WriteAttributeString("Parent", featureTuple.Parent);
                }

                if (!String.IsNullOrEmpty(featureTuple.Title))
                {
                    writer.WriteAttributeString("Title", featureTuple.Title);
                }

                if (!String.IsNullOrEmpty(featureTuple.Description))
                {
                    writer.WriteAttributeString("Description", featureTuple.Description);
                }

                writer.WriteAttributeString("Display", featureTuple.Display.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Level", featureTuple.Level.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Directory", featureTuple.Directory);
                writer.WriteAttributeString("Attributes", featureTuple.Attributes.ToString(CultureInfo.InvariantCulture));

                writer.WriteEndElement();
            }
        }

        private void WritePayloadInfo(XmlTextWriter writer)
        {
            var payloadTuples = this.Section.Tuples.OfType<WixBundlePayloadTuple>();

            foreach (var payloadTuple in payloadTuples)
            {
                writer.WriteStartElement("WixPayloadProperties");

                writer.WriteAttributeString("Payload", payloadTuple.Id.Id);

                if (!String.IsNullOrEmpty(payloadTuple.PackageRef))
                {
                    writer.WriteAttributeString("Package", payloadTuple.PackageRef);
                }

                if (!String.IsNullOrEmpty(payloadTuple.ContainerRef))
                {
                    writer.WriteAttributeString("Container", payloadTuple.ContainerRef);
                }

                writer.WriteAttributeString("Name", payloadTuple.Name);
                writer.WriteAttributeString("Size", payloadTuple.FileSize.Value.ToString(CultureInfo.InvariantCulture));

                if (!String.IsNullOrEmpty(payloadTuple.DownloadUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", payloadTuple.DownloadUrl);
                }

                writer.WriteAttributeString("LayoutOnly", payloadTuple.LayoutOnly ? "yes" : "no");

                writer.WriteEndElement();
            }
        }

        private void WriteCustomBootstrapperApplicationData(XmlTextWriter writer)
        {
            var dataTuplesGroupedByDefinitionName = this.Section.Tuples
                .Where(t => t.Definition.HasTag(BurnConstants.BootstrapperApplicationDataTupleDefinitionTag))
                .GroupBy(t => t.Definition);

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

                foreach (var row in group)
                {
                    writer.WriteStartElement(definition.Name);

                    foreach (var field in row.Fields)
                    {
                        if (!field.IsNull())
                        {
                            writer.WriteAttributeString(field.Definition.Name, field.AsString());
                        }
                    }

                    writer.WriteEndElement();
                }
            }

            var dataTablesById = this.Section.Tuples.OfType<WixCustomTableTuple>()
                                                .Where(t => t.Unreal && t.Id != null)
                                                .ToDictionary(t => t.Id.Id);
            var dataRowsByTable = this.Section.Tuples.OfType<WixCustomRowTuple>()
                                              .GroupBy(t => t.Table);
            foreach (var tableDataRows in dataRowsByTable)
            {
                var tableName = tableDataRows.Key;
                if (!dataTablesById.TryGetValue(tableName, out var tableTuple))
                {
                    // This should have been a linker error.
                    continue;
                }

                var columnNames = tableTuple.ColumnNames.Split('\t');

                // We simply assert that the table (and field) name is valid, because
                // this is up to the extension developer to get right. An author will
                // only affect the attribute value, and that will get properly escaped.
#if DEBUG
                Debug.Assert(Common.IsIdentifier(tableName));
                foreach (var columnName in columnNames)
                {
                    Debug.Assert(Common.IsIdentifier(columnName));
                }
#endif // DEBUG

                foreach (var rowTuple in tableDataRows)
                {
                    writer.WriteStartElement(tableName);

                    //var rowFields = rowTuple.FieldDataSeparated;
                    foreach (var field in rowTuple.FieldDataSeparated)
                    {
                        var splitField = field.Split(ColonCharacter, 2);
                        if (splitField.Length == 2)
                        {
                            writer.WriteAttributeString(splitField[0], splitField[1]);
                        }
                    }

                    writer.WriteEndElement();
                }
            }
        }

        private WixBundlePayloadTuple CreateBootstrapperApplicationManifestPayloadRow(string baManifestPath)
        {
            var generatedId = Common.GenerateIdentifier("ux", BurnCommon.BADataFileName);

            var tuple = this.Section.AddTuple(new WixBundlePayloadTuple(this.BundleTuple.SourceLineNumbers, new Identifier(AccessModifier.Private, generatedId))
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

            tuple.FileSize = (int)fileInfo.Length;

            tuple.Hash = BundleHashAlgorithm.Hash(fileInfo);

            return tuple;
        }
    }
}
