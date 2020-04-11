// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixDeltaPatchFile = new IntermediateTupleDefinition(
            TupleDefinitionType.WixDeltaPatchFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileTupleFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileTupleFields.RetainLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileTupleFields.IgnoreOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileTupleFields.IgnoreLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileTupleFields.RetainOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchFileTupleFields.SymbolPaths), IntermediateFieldType.String),
            },
            typeof(WixDeltaPatchFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixDeltaPatchFileTupleFields
    {
        FileRef,
        RetainLengths,
        IgnoreOffsets,
        IgnoreLengths,
        RetainOffsets,
        SymbolPaths,
    }

    public class WixDeltaPatchFileTuple : IntermediateTuple
    {
        public WixDeltaPatchFileTuple() : base(TupleDefinitions.WixDeltaPatchFile, null, null)
        {
        }

        public WixDeltaPatchFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixDeltaPatchFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDeltaPatchFileTupleFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileTupleFields.FileRef];
            set => this.Set((int)WixDeltaPatchFileTupleFields.FileRef, value);
        }

        public string RetainLengths
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileTupleFields.RetainLengths];
            set => this.Set((int)WixDeltaPatchFileTupleFields.RetainLengths, value);
        }

        public string IgnoreOffsets
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileTupleFields.IgnoreOffsets];
            set => this.Set((int)WixDeltaPatchFileTupleFields.IgnoreOffsets, value);
        }

        public string IgnoreLengths
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileTupleFields.IgnoreLengths];
            set => this.Set((int)WixDeltaPatchFileTupleFields.IgnoreLengths, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileTupleFields.RetainOffsets];
            set => this.Set((int)WixDeltaPatchFileTupleFields.RetainOffsets, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)WixDeltaPatchFileTupleFields.SymbolPaths];
            set => this.Set((int)WixDeltaPatchFileTupleFields.SymbolPaths, value);
        }
    }
}