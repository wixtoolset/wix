// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition LaunchCondition = new IntermediateTupleDefinition(
            TupleDefinitionType.LaunchCondition,
            new[]
            {
                new IntermediateFieldDefinition(nameof(LaunchConditionTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LaunchConditionTupleFields.Description), IntermediateFieldType.String),
            },
            typeof(LaunchConditionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum LaunchConditionTupleFields
    {
        Condition,
        Description,
    }

    public class LaunchConditionTuple : IntermediateTuple
    {
        public LaunchConditionTuple() : base(TupleDefinitions.LaunchCondition, null, null)
        {
        }

        public LaunchConditionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.LaunchCondition, sourceLineNumber, id)
        {
        }

        public IntermediateField this[LaunchConditionTupleFields index] => this.Fields[(int)index];

        public string Condition
        {
            get => (string)this.Fields[(int)LaunchConditionTupleFields.Condition];
            set => this.Set((int)LaunchConditionTupleFields.Condition, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)LaunchConditionTupleFields.Description];
            set => this.Set((int)LaunchConditionTupleFields.Description, value);
        }
    }
}