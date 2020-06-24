// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition PatchMetadata = new IntermediateSymbolDefinition(
            SymbolDefinitionType.PatchMetadata,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PatchMetadataSymbolFields.Company), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchMetadataSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchMetadataSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(PatchMetadataSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum PatchMetadataSymbolFields
    {
        Company,
        Property,
        Value,
    }

    public class PatchMetadataSymbol : IntermediateSymbol
    {
        public PatchMetadataSymbol() : base(SymbolDefinitions.PatchMetadata, null, null)
        {
        }

        public PatchMetadataSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.PatchMetadata, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PatchMetadataSymbolFields index] => this.Fields[(int)index];

        public string Company
        {
            get => (string)this.Fields[(int)PatchMetadataSymbolFields.Company];
            set => this.Set((int)PatchMetadataSymbolFields.Company, value);
        }

        public string Property
        {
            get => (string)this.Fields[(int)PatchMetadataSymbolFields.Property];
            set => this.Set((int)PatchMetadataSymbolFields.Property, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)PatchMetadataSymbolFields.Value];
            set => this.Set((int)PatchMetadataSymbolFields.Value, value);
        }
    }
}