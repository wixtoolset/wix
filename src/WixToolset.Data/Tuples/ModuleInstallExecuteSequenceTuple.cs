// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleInstallExecuteSequence = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleInstallExecuteSequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleInstallExecuteSequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleInstallExecuteSequenceTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleInstallExecuteSequenceTupleFields.BaseAction), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleInstallExecuteSequenceTupleFields.After), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleInstallExecuteSequenceTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(ModuleInstallExecuteSequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleInstallExecuteSequenceTupleFields
    {
        Action,
        Sequence,
        BaseAction,
        After,
        Condition,
    }

    public class ModuleInstallExecuteSequenceTuple : IntermediateTuple
    {
        public ModuleInstallExecuteSequenceTuple() : base(TupleDefinitions.ModuleInstallExecuteSequence, null, null)
        {
        }

        public ModuleInstallExecuteSequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleInstallExecuteSequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleInstallExecuteSequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)ModuleInstallExecuteSequenceTupleFields.Action]?.Value;
            set => this.Set((int)ModuleInstallExecuteSequenceTupleFields.Action, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)ModuleInstallExecuteSequenceTupleFields.Sequence]?.Value;
            set => this.Set((int)ModuleInstallExecuteSequenceTupleFields.Sequence, value);
        }

        public string BaseAction
        {
            get => (string)this.Fields[(int)ModuleInstallExecuteSequenceTupleFields.BaseAction]?.Value;
            set => this.Set((int)ModuleInstallExecuteSequenceTupleFields.BaseAction, value);
        }

        public int After
        {
            get => (int)this.Fields[(int)ModuleInstallExecuteSequenceTupleFields.After]?.Value;
            set => this.Set((int)ModuleInstallExecuteSequenceTupleFields.After, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ModuleInstallExecuteSequenceTupleFields.Condition]?.Value;
            set => this.Set((int)ModuleInstallExecuteSequenceTupleFields.Condition, value);
        }
    }
}