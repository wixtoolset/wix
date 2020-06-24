// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition CheckBox = new IntermediateSymbolDefinition(
            SymbolDefinitionType.CheckBox,
            new[]
            {
                new IntermediateFieldDefinition(nameof(CheckBoxSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CheckBoxSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(CheckBoxSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum CheckBoxSymbolFields
    {
        Property,
        Value,
    }

    public class CheckBoxSymbol : IntermediateSymbol
    {
        public CheckBoxSymbol() : base(SymbolDefinitions.CheckBox, null, null)
        {
        }

        public CheckBoxSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.CheckBox, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CheckBoxSymbolFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)CheckBoxSymbolFields.Property];
            set => this.Set((int)CheckBoxSymbolFields.Property, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)CheckBoxSymbolFields.Value];
            set => this.Set((int)CheckBoxSymbolFields.Value, value);
        }
    }
}