// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixSetVariable = new IntermediateTupleDefinition(
            TupleDefinitionType.WixSetVariable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSetVariableTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSetVariableTupleFields.Type), IntermediateFieldType.String),
            },
            typeof(WixSetVariableTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixSetVariableTupleFields
    {
        Value,
        Type,
    }

    public class WixSetVariableTuple : IntermediateTuple
    {
        public WixSetVariableTuple() : base(TupleDefinitions.WixSetVariable, null, null)
        {
        }

        public WixSetVariableTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixSetVariable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSetVariableTupleFields index] => this.Fields[(int)index];

        public string Value
        {
            get => (string)this.Fields[(int)WixSetVariableTupleFields.Value];
            set => this.Set((int)WixSetVariableTupleFields.Value, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)WixSetVariableTupleFields.Type];
            set => this.Set((int)WixSetVariableTupleFields.Type, value);
        }
    }
}
