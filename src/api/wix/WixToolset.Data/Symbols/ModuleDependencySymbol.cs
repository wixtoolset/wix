// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ModuleDependency = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ModuleDependency,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleDependencySymbolFields.ModuleID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleDependencySymbolFields.ModuleLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleDependencySymbolFields.RequiredID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleDependencySymbolFields.RequiredLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleDependencySymbolFields.RequiredVersion), IntermediateFieldType.String),
            },
            typeof(ModuleDependencySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ModuleDependencySymbolFields
    {
        ModuleID,
        ModuleLanguage,
        RequiredID,
        RequiredLanguage,
        RequiredVersion,
    }

    public class ModuleDependencySymbol : IntermediateSymbol
    {
        public ModuleDependencySymbol() : base(SymbolDefinitions.ModuleDependency, null, null)
        {
        }

        public ModuleDependencySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ModuleDependency, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleDependencySymbolFields index] => this.Fields[(int)index];

        public string ModuleID
        {
            get => (string)this.Fields[(int)ModuleDependencySymbolFields.ModuleID];
            set => this.Set((int)ModuleDependencySymbolFields.ModuleID, value);
        }

        public int ModuleLanguage
        {
            get => (int)this.Fields[(int)ModuleDependencySymbolFields.ModuleLanguage];
            set => this.Set((int)ModuleDependencySymbolFields.ModuleLanguage, value);
        }

        public string RequiredID
        {
            get => (string)this.Fields[(int)ModuleDependencySymbolFields.RequiredID];
            set => this.Set((int)ModuleDependencySymbolFields.RequiredID, value);
        }

        public int RequiredLanguage
        {
            get => (int)this.Fields[(int)ModuleDependencySymbolFields.RequiredLanguage];
            set => this.Set((int)ModuleDependencySymbolFields.RequiredLanguage, value);
        }

        public string RequiredVersion
        {
            get => (string)this.Fields[(int)ModuleDependencySymbolFields.RequiredVersion];
            set => this.Set((int)ModuleDependencySymbolFields.RequiredVersion, value);
        }
    }
}