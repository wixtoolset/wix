// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ModuleExclusion = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ModuleExclusion,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleExclusionSymbolFields.ModuleID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleExclusionSymbolFields.ModuleLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleExclusionSymbolFields.ExcludedID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleExclusionSymbolFields.ExcludedLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleExclusionSymbolFields.ExcludedMinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleExclusionSymbolFields.ExcludedMaxVersion), IntermediateFieldType.String),
            },
            typeof(ModuleExclusionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ModuleExclusionSymbolFields
    {
        ModuleID,
        ModuleLanguage,
        ExcludedID,
        ExcludedLanguage,
        ExcludedMinVersion,
        ExcludedMaxVersion,
    }

    public class ModuleExclusionSymbol : IntermediateSymbol
    {
        public ModuleExclusionSymbol() : base(SymbolDefinitions.ModuleExclusion, null, null)
        {
        }

        public ModuleExclusionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ModuleExclusion, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleExclusionSymbolFields index] => this.Fields[(int)index];

        public string ModuleID
        {
            get => (string)this.Fields[(int)ModuleExclusionSymbolFields.ModuleID];
            set => this.Set((int)ModuleExclusionSymbolFields.ModuleID, value);
        }

        public int ModuleLanguage
        {
            get => (int)this.Fields[(int)ModuleExclusionSymbolFields.ModuleLanguage];
            set => this.Set((int)ModuleExclusionSymbolFields.ModuleLanguage, value);
        }

        public string ExcludedID
        {
            get => (string)this.Fields[(int)ModuleExclusionSymbolFields.ExcludedID];
            set => this.Set((int)ModuleExclusionSymbolFields.ExcludedID, value);
        }

        public int ExcludedLanguage
        {
            get => (int)this.Fields[(int)ModuleExclusionSymbolFields.ExcludedLanguage];
            set => this.Set((int)ModuleExclusionSymbolFields.ExcludedLanguage, value);
        }

        public string ExcludedMinVersion
        {
            get => (string)this.Fields[(int)ModuleExclusionSymbolFields.ExcludedMinVersion];
            set => this.Set((int)ModuleExclusionSymbolFields.ExcludedMinVersion, value);
        }

        public string ExcludedMaxVersion
        {
            get => (string)this.Fields[(int)ModuleExclusionSymbolFields.ExcludedMaxVersion];
            set => this.Set((int)ModuleExclusionSymbolFields.ExcludedMaxVersion, value);
        }
    }
}