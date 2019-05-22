// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ControlCondition = new IntermediateTupleDefinition(
            TupleDefinitionType.ControlCondition,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ControlConditionTupleFields.DialogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlConditionTupleFields.ControlRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlConditionTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlConditionTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(ControlConditionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ControlConditionTupleFields
    {
        DialogRef,
        ControlRef,
        Action,
        Condition,
    }

    public class ControlConditionTuple : IntermediateTuple
    {
        public ControlConditionTuple() : base(TupleDefinitions.ControlCondition, null, null)
        {
        }

        public ControlConditionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ControlCondition, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ControlConditionTupleFields index] => this.Fields[(int)index];

        public string DialogRef
        {
            get => (string)this.Fields[(int)ControlConditionTupleFields.DialogRef];
            set => this.Set((int)ControlConditionTupleFields.DialogRef, value);
        }

        public string ControlRef
        {
            get => (string)this.Fields[(int)ControlConditionTupleFields.ControlRef];
            set => this.Set((int)ControlConditionTupleFields.ControlRef, value);
        }

        public string Action
        {
            get => (string)this.Fields[(int)ControlConditionTupleFields.Action];
            set => this.Set((int)ControlConditionTupleFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ControlConditionTupleFields.Condition];
            set => this.Set((int)ControlConditionTupleFields.Condition, value);
        }
    }
}