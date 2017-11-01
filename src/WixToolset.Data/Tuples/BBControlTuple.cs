// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition BBControl = new IntermediateTupleDefinition(
            TupleDefinitionType.BBControl,
            new[]
            {
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Billboard_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.BBControl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.X), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Y), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Text), IntermediateFieldType.String),
            },
            typeof(BBControlTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum BBControlTupleFields
    {
        Billboard_,
        BBControl,
        Type,
        X,
        Y,
        Width,
        Height,
        Attributes,
        Text,
    }

    public class BBControlTuple : IntermediateTuple
    {
        public BBControlTuple() : base(TupleDefinitions.BBControl, null, null)
        {
        }

        public BBControlTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.BBControl, sourceLineNumber, id)
        {
        }

        public IntermediateField this[BBControlTupleFields index] => this.Fields[(int)index];

        public string Billboard_
        {
            get => (string)this.Fields[(int)BBControlTupleFields.Billboard_]?.Value;
            set => this.Set((int)BBControlTupleFields.Billboard_, value);
        }

        public string BBControl
        {
            get => (string)this.Fields[(int)BBControlTupleFields.BBControl]?.Value;
            set => this.Set((int)BBControlTupleFields.BBControl, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)BBControlTupleFields.Type]?.Value;
            set => this.Set((int)BBControlTupleFields.Type, value);
        }

        public int X
        {
            get => (int)this.Fields[(int)BBControlTupleFields.X]?.Value;
            set => this.Set((int)BBControlTupleFields.X, value);
        }

        public int Y
        {
            get => (int)this.Fields[(int)BBControlTupleFields.Y]?.Value;
            set => this.Set((int)BBControlTupleFields.Y, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)BBControlTupleFields.Width]?.Value;
            set => this.Set((int)BBControlTupleFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)BBControlTupleFields.Height]?.Value;
            set => this.Set((int)BBControlTupleFields.Height, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)BBControlTupleFields.Attributes]?.Value;
            set => this.Set((int)BBControlTupleFields.Attributes, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)BBControlTupleFields.Text]?.Value;
            set => this.Set((int)BBControlTupleFields.Text, value);
        }
    }
}