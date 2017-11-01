// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition BindImage = new IntermediateTupleDefinition(
            TupleDefinitionType.BindImage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(BindImageTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BindImageTupleFields.Path), IntermediateFieldType.String),
            },
            typeof(BindImageTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum BindImageTupleFields
    {
        File_,
        Path,
    }

    public class BindImageTuple : IntermediateTuple
    {
        public BindImageTuple() : base(TupleDefinitions.BindImage, null, null)
        {
        }

        public BindImageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.BindImage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[BindImageTupleFields index] => this.Fields[(int)index];

        public string File_
        {
            get => (string)this.Fields[(int)BindImageTupleFields.File_]?.Value;
            set => this.Set((int)BindImageTupleFields.File_, value);
        }

        public string Path
        {
            get => (string)this.Fields[(int)BindImageTupleFields.Path]?.Value;
            set => this.Set((int)BindImageTupleFields.Path, value);
        }
    }
}