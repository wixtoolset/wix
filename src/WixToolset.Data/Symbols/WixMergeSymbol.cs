// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixMerge = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixMerge,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixMergeSymbolFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMergeSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMergeSymbolFields.SourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixMergeSymbolFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMergeSymbolFields.FileAttributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMergeSymbolFields.ConfigurationData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMergeSymbolFields.FeatureRef), IntermediateFieldType.String),
            },
            typeof(WixMergeSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixMergeSymbolFields
    {
        Language,
        DirectoryRef,
        SourceFile,
        DiskId,
        FileAttributes,
        ConfigurationData,
        FeatureRef,
    }

    public class WixMergeSymbol : IntermediateSymbol
    {
        public WixMergeSymbol() : base(SymbolDefinitions.WixMerge, null, null)
        {
        }

        public WixMergeSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixMerge, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixMergeSymbolFields index] => this.Fields[(int)index];

        public int Language
        {
            get => (int)this.Fields[(int)WixMergeSymbolFields.Language];
            set => this.Set((int)WixMergeSymbolFields.Language, value);
        }

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)WixMergeSymbolFields.DirectoryRef];
            set => this.Set((int)WixMergeSymbolFields.DirectoryRef, value);
        }

        public string SourceFile
        {
            get => (string)this.Fields[(int)WixMergeSymbolFields.SourceFile];
            set => this.Set((int)WixMergeSymbolFields.SourceFile, value);
        }

        public int DiskId
        {
            get => (int)this.Fields[(int)WixMergeSymbolFields.DiskId];
            set => this.Set((int)WixMergeSymbolFields.DiskId, value);
        }

        public FileSymbolAttributes FileAttributes
        {
            get => (FileSymbolAttributes)this.Fields[(int)WixMergeSymbolFields.FileAttributes].AsNumber();
            set => this.Set((int)WixMergeSymbolFields.FileAttributes, (int)value);
        }

        public string ConfigurationData
        {
            get => (string)this.Fields[(int)WixMergeSymbolFields.ConfigurationData];
            set => this.Set((int)WixMergeSymbolFields.ConfigurationData, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)WixMergeSymbolFields.FeatureRef];
            set => this.Set((int)WixMergeSymbolFields.FeatureRef, value);
        }
    }
}
