// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Netfx.Symbols;

    public static class NetfxTableDefinitions
    {
        public static readonly TableDefinition NetFxNativeImage = new TableDefinition(
            "Wix4NetFxNativeImage",
            NetfxSymbolDefinitions.NetFxNativeImage,
            new[]
            {
                new ColumnDefinition("Wix4NetFxNativeImage", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The primary key, a non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "The assembly for which a native image will be generated.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Priority", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Integer, minValue: 0, maxValue: 3, description: "The priority for generating this native image: 0 is syncronous, 1-3 represent various levels of queued generation."),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Integer, minValue: 0, maxValue: 2147483647, description: "Integer containing bit flags representing native image attributes."),
                new ColumnDefinition("File_Application", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The application which loads this assembly.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Directory_ApplicationBase", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The directory containing the application which loads this assembly.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition[] All = new[]
        {
            NetFxNativeImage,
        };
    }
}
