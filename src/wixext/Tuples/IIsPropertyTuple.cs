// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsProperty = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsPropertyTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsPropertyTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsPropertyTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(IIsPropertyTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsPropertyTupleFields
    {
        ComponentRef,
        Attributes,
        Value,
    }

    public class IIsPropertyTuple : IntermediateTuple
    {
        public IIsPropertyTuple() : base(IisTupleDefinitions.IIsProperty, null, null)
        {
        }

        public IIsPropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsPropertyTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)IIsPropertyTupleFields.ComponentRef].AsString();
            set => this.Set((int)IIsPropertyTupleFields.ComponentRef, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsPropertyTupleFields.Attributes].AsNumber();
            set => this.Set((int)IIsPropertyTupleFields.Attributes, value);
        }

        public string Value
        {
            get => this.Fields[(int)IIsPropertyTupleFields.Value].AsString();
            set => this.Set((int)IIsPropertyTupleFields.Value, value);
        }
    }
}