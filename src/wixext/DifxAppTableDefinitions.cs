// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.DifxApp
{
    using WixToolset.Data.WindowsInstaller;

    public static class DifxAppTableDefinitions
    {
        public static readonly TableDefinition MsiDriverPackages = new TableDefinition(
            "MsiDriverPackages",
            DifxAppSymbolDefinitions.MsiDriverPackages,
            new[]
            {
                new ColumnDefinition("Component", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Name of the component that represents the driver package", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Flags", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 31, description: "Flags for installing and uninstalling driver packages"),
                new ColumnDefinition("Sequence", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, description: "Order in which the driver packages are processed"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition[] All = new[]
        {
            MsiDriverPackages,
        };
    }
}
