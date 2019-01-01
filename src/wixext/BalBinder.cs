// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WixToolset;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    public class BalBinder : BinderExtension
    {
        public override void Finish(Output output)
        {
            // Only process Bundles.
            if (OutputType.Bundle != output.Type)
            {
                return;
            }

            Table baTable = output.Tables["WixBootstrapperApplication"];
            Row baRow = baTable.Rows[0];
            string baId = (string)baRow[0];
            if (null == baId)
            {
                return;
            }

            bool isStdBA = baId.StartsWith("WixStandardBootstrapperApplication");
            bool isMBA = baId.StartsWith("ManagedBootstrapperApplicationHost");

            if (isStdBA || isMBA)
            {
                VerifyBAFunctions(output);
            }

            if (isMBA)
            {
                VerifyPrereqPackages(output);
            }
        }

        private void VerifyBAFunctions(Output output)
        {
            Row baFunctionsRow = null;
            Table baFunctionsTable = output.Tables["WixBalBAFunctions"];
            foreach (Row row in baFunctionsTable.RowsAs<Row>())
            {
                if (null == baFunctionsRow)
                {
                    baFunctionsRow = row;
                }
                else
                {
                    this.Core.OnMessage(BalErrors.MultipleBAFunctions(row.SourceLineNumbers));
                }
            }

            Table payloadPropertiesTable = output.Tables["WixPayloadProperties"];
            IEnumerable<WixPayloadPropertiesRow> payloadPropertiesRows = payloadPropertiesTable.RowsAs<WixPayloadPropertiesRow>();
            if (null == baFunctionsRow)
            {
                foreach (WixPayloadPropertiesRow payloadPropertiesRow in payloadPropertiesRows)
                {
                    // TODO: Make core WiX canonicalize Name (this won't catch '.\bafunctions.dll').
                    if (String.Equals(payloadPropertiesRow.Name, "bafunctions.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Core.OnMessage(BalWarnings.UnmarkedBAFunctionsDLL(payloadPropertiesRow.SourceLineNumbers));
                    }
                }
            }
            else
            {
                // TODO: May need to revisit this depending on the outcome of #5273.
                string payloadId = (string)baFunctionsRow[0];
                WixPayloadPropertiesRow bundlePayloadRow = payloadPropertiesRows.Single(x => payloadId == x.Id);
                if (Compiler.BurnUXContainerId != bundlePayloadRow.Container)
                {
                    this.Core.OnMessage(BalErrors.BAFunctionsPayloadRequiredInUXContainer(baFunctionsRow.SourceLineNumbers));
                }
            }
        }

        private void VerifyPrereqPackages(Output output)
        {
            Table prereqInfoTable = output.Tables["WixMbaPrereqInformation"];
            if (null == prereqInfoTable || prereqInfoTable.Rows.Count == 0)
            {
                this.Core.OnMessage(BalErrors.MissingPrereq());
                return;
            }

            bool foundLicenseFile = false;
            bool foundLicenseUrl = false;

            foreach (Row prereqInfoRow in prereqInfoTable.Rows)
            {
                if (null != prereqInfoRow[1])
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Core.OnMessage(BalErrors.MultiplePrereqLicenses(prereqInfoRow.SourceLineNumbers));
                        return;
                    }

                    foundLicenseFile = true;
                }

                if (null != prereqInfoRow[2])
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Core.OnMessage(BalErrors.MultiplePrereqLicenses(prereqInfoRow.SourceLineNumbers));
                        return;
                    }

                    foundLicenseUrl = true;
                }
            }
        }
    }
}
