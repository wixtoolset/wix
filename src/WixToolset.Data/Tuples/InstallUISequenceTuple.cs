// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition InstallUISequence = new IntermediateTupleDefinition(
            TupleDefinitionType.InstallUISequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(InstallUISequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(InstallUISequenceTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(InstallUISequenceTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(InstallUISequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum InstallUISequenceTupleFields
    {
        Action,
        Condition,
        Sequence,
    }

    public class InstallUISequenceTuple : IntermediateTuple
    {
        public InstallUISequenceTuple() : base(TupleDefinitions.InstallUISequence, null, null)
        {
        }

        public InstallUISequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.InstallUISequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[InstallUISequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)InstallUISequenceTupleFields.Action]?.Value;
            set => this.Set((int)InstallUISequenceTupleFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)InstallUISequenceTupleFields.Condition]?.Value;
            set => this.Set((int)InstallUISequenceTupleFields.Condition, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)InstallUISequenceTupleFields.Sequence]?.Value;
            set => this.Set((int)InstallUISequenceTupleFields.Sequence, value);
        }
    }
}