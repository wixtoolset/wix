// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition _SummaryInformation = new IntermediateTupleDefinition(
            TupleDefinitionType._SummaryInformation,
            new[]
            {
                new IntermediateFieldDefinition(nameof(_SummaryInformationTupleFields.PropertyId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(_SummaryInformationTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(_SummaryInformationTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum _SummaryInformationTupleFields
    {
        PropertyId,
        Value,
    }

    public class _SummaryInformationTuple : IntermediateTuple
    {
        public _SummaryInformationTuple() : base(TupleDefinitions._SummaryInformation, null, null)
        {
        }

        public _SummaryInformationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions._SummaryInformation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[_SummaryInformationTupleFields index] => this.Fields[(int)index];

        public int PropertyId
        {
            get => (int)this.Fields[(int)_SummaryInformationTupleFields.PropertyId];
            set => this.Set((int)_SummaryInformationTupleFields.PropertyId, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)_SummaryInformationTupleFields.Value];
            set => this.Set((int)_SummaryInformationTupleFields.Value, value);
        }
    }
}