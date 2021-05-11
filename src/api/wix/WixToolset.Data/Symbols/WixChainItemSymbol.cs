// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixChainItem = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixChainItem,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixChainItemSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixChainItemSymbolFields
    {
    }

    public class WixChainItemSymbol : IntermediateSymbol
    {
        public WixChainItemSymbol() : base(SymbolDefinitions.WixChainItem, null, null)
        {
        }

        public WixChainItemSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixChainItem, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixChainItemSymbolFields index] => this.Fields[(int)index];
    }
}