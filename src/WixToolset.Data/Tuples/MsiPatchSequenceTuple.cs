// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiPatchSequence = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiPatchSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchSequenceTupleFields.PatchFamily), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchSequenceTupleFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchSequenceTupleFields.Sequence), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchSequenceTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(MsiPatchSequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiPatchSequenceTupleFields
    {
        PatchFamily,
        ProductCode,
        Sequence,
        Attributes,
    }

    public class MsiPatchSequenceTuple : IntermediateTuple
    {
        public MsiPatchSequenceTuple() : base(TupleDefinitions.MsiPatchSequence, null, null)
        {
        }

        public MsiPatchSequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiPatchSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchSequenceTupleFields index] => this.Fields[(int)index];

        public string PatchFamily
        {
            get => (string)this.Fields[(int)MsiPatchSequenceTupleFields.PatchFamily]?.Value;
            set => this.Set((int)MsiPatchSequenceTupleFields.PatchFamily, value);
        }

        public string ProductCode
        {
            get => (string)this.Fields[(int)MsiPatchSequenceTupleFields.ProductCode]?.Value;
            set => this.Set((int)MsiPatchSequenceTupleFields.ProductCode, value);
        }

        public string Sequence
        {
            get => (string)this.Fields[(int)MsiPatchSequenceTupleFields.Sequence]?.Value;
            set => this.Set((int)MsiPatchSequenceTupleFields.Sequence, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)MsiPatchSequenceTupleFields.Attributes]?.Value;
            set => this.Set((int)MsiPatchSequenceTupleFields.Attributes, value);
        }
    }
}