// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class AutomaticallySlipstreamPatchesCommand : ICommand
    {
        public IEnumerable<PackageFacade> PackageFacades { private get; set; }

        public Table WixBundlePatchTargetCodeTable { private get; set; }

        public Table SlipstreamMspTable { private get; set; }

        public void Execute()
        {
            List<WixBundleMsiPackageRow> msiPackages = new List<WixBundleMsiPackageRow>();
            Dictionary<string, List<WixBundlePatchTargetCodeRow>> targetsProductCode = new Dictionary<string, List<WixBundlePatchTargetCodeRow>>();
            Dictionary<string, List<WixBundlePatchTargetCodeRow>> targetsUpgradeCode = new Dictionary<string, List<WixBundlePatchTargetCodeRow>>();

            foreach (PackageFacade facade in this.PackageFacades)
            {
                if (WixBundlePackageType.Msi == facade.Package.Type)
                {
                    // Keep track of all MSI packages.
                    msiPackages.Add(facade.MsiPackage);
                }
                else if (WixBundlePackageType.Msp == facade.Package.Type && facade.MspPackage.Slipstream)
                {
                    IEnumerable<WixBundlePatchTargetCodeRow> patchTargetCodeRows = this.WixBundlePatchTargetCodeTable.RowsAs<WixBundlePatchTargetCodeRow>().Where(r => r.MspPackageId == facade.Package.WixChainItemId);

                    // Index target ProductCodes and UpgradeCodes for slipstreamed MSPs.
                    foreach (WixBundlePatchTargetCodeRow row in patchTargetCodeRows)
                    {
                        if (row.TargetsProductCode)
                        {
                            List<WixBundlePatchTargetCodeRow> rows;
                            if (!targetsProductCode.TryGetValue(row.TargetCode, out rows))
                            {
                                rows = new List<WixBundlePatchTargetCodeRow>();
                                targetsProductCode.Add(row.TargetCode, rows);
                            }

                            rows.Add(row);
                        }
                        else if (row.TargetsUpgradeCode)
                        {
                            List<WixBundlePatchTargetCodeRow> rows;
                            if (!targetsUpgradeCode.TryGetValue(row.TargetCode, out rows))
                            {
                                rows = new List<WixBundlePatchTargetCodeRow>();
                                targetsUpgradeCode.Add(row.TargetCode, rows);
                            }
                        }
                    }
                }
            }

            RowIndexedList<Row> slipstreamMspRows = new RowIndexedList<Row>(SlipstreamMspTable);

            // Loop through the MSI and slipstream patches targeting it.
            foreach (WixBundleMsiPackageRow msi in msiPackages)
            {
                List<WixBundlePatchTargetCodeRow> rows;
                if (targetsProductCode.TryGetValue(msi.ProductCode, out rows))
                {
                    foreach (WixBundlePatchTargetCodeRow row in rows)
                    {
                        Debug.Assert(row.TargetsProductCode);
                        Debug.Assert(!row.TargetsUpgradeCode);

                        Row slipstreamMspRow = SlipstreamMspTable.CreateRow(row.SourceLineNumbers, false);
                        slipstreamMspRow[0] = msi.ChainPackageId;
                        slipstreamMspRow[1] = row.MspPackageId;

                        if (slipstreamMspRows.TryAdd(slipstreamMspRow))
                        {
                            SlipstreamMspTable.Rows.Add(slipstreamMspRow);
                        }
                    }

                    rows = null;
                }

                if (!String.IsNullOrEmpty(msi.UpgradeCode) && targetsUpgradeCode.TryGetValue(msi.UpgradeCode, out rows))
                {
                    foreach (WixBundlePatchTargetCodeRow row in rows)
                    {
                        Debug.Assert(!row.TargetsProductCode);
                        Debug.Assert(row.TargetsUpgradeCode);

                        Row slipstreamMspRow = SlipstreamMspTable.CreateRow(row.SourceLineNumbers, false);
                        slipstreamMspRow[0] = msi.ChainPackageId;
                        slipstreamMspRow[1] = row.MspPackageId;

                        if (slipstreamMspRows.TryAdd(slipstreamMspRow))
                        {
                            SlipstreamMspTable.Rows.Add(slipstreamMspRow);
                        }
                    }

                    rows = null;
                }
            }
        }
    }
}
