// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Initializes package state from the Msp contents.
    /// </summary>
    internal class ProcessMspPackageCommand
    {
        private const string PatchMetadataQuery = "SELECT `Value` FROM `MsiPatchMetadata` WHERE `Property` = ?";
        private static readonly XmlWriterSettings XmlSettings = new XmlWriterSettings()
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            NewLineChars = String.Empty,
            NewLineHandling = NewLineHandling.Replace,
        };

        public ProcessMspPackageCommand(IMessaging messaging, IntermediateSection section, PackageFacade facade, Dictionary<string, WixBundlePayloadSymbol> payloadSymbols)
        {
            this.Messaging = messaging;

            this.AuthoredPayloads = payloadSymbols;
            this.Section = section;
            this.Facade = facade;
        }

        public IMessaging Messaging { get; }

        public Dictionary<string, WixBundlePayloadSymbol> AuthoredPayloads { private get; set; }

        public PackageFacade Facade { private get; set; }

        public IntermediateSection Section { get; }

        /// <summary>
        /// Processes the Msp packages to add properties and payloads from the Msp packages.
        /// </summary>
        public void Execute()
        {
            var packagePayload = this.AuthoredPayloads[this.Facade.PackageSymbol.PayloadRef];

            var mspPackage = (WixBundleMspPackageSymbol)this.Facade.SpecificPackageSymbol;

            var sourcePath = packagePayload.SourceFile.Path;

            try
            {
                using (var db = new Database(sourcePath, OpenDatabase.ReadOnly | OpenDatabase.OpenPatchFile))
                {
                    // Read data out of the msp database...
                    using (var sumInfo = new SummaryInformation(db))
                    {
                        var patchCode = sumInfo.GetProperty(SummaryInformation.Patch.PatchCode);
                        mspPackage.PatchCode = patchCode.Substring(0, 38);
                    }

                    using (var view = db.OpenView(PatchMetadataQuery))
                    {
                        if (String.IsNullOrEmpty(this.Facade.PackageSymbol.DisplayName))
                        {
                            this.Facade.PackageSymbol.DisplayName = ProcessMspPackageCommand.GetPatchMetadataProperty(view, "DisplayName");
                        }

                        if (String.IsNullOrEmpty(this.Facade.PackageSymbol.Description))
                        {
                            this.Facade.PackageSymbol.Description = ProcessMspPackageCommand.GetPatchMetadataProperty(view, "Description");
                        }

                        mspPackage.Manufacturer = ProcessMspPackageCommand.GetPatchMetadataProperty(view, "ManufacturerName");
                    }
                }

                this.ProcessPatchXml(packagePayload, mspPackage, sourcePath);
            }
            catch (MsiException e)
            {
                this.Messaging.Write(ErrorMessages.UnableToReadPackageInformation(packagePayload.SourceLineNumbers, sourcePath, e.Message));
                return;
            }

            if (String.IsNullOrEmpty(this.Facade.PackageSymbol.CacheId))
            {
                this.Facade.PackageSymbol.CacheId = mspPackage.PatchCode;
            }
        }

        private void ProcessPatchXml(WixBundlePayloadSymbol packagePayload, WixBundleMspPackageSymbol mspPackage, string sourcePath)
        {
            var uniqueTargetCodes = new HashSet<string>();

            var patchXml = Installer.ExtractPatchXml(sourcePath);

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
                    this.Section.AddSymbol(new WixBundlePatchTargetCodeSymbol(packagePayload.SourceLineNumbers)
                    {
                        PackageRef = packagePayload.Id.Id,
                        TargetCode = targetCode,
                        Attributes = attributes
                    });
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
                using (var xmlWriter = XmlWriter.Create(writer, XmlSettings))
                {
                    doc.WriteTo(xmlWriter);
                }

                mspPackage.PatchXml = writer.ToString();
            }
        }

        private static string GetPatchMetadataProperty(View view, string property)
        {
            using (var queryRecord = new Record(1))
            {
                queryRecord[1] = property;

                view.Execute(queryRecord);

                using (var record = view.Fetch())
                {
                    return record?.GetString(1);
                }
            }
        }

        private static bool TargetsCode(XmlNode node) => "true" == node?.Attributes["Validate"]?.Value;
    }
}
