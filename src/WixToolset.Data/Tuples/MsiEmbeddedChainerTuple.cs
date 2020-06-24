// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiEmbeddedChainer = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiEmbeddedChainer,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiEmbeddedChainerSymbolFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedChainerSymbolFields.CommandLine), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedChainerSymbolFields.Source), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedChainerSymbolFields.Type), IntermediateFieldType.Number),
            },
            typeof(MsiEmbeddedChainerSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiEmbeddedChainerSymbolFields
    {
        Condition,
        CommandLine,
        Source,
        Type,
    }

    public class MsiEmbeddedChainerSymbol : IntermediateSymbol
    {
        public MsiEmbeddedChainerSymbol() : base(SymbolDefinitions.MsiEmbeddedChainer, null, null)
        {
        }

        public MsiEmbeddedChainerSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiEmbeddedChainer, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiEmbeddedChainerSymbolFields index] => this.Fields[(int)index];

        public string Condition
        {
            get => (string)this.Fields[(int)MsiEmbeddedChainerSymbolFields.Condition];
            set => this.Set((int)MsiEmbeddedChainerSymbolFields.Condition, value);
        }

        public string CommandLine
        {
            get => (string)this.Fields[(int)MsiEmbeddedChainerSymbolFields.CommandLine];
            set => this.Set((int)MsiEmbeddedChainerSymbolFields.CommandLine, value);
        }

        public string Source
        {
            get => (string)this.Fields[(int)MsiEmbeddedChainerSymbolFields.Source];
            set => this.Set((int)MsiEmbeddedChainerSymbolFields.Source, value);
        }

        public int Type
        {
            get => (int)this.Fields[(int)MsiEmbeddedChainerSymbolFields.Type];
            set => this.Set((int)MsiEmbeddedChainerSymbolFields.Type, value);
        }
    }
}