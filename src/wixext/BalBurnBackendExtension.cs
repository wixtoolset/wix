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

    public class BalBurnBackendExtension : BaseBurnBackendExtension
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
        };

        protected override IEnumerable<IntermediateSymbolDefinition> SymbolDefinitions => BurnSymbolDefinitions;

        public override void BundleFinalize()
        {
            base.BundleFinalize();

            var intermediate = this.Context.IntermediateRepresentation;
            var section = intermediate.Sections.Single();

            var baSymbol = section.Symbols.OfType<WixBootstrapperApplicationSymbol>().SingleOrDefault();
            var baId = baSymbol?.Id?.Id;
            if (null == baId)
            {
                return;
            }

            var isStdBA = baId.StartsWith("WixStandardBootstrapperApplication");
            var isMBA = baId.StartsWith("ManagedBootstrapperApplicationHost");
            var isDNC = baId.StartsWith("DotNetCoreBootstrapperApplicationHost");
            var isSCD = isDNC && this.VerifySCD(section);

            if (isStdBA || isMBA || isDNC)
            {
                this.VerifyBAFunctions(section);
            }

            if (isMBA || (isDNC && !isSCD))
            {
                this.VerifyPrereqPackages(section, isDNC);
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
                    // TODO: Make core WiX canonicalize Name (this won't catch '.\bafunctions.dll').
                    if (string.Equals(payloadPropertiesSymbol.Name, "bafunctions.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Messaging.Write(BalWarnings.UnmarkedBAFunctionsDLL(payloadPropertiesSymbol.SourceLineNumbers));
                    }
                }
            }
            else
            {
                var payloadId = baFunctionsSymbol.Id;
                var bundlePayloadSymbol = payloadPropertiesSymbols.Single(x => payloadId == x.Id);
                if (BurnConstants.BurnUXContainerName != bundlePayloadSymbol.ContainerRef)
                {
                    this.Messaging.Write(BalErrors.BAFunctionsPayloadRequiredInUXContainer(baFunctionsSymbol.SourceLineNumbers));
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
