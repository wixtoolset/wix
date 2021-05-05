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
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Protocol), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Program), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Profile), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionSymbolFields.Direction), IntermediateFieldType.Number),
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

        public int? Protocol
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Protocol].AsNullableNumber();
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

        public int Profile
        {
            get => this.Fields[(int)WixFirewallExceptionSymbolFields.Profile].AsNumber();
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
    }
}