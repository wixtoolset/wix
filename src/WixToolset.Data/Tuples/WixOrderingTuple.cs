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
                new IntermediateFieldDefinition(nameof(WixOrderingTupleFields.ItemId_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixOrderingTupleFields.DependsOnType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixOrderingTupleFields.DependsOnId_), IntermediateFieldType.String),
            },
            typeof(WixOrderingTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixOrderingTupleFields
    {
        ItemType,
        ItemId_,
        DependsOnType,
        DependsOnId_,
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

        public string ItemId_
        {
            get => (string)this.Fields[(int)WixOrderingTupleFields.ItemId_];
            set => this.Set((int)WixOrderingTupleFields.ItemId_, value);
        }

        public ComplexReferenceChildType DependsOnType
        {
            get => (ComplexReferenceChildType)this.Fields[(int)WixOrderingTupleFields.DependsOnType].AsNumber();
            set => this.Set((int)WixOrderingTupleFields.DependsOnType, (int)value);
        }

        public string DependsOnId_
        {
            get => (string)this.Fields[(int)WixOrderingTupleFields.DependsOnId_];
            set => this.Set((int)WixOrderingTupleFields.DependsOnId_, value);
        }
    }
}