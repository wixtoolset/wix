// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Properties = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Properties,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PropertiesSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PropertiesSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(PropertiesSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum PropertiesSymbolFields
    {
        Name,
        Value,
    }

    public class PropertiesSymbol : IntermediateSymbol
    {
        public PropertiesSymbol() : base(SymbolDefinitions.Properties, null, null)
        {
        }

        public PropertiesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Properties, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PropertiesSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)PropertiesSymbolFields.Name];
            set => this.Set((int)PropertiesSymbolFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)PropertiesSymbolFields.Value];
            set => this.Set((int)PropertiesSymbolFields.Value, value);
        }
    }
}