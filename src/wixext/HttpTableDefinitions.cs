// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data.WindowsInstaller;

    public static class HttpTableDefinitions
    {
        public static readonly TableDefinition WixHttpUrlReservation = new TableDefinition(
            "WixHttpUrlReservation",
            new[]
            {
                new ColumnDefinition("WixHttpUrlReservation", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("HandleExisting", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2, description: "The behavior when trying to install a URL reservation and it already exists."),
                new ColumnDefinition("Sddl", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Security descriptor for the URL reservation.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Url", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "URL to be reserved.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing the component that controls the URL reservation.", modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: HttpTupleDefinitions.WixHttpUrlReservation.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixHttpUrlAce = new TableDefinition(
            "WixHttpUrlAce",
            new[]
            {
                new ColumnDefinition("WixHttpUrlAce", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("WixHttpUrlReservation_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "WixHttpUrlReservation", keyColumn: 1, description: "Foreign key into the WixHttpUrlReservation table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SecurityPrincipal", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The security principal for this ACE.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Rights", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 1073741824, description: "The rights for this ACE."),
            },
            tupleDefinitionName: HttpTupleDefinitions.WixHttpUrlAce.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition[] All = new[]
        {
            WixHttpUrlReservation,
            WixHttpUrlAce,
        };
    }
}
