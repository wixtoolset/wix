// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleCatalog = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleCatalog,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleCatalogSymbolFields.PayloadRef), IntermediateFieldType.String),
            },
            typeof(WixBundleCatalogSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleCatalogSymbolFields
    {
        PayloadRef,
    }

    public class WixBundleCatalogSymbol : IntermediateSymbol
    {
        public WixBundleCatalogSymbol() : base(SymbolDefinitions.WixBundleCatalog, null, null)
        {
        }

        public WixBundleCatalogSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleCatalog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleCatalogSymbolFields index] => this.Fields[(int)index];

        public string PayloadRef
        {
            get => (string)this.Fields[(int)WixBundleCatalogSymbolFields.PayloadRef];
            set => this.Set((int)WixBundleCatalogSymbolFields.PayloadRef, value);
        }
    }
}
