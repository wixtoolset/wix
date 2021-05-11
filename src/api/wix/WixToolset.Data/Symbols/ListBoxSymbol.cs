// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ListBox = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ListBox,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ListBoxSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ListBoxSymbolFields.Order), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ListBoxSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ListBoxSymbolFields.Text), IntermediateFieldType.String),
            },
            typeof(ListBoxSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ListBoxSymbolFields
    {
        Property,
        Order,
        Value,
        Text,
    }

    public class ListBoxSymbol : IntermediateSymbol
    {
        public ListBoxSymbol() : base(SymbolDefinitions.ListBox, null, null)
        {
        }

        public ListBoxSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ListBox, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ListBoxSymbolFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)ListBoxSymbolFields.Property];
            set => this.Set((int)ListBoxSymbolFields.Property, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)ListBoxSymbolFields.Order];
            set => this.Set((int)ListBoxSymbolFields.Order, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ListBoxSymbolFields.Value];
            set => this.Set((int)ListBoxSymbolFields.Value, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)ListBoxSymbolFields.Text];
            set => this.Set((int)ListBoxSymbolFields.Text, value);
        }
    }
}