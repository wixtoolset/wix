// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ModuleSubstitution = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ModuleSubstitution,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleSubstitutionSymbolFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleSubstitutionSymbolFields.Row), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleSubstitutionSymbolFields.Column), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleSubstitutionSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ModuleSubstitutionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ModuleSubstitutionSymbolFields
    {
        Table,
        Row,
        Column,
        Value,
    }

    public class ModuleSubstitutionSymbol : IntermediateSymbol
    {
        public ModuleSubstitutionSymbol() : base(SymbolDefinitions.ModuleSubstitution, null, null)
        {
        }

        public ModuleSubstitutionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ModuleSubstitution, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleSubstitutionSymbolFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)ModuleSubstitutionSymbolFields.Table];
            set => this.Set((int)ModuleSubstitutionSymbolFields.Table, value);
        }

        public string Row
        {
            get => (string)this.Fields[(int)ModuleSubstitutionSymbolFields.Row];
            set => this.Set((int)ModuleSubstitutionSymbolFields.Row, value);
        }

        public string Column
        {
            get => (string)this.Fields[(int)ModuleSubstitutionSymbolFields.Column];
            set => this.Set((int)ModuleSubstitutionSymbolFields.Column, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ModuleSubstitutionSymbolFields.Value];
            set => this.Set((int)ModuleSubstitutionSymbolFields.Value, value);
        }
    }
}