// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using WixToolset.Data;

    public enum ExampleSearchSymbolFields
    {
        SearchFor,
    }

    public class ExampleSearchSymbol : IntermediateSymbol
    {
        public ExampleSearchSymbol() : base(ExampleSymbolDefinitions.ExampleSearch, null, null)
        {
        }

        public ExampleSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ExampleSymbolDefinitions.ExampleSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ExampleSymbolFields index] => this.Fields[(int)index];

        public string SearchFor
        {
            get => this.Fields[(int)ExampleSearchSymbolFields.SearchFor]?.AsString();
            set => this.Set((int)ExampleSearchSymbolFields.SearchFor, value);
        }
    }
}
