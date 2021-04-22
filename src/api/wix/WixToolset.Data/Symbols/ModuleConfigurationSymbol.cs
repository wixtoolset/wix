// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ModuleConfiguration = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ModuleConfiguration,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.Format), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.ContextData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.DefaultValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.KeyNoOrphan), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.NonNullable), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.HelpLocation), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleConfigurationSymbolFields.HelpKeyword), IntermediateFieldType.String),
            },
            typeof(ModuleConfigurationSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ModuleConfigurationSymbolFields
    {
        Format,
        Type,
        ContextData,
        DefaultValue,
        KeyNoOrphan,
        NonNullable,
        DisplayName,
        Description,
        HelpLocation,
        HelpKeyword,
    }

    public class ModuleConfigurationSymbol : IntermediateSymbol
    {
        public ModuleConfigurationSymbol() : base(SymbolDefinitions.ModuleConfiguration, null, null)
        {
        }

        public ModuleConfigurationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ModuleConfiguration, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleConfigurationSymbolFields index] => this.Fields[(int)index];

        public int Format
        {
            get => (int)this.Fields[(int)ModuleConfigurationSymbolFields.Format];
            set => this.Set((int)ModuleConfigurationSymbolFields.Format, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)ModuleConfigurationSymbolFields.Type];
            set => this.Set((int)ModuleConfigurationSymbolFields.Type, value);
        }

        public string ContextData
        {
            get => (string)this.Fields[(int)ModuleConfigurationSymbolFields.ContextData];
            set => this.Set((int)ModuleConfigurationSymbolFields.ContextData, value);
        }

        public string DefaultValue
        {
            get => (string)this.Fields[(int)ModuleConfigurationSymbolFields.DefaultValue];
            set => this.Set((int)ModuleConfigurationSymbolFields.DefaultValue, value);
        }

        public bool KeyNoOrphan
        {
            get => this.Fields[(int)ModuleConfigurationSymbolFields.KeyNoOrphan].AsBool();
            set => this.Set((int)ModuleConfigurationSymbolFields.KeyNoOrphan, value);
        }

        public bool NonNullable
        {
            get => this.Fields[(int)ModuleConfigurationSymbolFields.NonNullable].AsBool();
            set => this.Set((int)ModuleConfigurationSymbolFields.NonNullable, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)ModuleConfigurationSymbolFields.DisplayName];
            set => this.Set((int)ModuleConfigurationSymbolFields.DisplayName, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ModuleConfigurationSymbolFields.Description];
            set => this.Set((int)ModuleConfigurationSymbolFields.Description, value);
        }

        public string HelpLocation
        {
            get => (string)this.Fields[(int)ModuleConfigurationSymbolFields.HelpLocation];
            set => this.Set((int)ModuleConfigurationSymbolFields.HelpLocation, value);
        }

        public string HelpKeyword
        {
            get => (string)this.Fields[(int)ModuleConfigurationSymbolFields.HelpKeyword];
            set => this.Set((int)ModuleConfigurationSymbolFields.HelpKeyword, value);
        }
    }
}