// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixEnsureTable = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixEnsureTable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixEnsureTableSymbolFields.Table), IntermediateFieldType.String),
            },
            typeof(WixEnsureTableSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixEnsureTableSymbolFields
    {
        Table,
    }

    public class WixEnsureTableSymbol : IntermediateSymbol
    {
        public WixEnsureTableSymbol() : base(SymbolDefinitions.WixEnsureTable, null, null)
        {
        }

        public WixEnsureTableSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixEnsureTable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixEnsureTableSymbolFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)WixEnsureTableSymbolFields.Table];
            set => this.Set((int)WixEnsureTableSymbolFields.Table, value);
        }
    }
}