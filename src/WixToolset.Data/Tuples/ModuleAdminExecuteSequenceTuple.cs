// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleAdminExecuteSequence = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleAdminExecuteSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleAdminExecuteSequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleAdminExecuteSequenceTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleAdminExecuteSequenceTupleFields.BaseAction), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleAdminExecuteSequenceTupleFields.After), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleAdminExecuteSequenceTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(ModuleAdminExecuteSequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleAdminExecuteSequenceTupleFields
    {
        Action,
        Sequence,
        BaseAction,
        After,
        Condition,
    }

    public class ModuleAdminExecuteSequenceTuple : IntermediateTuple
    {
        public ModuleAdminExecuteSequenceTuple() : base(TupleDefinitions.ModuleAdminExecuteSequence, null, null)
        {
        }

        public ModuleAdminExecuteSequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleAdminExecuteSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleAdminExecuteSequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)ModuleAdminExecuteSequenceTupleFields.Action]?.Value;
            set => this.Set((int)ModuleAdminExecuteSequenceTupleFields.Action, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)ModuleAdminExecuteSequenceTupleFields.Sequence]?.Value;
            set => this.Set((int)ModuleAdminExecuteSequenceTupleFields.Sequence, value);
        }

        public string BaseAction
        {
            get => (string)this.Fields[(int)ModuleAdminExecuteSequenceTupleFields.BaseAction]?.Value;
            set => this.Set((int)ModuleAdminExecuteSequenceTupleFields.BaseAction, value);
        }

        public int After
        {
            get => (int)this.Fields[(int)ModuleAdminExecuteSequenceTupleFields.After]?.Value;
            set => this.Set((int)ModuleAdminExecuteSequenceTupleFields.After, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ModuleAdminExecuteSequenceTupleFields.Condition]?.Value;
            set => this.Set((int)ModuleAdminExecuteSequenceTupleFields.Condition, value);
        }
    }
}