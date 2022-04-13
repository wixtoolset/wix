// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
            this.Section = section;

            this.ChainPackage = facade.PackageSymbol;
            this.MspPackage = (WixBundleMspPackageSymbol)facade.SpecificPackageSymbol;
            this.PackagePayload = payloadSymbols[this.ChainPackage.PayloadRef];
        }

        private IMessaging Messaging { get; }

        private WixBundlePackageSymbol ChainPackage { get; }

        private WixBundleMspPackageSymbol MspPackage { get; }

        private WixBundlePayloadSymbol PackagePayload { get; }

        private IntermediateSection Section { get; }

        /// <summary>
        /// Processes the Msp packages to add properties and payloads from the Msp packages.
        /// </summary>
        public void Execute()
        {
            var harvestedMspPackage = this.Section.Symbols.OfType<WixBundleHarvestedMspPackageSymbol>()
                                                          .Where(h => h.Id == this.ChainPackage.Id)
                                                          .SingleOrDefault();

            if (harvestedMspPackage == null)
            {
                harvestedMspPackage = this.HarvestPackage();

                if (harvestedMspPackage == null)
                {
                    return;
                }
            }

            this.MspPackage.PatchCode = harvestedMspPackage.PatchCode;
            this.MspPackage.Manufacturer = harvestedMspPackage.ManufacturerName;
            this.MspPackage.PatchXml = harvestedMspPackage.PatchXml;

            if (String.IsNullOrEmpty(this.ChainPackage.DisplayName))
            {
                this.ChainPackage.DisplayName = harvestedMspPackage.DisplayName;
            }

            if (String.IsNullOrEmpty(this.ChainPackage.Description))
            {
                this.ChainPackage.Description = harvestedMspPackage.Description;
            }

            if (String.IsNullOrEmpty(this.ChainPackage.CacheId))
            {
                this.ChainPackage.CacheId = this.MspPackage.PatchCode;
            }
        }

        private WixBundleHarvestedMspPackageSymbol HarvestPackage()
        {
            string patchCode;
            string displayName;
            string description;
            string manufacturerName;
            string patchXml;

            var sourcePath = this.PackagePayload.SourceFile.Path;

            try
            {
                using (var db = new Database(sourcePath, OpenDatabase.ReadOnly | OpenDatabase.OpenPatchFile))
                {
                    // Read data out of the msp database...
                    using (var sumInfo = new SummaryInformation(db))
                    {
                        var patchCodeValue = sumInfo.GetProperty(SummaryInformation.Patch.PatchCode);
                        patchCode = patchCodeValue.Substring(0, 38);
                    }

                    using (var view = db.OpenView(PatchMetadataQuery))
                    {
                        displayName = ProcessMspPackageCommand.GetPatchMetadataProperty(view, "DisplayName");
                        description = ProcessMspPackageCommand.GetPatchMetadataProperty(view, "Description");
                        manufacturerName = ProcessMspPackageCommand.GetPatchMetadataProperty(view, "ManufacturerName");
                    }
                }

                patchXml = ProcessMspPackageCommand.ProcessPatchXml(sourcePath, this.Section, this.PackagePayload.SourceLineNumbers, this.PackagePayload.Id);
            }
            catch (MsiException e)
            {
                this.Messaging.Write(ErrorMessages.UnableToReadPackageInformation(this.PackagePayload.SourceLineNumbers, sourcePath, e.Message));
                return null;
            }

            return this.Section.AddSymbol(new WixBundleHarvestedMspPackageSymbol(this.PackagePayload.SourceLineNumbers, this.ChainPackage.Id)
            {
                PatchCode = patchCode,
                DisplayName = displayName,
                Description = description,
                ManufacturerName = manufacturerName,
                PatchXml = patchXml,
            });
        }

        private static string ProcessPatchXml(string sourcePath, IntermediateSection section, SourceLineNumber sourceLineNumbers, Identifier id)
        {
            var uniqueTargetCodes = new Dictionary<string, WixBundlePatchTargetCodeSymbol>();

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
                WixBundlePatchTargetCodeType type;

                if (ProcessMspPackageCommand.TargetsCode(targetCodeElement))
                {
                    type = WixBundlePatchTargetCodeType.ProductCode;
                }
                else // maybe targets an upgrade code?
                {
                    targetCodeElement = node.SelectSingleNode("p:UpgradeCode", nsmgr);
                    if (ProcessMspPackageCommand.TargetsCode(targetCodeElement))
                    {
                        type = WixBundlePatchTargetCodeType.UpgradeCode;
                    }
                    else // this patch targets an unknown number of products
                    {
                        type = WixBundlePatchTargetCodeType.Unspecified;
                    }
                }

                var targetCode = targetCodeElement.InnerText;

                if (!uniqueTargetCodes.TryGetValue(targetCode, out var existing))
                {
                    var symbol = section.AddSymbol(new WixBundlePatchTargetCodeSymbol(sourceLineNumbers)
                    {
                        PackageRef = id.Id,
                        TargetCode = targetCode,
                        Attributes = 0,
                        Type = type,
                    });

                    uniqueTargetCodes.Add(targetCode, symbol);
                }
                else if (type == WixBundlePatchTargetCodeType.Unspecified)
                {
                    existing.Type = type;
                }
            }

            // Suppress patch sequence data for improved performance.
            var root = doc.DocumentElement;
            foreach (XmlNode node in root.SelectNodes("p:SequenceData", nsmgr))
            {
                root.RemoveChild(node);
            }

            string compactPatchXml;

            // Save the XML as compact as possible.
            using (var writer = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(writer, XmlSettings))
                {
                    doc.WriteTo(xmlWriter);
                }

                compactPatchXml = writer.ToString();
            }

            return compactPatchXml;
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

        private static bool TargetsCode(XmlNode node)
        {
            return "true" == node?.Attributes["Validate"]?.Value;
        }
    }
}
