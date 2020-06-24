// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixOrdering = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixOrdering,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixOrderingSymbolFields.ItemType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixOrderingSymbolFields.ItemIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixOrderingSymbolFields.DependsOnType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixOrderingSymbolFields.DependsOnIdRef), IntermediateFieldType.String),
            },
            typeof(WixOrderingSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixOrderingSymbolFields
    {
        ItemType,
        ItemIdRef,
        DependsOnType,
        DependsOnIdRef,
    }

    public class WixOrderingSymbol : IntermediateSymbol
    {
        public WixOrderingSymbol() : base(SymbolDefinitions.WixOrdering, null, null)
        {
        }

        public WixOrderingSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixOrdering, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixOrderingSymbolFields index] => this.Fields[(int)index];

        public ComplexReferenceChildType ItemType
        {
            get => (ComplexReferenceChildType)this.Fields[(int)WixOrderingSymbolFields.ItemType].AsNumber();
            set => this.Set((int)WixOrderingSymbolFields.ItemType, (int)value);
        }

        public string ItemIdRef
        {
            get => (string)this.Fields[(int)WixOrderingSymbolFields.ItemIdRef];
            set => this.Set((int)WixOrderingSymbolFields.ItemIdRef, value);
        }

        public ComplexReferenceChildType DependsOnType
        {
            get => (ComplexReferenceChildType)this.Fields[(int)WixOrderingSymbolFields.DependsOnType].AsNumber();
            set => this.Set((int)WixOrderingSymbolFields.DependsOnType, (int)value);
        }

        public string DependsOnIdRef
        {
            get => (string)this.Fields[(int)WixOrderingSymbolFields.DependsOnIdRef];
            set => this.Set((int)WixOrderingSymbolFields.DependsOnIdRef, value);
        }
    }
}