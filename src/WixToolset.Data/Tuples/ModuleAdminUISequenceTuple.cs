// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleAdminUISequence = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleAdminUISequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleAdminUISequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleAdminUISequenceTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleAdminUISequenceTupleFields.BaseAction), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleAdminUISequenceTupleFields.After), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleAdminUISequenceTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(ModuleAdminUISequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleAdminUISequenceTupleFields
    {
        Action,
        Sequence,
        BaseAction,
        After,
        Condition,
    }

    public class ModuleAdminUISequenceTuple : IntermediateTuple
    {
        public ModuleAdminUISequenceTuple() : base(TupleDefinitions.ModuleAdminUISequence, null, null)
        {
        }

        public ModuleAdminUISequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleAdminUISequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleAdminUISequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)ModuleAdminUISequenceTupleFields.Action]?.Value;
            set => this.Set((int)ModuleAdminUISequenceTupleFields.Action, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)ModuleAdminUISequenceTupleFields.Sequence]?.Value;
            set => this.Set((int)ModuleAdminUISequenceTupleFields.Sequence, value);
        }

        public string BaseAction
        {
            get => (string)this.Fields[(int)ModuleAdminUISequenceTupleFields.BaseAction]?.Value;
            set => this.Set((int)ModuleAdminUISequenceTupleFields.BaseAction, value);
        }

        public int After
        {
            get => (int)this.Fields[(int)ModuleAdminUISequenceTupleFields.After]?.Value;
            set => this.Set((int)ModuleAdminUISequenceTupleFields.After, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ModuleAdminUISequenceTupleFields.Condition]?.Value;
            set => this.Set((int)ModuleAdminUISequenceTupleFields.Condition, value);
        }
    }
}