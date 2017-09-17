// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using Dtf = WixToolset.Dtf.WindowsInstaller;

    /// <summary>
    /// Initializes package state from the Msp contents.
    /// </summary>
    internal class ProcessMspPackageCommand : ICommand
    {
        private const string PatchMetadataFormat = "SELECT `Value` FROM `MsiPatchMetadata` WHERE `Property` = '{0}'";
        private static readonly Encoding XmlOutputEncoding = new UTF8Encoding(false);

        public RowDictionary<WixBundlePayloadRow> AuthoredPayloads { private get; set; }

        public PackageFacade Facade { private get; set; }

        public Table WixBundlePatchTargetCodeTable { private get; set; }

        /// <summary>
        /// Processes the Msp packages to add properties and payloads from the Msp packages.
        /// </summary>
        public void Execute()
        {
            WixBundlePayloadRow packagePayload = this.AuthoredPayloads.Get(this.Facade.Package.PackagePayload);

            string sourcePath = packagePayload.FullFileName;

            try
            {
                // Read data out of the msp database...
                using (Dtf.SummaryInfo sumInfo = new Dtf.SummaryInfo(sourcePath, false))
                {
                    this.Facade.MspPackage.PatchCode = sumInfo.RevisionNumber.Substring(0, 38);
                }

                using (Dtf.Database db = new Dtf.Database(sourcePath))
                {
                    if (String.IsNullOrEmpty(this.Facade.Package.DisplayName))
                    {
                        this.Facade.Package.DisplayName = ProcessMspPackageCommand.GetPatchMetadataProperty(db, "DisplayName");
                    }

                    if (String.IsNullOrEmpty(this.Facade.Package.Description))
                    {
                        this.Facade.Package.Description = ProcessMspPackageCommand.GetPatchMetadataProperty(db, "Description");
                    }

                    this.Facade.MspPackage.Manufacturer = ProcessMspPackageCommand.GetPatchMetadataProperty(db, "ManufacturerName");
                }

                this.ProcessPatchXml(packagePayload, sourcePath);
            }
            catch (Dtf.InstallerException e)
            {
                Messaging.Instance.OnMessage(WixErrors.UnableToReadPackageInformation(packagePayload.SourceLineNumbers, sourcePath, e.Message));
                return;
            }

            if (String.IsNullOrEmpty(this.Facade.Package.CacheId))
            {
                this.Facade.Package.CacheId = this.Facade.MspPackage.PatchCode;
            }
        }

        private void ProcessPatchXml(WixBundlePayloadRow packagePayload, string sourcePath)
        {
            HashSet<string> uniqueTargetCodes = new HashSet<string>();

            string patchXml = Dtf.Installer.ExtractPatchXmlData(sourcePath);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(patchXml);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("p", "http://www.microsoft.com/msi/patch_applicability.xsd");

            // Determine target ProductCodes and/or UpgradeCodes.
            foreach (XmlNode node in doc.SelectNodes("/p:MsiPatch/p:TargetProduct", nsmgr))
            {
                // If this patch targets a product code, this is the best case.
                XmlNode targetCodeElement = node.SelectSingleNode("p:TargetProductCode", nsmgr);
                WixBundlePatchTargetCodeAttributes attributes = WixBundlePatchTargetCodeAttributes.None;

                if (ProcessMspPackageCommand.TargetsCode(targetCodeElement))
                {
                    attributes = WixBundlePatchTargetCodeAttributes.TargetsProductCode;
                }
                else // maybe targets an upgrade code?
                {
                    targetCodeElement = node.SelectSingleNode("p:UpgradeCode", nsmgr);
                    if (ProcessMspPackageCommand.TargetsCode(targetCodeElement))
                    {
                        attributes = WixBundlePatchTargetCodeAttributes.TargetsUpgradeCode;
                    }
                    else // this patch targets an unknown number of products
                    {
                        this.Facade.MspPackage.Attributes |= WixBundleMspPackageAttributes.TargetUnspecified;
                    }
                }

                string targetCode = targetCodeElement.InnerText;

                if (uniqueTargetCodes.Add(targetCode))
                {
                    WixBundlePatchTargetCodeRow row = (WixBundlePatchTargetCodeRow)this.WixBundlePatchTargetCodeTable.CreateRow(packagePayload.SourceLineNumbers);
                    row.MspPackageId = packagePayload.Id;
                    row.TargetCode = targetCode;
                    row.Attributes = attributes;
                }
            }

            // Suppress patch sequence data for improved performance.
            XmlNode root = doc.DocumentElement;
            foreach (XmlNode node in root.SelectNodes("p:SequenceData", nsmgr))
            {
                root.RemoveChild(node);
            }

            // Save the XML as compact as possible.
            using (StringWriter writer = new StringWriter())
            {
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Encoding = ProcessMspPackageCommand.XmlOutputEncoding,
                    Indent = false,
                    NewLineChars = string.Empty,
                    NewLineHandling = NewLineHandling.Replace,
                };

                using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
                {
                    doc.WriteTo(xmlWriter);
                }

                this.Facade.MspPackage.PatchXml = writer.ToString();
            }
        }

        /// <summary>
        /// Queries a Windows Installer patch database for a Property value from the MsiPatchMetadata table.
        /// </summary>
        /// <param name="db">Database to query.</param>
        /// <param name="property">Property to examine.</param>
        /// <returns>String value for result or null if query doesn't match a single result.</returns>
        private static string GetPatchMetadataProperty(Dtf.Database db, string property)
        {
            try
            {
                return db.ExecuteScalar(PatchMetadataPropertyQuery(property)).ToString();
            }
            catch (Dtf.InstallerException)
            {
            }

            return null;
        }

        private static string PatchMetadataPropertyQuery(string property)
        {
            // quick sanity check that we'll be creating a valid query...
            // TODO: Are there any other special characters we should be looking for?
            Debug.Assert(!property.Contains("'"));

            return String.Format(CultureInfo.InvariantCulture, ProcessMspPackageCommand.PatchMetadataFormat, property);
        }

        private static bool TargetsCode(XmlNode node)
        {
            if (null != node)
            {
                XmlAttribute attr = node.Attributes["Validate"];
                return null != attr && "true".Equals(attr.Value);
            }

            return false;
        }
    }
}
