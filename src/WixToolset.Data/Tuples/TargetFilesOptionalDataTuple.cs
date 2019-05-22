// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition TargetFilesOptionalData = new IntermediateTupleDefinition(
            TupleDefinitionType.TargetFilesOptionalData,
            new[]
            {
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataTupleFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataTupleFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataTupleFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataTupleFields.IgnoreOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataTupleFields.IgnoreLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFilesOptionalDataTupleFields.RetainOffsets), IntermediateFieldType.String),
            },
            typeof(TargetFilesOptionalDataTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum TargetFilesOptionalDataTupleFields
    {
        Target,
        FTK,
        SymbolPaths,
        IgnoreOffsets,
        IgnoreLengths,
        RetainOffsets,
    }

    public class TargetFilesOptionalDataTuple : IntermediateTuple
    {
        public TargetFilesOptionalDataTuple() : base(TupleDefinitions.TargetFilesOptionalData, null, null)
        {
        }

        public TargetFilesOptionalDataTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.TargetFilesOptionalData, sourceLineNumber, id)
        {
        }

        public IntermediateField this[TargetFilesOptionalDataTupleFields index] => this.Fields[(int)index];

        public string Target
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataTupleFields.Target];
            set => this.Set((int)TargetFilesOptionalDataTupleFields.Target, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataTupleFields.FTK];
            set => this.Set((int)TargetFilesOptionalDataTupleFields.FTK, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataTupleFields.SymbolPaths];
            set => this.Set((int)TargetFilesOptionalDataTupleFields.SymbolPaths, value);
        }

        public string IgnoreOffsets
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataTupleFields.IgnoreOffsets];
            set => this.Set((int)TargetFilesOptionalDataTupleFields.IgnoreOffsets, value);
        }

        public string IgnoreLengths
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataTupleFields.IgnoreLengths];
            set => this.Set((int)TargetFilesOptionalDataTupleFields.IgnoreLengths, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)TargetFilesOptionalDataTupleFields.RetainOffsets];
            set => this.Set((int)TargetFilesOptionalDataTupleFields.RetainOffsets, value);
        }
    }
}