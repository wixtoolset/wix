// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;

    internal class CreateBootstrapperApplicationManifestCommand
    {
#if TODO
        public WixBundleRow BundleRow { private get; set; }

        public IEnumerable<PackageFacade> ChainPackages { private get; set; }

        public int LastUXPayloadIndex { private get; set; }

        public IEnumerable<WixBundleMsiFeatureRow> MsiFeatures { private get; set; }

        public Output Output { private get; set; }

        public RowDictionary<WixBundlePayloadRow> Payloads { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public string TempFilesLocation { private get; set; }

        public WixBundlePayloadRow BootstrapperApplicationManifestPayloadRow { get; private set; }

        public void Execute()
        {
            this.GenerateBAManifestBundleTables();

            this.GenerateBAManifestMsiFeatureTables();

            this.GenerateBAManifestPackageTables();

            this.GenerateBAManifestPayloadTables();

            string baManifestPath = Path.Combine(this.TempFilesLocation, "wix-badata.xml");

            this.CreateBootstrapperApplicationManifest(baManifestPath);

            this.BootstrapperApplicationManifestPayloadRow = this.CreateBootstrapperApplicationManifestPayloadRow(baManifestPath);
        }

        private void GenerateBAManifestBundleTables()
        {
            Table wixBundlePropertiesTable = this.Output.EnsureTable(this.TableDefinitions["WixBundleProperties"]);

            Row row = wixBundlePropertiesTable.CreateRow(this.BundleRow.SourceLineNumbers);
            row[0] = this.BundleRow.Name;
            row[1] = this.BundleRow.LogPathVariable;
            row[2] = (YesNoDefaultType.Yes == this.BundleRow.Compressed) ? "yes" : "no";
            row[3] = this.BundleRow.BundleId.ToString("B");
            row[4] = this.BundleRow.UpgradeCode;
            row[5] = this.BundleRow.PerMachine ? "yes" : "no";
        }

        private void GenerateBAManifestPackageTables()
        {
            Table wixPackagePropertiesTable = this.Output.EnsureTable(this.TableDefinitions["WixPackageProperties"]);

            foreach (PackageFacade package in this.ChainPackages)
            {
                WixBundlePayloadRow packagePayload = this.Payloads[package.Package.PackagePayload];

                Row row = wixPackagePropertiesTable.CreateRow(package.Package.SourceLineNumbers);
                row[0] = package.Package.WixChainItemId;
                row[1] = (YesNoType.Yes == package.Package.Vital) ? "yes" : "no";
                row[2] = package.Package.DisplayName;
                row[3] = package.Package.Description;
                row[4] = package.Package.Size.ToString(CultureInfo.InvariantCulture); // TODO: DownloadSize (compressed) (what does this mean when it's embedded?)
                row[5] = package.Package.Size.ToString(CultureInfo.InvariantCulture); // Package.Size (uncompressed)
                row[6] = package.Package.InstallSize.Value.ToString(CultureInfo.InvariantCulture); // InstallSize (required disk space)
                row[7] = package.Package.Type.ToString();
                row[8] = package.Package.Permanent ? "yes" : "no";
                row[9] = package.Package.LogPathVariable;
                row[10] = package.Package.RollbackLogPathVariable;
                row[11] = (PackagingType.Embedded == packagePayload.Packaging) ? "yes" : "no";

                if (WixBundlePackageType.Msi == package.Package.Type)
                {
                    row[12] = package.MsiPackage.DisplayInternalUI ? "yes" : "no";

                    if (!String.IsNullOrEmpty(package.MsiPackage.ProductCode))
                    {
                        row[13] = package.MsiPackage.ProductCode;
                    }

                    if (!String.IsNullOrEmpty(package.MsiPackage.UpgradeCode))
                    {
                        row[14] = package.MsiPackage.UpgradeCode;
                    }
                }
                else if (WixBundlePackageType.Msp == package.Package.Type)
                {
                    row[12] = package.MspPackage.DisplayInternalUI ? "yes" : "no";

                    if (!String.IsNullOrEmpty(package.MspPackage.PatchCode))
                    {
                        row[13] = package.MspPackage.PatchCode;
                    }
                }

                if (!String.IsNullOrEmpty(package.Package.Version))
                {
                    row[15] = package.Package.Version;
                }

                if (!String.IsNullOrEmpty(package.Package.InstallCondition))
                {
                    row[16] = package.Package.InstallCondition;
                }

                switch (package.Package.Cache)
                {
                    case YesNoAlwaysType.No:
                        row[17] = "no";
                        break;
                    case YesNoAlwaysType.Yes:
                        row[17] = "yes";
                        break;
                    case YesNoAlwaysType.Always:
                        row[17] = "always";
                        break;
                }
            }
        }

        private void GenerateBAManifestMsiFeatureTables()
        {
            Table wixPackageFeatureInfoTable = this.Output.EnsureTable(this.TableDefinitions["WixPackageFeatureInfo"]);

            foreach (WixBundleMsiFeatureRow feature in this.MsiFeatures)
            {
                Row row = wixPackageFeatureInfoTable.CreateRow(feature.SourceLineNumbers);
                row[0] = feature.ChainPackageId;
                row[1] = feature.Name;
                row[2] = Convert.ToString(feature.Size, CultureInfo.InvariantCulture);
                row[3] = feature.Parent;
                row[4] = feature.Title;
                row[5] = feature.Description;
                row[6] = Convert.ToString(feature.Display, CultureInfo.InvariantCulture);
                row[7] = Convert.ToString(feature.Level, CultureInfo.InvariantCulture);
                row[8] = feature.Directory;
                row[9] = Convert.ToString(feature.Attributes, CultureInfo.InvariantCulture);
            }

        }

        private void GenerateBAManifestPayloadTables()
        {
            Table wixPayloadPropertiesTable = this.Output.EnsureTable(this.TableDefinitions["WixPayloadProperties"]);

            foreach (WixBundlePayloadRow payload in this.Payloads.Values)
            {
                WixPayloadPropertiesRow row = (WixPayloadPropertiesRow)wixPayloadPropertiesTable.CreateRow(payload.SourceLineNumbers);
                row.Id = payload.Id;
                row.Package = payload.Package;
                row.Container = payload.Container;
                row.Name = payload.Name;
                row.Size = payload.FileSize.ToString();
                row.DownloadUrl = payload.DownloadUrl;
                row.LayoutOnly = payload.LayoutOnly ? "yes" : "no";
            }
        }

        private void CreateBootstrapperApplicationManifest(string path)
        {
            using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.Unicode))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("BootstrapperApplicationData", "http://wixtoolset.org/schemas/v4/2010/BootstrapperApplicationData");

                foreach (Table table in this.Output.Tables)
                {
                    if (table.Definition.BootstrapperApplicationData)
                    {
                        // We simply assert that the table (and field) name is valid, because
                        // this is up to the extension developer to get right. An author will
                        // only affect the attribute value, and that will get properly escaped.
#if DEBUG
                        Debug.Assert(Common.IsIdentifier(table.Name));
                        foreach (ColumnDefinition column in table.Definition.Columns)
                        {
                            Debug.Assert(Common.IsIdentifier(column.Name));
                        }
#endif // DEBUG

                        foreach (Row row in table.Rows)
                        {
                            writer.WriteStartElement(table.Name);

                            foreach (Field field in row.Fields)
                            {
                                if (null != field.Data)
                                {
                                    writer.WriteAttributeString(field.Column.Name, field.Data.ToString());
                                }
                            }

                            writer.WriteEndElement();
                        }
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private WixBundlePayloadRow CreateBootstrapperApplicationManifestPayloadRow(string baManifestPath)
        {
            Table payloadTable = this.Output.EnsureTable(this.TableDefinitions["WixBundlePayload"]);
            WixBundlePayloadRow row = (WixBundlePayloadRow)payloadTable.CreateRow(this.BundleRow.SourceLineNumbers);
            row.Id = Common.GenerateIdentifier("ux", "BootstrapperApplicationData.xml");
            row.Name = "BootstrapperApplicationData.xml";
            row.SourceFile = baManifestPath;
            row.Compressed = YesNoDefaultType.Yes;
            row.UnresolvedSourceFile = baManifestPath;
            row.Container = Compiler.BurnUXContainerId;
            row.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, this.LastUXPayloadIndex);
            row.Packaging = PackagingType.Embedded;

            FileInfo fileInfo = new FileInfo(row.SourceFile);

            row.FileSize = (int)fileInfo.Length;

            row.Hash = Common.GetFileHash(fileInfo.FullName);

            return row;
        }
#endif
    }
}
