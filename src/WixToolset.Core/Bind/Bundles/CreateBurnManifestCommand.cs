// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    internal class CreateBurnManifestCommand : ICommand
    {
        public IEnumerable<IBinderFileManager> FileManagers { private get; set; }

        public Output Output { private get; set; }

        public string ExecutableName { private get; set; }

        public WixBundleRow BundleInfo { private get; set; }

        public WixChainRow Chain { private get; set; }

        public string OutputPath { private get; set; }

        public IEnumerable<WixBundleRollbackBoundaryRow> RollbackBoundaries { private get; set; }

        public IEnumerable<PackageFacade> OrderedPackages { private get; set; }

        public IEnumerable<WixSearchInfo> OrderedSearches { private get; set; }

        public Dictionary<string, WixBundlePayloadRow> Payloads { private get; set; }

        public Dictionary<string, WixBundleContainerRow> Containers { private get; set; }

        public IEnumerable<WixBundlePayloadRow> UXContainerPayloads { private get; set; }

        public IEnumerable<WixBundleCatalogRow> Catalogs { private get; set; }

        public void Execute()
        {
            using (XmlTextWriter writer = new XmlTextWriter(this.OutputPath, Encoding.UTF8))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("BurnManifest", BurnCommon.BurnNamespace);

                // Write the condition, if there is one
                if (null != this.BundleInfo.Condition)
                {
                    writer.WriteElementString("Condition", this.BundleInfo.Condition);
                }

                // Write the log element if default logging wasn't disabled.
                if (!String.IsNullOrEmpty(this.BundleInfo.LogPrefix))
                {
                    writer.WriteStartElement("Log");
                    if (!String.IsNullOrEmpty(this.BundleInfo.LogPathVariable))
                    {
                        writer.WriteAttributeString("PathVariable", this.BundleInfo.LogPathVariable);
                    }
                    writer.WriteAttributeString("Prefix", this.BundleInfo.LogPrefix);
                    writer.WriteAttributeString("Extension", this.BundleInfo.LogExtension);
                    writer.WriteEndElement();
                }


                // Get update if specified.
                WixBundleUpdateRow updateRow = this.Output.Tables["WixBundleUpdate"].RowsAs<WixBundleUpdateRow>().FirstOrDefault();

                if (null != updateRow)
                {
                    writer.WriteStartElement("Update");
                    writer.WriteAttributeString("Location", updateRow.Location);
                    writer.WriteEndElement(); // </Update>
                }

                // Write the RelatedBundle elements

                // For the related bundles with duplicated identifiers the second instance is ignored (i.e. the Duplicates
                // enumeration in the index row list is not used).
                RowIndexedList<WixRelatedBundleRow> relatedBundles = new RowIndexedList<WixRelatedBundleRow>(this.Output.Tables["WixRelatedBundle"]);

                foreach (WixRelatedBundleRow relatedBundle in relatedBundles)
                {
                    writer.WriteStartElement("RelatedBundle");
                    writer.WriteAttributeString("Id", relatedBundle.Id);
                    writer.WriteAttributeString("Action", Convert.ToString(relatedBundle.Action, CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }

                // Write the variables
                IEnumerable<WixBundleVariableRow> variables = this.Output.Tables["WixBundleVariable"].RowsAs<WixBundleVariableRow>();

                foreach (WixBundleVariableRow variable in variables)
                {
                    writer.WriteStartElement("Variable");
                    writer.WriteAttributeString("Id", variable.Id);
                    if (null != variable.Type)
                    {
                        writer.WriteAttributeString("Value", variable.Value);
                        writer.WriteAttributeString("Type", variable.Type);
                    }
                    writer.WriteAttributeString("Hidden", variable.Hidden ? "yes" : "no");
                    writer.WriteAttributeString("Persisted", variable.Persisted ? "yes" : "no");
                    writer.WriteEndElement();
                }

                // Write the searches
                foreach (WixSearchInfo searchinfo in this.OrderedSearches)
                {
                    searchinfo.WriteXml(writer);
                }

                // write the UX element
                writer.WriteStartElement("UX");
                if (!String.IsNullOrEmpty(this.BundleInfo.SplashScreenBitmapPath))
                {
                    writer.WriteAttributeString("SplashScreen", "yes");
                }

                // write the UX allPayloads...
                foreach (WixBundlePayloadRow payload in this.UXContainerPayloads)
                {
                    writer.WriteStartElement("Payload");
                    this.WriteBurnManifestPayloadAttributes(writer, payload, true, this.Payloads);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // </UX>

                // write the catalog elements
                if (this.Catalogs.Any())
                {
                    foreach (WixBundleCatalogRow catalog in this.Catalogs)
                    {
                        writer.WriteStartElement("Catalog");
                        writer.WriteAttributeString("Id", catalog.Id);
                        writer.WriteAttributeString("Payload", catalog.Payload);
                        writer.WriteEndElement();
                    }
                }

                foreach (WixBundleContainerRow container in this.Containers.Values)
                {
                    if (!String.IsNullOrEmpty(container.WorkingPath) && Compiler.BurnUXContainerId != container.Id)
                    {
                        writer.WriteStartElement("Container");
                        this.WriteBurnManifestContainerAttributes(writer, this.ExecutableName, container);
                        writer.WriteEndElement();
                    }
                }

                foreach (WixBundlePayloadRow payload in this.Payloads.Values)
                {
                    if (PackagingType.Embedded == payload.Packaging && Compiler.BurnUXContainerId != payload.Container)
                    {
                        writer.WriteStartElement("Payload");
                        this.WriteBurnManifestPayloadAttributes(writer, payload, true, this.Payloads);
                        writer.WriteEndElement();
                    }
                    else if (PackagingType.External == payload.Packaging)
                    {
                        writer.WriteStartElement("Payload");
                        this.WriteBurnManifestPayloadAttributes(writer, payload, false, this.Payloads);
                        writer.WriteEndElement();
                    }
                }

                foreach (WixBundleRollbackBoundaryRow rollbackBoundary in this.RollbackBoundaries)
                {
                    writer.WriteStartElement("RollbackBoundary");
                    writer.WriteAttributeString("Id", rollbackBoundary.ChainPackageId);
                    writer.WriteAttributeString("Vital", YesNoType.Yes == rollbackBoundary.Vital ? "yes" : "no");
                    writer.WriteAttributeString("Transaction", YesNoType.Yes == rollbackBoundary.Transaction ? "yes" : "no");
                    writer.WriteEndElement();
                }

                // Write the registration information...
                writer.WriteStartElement("Registration");

                writer.WriteAttributeString("Id", this.BundleInfo.BundleId.ToString("B"));
                writer.WriteAttributeString("ExecutableName", this.ExecutableName);
                writer.WriteAttributeString("PerMachine", this.BundleInfo.PerMachine ? "yes" : "no");
                writer.WriteAttributeString("Tag", this.BundleInfo.Tag);
                writer.WriteAttributeString("Version", this.BundleInfo.Version);
                writer.WriteAttributeString("ProviderKey", this.BundleInfo.ProviderKey);

                writer.WriteStartElement("Arp");
                writer.WriteAttributeString("Register", (0 < this.BundleInfo.DisableModify && this.BundleInfo.DisableRemove) ? "no" : "yes"); // do not register if disabled modify and remove.
                writer.WriteAttributeString("DisplayName", this.BundleInfo.Name);
                writer.WriteAttributeString("DisplayVersion", this.BundleInfo.Version);

                if (!String.IsNullOrEmpty(this.BundleInfo.Publisher))
                {
                    writer.WriteAttributeString("Publisher", this.BundleInfo.Publisher);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.HelpLink))
                {
                    writer.WriteAttributeString("HelpLink", this.BundleInfo.HelpLink);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.HelpTelephone))
                {
                    writer.WriteAttributeString("HelpTelephone", this.BundleInfo.HelpTelephone);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.AboutUrl))
                {
                    writer.WriteAttributeString("AboutUrl", this.BundleInfo.AboutUrl);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.UpdateUrl))
                {
                    writer.WriteAttributeString("UpdateUrl", this.BundleInfo.UpdateUrl);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.ParentName))
                {
                    writer.WriteAttributeString("ParentDisplayName", this.BundleInfo.ParentName);
                }

                if (1 == this.BundleInfo.DisableModify)
                {
                    writer.WriteAttributeString("DisableModify", "yes");
                }
                else if (2 == this.BundleInfo.DisableModify)
                {
                    writer.WriteAttributeString("DisableModify", "button");
                }

                if (this.BundleInfo.DisableRemove)
                {
                    writer.WriteAttributeString("DisableRemove", "yes");
                }
                writer.WriteEndElement(); // </Arp>

                // Get update registration if specified.
                WixUpdateRegistrationRow updateRegistrationInfo = this.Output.Tables["WixUpdateRegistration"].RowsAs<WixUpdateRegistrationRow>().FirstOrDefault();

                if (null != updateRegistrationInfo)
                {
                    writer.WriteStartElement("Update"); // <Update>
                    writer.WriteAttributeString("Manufacturer", updateRegistrationInfo.Manufacturer);

                    if (!String.IsNullOrEmpty(updateRegistrationInfo.Department))
                    {
                        writer.WriteAttributeString("Department", updateRegistrationInfo.Department);
                    }

                    if (!String.IsNullOrEmpty(updateRegistrationInfo.ProductFamily))
                    {
                        writer.WriteAttributeString("ProductFamily", updateRegistrationInfo.ProductFamily);
                    }

                    writer.WriteAttributeString("Name", updateRegistrationInfo.Name);
                    writer.WriteAttributeString("Classification", updateRegistrationInfo.Classification);
                    writer.WriteEndElement(); // </Update>
                }

                IEnumerable<Row> bundleTags = this.Output.Tables["WixBundleTag"].RowsAs<Row>();

                foreach (Row row in bundleTags)
                {
                    writer.WriteStartElement("SoftwareTag");
                    writer.WriteAttributeString("Filename", (string)row[0]);
                    writer.WriteAttributeString("Regid", (string)row[1]);
                    writer.WriteCData((string)row[4]);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // </Register>

                // write the Chain...
                writer.WriteStartElement("Chain");
                if (this.Chain.DisableRollback)
                {
                    writer.WriteAttributeString("DisableRollback", "yes");
                }

                if (this.Chain.DisableSystemRestore)
                {
                    writer.WriteAttributeString("DisableSystemRestore", "yes");
                }

                if (this.Chain.ParallelCache)
                {
                    writer.WriteAttributeString("ParallelCache", "yes");
                }

                // Index a few tables by package.
                ILookup<string, WixBundlePatchTargetCodeRow> targetCodesByPatch = this.Output.Tables["WixBundlePatchTargetCode"].RowsAs<WixBundlePatchTargetCodeRow>().ToLookup(r => r.MspPackageId);
                ILookup<string, WixBundleMsiFeatureRow> msiFeaturesByPackage = this.Output.Tables["WixBundleMsiFeature"].RowsAs<WixBundleMsiFeatureRow>().ToLookup(r => r.ChainPackageId);
                ILookup<string, WixBundleMsiPropertyRow> msiPropertiesByPackage = this.Output.Tables["WixBundleMsiProperty"].RowsAs<WixBundleMsiPropertyRow>().ToLookup(r => r.ChainPackageId);
                ILookup<string, WixBundlePayloadRow> payloadsByPackage = this.Payloads.Values.ToLookup(p => p.Package);
                ILookup<string, WixBundleRelatedPackageRow> relatedPackagesByPackage = this.Output.Tables["WixBundleRelatedPackage"].RowsAs<WixBundleRelatedPackageRow>().ToLookup(r => r.ChainPackageId);
                ILookup<string, WixBundleSlipstreamMspRow> slipstreamMspsByPackage = this.Output.Tables["WixBundleSlipstreamMsp"].RowsAs<WixBundleSlipstreamMspRow>().ToLookup(r => r.ChainPackageId);
                ILookup<string, WixBundlePackageExitCodeRow> exitCodesByPackage = this.Output.Tables["WixBundlePackageExitCode"].RowsAs<WixBundlePackageExitCodeRow>().ToLookup(r => r.ChainPackageId);
                ILookup<string, WixBundlePackageCommandLineRow> commandLinesByPackage = this.Output.Tables["WixBundlePackageCommandLine"].RowsAs<WixBundlePackageCommandLineRow>().ToLookup(r => r.ChainPackageId);

                // Build up the list of target codes from all the MSPs in the chain.
                List<WixBundlePatchTargetCodeRow> targetCodes = new List<WixBundlePatchTargetCodeRow>();

                foreach (PackageFacade package in this.OrderedPackages)
                {
                    writer.WriteStartElement(String.Format(CultureInfo.InvariantCulture, "{0}Package", package.Package.Type));

                    writer.WriteAttributeString("Id", package.Package.WixChainItemId);

                    switch (package.Package.Cache)
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

                    writer.WriteAttributeString("CacheId", package.Package.CacheId);
                    writer.WriteAttributeString("InstallSize", Convert.ToString(package.Package.InstallSize));
                    writer.WriteAttributeString("Size", Convert.ToString(package.Package.Size));
                    writer.WriteAttributeString("PerMachine", YesNoDefaultType.Yes == package.Package.PerMachine ? "yes" : "no");
                    writer.WriteAttributeString("Permanent", package.Package.Permanent ? "yes" : "no");
                    writer.WriteAttributeString("Vital", (YesNoType.Yes == package.Package.Vital) ? "yes" : "no");

                    if (null != package.Package.RollbackBoundary)
                    {
                        writer.WriteAttributeString("RollbackBoundaryForward", package.Package.RollbackBoundary);
                    }

                    if (!String.IsNullOrEmpty(package.Package.RollbackBoundaryBackward))
                    {
                        writer.WriteAttributeString("RollbackBoundaryBackward", package.Package.RollbackBoundaryBackward);
                    }

                    if (!String.IsNullOrEmpty(package.Package.LogPathVariable))
                    {
                        writer.WriteAttributeString("LogPathVariable", package.Package.LogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.Package.RollbackLogPathVariable))
                    {
                        writer.WriteAttributeString("RollbackLogPathVariable", package.Package.RollbackLogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.Package.InstallCondition))
                    {
                        writer.WriteAttributeString("InstallCondition", package.Package.InstallCondition);
                    }

                    if (WixBundlePackageType.Exe == package.Package.Type)
                    {
                        writer.WriteAttributeString("DetectCondition", package.ExePackage.DetectCondition);
                        writer.WriteAttributeString("InstallArguments", package.ExePackage.InstallCommand);
                        writer.WriteAttributeString("UninstallArguments", package.ExePackage.UninstallCommand);
                        writer.WriteAttributeString("RepairArguments", package.ExePackage.RepairCommand);
                        writer.WriteAttributeString("Repairable", package.ExePackage.Repairable ? "yes" : "no");
                        if (!String.IsNullOrEmpty(package.ExePackage.ExeProtocol))
                        {
                            writer.WriteAttributeString("Protocol", package.ExePackage.ExeProtocol);
                        }
                    }
                    else if (WixBundlePackageType.Msi == package.Package.Type)
                    {
                        writer.WriteAttributeString("ProductCode", package.MsiPackage.ProductCode);
                        writer.WriteAttributeString("Language", package.MsiPackage.ProductLanguage.ToString(CultureInfo.InvariantCulture));
                        writer.WriteAttributeString("Version", package.MsiPackage.ProductVersion);
                        writer.WriteAttributeString("DisplayInternalUI", package.MsiPackage.DisplayInternalUI ? "yes" : "no");
                        if (!String.IsNullOrEmpty(package.MsiPackage.UpgradeCode))
                        {
                            writer.WriteAttributeString("UpgradeCode", package.MsiPackage.UpgradeCode);
                        }
                    }
                    else if (WixBundlePackageType.Msp == package.Package.Type)
                    {
                        writer.WriteAttributeString("PatchCode", package.MspPackage.PatchCode);
                        writer.WriteAttributeString("PatchXml", package.MspPackage.PatchXml);
                        writer.WriteAttributeString("DisplayInternalUI", package.MspPackage.DisplayInternalUI ? "yes" : "no");

                        // If there is still a chance that all of our patches will target a narrow set of
                        // product codes, add the patch list to the overall list.
                        if (null != targetCodes)
                        {
                            if (!package.MspPackage.TargetUnspecified)
                            {
                                IEnumerable<WixBundlePatchTargetCodeRow> patchTargetCodes = targetCodesByPatch[package.MspPackage.ChainPackageId];

                                targetCodes.AddRange(patchTargetCodes);
                            }
                            else // we have a patch that targets the world, so throw the whole list away.
                            {
                                targetCodes = null;
                            }
                        }
                    }
                    else if (WixBundlePackageType.Msu == package.Package.Type)
                    {
                        writer.WriteAttributeString("DetectCondition", package.MsuPackage.DetectCondition);
                        writer.WriteAttributeString("KB", package.MsuPackage.MsuKB);
                    }

                    IEnumerable<WixBundleMsiFeatureRow> packageMsiFeatures = msiFeaturesByPackage[package.Package.WixChainItemId];

                    foreach (WixBundleMsiFeatureRow feature in packageMsiFeatures)
                    {
                        writer.WriteStartElement("MsiFeature");
                        writer.WriteAttributeString("Id", feature.Name);
                        writer.WriteEndElement();
                    }

                    IEnumerable<WixBundleMsiPropertyRow> packageMsiProperties = msiPropertiesByPackage[package.Package.WixChainItemId];

                    foreach (WixBundleMsiPropertyRow msiProperty in packageMsiProperties)
                    {
                        writer.WriteStartElement("MsiProperty");
                        writer.WriteAttributeString("Id", msiProperty.Name);
                        writer.WriteAttributeString("Value", msiProperty.Value);
                        if (!String.IsNullOrEmpty(msiProperty.Condition))
                        {
                            writer.WriteAttributeString("Condition", msiProperty.Condition);
                        }
                        writer.WriteEndElement();
                    }

                    IEnumerable<WixBundleSlipstreamMspRow> packageSlipstreamMsps = slipstreamMspsByPackage[package.Package.WixChainItemId];

                    foreach (WixBundleSlipstreamMspRow slipstreamMsp in packageSlipstreamMsps)
                    {
                        writer.WriteStartElement("SlipstreamMsp");
                        writer.WriteAttributeString("Id", slipstreamMsp.MspPackageId);
                        writer.WriteEndElement();
                    }

                    IEnumerable<WixBundlePackageExitCodeRow> packageExitCodes = exitCodesByPackage[package.Package.WixChainItemId];

                    foreach (WixBundlePackageExitCodeRow exitCode in packageExitCodes)
                    {
                        writer.WriteStartElement("ExitCode");

                        if (exitCode.Code.HasValue)
                        {
                            writer.WriteAttributeString("Code", unchecked((uint)exitCode.Code).ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            writer.WriteAttributeString("Code", "*");
                        }

                        writer.WriteAttributeString("Type", ((int)exitCode.Behavior).ToString(CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }

                    IEnumerable<WixBundlePackageCommandLineRow> packageCommandLines = commandLinesByPackage[package.Package.WixChainItemId];

                    foreach (WixBundlePackageCommandLineRow commandLine in packageCommandLines)
                    {
                        writer.WriteStartElement("CommandLine");
                        writer.WriteAttributeString("InstallArgument", commandLine.InstallArgument);
                        writer.WriteAttributeString("UninstallArgument", commandLine.UninstallArgument);
                        writer.WriteAttributeString("RepairArgument", commandLine.RepairArgument);
                        writer.WriteAttributeString("Condition", commandLine.Condition);
                        writer.WriteEndElement();
                    }

                    // Output the dependency information.
                    foreach (ProvidesDependency dependency in package.Provides)
                    {
                        // TODO: Add to wixpdb as an imported table, or link package wixpdbs to bundle wixpdbs.
                        dependency.WriteXml(writer);
                    }

                    IEnumerable<WixBundleRelatedPackageRow> packageRelatedPackages = relatedPackagesByPackage[package.Package.WixChainItemId];

                    foreach (WixBundleRelatedPackageRow related in packageRelatedPackages)
                    {
                        writer.WriteStartElement("RelatedPackage");
                        writer.WriteAttributeString("Id", related.Id);
                        if (!String.IsNullOrEmpty(related.MinVersion))
                        {
                            writer.WriteAttributeString("MinVersion", related.MinVersion);
                            writer.WriteAttributeString("MinInclusive", related.MinInclusive ? "yes" : "no");
                        }
                        if (!String.IsNullOrEmpty(related.MaxVersion))
                        {
                            writer.WriteAttributeString("MaxVersion", related.MaxVersion);
                            writer.WriteAttributeString("MaxInclusive", related.MaxInclusive ? "yes" : "no");
                        }
                        writer.WriteAttributeString("OnlyDetect", related.OnlyDetect ? "yes" : "no");

                        string[] relatedLanguages = related.Languages.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (0 < relatedLanguages.Length)
                        {
                            writer.WriteAttributeString("LangInclusive", related.LangInclusive ? "yes" : "no");
                            foreach (string language in relatedLanguages)
                            {
                                writer.WriteStartElement("Language");
                                writer.WriteAttributeString("Id", language);
                                writer.WriteEndElement();
                            }
                        }
                        writer.WriteEndElement();
                    }

                    // Write any contained Payloads with the PackagePayload being first
                    writer.WriteStartElement("PayloadRef");
                    writer.WriteAttributeString("Id", package.Package.PackagePayload);
                    writer.WriteEndElement();

                    IEnumerable<WixBundlePayloadRow> packagePayloads = payloadsByPackage[package.Package.WixChainItemId];

                    foreach (WixBundlePayloadRow payload in packagePayloads)
                    {
                        if (payload.Id != package.Package.PackagePayload)
                        {
                            writer.WriteStartElement("PayloadRef");
                            writer.WriteAttributeString("Id", payload.Id);
                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteEndElement(); // </XxxPackage>
                }
                writer.WriteEndElement(); // </Chain>

                if (null != targetCodes)
                {
                    foreach (WixBundlePatchTargetCodeRow targetCode in targetCodes)
                    {
                        writer.WriteStartElement("PatchTargetCode");
                        writer.WriteAttributeString("TargetCode", targetCode.TargetCode);
                        writer.WriteAttributeString("Product", targetCode.TargetsProductCode ? "yes" : "no");
                        writer.WriteEndElement();
                    }
                }

                // Write the ApprovedExeForElevation elements.
                IEnumerable<WixApprovedExeForElevationRow> approvedExesForElevation = this.Output.Tables["WixApprovedExeForElevation"].RowsAs<WixApprovedExeForElevationRow>();

                foreach (WixApprovedExeForElevationRow approvedExeForElevation in approvedExesForElevation)
                {
                    writer.WriteStartElement("ApprovedExeForElevation");
                    writer.WriteAttributeString("Id", approvedExeForElevation.Id);
                    writer.WriteAttributeString("Key", approvedExeForElevation.Key);

                    if (!String.IsNullOrEmpty(approvedExeForElevation.ValueName))
                    {
                        writer.WriteAttributeString("ValueName", approvedExeForElevation.ValueName);
                    }

                    if (approvedExeForElevation.Win64)
                    {
                        writer.WriteAttributeString("Win64", "yes");
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndDocument(); // </BurnManifest>
            }
        }

        private void WriteBurnManifestContainerAttributes(XmlTextWriter writer, string executableName, WixBundleContainerRow container)
        {
            writer.WriteAttributeString("Id", container.Id);
            writer.WriteAttributeString("FileSize", container.Size.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", container.Hash);

            if (ContainerType.Detached == container.Type)
            {
                string resolvedUrl = this.ResolveUrl(container.DownloadUrl, null, null, container.Id, container.Name);
                if (!String.IsNullOrEmpty(resolvedUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", resolvedUrl);
                }
                else if (!String.IsNullOrEmpty(container.DownloadUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", container.DownloadUrl);
                }

                writer.WriteAttributeString("FilePath", container.Name);
            }
            else if (ContainerType.Attached == container.Type)
            {
                if (!String.IsNullOrEmpty(container.DownloadUrl))
                {
                    Messaging.Instance.OnMessage(WixWarnings.DownloadUrlNotSupportedForAttachedContainers(container.SourceLineNumbers, container.Id));
                }

                writer.WriteAttributeString("FilePath", executableName); // attached containers use the name of the bundle since they are attached to the executable.
                writer.WriteAttributeString("AttachedIndex", container.AttachedContainerIndex.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Attached", "yes");
                writer.WriteAttributeString("Primary", "yes");
            }
        }

        private void WriteBurnManifestPayloadAttributes(XmlTextWriter writer, WixBundlePayloadRow payload, bool embeddedOnly, Dictionary<string, WixBundlePayloadRow> allPayloads)
        {
            Debug.Assert(!embeddedOnly || PackagingType.Embedded == payload.Packaging);

            writer.WriteAttributeString("Id", payload.Id);
            writer.WriteAttributeString("FilePath", payload.Name);
            writer.WriteAttributeString("FileSize", payload.FileSize.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", payload.Hash);

            if (payload.LayoutOnly)
            {
                writer.WriteAttributeString("LayoutOnly", "yes");
            }

            if (!String.IsNullOrEmpty(payload.PublicKey))
            {
                writer.WriteAttributeString("CertificateRootPublicKeyIdentifier", payload.PublicKey);
            }

            if (!String.IsNullOrEmpty(payload.Thumbprint))
            {
                writer.WriteAttributeString("CertificateRootThumbprint", payload.Thumbprint);
            }

            switch (payload.Packaging)
            {
                case PackagingType.Embedded: // this means it's in a container.
                    if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DownloadUrlNotSupportedForEmbeddedPayloads(payload.SourceLineNumbers, payload.Id));
                    }

                    writer.WriteAttributeString("Packaging", "embedded");
                    writer.WriteAttributeString("SourcePath", payload.EmbeddedId);

                    if (Compiler.BurnUXContainerId != payload.Container)
                    {
                        writer.WriteAttributeString("Container", payload.Container);
                    }
                    break;

                case PackagingType.External:
                    string packageId = payload.ParentPackagePayload;
                    string parentUrl = payload.ParentPackagePayload == null ? null : allPayloads[payload.ParentPackagePayload].DownloadUrl;
                    string resolvedUrl = this.ResolveUrl(payload.DownloadUrl, parentUrl, packageId, payload.Id, payload.Name);
                    if (!String.IsNullOrEmpty(resolvedUrl))
                    {
                        writer.WriteAttributeString("DownloadUrl", resolvedUrl);
                    }
                    else if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        writer.WriteAttributeString("DownloadUrl", payload.DownloadUrl);
                    }

                    writer.WriteAttributeString("Packaging", "external");
                    writer.WriteAttributeString("SourcePath", payload.Name);
                    break;
            }

            if (!String.IsNullOrEmpty(payload.Catalog))
            {
                writer.WriteAttributeString("Catalog", payload.Catalog);
            }
        }

        private string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            string resolved = null;
            foreach (IBinderFileManager fileManager in this.FileManagers)
            {
                resolved = fileManager.ResolveUrl(url, fallbackUrl, packageId, payloadId, fileName);
                if (!String.IsNullOrEmpty(resolved))
                {
                    break;
                }
            }

            return resolved;
        }
    }
}
