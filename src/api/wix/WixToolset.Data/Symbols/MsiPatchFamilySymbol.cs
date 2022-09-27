// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiPatchFamily = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiPatchFamily,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchFamilySymbolFields.PatchFamily), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchFamilySymbolFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchFamilySymbolFields.Sequence), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchFamilySymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(MsiPatchFamilySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiPatchFamilySymbolFields
    {
        PatchFamily,
        ProductCode,
        Sequence,
        Attributes,
    }

    public class MsiPatchFamilySymbol : IntermediateSymbol
    {
        public MsiPatchFamilySymbol() : base(SymbolDefinitions.MsiPatchFamily, null, null)
        {
        }

        public MsiPatchFamilySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiPatchFamily, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchFamilySymbolFields index] => this.Fields[(int)index];

        public string PatchFamily
        {
            get => (string)this.Fields[(int)MsiPatchFamilySymbolFields.PatchFamily];
            set => this.Set((int)MsiPatchFamilySymbolFields.PatchFamily, value);
        }

        public string ProductCode
        {
            get => (string)this.Fields[(int)MsiPatchFamilySymbolFields.ProductCode];
            set => this.Set((int)MsiPatchFamilySymbolFields.ProductCode, value);
        }

        public string Sequence
        {
            get => (string)this.Fields[(int)MsiPatchFamilySymbolFields.Sequence];
            set => this.Set((int)MsiPatchFamilySymbolFields.Sequence, value);
        }

        public int? Attributes
        {
            get => (int?)this.Fields[(int)MsiPatchFamilySymbolFields.Attributes];
            set => this.Set((int)MsiPatchFamilySymbolFields.Attributes, value);
        }
    }
}