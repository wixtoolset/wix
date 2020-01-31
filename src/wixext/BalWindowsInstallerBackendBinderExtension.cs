// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    public class BalWindowsInstallerBackendBinderExtension : BaseWindowsInstallerBackendBinderExtension
    {
        private static readonly TableDefinition[] Tables = LoadTables();

        public override IEnumerable<TableDefinition> TableDefinitions => Tables;

        private static TableDefinition[] LoadTables()
        {
            using (var resourceStream = typeof(BalWindowsInstallerBackendBinderExtension).Assembly.GetManifestResourceStream("WixToolset.Bal.tables.xml"))
            using (var reader = XmlReader.Create(resourceStream))
            {
                var tables = TableDefinitionCollection.Load(reader);
                return tables.ToArray();
            }
        }

        public override void PostBackendBind(IBindResult result, WixOutput wixout)
        {
            base.PostBackendBind(result, wixout);

            var output = WindowsInstallerData.Load(wixout.Uri.AbsoluteUri, false);

            // Only process Bundles.
            if (OutputType.Bundle != output.Type)
            {
                return;
            }

            var baTable = output.Tables["WixBootstrapperApplication"];
            var baRow = baTable.Rows[0];
            var baId = (string)baRow[0];
            if (null == baId)
            {
                return;
            }

            var isStdBA = baId.StartsWith("WixStandardBootstrapperApplication");
            var isMBA = baId.StartsWith("ManagedBootstrapperApplicationHost");

            if (isStdBA || isMBA)
            {
                this.VerifyBAFunctions(output);
            }

            if (isMBA)
            {
                this.VerifyPrereqPackages(output);
            }
        }

        private void VerifyBAFunctions(WindowsInstallerData output)
        {
            Row baFunctionsRow = null;
            var baFunctionsTable = output.Tables["WixBalBAFunctions"];
            foreach (var row in baFunctionsTable.Rows)
            {
                if (null == baFunctionsRow)
                {
                    baFunctionsRow = row;
                }
                else
                {
                    this.Messaging.Write(BalErrors.MultipleBAFunctions(row.SourceLineNumbers));
                }
            }

            var payloadPropertiesTable = output.Tables["WixPayloadProperties"];
            var payloadPropertiesRows = payloadPropertiesTable.Rows.Cast<WixPayloadPropertiesRow>();
            if (null == baFunctionsRow)
            {
                foreach (var payloadPropertiesRow in payloadPropertiesRows)
                {
                    // TODO: Make core WiX canonicalize Name (this won't catch '.\bafunctions.dll').
                    if (string.Equals(payloadPropertiesRow.Name, "bafunctions.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Messaging.Write(BalWarnings.UnmarkedBAFunctionsDLL(payloadPropertiesRow.SourceLineNumbers));
                    }
                }
            }
            else
            {
                // TODO: May need to revisit this depending on the outcome of #5273.
                var payloadId = (string)baFunctionsRow[0];
                var bundlePayloadRow = payloadPropertiesRows.Single(x => payloadId == x.Id);
                if (BurnConstants.BurnUXContainerName != bundlePayloadRow.Container)
                {
                    this.Messaging.Write(BalErrors.BAFunctionsPayloadRequiredInUXContainer(baFunctionsRow.SourceLineNumbers));
                }
            }
        }

        private void VerifyPrereqPackages(WindowsInstallerData output)
        {
            var prereqInfoTable = output.Tables["WixMbaPrereqInformation"];
            if (null == prereqInfoTable || prereqInfoTable.Rows.Count == 0)
            {
                this.Messaging.Write(BalErrors.MissingPrereq());
                return;
            }

            var foundLicenseFile = false;
            var foundLicenseUrl = false;

            foreach (Row prereqInfoRow in prereqInfoTable.Rows)
            {
                if (null != prereqInfoRow[1])
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Messaging.Write(BalErrors.MultiplePrereqLicenses(prereqInfoRow.SourceLineNumbers));
                        return;
                    }

                    foundLicenseFile = true;
                }

                if (null != prereqInfoRow[2])
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Messaging.Write(BalErrors.MultiplePrereqLicenses(prereqInfoRow.SourceLineNumbers));
                        return;
                    }

                    foundLicenseUrl = true;
                }
            }
        }
    }
}
