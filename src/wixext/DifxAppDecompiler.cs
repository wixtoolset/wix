// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections;
    using System.Globalization;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using DifxApp = WixToolset.Extensions.Serialize.DifxApp;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The decompiler for the WiX Toolset Driver Install Frameworks for Applications Extension.
    /// </summary>
    public sealed class DifxAppDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Creates a decompiler for Gaming Extension.
        /// </summary>
        public DifxAppDecompiler()
        {
            this.TableDefinitions = DifxAppExtensionData.GetExtensionTableDefinitions();
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "MsiDriverPackages":
                    this.DecompileMsiDriverPackagesTable(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Decompile the MsiDriverPackages table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiDriverPackagesTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                DifxApp.Driver driver = new DifxApp.Driver();

                int attributes = (int)row[1];
                if (0x1 == (attributes & 0x1))
                {
                    driver.ForceInstall = DifxApp.YesNoType.yes;
                }

                if (0x2 == (attributes & 0x2))
                {
                    driver.PlugAndPlayPrompt = DifxApp.YesNoType.no;
                }

                if (0x4 == (attributes & 0x4))
                {
                    driver.AddRemovePrograms = DifxApp.YesNoType.no;
                }

                if (0x8 == (attributes & 0x8))
                {
                    driver.Legacy = DifxApp.YesNoType.yes;
                }

                if (0x10 == (attributes & 0x10))
                {
                    driver.DeleteFiles = DifxApp.YesNoType.yes;
                }

                if (null != row[2])
                {
                    driver.Sequence = (int)row[2];
                }

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[0]);
                if (null != component)
                {
                    component.AddChild(driver);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component", (string)row[0], "Component"));
                }
            }
        }
    }
}
