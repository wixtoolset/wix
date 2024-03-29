// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class AutomaticallySlipstreamPatchesCommand
    {
        public AutomaticallySlipstreamPatchesCommand(IMessaging messaging, IntermediateSection section, PackageFacades packageFacades)
        {
            this.Messaging = messaging;
            this.Section = section;
            this.PackageFacades = packageFacades;
        }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        private PackageFacades PackageFacades { get; }

        public void Execute()
        {
            var msiPackages = new List<WixBundleMsiPackageSymbol>();
            var targetsProductCode = new Dictionary<string, List<WixBundlePatchTargetCodeSymbol>>();
            var targetsUpgradeCode = new Dictionary<string, List<WixBundlePatchTargetCodeSymbol>>();

            foreach (var facade in this.PackageFacades.Values)
            {
                // Keep track of all MSI packages.
                if (facade.SpecificPackageSymbol is WixBundleMsiPackageSymbol msiPackage)
                {
                    msiPackages.Add(msiPackage);
                }
                else if (facade.SpecificPackageSymbol is WixBundleMspPackageSymbol mspPackage && mspPackage.Slipstream)
                {
                    var patchTargetCodeSymbols = this.Section.Symbols
                        .OfType<WixBundlePatchTargetCodeSymbol>()
                        .Where(r => r.PackagePayloadRef == facade.PackageSymbol.PayloadRef);

                    // Index target ProductCodes and UpgradeCodes for slipstreamed MSPs.
                    foreach (var symbol in patchTargetCodeSymbols)
                    {
                        if (symbol.Type == WixBundlePatchTargetCodeType.ProductCode)
                        {
                            if (!targetsProductCode.TryGetValue(symbol.TargetCode, out var symbols))
                            {
                                symbols = new List<WixBundlePatchTargetCodeSymbol>();
                                targetsProductCode.Add(symbol.TargetCode, symbols);
                            }

                            symbols.Add(symbol);
                        }
                        else if (symbol.Type == WixBundlePatchTargetCodeType.UpgradeCode)
                        {
                            if (!targetsUpgradeCode.TryGetValue(symbol.TargetCode, out var symbols))
                            {
                                symbols = new List<WixBundlePatchTargetCodeSymbol>();
                                targetsUpgradeCode.Add(symbol.TargetCode, symbols);
                            }

                            symbols.Add(symbol);
                        }
                    }
                }
            }

            var slipstreamMspIds = new HashSet<string>();

            // Loop through the MSI and slipstream patches targeting it.
            foreach (var msi in msiPackages)
            {
                if (targetsProductCode.TryGetValue(msi.ProductCode, out var symbols))
                {
                    foreach (var symbol in symbols)
                    {
                        this.TryAddSlipstreamSymbol(slipstreamMspIds, msi, symbol);
                    }
                }

                if (!String.IsNullOrEmpty(msi.UpgradeCode) && targetsUpgradeCode.TryGetValue(msi.UpgradeCode, out symbols))
                {
                    foreach (var symbol in symbols)
                    {
                        this.TryAddSlipstreamSymbol(slipstreamMspIds, msi, symbol);
                    }
                }
            }
        }

        private void TryAddSlipstreamSymbol(HashSet<string> slipstreamMspIds, WixBundleMsiPackageSymbol msiPackage, WixBundlePatchTargetCodeSymbol patchTargetCode)
        {
            if (!this.PackageFacades.TryGetFacadesByPackagePayloadId(patchTargetCode.PackagePayloadRef, out var packageFacades))
            {
                this.Messaging.Write(ErrorMessages.IdentifierNotFound("Package.PayloadRef", patchTargetCode.PackagePayloadRef));
                return;
            }

            foreach (var packageFacade in packageFacades)
            {
                var id = new Identifier(AccessModifier.Section, msiPackage.Id.Id, packageFacade.PackageId);

                if (slipstreamMspIds.Add(id.Id))
                {
                    this.Section.AddSymbol(new WixBundleSlipstreamMspSymbol(patchTargetCode.SourceLineNumbers)
                    {
                        TargetPackageRef = msiPackage.Id.Id,
                        MspPackageRef = packageFacade.PackageId,
                    });
                }
            }
        }
    }
}
