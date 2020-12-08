// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBootstrapperApplication = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBootstrapperApplication,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixBootstrapperApplicationSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBootstrapperApplicationSymbolFields
    {
    }

    public class WixBootstrapperApplicationSymbol : IntermediateSymbol
    {
        public WixBootstrapperApplicationSymbol() : base(SymbolDefinitions.WixBootstrapperApplication, null, null)
        {
        }

        public WixBootstrapperApplicationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBootstrapperApplication, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBootstrapperApplicationSymbolFields index] => this.Fields[(int)index];
    }
}
