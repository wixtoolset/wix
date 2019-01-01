// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Tuples;

    public static partial class BalTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBalCondition = new IntermediateTupleDefinition(
            BalTupleDefinitionType.WixBalCondition.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalConditionTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBalConditionTupleFields.Message), IntermediateFieldType.String),
            },
            typeof(WixBalConditionTuple));
    }
}

namespace WixToolset.Bal.Tuples
{
    using WixToolset.Data;

    public enum WixBalConditionTupleFields
    {
        Condition,
        Message,
    }

    public class WixBalConditionTuple : IntermediateTuple
    {
        public WixBalConditionTuple() : base(BalTupleDefinitions.WixBalCondition, null, null)
        {
        }

        public WixBalConditionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalTupleDefinitions.WixBalCondition, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalConditionTupleFields index] => this.Fields[(int)index];

        public string Condition
        {
            get => this.Fields[(int)WixBalConditionTupleFields.Condition].AsString();
            set => this.Set((int)WixBalConditionTupleFields.Condition, value);
        }

        public string Message
        {
            get => this.Fields[(int)WixBalConditionTupleFields.Message].AsString();
            set => this.Set((int)WixBalConditionTupleFields.Message, value);
        }
    }
}