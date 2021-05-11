// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsHttpHeader = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsHttpHeader.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderSymbolFields.HttpHeader), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderSymbolFields.ParentType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderSymbolFields.ParentValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsHttpHeaderSymbolFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(IIsHttpHeaderSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsHttpHeaderSymbolFields
    {
        HttpHeader,
        ParentType,
        ParentValue,
        Name,
        Value,
        Attributes,
        Sequence,
    }

    public class IIsHttpHeaderSymbol : IntermediateSymbol
    {
        public IIsHttpHeaderSymbol() : base(IisSymbolDefinitions.IIsHttpHeader, null, null)
        {
        }

        public IIsHttpHeaderSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsHttpHeader, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsHttpHeaderSymbolFields index] => this.Fields[(int)index];

        public string HttpHeader
        {
            get => this.Fields[(int)IIsHttpHeaderSymbolFields.HttpHeader].AsString();
            set => this.Set((int)IIsHttpHeaderSymbolFields.HttpHeader, value);
        }

        public int ParentType
        {
            get => this.Fields[(int)IIsHttpHeaderSymbolFields.ParentType].AsNumber();
            set => this.Set((int)IIsHttpHeaderSymbolFields.ParentType, value);
        }

        public string ParentValue
        {
            get => this.Fields[(int)IIsHttpHeaderSymbolFields.ParentValue].AsString();
            set => this.Set((int)IIsHttpHeaderSymbolFields.ParentValue, value);
        }

        public string Name
        {
            get => this.Fields[(int)IIsHttpHeaderSymbolFields.Name].AsString();
            set => this.Set((int)IIsHttpHeaderSymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)IIsHttpHeaderSymbolFields.Value].AsString();
            set => this.Set((int)IIsHttpHeaderSymbolFields.Value, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsHttpHeaderSymbolFields.Attributes].AsNumber();
            set => this.Set((int)IIsHttpHeaderSymbolFields.Attributes, value);
        }

        public int? Sequence
        {
            get => this.Fields[(int)IIsHttpHeaderSymbolFields.Sequence].AsNullableNumber();
            set => this.Set((int)IIsHttpHeaderSymbolFields.Sequence, value);
        }
    }
}