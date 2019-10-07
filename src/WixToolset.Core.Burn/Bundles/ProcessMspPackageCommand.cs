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
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Services;
    using Dtf = WixToolset.Dtf.WindowsInstaller;

    /// <summary>
    /// Initializes package state from the Msp contents.
    /// </summary>
    internal class ProcessMspPackageCommand
    {
        private const string PatchMetadataFormat = "SELECT `Value` FROM `MsiPatchMetadata` WHERE `Property` = '{0}'";
        private static readonly Encoding XmlOutputEncoding = new UTF8Encoding(false);

        public ProcessMspPackageCommand(IMessaging messaging, IntermediateSection section, PackageFacade facade, Dictionary<string, WixBundlePayloadTuple> payloadTuples)
        {
            this.Messaging = messaging;

            this.AuthoredPayloads = payloadTuples;
            this.Section = section;
            this.Facade = facade;
        }

        public IMessaging Messaging { get; }

        public Dictionary<string, WixBundlePayloadTuple> AuthoredPayloads { private get; set; }

        public PackageFacade Facade { private get; set; }

        public IntermediateSection Section { get; }

        /// <summary>
        /// Processes the Msp packages to add properties and payloads from the Msp packages.
        /// </summary>
        public void Execute()
        {
            var packagePayload = this.AuthoredPayloads[this.Facade.PackageTuple.PayloadRef];

            var mspPackage = (WixBundleMspPackageTuple)this.Facade.SpecificPackageTuple;

            var sourcePath = packagePayload.SourceFile.Path;

            try
            {
                // Read data out of the msp database...
                using (var sumInfo = new Dtf.SummaryInfo(sourcePath, false))
                {
                    mspPackage.PatchCode = sumInfo.RevisionNumber.Substring(0, 38);
                }

                using (var db = new Dtf.Database(sourcePath))
                {
                    if (String.IsNullOrEmpty(this.Facade.PackageTuple.DisplayName))
                    {
                        this.Facade.PackageTuple.DisplayName = ProcessMspPackageCommand.GetPatchMetadataProperty(db, "DisplayName");
                    }

                    if (String.IsNullOrEmpty(this.Facade.PackageTuple.Description))
                    {
                        this.Facade.PackageTuple.Description = ProcessMspPackageCommand.GetPatchMetadataProperty(db, "Description");
                    }

                    mspPackage.Manufacturer = ProcessMspPackageCommand.GetPatchMetadataProperty(db, "ManufacturerName");
                }

                this.ProcessPatchXml(packagePayload, mspPackage, sourcePath);
            }
            catch (Dtf.InstallerException e)
            {
                this.Messaging.Write(ErrorMessages.UnableToReadPackageInformation(packagePayload.SourceLineNumbers, sourcePath, e.Message));
                return;
            }

            if (String.IsNullOrEmpty(this.Facade.PackageTuple.CacheId))
            {
                this.Facade.PackageTuple.CacheId = mspPackage.PatchCode;
            }
        }

        private void ProcessPatchXml(WixBundlePayloadTuple packagePayload, WixBundleMspPackageTuple mspPackage, string sourcePath)
        {
            var uniqueTargetCodes = new HashSet<string>();

            var patchXml = Dtf.Installer.ExtractPatchXmlData(sourcePath);

            var doc = new XmlDocument();
            doc.LoadXml(patchXml);

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("p", "http://www.microsoft.com/msi/patch_applicability.xsd");

            // Determine target ProductCodes and/or UpgradeCodes.
            foreach (XmlNode node in doc.SelectNodes("/p:MsiPatch/p:TargetProduct", nsmgr))
            {
                // If this patch targets a product code, this is the best case.
                var targetCodeElement = node.SelectSingleNode("p:TargetProductCode", nsmgr);
                var attributes = WixBundlePatchTargetCodeAttributes.None;

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
                        mspPackage.Attributes |= WixBundleMspPackageAttributes.TargetUnspecified;
                    }
                }

                var targetCode = targetCodeElement.InnerText;

                if (uniqueTargetCodes.Add(targetCode))
                {
                    var tuple = new WixBundlePatchTargetCodeTuple(packagePayload.SourceLineNumbers)
                    {
                        PackageRef = packagePayload.Id.Id,
                        TargetCode = targetCode,
                        Attributes = attributes
                    };

                    this.Section.Tuples.Add(tuple);
                }
            }

            // Suppress patch sequence data for improved performance.
            var root = doc.DocumentElement;
            foreach (XmlNode node in root.SelectNodes("p:SequenceData", nsmgr))
            {
                root.RemoveChild(node);
            }

            // Save the XML as compact as possible.
            using (var writer = new StringWriter())
            {
                var settings = new XmlWriterSettings()
                {
                    Encoding = ProcessMspPackageCommand.XmlOutputEncoding,
                    Indent = false,
                    NewLineChars = String.Empty,
                    NewLineHandling = NewLineHandling.Replace,
                };

                using (var xmlWriter = XmlWriter.Create(writer, settings))
                {
                    doc.WriteTo(xmlWriter);
                }

                mspPackage.PatchXml = writer.ToString();
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

        private static bool TargetsCode(XmlNode node) => "true" == node?.Attributes["Validate"]?.Value;
    }
}
