// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ModuleIgnoreTable = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ModuleIgnoreTable,
            new IntermediateFieldDefinition[]
            {
            },
            typeof(ModuleIgnoreTableSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ModuleIgnoreTableSymbolFields
    {
    }

    public class ModuleIgnoreTableSymbol : IntermediateSymbol
    {
        public ModuleIgnoreTableSymbol() : base(SymbolDefinitions.ModuleIgnoreTable, null, null)
        {
        }

        public ModuleIgnoreTableSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ModuleIgnoreTable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleIgnoreTableSymbolFields index] => this.Fields[(int)index];
    }
}