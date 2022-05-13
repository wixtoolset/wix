// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Bal.Symbols;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    public class BalBurnBackendExtension : BaseBurnBackendBinderExtension
    {
        private static readonly IntermediateSymbolDefinition[] BurnSymbolDefinitions =
        {
            BalSymbolDefinitions.WixBalBAFactoryAssembly,
            BalSymbolDefinitions.WixBalBAFunctions,
            BalSymbolDefinitions.WixBalCondition,
            BalSymbolDefinitions.WixBalPackageInfo,
            BalSymbolDefinitions.WixDncOptions,
            BalSymbolDefinitions.WixMbaPrereqInformation,
            BalSymbolDefinitions.WixStdbaOptions,
            BalSymbolDefinitions.WixStdbaOverridableVariable,
            BalSymbolDefinitions.WixMbaPrereqOptions,
        };

        protected override IReadOnlyCollection<IntermediateSymbolDefinition> SymbolDefinitions => BurnSymbolDefinitions;

        public override void SymbolsFinalized(IntermediateSection section)
        {
            base.SymbolsFinalized(section);

            this.VerifyBalConditions(section);
            this.VerifyBalPackageInfos(section);
            this.VerifyOverridableVariables(section);

            var baSymbol = section.Symbols.OfType<WixBootstrapperApplicationDllSymbol>().SingleOrDefault();
            var baId = baSymbol?.Id?.Id;
            if (null == baId)
            {
                return;
            }

            var isStdBA = baId.StartsWith("WixStandardBootstrapperApplication");
            var isMBA = baId.StartsWith("WixManagedBootstrapperApplicationHost");
            var isDNC = baId.StartsWith("WixDotNetCoreBootstrapperApplicationHost");
            var isSCD = isDNC && this.VerifySCD(section);

            if (isDNC)
            {
                this.FinalizeBAFactorySymbol(section);
            }

            if (isStdBA || isMBA || isDNC)
            {
                this.VerifyBAFunctions(section);
            }

            if (isMBA || (isDNC && !isSCD))
            {
                this.VerifyPrereqPackages(section, isDNC);
            }
        }

        private void FinalizeBAFactorySymbol(IntermediateSection section)
        {
            var factorySymbol = section.Symbols.OfType<WixBalBAFactoryAssemblySymbol>().SingleOrDefault();
            if (null == factorySymbol)
            {
                return;
            }

            var factoryPayloadSymbol = section.Symbols.OfType<WixBundlePayloadSymbol>()
                                                      .Where(p => p.Id.Id == factorySymbol.PayloadId)
                                                      .SingleOrDefault();
            if (null == factoryPayloadSymbol)
            {
                return;
            }

            factorySymbol.FilePath = factoryPayloadSymbol.Name;
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

        private void VerifyBalPackageInfos(IntermediateSection section)
        {
            var balPackageInfoSymbols = section.Symbols.OfType<WixBalPackageInfoSymbol>().ToList();
            foreach (var balPackageInfoSymbol in balPackageInfoSymbols)
            {
                this.BackendHelper.ValidateBundleCondition(balPackageInfoSymbol.SourceLineNumbers, "*Package", "bal:DisplayInternalUICondition", balPackageInfoSymbol.DisplayInternalUICondition, BundleConditionPhase.Plan);
            }
        }

        private void VerifyOverridableVariables(IntermediateSection section)
        {
            var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
            if (bundleSymbol.CommandLineVariables != WixBundleCommandLineVariables.UpperCase)
            {
                return;
            }

            var overridableVariableSymbols = section.Symbols.OfType<WixStdbaOverridableVariableSymbol>().ToList();
            foreach (var overridableVariableSymbol in overridableVariableSymbols)
            {
                var upperName = overridableVariableSymbol.Name.ToUpperInvariant();
                if (upperName != overridableVariableSymbol.Name)
                {
                    this.Messaging.Write(BalErrors.NonUpperCaseOverridableVariable(overridableVariableSymbol.SourceLineNumbers, overridableVariableSymbol.Name, upperName));
                }
            }
        }

        private void VerifyPrereqPackages(IntermediateSection section, bool isDNC)
        {
            var prereqInfoSymbols = section.Symbols.OfType<WixMbaPrereqInformationSymbol>().ToList();
            if (prereqInfoSymbols.Count == 0)
            {
                var message = isDNC ? BalErrors.MissingDNCPrereq() : BalErrors.MissingMBAPrereq();
                this.Messaging.Write(message);
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

        private bool VerifySCD(IntermediateSection section)
        {
            var isSCD = false;

            var dncOptions = section.Symbols.OfType<WixDncOptionsSymbol>().SingleOrDefault();
            if (dncOptions != null)
            {
                isSCD = dncOptions.SelfContainedDeployment != 0;
            }

            return isSCD;
        }
    }
}
