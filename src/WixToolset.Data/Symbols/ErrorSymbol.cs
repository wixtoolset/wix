// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Error = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Error,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ErrorSymbolFields.Message), IntermediateFieldType.String),
            },
            typeof(ErrorSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ErrorSymbolFields
    {
        Message,
    }

    public class ErrorSymbol : IntermediateSymbol
    {
        public ErrorSymbol() : base(SymbolDefinitions.Error, null, null)
        {
        }

        public ErrorSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Error, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ErrorSymbolFields index] => this.Fields[(int)index];

        public string Message
        {
            get => (string)this.Fields[(int)ErrorSymbolFields.Message];
            set => this.Set((int)ErrorSymbolFields.Message, value);
        }
    }
}