// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition SelfReg = new IntermediateTupleDefinition(
            TupleDefinitionType.SelfReg,
            new[]
            {
                new IntermediateFieldDefinition(nameof(SelfRegTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SelfRegTupleFields.Cost), IntermediateFieldType.Number),
            },
            typeof(SelfRegTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum SelfRegTupleFields
    {
        File_,
        Cost,
    }

    public class SelfRegTuple : IntermediateTuple
    {
        public SelfRegTuple() : base(TupleDefinitions.SelfReg, null, null)
        {
        }

        public SelfRegTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.SelfReg, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SelfRegTupleFields index] => this.Fields[(int)index];

        public string File_
        {
            get => (string)this.Fields[(int)SelfRegTupleFields.File_]?.Value;
            set => this.Set((int)SelfRegTupleFields.File_, value);
        }

        public int Cost
        {
            get => (int)this.Fields[(int)SelfRegTupleFields.Cost]?.Value;
            set => this.Set((int)SelfRegTupleFields.Cost, value);
        }
    }
}