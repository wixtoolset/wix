// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall.Tuples
{
    using WixToolset.Data;

    public enum WixFirewallExceptionTupleFields
    {
        WixFirewallException,
        Name,
        RemoteAddresses,
        Port,
        Protocol,
        Program,
        Attributes,
        Profile,
        Component_,
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

        public string WixFirewallException
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.WixFirewallException].AsString();
            set => this.Set((int)WixFirewallExceptionTupleFields.WixFirewallException, value);
        }

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

        public int Protocol
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Protocol].AsNumber();
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

        public string Component_
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Component_].AsString();
            set => this.Set((int)WixFirewallExceptionTupleFields.Component_, value);
        }

        public string Description
        {
            get => this.Fields[(int)WixFirewallExceptionTupleFields.Description].AsString();
            set => this.Set((int)WixFirewallExceptionTupleFields.Description, value);
        }
    }
}