// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition FamilyFileRanges = new IntermediateSymbolDefinition(
            SymbolDefinitionType.FamilyFileRanges,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FamilyFileRangesSymbolFields.Family), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FamilyFileRangesSymbolFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FamilyFileRangesSymbolFields.RetainOffsets), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FamilyFileRangesSymbolFields.RetainLengths), IntermediateFieldType.String),
            },
            typeof(FamilyFileRangesSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum FamilyFileRangesSymbolFields
    {
        Family,
        FTK,
        RetainOffsets,
        RetainLengths,
    }

    public class FamilyFileRangesSymbol : IntermediateSymbol
    {
        public FamilyFileRangesSymbol() : base(SymbolDefinitions.FamilyFileRanges, null, null)
        {
        }

        public FamilyFileRangesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.FamilyFileRanges, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FamilyFileRangesSymbolFields index] => this.Fields[(int)index];

        public string Family
        {
            get => (string)this.Fields[(int)FamilyFileRangesSymbolFields.Family];
            set => this.Set((int)FamilyFileRangesSymbolFields.Family, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)FamilyFileRangesSymbolFields.FTK];
            set => this.Set((int)FamilyFileRangesSymbolFields.FTK, value);
        }

        public string RetainOffsets
        {
            get => (string)this.Fields[(int)FamilyFileRangesSymbolFields.RetainOffsets];
            set => this.Set((int)FamilyFileRangesSymbolFields.RetainOffsets, value);
        }

        public string RetainLengths
        {
            get => (string)this.Fields[(int)FamilyFileRangesSymbolFields.RetainLengths];
            set => this.Set((int)FamilyFileRangesSymbolFields.RetainLengths, value);
        }
    }
}