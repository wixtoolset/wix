// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPatchRef = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixPatchRef,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchRefSymbolFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPatchRefSymbolFields.PrimaryKeys), IntermediateFieldType.String),
            },
            typeof(WixPatchRefSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixPatchRefSymbolFields
    {
        Table,
        PrimaryKeys,
    }

    public class WixPatchRefSymbol : IntermediateSymbol
    {
        public WixPatchRefSymbol() : base(SymbolDefinitions.WixPatchRef, null, null)
        {
        }

        public WixPatchRefSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixPatchRef, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchRefSymbolFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)WixPatchRefSymbolFields.Table];
            set => this.Set((int)WixPatchRefSymbolFields.Table, value);
        }

        public string PrimaryKeys
        {
            get => (string)this.Fields[(int)WixPatchRefSymbolFields.PrimaryKeys];
            set => this.Set((int)WixPatchRefSymbolFields.PrimaryKeys, value);
        }
    }
}