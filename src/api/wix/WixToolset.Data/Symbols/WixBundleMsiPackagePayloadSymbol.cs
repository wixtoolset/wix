// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleMsiPackagePayload = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleMsiPackagePayload,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixBundleMsiPackagePayloadSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleMsiPackagePayloadSymbolFields
    {
    }

    public class WixBundleMsiPackagePayloadSymbol : IntermediateSymbol
    {
        public WixBundleMsiPackagePayloadSymbol() : base(SymbolDefinitions.WixBundleMsiPackagePayload, null, null)
        {
        }

        public WixBundleMsiPackagePayloadSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleMsiPackagePayload, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsiPackagePayloadSymbolFields index] => this.Fields[(int)index];
    }
}
