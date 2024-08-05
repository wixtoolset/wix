// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data.WindowsInstaller;

    public static class ComPlusTableDefinitions
    {
        public static readonly TableDefinition ComPlusPartition = new TableDefinition(
            "Wix4ComPlusPartition",
            ComPlusSymbolDefinitions.ComPlusPartition,
            new[]
            {
                new ColumnDefinition("Partition", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Id", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusPartitionProperty = new TableDefinition(
            "Wix4ComPlusPartitionProperty",
            ComPlusSymbolDefinitions.ComPlusPartitionProperty,
            new[]
            {
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusPartitionRole = new TableDefinition(
            "Wix4ComPlusPartitionRole",
            ComPlusSymbolDefinitions.ComPlusPartitionRole,
            new[]
            {
                new ColumnDefinition("PartitionRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusUserInPartitionRole = new TableDefinition(
            "Wix4ComPlusUserInPartitionRole",
            ComPlusSymbolDefinitions.ComPlusUserInPartitionRole,
            new[]
            {
                new ColumnDefinition("UserInPartitionRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("PartitionRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusPartitionRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusGroupInPartitionRole = new TableDefinition(
            "Wix4ComPlusGroupInPartitionRole",
            ComPlusSymbolDefinitions.ComPlusGroupInPartitionRole,
            new[]
            {
                new ColumnDefinition("GroupInPartitionRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("PartitionRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusPartitionRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Group_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusPartitionUser = new TableDefinition(
            "Wix4ComPlusPartitionUser",
            ComPlusSymbolDefinitions.ComPlusPartitionUser,
            new[]
            {
                new ColumnDefinition("PartitionUser", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusApplication = new TableDefinition(
            "Wix4ComPlusApplication",
            ComPlusSymbolDefinitions.ComPlusApplication,
            new[]
            {
                new ColumnDefinition("Application", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Wix4ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Id", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusApplicationProperty = new TableDefinition(
            "Wix4ComPlusApplicationProperty",
            ComPlusSymbolDefinitions.ComPlusApplicationProperty,
            new[]
            {
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusApplication", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusApplicationRole = new TableDefinition(
            "Wix4ComPlusApplicationRole",
            ComPlusSymbolDefinitions.ComPlusApplicationRole,
            new[]
            {
                new ColumnDefinition("ApplicationRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusApplication", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusApplicationRoleProperty = new TableDefinition(
            "Wix4ComPlusAppRoleProperty",
            ComPlusSymbolDefinitions.ComPlusApplicationRoleProperty,
            new[]
            {
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusUserInApplicationRole = new TableDefinition(
            "Wix4ComPlusUserInAppRole",
            ComPlusSymbolDefinitions.ComPlusUserInApplicationRole,
            new[]
            {
                new ColumnDefinition("UserInApplicationRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusGroupInApplicationRole = new TableDefinition(
            "Wix4ComPlusGroupInAppRole",
            ComPlusSymbolDefinitions.ComPlusGroupInApplicationRole,
            new[]
            {
                new ColumnDefinition("GroupInApplicationRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Group_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusAssembly = new TableDefinition(
            "Wix4ComPlusAssembly",
            ComPlusSymbolDefinitions.ComPlusAssembly,
            new[]
            {
                new ColumnDefinition("Assembly", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Wix4ComPlusApplication", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("AssemblyName", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("DllPath", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("TlbPath", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("PSDllPath", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusAssemblyDependency = new TableDefinition(
            "Wix4ComPlusAssemblyDependency",
            ComPlusSymbolDefinitions.ComPlusAssemblyDependency,
            new[]
            {
                new ColumnDefinition("Assembly_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusAssembly", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("RequiredAssembly_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusAssembly", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusComponent = new TableDefinition(
            "Wix4ComPlusComponent",
            ComPlusSymbolDefinitions.ComPlusComponent,
            new[]
            {
                new ColumnDefinition("ComPlusComponent", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Assembly_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusAssembly", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("CLSID", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusComponentProperty = new TableDefinition(
            "Wix4ComPlusComponentProperty",
            ComPlusSymbolDefinitions.ComPlusComponentProperty,
            new[]
            {
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusRoleForComponent = new TableDefinition(
            "Wix4ComPlusRoleForComponent",
            ComPlusSymbolDefinitions.ComPlusRoleForComponent,
            new[]
            {
                new ColumnDefinition("RoleForComponent", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusInterface = new TableDefinition(
            "Wix4ComPlusInterface",
            ComPlusSymbolDefinitions.ComPlusInterface,
            new[]
            {
                new ColumnDefinition("Interface", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("IID", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusInterfaceProperty = new TableDefinition(
            "Wix4ComPlusInterfaceProperty",
            ComPlusSymbolDefinitions.ComPlusInterfaceProperty,
            new[]
            {
                new ColumnDefinition("Interface_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusInterface", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusRoleForInterface = new TableDefinition(
            "Wix4ComPlusRoleForInterface",
            ComPlusSymbolDefinitions.ComPlusRoleForInterface,
            new[]
            {
                new ColumnDefinition("RoleForInterface", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Interface_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusInterface", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusMethod = new TableDefinition(
            "Wix4ComPlusMethod",
            ComPlusSymbolDefinitions.ComPlusMethod,
            new[]
            {
                new ColumnDefinition("Method", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Interface_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusInterface", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Index", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusMethodProperty = new TableDefinition(
            "Wix4ComPlusMethodProperty",
            ComPlusSymbolDefinitions.ComPlusMethodProperty,
            new[]
            {
                new ColumnDefinition("Method_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusMethod", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusRoleForMethod = new TableDefinition(
            "Wix4ComPlusRoleForMethod",
            ComPlusSymbolDefinitions.ComPlusRoleForMethod,
            new[]
            {
                new ColumnDefinition("RoleForMethod", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Method_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusMethod", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusSubscription = new TableDefinition(
            "Wix4ComPlusSubscription",
            ComPlusSymbolDefinitions.ComPlusSubscription,
            new[]
            {
                new ColumnDefinition("Subscription", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Id", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("EventCLSID", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("PublisherID", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusSubscriptionProperty = new TableDefinition(
            "Wix4ComPlusSubscriptionProperty",
            ComPlusSymbolDefinitions.ComPlusSubscriptionProperty,
            new[]
            {
                new ColumnDefinition("Subscription_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4ComPlusSubscription", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition[] All = new[]
        {
            ComPlusPartition,
            ComPlusPartitionProperty,
            ComPlusPartitionRole,
            ComPlusUserInPartitionRole,
            ComPlusGroupInPartitionRole,
            ComPlusPartitionUser,
            ComPlusApplication,
            ComPlusApplicationProperty,
            ComPlusApplicationRole,
            ComPlusApplicationRoleProperty,
            ComPlusUserInApplicationRole,
            ComPlusGroupInApplicationRole,
            ComPlusAssembly,
            ComPlusAssemblyDependency,
            ComPlusComponent,
            ComPlusComponentProperty,
            ComPlusRoleForComponent,
            ComPlusInterface,
            ComPlusInterfaceProperty,
            ComPlusRoleForInterface,
            ComPlusMethod,
            ComPlusMethodProperty,
            ComPlusRoleForMethod,
            ComPlusSubscription,
            ComPlusSubscriptionProperty,
        };
    }
}
