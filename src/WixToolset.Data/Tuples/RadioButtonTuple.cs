// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition RadioButton = new IntermediateTupleDefinition(
            TupleDefinitionType.RadioButton,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RadioButtonTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RadioButtonTupleFields.Order), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RadioButtonTupleFields.X), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonTupleFields.Y), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonTupleFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonTupleFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RadioButtonTupleFields.Text), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RadioButtonTupleFields.Help), IntermediateFieldType.String),
            },
            typeof(RadioButtonTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RadioButtonTupleFields
    {
        Property,
        Order,
        Value,
        X,
        Y,
        Width,
        Height,
        Text,
        Help,
    }

    public class RadioButtonTuple : IntermediateTuple
    {
        public RadioButtonTuple() : base(TupleDefinitions.RadioButton, null, null)
        {
        }

        public RadioButtonTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.RadioButton, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RadioButtonTupleFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)RadioButtonTupleFields.Property];
            set => this.Set((int)RadioButtonTupleFields.Property, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)RadioButtonTupleFields.Order];
            set => this.Set((int)RadioButtonTupleFields.Order, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)RadioButtonTupleFields.Value];
            set => this.Set((int)RadioButtonTupleFields.Value, value);
        }

        public int X
        {
            get => (int)this.Fields[(int)RadioButtonTupleFields.X];
            set => this.Set((int)RadioButtonTupleFields.X, value);
        }

        public int Y
        {
            get => (int)this.Fields[(int)RadioButtonTupleFields.Y];
            set => this.Set((int)RadioButtonTupleFields.Y, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)RadioButtonTupleFields.Width];
            set => this.Set((int)RadioButtonTupleFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)RadioButtonTupleFields.Height];
            set => this.Set((int)RadioButtonTupleFields.Height, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)RadioButtonTupleFields.Text];
            set => this.Set((int)RadioButtonTupleFields.Text, value);
        }

        public string Help
        {
            get => (string)this.Fields[(int)RadioButtonTupleFields.Help];
            set => this.Set((int)RadioButtonTupleFields.Help, value);
        }
    }
}