// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixAction = new IntermediateTupleDefinition(
            TupleDefinitionType.WixAction,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixActionTupleFields.SequenceTable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixActionTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixActionTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixActionTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixActionTupleFields.Before), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixActionTupleFields.After), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixActionTupleFields.Overridable), IntermediateFieldType.Bool),
            },
            typeof(WixActionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixActionTupleFields
    {
        SequenceTable,
        Action,
        Condition,
        Sequence,
        Before,
        After,
        Overridable,
    }

    public enum SequenceTable
    {
        AdminUISequence,
        AdminExecuteSequence,
        AdvtExecuteSequence,
        InstallUISequence,
        InstallExecuteSequence
    }

    public class WixActionTuple : IntermediateTuple
    {
        public WixActionTuple() : base(TupleDefinitions.WixAction, null, null)
        {
        }

        public WixActionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixAction, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixActionTupleFields index] => this.Fields[(int)index];

        public SequenceTable SequenceTable
        {
            get => (SequenceTable)Enum.Parse(typeof(SequenceTable), (string)this.Fields[(int)WixActionTupleFields.SequenceTable]?.Value);
            set => this.Set((int)WixActionTupleFields.SequenceTable, value.ToString());
        }

        public string Action
        {
            get => (string)this.Fields[(int)WixActionTupleFields.Action]?.Value;
            set => this.Set((int)WixActionTupleFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixActionTupleFields.Condition]?.Value;
            set => this.Set((int)WixActionTupleFields.Condition, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)WixActionTupleFields.Sequence]?.Value;
            set => this.Set((int)WixActionTupleFields.Sequence, value);
        }

        public string Before
        {
            get => (string)this.Fields[(int)WixActionTupleFields.Before]?.Value;
            set => this.Set((int)WixActionTupleFields.Before, value);
        }

        public string After
        {
            get => (string)this.Fields[(int)WixActionTupleFields.After]?.Value;
            set => this.Set((int)WixActionTupleFields.After, value);
        }

        public bool Overridable
        {
            get => (bool)this.Fields[(int)WixActionTupleFields.Overridable]?.Value;
            set => this.Set((int)WixActionTupleFields.Overridable, value);
        }
    }
}