// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition CCPSearch = new IntermediateSymbolDefinition(
            SymbolDefinitionType.CCPSearch,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(CCPSearchSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum CCPSearchSymbolFields
    {
    }

    public class CCPSearchSymbol : IntermediateSymbol
    {
        public CCPSearchSymbol() : base(SymbolDefinitions.CCPSearch, null, null)
        {
        }

        public CCPSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.CCPSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CCPSearchSymbolFields index] => this.Fields[(int)index];
    }
}