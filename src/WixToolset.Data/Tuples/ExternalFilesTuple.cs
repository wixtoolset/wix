// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ExternalFiles = new IntermediateTupleDefinition(
            TupleDefinitionType.ExternalFiles,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExternalFilesTupleFields.Family), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesTupleFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesTupleFields.FilePath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesTupleFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesTupleFields.IgnoreOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesTupleFields.IgnoreLengths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesTupleFields.RetainOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExternalFilesTupleFields.Order), IntermediateFieldType.Number),
            },
            typeof(ExternalFilesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ExternalFilesTupleFields
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

    public class ExternalFilesTuple : IntermediateTuple
    {
        public ExternalFilesTuple() : base(TupleDefinitions.ExternalFiles, null, null)
        {
        }

        public ExternalFilesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ExternalFiles, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ExternalFilesTupleFields index] => this.Fields[(int)index];

        public string Family
        {
            get => (string)this.Fields[(int)ExternalFilesTupleFields.Family];
            set => this.Set((int)ExternalFilesTupleFields.Family, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)ExternalFilesTupleFields.FTK];
            set => this.Set((int)ExternalFilesTupleFields.FTK, value);
        }

        public string FilePath
        {
            get => (string)this.Fields[(int)ExternalFilesTupleFields.FilePath];
            set => this.Set((int)ExternalFilesTupleFields.FilePath, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)ExternalFilesTupleFields.SymbolPaths];
            set => this.Set((int)ExternalFilesTupleFields.SymbolPaths, value);
        }

        public string IgnoreOffsets
        {
            get => (string)this.Fields[(int)ExternalFilesTupleFields.IgnoreOffsets];
            set => this.Set((int)ExternalFilesTupleFields.IgnoreOffsets, value);
        }

        public string IgnoreLengths
        {
            get => (string)this.Fields[(int)ExternalFilesTupleFields.IgnoreLengths];
            set => this.Set((int)ExternalFilesTupleFields.IgnoreLengths, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)ExternalFilesTupleFields.RetainOffsets];
            set => this.Set((int)ExternalFilesTupleFields.RetainOffsets, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)ExternalFilesTupleFields.Order];
            set => this.Set((int)ExternalFilesTupleFields.Order, value);
        }
    }
}