// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ExternalFiles = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ExternalFiles,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExternalFilesSymbolFields.Family), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesSymbolFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesSymbolFields.FilePath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesSymbolFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesSymbolFields.IgnoreOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesSymbolFields.IgnoreLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesSymbolFields.RetainOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesSymbolFields.Order), IntermediateFieldType.Number),
            },
            typeof(ExternalFilesSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ExternalFilesSymbolFields
    {
        Family,
        FTK,
        FilePath,
        SymbolPaths,
        IgnoreOffsets,
        IgnoreLengths,
        RetainOffsets,
        Order,
    }

    public class ExternalFilesSymbol : IntermediateSymbol
    {
        public ExternalFilesSymbol() : base(SymbolDefinitions.ExternalFiles, null, null)
        {
        }

        public ExternalFilesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ExternalFiles, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ExternalFilesSymbolFields index] => this.Fields[(int)index];

        public string Family
        {
            get => (string)this.Fields[(int)ExternalFilesSymbolFields.Family];
            set => this.Set((int)ExternalFilesSymbolFields.Family, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)ExternalFilesSymbolFields.FTK];
            set => this.Set((int)ExternalFilesSymbolFields.FTK, value);
        }

        public string FilePath
        {
            get => (string)this.Fields[(int)ExternalFilesSymbolFields.FilePath];
            set => this.Set((int)ExternalFilesSymbolFields.FilePath, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)ExternalFilesSymbolFields.SymbolPaths];
            set => this.Set((int)ExternalFilesSymbolFields.SymbolPaths, value);
        }

        public string IgnoreOffsets
        {
            get => (string)this.Fields[(int)ExternalFilesSymbolFields.IgnoreOffsets];
            set => this.Set((int)ExternalFilesSymbolFields.IgnoreOffsets, value);
        }

        public string IgnoreLengths
        {
            get => (string)this.Fields[(int)ExternalFilesSymbolFields.IgnoreLengths];
            set => this.Set((int)ExternalFilesSymbolFields.IgnoreLengths, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)ExternalFilesSymbolFields.RetainOffsets];
            set => this.Set((int)ExternalFilesSymbolFields.RetainOffsets, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)ExternalFilesSymbolFields.Order];
            set => this.Set((int)ExternalFilesSymbolFields.Order, value);
        }
    }
}