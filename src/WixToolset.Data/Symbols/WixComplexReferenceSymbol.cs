// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixComplexReference = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixComplexReference,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixComplexReferenceSymbolFields.Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceSymbolFields.ParentAttributes), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceSymbolFields.ParentLanguage), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceSymbolFields.Child), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceSymbolFields.ChildAttributes), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceSymbolFields.Attributes), IntermediateFieldType.Bool),
            },
            typeof(WixComplexReferenceSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixComplexReferenceSymbolFields
    {
        Parent,
        ParentAttributes,
        ParentLanguage,
        Child,
        ChildAttributes,
        Attributes,
    }

    public class WixComplexReferenceSymbol : IntermediateSymbol
    {
        public WixComplexReferenceSymbol() : base(SymbolDefinitions.WixComplexReference, null, null)
        {
        }

        public WixComplexReferenceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixComplexReference, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixComplexReferenceSymbolFields index] => this.Fields[(int)index];

        public string Parent
        {
            get => (string)this.Fields[(int)WixComplexReferenceSymbolFields.Parent];
            set => this.Set((int)WixComplexReferenceSymbolFields.Parent, value);
        }

        public ComplexReferenceParentType ParentType
        {
            get => (ComplexReferenceParentType)Enum.Parse(typeof(ComplexReferenceParentType), (string)this.Fields[(int)WixComplexReferenceSymbolFields.ParentAttributes], true);
            set => this.Set((int)WixComplexReferenceSymbolFields.ParentAttributes, value.ToString());
        }

        public string ParentLanguage
        {
            get => (string)this.Fields[(int)WixComplexReferenceSymbolFields.ParentLanguage];
            set => this.Set((int)WixComplexReferenceSymbolFields.ParentLanguage, value);
        }

        public string Child
        {
            get => (string)this.Fields[(int)WixComplexReferenceSymbolFields.Child];
            set => this.Set((int)WixComplexReferenceSymbolFields.Child, value);
        }

        public ComplexReferenceChildType ChildType
        {
            get => (ComplexReferenceChildType)Enum.Parse(typeof(ComplexReferenceChildType), (string)this.Fields[(int)WixComplexReferenceSymbolFields.ChildAttributes], true);
            set => this.Set((int)WixComplexReferenceSymbolFields.ChildAttributes, value.ToString());
        }

        public bool IsPrimary
        {
            get => (bool)this.Fields[(int)WixComplexReferenceSymbolFields.Attributes];
            set => this.Set((int)WixComplexReferenceSymbolFields.Attributes, value);
        }
    }
}