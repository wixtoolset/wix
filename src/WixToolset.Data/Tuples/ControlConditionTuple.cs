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
                new IntermediateFieldDefinition(nameof(ControlConditionTupleFields.Dialog_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlConditionTupleFields.Control_), IntermediateFieldType.String),
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
        Dialog_,
        Control_,
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

        public string Dialog_
        {
            get => (string)this.Fields[(int)ControlConditionTupleFields.Dialog_]?.Value;
            set => this.Set((int)ControlConditionTupleFields.Dialog_, value);
        }

        public string Control_
        {
            get => (string)this.Fields[(int)ControlConditionTupleFields.Control_]?.Value;
            set => this.Set((int)ControlConditionTupleFields.Control_, value);
        }

        public string Action
        {
            get => (string)this.Fields[(int)ControlConditionTupleFields.Action]?.Value;
            set => this.Set((int)ControlConditionTupleFields.Action, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ControlConditionTupleFields.Condition]?.Value;
            set => this.Set((int)ControlConditionTupleFields.Condition, value);
        }
    }
}