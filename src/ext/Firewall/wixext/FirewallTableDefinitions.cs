// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using WixToolset.Data.WindowsInstaller;

    public static class FirewallTableDefinitions
    {
        public static readonly TableDefinition WixFirewallException = new TableDefinition(
            "Wix5FirewallException",
            FirewallSymbolDefinitions.WixFirewallException,
            new[]
            {
                new ColumnDefinition("Wix5FirewallException", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The primary key, a non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Localizable display name.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("RemoteAddresses", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Remote address to accept incoming connections from.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Port", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, minValue: 1, maxValue: 65535, description: "Local Port number.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Protocol", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, minValue: 0, maxValue: 255, description: "Protocol (6=TCP; 17=UDP). https://www.iana.org/assignments/protocol-numbers", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Program", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Exception for a program (formatted path name).", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Vital=1; IgnoreUpdates=2; EnableOnChange=4; INetFwRule2=8; INetFwRule3=16"),
                new ColumnDefinition("Profile", ColumnType.String, 4, primaryKey: false, nullable: true, ColumnCategory.Formatted, minValue: 1, maxValue: 2147483647, description: "Profile (1=domain; 2=private; 4=public; 2147483647=all).", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the firewall configuration.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Description displayed in Windows Firewall manager for this firewall rule."),
                new ColumnDefinition("Direction", ColumnType.Number, 1, primaryKey: false, nullable: false, ColumnCategory.Integer, minValue: 1, maxValue: 2, description: "Direction (1=in; 2=out)"),
                new ColumnDefinition("Action", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, minValue: 0, maxValue: 1, description: "Action (0=Block; 1=Allow).", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("EdgeTraversal", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, minValue: 0, maxValue: 3, description: "Edge traversal (0=Deny; 1=Allow; 2=DeferToApp; 3=DeferToUser).", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Enabled", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, minValue: 0, maxValue: 1, description: "Enabled (0=Disabled; 1=Enabled).", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Grouping", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The group to which the rule belongs.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("IcmpTypesAndCodes", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Comma separated list of ICMP types and codes separated by colons.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Interfaces", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "A list of network interfaces separated by a pipe character.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("InterfaceTypes", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Comma separated list of interface types (combination of Wireless,Lan,RemoteAccess or All).", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("LocalAddresses", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Local address to accept incoming connections on.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("RemotePort", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, minValue: 1, maxValue: 65535, description: "Remote Port number.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("ServiceName", ColumnType.String, 256, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Windows Service short name.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("LocalAppPackageId", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Package identifier or the app container identifier of a process.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("LocalUserAuthorizedList", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "List of authorized local users for an app container.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("LocalUserOwner", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "SID of the user who is the owner of the rule.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("RemoteMachineAuthorizedList", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "List of remote computers which are authorized to access an app container.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("RemoteUserAuthorizedList", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "List of remote users who are authorized to access an app container.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("SecureFlags", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, minValue: 0, maxValue: 1, description: "NET_FW_AUTHENTICATE_TYPE IPsec verification level.", modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition[] All = new[]
        {
            WixFirewallException,
        };
    }
}
