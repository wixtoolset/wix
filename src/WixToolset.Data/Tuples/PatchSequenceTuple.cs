// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition PatchSequence = new IntermediateTupleDefinition(
            TupleDefinitionType.PatchSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PatchSequenceTupleFields.PatchFamily), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchSequenceTupleFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchSequenceTupleFields.Sequence), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchSequenceTupleFields.Supersede), IntermediateFieldType.Number),
            },
            typeof(PatchSequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum PatchSequenceTupleFields
    {
        PatchFamily,
        Target,
        Sequence,
        Supersede,
    }

    public class PatchSequenceTuple : IntermediateTuple
    {
        public PatchSequenceTuple() : base(TupleDefinitions.PatchSequence, null, null)
        {
        }

        public PatchSequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.PatchSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PatchSequenceTupleFields index] => this.Fields[(int)index];

        public string PatchFamily
        {
            get => (string)this.Fields[(int)PatchSequenceTupleFields.PatchFamily];
            set => this.Set((int)PatchSequenceTupleFields.PatchFamily, value);
        }

        public string Target
        {
            get => (string)this.Fields[(int)PatchSequenceTupleFields.Target];
            set => this.Set((int)PatchSequenceTupleFields.Target, value);
        }

        public string Sequence
        {
            get => (string)this.Fields[(int)PatchSequenceTupleFields.Sequence];
            set => this.Set((int)PatchSequenceTupleFields.Sequence, value);
        }

        public int? Supersede
        {
            get => (int?)this.Fields[(int)PatchSequenceTupleFields.Supersede];
            set => this.Set((int)PatchSequenceTupleFields.Supersede, value);
        }
    }
}