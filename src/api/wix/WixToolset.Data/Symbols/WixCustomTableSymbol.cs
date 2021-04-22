// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixCustomTable = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixCustomTable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixCustomTableSymbolFields.ColumnNames), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCustomTableSymbolFields.Unreal), IntermediateFieldType.Bool),
            },
            typeof(WixCustomTableSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixCustomTableSymbolFields
    {
        ColumnNames,
        Unreal,
    }

    public class WixCustomTableSymbol : IntermediateSymbol
    {
        public const char ColumnNamesSeparator = '\x85';

        public WixCustomTableSymbol() : base(SymbolDefinitions.WixCustomTable, null, null)
        {
        }

        public WixCustomTableSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixCustomTable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixCustomTableSymbolFields index] => this.Fields[(int)index];

        public string ColumnNames
        {
            get => (string)this.Fields[(int)WixCustomTableSymbolFields.ColumnNames];
            set => this.Set((int)WixCustomTableSymbolFields.ColumnNames, value);
        }

        public bool Unreal
        {
            get => (bool)this.Fields[(int)WixCustomTableSymbolFields.Unreal];
            set => this.Set((int)WixCustomTableSymbolFields.Unreal, value);
        }

        public string[] ColumnNamesSeparated => this.ColumnNames.Split(ColumnNamesSeparator);
    }
}
