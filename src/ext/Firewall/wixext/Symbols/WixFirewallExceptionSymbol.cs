// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using WixToolset.Data;
    using WixToolset.Firewall.Symbols;

    public static partial class FirewallSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixFirewallException = new IntermediateSymbolDefinition(
            FirewallSymbolDefinitionType.WixFirewallException.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.RemoteAddresses), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Port), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Protocol), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Program), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Profile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Direction), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.EdgeTraversal), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Enabled), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Grouping), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.IcmpTypesAndCodes), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Interfaces), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.InterfaceTypes), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.LocalAddresses), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.RemotePort), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.ServiceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.LocalAppPackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.LocalUserAuthorizedList), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.LocalUserOwner), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.RemoteMachineAuthorizedList), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.RemoteUserAuthorizedList), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.SecureFlags), IntermediateFieldType.String),
            },
            typeof(WixFirewallExceptionSymbol));
    }
}

namespace WixToolset.Firewall.Symbols
{
    using WixToolset.Data;

    public enum WixFirewallExceptionSymbolFields
    {
        Name,
        RemoteAddresses,
        Port,
        Protocol,
        Program,
        Attributes,
        Profile,
        ComponentRef,
        Description,
        Direction,
        Action,
        EdgeTraversal,
        Enabled,
        Grouping,
        IcmpTypesAndCodes,
        Interfaces,
        InterfaceTypes,
        LocalAddresses,
        RemotePort,
        ServiceName,
        LocalAppPackageId,
        LocalUserAuthorizedList,
        LocalUserOwner,
        RemoteMachineAuthorizedList,
        RemoteUserAuthorizedList,
        SecureFlags,
    }

    public class WixFirewallExceptionSymbol : IntermediateSymbol
    {
        public WixFirewallExceptionSymbol() : base(FirewallSymbolDefinitions.WixFirewallException, null, null)
        {
        }

        public WixFirewallExceptionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(FirewallSymbolDefinitions.WixFirewallException, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFirewallExceptionSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Name].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Name, value);
        }

        public string RemoteAddresses
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.RemoteAddresses].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.RemoteAddresses, value);
        }

        public string Port
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Port].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Port, value);
        }

        public string Protocol
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Protocol].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Protocol, value);
        }

        public string Program
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Program].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Program, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Attributes, value);
        }

        public string Profile
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Profile].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Profile, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.ComponentRef].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.ComponentRef, value);
        }

        public string Description
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Description].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Description, value);
        }

        public int Direction
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Direction].AsNumber();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Direction, value);
        }

        public string Action
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Action].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Action, value);
        }

        public string EdgeTraversal
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.EdgeTraversal].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.EdgeTraversal, value);
        }

        public string Enabled
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Enabled].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Enabled, value);
        }

        public string Grouping
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Grouping].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Grouping, value);
        }

        public string IcmpTypesAndCodes
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.IcmpTypesAndCodes].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.IcmpTypesAndCodes, value);
        }

        public string Interfaces
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Interfaces].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.Interfaces, value);
        }

        public string InterfaceTypes
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.InterfaceTypes].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.InterfaceTypes, value);
        }

        public string LocalAddresses
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.LocalAddresses].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.LocalAddresses, value);
        }

        public string RemotePort
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.RemotePort].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.RemotePort, value);
        }

        public string ServiceName
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.ServiceName].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.ServiceName, value);
        }

        public string LocalAppPackageId
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.LocalAppPackageId].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.LocalAppPackageId, value);
        }

        public string LocalUserAuthorizedList
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.LocalUserAuthorizedList].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.LocalUserAuthorizedList, value);
        }

        public string LocalUserOwner
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.LocalUserOwner].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.LocalUserOwner, value);
        }

        public string RemoteMachineAuthorizedList
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.RemoteMachineAuthorizedList].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.RemoteMachineAuthorizedList, value);
        }

        public string RemoteUserAuthorizedList
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.RemoteUserAuthorizedList].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.RemoteUserAuthorizedList, value);
        }

        public string SecureFlags
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.SecureFlags].AsString();
            set => this.Set((int)WixFirewallExceptionSymbolFields.SecureFlags, value);
        }
    }
}
