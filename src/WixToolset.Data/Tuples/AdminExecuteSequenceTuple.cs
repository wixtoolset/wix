// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition AdminExecuteSequence = new IntermediateTupleDefinition(
            TupleDefinitionType.AdminExecuteSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(AdminExecuteSequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AdminExecuteSequenceTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AdminExecuteSequenceTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(AdminExecuteSequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum AdminExecuteSequenceTupleFields
    {
        Action,
        Condition,
        Sequence,
    }

    public class AdminExecuteSequenceTuple : IntermediateTuple
    {
        public AdminExecuteSequenceTuple() : base(TupleDefinitions.AdminExecuteSequence, null, null)
        {
        }

        public AdminExecuteSequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.AdminExecuteSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[AdminExecuteSequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)AdminExecuteSequenceTupleFields.Action]?.Value;
            set => this.Set((int)AdminExecuteSequenceTupleFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)AdminExecuteSequenceTupleFields.Condition]?.Value;
            set => this.Set((int)AdminExecuteSequenceTupleFields.Condition, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)AdminExecuteSequenceTupleFields.Sequence]?.Value;
            set => this.Set((int)AdminExecuteSequenceTupleFields.Sequence, value);
        }
    }
}