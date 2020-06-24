// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Billboard = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Billboard,
            new[]
            {
                new IntermediateFieldDefinition(nameof(BillboardSymbolFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BillboardSymbolFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BillboardSymbolFields.Ordering), IntermediateFieldType.Number),
            },
            typeof(BillboardSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum BillboardSymbolFields
    {
        FeatureRef,
        Action,
        Ordering,
    }

    public class BillboardSymbol : IntermediateSymbol
    {
        public BillboardSymbol() : base(SymbolDefinitions.Billboard, null, null)
        {
        }

        public BillboardSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Billboard, sourceLineNumber, id)
        {
        }

        public IntermediateField this[BillboardSymbolFields index] => this.Fields[(int)index];

        public string FeatureRef
        {
            get => (string)this.Fields[(int)BillboardSymbolFields.FeatureRef];
            set => this.Set((int)BillboardSymbolFields.FeatureRef, value);
        }

        public string Action
        {
            get => (string)this.Fields[(int)BillboardSymbolFields.Action];
            set => this.Set((int)BillboardSymbolFields.Action, value);
        }

        public int? Ordering
        {
            get => (int?)this.Fields[(int)BillboardSymbolFields.Ordering];
            set => this.Set((int)BillboardSymbolFields.Ordering, value);
        }
    }
}