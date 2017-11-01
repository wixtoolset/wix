// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition _Streams = new IntermediateTupleDefinition(
            TupleDefinitionType._Streams,
            new[]
            {
                new IntermediateFieldDefinition(nameof(_StreamsTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_StreamsTupleFields.Data), IntermediateFieldType.Path),
            },
            typeof(_StreamsTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum _StreamsTupleFields
    {
        Name,
        Data,
    }

    public class _StreamsTuple : IntermediateTuple
    {
        public _StreamsTuple() : base(TupleDefinitions._Streams, null, null)
        {
        }

        public _StreamsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions._Streams, sourceLineNumber, id)
        {
        }

        public IntermediateField this[_StreamsTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)_StreamsTupleFields.Name]?.Value;
            set => this.Set((int)_StreamsTupleFields.Name, value);
        }

        public string Data
        {
            get => (string)this.Fields[(int)_StreamsTupleFields.Data]?.Value;
            set => this.Set((int)_StreamsTupleFields.Data, value);
        }
    }
}