// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleExePackagePayload = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleExePackagePayload,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixBundleExePackagePayloadSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleExePackagePayloadSymbolFields
    {
    }

    public class WixBundleExePackagePayloadSymbol : IntermediateSymbol
    {
        public WixBundleExePackagePayloadSymbol() : base(SymbolDefinitions.WixBundleExePackagePayload, null, null)
        {
        }

        public WixBundleExePackagePayloadSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleExePackagePayload, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleExePackagePayloadSymbolFields index] => this.Fields[(int)index];
    }
}
