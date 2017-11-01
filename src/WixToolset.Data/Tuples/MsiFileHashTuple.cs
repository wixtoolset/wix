// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiFileHash = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiFileHash,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiFileHashTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiFileHashTupleFields.Options), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiFileHashTupleFields.HashPart1), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiFileHashTupleFields.HashPart2), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiFileHashTupleFields.HashPart3), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiFileHashTupleFields.HashPart4), IntermediateFieldType.Number),
            },
            typeof(MsiFileHashTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiFileHashTupleFields
    {
        File_,
        Options,
        HashPart1,
        HashPart2,
        HashPart3,
        HashPart4,
    }

    public class MsiFileHashTuple : IntermediateTuple
    {
        public MsiFileHashTuple() : base(TupleDefinitions.MsiFileHash, null, null)
        {
        }

        public MsiFileHashTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiFileHash, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiFileHashTupleFields index] => this.Fields[(int)index];

        public string File_
        {
            get => (string)this.Fields[(int)MsiFileHashTupleFields.File_]?.Value;
            set => this.Set((int)MsiFileHashTupleFields.File_, value);
        }

        public int Options
        {
            get => (int)this.Fields[(int)MsiFileHashTupleFields.Options]?.Value;
            set => this.Set((int)MsiFileHashTupleFields.Options, value);
        }

        public int HashPart1
        {
            get => (int)this.Fields[(int)MsiFileHashTupleFields.HashPart1]?.Value;
            set => this.Set((int)MsiFileHashTupleFields.HashPart1, value);
        }

        public int HashPart2
        {
            get => (int)this.Fields[(int)MsiFileHashTupleFields.HashPart2]?.Value;
            set => this.Set((int)MsiFileHashTupleFields.HashPart2, value);
        }

        public int HashPart3
        {
            get => (int)this.Fields[(int)MsiFileHashTupleFields.HashPart3]?.Value;
            set => this.Set((int)MsiFileHashTupleFields.HashPart3, value);
        }

        public int HashPart4
        {
            get => (int)this.Fields[(int)MsiFileHashTupleFields.HashPart4]?.Value;
            set => this.Set((int)MsiFileHashTupleFields.HashPart4, value);
        }
    }
}