// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixDeltaPatchFile = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixDeltaPatchFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileSymbolFields.RetainLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileSymbolFields.IgnoreOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileSymbolFields.IgnoreLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileSymbolFields.RetainOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileSymbolFields.SymbolPaths), IntermediateFieldType.String),
            },
            typeof(WixDeltaPatchFileSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixDeltaPatchFileSymbolFields
    {
        FileRef,
        RetainLengths,
        IgnoreOffsets,
        IgnoreLengths,
        RetainOffsets,
        SymbolPaths,
    }

    public class WixDeltaPatchFileSymbol : IntermediateSymbol
    {
        public WixDeltaPatchFileSymbol() : base(SymbolDefinitions.WixDeltaPatchFile, null, null)
        {
        }

        public WixDeltaPatchFileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixDeltaPatchFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDeltaPatchFileSymbolFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileSymbolFields.FileRef];
            set => this.Set((int)WixDeltaPatchFileSymbolFields.FileRef, value);
        }

        public string RetainLengths
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileSymbolFields.RetainLengths];
            set => this.Set((int)WixDeltaPatchFileSymbolFields.RetainLengths, value);
        }

        public string IgnoreOffsets
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileSymbolFields.IgnoreOffsets];
            set => this.Set((int)WixDeltaPatchFileSymbolFields.IgnoreOffsets, value);
        }

        public string IgnoreLengths
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileSymbolFields.IgnoreLengths];
            set => this.Set((int)WixDeltaPatchFileSymbolFields.IgnoreLengths, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileSymbolFields.RetainOffsets];
            set => this.Set((int)WixDeltaPatchFileSymbolFields.RetainOffsets, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileSymbolFields.SymbolPaths];
            set => this.Set((int)WixDeltaPatchFileSymbolFields.SymbolPaths, value);
        }
    }
}