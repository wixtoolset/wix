// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Patch = new IntermediateTupleDefinition(
            TupleDefinitionType.Patch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PatchTupleFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(PatchTupleFields.PatchSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(PatchTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(PatchTupleFields.Header), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(PatchTupleFields.StreamRef), IntermediateFieldType.String),
            },
            typeof(PatchTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum PatchTupleFields
    {
        FileRef,
        Sequence,
        PatchSize,
        Attributes,
        Header,
        StreamRef,
    }

    public class PatchTuple : IntermediateTuple
    {
        public PatchTuple() : base(TupleDefinitions.Patch, null, null)
        {
        }

        public PatchTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Patch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PatchTupleFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => (string)this.Fields[(int)PatchTupleFields.FileRef];
            set => this.Set((int)PatchTupleFields.FileRef, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)PatchTupleFields.Sequence];
            set => this.Set((int)PatchTupleFields.Sequence, value);
        }

        public int PatchSize
        {
            get => (int)this.Fields[(int)PatchTupleFields.PatchSize];
            set => this.Set((int)PatchTupleFields.PatchSize, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)PatchTupleFields.Attributes];
            set => this.Set((int)PatchTupleFields.Attributes, value);
        }

        public string Header
        {
            get => (string)this.Fields[(int)PatchTupleFields.Header];
            set => this.Set((int)PatchTupleFields.Header, value);
        }

        public string StreamRef
        {
            get => (string)this.Fields[(int)PatchTupleFields.StreamRef];
            set => this.Set((int)PatchTupleFields.StreamRef, value);
        }
    }
}