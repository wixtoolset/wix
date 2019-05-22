// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Binary = new IntermediateTupleDefinition(
            TupleDefinitionType.Binary,
            new[]
            {
                new IntermediateFieldDefinition(nameof(BinaryTupleFields.Data), IntermediateFieldType.Path),
            },
            typeof(BinaryTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum BinaryTupleFields
    {
        Data,
    }

    public class BinaryTuple : IntermediateTuple
    {
        public BinaryTuple() : base(TupleDefinitions.Binary, null, null)
        {
        }

        public BinaryTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Binary, sourceLineNumber, id)
        {
        }

        public IntermediateField this[BinaryTupleFields index] => this.Fields[(int)index];

        public string Data
        {
            get => (string)this.Fields[(int)BinaryTupleFields.Data];
            set => this.Set((int)BinaryTupleFields.Data, value);
        }
    }
}