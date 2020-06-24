// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixSearchRelation = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixSearchRelation,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSearchRelationSymbolFields.ParentSearchRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSearchRelationSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixSearchRelationSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixSearchRelationSymbolFields
    {
        ParentSearchRef,
        Attributes,
    }

    public class WixSearchRelationSymbol : IntermediateSymbol
    {
        public WixSearchRelationSymbol() : base(SymbolDefinitions.WixSearchRelation, null, null)
        {
        }

        public WixSearchRelationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixSearchRelation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSearchRelationSymbolFields index] => this.Fields[(int)index];

        public string ParentSearchRef
        {
            get => (string)this.Fields[(int)WixSearchRelationSymbolFields.ParentSearchRef];
            set => this.Set((int)WixSearchRelationSymbolFields.ParentSearchRef, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixSearchRelationSymbolFields.Attributes];
            set => this.Set((int)WixSearchRelationSymbolFields.Attributes, value);
        }
    }
}