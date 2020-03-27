// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using WixToolset.Data;

    public enum ExampleSearchTupleFields
    {
        Example,
        SearchFor,
    }

    public class ExampleSearchTuple : IntermediateTuple
    {
        public ExampleSearchTuple() : base(ExampleTupleDefinitions.ExampleSearch, null, null)
        {
        }

        public ExampleSearchTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ExampleTupleDefinitions.ExampleSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ExampleTupleFields index] => this.Fields[(int)index];

        public string SearchFor
        {
            get => this.Fields[(int)ExampleSearchTupleFields.SearchFor]?.AsString();
            set => this.Set((int)ExampleSearchTupleFields.SearchFor, value);
        }
    }
}
