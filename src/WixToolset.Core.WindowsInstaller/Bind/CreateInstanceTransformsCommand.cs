// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Services;

    internal class CreateInstanceTransformsCommand
    {
        public CreateInstanceTransformsCommand(IntermediateSection section, WindowsInstallerData output, TableDefinitionCollection tableDefinitions, IBackendHelper backendHelper)
        {
            this.Section = section;
            this.Output = output;
            this.TableDefinitions = tableDefinitions;
            this.BackendHelper = backendHelper;
        }

        private IntermediateSection Section { get; }

        private WindowsInstallerData Output { get; }

        public TableDefinitionCollection TableDefinitions { get; }

        private  IBackendHelper BackendHelper { get; }

        public void Execute()
        {
            // Create and add substorages for instance transforms.
            var wixInstanceTransformsSymbols = this.Section.Symbols.OfType<WixInstanceTransformsSymbol>();

            if (wixInstanceTransformsSymbols.Any())
            {
                string targetProductCode = null;
                string targetUpgradeCode = null;
                string targetProductVersion = null;

                var targetSummaryInformationTable = this.Output.Tables["_SummaryInformation"];
                var targetPropertyTable = this.Output.Tables["Property"];

                // Get the data from target database
                foreach (var propertyRow in targetPropertyTable.Rows)
                {
                    if ("ProductCode" == (string)propertyRow[0])
                    {
                        targetProductCode = (string)propertyRow[1];
                    }
                    else if ("ProductVersion" == (string)propertyRow[0])
                    {
                        targetProductVersion = (string)propertyRow[1];
                    }
                    else if ("UpgradeCode" == (string)propertyRow[0])
                    {
                        targetUpgradeCode = (string)propertyRow[1];
                    }
                }

                // Index the Instance Component Rows, we'll get the Components rows from the real Component table.
                var targetInstanceComponentTable = this.Section.Symbols.OfType<WixInstanceComponentSymbol>();
                var instanceComponentGuids = targetInstanceComponentTable.ToDictionary(t => t.Id.Id, t => (ComponentRow)null);

                if (instanceComponentGuids.Any())
                {
                    var targetComponentTable = this.Output.Tables["Component"];
                    foreach (ComponentRow componentRow in targetComponentTable.Rows)
                    {
                        var component = (string)componentRow[0];
                        if (instanceComponentGuids.ContainsKey(component))
                        {
                            instanceComponentGuids[component] = componentRow;
                        }
                    }
                }

                // Generate the instance transforms
                foreach (var instanceSymbol in wixInstanceTransformsSymbols)
                {
                    var instanceId = instanceSymbol.Id.Id;

                    var instanceTransform = new WindowsInstallerData(instanceSymbol.SourceLineNumbers);
                    instanceTransform.Type = OutputType.Transform;
                    instanceTransform.Codepage = this.Output.Codepage;

                    var instanceSummaryInformationTable = instanceTransform.EnsureTable(this.TableDefinitions["_SummaryInformation"]);
                    string targetPlatformAndLanguage = null;

                    foreach (var summaryInformationRow in targetSummaryInformationTable.Rows)
                    {
                        if (7 == (int)summaryInformationRow[0]) // PID_TEMPLATE
                        {
                            targetPlatformAndLanguage = (string)summaryInformationRow[1];
                        }

                        // Copy the row's data to the transform.
                        var copyOfSummaryRow = instanceSummaryInformationTable.CreateRow(summaryInformationRow.SourceLineNumbers);
                        copyOfSummaryRow[0] = summaryInformationRow[0];
                        copyOfSummaryRow[1] = summaryInformationRow[1];
                    }

                    // Modify the appropriate properties.
                    var propertyTable = instanceTransform.EnsureTable(this.TableDefinitions["Property"]);

                    // Change the ProductCode property
                    var productCode = instanceSymbol.ProductCode;
                    if ("*" == productCode)
                    {
                        productCode = this.BackendHelper.CreateGuid();
                    }

                    var productCodeRow = propertyTable.CreateRow(instanceSymbol.SourceLineNumbers);
                    productCodeRow.Operation = RowOperation.Modify;
                    productCodeRow.Fields[1].Modified = true;
                    productCodeRow[0] = "ProductCode";
                    productCodeRow[1] = productCode;

                    // Change the instance property
                    var instanceIdRow = propertyTable.CreateRow(instanceSymbol.SourceLineNumbers);
                    instanceIdRow.Operation = RowOperation.Modify;
                    instanceIdRow.Fields[1].Modified = true;
                    instanceIdRow[0] = instanceSymbol.PropertyId;
                    instanceIdRow[1] = instanceId;

                    if (!String.IsNullOrEmpty(instanceSymbol.ProductName))
                    {
                        // Change the ProductName property
                        var productNameRow = propertyTable.CreateRow(instanceSymbol.SourceLineNumbers);
                        productNameRow.Operation = RowOperation.Modify;
                        productNameRow.Fields[1].Modified = true;
                        productNameRow[0] = "ProductName";
                        productNameRow[1] = instanceSymbol.ProductName;
                    }

                    if (!String.IsNullOrEmpty(instanceSymbol.UpgradeCode))
                    {
                        // Change the UpgradeCode property
                        var upgradeCodeRow = propertyTable.CreateRow(instanceSymbol.SourceLineNumbers);
                        upgradeCodeRow.Operation = RowOperation.Modify;
                        upgradeCodeRow.Fields[1].Modified = true;
                        upgradeCodeRow[0] = "UpgradeCode";
                        upgradeCodeRow[1] = instanceSymbol.UpgradeCode;

                        // Change the Upgrade table
                        var targetUpgradeTable = this.Output.Tables["Upgrade"];
                        if (null != targetUpgradeTable && 0 <= targetUpgradeTable.Rows.Count)
                        {
                            var upgradeId = instanceSymbol.UpgradeCode;
                            var upgradeTable = instanceTransform.EnsureTable(this.TableDefinitions["Upgrade"]);
                            foreach (var row in targetUpgradeTable.Rows)
                            {
                                // In case they are upgrading other codes to this new product, leave the ones that don't match the
                                // Product.UpgradeCode intact.
                                if (targetUpgradeCode == (string)row[0])
                                {
                                    var upgradeRow = upgradeTable.CreateRow(row.SourceLineNumbers);
                                    upgradeRow.Operation = RowOperation.Add;
                                    upgradeRow.Fields[0].Modified = true;
                                    // I was hoping to be able to RowOperation.Modify, but that didn't appear to function.
                                    // upgradeRow.Fields[0].PreviousData = (string)row[0];

                                    // Inserting a new Upgrade record with the updated UpgradeCode
                                    upgradeRow[0] = upgradeId;
                                    upgradeRow[1] = row[1];
                                    upgradeRow[2] = row[2];
                                    upgradeRow[3] = row[3];
                                    upgradeRow[4] = row[4];
                                    upgradeRow[5] = row[5];
                                    upgradeRow[6] = row[6];

                                    // Delete the old row
                                    var upgradeRemoveRow = upgradeTable.CreateRow(row.SourceLineNumbers);
                                    upgradeRemoveRow.Operation = RowOperation.Delete;
                                    upgradeRemoveRow[0] = row[0];
                                    upgradeRemoveRow[1] = row[1];
                                    upgradeRemoveRow[2] = row[2];
                                    upgradeRemoveRow[3] = row[3];
                                    upgradeRemoveRow[4] = row[4];
                                    upgradeRemoveRow[5] = row[5];
                                    upgradeRemoveRow[6] = row[6];
                                }
                            }
                        }
                    }

                    // If there are instance Components generate new GUIDs for them.
                    if (0 < instanceComponentGuids.Count)
                    {
                        var componentTable = instanceTransform.EnsureTable(this.TableDefinitions["Component"]);
                        foreach (var targetComponentRow in instanceComponentGuids.Values)
                        {
                            var guid = targetComponentRow.Guid;
                            if (!String.IsNullOrEmpty(guid))
                            {
                                var instanceComponentRow = componentTable.CreateRow(targetComponentRow.SourceLineNumbers);
                                instanceComponentRow.Operation = RowOperation.Modify;
                                instanceComponentRow.Fields[1].Modified = true;
                                instanceComponentRow[0] = targetComponentRow[0];
                                instanceComponentRow[1] = this.BackendHelper.CreateGuid(BindDatabaseCommand.WixComponentGuidNamespace, String.Concat(guid, instanceId));
                                instanceComponentRow[2] = targetComponentRow[2];
                                instanceComponentRow[3] = targetComponentRow[3];
                                instanceComponentRow[4] = targetComponentRow[4];
                                instanceComponentRow[5] = targetComponentRow[5];
                            }
                        }
                    }

                    // Update the summary information
                    var summaryRows = new Dictionary<int, Row>(instanceSummaryInformationTable.Rows.Count);
                    foreach (var row in instanceSummaryInformationTable.Rows)
                    {
                        summaryRows[(int)row[0]] = row;

                        if ((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage == (int)row[0])
                        {
                            row[1] = targetPlatformAndLanguage;
                        }
                        else if ((int)SummaryInformation.Transform.ProductCodes == (int)row[0])
                        {
                            row[1] = String.Concat(targetProductCode, targetProductVersion, ';', productCode, targetProductVersion, ';', targetUpgradeCode);
                        }
                        else if ((int)SummaryInformation.Transform.ValidationFlags == (int)row[0])
                        {
                            row[1] = 0;
                        }
                        else if ((int)SummaryInformation.Transform.Security == (int)row[0])
                        {
                            row[1] = "4";
                        }
                    }

                    if (!summaryRows.ContainsKey((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage))
                    {
                        var summaryRow = instanceSummaryInformationTable.CreateRow(instanceSymbol.SourceLineNumbers);
                        summaryRow[0] = (int)SummaryInformation.Transform.UpdatedPlatformAndLanguage;
                        summaryRow[1] = targetPlatformAndLanguage;
                    }
                    else if (!summaryRows.ContainsKey((int)SummaryInformation.Transform.ValidationFlags))
                    {
                        var summaryRow = instanceSummaryInformationTable.CreateRow(instanceSymbol.SourceLineNumbers);
                        summaryRow[0] = (int)SummaryInformation.Transform.ValidationFlags;
                        summaryRow[1] = "0";
                    }
                    else if (!summaryRows.ContainsKey((int)SummaryInformation.Transform.Security))
                    {
                        var summaryRow = instanceSummaryInformationTable.CreateRow(instanceSymbol.SourceLineNumbers);
                        summaryRow[0] = (int)SummaryInformation.Transform.Security;
                        summaryRow[1] = "4";
                    }

                    this.Output.SubStorages.Add(new SubStorage(instanceId, instanceTransform));
                }
            }
        }
    }
}
