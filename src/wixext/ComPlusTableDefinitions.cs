// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data.WindowsInstaller;

    public static class ComPlusTableDefinitions
    {
        public static readonly TableDefinition ComPlusPartition = new TableDefinition(
            "ComPlusPartition",
            new[]
            {
                new ColumnDefinition("Partition", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Id", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusPartition.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusPartitionProperty = new TableDefinition(
            "ComPlusPartitionProperty",
            new[]
            {
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusPartitionProperty.Name,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusPartitionRole = new TableDefinition(
            "ComPlusPartitionRole",
            new[]
            {
                new ColumnDefinition("PartitionRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusPartitionRole.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusUserInPartitionRole = new TableDefinition(
            "ComPlusUserInPartitionRole",
            new[]
            {
                new ColumnDefinition("UserInPartitionRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("PartitionRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartitionRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusUserInPartitionRole.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusGroupInPartitionRole = new TableDefinition(
            "ComPlusGroupInPartitionRole",
            new[]
            {
                new ColumnDefinition("GroupInPartitionRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("PartitionRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartitionRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Group_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusGroupInPartitionRole.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusPartitionUser = new TableDefinition(
            "ComPlusPartitionUser",
            new[]
            {
                new ColumnDefinition("PartitionUser", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusPartitionUser.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusApplication = new TableDefinition(
            "ComPlusApplication",
            new[]
            {
                new ColumnDefinition("Application", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Partition_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ComPlusPartition", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Id", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusApplication.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusApplicationProperty = new TableDefinition(
            "ComPlusApplicationProperty",
            new[]
            {
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplication", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusApplicationProperty.Name,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusApplicationRole = new TableDefinition(
            "ComPlusApplicationRole",
            new[]
            {
                new ColumnDefinition("ApplicationRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplication", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusApplicationRole.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusApplicationRoleProperty = new TableDefinition(
            "ComPlusApplicationRoleProperty",
            new[]
            {
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusApplicationRoleProperty.Name,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusUserInApplicationRole = new TableDefinition(
            "ComPlusUserInApplicationRole",
            new[]
            {
                new ColumnDefinition("UserInApplicationRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusUserInApplicationRole.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusGroupInApplicationRole = new TableDefinition(
            "ComPlusGroupInApplicationRole",
            new[]
            {
                new ColumnDefinition("GroupInApplicationRole", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Group_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusGroupInApplicationRole.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusAssembly = new TableDefinition(
            "ComPlusAssembly",
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
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusAssembly.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusAssemblyDependency = new TableDefinition(
            "ComPlusAssemblyDependency",
            new[]
            {
                new ColumnDefinition("Assembly_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusAssembly", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("RequiredAssembly_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusAssembly", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusAssemblyDependency.Name,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusComponent = new TableDefinition(
            "ComPlusComponent",
            new[]
            {
                new ColumnDefinition("ComPlusComponent", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Assembly_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusAssembly", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("CLSID", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusComponent.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusComponentProperty = new TableDefinition(
            "ComPlusComponentProperty",
            new[]
            {
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusComponentProperty.Name,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusRoleForComponent = new TableDefinition(
            "ComPlusRoleForComponent",
            new[]
            {
                new ColumnDefinition("RoleForComponent", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusRoleForComponent.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusInterface = new TableDefinition(
            "ComPlusInterface",
            new[]
            {
                new ColumnDefinition("Interface", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComPlusComponent_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusComponent", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("IID", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusInterface.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusInterfaceProperty = new TableDefinition(
            "ComPlusInterfaceProperty",
            new[]
            {
                new ColumnDefinition("Interface_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusInterface", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusInterfaceProperty.Name,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusRoleForInterface = new TableDefinition(
            "ComPlusRoleForInterface",
            new[]
            {
                new ColumnDefinition("RoleForInterface", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Interface_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusInterface", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusRoleForInterface.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusMethod = new TableDefinition(
            "ComPlusMethod",
            new[]
            {
                new ColumnDefinition("Method", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Interface_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusInterface", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Index", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusMethod.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusMethodProperty = new TableDefinition(
            "ComPlusMethodProperty",
            new[]
            {
                new ColumnDefinition("Method_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusMethod", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusMethodProperty.Name,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusRoleForMethod = new TableDefinition(
            "ComPlusRoleForMethod",
            new[]
            {
                new ColumnDefinition("RoleForMethod", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Method_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusMethod", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ApplicationRole_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusApplicationRole", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusRoleForMethod.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ComPlusSubscription = new TableDefinition(
            "ComPlusSubscription",
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
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusSubscription.Name,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComPlusSubscriptionProperty = new TableDefinition(
            "ComPlusSubscriptionProperty",
            new[]
            {
                new ColumnDefinition("Subscription_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ComPlusSubscription", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
            },
            tupleDefinitionName: ComPlusTupleDefinitions.ComPlusSubscriptionProperty.Name,
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
