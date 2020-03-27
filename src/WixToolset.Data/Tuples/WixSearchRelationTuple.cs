// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixSearchRelation = new IntermediateTupleDefinition(
            TupleDefinitionType.WixSearchRelation,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSearchRelationTupleFields.ParentSearchRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSearchRelationTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixSearchRelationTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixSearchRelationTupleFields
    {
        ParentSearchRef,
        Attributes,
    }

    public class WixSearchRelationTuple : IntermediateTuple
    {
        public WixSearchRelationTuple() : base(TupleDefinitions.WixSearchRelation, null, null)
        {
        }

        public WixSearchRelationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixSearchRelation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSearchRelationTupleFields index] => this.Fields[(int)index];

        public string ParentSearchRef
        {
            get => (string)this.Fields[(int)WixSearchRelationTupleFields.ParentSearchRef];
            set => this.Set((int)WixSearchRelationTupleFields.ParentSearchRef, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixSearchRelationTupleFields.Attributes];
            set => this.Set((int)WixSearchRelationTupleFields.Attributes, value);
        }
    }
}