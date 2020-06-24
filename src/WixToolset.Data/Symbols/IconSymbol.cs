// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Icon = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Icon,
            new[]
            {
                new IntermediateFieldDefinition(nameof(IconSymbolFields.Data), IntermediateFieldType.Path),
            },
            typeof(IconSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum IconSymbolFields
    {
        Data,
    }

    public class IconSymbol : IntermediateSymbol
    {
        public IconSymbol() : base(SymbolDefinitions.Icon, null, null)
        {
        }

        public IconSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Icon, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IconSymbolFields index] => this.Fields[(int)index];

        public IntermediateFieldPathValue Data
        {
            get => this.Fields[(int)IconSymbolFields.Data].AsPath();
            set => this.Set((int)IconSymbolFields.Data, value);
        }
    }
}