// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleInstallUISequence = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleInstallUISequence,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleInstallUISequenceTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleInstallUISequenceTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleInstallUISequenceTupleFields.BaseAction), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleInstallUISequenceTupleFields.After), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleInstallUISequenceTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(ModuleInstallUISequenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleInstallUISequenceTupleFields
    {
        Action,
        Sequence,
        BaseAction,
        After,
        Condition,
    }

    public class ModuleInstallUISequenceTuple : IntermediateTuple
    {
        public ModuleInstallUISequenceTuple() : base(TupleDefinitions.ModuleInstallUISequence, null, null)
        {
        }

        public ModuleInstallUISequenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleInstallUISequence, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleInstallUISequenceTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)ModuleInstallUISequenceTupleFields.Action];
            set => this.Set((int)ModuleInstallUISequenceTupleFields.Action, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)ModuleInstallUISequenceTupleFields.Sequence];
            set => this.Set((int)ModuleInstallUISequenceTupleFields.Sequence, value);
        }

        public string BaseAction
        {
            get => (string)this.Fields[(int)ModuleInstallUISequenceTupleFields.BaseAction];
            set => this.Set((int)ModuleInstallUISequenceTupleFields.BaseAction, value);
        }

        public int After
        {
            get => (int)this.Fields[(int)ModuleInstallUISequenceTupleFields.After];
            set => this.Set((int)ModuleInstallUISequenceTupleFields.After, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ModuleInstallUISequenceTupleFields.Condition];
            set => this.Set((int)ModuleInstallUISequenceTupleFields.Condition, value);
        }
    }
}