// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiPatchSequence = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiPatchSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchSequenceSymbolFields.PatchFamily), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchSequenceSymbolFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchSequenceSymbolFields.Sequence), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchSequenceSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(MsiPatchSequenceSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiPatchSequenceSymbolFields
    {
        PatchFamily,
        ProductCode,
        Sequence,
        Attributes,
    }

    public class MsiPatchSequenceSymbol : IntermediateSymbol
    {
        public MsiPatchSequenceSymbol() : base(SymbolDefinitions.MsiPatchSequence, null, null)
        {
        }

        public MsiPatchSequenceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiPatchSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchSequenceSymbolFields index] => this.Fields[(int)index];

        public string PatchFamily
        {
            get => (string)this.Fields[(int)MsiPatchSequenceSymbolFields.PatchFamily];
            set => this.Set((int)MsiPatchSequenceSymbolFields.PatchFamily, value);
        }

        public string ProductCode
        {
            get => (string)this.Fields[(int)MsiPatchSequenceSymbolFields.ProductCode];
            set => this.Set((int)MsiPatchSequenceSymbolFields.ProductCode, value);
        }

        public string Sequence
        {
            get => (string)this.Fields[(int)MsiPatchSequenceSymbolFields.Sequence];
            set => this.Set((int)MsiPatchSequenceSymbolFields.Sequence, value);
        }

        public int? Attributes
        {
            get => (int?)this.Fields[(int)MsiPatchSequenceSymbolFields.Attributes];
            set => this.Set((int)MsiPatchSequenceSymbolFields.Attributes, value);
        }
    }
}