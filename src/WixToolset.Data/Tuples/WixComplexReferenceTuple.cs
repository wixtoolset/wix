// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixComplexReference = new IntermediateTupleDefinition(
            TupleDefinitionType.WixComplexReference,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixComplexReferenceTupleFields.Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceTupleFields.ParentAttributes), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceTupleFields.ParentLanguage), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceTupleFields.Child), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceTupleFields.ChildAttributes), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComplexReferenceTupleFields.Attributes), IntermediateFieldType.Bool),
            },
            typeof(WixComplexReferenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixComplexReferenceTupleFields
    {
        Parent,
        ParentAttributes,
        ParentLanguage,
        Child,
        ChildAttributes,
        Attributes,
    }

    public class WixComplexReferenceTuple : IntermediateTuple
    {
        public WixComplexReferenceTuple() : base(TupleDefinitions.WixComplexReference, null, null)
        {
        }

        public WixComplexReferenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixComplexReference, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixComplexReferenceTupleFields index] => this.Fields[(int)index];

        public string Parent
        {
            get => (string)this.Fields[(int)WixComplexReferenceTupleFields.Parent];
            set => this.Set((int)WixComplexReferenceTupleFields.Parent, value);
        }

        public ComplexReferenceParentType ParentType
        {
            get => (ComplexReferenceParentType)Enum.Parse(typeof(ComplexReferenceParentType), (string)this.Fields[(int)WixComplexReferenceTupleFields.ParentAttributes], true);
            set => this.Set((int)WixComplexReferenceTupleFields.ParentAttributes, value.ToString());
        }

        public string ParentLanguage
        {
            get => (string)this.Fields[(int)WixComplexReferenceTupleFields.ParentLanguage];
            set => this.Set((int)WixComplexReferenceTupleFields.ParentLanguage, value);
        }

        public string Child
        {
            get => (string)this.Fields[(int)WixComplexReferenceTupleFields.Child];
            set => this.Set((int)WixComplexReferenceTupleFields.Child, value);
        }

        public ComplexReferenceChildType ChildType
        {
            get => (ComplexReferenceChildType)Enum.Parse(typeof(ComplexReferenceChildType), (string)this.Fields[(int)WixComplexReferenceTupleFields.ChildAttributes], true);
            set => this.Set((int)WixComplexReferenceTupleFields.ChildAttributes, value.ToString());
        }

        public bool IsPrimary
        {
            get => (bool)this.Fields[(int)WixComplexReferenceTupleFields.Attributes];
            set => this.Set((int)WixComplexReferenceTupleFields.Attributes, value);
        }
    }
}