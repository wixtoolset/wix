// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixOrdering = new IntermediateTupleDefinition(
            TupleDefinitionType.WixOrdering,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixOrderingTupleFields.ItemType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixOrderingTupleFields.ItemIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixOrderingTupleFields.DependsOnType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixOrderingTupleFields.DependsOnIdRef), IntermediateFieldType.String),
            },
            typeof(WixOrderingTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixOrderingTupleFields
    {
        ItemType,
        ItemIdRef,
        DependsOnType,
        DependsOnIdRef,
    }

    public class WixOrderingTuple : IntermediateTuple
    {
        public WixOrderingTuple() : base(TupleDefinitions.WixOrdering, null, null)
        {
        }

        public WixOrderingTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixOrdering, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixOrderingTupleFields index] => this.Fields[(int)index];

        public ComplexReferenceChildType ItemType
        {
            get => (ComplexReferenceChildType)this.Fields[(int)WixOrderingTupleFields.ItemType].AsNumber();
            set => this.Set((int)WixOrderingTupleFields.ItemType, (int)value);
        }

        public string ItemIdRef
        {
            get => (string)this.Fields[(int)WixOrderingTupleFields.ItemIdRef];
            set => this.Set((int)WixOrderingTupleFields.ItemIdRef, value);
        }

        public ComplexReferenceChildType DependsOnType
        {
            get => (ComplexReferenceChildType)this.Fields[(int)WixOrderingTupleFields.DependsOnType].AsNumber();
            set => this.Set((int)WixOrderingTupleFields.DependsOnType, (int)value);
        }

        public string DependsOnIdRef
        {
            get => (string)this.Fields[(int)WixOrderingTupleFields.DependsOnIdRef];
            set => this.Set((int)WixOrderingTupleFields.DependsOnIdRef, value);
        }
    }
}