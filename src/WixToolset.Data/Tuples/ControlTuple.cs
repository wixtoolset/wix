// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Control = new IntermediateTupleDefinition(
            TupleDefinitionType.Control,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Dialog_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Control), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.X), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Y), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Text), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Control_Next), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Help), IntermediateFieldType.String),
            },
            typeof(ControlTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ControlTupleFields
    {
        Dialog_,
        Control,
        Type,
        X,
        Y,
        Width,
        Height,
        Attributes,
        Property,
        Text,
        Control_Next,
        Help,
    }

    public class ControlTuple : IntermediateTuple
    {
        public ControlTuple() : base(TupleDefinitions.Control, null, null)
        {
        }

        public ControlTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Control, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ControlTupleFields index] => this.Fields[(int)index];

        public string Dialog_
        {
            get => (string)this.Fields[(int)ControlTupleFields.Dialog_]?.Value;
            set => this.Set((int)ControlTupleFields.Dialog_, value);
        }

        public string Control
        {
            get => (string)this.Fields[(int)ControlTupleFields.Control]?.Value;
            set => this.Set((int)ControlTupleFields.Control, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)ControlTupleFields.Type]?.Value;
            set => this.Set((int)ControlTupleFields.Type, value);
        }

        public int X
        {
            get => (int)this.Fields[(int)ControlTupleFields.X]?.Value;
            set => this.Set((int)ControlTupleFields.X, value);
        }

        public int Y
        {
            get => (int)this.Fields[(int)ControlTupleFields.Y]?.Value;
            set => this.Set((int)ControlTupleFields.Y, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)ControlTupleFields.Width]?.Value;
            set => this.Set((int)ControlTupleFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)ControlTupleFields.Height]?.Value;
            set => this.Set((int)ControlTupleFields.Height, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)ControlTupleFields.Attributes]?.Value;
            set => this.Set((int)ControlTupleFields.Attributes, value);
        }

        public string Property
        {
            get => (string)this.Fields[(int)ControlTupleFields.Property]?.Value;
            set => this.Set((int)ControlTupleFields.Property, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)ControlTupleFields.Text]?.Value;
            set => this.Set((int)ControlTupleFields.Text, value);
        }

        public string Control_Next
        {
            get => (string)this.Fields[(int)ControlTupleFields.Control_Next]?.Value;
            set => this.Set((int)ControlTupleFields.Control_Next, value);
        }

        public string Help
        {
            get => (string)this.Fields[(int)ControlTupleFields.Help]?.Value;
            set => this.Set((int)ControlTupleFields.Help, value);
        }
    }
}