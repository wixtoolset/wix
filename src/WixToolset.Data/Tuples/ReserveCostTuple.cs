// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ReserveCost = new IntermediateTupleDefinition(
            TupleDefinitionType.ReserveCost,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ReserveCostTupleFields.ReserveKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ReserveCostTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ReserveCostTupleFields.ReserveFolder), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ReserveCostTupleFields.ReserveLocal), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ReserveCostTupleFields.ReserveSource), IntermediateFieldType.Number),
            },
            typeof(ReserveCostTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ReserveCostTupleFields
    {
        ReserveKey,
        Component_,
        ReserveFolder,
        ReserveLocal,
        ReserveSource,
    }

    public class ReserveCostTuple : IntermediateTuple
    {
        public ReserveCostTuple() : base(TupleDefinitions.ReserveCost, null, null)
        {
        }

        public ReserveCostTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ReserveCost, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ReserveCostTupleFields index] => this.Fields[(int)index];

        public string ReserveKey
        {
            get => (string)this.Fields[(int)ReserveCostTupleFields.ReserveKey];
            set => this.Set((int)ReserveCostTupleFields.ReserveKey, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)ReserveCostTupleFields.Component_];
            set => this.Set((int)ReserveCostTupleFields.Component_, value);
        }

        public string ReserveFolder
        {
            get => (string)this.Fields[(int)ReserveCostTupleFields.ReserveFolder];
            set => this.Set((int)ReserveCostTupleFields.ReserveFolder, value);
        }

        public int ReserveLocal
        {
            get => (int)this.Fields[(int)ReserveCostTupleFields.ReserveLocal];
            set => this.Set((int)ReserveCostTupleFields.ReserveLocal, value);
        }

        public int ReserveSource
        {
            get => (int)this.Fields[(int)ReserveCostTupleFields.ReserveSource];
            set => this.Set((int)ReserveCostTupleFields.ReserveSource, value);
        }
    }
}