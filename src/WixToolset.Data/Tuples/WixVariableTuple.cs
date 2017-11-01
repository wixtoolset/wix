// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixVariable = new IntermediateTupleDefinition(
            TupleDefinitionType.WixVariable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixVariableTupleFields.WixVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixVariableTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixVariableTupleFields.Overridable), IntermediateFieldType.Bool),
            },
            typeof(WixVariableTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixVariableTupleFields
    {
        WixVariable,
        Value,
        Overridable,
    }

    public class WixVariableTuple : IntermediateTuple
    {
        public WixVariableTuple() : base(TupleDefinitions.WixVariable, null, null)
        {
        }

        public WixVariableTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixVariable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixVariableTupleFields index] => this.Fields[(int)index];

        public string WixVariable
        {
            get => (string)this.Fields[(int)WixVariableTupleFields.WixVariable]?.Value;
            set => this.Set((int)WixVariableTupleFields.WixVariable, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)WixVariableTupleFields.Value]?.Value;
            set => this.Set((int)WixVariableTupleFields.Value, value);
        }

        public bool Overridable
        {
            get => (bool)this.Fields[(int)WixVariableTupleFields.Overridable]?.Value;
            set => this.Set((int)WixVariableTupleFields.Overridable, value);
        }
    }
}