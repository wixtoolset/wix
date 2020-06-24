// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition RadioButton = new IntermediateSymbolDefinition(
            SymbolDefinitionType.RadioButton,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RadioButtonSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RadioButtonSymbolFields.Order), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RadioButtonSymbolFields.X), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonSymbolFields.Y), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonSymbolFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonSymbolFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonSymbolFields.Text), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RadioButtonSymbolFields.Help), IntermediateFieldType.String),
            },
            typeof(RadioButtonSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum RadioButtonSymbolFields
    {
        Property,
        Order,
        Value,
        X,
        Y,
        Width,
        Height,
        Text,
        Help,
    }

    public class RadioButtonSymbol : IntermediateSymbol
    {
        public RadioButtonSymbol() : base(SymbolDefinitions.RadioButton, null, null)
        {
        }

        public RadioButtonSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.RadioButton, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RadioButtonSymbolFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)RadioButtonSymbolFields.Property];
            set => this.Set((int)RadioButtonSymbolFields.Property, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)RadioButtonSymbolFields.Order];
            set => this.Set((int)RadioButtonSymbolFields.Order, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)RadioButtonSymbolFields.Value];
            set => this.Set((int)RadioButtonSymbolFields.Value, value);
        }

        public int X
        {
            get => (int)this.Fields[(int)RadioButtonSymbolFields.X];
            set => this.Set((int)RadioButtonSymbolFields.X, value);
        }

        public int Y
        {
            get => (int)this.Fields[(int)RadioButtonSymbolFields.Y];
            set => this.Set((int)RadioButtonSymbolFields.Y, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)RadioButtonSymbolFields.Width];
            set => this.Set((int)RadioButtonSymbolFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)RadioButtonSymbolFields.Height];
            set => this.Set((int)RadioButtonSymbolFields.Height, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)RadioButtonSymbolFields.Text];
            set => this.Set((int)RadioButtonSymbolFields.Text, value);
        }

        public string Help
        {
            get => (string)this.Fields[(int)RadioButtonSymbolFields.Help];
            set => this.Set((int)RadioButtonSymbolFields.Help, value);
        }
    }
}