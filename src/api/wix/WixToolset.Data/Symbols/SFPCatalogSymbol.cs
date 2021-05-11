// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition SFPCatalog = new IntermediateSymbolDefinition(
            SymbolDefinitionType.SFPCatalog,
            new[]
            {
                new IntermediateFieldDefinition(nameof(SFPCatalogSymbolFields.SFPCatalog), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SFPCatalogSymbolFields.Catalog), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(SFPCatalogSymbolFields.Dependency), IntermediateFieldType.String),
            },
            typeof(SFPCatalogSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum SFPCatalogSymbolFields
    {
        SFPCatalog,
        Catalog,
        Dependency,
    }

    public class SFPCatalogSymbol : IntermediateSymbol
    {
        public SFPCatalogSymbol() : base(SymbolDefinitions.SFPCatalog, null, null)
        {
        }

        public SFPCatalogSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.SFPCatalog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SFPCatalogSymbolFields index] => this.Fields[(int)index];

        public string SFPCatalog
        {
            get => (string)this.Fields[(int)SFPCatalogSymbolFields.SFPCatalog];
            set => this.Set((int)SFPCatalogSymbolFields.SFPCatalog, value);
        }

        public string Catalog
        {
            get => (string)this.Fields[(int)SFPCatalogSymbolFields.Catalog];
            set => this.Set((int)SFPCatalogSymbolFields.Catalog, value);
        }

        public string Dependency
        {
            get => (string)this.Fields[(int)SFPCatalogSymbolFields.Dependency];
            set => this.Set((int)SFPCatalogSymbolFields.Dependency, value);
        }
    }
}