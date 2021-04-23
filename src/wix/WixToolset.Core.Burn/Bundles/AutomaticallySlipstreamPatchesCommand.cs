// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class AutomaticallySlipstreamPatchesCommand
    {
        public AutomaticallySlipstreamPatchesCommand(IntermediateSection section, ICollection<PackageFacade> packageFacades)
        {
            this.Section = section;
            this.PackageFacades = packageFacades;
        }

        private IntermediateSection Section { get; }

        private IEnumerable<PackageFacade> PackageFacades { get; }

        public void Execute()
        {
            var msiPackages = new List<WixBundleMsiPackageSymbol>();
            var targetsProductCode = new Dictionary<string, List<WixBundlePatchTargetCodeSymbol>>();
            var targetsUpgradeCode = new Dictionary<string, List<WixBundlePatchTargetCodeSymbol>>();

            foreach (var facade in this.PackageFacades)
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
                        .Where(r => r.PackageRef == facade.PackageId);

                    // Index target ProductCodes and UpgradeCodes for slipstreamed MSPs.
                    foreach (var symbol in patchTargetCodeSymbols)
                    {
                        if (symbol.TargetsProductCode)
                        {
                            if (!targetsProductCode.TryGetValue(symbol.TargetCode, out var symbols))
                            {
                                symbols = new List<WixBundlePatchTargetCodeSymbol>();
                                targetsProductCode.Add(symbol.TargetCode, symbols);
                            }

                            symbols.Add(symbol);
                        }
                        else if (symbol.TargetsUpgradeCode)
                        {
                            if (!targetsUpgradeCode.TryGetValue(symbol.TargetCode, out var symbols))
                            {
                                symbols = new List<WixBundlePatchTargetCodeSymbol>();
                                targetsUpgradeCode.Add(symbol.TargetCode, symbols);
                            }
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
                        Debug.Assert(symbol.TargetsProductCode);
                        Debug.Assert(!symbol.TargetsUpgradeCode);

                        this.TryAddSlipstreamSymbol(slipstreamMspIds, msi, symbol);
                    }
                }

                if (!String.IsNullOrEmpty(msi.UpgradeCode) && targetsUpgradeCode.TryGetValue(msi.UpgradeCode, out symbols))
                {
                    foreach (var symbol in symbols)
                    {
                        Debug.Assert(!symbol.TargetsProductCode);
                        Debug.Assert(symbol.TargetsUpgradeCode);

                        this.TryAddSlipstreamSymbol(slipstreamMspIds, msi, symbol);
                    }

                    symbols = null;
                }
            }
        }

        private bool TryAddSlipstreamSymbol(HashSet<string> slipstreamMspIds, WixBundleMsiPackageSymbol msiPackage, WixBundlePatchTargetCodeSymbol patchTargetCode)
        {
            var id = new Identifier(AccessModifier.Section, msiPackage.Id.Id, patchTargetCode.PackageRef);

            if (slipstreamMspIds.Add(id.Id))
            {
                this.Section.AddSymbol(new WixBundleSlipstreamMspSymbol(patchTargetCode.SourceLineNumbers)
                {
                    TargetPackageRef = msiPackage.Id.Id,
                    MspPackageRef = patchTargetCode.PackageRef,
                });

                return true;
            }

            return false;
        }
    }
}
