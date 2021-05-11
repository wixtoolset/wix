// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ModuleComponents = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ModuleComponents,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleComponentsSymbolFields.Component), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleComponentsSymbolFields.ModuleID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleComponentsSymbolFields.Language), IntermediateFieldType.Number),
            },
            typeof(ModuleComponentsSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ModuleComponentsSymbolFields
    {
        Component,
        ModuleID,
        Language,
    }

    public class ModuleComponentsSymbol : IntermediateSymbol
    {
        public ModuleComponentsSymbol() : base(SymbolDefinitions.ModuleComponents, null, null)
        {
        }

        public ModuleComponentsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ModuleComponents, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleComponentsSymbolFields index] => this.Fields[(int)index];

        public string Component
        {
            get => (string)this.Fields[(int)ModuleComponentsSymbolFields.Component];
            set => this.Set((int)ModuleComponentsSymbolFields.Component, value);
        }

        public string ModuleID
        {
            get => (string)this.Fields[(int)ModuleComponentsSymbolFields.ModuleID];
            set => this.Set((int)ModuleComponentsSymbolFields.ModuleID, value);
        }

        public int Language
        {
            get => (int)this.Fields[(int)ModuleComponentsSymbolFields.Language];
            set => this.Set((int)ModuleComponentsSymbolFields.Language, value);
        }
    }
}