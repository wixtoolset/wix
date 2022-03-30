// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleBundlePackagePayload = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleBundlePackagePayload,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixBundleBundlePackagePayloadSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleBundlePackagePayloadSymbolFields
    {
    }

    public class WixBundleBundlePackagePayloadSymbol : IntermediateSymbol
    {
        public WixBundleBundlePackagePayloadSymbol() : base(SymbolDefinitions.WixBundleBundlePackagePayload, null, null)
        {
        }

        public WixBundleBundlePackagePayloadSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleBundlePackagePayload, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleBundlePackagePayloadSymbolFields index] => this.Fields[(int)index];
    }
}
