// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixFeatureGroup = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixFeatureGroup,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixFeatureGroupSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixFeatureGroupSymbolFields
    {
    }

    public class WixFeatureGroupSymbol : IntermediateSymbol
    {
        public WixFeatureGroupSymbol() : base(SymbolDefinitions.WixFeatureGroup, null, null)
        {
        }

        public WixFeatureGroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixFeatureGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFeatureGroupSymbolFields index] => this.Fields[(int)index];
    }
}