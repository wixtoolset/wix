// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// The decompiler for the WiX Toolset Firewall Extension.
    /// </summary>
    public sealed class FirewallDecompiler : BaseWindowsInstallerDecompilerExtension
    {
        public override IReadOnlyCollection<TableDefinition> TableDefinitions => FirewallTableDefinitions.All;

        private IParseHelper ParseHelper { get; set; }

        public override void PreDecompile(IWindowsInstallerDecompileContext context, IWindowsInstallerDecompilerHelper helper)
        {
            base.PreDecompile(context, helper);
            this.ParseHelper = context.ServiceProvider.GetService<IParseHelper>();
        }

        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void PreDecompileTables(TableIndexedCollection tables)
        {
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override bool TryDecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "WixFirewallException":
                case "Wix4FirewallException":
                case "Wix5FirewallException":
                    this.DecompileWixFirewallExceptionTable(table);
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Finalize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void PostDecompileTables(TableIndexedCollection tables)
        {
            this.FinalizeFirewallExceptionTable(tables);
        }

        /// <summary>
        /// Decompile the WixFirewallException table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixFirewallExceptionTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var firewallException = new XElement(FirewallConstants.FirewallExceptionName,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(1))
                );

                if (!row.IsColumnEmpty(2))
                {
                    string[] addresses = ((string)row[2]).Split(',');
                    if (addresses.Length == 1)
                    {
                        switch(addresses[0])
                        {
                            case "*":
                                firewallException.Add(new XAttribute("Scope", "any"));
                                break;
                            case "LocalSubnet":
                                firewallException.Add(new XAttribute("Scope", "localSubnet"));
                                break;
                            case "dns":
                                firewallException.Add(new XAttribute("Scope", "DNS"));
                                break;
                            case "dhcp":
                                firewallException.Add(new XAttribute("Scope", "DHCP"));
                                break;
                            case "wins":
                                firewallException.Add(new XAttribute("Scope", "WINS"));
                                break;
                            case "DefaultGateway":
                                firewallException.Add(new XAttribute("Scope", "defaultGateway"));
                                break;
                            default:
                                if (this.ParseHelper.ContainsProperty(addresses[0]))
                                {
                                    firewallException.Add(new XAttribute("Scope", addresses[0]));
                                }
                                else
                                {
                                    FirewallDecompiler.AddRemoteAddress(firewallException, addresses[0]);
                                }
                                break;
                        }
                    }
                    else
                    {
                        foreach (var address in addresses)
                        {
                            FirewallDecompiler.AddRemoteAddress(firewallException, address);
                        }
                    }
                }

                if (!row.IsColumnEmpty(3))
                {
                    firewallException.Add(new XAttribute("Port", row.FieldAsString(3)));
                }

                if (!row.IsColumnEmpty(4))
                {
                    switch (row.FieldAsString(4))
                    {
                        case FirewallConstants.IntegerNotSetString:
                            break;
                        case "6":
                            firewallException.Add(new XAttribute("Protocol", "tcp"));
                            break;
                        case "17":
                            firewallException.Add(new XAttribute("Protocol", "udp"));
                            break;

                        default:
                            firewallException.Add(new XAttribute("Protocol", row.FieldAsString(4)));
                            break;
                    }
                }

                if (!row.IsColumnEmpty(5))
                {
                    firewallException.Add(new XAttribute("Program", row.FieldAsString(5)));
                }

                if (!row.IsColumnEmpty(6))
                {
                    var attr = row.FieldAsInteger(6);
                    if ((attr & 0x1) == 0x1)
                    {
                        AttributeIfNotNull("IgnoreFailure", true);
                    }

                    if ((attr & 0x2) == 0x2)
                    {
                        firewallException.Add(new XAttribute("OnUpdate", "DoNothing"));
                    }
                    else if ((attr & 0x4) == 0x4)
                    {
                        firewallException.Add(new XAttribute("OnUpdate", "EnableOnly"));
                    }
                }

                if (!row.IsColumnEmpty(7))
                {
                    switch (row.FieldAsString(7))
                    {
                        case FirewallConstants.IntegerNotSetString:
                            break;
                        case "1":
                            firewallException.Add(new XAttribute("Profile", "domain"));
                            break;
                        case "2":
                            firewallException.Add(new XAttribute("Profile", "private"));
                            break;
                        case "4":
                            firewallException.Add(new XAttribute("Profile", "public"));
                            break;
                        case "2147483647":
                            firewallException.Add(new XAttribute("Profile", "all"));
                            break;

                        default:
                            firewallException.Add(new XAttribute("Profile", row.FieldAsString(7)));
                            break;
                    }
                }

                if (!row.IsColumnEmpty(9))
                {
                    firewallException.Add(new XAttribute("Description", row.FieldAsString(9)));
                }

                if (!row.IsColumnEmpty(10))
                {
                    switch (Convert.ToInt32(row[10]))
                    {
                        case FirewallConstants.NET_FW_RULE_DIR_IN:
                            break;
                        case FirewallConstants.NET_FW_RULE_DIR_OUT:
                            firewallException.Add(AttributeIfNotNull("Outbound", true));
                            break;
                    }
                }

                // Introduced in 5.0.0
                if (row.Fields.Length > 11)
                {
                    if (!row.IsColumnEmpty(11))
                    {
                        var action = row.FieldAsString(11);
                        switch (action)
                        {
                            case FirewallConstants.IntegerNotSetString:
                                break;
                            case "1":
                                firewallException.Add(new XAttribute("Action", "Allow"));
                                break;
                            case "0":
                                firewallException.Add(new XAttribute("Action", "Block"));
                                break;
                            default:
                                firewallException.Add(new XAttribute("Action", action));
                                break;
                        }
                    }

                    if (!row.IsColumnEmpty(12))
                    {
                        var edgeTraversal = row.FieldAsString(12);
                        switch (edgeTraversal)
                        {
                            case FirewallConstants.IntegerNotSetString:
                                break;
                            case "0":
                                firewallException.Add(new XAttribute("EdgeTraversal", "Deny"));
                                break;
                            case "1":
                                firewallException.Add(new XAttribute("EdgeTraversal", "Allow"));
                                break;
                            case "2":
                                firewallException.Add(new XAttribute("EdgeTraversal", "DeferToApp"));
                                break;
                            case "3":
                                firewallException.Add(new XAttribute("EdgeTraversal", "DeferToUser"));
                                break;
                            default:
                                firewallException.Add(new XAttribute("EdgeTraversal", edgeTraversal));
                                break;
                        }
                    }

                    if (!row.IsColumnEmpty(13))
                    {
                        var enabled = row.FieldAsString(13);
                        switch (enabled)
                        {
                            case FirewallConstants.IntegerNotSetString:
                                break;
                            case "1":
                                firewallException.Add(new XAttribute("Enabled", "yes"));
                                break;
                            case "0":
                                firewallException.Add(new XAttribute("Enabled", "no"));
                                break;
                            default:
                                firewallException.Add(new XAttribute("Enabled", enabled));
                                break;
                        }
                    }

                    if (!row.IsColumnEmpty(14))
                    {
                        firewallException.Add(new XAttribute("Grouping", row.FieldAsString(14)));
                    }

                    if (!row.IsColumnEmpty(15))
                    {
                        firewallException.Add(new XAttribute("IcmpTypesAndCodes", row.FieldAsString(15)));
                    }

                    if (!row.IsColumnEmpty(16))
                    {
                        string[] interfaces = row.FieldAsString(16).Split(new[] { FirewallConstants.FORBIDDEN_FIREWALL_CHAR }, StringSplitOptions.RemoveEmptyEntries);
                        if (interfaces.Length == 1)
                        {
                            firewallException.Add(new XAttribute("Interface", interfaces[0]));
                        }
                        else
                        {
                            foreach (var interfaceItem in interfaces)
                            {
                                FirewallDecompiler.AddInterface(firewallException, interfaceItem);
                            }
                        }
                    }

                    if (!row.IsColumnEmpty(17))
                    {
                        string[] interfaceTypes = row.FieldAsString(17).Split(',');
                        if (interfaceTypes.Length == 1)
                        {
                            firewallException.Add(new XAttribute("InterfaceType", interfaceTypes[0]));
                        }
                        else
                        {
                            foreach (var interfaceType in interfaceTypes)
                            {
                                FirewallDecompiler.AddInterfaceType(firewallException, interfaceType);
                            }
                        }
                    }

                    if (!row.IsColumnEmpty(18))
                    {
                        string[] addresses = row.FieldAsString(18).Split(',');
                        if (addresses.Length == 1)
                        {
                            switch (addresses[0])
                            {
                                case "*":
                                    firewallException.Add(new XAttribute("LocalScope", "any"));
                                    break;
                                case "LocalSubnet":
                                    firewallException.Add(new XAttribute("LocalScope", "localSubnet"));
                                    break;
                                case "dns":
                                    firewallException.Add(new XAttribute("LocalScope", "DNS"));
                                    break;
                                case "dhcp":
                                    firewallException.Add(new XAttribute("LocalScope", "DHCP"));
                                    break;
                                case "wins":
                                    firewallException.Add(new XAttribute("LocalScope", "WINS"));
                                    break;
                                case "DefaultGateway":
                                    firewallException.Add(new XAttribute("LocalScope", "defaultGateway"));
                                    break;
                                default:
                                    if (this.ParseHelper.ContainsProperty(addresses[0]))
                                    {
                                        firewallException.Add(new XAttribute("LocalScope", addresses[0]));
                                    }
                                    else
                                    {
                                        FirewallDecompiler.AddLocalAddress(firewallException, addresses[0]);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            foreach (var address in addresses)
                            {
                                FirewallDecompiler.AddLocalAddress(firewallException, address);
                            }
                        }
                    }

                    if (!row.IsColumnEmpty(19))
                    {
                        firewallException.Add(new XAttribute("RemotePort", row.FieldAsString(19)));
                    }

                    if (!row.IsColumnEmpty(20))
                    {
                        firewallException.Add(new XAttribute("Service", row.FieldAsString(20)));
                    }

                    if (!row.IsColumnEmpty(21))
                    {
                        firewallException.Add(new XAttribute("LocalAppPackageId", row.FieldAsString(21)));
                    }

                    if (!row.IsColumnEmpty(22))
                    {
                        firewallException.Add(new XAttribute("LocalUserAuthorizedList", row.FieldAsString(22)));
                    }

                    if (!row.IsColumnEmpty(23))
                    {
                        firewallException.Add(new XAttribute("LocalUserOwner", row.FieldAsString(23)));
                    }

                    if (!row.IsColumnEmpty(24))
                    {
                        firewallException.Add(new XAttribute("RemoteMachineAuthorizedList", row.FieldAsString(24)));
                    }

                    if (!row.IsColumnEmpty(25))
                    {
                        firewallException.Add(new XAttribute("RemoteUserAuthorizedList", row.FieldAsString(25)));
                    }

                    if (!row.IsColumnEmpty(26))
                    {
                        var secureFlags = row.FieldAsString(26);
                        switch (secureFlags)
                        {
                            case FirewallConstants.IntegerNotSetString:
                                break;
                            case "0":
                                firewallException.Add(new XAttribute("IPSecSecureFlags", "None"));
                                break;
                            case "1":
                                firewallException.Add(new XAttribute("IPSecSecureFlags", "NoEncapsulation"));
                                break;
                            case "2":
                                firewallException.Add(new XAttribute("IPSecSecureFlags", "WithIntegrity"));
                                break;
                            case "3":
                                firewallException.Add(new XAttribute("IPSecSecureFlags", "NegotiateEncryption"));
                                break;
                            case "4":
                                firewallException.Add(new XAttribute("IPSecSecureFlags", "Encrypt"));
                                break;
                            default:
                                firewallException.Add(new XAttribute("IPSecSecureFlags", secureFlags));
                                break;
                        }
                    }
                }

                this.DecompilerHelper.IndexElement(row, firewallException);
            }
        }

        private static void AddRemoteAddress(XElement firewallException, string address)
        {
            var remoteAddress = new XElement(FirewallConstants.RemoteAddressName,
                new XAttribute("Value", address)
            );

            firewallException.Add(remoteAddress);
        }

        private static void AddInterfaceType(XElement firewallException, string type)
        {
            var interfaceType = new XElement(FirewallConstants.InterfaceTypeName,
                new XAttribute("Value", type)
            );

            firewallException.Add(interfaceType);
        }

        private static void AddLocalAddress(XElement firewallException, string address)
        {
            var localAddress = new XElement(FirewallConstants.LocalAddressName,
                new XAttribute("Value", address)
            );

            firewallException.Add(localAddress);
        }

        private static void AddInterface(XElement firewallException, string value)
        {
            var interfaceName = new XElement(FirewallConstants.InterfaceName,
                new XAttribute("Name", value)
            );

            firewallException.Add(interfaceName);
        }

        private static XAttribute AttributeIfNotNull(string name, bool value)
        {
            return new XAttribute(name, value ? "yes" : "no");
        }

        /// <summary>
        /// Finalize the FirewallException table.
        /// </summary>
        /// <param name="tables">Collection of all tables.</param>
        private void FinalizeFirewallExceptionTable(TableIndexedCollection tables)
        {
            if (tables.TryGetTable("Wix5FirewallException", out var firewallExceptionTable))
            {
                foreach (var row in firewallExceptionTable.Rows)
                {
                    var xmlConfig = this.DecompilerHelper.GetIndexedElement(row);

                    var componentId = row.FieldAsString(8);
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                    {
                        component.Add(xmlConfig);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, firewallExceptionTable.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                    }
                }
            }
        }
    }
}
