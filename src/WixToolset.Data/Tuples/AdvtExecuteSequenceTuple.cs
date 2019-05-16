// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition AdvtExecuteSequence = new IntermediateTupleDefinition(
            TupleDefinitionType.AdvtExecuteSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(AdvtExecuteSequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AdvtExecuteSequenceTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AdvtExecuteSequenceTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(AdvtExecuteSequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum AdvtExecuteSequenceTupleFields
    {
        Action,
        Condition,
        Sequence,
    }

    public class AdvtExecuteSequenceTuple : IntermediateTuple
    {
        public AdvtExecuteSequenceTuple() : base(TupleDefinitions.AdvtExecuteSequence, null, null)
        {
        }

        public AdvtExecuteSequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.AdvtExecuteSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[AdvtExecuteSequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)AdvtExecuteSequenceTupleFields.Action];
            set => this.Set((int)AdvtExecuteSequenceTupleFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)AdvtExecuteSequenceTupleFields.Condition];
            set => this.Set((int)AdvtExecuteSequenceTupleFields.Condition, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)AdvtExecuteSequenceTupleFields.Sequence];
            set => this.Set((int)AdvtExecuteSequenceTupleFields.Sequence, value);
        }
    }
}