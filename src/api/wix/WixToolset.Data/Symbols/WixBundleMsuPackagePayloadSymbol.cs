// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleMsuPackagePayload = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleMsuPackagePayload,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixBundleMsuPackagePayloadSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleMsuPackagePayloadSymbolFields
    {
    }

    public class WixBundleMsuPackagePayloadSymbol : IntermediateSymbol
    {
        public WixBundleMsuPackagePayloadSymbol() : base(SymbolDefinitions.WixBundleMsuPackagePayload, null, null)
        {
        }

        public WixBundleMsuPackagePayloadSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleMsuPackagePayload, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsuPackagePayloadSymbolFields index] => this.Fields[(int)index];
    }
}
