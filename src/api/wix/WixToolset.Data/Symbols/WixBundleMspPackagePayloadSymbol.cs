// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleMspPackagePayload = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleMspPackagePayload,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixBundleMspPackagePayloadSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleMspPackagePayloadSymbolFields
    {
    }

    public class WixBundleMspPackagePayloadSymbol : IntermediateSymbol
    {
        public WixBundleMspPackagePayloadSymbol() : base(SymbolDefinitions.WixBundleMspPackagePayload, null, null)
        {
        }

        public WixBundleMspPackagePayloadSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleMspPackagePayload, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMspPackagePayloadSymbolFields index] => this.Fields[(int)index];
    }
}
