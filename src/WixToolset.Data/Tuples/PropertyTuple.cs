// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Property = new IntermediateTupleDefinition(
            TupleDefinitionType.Property,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PropertyTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(PropertyTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum PropertyTupleFields
    {
        Value,
    }

    public class PropertyTuple : IntermediateTuple
    {
        public PropertyTuple() : base(TupleDefinitions.Property, null, null)
        {
        }

        public PropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Property, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PropertyTupleFields index] => this.Fields[(int)index];

        public string Value
        {
            get => (string)this.Fields[(int)PropertyTupleFields.Value];
            set => this.Set((int)PropertyTupleFields.Value, value);
        }
    }
}