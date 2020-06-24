// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Property = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Property,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PropertySymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(PropertySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum PropertySymbolFields
    {
        Value,
    }

    public class PropertySymbol : IntermediateSymbol
    {
        public PropertySymbol() : base(SymbolDefinitions.Property, null, null)
        {
        }

        public PropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Property, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PropertySymbolFields index] => this.Fields[(int)index];

        public string Value
        {
            get => (string)this.Fields[(int)PropertySymbolFields.Value];
            set => this.Set((int)PropertySymbolFields.Value, value);
        }
    }
}