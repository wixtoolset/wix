// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixGroup = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixGroup,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixGroupSymbolFields.ParentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixGroupSymbolFields.ParentType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixGroupSymbolFields.ChildId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixGroupSymbolFields.ChildType), IntermediateFieldType.String),
            },
            typeof(WixGroupSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixGroupSymbolFields
    {
        ParentId,
        ParentType,
        ChildId,
        ChildType,
    }

    public class WixGroupSymbol : IntermediateSymbol
    {
        public WixGroupSymbol() : base(SymbolDefinitions.WixGroup, null, null)
        {
        }

        public WixGroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixGroupSymbolFields index] => this.Fields[(int)index];

        public string ParentId
        {
            get => (string)this.Fields[(int)WixGroupSymbolFields.ParentId];
            set => this.Set((int)WixGroupSymbolFields.ParentId, value);
        }

        public ComplexReferenceParentType ParentType
        {
            get => (ComplexReferenceParentType)Enum.Parse(typeof(ComplexReferenceParentType), (string)this.Fields[(int)WixGroupSymbolFields.ParentType], true);
            set => this.Set((int)WixGroupSymbolFields.ParentType, value.ToString());
        }

        public string ChildId
        {
            get => (string)this.Fields[(int)WixGroupSymbolFields.ChildId];
            set => this.Set((int)WixGroupSymbolFields.ChildId, value);
        }

        public ComplexReferenceChildType ChildType
        {
            get => (ComplexReferenceChildType)Enum.Parse(typeof(ComplexReferenceChildType), (string)this.Fields[(int)WixGroupSymbolFields.ChildType], true);
            set => this.Set((int)WixGroupSymbolFields.ChildType, value.ToString());
        }
    }
}