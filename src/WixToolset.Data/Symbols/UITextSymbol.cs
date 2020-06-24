// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition UIText = new IntermediateSymbolDefinition(
            SymbolDefinitionType.UIText,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UITextSymbolFields.Text), IntermediateFieldType.String),
            },
            typeof(UITextSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum UITextSymbolFields
    {
        Text,
    }

    public class UITextSymbol : IntermediateSymbol
    {
        public UITextSymbol() : base(SymbolDefinitions.UIText, null, null)
        {
        }

        public UITextSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.UIText, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UITextSymbolFields index] => this.Fields[(int)index];

        public string Text
        {
            get => (string)this.Fields[(int)UITextSymbolFields.Text];
            set => this.Set((int)UITextSymbolFields.Text, value);
        }
    }
}
