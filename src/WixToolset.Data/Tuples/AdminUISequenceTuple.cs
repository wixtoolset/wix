// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition AdminUISequence = new IntermediateTupleDefinition(
            TupleDefinitionType.AdminUISequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(AdminUISequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AdminUISequenceTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AdminUISequenceTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(AdminUISequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum AdminUISequenceTupleFields
    {
        Action,
        Condition,
        Sequence,
    }

    public class AdminUISequenceTuple : IntermediateTuple
    {
        public AdminUISequenceTuple() : base(TupleDefinitions.AdminUISequence, null, null)
        {
        }

        public AdminUISequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.AdminUISequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[AdminUISequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)AdminUISequenceTupleFields.Action]?.Value;
            set => this.Set((int)AdminUISequenceTupleFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)AdminUISequenceTupleFields.Condition]?.Value;
            set => this.Set((int)AdminUISequenceTupleFields.Condition, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)AdminUISequenceTupleFields.Sequence]?.Value;
            set => this.Set((int)AdminUISequenceTupleFields.Sequence, value);
        }
    }
}