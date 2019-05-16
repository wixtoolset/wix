// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiEmbeddedChainer = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiEmbeddedChainer,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiEmbeddedChainerTupleFields.MsiEmbeddedChainer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedChainerTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedChainerTupleFields.CommandLine), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedChainerTupleFields.Source), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedChainerTupleFields.Type), IntermediateFieldType.Number),
            },
            typeof(MsiEmbeddedChainerTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiEmbeddedChainerTupleFields
    {
        MsiEmbeddedChainer,
        Condition,
        CommandLine,
        Source,
        Type,
    }

    public class MsiEmbeddedChainerTuple : IntermediateTuple
    {
        public MsiEmbeddedChainerTuple() : base(TupleDefinitions.MsiEmbeddedChainer, null, null)
        {
        }

        public MsiEmbeddedChainerTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiEmbeddedChainer, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiEmbeddedChainerTupleFields index] => this.Fields[(int)index];

        public string MsiEmbeddedChainer
        {
            get => (string)this.Fields[(int)MsiEmbeddedChainerTupleFields.MsiEmbeddedChainer];
            set => this.Set((int)MsiEmbeddedChainerTupleFields.MsiEmbeddedChainer, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)MsiEmbeddedChainerTupleFields.Condition];
            set => this.Set((int)MsiEmbeddedChainerTupleFields.Condition, value);
        }

        public string CommandLine
        {
            get => (string)this.Fields[(int)MsiEmbeddedChainerTupleFields.CommandLine];
            set => this.Set((int)MsiEmbeddedChainerTupleFields.CommandLine, value);
        }

        public string Source
        {
            get => (string)this.Fields[(int)MsiEmbeddedChainerTupleFields.Source];
            set => this.Set((int)MsiEmbeddedChainerTupleFields.Source, value);
        }

        public int Type
        {
            get => (int)this.Fields[(int)MsiEmbeddedChainerTupleFields.Type];
            set => this.Set((int)MsiEmbeddedChainerTupleFields.Type, value);
        }
    }
}