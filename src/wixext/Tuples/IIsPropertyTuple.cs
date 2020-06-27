// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsProperty = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsPropertySymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsPropertySymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsPropertySymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(IIsPropertySymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsPropertySymbolFields
    {
        ComponentRef,
        Attributes,
        Value,
    }

    public class IIsPropertySymbol : IntermediateSymbol
    {
        public IIsPropertySymbol() : base(IisSymbolDefinitions.IIsProperty, null, null)
        {
        }

        public IIsPropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsPropertySymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)IIsPropertySymbolFields.ComponentRef].AsString();
            set => this.Set((int)IIsPropertySymbolFields.ComponentRef, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsPropertySymbolFields.Attributes].AsNumber();
            set => this.Set((int)IIsPropertySymbolFields.Attributes, value);
        }

        public string Value
        {
            get => this.Fields[(int)IIsPropertySymbolFields.Value].AsString();
            set => this.Set((int)IIsPropertySymbolFields.Value, value);
        }
    }
}