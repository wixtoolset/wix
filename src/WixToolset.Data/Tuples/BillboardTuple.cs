// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Billboard = new IntermediateTupleDefinition(
            TupleDefinitionType.Billboard,
            new[]
            {
                new IntermediateFieldDefinition(nameof(BillboardTupleFields.Billboard), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BillboardTupleFields.Feature_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BillboardTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BillboardTupleFields.Ordering), IntermediateFieldType.Number),
            },
            typeof(BillboardTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum BillboardTupleFields
    {
        Billboard,
        Feature_,
        Action,
        Ordering,
    }

    public class BillboardTuple : IntermediateTuple
    {
        public BillboardTuple() : base(TupleDefinitions.Billboard, null, null)
        {
        }

        public BillboardTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Billboard, sourceLineNumber, id)
        {
        }

        public IntermediateField this[BillboardTupleFields index] => this.Fields[(int)index];

        public string Billboard
        {
            get => (string)this.Fields[(int)BillboardTupleFields.Billboard]?.Value;
            set => this.Set((int)BillboardTupleFields.Billboard, value);
        }

        public string Feature_
        {
            get => (string)this.Fields[(int)BillboardTupleFields.Feature_]?.Value;
            set => this.Set((int)BillboardTupleFields.Feature_, value);
        }

        public string Action
        {
            get => (string)this.Fields[(int)BillboardTupleFields.Action]?.Value;
            set => this.Set((int)BillboardTupleFields.Action, value);
        }

        public int Ordering
        {
            get => (int)this.Fields[(int)BillboardTupleFields.Ordering]?.Value;
            set => this.Set((int)BillboardTupleFields.Ordering, value);
        }
    }
}