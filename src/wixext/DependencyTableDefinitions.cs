// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;

    public static class DependencyTableDefinitions
    {
        public static readonly TableDefinition WixDependencyProvider = new TableDefinition(
            "WixDependencyProvider",
            new[]
            {
                new ColumnDefinition("WixDependencyProvider", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "The foreign key into the Component table used to determine install state.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ProviderKey", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the registry key that holds the provider identity."),
                new ColumnDefinition("Version", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Version, description: "The version of the package."),
                new ColumnDefinition("DisplayName", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The display name of the package."),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied."),
            },
            tupleDefinitionName: TupleDefinitions.WixDependencyProvider.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixDependency = new TableDefinition(
            "WixDependency",
            new[]
            {
                new ColumnDefinition("WixDependency", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ProviderKey", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the registry key that holds the provider identity."),
                new ColumnDefinition("MinVersion", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Version, description: "The minimum version of the provider supported."),
                new ColumnDefinition("MaxVersion", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Version, description: "The maximum version of the provider supported."),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied."),
            },
            tupleDefinitionName: DependencyTupleDefinitions.WixDependency.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixDependencyRef = new TableDefinition(
            "WixDependencyRef",
            new[]
            {
                new ColumnDefinition("WixDependencyProvider_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "WixDependencyProvider", keyColumn: 1, description: "Foreign key into the Component table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("WixDependency_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "WixDependency", keyColumn: 1, description: "Foreign key into the WixDependency table.", modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: DependencyTupleDefinitions.WixDependencyRef.Name,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition[] All = new[]
        {
            WixDependencyProvider,
            WixDependency,
            WixDependencyRef,
        };
    }
}
