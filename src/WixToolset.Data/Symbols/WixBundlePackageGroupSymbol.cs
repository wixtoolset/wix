// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundlePackageGroup = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundlePackageGroup,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(WixBundlePackageGroupSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundlePackageGroupSymbolFields
    {
    }

    public class WixBundlePackageGroupSymbol : IntermediateSymbol
    {
        public WixBundlePackageGroupSymbol() : base(SymbolDefinitions.WixBundlePackageGroup, null, null)
        {
        }

        public WixBundlePackageGroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundlePackageGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePackageGroupSymbolFields index] => this.Fields[(int)index];
    }
}