// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComboBox = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ComboBox,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComboBoxSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComboBoxSymbolFields.Order), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ComboBoxSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComboBoxSymbolFields.Text), IntermediateFieldType.String),
            },
            typeof(ComboBoxSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ComboBoxSymbolFields
    {
        Property,
        Order,
        Value,
        Text,
    }

    public class ComboBoxSymbol : IntermediateSymbol
    {
        public ComboBoxSymbol() : base(SymbolDefinitions.ComboBox, null, null)
        {
        }

        public ComboBoxSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ComboBox, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComboBoxSymbolFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)ComboBoxSymbolFields.Property];
            set => this.Set((int)ComboBoxSymbolFields.Property, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)ComboBoxSymbolFields.Order];
            set => this.Set((int)ComboBoxSymbolFields.Order, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ComboBoxSymbolFields.Value];
            set => this.Set((int)ComboBoxSymbolFields.Value, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)ComboBoxSymbolFields.Text];
            set => this.Set((int)ComboBoxSymbolFields.Text, value);
        }
    }
}