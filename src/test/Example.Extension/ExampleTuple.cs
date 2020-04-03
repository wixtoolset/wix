// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using WixToolset.Data;

    public enum ExampleTupleFields
    {
        Value,
    }

    public class ExampleTuple : IntermediateTuple
    {
        public ExampleTuple() : base(ExampleTupleDefinitions.Example, null, null)
        {
        }

        public ExampleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ExampleTupleDefinitions.Example, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ExampleTupleFields index] => this.Fields[(int)index];

        public string Value
        {
            get => this.Fields[(int)ExampleTupleFields.Value]?.AsString();
            set => this.Set((int)ExampleTupleFields.Value, value);
        }
    }
}
