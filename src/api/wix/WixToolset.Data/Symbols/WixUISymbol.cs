// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixUI = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixUI,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixUISymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixUISymbolFields
    {
    }

    public class WixUISymbol : IntermediateSymbol
    {
        public WixUISymbol() : base(SymbolDefinitions.WixUI, null, null)
        {
        }

        public WixUISymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixUI, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixUISymbolFields index] => this.Fields[(int)index];
    }
}
