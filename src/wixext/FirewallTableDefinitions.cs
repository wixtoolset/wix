// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using WixToolset.Data.WindowsInstaller;

    public static class FirewallTableDefinitions
    {
        public static readonly TableDefinition WixFirewallException = new TableDefinition(
            "WixFirewallException",
            new[]
            {
                new ColumnDefinition("WixFirewallException", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The primary key, a non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Localizable display name.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("RemoteAddresses", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Remote address to accept incoming connections from.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Port", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, minValue: 1, description: "Port number.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Protocol", ColumnType.Number, 1, primaryKey: false, nullable: true, ColumnCategory.Integer, minValue: 6, maxValue: 17, description: "Protocol (6=TCP; 17=UDP)."),
                new ColumnDefinition("Program", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Exception for a program (formatted path name).", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Vital=1"),
                new ColumnDefinition("Profile", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Integer, minValue: 1, maxValue: 2147483647, description: "Profile (1=domain; 2=private; 4=public; 2147483647=all)."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the firewall configuration.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Description displayed in Windows Firewall manager for this firewall rule."),
            },
            tupleDefinitionName: FirewallTupleDefinitions.WixFirewallException.Name,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition[] All = new[]
        {
            WixFirewallException,
        };
    }
}
