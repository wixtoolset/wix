// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition PatchSequence = new IntermediateSymbolDefinition(
            SymbolDefinitionType.PatchSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PatchSequenceSymbolFields.PatchFamily), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchSequenceSymbolFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchSequenceSymbolFields.Sequence), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchSequenceSymbolFields.Supersede), IntermediateFieldType.Number),
            },
            typeof(PatchSequenceSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum PatchSequenceSymbolFields
    {
        PatchFamily,
        Target,
        Sequence,
        Supersede,
    }

    public class PatchSequenceSymbol : IntermediateSymbol
    {
        public PatchSequenceSymbol() : base(SymbolDefinitions.PatchSequence, null, null)
        {
        }

        public PatchSequenceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.PatchSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PatchSequenceSymbolFields index] => this.Fields[(int)index];

        public string PatchFamily
        {
            get => (string)this.Fields[(int)PatchSequenceSymbolFields.PatchFamily];
            set => this.Set((int)PatchSequenceSymbolFields.PatchFamily, value);
        }

        public string Target
        {
            get => (string)this.Fields[(int)PatchSequenceSymbolFields.Target];
            set => this.Set((int)PatchSequenceSymbolFields.Target, value);
        }

        public string Sequence
        {
            get => (string)this.Fields[(int)PatchSequenceSymbolFields.Sequence];
            set => this.Set((int)PatchSequenceSymbolFields.Sequence, value);
        }

        public int? Supersede
        {
            get => (int?)this.Fields[(int)PatchSequenceSymbolFields.Supersede];
            set => this.Set((int)PatchSequenceSymbolFields.Supersede, value);
        }
    }
}