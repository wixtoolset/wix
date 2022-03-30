// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using WixToolset.Data;

    public enum ExampleSymbolFields
    {
        ComponentRef,
        Value,
    }

    public class ExampleSymbol : IntermediateSymbol
    {
        public ExampleSymbol() : base(ExampleSymbolDefinitions.Example, null, null)
        {
        }

        public ExampleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ExampleSymbolDefinitions.Example, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ExampleSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)ExampleSymbolFields.ComponentRef]?.AsString();
            set => this.Set((int)ExampleSymbolFields.ComponentRef, value);
        }

        public string Value
        {
            get => this.Fields[(int)ExampleSymbolFields.Value]?.AsString();
            set => this.Set((int)ExampleSymbolFields.Value, value);
        }
    }
}
