// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComboBox = new IntermediateTupleDefinition(
            TupleDefinitionType.ComboBox,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComboBoxTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComboBoxTupleFields.Order), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ComboBoxTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComboBoxTupleFields.Text), IntermediateFieldType.String),
            },
            typeof(ComboBoxTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ComboBoxTupleFields
    {
        Property,
        Order,
        Value,
        Text,
    }

    public class ComboBoxTuple : IntermediateTuple
    {
        public ComboBoxTuple() : base(TupleDefinitions.ComboBox, null, null)
        {
        }

        public ComboBoxTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ComboBox, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComboBoxTupleFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)ComboBoxTupleFields.Property]?.Value;
            set => this.Set((int)ComboBoxTupleFields.Property, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)ComboBoxTupleFields.Order]?.Value;
            set => this.Set((int)ComboBoxTupleFields.Order, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ComboBoxTupleFields.Value]?.Value;
            set => this.Set((int)ComboBoxTupleFields.Value, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)ComboBoxTupleFields.Text]?.Value;
            set => this.Set((int)ComboBoxTupleFields.Text, value);
        }
    }
}