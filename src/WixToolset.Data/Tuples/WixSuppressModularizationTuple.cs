// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixSuppressModularization = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixSuppressModularization,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixSuppressModularizationSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixSuppressModularizationSymbolFields
    {
    }

    public class WixSuppressModularizationSymbol : IntermediateSymbol
    {
        public WixSuppressModularizationSymbol() : base(SymbolDefinitions.WixSuppressModularization, null, null)
        {
        }

        public WixSuppressModularizationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixSuppressModularization, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSuppressModularizationSymbolFields index] => this.Fields[(int)index];
    }
}