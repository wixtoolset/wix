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
                new IntermediateFieldDefinition(nameof(WixSearchRelationTupleFields.WixSearch_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSearchRelationTupleFields.ParentId_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSearchRelationTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixSearchRelationTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixSearchRelationTupleFields
    {
        WixSearch_,
        ParentId_,
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

        public string WixSearch_
        {
            get => (string)this.Fields[(int)WixSearchRelationTupleFields.WixSearch_]?.Value;
            set => this.Set((int)WixSearchRelationTupleFields.WixSearch_, value);
        }

        public string ParentId_
        {
            get => (string)this.Fields[(int)WixSearchRelationTupleFields.ParentId_]?.Value;
            set => this.Set((int)WixSearchRelationTupleFields.ParentId_, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixSearchRelationTupleFields.Attributes]?.Value;
            set => this.Set((int)WixSearchRelationTupleFields.Attributes, value);
        }
    }
}