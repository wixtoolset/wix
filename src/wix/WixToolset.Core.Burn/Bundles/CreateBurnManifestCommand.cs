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

    internal class CreateBurnManifestCommand
    {
        public CreateBurnManifestCommand(string executableName, IntermediateSection section, WixBundleSymbol bundleSymbol, WixBootstrapperApplicationSymbol primaryBundleApplicationSymbol, WixBootstrapperApplicationSymbol secondaryBundleApplicationSymbol, IEnumerable<WixBundleContainerSymbol> containers, WixChainSymbol chainSymbol, PackageFacades packageFacades, IEnumerable<WixBundleRollbackBoundarySymbol> boundaries, IEnumerable<WixBundlePayloadSymbol> uxPayloads, Dictionary<string, WixBundlePayloadSymbol> allPayloadsById, Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>> packagesPayloads, IEnumerable<ISearchFacade> orderedSearches, string intermediateFolder)
        {
            this.ExecutableName = executableName;
            this.Section = section;
            this.BundleSymbol = bundleSymbol;
            this.PrimaryBundleApplicationSymbol = primaryBundleApplicationSymbol;
            this.SecondaryBundleApplicationSymbol = secondaryBundleApplicationSymbol;
            this.Chain = chainSymbol;
            this.Containers = containers;
            this.PackageFacades = packageFacades;
            this.RollbackBoundaries = boundaries;
            this.UXContainerPayloads = uxPayloads;
            this.Payloads = allPayloadsById;
            this.PackagesPayloads = packagesPayloads;
            this.OrderedSearches = orderedSearches;
            this.IntermediateFolder = intermediateFolder;
        }

        public string OutputPath { get; private set; }

        private string ExecutableName { get; }

        private IntermediateSection Section { get; }

        private WixBundleSymbol BundleSymbol { get; }

        private WixBootstrapperApplicationSymbol PrimaryBundleApplicationSymbol { get; }

        private WixBootstrapperApplicationSymbol SecondaryBundleApplicationSymbol { get; }

        private WixChainSymbol Chain { get; }

        private IEnumerable<WixBundleRollbackBoundarySymbol> RollbackBoundaries { get; }

        private PackageFacades PackageFacades { get; }

        private IEnumerable<ISearchFacade> OrderedSearches { get; }

        private Dictionary<string, WixBundlePayloadSymbol> Payloads { get; }

        private Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>> PackagesPayloads { get; }

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

                // Write attributes to support harvesting bundles.
                writer.WriteAttributeString("EngineVersion", $"{SomeVerInfo.Major}.{SomeVerInfo.Minor}.{SomeVerInfo.Patch}.{SomeVerInfo.Commits}");
                writer.WriteAttributeString("ProtocolVersion", BurnCommon.BURN_PROTOCOL_VERSION.ToString());
                writer.WriteAttributeString("Win64", this.BundleSymbol.Platform == Platform.X86 ? "no" : "yes");

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

                writer.WriteAttributeString("PrimaryPayloadId", this.PrimaryBundleApplicationSymbol.ExePayloadRef);

                if (!String.IsNullOrEmpty(this.SecondaryBundleApplicationSymbol?.ExePayloadRef))
                {
                    writer.WriteAttributeString("SecondaryPayloadId", this.SecondaryBundleApplicationSymbol.ExePayloadRef);
                }

                // write the UX allPayloads...
                foreach (var payload in this.UXContainerPayloads)
                {
                    this.WriteBurnManifestUXPayload(writer, payload);
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

                foreach (var payload in this.Payloads.Values.Where(p => p.ContainerRef != BurnConstants.BurnUXContainerName))
                {
                    this.WriteBurnManifestPayload(writer, payload);
                }

                foreach (var rollbackBoundary in this.RollbackBoundaries)
                {
                    writer.WriteStartElement("RollbackBoundary");
                    writer.WriteAttributeString("Id", rollbackBoundary.Id.Id);
                    writer.WriteAttributeString("Vital", rollbackBoundary.Vital ? "yes" : "no");
                    writer.WriteAttributeString("Transaction", rollbackBoundary.Transaction ? "yes" : "no");

                    if (!String.IsNullOrEmpty(rollbackBoundary.LogPathVariable))
                    {
                        writer.WriteAttributeString("LogPathVariable", rollbackBoundary.LogPathVariable);
                    }

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
                writer.WriteAttributeString("DisplayName", this.BundleSymbol.Name);
                writer.WriteAttributeString("DisplayVersion", this.BundleSymbol.Version);

                if (!String.IsNullOrEmpty(this.BundleSymbol.InProgressName))
                {
                    writer.WriteAttributeString("InProgressDisplayName", this.BundleSymbol.InProgressName);
                }

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

                switch (this.BundleSymbol.DisableModify)
                {
                    case WixBundleModifyType.Disabled:
                        writer.WriteAttributeString("DisableModify", "yes");
                        break;
                    case WixBundleModifyType.SingleChangeUninstallButton:
                        writer.WriteAttributeString("DisableModify", "button");
                        break;
                }

                if (this.BundleSymbol.DisableRemove)
                {
                    writer.WriteAttributeString("DisableRemove", "yes");
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

                foreach (var bundleTagSymbol in this.Section.Symbols.OfType<WixBundleTagSymbol>())
                {
                    writer.WriteStartElement("SoftwareTag");
                    writer.WriteAttributeString("Filename", bundleTagSymbol.Filename);
                    writer.WriteAttributeString("Regid", bundleTagSymbol.Regid);
                    writer.WriteAttributeString("Path", bundleTagSymbol.InstallPath);
                    writer.WriteCData(bundleTagSymbol.Xml);
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
                var targetCodesByPackagePayload = this.Section.Symbols.OfType<WixBundlePatchTargetCodeSymbol>().ToLookup(r => r.PackagePayloadRef);
                var msiFeaturesByPackagePayload = this.Section.Symbols.OfType<WixBundleMsiFeatureSymbol>().ToLookup(r => r.PackagePayloadRef);
                var msiPropertiesByPackage = this.Section.Symbols.OfType<WixBundleMsiPropertySymbol>().ToLookup(r => r.PackageRef);
                var relatedBundlesByPackagePayload = this.Section.Symbols.OfType<WixBundlePackageRelatedBundleSymbol>().ToLookup(r => r.PackagePayloadRef);
                var relatedPackagesByPackagePayload = this.Section.Symbols.OfType<WixBundleRelatedPackageSymbol>().ToLookup(r => r.PackagePayloadRef);
                var slipstreamMspsByPackage = this.Section.Symbols.OfType<WixBundleSlipstreamMspSymbol>().ToLookup(r => r.TargetPackageRef);
                var exitCodesByPackage = this.Section.Symbols.OfType<WixBundlePackageExitCodeSymbol>().ToLookup(r => r.ChainPackageId);
                var commandLinesByPackage = this.Section.Symbols.OfType<WixBundlePackageCommandLineSymbol>().ToLookup(r => r.WixBundlePackageRef);

                var dependenciesByPackage = this.Section.Symbols.OfType<WixDependencyProviderSymbol>().ToLookup(p => p.ParentRef);


                // Build up the list of target codes from all the MSPs in the chain.
                var targetCodes = new List<WixBundlePatchTargetCodeSymbol>();

                foreach (var package in this.PackageFacades.OrderedValues)
                {
                    var packagePayloadId = package.PackageSymbol.PayloadRef;

                    writer.WriteStartElement(String.Format(CultureInfo.InvariantCulture, "{0}Package", package.PackageSymbol.Type));

                    writer.WriteAttributeString("Id", package.PackageId);

                    switch (package.PackageSymbol.Cache)
                    {
                        case BundleCacheType.Remove:
                            writer.WriteAttributeString("Cache", "remove");
                            break;
                        case BundleCacheType.Keep:
                            writer.WriteAttributeString("Cache", "keep");
                            break;
                        case BundleCacheType.Force:
                            writer.WriteAttributeString("Cache", "force");
                            break;
                    }

                    writer.WriteAttributeString("CacheId", package.PackageSymbol.CacheId);
                    writer.WriteAttributeString("InstallSize", Convert.ToString(package.PackageSymbol.InstallSize));
                    writer.WriteAttributeString("Size", Convert.ToString(package.PackageSymbol.Size));
                    writer.WriteAttributeString("PerMachine", package.PackageSymbol.PerMachine.HasValue && package.PackageSymbol.PerMachine.Value ? "yes" : "no");
                    writer.WriteAttributeString("Permanent", package.PackageSymbol.Permanent ? "yes" : "no");
                    writer.WriteAttributeString("Vital", package.PackageSymbol.Vital ? "yes" : "no");

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

                    if (!String.IsNullOrEmpty(package.PackageSymbol.RepairCondition))
                    {
                        writer.WriteAttributeString("RepairCondition", package.PackageSymbol.RepairCondition);
                    }

                    if (package.SpecificPackageSymbol is WixBundleBundlePackageSymbol bundlePackage) // BUNDLE
                    {
                        writer.WriteAttributeString("BundleId", bundlePackage.BundleId);
                        writer.WriteAttributeString("Version", bundlePackage.Version);
                        writer.WriteAttributeString("InstallArguments", bundlePackage.InstallCommand);
                        writer.WriteAttributeString("UninstallArguments", bundlePackage.UninstallCommand);
                        writer.WriteAttributeString("RepairArguments", bundlePackage.RepairCommand);
                        writer.WriteAttributeString("SupportsBurnProtocol", bundlePackage.SupportsBurnProtocol ? "yes" : "no");
                        writer.WriteAttributeString("Win64", package.PackageSymbol.Win64 ? "yes" : "no");

                        if (!package.PackageSymbol.Visible)
                        {
                            writer.WriteAttributeString("HideARP", "yes");
                        }
                    }
                    else if (package.SpecificPackageSymbol is WixBundleExePackageSymbol exePackage) // EXE
                    {
                        writer.WriteAttributeString("InstallArguments", exePackage.InstallCommand);
                        writer.WriteAttributeString("RepairArguments", exePackage.RepairCommand);
                        writer.WriteAttributeString("Repairable", exePackage.Repairable ? "yes" : "no");

                        switch (exePackage.DetectionType)
                        {
                            case WixBundleExePackageDetectionType.Condition:
                                writer.WriteAttributeString("DetectionType", "condition");
                                writer.WriteAttributeString("DetectCondition", exePackage.DetectCondition);

                                if (exePackage.Uninstallable)
                                {
                                    writer.WriteAttributeString("UninstallArguments", exePackage.UninstallCommand);
                                    writer.WriteAttributeString("Uninstallable", "yes");
                                }
                                break;
                            case WixBundleExePackageDetectionType.Arp:
                                writer.WriteAttributeString("DetectionType", "arp");
                                writer.WriteAttributeString("ArpId", exePackage.ArpId);
                                writer.WriteAttributeString("ArpDisplayVersion", exePackage.ArpDisplayVersion);

                                if (exePackage.ArpWin64)
                                {
                                    writer.WriteAttributeString("ArpWin64", "yes");
                                }

                                if (exePackage.ArpUseUninstallString)
                                {
                                    writer.WriteAttributeString("ArpUseUninstallString", "yes");
                                }

                                if (!String.IsNullOrEmpty(exePackage.UninstallCommand))
                                {
                                    writer.WriteAttributeString("UninstallArguments", exePackage.UninstallCommand);
                                }
                                break;
                            case WixBundleExePackageDetectionType.None:
                                writer.WriteAttributeString("DetectionType", "none");
                                break;
                        }

                        if (!String.IsNullOrEmpty(exePackage.ExeProtocol))
                        {
                            writer.WriteAttributeString("Protocol", exePackage.ExeProtocol);
                        }

                        if (exePackage.IsBundle)
                        {
                            writer.WriteAttributeString("Bundle", "yes");
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

                        // If feature selection is enabled, represent the Feature table in the manifest.
                        if (msiPackage.EnableFeatureSelection)
                        {
                            var packageMsiFeatures = msiFeaturesByPackagePayload[packagePayloadId];

                            foreach (var feature in packageMsiFeatures)
                            {
                                writer.WriteStartElement("MsiFeature");
                                writer.WriteAttributeString("Id", feature.Name);
                                writer.WriteEndElement();
                            }
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
                            foreach (var patchTargetCode in targetCodesByPackagePayload[packagePayloadId])
                            {
                                if (patchTargetCode.Type == WixBundlePatchTargetCodeType.Unspecified)
                                {
                                    targetCodes = null;
                                    break;
                                }

                                targetCodes.Add(patchTargetCode);
                            }
                        }
                    }
                    else if (package.SpecificPackageSymbol is WixBundleMsuPackageSymbol msuPackage) // MSU
                    {
                        writer.WriteAttributeString("DetectCondition", msuPackage.DetectCondition);
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
                            writer.WriteAttributeString("Code", exitCode.Code.Value.ToString(CultureInfo.InvariantCulture));
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
                        writer.WriteAttributeString("Key", dependency.ProviderKey);

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
                            // The package dependency was harvested from the package.
                            writer.WriteAttributeString("Imported", "yes");
                        }

                        writer.WriteEndElement();
                    }

                    var packageRelatedBundles = relatedBundlesByPackagePayload[packagePayloadId];

                    foreach (var relatedBundle in packageRelatedBundles)
                    {
                        writer.WriteStartElement("RelatedBundle");
                        writer.WriteAttributeString("Id", relatedBundle.BundleId);
                        writer.WriteAttributeString("Action", relatedBundle.Action.ToString());
                        writer.WriteEndElement();
                    }

                    var packageRelatedPackages = relatedPackagesByPackagePayload[packagePayloadId];

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

                        var relatedLanguages = related.Languages?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (null != relatedLanguages && 0 < relatedLanguages.Length)
                        {
                            writer.WriteAttributeString("LangInclusive", related.LangInclusive ? "yes" : "no");
                            foreach (var language in relatedLanguages)
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
                    writer.WriteAttributeString("Id", packagePayloadId);
                    writer.WriteEndElement();

                    var packagePayloads = this.PackagesPayloads[package.PackageId];

                    foreach (var payload in packagePayloads.Values)
                    {
                        if (payload.Id.Id != packagePayloadId)
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
                        Debug.Assert(targetCode.Type == WixBundlePatchTargetCodeType.ProductCode || targetCode.Type == WixBundlePatchTargetCodeType.UpgradeCode);

                        writer.WriteStartElement("PatchTargetCode");
                        writer.WriteAttributeString("TargetCode", targetCode.TargetCode);
                        writer.WriteAttributeString("Product", targetCode.Type == WixBundlePatchTargetCodeType.ProductCode ? "yes" : "no");
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

                // Write the BootstrapperExtension elements.
                var bootstrapperExtensions = this.Section.Symbols.OfType<WixBootstrapperExtensionSymbol>();
                var uxPayloadsById = this.UXContainerPayloads.ToDictionary(p => p.Id.Id);

                foreach (var bootstrapperExtension in bootstrapperExtensions)
                {
                    var entryPayload = uxPayloadsById[bootstrapperExtension.PayloadRef];

                    writer.WriteStartElement("BootstrapperExtension");
                    writer.WriteAttributeString("Id", bootstrapperExtension.Id.Id);
                    writer.WriteAttributeString("EntryPayloadSourcePath", entryPayload.EmbeddedId);

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
                if (!String.IsNullOrEmpty(container.DownloadUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", container.DownloadUrl);
                }

                writer.WriteAttributeString("FilePath", container.Name);
            }
            else if (ContainerType.Attached == container.Type)
            {
                writer.WriteAttributeString("FilePath", executableName); // attached containers use the name of the bundle since they are attached to the executable.
                writer.WriteAttributeString("AttachedIndex", container.AttachedContainerIndex.Value.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Attached", "yes");
                writer.WriteAttributeString("Primary", "yes");
            }
        }

        private void WriteBurnManifestPayload(XmlTextWriter writer, WixBundlePayloadSymbol payload)
        {
            writer.WriteStartElement("Payload");

            writer.WriteAttributeString("Id", payload.Id.Id);
            writer.WriteAttributeString("FilePath", payload.Name);
            writer.WriteAttributeString("FileSize", payload.FileSize.Value.ToString(CultureInfo.InvariantCulture));

            if (!String.IsNullOrEmpty(payload.CertificatePublicKey) && !String.IsNullOrEmpty(payload.CertificateThumbprint))
            {
                writer.WriteAttributeString("CertificateRootPublicKeyIdentifier", payload.CertificatePublicKey);
                writer.WriteAttributeString("CertificateRootThumbprint", payload.CertificateThumbprint);
            }
            else
            {
                writer.WriteAttributeString("Hash", payload.Hash);
            }

            if (payload.LayoutOnly)
            {
                writer.WriteAttributeString("LayoutOnly", "yes");
            }

            if (!String.IsNullOrEmpty(payload.DownloadUrl))
            {
                writer.WriteAttributeString("DownloadUrl", payload.DownloadUrl);
            }

            switch (payload.Packaging)
            {
                case PackagingType.Embedded: // this means it's in a container.
                    Debug.Assert(BurnConstants.BurnUXContainerName != payload.ContainerRef);

                    writer.WriteAttributeString("Packaging", "embedded");
                    writer.WriteAttributeString("SourcePath", payload.EmbeddedId);
                    writer.WriteAttributeString("Container", payload.ContainerRef);
                    break;

                case PackagingType.External:
                    writer.WriteAttributeString("Packaging", "external");
                    writer.WriteAttributeString("SourcePath", payload.Name);
                    break;
            }

            writer.WriteEndElement();
        }

        private void WriteBurnManifestUXPayload(XmlTextWriter writer, WixBundlePayloadSymbol payload)
        {
            Debug.Assert(PackagingType.Embedded == payload.Packaging);
            Debug.Assert(BurnConstants.BurnUXContainerName == payload.ContainerRef);

            writer.WriteStartElement("Payload");

            writer.WriteAttributeString("Id", payload.Id.Id);
            writer.WriteAttributeString("FilePath", payload.Name);
            writer.WriteAttributeString("SourcePath", payload.EmbeddedId);

            writer.WriteEndElement();
        }
    }
}
