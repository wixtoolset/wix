// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleCustomDataCell = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleCustomDataCell,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataCellSymbolFields.CustomDataRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataCellSymbolFields.AttributeRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataCellSymbolFields.ElementId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataCellSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(WixBundleCustomDataCellSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleCustomDataCellSymbolFields
    {
        CustomDataRef,
        AttributeRef,
        ElementId,
        Value,
    }

    public class WixBundleCustomDataCellSymbol : IntermediateSymbol
    {
        public WixBundleCustomDataCellSymbol() : base(SymbolDefinitions.WixBundleCustomDataCell, null, null)
        {
        }

        public WixBundleCustomDataCellSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleCustomDataCell, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleCustomDataCellSymbolFields index] => this.Fields[(int)index];

        public string CustomDataRef
        {
            get => (string)this.Fields[(int)WixBundleCustomDataCellSymbolFields.CustomDataRef];
            set => this.Set((int)WixBundleCustomDataCellSymbolFields.CustomDataRef, value);
        }

        public string AttributeRef
        {
            get => (string)this.Fields[(int)WixBundleCustomDataCellSymbolFields.AttributeRef];
            set => this.Set((int)WixBundleCustomDataCellSymbolFields.AttributeRef, value);
        }

        public string ElementId
        {
            get => (string)this.Fields[(int)WixBundleCustomDataCellSymbolFields.ElementId];
            set => this.Set((int)WixBundleCustomDataCellSymbolFields.ElementId, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)WixBundleCustomDataCellSymbolFields.Value];
            set => this.Set((int)WixBundleCustomDataCellSymbolFields.Value, value);
        }
    }
}
