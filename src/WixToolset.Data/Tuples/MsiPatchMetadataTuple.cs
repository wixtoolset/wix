// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiPatchMetadata = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiPatchMetadata,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchMetadataSymbolFields.Company), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchMetadataSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchMetadataSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(MsiPatchMetadataSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiPatchMetadataSymbolFields
    {
        Company,
        Property,
        Value,
    }

    public class MsiPatchMetadataSymbol : IntermediateSymbol
    {
        public MsiPatchMetadataSymbol() : base(SymbolDefinitions.MsiPatchMetadata, null, null)
        {
        }

        public MsiPatchMetadataSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiPatchMetadata, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchMetadataSymbolFields index] => this.Fields[(int)index];

        public string Company
        {
            get => (string)this.Fields[(int)MsiPatchMetadataSymbolFields.Company];
            set => this.Set((int)MsiPatchMetadataSymbolFields.Company, value);
        }

        public string Property
        {
            get => (string)this.Fields[(int)MsiPatchMetadataSymbolFields.Property];
            set => this.Set((int)MsiPatchMetadataSymbolFields.Property, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)MsiPatchMetadataSymbolFields.Value];
            set => this.Set((int)MsiPatchMetadataSymbolFields.Value, value);
        }
    }
}