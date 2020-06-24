// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundlePayloadGroup = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundlePayloadGroup,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixBundlePayloadGroupSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundlePayloadGroupSymbolFields
    {
    }

    public class WixBundlePayloadGroupSymbol : IntermediateSymbol
    {
        public WixBundlePayloadGroupSymbol() : base(SymbolDefinitions.WixBundlePayloadGroup, null, null)
        {
        }

        public WixBundlePayloadGroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundlePayloadGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePayloadGroupSymbolFields index] => this.Fields[(int)index];
    }
}