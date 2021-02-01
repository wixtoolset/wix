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
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class CreateBurnManifestCommand
    {
        public CreateBurnManifestCommand(IMessaging messaging, IEnumerable<IBurnBackendExtension> backendExtensions, string executableName, IntermediateSection section, WixBundleSymbol bundleSymbol, IEnumerable<WixBundleContainerSymbol> containers, WixChainSymbol chainSymbol, IEnumerable<PackageFacade> orderedPackages, IEnumerable<WixBundleRollbackBoundarySymbol> boundaries, IEnumerable<WixBundlePayloadSymbol> uxPayloads, Dictionary<string, WixBundlePayloadSymbol> allPayloadsById, IEnumerable<ISearchFacade> orderedSearches, string intermediateFolder)
        {
            this.Messaging = messaging;
            this.BackendExtensions = backendExtensions;
            this.ExecutableName = executableName;
            this.Section = section;
            this.BundleSymbol = bundleSymbol;
            this.Chain = chainSymbol;
            this.Containers = containers;
            this.OrderedPackages = orderedPackages;
            this.RollbackBoundaries = boundaries;
            this.UXContainerPayloads = uxPayloads;
            this.Payloads = allPayloadsById;
            this.OrderedSearches = orderedSearches;
            this.IntermediateFolder = intermediateFolder;
        }

        public string OutputPath { get; private set; }

        private IMessaging Messaging { get; }

        private IEnumerable<IBurnBackendExtension> BackendExtensions { get; }

        private string ExecutableName { get; }

        private IntermediateSection Section { get; }

        private WixBundleSymbol BundleSymbol { get; }

        private WixChainSymbol Chain { get; }

        private IEnumerable<WixBundleRollbackBoundarySymbol> RollbackBoundaries { get; }

        private IEnumerable<PackageFacade> OrderedPackages { get; }

        private IEnumerable<ISearchFacade> OrderedSearches { get; }

        private Dictionary<string, WixBundlePayloadSymbol> Payloads { get; }

        private IEnumerable<WixBundleContainerSymbol> Containers { get; }

        private IEnumerable<WixBundlePayloadSymbol> UXContainerPayloads { get; }

        private string IntermediateFolder { get; }

        public void Execute()
        {
            this.OutputPath = Path.Combine(this.IntermediateFolder, "bundle-manifest.xml");

            using (var writer = new XmlTextWriter(this.OutputPath, Encoding.UTF8))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("BurnManifest", BurnCommon.BurnNamespace);

                // Write the condition, if there is one
                if (null != this.BundleSymbol.Condition)
                {
                    writer.WriteElementString("Condition", this.BundleSymbol.Condition);
                }

                // Write the log element if default logging wasn't disabled.
                if (!String.IsNullOrEmpty(this.BundleSymbol.LogPrefix))
                {
                    writer.WriteStartElement("Log");
                    if (!String.IsNullOrEmpty(this.BundleSymbol.LogPathVariable))
                    {
                        writer.WriteAttributeString("PathVariable", this.BundleSymbol.LogPathVariable);
                    }
                    writer.WriteAttributeString("Prefix", this.BundleSymbol.LogPrefix);
                    writer.WriteAttributeString("Extension", this.BundleSymbol.LogExtension);
                    writer.WriteEndElement();
                }


                // Get update if specified.
                var updateSymbol = this.Section.Symbols.OfType<WixBundleUpdateSymbol>().FirstOrDefault();

                if (null != updateSymbol)
                {
                    writer.WriteStartElement("Update");
                    writer.WriteAttributeString("Location", updateSymbol.Location);
                    writer.WriteEndElement(); // </Update>
                }

                // Write the RelatedBundle elements

                // For the related bundles with duplicated identifiers the second instance is ignored (i.e. the Duplicates
                // enumeration in the index row list is not used).
                var relatedBundles = this.Section.Symbols.OfType<WixRelatedBundleSymbol>();
                var distinctRelatedBundles = new HashSet<string>();

                foreach (var relatedBundle in relatedBundles)
                {
                    if (distinctRelatedBundles.Add(relatedBundle.BundleId))
                    {
                        writer.WriteStartElement("RelatedBundle");
                        writer.WriteAttributeString("Id", relatedBundle.BundleId);
                        writer.WriteAttributeString("Action", relatedBundle.Action.ToString());
                        writer.WriteEndElement();
                    }
                }

                // Write the variables
                var variables = this.Section.Symbols.OfType<WixBundleVariableSymbol>();

                foreach (var variable in variables)
                {
                    writer.WriteStartElement("Variable");
                    writer.WriteAttributeString("Id", variable.Id.Id);
                    if (variable.Type != WixBundleVariableType.Unknown)
                    {
                        writer.WriteAttributeString("Value", variable.Value);

                        switch (variable.Type)
                        {
                            case WixBundleVariableType.Formatted:
                                writer.WriteAttributeString("Type", "formatted");
                                break;
                            case WixBundleVariableType.Numeric:
                                writer.WriteAttributeString("Type", "numeric");
                                break;
                            case WixBundleVariableType.String:
                                writer.WriteAttributeString("Type", "string");
                                break;
                            case WixBundleVariableType.Version:
                                writer.WriteAttributeString("Type", "version");
                                break;
                        }
                    }
                    writer.WriteAttributeString("Hidden", variable.Hidden ? "yes" : "no");
                    writer.WriteAttributeString("Persisted", variable.Persisted ? "yes" : "no");
                    writer.WriteEndElement();
                }

                // Write the searches
                foreach (var searchinfo in this.OrderedSearches)
                {
                    searchinfo.WriteXml(writer);
                }

                // write the UX element
                writer.WriteStartElement("UX");
                if (!String.IsNullOrEmpty(this.BundleSymbol.SplashScreenSourceFile))
                {
                    writer.WriteAttributeString("SplashScreen", "yes");
                }

                // write the UX allPayloads...
                foreach (var payload in this.UXContainerPayloads)
                {
                    writer.WriteStartElement("Payload");
                    this.WriteBurnManifestPayloadAttributes(writer, payload, true, this.Payloads);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // </UX>

                foreach (var container in this.Containers)
                {
                    if (!String.IsNullOrEmpty(container.WorkingPath) && BurnConstants.BurnUXContainerName != container.Id.Id)
                    {
                        writer.WriteStartElement("Container");
                        this.WriteBurnManifestContainerAttributes(writer, this.ExecutableName, container);
                        writer.WriteEndElement();
                    }
                }

                foreach (var payload in this.Payloads.Values)
                {
                    if (PackagingType.Embedded == payload.Packaging && BurnConstants.BurnUXContainerName != payload.ContainerRef)
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

                foreach (var rollbackBoundary in this.RollbackBoundaries)
                {
                    writer.WriteStartElement("RollbackBoundary");
                    writer.WriteAttributeString("Id", rollbackBoundary.Id.Id);
                    writer.WriteAttributeString("Vital", rollbackBoundary.Vital == false ? "no" : "yes");
                    writer.WriteAttributeString("Transaction", rollbackBoundary.Transaction == true ? "yes" : "no");
                    writer.WriteEndElement();
                }

                // Write the registration information...
                writer.WriteStartElement("Registration");

                writer.WriteAttributeString("Id", this.BundleSymbol.BundleId);
                writer.WriteAttributeString("ExecutableName", this.ExecutableName);
                writer.WriteAttributeString("PerMachine", this.BundleSymbol.PerMachine ? "yes" : "no");
                writer.WriteAttributeString("Tag", this.BundleSymbol.Tag);
                writer.WriteAttributeString("Version", this.BundleSymbol.Version);
                writer.WriteAttributeString("ProviderKey", this.BundleSymbol.ProviderKey);

                writer.WriteStartElement("Arp");
                writer.WriteAttributeString("Register", (this.BundleSymbol.DisableModify || this.BundleSymbol.SingleChangeUninstallButton) && this.BundleSymbol.DisableRemove ? "no" : "yes"); // do not register if disabled modify and remove.
                writer.WriteAttributeString("DisplayName", this.BundleSymbol.Name);
                writer.WriteAttributeString("DisplayVersion", this.BundleSymbol.Version);

                if (!String.IsNullOrEmpty(this.BundleSymbol.Manufacturer))
                {
                    writer.WriteAttributeString("Publisher", this.BundleSymbol.Manufacturer);
                }

                if (!String.IsNullOrEmpty(this.BundleSymbol.HelpUrl))
                {
                    writer.WriteAttributeString("HelpLink", this.BundleSymbol.HelpUrl);
                }

                if (!String.IsNullOrEmpty(this.BundleSymbol.HelpTelephone))
                {
                    writer.WriteAttributeString("HelpTelephone", this.BundleSymbol.HelpTelephone);
                }

                if (!String.IsNullOrEmpty(this.BundleSymbol.AboutUrl))
                {
                    writer.WriteAttributeString("AboutUrl", this.BundleSymbol.AboutUrl);
                }

                if (!String.IsNullOrEmpty(this.BundleSymbol.UpdateUrl))
                {
                    writer.WriteAttributeString("UpdateUrl", this.BundleSymbol.UpdateUrl);
                }

                if (!String.IsNullOrEmpty(this.BundleSymbol.ParentName))
                {
                    writer.WriteAttributeString("ParentDisplayName", this.BundleSymbol.ParentName);
                }

                if (this.BundleSymbol.DisableModify)
                {
                    writer.WriteAttributeString("DisableModify", "yes");
                }

                if (this.BundleSymbol.DisableRemove)
                {
                    writer.WriteAttributeString("DisableRemove", "yes");
                }

                if (this.BundleSymbol.SingleChangeUninstallButton)
                {
                    writer.WriteAttributeString("DisableModify", "button");
                }
                writer.WriteEndElement(); // </Arp>

                // Get update registration if specified.
                var updateRegistrationInfo = this.Section.Symbols.OfType<WixUpdateRegistrationSymbol>().FirstOrDefault();

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

#if TODO // Handle SWID Tags
                var bundleTags = this.Output.Tables["WixBundleTag"].RowsAs<Row>();
                foreach (var row in bundleTags)
                {
                    writer.WriteStartElement("SoftwareTag");
                    writer.WriteAttributeString("Filename", (string)row[0]);
                    writer.WriteAttributeString("Regid", (string)row[1]);
                    writer.WriteCData((string)row[4]);
                    writer.WriteEndElement();
                }
#endif

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
                var targetCodesByPatch = this.Section.Symbols.OfType<WixBundlePatchTargetCodeSymbol>().ToLookup(r => r.PackageRef);
                var msiFeaturesByPackage = this.Section.Symbols.OfType<WixBundleMsiFeatureSymbol>().ToLookup(r => r.PackageRef);
                var msiPropertiesByPackage = this.Section.Symbols.OfType<WixBundleMsiPropertySymbol>().ToLookup(r => r.PackageRef);
                var payloadsByPackage = this.Payloads.Values.ToLookup(p => p.PackageRef);
                var relatedPackagesByPackage = this.Section.Symbols.OfType<WixBundleRelatedPackageSymbol>().ToLookup(r => r.PackageRef);
                var slipstreamMspsByPackage = this.Section.Symbols.OfType<WixBundleSlipstreamMspSymbol>().ToLookup(r => r.MspPackageRef);
                var exitCodesByPackage = this.Section.Symbols.OfType<WixBundlePackageExitCodeSymbol>().ToLookup(r => r.ChainPackageId);
                var commandLinesByPackage = this.Section.Symbols.OfType<WixBundlePackageCommandLineSymbol>().ToLookup(r => r.WixBundlePackageRef);

                var dependenciesByPackage = this.Section.Symbols.OfType<ProvidesDependencySymbol>().ToLookup(p => p.PackageRef);


                // Build up the list of target codes from all the MSPs in the chain.
                var targetCodes = new List<WixBundlePatchTargetCodeSymbol>();

                foreach (var package in this.OrderedPackages)
                {
                    writer.WriteStartElement(String.Format(CultureInfo.InvariantCulture, "{0}Package", package.PackageSymbol.Type));

                    writer.WriteAttributeString("Id", package.PackageId);

                    switch (package.PackageSymbol.Cache)
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

                    writer.WriteAttributeString("CacheId", package.PackageSymbol.CacheId);
                    writer.WriteAttributeString("InstallSize", Convert.ToString(package.PackageSymbol.InstallSize));
                    writer.WriteAttributeString("Size", Convert.ToString(package.PackageSymbol.Size));
                    writer.WriteAttributeString("PerMachine", YesNoDefaultType.Yes == package.PackageSymbol.PerMachine ? "yes" : "no");
                    writer.WriteAttributeString("Permanent", package.PackageSymbol.Permanent ? "yes" : "no");
                    writer.WriteAttributeString("Vital", package.PackageSymbol.Vital == false ? "no" : "yes");

                    if (null != package.PackageSymbol.RollbackBoundaryRef)
                    {
                        writer.WriteAttributeString("RollbackBoundaryForward", package.PackageSymbol.RollbackBoundaryRef);
                    }

                    if (!String.IsNullOrEmpty(package.PackageSymbol.RollbackBoundaryBackwardRef))
                    {
                        writer.WriteAttributeString("RollbackBoundaryBackward", package.PackageSymbol.RollbackBoundaryBackwardRef);
                    }

                    if (!String.IsNullOrEmpty(package.PackageSymbol.LogPathVariable))
                    {
                        writer.WriteAttributeString("LogPathVariable", package.PackageSymbol.LogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.PackageSymbol.RollbackLogPathVariable))
                    {
                        writer.WriteAttributeString("RollbackLogPathVariable", package.PackageSymbol.RollbackLogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.PackageSymbol.InstallCondition))
                    {
                        writer.WriteAttributeString("InstallCondition", package.PackageSymbol.InstallCondition);
                    }

                    if (package.SpecificPackageSymbol is WixBundleExePackageSymbol exePackage) // EXE
                    {
                        writer.WriteAttributeString("DetectCondition", exePackage.DetectCondition);
                        writer.WriteAttributeString("InstallArguments", exePackage.InstallCommand);
                        writer.WriteAttributeString("UninstallArguments", exePackage.UninstallCommand);
                        writer.WriteAttributeString("RepairArguments", exePackage.RepairCommand);
                        writer.WriteAttributeString("Repairable", exePackage.Repairable ? "yes" : "no");
                        if (!String.IsNullOrEmpty(exePackage.ExeProtocol))
                        {
                            writer.WriteAttributeString("Protocol", exePackage.ExeProtocol);
                        }
                    }
                    else if (package.SpecificPackageSymbol is WixBundleMsiPackageSymbol msiPackage) // MSI
                    {
                        writer.WriteAttributeString("ProductCode", msiPackage.ProductCode);
                        writer.WriteAttributeString("Language", msiPackage.ProductLanguage.ToString(CultureInfo.InvariantCulture));
                        writer.WriteAttributeString("Version", msiPackage.ProductVersion);
                        if (!String.IsNullOrEmpty(msiPackage.UpgradeCode))
                        {
                            writer.WriteAttributeString("UpgradeCode", msiPackage.UpgradeCode);
                        }
                    }
                    else if (package.SpecificPackageSymbol is WixBundleMspPackageSymbol mspPackage) // MSP
                    {
                        writer.WriteAttributeString("PatchCode", mspPackage.PatchCode);
                        writer.WriteAttributeString("PatchXml", mspPackage.PatchXml);

                        // If there is still a chance that all of our patches will target a narrow set of
                        // product codes, add the patch list to the overall list.
                        if (null != targetCodes)
                        {
                            if (!mspPackage.TargetUnspecified)
                            {
                                var patchTargetCodes = targetCodesByPatch[mspPackage.Id.Id];

                                targetCodes.AddRange(patchTargetCodes);
                            }
                            else // we have a patch that targets the world, so throw the whole list away.
                            {
                                targetCodes = null;
                            }
                        }
                    }
                    else if (package.SpecificPackageSymbol is WixBundleMsuPackageSymbol msuPackage) // MSU
                    {
                        writer.WriteAttributeString("DetectCondition", msuPackage.DetectCondition);
                        writer.WriteAttributeString("KB", msuPackage.MsuKB);
                    }

                    var packageMsiFeatures = msiFeaturesByPackage[package.PackageId];

                    foreach (var feature in packageMsiFeatures)
                    {
                        writer.WriteStartElement("MsiFeature");
                        writer.WriteAttributeString("Id", feature.Name);
                        writer.WriteEndElement();
                    }

                    var packageMsiProperties = msiPropertiesByPackage[package.PackageId];

                    foreach (var msiProperty in packageMsiProperties)
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

                    var packageSlipstreamMsps = slipstreamMspsByPackage[package.PackageId];

                    foreach (var slipstreamMsp in packageSlipstreamMsps)
                    {
                        writer.WriteStartElement("SlipstreamMsp");
                        writer.WriteAttributeString("Id", slipstreamMsp.MspPackageRef);
                        writer.WriteEndElement();
                    }

                    var packageExitCodes = exitCodesByPackage[package.PackageId];

                    foreach (var exitCode in packageExitCodes)
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

                    var packageCommandLines = commandLinesByPackage[package.PackageId];

                    foreach (var commandLine in packageCommandLines)
                    {
                        writer.WriteStartElement("CommandLine");
                        writer.WriteAttributeString("InstallArgument", commandLine.InstallArgument);
                        writer.WriteAttributeString("UninstallArgument", commandLine.UninstallArgument);
                        writer.WriteAttributeString("RepairArgument", commandLine.RepairArgument);
                        writer.WriteAttributeString("Condition", commandLine.Condition);
                        writer.WriteEndElement();
                    }

                    // Output the dependency information.
                    var dependencies = dependenciesByPackage[package.PackageId];

                    foreach (var dependency in dependencies)
                    {
                        writer.WriteStartElement("Provides");
                        writer.WriteAttributeString("Key", dependency.Key);

                        if (!String.IsNullOrEmpty(dependency.Version))
                        {
                            writer.WriteAttributeString("Version", dependency.Version);
                        }

                        if (!String.IsNullOrEmpty(dependency.DisplayName))
                        {
                            writer.WriteAttributeString("DisplayName", dependency.DisplayName);
                        }

                        if (dependency.Imported)
                        {
                            // The package dependency was explicitly authored into the manifest.
                            writer.WriteAttributeString("Imported", "yes");
                        }

                        writer.WriteEndElement();
                    }

                    var packageRelatedPackages = relatedPackagesByPackage[package.PackageId];

                    foreach (var related in packageRelatedPackages)
                    {
                        writer.WriteStartElement("RelatedPackage");
                        writer.WriteAttributeString("Id", related.RelatedId);
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

                        var relatedLanguages = related.Languages.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

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
                    writer.WriteAttributeString("Id", package.PackageSymbol.PayloadRef);
                    writer.WriteEndElement();

                    var packagePayloads = payloadsByPackage[package.PackageId];

                    foreach (var payload in packagePayloads)
                    {
                        if (payload.Id.Id != package.PackageSymbol.PayloadRef)
                        {
                            writer.WriteStartElement("PayloadRef");
                            writer.WriteAttributeString("Id", payload.Id.Id);
                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteEndElement(); // </XxxPackage>
                }
                writer.WriteEndElement(); // </Chain>

                if (null != targetCodes)
                {
                    foreach (var targetCode in targetCodes)
                    {
                        writer.WriteStartElement("PatchTargetCode");
                        writer.WriteAttributeString("TargetCode", targetCode.TargetCode);
                        writer.WriteAttributeString("Product", targetCode.TargetsProductCode ? "yes" : "no");
                        writer.WriteEndElement();
                    }
                }

                // Write the ApprovedExeForElevation elements.
                var approvedExesForElevation = this.Section.Symbols.OfType<WixApprovedExeForElevationSymbol>();

                foreach (var approvedExeForElevation in approvedExesForElevation)
                {
                    writer.WriteStartElement("ApprovedExeForElevation");
                    writer.WriteAttributeString("Id", approvedExeForElevation.Id.Id);
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

                // Write the BundleExtension elements.
                var bundleExtensions = this.Section.Symbols.OfType<WixBundleExtensionSymbol>();

                foreach (var bundleExtension in bundleExtensions)
                {
                    writer.WriteStartElement("BundleExtension");
                    writer.WriteAttributeString("Id", bundleExtension.Id.Id);
                    writer.WriteAttributeString("EntryPayloadId", bundleExtension.PayloadRef);

                    writer.WriteEndElement();
                }

                writer.WriteEndDocument(); // </BurnManifest>
            }
        }

        private void WriteBurnManifestContainerAttributes(XmlTextWriter writer, string executableName, WixBundleContainerSymbol container)
        {
            writer.WriteAttributeString("Id", container.Id.Id);
            writer.WriteAttributeString("FileSize", container.Size.Value.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", container.Hash);

            if (ContainerType.Detached == container.Type)
            {
                string resolvedUrl = this.ResolveUrl(container.DownloadUrl, null, null, container.Id.Id, container.Name);
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
                    this.Messaging.Write(WarningMessages.DownloadUrlNotSupportedForAttachedContainers(container.SourceLineNumbers, container.Id.Id));
                }

                writer.WriteAttributeString("FilePath", executableName); // attached containers use the name of the bundle since they are attached to the executable.
                writer.WriteAttributeString("AttachedIndex", container.AttachedContainerIndex.Value.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Attached", "yes");
                writer.WriteAttributeString("Primary", "yes");
            }
        }

        private void WriteBurnManifestPayloadAttributes(XmlTextWriter writer, WixBundlePayloadSymbol payload, bool embeddedOnly, Dictionary<string, WixBundlePayloadSymbol> allPayloads)
        {
            Debug.Assert(!embeddedOnly || PackagingType.Embedded == payload.Packaging);

            writer.WriteAttributeString("Id", payload.Id.Id);
            writer.WriteAttributeString("FilePath", payload.Name);
            writer.WriteAttributeString("FileSize", payload.FileSize.Value.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", payload.Hash);

            if (payload.LayoutOnly)
            {
                writer.WriteAttributeString("LayoutOnly", "yes");
            }

            switch (payload.Packaging)
            {
                case PackagingType.Embedded: // this means it's in a container.
                    if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        this.Messaging.Write(WarningMessages.DownloadUrlNotSupportedForEmbeddedPayloads(payload.SourceLineNumbers, payload.Id.Id));
                    }

                    writer.WriteAttributeString("Packaging", "embedded");
                    writer.WriteAttributeString("SourcePath", payload.EmbeddedId);

                    if (BurnConstants.BurnUXContainerName != payload.ContainerRef)
                    {
                        writer.WriteAttributeString("Container", payload.ContainerRef);
                    }
                    break;

                case PackagingType.External:
                    var packageId = payload.ParentPackagePayloadRef;
                    var parentUrl = payload.ParentPackagePayloadRef == null ? null : allPayloads[payload.ParentPackagePayloadRef].DownloadUrl;
                    var resolvedUrl = this.ResolveUrl(payload.DownloadUrl, parentUrl, packageId, payload.Id.Id, payload.Name);
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
        }

        private string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            string resolved = null;
            foreach (var extension in this.BackendExtensions)
            {
                resolved = extension.ResolveUrl(url, fallbackUrl, packageId, payloadId, fileName);
                if (!String.IsNullOrEmpty(resolved))
                {
                    break;
                }
            }

            return resolved;
        }
    }
}
