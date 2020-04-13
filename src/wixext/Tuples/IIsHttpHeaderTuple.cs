// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsHttpHeader = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsHttpHeader.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderTupleFields.HttpHeader), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderTupleFields.ParentType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderTupleFields.ParentValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(IIsHttpHeaderTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsHttpHeaderTupleFields
    {
        HttpHeader,
        ParentType,
        ParentValue,
        Name,
        Value,
        Attributes,
        Sequence,
    }

    public class IIsHttpHeaderTuple : IntermediateTuple
    {
        public IIsHttpHeaderTuple() : base(IisTupleDefinitions.IIsHttpHeader, null, null)
        {
        }

        public IIsHttpHeaderTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsHttpHeader, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsHttpHeaderTupleFields index] => this.Fields[(int)index];

        public string HttpHeader
        {
            get => this.Fields[(int)IIsHttpHeaderTupleFields.HttpHeader].AsString();
            set => this.Set((int)IIsHttpHeaderTupleFields.HttpHeader, value);
        }

        public int ParentType
        {
            get => this.Fields[(int)IIsHttpHeaderTupleFields.ParentType].AsNumber();
            set => this.Set((int)IIsHttpHeaderTupleFields.ParentType, value);
        }

        public string ParentValue
        {
            get => this.Fields[(int)IIsHttpHeaderTupleFields.ParentValue].AsString();
            set => this.Set((int)IIsHttpHeaderTupleFields.ParentValue, value);
        }

        public string Name
        {
            get => this.Fields[(int)IIsHttpHeaderTupleFields.Name].AsString();
            set => this.Set((int)IIsHttpHeaderTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)IIsHttpHeaderTupleFields.Value].AsString();
            set => this.Set((int)IIsHttpHeaderTupleFields.Value, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsHttpHeaderTupleFields.Attributes].AsNumber();
            set => this.Set((int)IIsHttpHeaderTupleFields.Attributes, value);
        }

        public int? Sequence
        {
            get => this.Fields[(int)IIsHttpHeaderTupleFields.Sequence].AsNullableNumber();
            set => this.Set((int)IIsHttpHeaderTupleFields.Sequence, value);
        }
    }
}