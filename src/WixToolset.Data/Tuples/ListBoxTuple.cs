// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ListBox = new IntermediateTupleDefinition(
            TupleDefinitionType.ListBox,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ListBoxTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ListBoxTupleFields.Order), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ListBoxTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ListBoxTupleFields.Text), IntermediateFieldType.String),
            },
            typeof(ListBoxTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ListBoxTupleFields
    {
        Property,
        Order,
        Value,
        Text,
    }

    public class ListBoxTuple : IntermediateTuple
    {
        public ListBoxTuple() : base(TupleDefinitions.ListBox, null, null)
        {
        }

        public ListBoxTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ListBox, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ListBoxTupleFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)ListBoxTupleFields.Property];
            set => this.Set((int)ListBoxTupleFields.Property, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)ListBoxTupleFields.Order];
            set => this.Set((int)ListBoxTupleFields.Order, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ListBoxTupleFields.Value];
            set => this.Set((int)ListBoxTupleFields.Value, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)ListBoxTupleFields.Text];
            set => this.Set((int)ListBoxTupleFields.Text, value);
        }
    }
}