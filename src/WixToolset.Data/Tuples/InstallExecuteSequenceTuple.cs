// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition InstallExecuteSequence = new IntermediateTupleDefinition(
            TupleDefinitionType.InstallExecuteSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(InstallExecuteSequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(InstallExecuteSequenceTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(InstallExecuteSequenceTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(InstallExecuteSequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum InstallExecuteSequenceTupleFields
    {
        Action,
        Condition,
        Sequence,
    }

    public class InstallExecuteSequenceTuple : IntermediateTuple
    {
        public InstallExecuteSequenceTuple() : base(TupleDefinitions.InstallExecuteSequence, null, null)
        {
        }

        public InstallExecuteSequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.InstallExecuteSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[InstallExecuteSequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)InstallExecuteSequenceTupleFields.Action];
            set => this.Set((int)InstallExecuteSequenceTupleFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)InstallExecuteSequenceTupleFields.Condition];
            set => this.Set((int)InstallExecuteSequenceTupleFields.Condition, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)InstallExecuteSequenceTupleFields.Sequence];
            set => this.Set((int)InstallExecuteSequenceTupleFields.Sequence, value);
        }
    }
}