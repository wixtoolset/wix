// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPatchBaseline = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixPatchBaseline,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchBaselineSymbolFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineSymbolFields.ValidationFlags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineSymbolFields.BaselineFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineSymbolFields.UpdateFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineSymbolFields.TransformFile), IntermediateFieldType.Path),
            },
            typeof(WixPatchBaselineSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixPatchBaselineSymbolFields
    {
        DiskId,
        ValidationFlags,
        BaselineFile,
        UpdateFile,
        TransformFile,
    }

    public class WixPatchBaselineSymbol : IntermediateSymbol
    {
        public WixPatchBaselineSymbol() : base(SymbolDefinitions.WixPatchBaseline, null, null)
        {
        }

        public WixPatchBaselineSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixPatchBaseline, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchBaselineSymbolFields index] => this.Fields[(int)index];

        public int DiskId
        {
            get => (int)this.Fields[(int)WixPatchBaselineSymbolFields.DiskId];
            set => this.Set((int)WixPatchBaselineSymbolFields.DiskId, value);
        }

        public TransformFlags ValidationFlags
        {
            get => (TransformFlags)this.Fields[(int)WixPatchBaselineSymbolFields.ValidationFlags].AsNumber();
            set => this.Set((int)WixPatchBaselineSymbolFields.ValidationFlags, (int)value);
        }

        public IntermediateFieldPathValue BaselineFile
        {
            get => this.Fields[(int)WixPatchBaselineSymbolFields.BaselineFile].AsPath();
            set => this.Set((int)WixPatchBaselineSymbolFields.BaselineFile, value);
        }

        public IntermediateFieldPathValue UpdateFile
        {
            get => this.Fields[(int)WixPatchBaselineSymbolFields.UpdateFile].AsPath();
            set => this.Set((int)WixPatchBaselineSymbolFields.UpdateFile, value);
        }

        public IntermediateFieldPathValue TransformFile
        {
            get => this.Fields[(int)WixPatchBaselineSymbolFields.TransformFile].AsPath();
            set => this.Set((int)WixPatchBaselineSymbolFields.TransformFile, value);
        }
    }
}
