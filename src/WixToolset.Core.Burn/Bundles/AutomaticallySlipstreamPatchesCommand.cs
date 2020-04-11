// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

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
            var msiPackages = new List<WixBundleMsiPackageTuple>();
            var targetsProductCode = new Dictionary<string, List<WixBundlePatchTargetCodeTuple>>();
            var targetsUpgradeCode = new Dictionary<string, List<WixBundlePatchTargetCodeTuple>>();

            foreach (var facade in this.PackageFacades)
            {
                // Keep track of all MSI packages.
                if (facade.SpecificPackageTuple is WixBundleMsiPackageTuple msiPackage)
                {
                    msiPackages.Add(msiPackage);
                }
                else if (facade.SpecificPackageTuple is WixBundleMspPackageTuple mspPackage && mspPackage.Slipstream)
                {
                    var patchTargetCodeTuples = this.Section.Tuples
                        .OfType<WixBundlePatchTargetCodeTuple>()
                        .Where(r => r.PackageRef == facade.PackageId);

                    // Index target ProductCodes and UpgradeCodes for slipstreamed MSPs.
                    foreach (var tuple in patchTargetCodeTuples)
                    {
                        if (tuple.TargetsProductCode)
                        {
                            if (!targetsProductCode.TryGetValue(tuple.TargetCode, out var tuples))
                            {
                                tuples = new List<WixBundlePatchTargetCodeTuple>();
                                targetsProductCode.Add(tuple.TargetCode, tuples);
                            }

                            tuples.Add(tuple);
                        }
                        else if (tuple.TargetsUpgradeCode)
                        {
                            if (!targetsUpgradeCode.TryGetValue(tuple.TargetCode, out var tuples))
                            {
                                tuples = new List<WixBundlePatchTargetCodeTuple>();
                                targetsUpgradeCode.Add(tuple.TargetCode, tuples);
                            }
                        }
                    }
                }
            }

            var slipstreamMspIds = new HashSet<string>();

            // Loop through the MSI and slipstream patches targeting it.
            foreach (var msi in msiPackages)
            {
                if (targetsProductCode.TryGetValue(msi.ProductCode, out var tuples))
                {
                    foreach (var tuple in tuples)
                    {
                        Debug.Assert(tuple.TargetsProductCode);
                        Debug.Assert(!tuple.TargetsUpgradeCode);

                        this.TryAddSlipstreamTuple(slipstreamMspIds, msi, tuple);
                    }
                }

                if (!String.IsNullOrEmpty(msi.UpgradeCode) && targetsUpgradeCode.TryGetValue(msi.UpgradeCode, out tuples))
                {
                    foreach (var tuple in tuples)
                    {
                        Debug.Assert(!tuple.TargetsProductCode);
                        Debug.Assert(tuple.TargetsUpgradeCode);

                        this.TryAddSlipstreamTuple(slipstreamMspIds, msi, tuple);
                    }

                    tuples = null;
                }
            }
        }

        private bool TryAddSlipstreamTuple(HashSet<string> slipstreamMspIds, WixBundleMsiPackageTuple msiPackage, WixBundlePatchTargetCodeTuple patchTargetCode)
        {
            var id = new Identifier(AccessModifier.Private, msiPackage.Id.Id, patchTargetCode.PackageRef);

            if (slipstreamMspIds.Add(id.Id))
            {
                this.Section.AddTuple(new WixBundleSlipstreamMspTuple(patchTargetCode.SourceLineNumbers)
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
