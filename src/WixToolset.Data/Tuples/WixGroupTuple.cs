// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixGroup = new IntermediateTupleDefinition(
            TupleDefinitionType.WixGroup,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixGroupTupleFields.ParentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixGroupTupleFields.ParentType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixGroupTupleFields.ChildId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixGroupTupleFields.ChildType), IntermediateFieldType.String),
            },
            typeof(WixGroupTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixGroupTupleFields
    {
        ParentId,
        ParentType,
        ChildId,
        ChildType,
    }

    public class WixGroupTuple : IntermediateTuple
    {
        public WixGroupTuple() : base(TupleDefinitions.WixGroup, null, null)
        {
        }

        public WixGroupTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixGroupTupleFields index] => this.Fields[(int)index];

        public string ParentId
        {
            get => (string)this.Fields[(int)WixGroupTupleFields.ParentId]?.Value;
            set => this.Set((int)WixGroupTupleFields.ParentId, value);
        }

        public ComplexReferenceParentType ParentType
        {
            get => (ComplexReferenceParentType)Enum.Parse(typeof(ComplexReferenceParentType), (string)this.Fields[(int)WixGroupTupleFields.ParentType]?.Value, true);
            set => this.Set((int)WixGroupTupleFields.ParentType, value.ToString());
        }

        public string ChildId
        {
            get => (string)this.Fields[(int)WixGroupTupleFields.ChildId]?.Value;
            set => this.Set((int)WixGroupTupleFields.ChildId, value);
        }

        public ComplexReferenceChildType ChildType
        {
            get => (ComplexReferenceChildType)Enum.Parse(typeof(ComplexReferenceChildType), (string)this.Fields[(int)WixGroupTupleFields.ChildType]?.Value, true);
            set => this.Set((int)WixGroupTupleFields.ChildType, value.ToString());
        }
    }
}