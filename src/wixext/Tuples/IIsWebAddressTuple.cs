// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebAddress = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebAddress.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebAddressTupleFields.WebRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebAddressTupleFields.IP), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebAddressTupleFields.Port), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebAddressTupleFields.Header), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebAddressTupleFields.Secure), IntermediateFieldType.Number),
            },
            typeof(IIsWebAddressTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebAddressTupleFields
    {
        WebRef,
        IP,
        Port,
        Header,
        Secure,
    }

    public class IIsWebAddressTuple : IntermediateTuple
    {
        public IIsWebAddressTuple() : base(IisTupleDefinitions.IIsWebAddress, null, null)
        {
        }

        public IIsWebAddressTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebAddress, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebAddressTupleFields index] => this.Fields[(int)index];

        public string WebRef
        {
            get => this.Fields[(int)IIsWebAddressTupleFields.WebRef].AsString();
            set => this.Set((int)IIsWebAddressTupleFields.WebRef, value);
        }

        public string IP
        {
            get => this.Fields[(int)IIsWebAddressTupleFields.IP].AsString();
            set => this.Set((int)IIsWebAddressTupleFields.IP, value);
        }

        public string Port
        {
            get => this.Fields[(int)IIsWebAddressTupleFields.Port].AsString();
            set => this.Set((int)IIsWebAddressTupleFields.Port, value);
        }

        public string Header
        {
            get => this.Fields[(int)IIsWebAddressTupleFields.Header].AsString();
            set => this.Set((int)IIsWebAddressTupleFields.Header, value);
        }

        public int Secure
        {
            get => this.Fields[(int)IIsWebAddressTupleFields.Secure].AsNumber();
            set => this.Set((int)IIsWebAddressTupleFields.Secure, value);
        }
    }
}