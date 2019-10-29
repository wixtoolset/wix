// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall.Tuples
{
    using WixToolset.Data;

    public enum WixFirewallExceptionTupleFields
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
    }

    public class WixFirewallExceptionTuple : IntermediateTuple
    {
        public WixFirewallExceptionTuple() : base(FirewallTupleDefinitions.WixFirewallException, null, null)
        {
        }

        public WixFirewallExceptionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(FirewallTupleDefinitions.WixFirewallException, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFirewallExceptionTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Name].AsString();
            set => this.Set((int)WixFirewallExceptionTupleFields.Name, value);
        }

        public string RemoteAddresses
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.RemoteAddresses].AsString();
            set => this.Set((int)WixFirewallExceptionTupleFields.RemoteAddresses, value);
        }

        public string Port
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Port].AsString();
            set => this.Set((int)WixFirewallExceptionTupleFields.Port, value);
        }

        public int? Protocol
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Protocol].AsNullableNumber();
            set => this.Set((int)WixFirewallExceptionTupleFields.Protocol, value);
        }

        public string Program
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Program].AsString();
            set => this.Set((int)WixFirewallExceptionTupleFields.Program, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixFirewallExceptionTupleFields.Attributes, value);
        }

        public int Profile
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Profile].AsNumber();
            set => this.Set((int)WixFirewallExceptionTupleFields.Profile, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.ComponentRef].AsString();
            set => this.Set((int)WixFirewallExceptionTupleFields.ComponentRef, value);
        }

        public string Description
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Description].AsString();
            set => this.Set((int)WixFirewallExceptionTupleFields.Description, value);
        }
    }
}