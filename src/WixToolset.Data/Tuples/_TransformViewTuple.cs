// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition _TransformView = new IntermediateTupleDefinition(
            TupleDefinitionType._TransformView,
            new[]
            {
                new IntermediateFieldDefinition(nameof(_TransformViewTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_TransformViewTupleFields.Column), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_TransformViewTupleFields.Row), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_TransformViewTupleFields.Data), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(_TransformViewTupleFields.Current), IntermediateFieldType.String),
            },
            typeof(_TransformViewTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum _TransformViewTupleFields
    {
        Table,
        Column,
        Row,
        Data,
        Current,
    }

    public class _TransformViewTuple : IntermediateTuple
    {
        public _TransformViewTuple() : base(TupleDefinitions._TransformView, null, null)
        {
        }

        public _TransformViewTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions._TransformView, sourceLineNumber, id)
        {
        }

        public IntermediateField this[_TransformViewTupleFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)_TransformViewTupleFields.Table]?.Value;
            set => this.Set((int)_TransformViewTupleFields.Table, value);
        }

        public string Column
        {
            get => (string)this.Fields[(int)_TransformViewTupleFields.Column]?.Value;
            set => this.Set((int)_TransformViewTupleFields.Column, value);
        }

        public string Row
        {
            get => (string)this.Fields[(int)_TransformViewTupleFields.Row]?.Value;
            set => this.Set((int)_TransformViewTupleFields.Row, value);
        }

        public string Data
        {
            get => (string)this.Fields[(int)_TransformViewTupleFields.Data]?.Value;
            set => this.Set((int)_TransformViewTupleFields.Data, value);
        }

        public string Current
        {
            get => (string)this.Fields[(int)_TransformViewTupleFields.Current]?.Value;
            set => this.Set((int)_TransformViewTupleFields.Current, value);
        }
    }
}