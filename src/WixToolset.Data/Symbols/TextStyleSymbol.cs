// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition TextStyle = new IntermediateSymbolDefinition(
            SymbolDefinitionType.TextStyle,
            new[]
            {
                new IntermediateFieldDefinition(nameof(TextStyleSymbolFields.FaceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TextStyleSymbolFields.Size), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TextStyleSymbolFields.Red), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TextStyleSymbolFields.Green), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TextStyleSymbolFields.Blue), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TextStyleSymbolFields.Bold), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(TextStyleSymbolFields.Italic), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(TextStyleSymbolFields.Strike), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(TextStyleSymbolFields.Underline), IntermediateFieldType.Bool),
            },
            typeof(TextStyleSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum TextStyleSymbolFields
    {
        FaceName,
        Size,
        Red,
        Green,
        Blue,
        Bold,
        Italic,
        Strike,
        Underline,
    }

    public class TextStyleSymbol : IntermediateSymbol
    {
        public TextStyleSymbol() : base(SymbolDefinitions.TextStyle, null, null)
        {
        }

        public TextStyleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.TextStyle, sourceLineNumber, id)
        {
        }

        public IntermediateField this[TextStyleSymbolFields index] => this.Fields[(int)index];

        public string FaceName
        {
            get => (string)this.Fields[(int)TextStyleSymbolFields.FaceName];
            set => this.Set((int)TextStyleSymbolFields.FaceName, value);
        }

        public string LocalizedSize
        {
            get => (string)this.Fields[(int)TextStyleSymbolFields.Size];
            set => this.Set((int)TextStyleSymbolFields.Size, value);
        }

        public int Size
        {
            get => (int)this.Fields[(int)TextStyleSymbolFields.Size];
            set => this.Set((int)TextStyleSymbolFields.Size, value);
        }

        public int? Red
        {
            get => (int?)this.Fields[(int)TextStyleSymbolFields.Red];
            set => this.Set((int)TextStyleSymbolFields.Red, value);
        }

        public int? Green
        {
            get => (int?)this.Fields[(int)TextStyleSymbolFields.Green];
            set => this.Set((int)TextStyleSymbolFields.Green, value);
        }

        public int? Blue
        {
            get => (int?)this.Fields[(int)TextStyleSymbolFields.Blue];
            set => this.Set((int)TextStyleSymbolFields.Blue, value);
        }

        public bool Bold
        {
            get => (bool)this.Fields[(int)TextStyleSymbolFields.Bold];
            set => this.Set((int)TextStyleSymbolFields.Bold, value);
        }

        public bool Italic
        {
            get => (bool)this.Fields[(int)TextStyleSymbolFields.Italic];
            set => this.Set((int)TextStyleSymbolFields.Italic, value);
        }

        public bool Strike
        {
            get => (bool)this.Fields[(int)TextStyleSymbolFields.Strike];
            set => this.Set((int)TextStyleSymbolFields.Strike, value);
        }

        public bool Underline
        {
            get => (bool)this.Fields[(int)TextStyleSymbolFields.Underline];
            set => this.Set((int)TextStyleSymbolFields.Underline, value);
        }
    }
}