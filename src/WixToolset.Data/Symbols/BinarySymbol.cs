// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Binary = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Binary,
            new[]
            {
                new IntermediateFieldDefinition(nameof(BinarySymbolFields.Data), IntermediateFieldType.Path),
            },
            typeof(BinarySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum BinarySymbolFields
    {
        Data,
    }

    public class BinarySymbol : IntermediateSymbol
    {
        public BinarySymbol() : base(SymbolDefinitions.Binary, null, null)
        {
        }

        public BinarySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Binary, sourceLineNumber, id)
        {
        }

        public IntermediateField this[BinarySymbolFields index] => this.Fields[(int)index];

        public IntermediateFieldPathValue Data
        {
            get => this.Fields[(int)BinarySymbolFields.Data].AsPath();
            set => this.Set((int)BinarySymbolFields.Data, value);
        }
    }
}