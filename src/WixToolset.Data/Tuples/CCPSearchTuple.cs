// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition CCPSearch = new IntermediateTupleDefinition(
            TupleDefinitionType.CCPSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(CCPSearchTupleFields.Signature_), IntermediateFieldType.String),
            },
            typeof(CCPSearchTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum CCPSearchTupleFields
    {
        Signature_,
    }

    public class CCPSearchTuple : IntermediateTuple
    {
        public CCPSearchTuple() : base(TupleDefinitions.CCPSearch, null, null)
        {
        }

        public CCPSearchTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.CCPSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CCPSearchTupleFields index] => this.Fields[(int)index];

        public string Signature_
        {
            get => (string)this.Fields[(int)CCPSearchTupleFields.Signature_];
            set => this.Set((int)CCPSearchTupleFields.Signature_, value);
        }
    }
}