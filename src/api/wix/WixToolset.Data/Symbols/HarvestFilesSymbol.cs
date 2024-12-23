// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition HarvestFiles = new IntermediateSymbolDefinition(
            SymbolDefinitionType.HarvestFiles,
            new[]
            {
                new IntermediateFieldDefinition(nameof(HarvestFilesSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HarvestFilesSymbolFields.Inclusions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HarvestFilesSymbolFields.Exclusions), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HarvestFilesSymbolFields.ComplexReferenceParentType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HarvestFilesSymbolFields.ParentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HarvestFilesSymbolFields.SourcePath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HarvestFilesSymbolFields.ModuleLanguage), IntermediateFieldType.String),
            },
            typeof(HarvestFilesSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum HarvestFilesSymbolFields
    {
        DirectoryRef,
        Inclusions,
        Exclusions,
        ComplexReferenceParentType,
        ParentId,
        SourcePath,
        ModuleLanguage,
    }

    public class HarvestFilesSymbol : IntermediateSymbol
    {
        public HarvestFilesSymbol() : base(SymbolDefinitions.HarvestFiles, null, null)
        {
        }

        public HarvestFilesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.HarvestFiles, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HarvestFilesSymbolFields index] => this.Fields[(int)index];

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)HarvestFilesSymbolFields.DirectoryRef];
            set => this.Set((int)HarvestFilesSymbolFields.DirectoryRef, value);
        }

        public string Inclusions
        {
            get => (string)this.Fields[(int)HarvestFilesSymbolFields.Inclusions];
            set => this.Set((int)HarvestFilesSymbolFields.Inclusions, value);
        }

        public string Exclusions
        {
            get => (string)this.Fields[(int)HarvestFilesSymbolFields.Exclusions];
            set => this.Set((int)HarvestFilesSymbolFields.Exclusions, value);
        }

        public string ComplexReferenceParentType
        {
            get => (string)this.Fields[(int)HarvestFilesSymbolFields.ComplexReferenceParentType];
            set => this.Set((int)HarvestFilesSymbolFields.ComplexReferenceParentType, value);
        }

        public string ParentId
        {
            get => (string)this.Fields[(int)HarvestFilesSymbolFields.ParentId];
            set => this.Set((int)HarvestFilesSymbolFields.ParentId, value);
        }

        public string SourcePath
        {
            get => (string)this.Fields[(int)HarvestFilesSymbolFields.SourcePath];
            set => this.Set((int)HarvestFilesSymbolFields.SourcePath, value);
        }

        public string ModuleLanguage
        {
            get => (string)this.Fields[(int)HarvestFilesSymbolFields.ModuleLanguage];
            set => this.Set((int)HarvestFilesSymbolFields.ModuleLanguage, value);
        }
    }
}
