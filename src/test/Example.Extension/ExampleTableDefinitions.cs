// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using WixToolset.Data.WindowsInstaller;

    public static class ExampleTableDefinitions
    {
        public static readonly TableDefinition ExampleTable = new TableDefinition(
            "Wix4Example",
            ExampleTupleDefinitions.Example,
            new[]
            {
                new ColumnDefinition("Example", ColumnType.String, 72, true, false, ColumnCategory.Identifier),
                new ColumnDefinition("Value", ColumnType.String, 0, false, false, ColumnCategory.Formatted),
            },
            strongRowType: typeof(ExampleRow),
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition NotInAll = new TableDefinition(
            "TableDefinitionNotExposedByExtension",
            null,
            new[]
            {
                new ColumnDefinition("Example", ColumnType.String, 72, true, false, ColumnCategory.Identifier),
                new ColumnDefinition("Value", ColumnType.String, 0, false, false, ColumnCategory.Formatted),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition[] All = new[] { ExampleTable };
    }
}
