// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ListView = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ListView,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ListViewSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ListViewSymbolFields.Order), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ListViewSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ListViewSymbolFields.Text), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ListViewSymbolFields.BinaryRef), IntermediateFieldType.String),
            },
            typeof(ListViewSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ListViewSymbolFields
    {
        Property,
        Order,
        Value,
        Text,
        BinaryRef,
    }

    public class ListViewSymbol : IntermediateSymbol
    {
        public ListViewSymbol() : base(SymbolDefinitions.ListView, null, null)
        {
        }

        public ListViewSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ListView, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ListViewSymbolFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)ListViewSymbolFields.Property];
            set => this.Set((int)ListViewSymbolFields.Property, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)ListViewSymbolFields.Order];
            set => this.Set((int)ListViewSymbolFields.Order, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ListViewSymbolFields.Value];
            set => this.Set((int)ListViewSymbolFields.Value, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)ListViewSymbolFields.Text];
            set => this.Set((int)ListViewSymbolFields.Text, value);
        }

        public string BinaryRef
        {
            get => (string)this.Fields[(int)ListViewSymbolFields.BinaryRef];
            set => this.Set((int)ListViewSymbolFields.BinaryRef, value);
        }
    }
}