// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition TargetFiles_OptionalData = new IntermediateTupleDefinition(
            TupleDefinitionType.TargetFiles_OptionalData,
            new[]
            {
                new IntermediateFieldDefinition(nameof(TargetFiles_OptionalDataTupleFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFiles_OptionalDataTupleFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFiles_OptionalDataTupleFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFiles_OptionalDataTupleFields.IgnoreOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFiles_OptionalDataTupleFields.IgnoreLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetFiles_OptionalDataTupleFields.RetainOffsets), IntermediateFieldType.String),
            },
            typeof(TargetFiles_OptionalDataTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum TargetFiles_OptionalDataTupleFields
    {
        Target,
        FTK,
        SymbolPaths,
        IgnoreOffsets,
        IgnoreLengths,
        RetainOffsets,
    }

    public class TargetFiles_OptionalDataTuple : IntermediateTuple
    {
        public TargetFiles_OptionalDataTuple() : base(TupleDefinitions.TargetFiles_OptionalData, null, null)
        {
        }

        public TargetFiles_OptionalDataTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.TargetFiles_OptionalData, sourceLineNumber, id)
        {
        }

        public IntermediateField this[TargetFiles_OptionalDataTupleFields index] => this.Fields[(int)index];

        public string Target
        {
            get => (string)this.Fields[(int)TargetFiles_OptionalDataTupleFields.Target];
            set => this.Set((int)TargetFiles_OptionalDataTupleFields.Target, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)TargetFiles_OptionalDataTupleFields.FTK];
            set => this.Set((int)TargetFiles_OptionalDataTupleFields.FTK, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)TargetFiles_OptionalDataTupleFields.SymbolPaths];
            set => this.Set((int)TargetFiles_OptionalDataTupleFields.SymbolPaths, value);
        }

        public string IgnoreOffsets
        {
            get => (string)this.Fields[(int)TargetFiles_OptionalDataTupleFields.IgnoreOffsets];
            set => this.Set((int)TargetFiles_OptionalDataTupleFields.IgnoreOffsets, value);
        }

        public string IgnoreLengths
        {
            get => (string)this.Fields[(int)TargetFiles_OptionalDataTupleFields.IgnoreLengths];
            set => this.Set((int)TargetFiles_OptionalDataTupleFields.IgnoreLengths, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)TargetFiles_OptionalDataTupleFields.RetainOffsets];
            set => this.Set((int)TargetFiles_OptionalDataTupleFields.RetainOffsets, value);
        }
    }
}