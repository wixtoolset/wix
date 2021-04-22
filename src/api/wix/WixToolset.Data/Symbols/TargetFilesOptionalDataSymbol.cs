// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition TargetFilesOptionalData = new IntermediateSymbolDefinition(
            SymbolDefinitionType.TargetFilesOptionalData,
            new[]
            {
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataSymbolFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataSymbolFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataSymbolFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataSymbolFields.IgnoreOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataSymbolFields.IgnoreLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataSymbolFields.RetainOffsets), IntermediateFieldType.String),
            },
            typeof(TargetFilesOptionalDataSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum TargetFilesOptionalDataSymbolFields
    {
        Target,
        FTK,
        SymbolPaths,
        IgnoreOffsets,
        IgnoreLengths,
        RetainOffsets,
    }

    public class TargetFilesOptionalDataSymbol : IntermediateSymbol
    {
        public TargetFilesOptionalDataSymbol() : base(SymbolDefinitions.TargetFilesOptionalData, null, null)
        {
        }

        public TargetFilesOptionalDataSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.TargetFilesOptionalData, sourceLineNumber, id)
        {
        }

        public IntermediateField this[TargetFilesOptionalDataSymbolFields index] => this.Fields[(int)index];

        public string Target
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataSymbolFields.Target];
            set => this.Set((int)TargetFilesOptionalDataSymbolFields.Target, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataSymbolFields.FTK];
            set => this.Set((int)TargetFilesOptionalDataSymbolFields.FTK, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataSymbolFields.SymbolPaths];
            set => this.Set((int)TargetFilesOptionalDataSymbolFields.SymbolPaths, value);
        }

        public string IgnoreOffsets
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataSymbolFields.IgnoreOffsets];
            set => this.Set((int)TargetFilesOptionalDataSymbolFields.IgnoreOffsets, value);
        }

        public string IgnoreLengths
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataSymbolFields.IgnoreLengths];
            set => this.Set((int)TargetFilesOptionalDataSymbolFields.IgnoreLengths, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataSymbolFields.RetainOffsets];
            set => this.Set((int)TargetFilesOptionalDataSymbolFields.RetainOffsets, value);
        }
    }
}