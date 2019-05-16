// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleAdvtExecuteSequence = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleAdvtExecuteSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleAdvtExecuteSequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleAdvtExecuteSequenceTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleAdvtExecuteSequenceTupleFields.BaseAction), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleAdvtExecuteSequenceTupleFields.After), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleAdvtExecuteSequenceTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(ModuleAdvtExecuteSequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleAdvtExecuteSequenceTupleFields
    {
        Action,
        Sequence,
        BaseAction,
        After,
        Condition,
    }

    public class ModuleAdvtExecuteSequenceTuple : IntermediateTuple
    {
        public ModuleAdvtExecuteSequenceTuple() : base(TupleDefinitions.ModuleAdvtExecuteSequence, null, null)
        {
        }

        public ModuleAdvtExecuteSequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleAdvtExecuteSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleAdvtExecuteSequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)ModuleAdvtExecuteSequenceTupleFields.Action];
            set => this.Set((int)ModuleAdvtExecuteSequenceTupleFields.Action, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)ModuleAdvtExecuteSequenceTupleFields.Sequence];
            set => this.Set((int)ModuleAdvtExecuteSequenceTupleFields.Sequence, value);
        }

        public string BaseAction
        {
            get => (string)this.Fields[(int)ModuleAdvtExecuteSequenceTupleFields.BaseAction];
            set => this.Set((int)ModuleAdvtExecuteSequenceTupleFields.BaseAction, value);
        }

        public int After
        {
            get => (int)this.Fields[(int)ModuleAdvtExecuteSequenceTupleFields.After];
            set => this.Set((int)ModuleAdvtExecuteSequenceTupleFields.After, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ModuleAdvtExecuteSequenceTupleFields.Condition];
            set => this.Set((int)ModuleAdvtExecuteSequenceTupleFields.Condition, value);
        }
    }
}