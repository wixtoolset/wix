// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixComponentGroup = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixComponentGroup,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixComponentGroupSymbolFields.WixComponentGroup), IntermediateFieldType.String),
            },
            typeof(WixComponentGroupSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixComponentGroupSymbolFields
    {
        WixComponentGroup,
    }

    public class WixComponentGroupSymbol : IntermediateSymbol
    {
        public WixComponentGroupSymbol() : base(SymbolDefinitions.WixComponentGroup, null, null)
        {
        }

        public WixComponentGroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixComponentGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixComponentGroupSymbolFields index] => this.Fields[(int)index];

        public string WixComponentGroup
        {
            get => (string)this.Fields[(int)WixComponentGroupSymbolFields.WixComponentGroup];
            set => this.Set((int)WixComponentGroupSymbolFields.WixComponentGroup, value);
        }
    }
}