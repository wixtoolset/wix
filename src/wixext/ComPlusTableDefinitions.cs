// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data.WindowsInstaller;

    public static class ComPlusTableDefinitions
    {
        public static readonly TableDefinition ComPlusPartition = new TableDefinition(
            "ComPlusPartition",
            ComPlusTupleDefinitions.ComPlusPartition,
            new[]
            {
                new ColumnDefinition("Partition", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Id", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusPartitionProperty = new TableDefinition(
            "ComPlusPartitionProperty",
            ComPlusTupleDefinitions.ComPlusPartitionProperty,
            new[]
            {
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusPartitionRole = new TableDefinition(
            "ComPlusPartitionRole",
            ComPlusTupleDefinitions.ComPlusPartitionRole,
            new[]
            {
                new ColumnDefinition("PartitionRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusUserInPartitionRole = new TableDefinition(
            "ComPlusUserInPartitionRole",
            ComPlusTupleDefinitions.ComPlusUserInPartitionRole,
            new[]
            {
                new ColumnDefinition("UserInPartitionRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("PartitionRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartitionRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusGroupInPartitionRole = new TableDefinition(
            "ComPlusGroupInPartitionRole",
            ComPlusTupleDefinitions.ComPlusGroupInPartitionRole,
            new[]
            {
                new ColumnDefinition("GroupInPartitionRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("PartitionRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartitionRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Group_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusPartitionUser = new TableDefinition(
            "ComPlusPartitionUser",
            ComPlusTupleDefinitions.ComPlusPartitionUser,
            new[]
            {
                new ColumnDefinition("PartitionUser", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusApplication = new TableDefinition(
            "ComPlusApplication",
            ComPlusTupleDefinitions.ComPlusApplication,
            new[]
            {
                new ColumnDefinition("Application", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Id", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusApplicationProperty = new TableDefinition(
            "ComPlusApplicationProperty",
            ComPlusTupleDefinitions.ComPlusApplicationProperty,
            new[]
            {
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplication", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusApplicationRole = new TableDefinition(
            "ComPlusApplicationRole",
            ComPlusTupleDefinitions.ComPlusApplicationRole,
            new[]
            {
                new ColumnDefinition("ApplicationRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplication", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusApplicationRoleProperty = new TableDefinition(
            "ComPlusApplicationRoleProperty",
            ComPlusTupleDefinitions.ComPlusApplicationRoleProperty,
            new[]
            {
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusUserInApplicationRole = new TableDefinition(
            "ComPlusUserInApplicationRole",
            ComPlusTupleDefinitions.ComPlusUserInApplicationRole,
            new[]
            {
                new ColumnDefinition("UserInApplicationRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusGroupInApplicationRole = new TableDefinition(
            "ComPlusGroupInApplicationRole",
            ComPlusTupleDefinitions.ComPlusGroupInApplicationRole,
            new[]
            {
                new ColumnDefinition("GroupInApplicationRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Group_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusAssembly = new TableDefinition(
            "ComPlusAssembly",
            ComPlusTupleDefinitions.ComPlusAssembly,
            new[]
            {
                new ColumnDefinition("Assembly", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ComPlusApplication", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("AssemblyName", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("DllPath", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("TlbPath", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("PSDllPath", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusAssemblyDependency = new TableDefinition(
            "ComPlusAssemblyDependency",
            ComPlusTupleDefinitions.ComPlusAssemblyDependency,
            new[]
            {
                new ColumnDefinition("Assembly_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusAssembly", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("RequiredAssembly_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusAssembly", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusComponent = new TableDefinition(
            "ComPlusComponent",
            ComPlusTupleDefinitions.ComPlusComponent,
            new[]
            {
                new ColumnDefinition("ComPlusComponent", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Assembly_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusAssembly", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("CLSID", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusComponentProperty = new TableDefinition(
            "ComPlusComponentProperty",
            ComPlusTupleDefinitions.ComPlusComponentProperty,
            new[]
            {
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusRoleForComponent = new TableDefinition(
            "ComPlusRoleForComponent",
            ComPlusTupleDefinitions.ComPlusRoleForComponent,
            new[]
            {
                new ColumnDefinition("RoleForComponent", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusInterface = new TableDefinition(
            "ComPlusInterface",
            ComPlusTupleDefinitions.ComPlusInterface,
            new[]
            {
                new ColumnDefinition("Interface", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("IID", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusInterfaceProperty = new TableDefinition(
            "ComPlusInterfaceProperty",
            ComPlusTupleDefinitions.ComPlusInterfaceProperty,
            new[]
            {
                new ColumnDefinition("Interface_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusInterface", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusRoleForInterface = new TableDefinition(
            "ComPlusRoleForInterface",
            ComPlusTupleDefinitions.ComPlusRoleForInterface,
            new[]
            {
                new ColumnDefinition("RoleForInterface", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Interface_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusInterface", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusMethod = new TableDefinition(
            "ComPlusMethod",
            ComPlusTupleDefinitions.ComPlusMethod,
            new[]
            {
                new ColumnDefinition("Method", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Interface_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusInterface", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Index", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusMethodProperty = new TableDefinition(
            "ComPlusMethodProperty",
            ComPlusTupleDefinitions.ComPlusMethodProperty,
            new[]
            {
                new ColumnDefinition("Method_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusMethod", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusRoleForMethod = new TableDefinition(
            "ComPlusRoleForMethod",
            ComPlusTupleDefinitions.ComPlusRoleForMethod,
            new[]
            {
                new ColumnDefinition("RoleForMethod", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Method_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusMethod", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusSubscription = new TableDefinition(
            "ComPlusSubscription",
            ComPlusTupleDefinitions.ComPlusSubscription,
            new[]
            {
                new ColumnDefinition("Subscription", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Id", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("EventCLSID", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("PublisherID", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusSubscriptionProperty = new TableDefinition(
            "ComPlusSubscriptionProperty",
            ComPlusTupleDefinitions.ComPlusSubscriptionProperty,
            new[]
            {
                new ColumnDefinition("Subscription_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusSubscription", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
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
