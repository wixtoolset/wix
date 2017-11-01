// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Font = new IntermediateTupleDefinition(
            TupleDefinitionType.Font,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FontTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FontTupleFields.FontTitle), IntermediateFieldType.String),
            },
            typeof(FontTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FontTupleFields
    {
        File_,
        FontTitle,
    }

    public class FontTuple : IntermediateTuple
    {
        public FontTuple() : base(TupleDefinitions.Font, null, null)
        {
        }

        public FontTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Font, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FontTupleFields index] => this.Fields[(int)index];

        public string File_
        {
            get => (string)this.Fields[(int)FontTupleFields.File_]?.Value;
            set => this.Set((int)FontTupleFields.File_, value);
        }

        public string FontTitle
        {
            get => (string)this.Fields[(int)FontTupleFields.FontTitle]?.Value;
            set => this.Set((int)FontTupleFields.FontTitle, value);
        }
    }
}