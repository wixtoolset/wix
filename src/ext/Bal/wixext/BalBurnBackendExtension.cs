// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using WixToolset.BootstrapperApplications.Symbols;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    public class BalBurnBackendExtension : BaseBurnBackendBinderExtension
    {
        private static readonly IntermediateSymbolDefinition[] BurnSymbolDefinitions =
        {
#pragma warning disable 0612 // obsolete
            BalSymbolDefinitions.WixBalBAFactoryAssembly,
#pragma warning restore 0612
            BalSymbolDefinitions.WixBalBAFunctions,
            BalSymbolDefinitions.WixBalCondition,
            BalSymbolDefinitions.WixBalPackageInfo,
            BalSymbolDefinitions.WixPrereqInformation,
            BalSymbolDefinitions.WixStdbaCommandLine,
            BalSymbolDefinitions.WixStdbaOptions,
            BalSymbolDefinitions.WixStdbaOverridableVariable,
            BalSymbolDefinitions.WixPrereqOptions,
        };

        protected override IReadOnlyCollection<IntermediateSymbolDefinition> SymbolDefinitions => BurnSymbolDefinitions;

        public override bool TryProcessSymbol(IntermediateSection section, IntermediateSymbol symbol)
        {
            if (symbol is WixBalPackageInfoSymbol balPackageInfoSymbol)
            {
                // There might be a more efficient way to do this,
                // but this is an easy way to ensure we're creating valid XML.
                var sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb))
                {
                    writer.WriteStartElement(symbol.Definition.Name, BurnConstants.BootstrapperApplicationDataNamespace);

                    writer.WriteAttributeString("PackageId", balPackageInfoSymbol.PackageId);

                    if (balPackageInfoSymbol.DisplayInternalUICondition != null)
                    {
                        writer.WriteAttributeString("DisplayInternalUICondition", balPackageInfoSymbol.DisplayInternalUICondition);
                    }

                    if (balPackageInfoSymbol.DisplayFilesInUseDialogCondition != null)
                    {
                        writer.WriteAttributeString("DisplayFilesInUseDialogCondition", balPackageInfoSymbol.DisplayFilesInUseDialogCondition);
                    }

                    if (balPackageInfoSymbol.PrimaryPackageType != BalPrimaryPackageType.None)
                    {
                        writer.WriteAttributeString("PrimaryPackageType", balPackageInfoSymbol.PrimaryPackageType.ToString().ToLower());
                    }

                    writer.WriteEndElement();
                }

                this.BackendHelper.AddBootstrapperApplicationData(sb.ToString());

                return true;
            }
            else if (symbol is WixStdbaCommandLineSymbol stdbaCommandLineSymbol)
            {
                var sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb))
                {
                    writer.WriteStartElement(symbol.Definition.Name, BurnConstants.BootstrapperApplicationDataNamespace);

                    switch (stdbaCommandLineSymbol.VariableType)
                    {
                        case WixStdbaCommandLineVariableType.CaseInsensitive:
                            writer.WriteAttributeString("VariableType", "caseInsensitive");
                            break;
                        default:
                            writer.WriteAttributeString("VariableType", "caseSensitive");
                            break;
                    }

                    writer.WriteEndElement();
                }

                this.BackendHelper.AddBootstrapperApplicationData(sb.ToString());

                return true;
            }
            else if (symbol is WixBalBootstrapperApplicationSymbol)
            {
                // This symbol is only for the processing in SymbolsFinalized.
                return true;
            }
            else
            {
                return base.TryProcessSymbol(section, symbol);
            }
        }

        public override void SymbolsFinalized(IntermediateSection section)
        {
            base.SymbolsFinalized(section);

            this.VerifyBalConditions(section);
            this.VerifyDisplayInternalUICondition(section);
            this.VerifyDisplayFilesInUseDialogCondition(section);
            this.VerifyOverridableVariables(section);

            var balBaSymbol = section.Symbols.OfType<WixBalBootstrapperApplicationSymbol>().SingleOrDefault();
            if (balBaSymbol == null)
            {
                return;
            }

            var isIuiBA = balBaSymbol.Type == WixBalBootstrapperApplicationType.InternalUi;
            var isPreqBA = balBaSymbol.Type == WixBalBootstrapperApplicationType.Prerequisite;
            var isStdBA = balBaSymbol.Type == WixBalBootstrapperApplicationType.Standard;

            if (!isIuiBA && !isPreqBA && !isStdBA)
            {
                throw new WixException($"Invalid WixBalBootstrapperApplicationType: '{balBaSymbol.Type}'");
            }

            this.VerifyBAFunctions(section);

            if (isIuiBA)
            {
                // This needs to happen before VerifyPrereqPackages because it can add prereq packages.
                this.VerifyPrimaryPackages(section, balBaSymbol.SourceLineNumbers);
            }

            if (isIuiBA || isPreqBA)
            {
                this.VerifyPrereqPackages(section, balBaSymbol.SourceLineNumbers, isIuiBA);
            }
        }

        private void VerifyBAFunctions(IntermediateSection section)
        {
            WixBalBAFunctionsSymbol baFunctionsSymbol = null;
            foreach (var symbol in section.Symbols.OfType<WixBalBAFunctionsSymbol>())
            {
                if (null == baFunctionsSymbol)
                {
                    baFunctionsSymbol = symbol;
                }
                else
                {
                    this.Messaging.Write(BalErrors.MultipleBAFunctions(symbol.SourceLineNumbers));
                }
            }

            var payloadPropertiesSymbols = section.Symbols.OfType<WixBundlePayloadSymbol>().ToList();
            if (null == baFunctionsSymbol)
            {
                foreach (var payloadPropertiesSymbol in payloadPropertiesSymbols)
                {
                    if (String.Equals(payloadPropertiesSymbol.Name, "bafunctions.dll", StringComparison.OrdinalIgnoreCase) &&
                        BurnConstants.BurnUXContainerName == payloadPropertiesSymbol.ContainerRef)
                    {
                        this.Messaging.Write(BalWarnings.UnmarkedBAFunctionsDLL(payloadPropertiesSymbol.SourceLineNumbers));
                    }
                }
            }
            else
            {
                var payloadId = baFunctionsSymbol.PayloadId;
                var bundlePayloadSymbol = payloadPropertiesSymbols.Single(x => payloadId == x.Id.Id);
                if (BurnConstants.BurnUXContainerName != bundlePayloadSymbol.ContainerRef)
                {
                    this.Messaging.Write(BalErrors.BAFunctionsPayloadRequiredInUXContainer(baFunctionsSymbol.SourceLineNumbers));
                }

                baFunctionsSymbol.FilePath = bundlePayloadSymbol.Name;
            }
        }

        private void VerifyBalConditions(IntermediateSection section)
        {
            var balConditionSymbols = section.Symbols.OfType<WixBalConditionSymbol>().ToList();
            foreach (var balConditionSymbol in balConditionSymbols)
            {
                this.BackendHelper.ValidateBundleCondition(balConditionSymbol.SourceLineNumbers, "bal:Condition", "Condition", balConditionSymbol.Condition, BundleConditionPhase.Detect);
            }
        }

        private void VerifyDisplayInternalUICondition(IntermediateSection section)
        {
            foreach (var balPackageInfoSymbol in section.Symbols.OfType<WixBalPackageInfoSymbol>().ToList())
            {
                if (balPackageInfoSymbol.DisplayInternalUICondition != null)
                {
                    this.BackendHelper.ValidateBundleCondition(balPackageInfoSymbol.SourceLineNumbers, "*Package", "bal:DisplayInternalUICondition", balPackageInfoSymbol.DisplayInternalUICondition, BundleConditionPhase.Plan);
                }
            }
        }

        private void VerifyDisplayFilesInUseDialogCondition(IntermediateSection section)
        {
            foreach (var balPackageInfoSymbol in section.Symbols.OfType<WixBalPackageInfoSymbol>().ToList())
            {
                if (balPackageInfoSymbol.DisplayFilesInUseDialogCondition != null)
                {
                    this.BackendHelper.ValidateBundleCondition(balPackageInfoSymbol.SourceLineNumbers, "*Package", "bal:DisplayFilesInUseDialogCondition", balPackageInfoSymbol.DisplayFilesInUseDialogCondition, BundleConditionPhase.Plan);
                }
            }
        }

        private void VerifyPrimaryPackages(IntermediateSection section, SourceLineNumber baSourceLineNumbers)
        {
            WixBalPackageInfoSymbol defaultPrimaryPackage = null;
            WixBalPackageInfoSymbol x86PrimaryPackage = null;
            WixBalPackageInfoSymbol x64PrimaryPackage = null;
            WixBalPackageInfoSymbol arm64PrimaryPackage = null;
            var nonPermanentNonPrimaryPackages = new List<WixBundlePackageSymbol>();

            var balPackageInfoSymbolsByPackageId = section.Symbols.OfType<WixBalPackageInfoSymbol>().ToDictionary(x => x.PackageId);
            var mbaPrereqInfoSymbolsByPackageId = section.Symbols.OfType<WixPrereqInformationSymbol>().ToDictionary(x => x.PackageId);
            var msiPackageSymbolsByPackageId = section.Symbols.OfType<WixBundleMsiPackageSymbol>().ToDictionary(x => x.Id.Id);
            var packageSymbols = section.Symbols.OfType<WixBundlePackageSymbol>().ToList();
            foreach (var packageSymbol in packageSymbols)
            {
                var packageId = packageSymbol.Id?.Id;
                var isPrereq = false;
                var primaryPackageType = BalPrimaryPackageType.None;

                if (mbaPrereqInfoSymbolsByPackageId.TryGetValue(packageId, out var _))
                {
                    isPrereq = true;
                }

                if (balPackageInfoSymbolsByPackageId.TryGetValue(packageId, out var balPackageInfoSymbol))
                {
                    primaryPackageType = balPackageInfoSymbol.PrimaryPackageType;
                }

                if (packageSymbol.Permanent)
                {
                    if (primaryPackageType != BalPrimaryPackageType.None)
                    {
                        this.Messaging.Write(BalErrors.IuibaPermanentPrimaryPackageType(packageSymbol.SourceLineNumbers));
                    }
                    else
                    {
                        if (!isPrereq)
                        {
                            var prereqInfoSymbol = section.AddSymbol(new WixPrereqInformationSymbol(packageSymbol.SourceLineNumbers, new Identifier(AccessModifier.Global, packageId))
                            {
                                PackageId = packageId,
                            });

                            mbaPrereqInfoSymbolsByPackageId.Add(packageId, prereqInfoSymbol);
                        }

                        this.VerifyIuibaPrereqPackage(packageSymbol);
                    }
                }
                else
                {
                    if (isPrereq)
                    {
                        if (primaryPackageType == BalPrimaryPackageType.None)
                        {
                            this.Messaging.Write(BalErrors.IuibaNonPermanentPrereqPackage(packageSymbol.SourceLineNumbers));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(
                                packageSymbol.SourceLineNumbers,
                                packageSymbol.Type + "Package",
                                "PrereqPackage",
                                "yes",
                                "PrimaryPackageType"));
                        }
                    }
                    else if (primaryPackageType == BalPrimaryPackageType.None)
                    {
                        nonPermanentNonPrimaryPackages.Add(packageSymbol);
                    }
                    else if (packageSymbol.Type != WixBundlePackageType.Msi)
                    {
                        this.Messaging.Write(BalErrors.IuibaNonMsiPrimaryPackage(packageSymbol.SourceLineNumbers));
                    }
                    else if (!msiPackageSymbolsByPackageId.TryGetValue(packageId, out var msiPackageSymbol))
                    {
                        throw new WixException($"Missing WixBundleMsiPackageSymbol for package '{packageId}'");
                    }
                    else if (msiPackageSymbol.EnableFeatureSelection)
                    {
                        this.Messaging.Write(BalErrors.IuibaPrimaryPackageEnableFeatureSelection(packageSymbol.SourceLineNumbers));
                    }
                    else
                    {
                        if (primaryPackageType == BalPrimaryPackageType.Default)
                        {
                            if (defaultPrimaryPackage == null)
                            {
                                defaultPrimaryPackage = balPackageInfoSymbol;
                            }
                            else
                            {
                                this.Messaging.Write(BalErrors.MultiplePrimaryPackageType(balPackageInfoSymbol.SourceLineNumbers, "default"));
                                this.Messaging.Write(BalErrors.MultiplePrimaryPackageType2(defaultPrimaryPackage.SourceLineNumbers));
                            }
                        }
                        else if (balPackageInfoSymbol.PrimaryPackageType == BalPrimaryPackageType.X86)
                        {
                            if (x86PrimaryPackage == null)
                            {
                                x86PrimaryPackage = balPackageInfoSymbol;
                            }
                            else
                            {
                                this.Messaging.Write(BalErrors.MultiplePrimaryPackageType(balPackageInfoSymbol.SourceLineNumbers, "x86"));
                                this.Messaging.Write(BalErrors.MultiplePrimaryPackageType2(x86PrimaryPackage.SourceLineNumbers));
                            }
                        }
                        else if (balPackageInfoSymbol.PrimaryPackageType == BalPrimaryPackageType.X64)
                        {
                            if (x64PrimaryPackage == null)
                            {
                                x64PrimaryPackage = balPackageInfoSymbol;
                            }
                            else
                            {
                                this.Messaging.Write(BalErrors.MultiplePrimaryPackageType(balPackageInfoSymbol.SourceLineNumbers, "x64"));
                                this.Messaging.Write(BalErrors.MultiplePrimaryPackageType2(x64PrimaryPackage.SourceLineNumbers));
                            }
                        }
                        else if (balPackageInfoSymbol.PrimaryPackageType == BalPrimaryPackageType.ARM64)
                        {
                            if (arm64PrimaryPackage == null)
                            {
                                arm64PrimaryPackage = balPackageInfoSymbol;
                            }
                            else
                            {
                                this.Messaging.Write(BalErrors.MultiplePrimaryPackageType(balPackageInfoSymbol.SourceLineNumbers, "arm64"));
                                this.Messaging.Write(BalErrors.MultiplePrimaryPackageType2(arm64PrimaryPackage.SourceLineNumbers));
                            }
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }

                        this.VerifyIuibaPrimaryPackage(packageSymbol, balPackageInfoSymbol);
                    }
                }
            }

            if (defaultPrimaryPackage == null && nonPermanentNonPrimaryPackages.Count == 1)
            {
                var packageSymbol = nonPermanentNonPrimaryPackages[0];

                if (packageSymbol.Type == WixBundlePackageType.Msi)
                {
                    var packageId = packageSymbol.Id?.Id;
                    var msiPackageSymbol = section.Symbols.OfType<WixBundleMsiPackageSymbol>()
                                                  .SingleOrDefault(x => x.Id.Id == packageId);
                    if (!msiPackageSymbol.EnableFeatureSelection)
                    {
                        if (!balPackageInfoSymbolsByPackageId.TryGetValue(packageId, out var balPackageInfoSymbol))
                        {
                            balPackageInfoSymbol = section.AddSymbol(new WixBalPackageInfoSymbol(packageSymbol.SourceLineNumbers, new Identifier(AccessModifier.Global, packageId))
                            {
                                PackageId = packageId,
                            });

                            balPackageInfoSymbolsByPackageId.Add(packageId, balPackageInfoSymbol);
                        }

                        balPackageInfoSymbol.PrimaryPackageType = BalPrimaryPackageType.Default;
                        defaultPrimaryPackage = balPackageInfoSymbol;
                        nonPermanentNonPrimaryPackages.RemoveAt(0);

                        this.VerifyIuibaPrimaryPackage(packageSymbol, balPackageInfoSymbol);
                    }
                }
            }

            if (nonPermanentNonPrimaryPackages.Count > 0)
            {
                foreach (var packageSymbol in nonPermanentNonPrimaryPackages)
                {
                    this.Messaging.Write(BalErrors.IuibaNonPermanentNonPrimaryPackage(packageSymbol.SourceLineNumbers));
                }
            }
            else if (defaultPrimaryPackage == null)
            {
                this.Messaging.Write(BalErrors.MissingIUIPrimaryPackage(baSourceLineNumbers));
            }
            else
            {
                var foundPrimaryPackage = false;
                var chainPackageGroupSymbols = section.Symbols.OfType<WixGroupSymbol>()
                                                              .Where(x => x.ChildType == ComplexReferenceChildType.Package &&
                                                                          x.ParentType == ComplexReferenceParentType.PackageGroup &&
                                                                          x.ParentId == BurnConstants.BundleChainPackageGroupId);
                foreach (var chainPackageGroupSymbol in chainPackageGroupSymbols)
                {
                    var packageId = chainPackageGroupSymbol.ChildId;
                    if (balPackageInfoSymbolsByPackageId.TryGetValue(packageId, out var balPackageInfo) && balPackageInfo.PrimaryPackageType != BalPrimaryPackageType.None)
                    {
                        foundPrimaryPackage = true;
                    }
                    else if (foundPrimaryPackage && mbaPrereqInfoSymbolsByPackageId.TryGetValue(packageId, out var mbaPrereqInformationSymbol))
                    {
                        this.Messaging.Write(BalWarnings.IuibaPrereqPackageAfterPrimaryPackage(chainPackageGroupSymbol.SourceLineNumbers));
                    }
                }
            }
        }

        private void VerifyIuibaPrereqPackage(WixBundlePackageSymbol packageSymbol)
        {
            if (packageSymbol.Cache == BundleCacheType.Force)
            {
                this.Messaging.Write(BalWarnings.IuibaForceCachePrereq(packageSymbol.SourceLineNumbers));
            }
        }

        private void VerifyIuibaPrimaryPackage(WixBundlePackageSymbol packageSymbol, WixBalPackageInfoSymbol balPackageInfoSymbol)
        {
            if (packageSymbol.InstallCondition != null)
            {
                this.Messaging.Write(BalWarnings.IuibaPrimaryPackageInstallCondition(packageSymbol.SourceLineNumbers));
            }

            if (balPackageInfoSymbol.DisplayInternalUICondition != null)
            {
                this.Messaging.Write(BalWarnings.IuibaPrimaryPackageDisplayInternalUICondition(packageSymbol.SourceLineNumbers));
            }

            if (balPackageInfoSymbol.DisplayFilesInUseDialogCondition != null)
            {
                this.Messaging.Write(BalWarnings.IuibaPrimaryPackageDisplayFilesInUseDialogCondition(packageSymbol.SourceLineNumbers));
            }
        }

        private void VerifyOverridableVariables(IntermediateSection section)
        {
            var commandLineSymbol = section.Symbols.OfType<WixStdbaCommandLineSymbol>().SingleOrDefault();
            if (commandLineSymbol?.VariableType != WixStdbaCommandLineVariableType.CaseInsensitive)
            {
                return;
            }

            var overridableVariableSymbols = section.Symbols.OfType<WixStdbaOverridableVariableSymbol>().ToList();
            var overridableVariables = new Dictionary<string, WixStdbaOverridableVariableSymbol>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var overridableVariableSymbol in overridableVariableSymbols)
            {
                if (!overridableVariables.TryGetValue(overridableVariableSymbol.Name, out var collisionVariableSymbol))
                {
                    overridableVariables.Add(overridableVariableSymbol.Name, overridableVariableSymbol);
                }
                else
                {
                    this.Messaging.Write(BalErrors.OverridableVariableCollision(overridableVariableSymbol.SourceLineNumbers, overridableVariableSymbol.Name, collisionVariableSymbol.Name));
                    this.Messaging.Write(BalErrors.OverridableVariableCollision2(collisionVariableSymbol.SourceLineNumbers));
                }
            }
        }

        private void VerifyPrereqPackages(IntermediateSection section, SourceLineNumber baSourceLineNumbers, bool isIuiBA)
        {
            var prereqInfoSymbols = section.Symbols.OfType<WixPrereqInformationSymbol>().ToList();
            if (!isIuiBA && prereqInfoSymbols.Count == 0)
            {
                this.Messaging.Write(BalErrors.MissingPrereq(baSourceLineNumbers));
                return;
            }

            var foundLicenseFile = false;
            var foundLicenseUrl = false;

            foreach (var prereqInfoSymbol in prereqInfoSymbols)
            {
                if (null != prereqInfoSymbol.LicenseFile)
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Messaging.Write(BalErrors.MultiplePrereqLicenses(prereqInfoSymbol.SourceLineNumbers));
                        return;
                    }

                    foundLicenseFile = true;
                }

                if (null != prereqInfoSymbol.LicenseUrl)
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Messaging.Write(BalErrors.MultiplePrereqLicenses(prereqInfoSymbol.SourceLineNumbers));
                        return;
                    }

                    foundLicenseUrl = true;
                }
            }
        }
    }
}
