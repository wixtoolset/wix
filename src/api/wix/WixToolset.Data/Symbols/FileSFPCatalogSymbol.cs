// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition FileSFPCatalog = new IntermediateSymbolDefinition(
            SymbolDefinitionType.FileSFPCatalog,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileSFPCatalogSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSFPCatalogSymbolFields.SFPCatalogRef), IntermediateFieldType.String),
            },
            typeof(FileSFPCatalogSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum FileSFPCatalogSymbolFields
    {
        FileRef,
        SFPCatalogRef,
    }

    public class FileSFPCatalogSymbol : IntermediateSymbol
    {
        public FileSFPCatalogSymbol() : base(SymbolDefinitions.FileSFPCatalog, null, null)
        {
        }

        public FileSFPCatalogSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.FileSFPCatalog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileSFPCatalogSymbolFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => (string)this.Fields[(int)FileSFPCatalogSymbolFields.FileRef];
            set => this.Set((int)FileSFPCatalogSymbolFields.FileRef, value);
        }

        public string SFPCatalogRef
        {
            get => (string)this.Fields[(int)FileSFPCatalogSymbolFields.SFPCatalogRef];
            set => this.Set((int)FileSFPCatalogSymbolFields.SFPCatalogRef, value);
        }
    }
}