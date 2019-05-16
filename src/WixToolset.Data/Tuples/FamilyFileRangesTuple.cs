// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition FamilyFileRanges = new IntermediateTupleDefinition(
            TupleDefinitionType.FamilyFileRanges,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FamilyFileRangesTupleFields.Family), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FamilyFileRangesTupleFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FamilyFileRangesTupleFields.RetainOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FamilyFileRangesTupleFields.RetainLengths), IntermediateFieldType.String),
            },
            typeof(FamilyFileRangesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FamilyFileRangesTupleFields
    {
        Family,
        FTK,
        RetainOffsets,
        RetainLengths,
    }

    public class FamilyFileRangesTuple : IntermediateTuple
    {
        public FamilyFileRangesTuple() : base(TupleDefinitions.FamilyFileRanges, null, null)
        {
        }

        public FamilyFileRangesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.FamilyFileRanges, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FamilyFileRangesTupleFields index] => this.Fields[(int)index];

        public string Family
        {
            get => (string)this.Fields[(int)FamilyFileRangesTupleFields.Family];
            set => this.Set((int)FamilyFileRangesTupleFields.Family, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)FamilyFileRangesTupleFields.FTK];
            set => this.Set((int)FamilyFileRangesTupleFields.FTK, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)FamilyFileRangesTupleFields.RetainOffsets];
            set => this.Set((int)FamilyFileRangesTupleFields.RetainOffsets, value);
        }

        public string RetainLengths
        {
            get => (string)this.Fields[(int)FamilyFileRangesTupleFields.RetainLengths];
            set => this.Set((int)FamilyFileRangesTupleFields.RetainLengths, value);
        }
    }
}