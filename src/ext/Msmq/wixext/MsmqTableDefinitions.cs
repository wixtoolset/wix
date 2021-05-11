// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using WixToolset.Data.WindowsInstaller;

    public static class MsmqTableDefinitions
    {
        public static readonly TableDefinition MessageQueue = new TableDefinition(
            "MessageQueue",
            MsmqSymbolDefinitions.MessageQueue,
            new[]
            {
                new ColumnDefinition("MessageQueue", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("BasePriority", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("JournalQuota", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Label", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("MulticastAddress", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("PathName", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("PrivLevel", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Quota", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("ServiceTypeGuid", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MessageQueueUserPermission = new TableDefinition(
            "MessageQueueUserPermission",
            MsmqSymbolDefinitions.MessageQueueUserPermission,
            new[]
            {
                new ColumnDefinition("MessageQueueUserPermission", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("MessageQueue_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "MessageQueue", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Permissions", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MessageQueueGroupPermission = new TableDefinition(
            "MessageQueueGroupPermission",
            MsmqSymbolDefinitions.MessageQueueGroupPermission,
            new[]
            {
                new ColumnDefinition("MessageQueueGroupPermission", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("MessageQueue_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "MessageQueue", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Group_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Permissions", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition[] All = new[]
        {
            MessageQueue,
            MessageQueueUserPermission,
            MessageQueueGroupPermission,
        };
    }
}
