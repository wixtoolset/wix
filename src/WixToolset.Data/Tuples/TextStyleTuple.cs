// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition TextStyle = new IntermediateTupleDefinition(
            TupleDefinitionType.TextStyle,
            new[]
            {
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.FaceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Size), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Red), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Green), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Blue), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Bold), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Italic), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Strike), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Underline), IntermediateFieldType.Bool),
            },
            typeof(TextStyleTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum TextStyleTupleFields
    {
        FaceName,
        Size,
        Red,
        Green,
        Blue,
        Bold,
        Italic,
        Strike,
        Underline,
    }

    public class TextStyleTuple : IntermediateTuple
    {
        public TextStyleTuple() : base(TupleDefinitions.TextStyle, null, null)
        {
        }

        public TextStyleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.TextStyle, sourceLineNumber, id)
        {
        }

        public IntermediateField this[TextStyleTupleFields index] => this.Fields[(int)index];

        public string FaceName
        {
            get => (string)this.Fields[(int)TextStyleTupleFields.FaceName];
            set => this.Set((int)TextStyleTupleFields.FaceName, value);
        }

        public int Size
        {
            get => (int)this.Fields[(int)TextStyleTupleFields.Size];
            set => this.Set((int)TextStyleTupleFields.Size, value);
        }

        public int? Red
        {
            get => (int?)this.Fields[(int)TextStyleTupleFields.Red];
            set => this.Set((int)TextStyleTupleFields.Red, value);
        }

        public int? Green
        {
            get => (int?)this.Fields[(int)TextStyleTupleFields.Green];
            set => this.Set((int)TextStyleTupleFields.Green, value);
        }

        public int? Blue
        {
            get => (int?)this.Fields[(int)TextStyleTupleFields.Blue];
            set => this.Set((int)TextStyleTupleFields.Blue, value);
        }

        public bool Bold
        {
            get => (bool)this.Fields[(int)TextStyleTupleFields.Bold];
            set => this.Set((int)TextStyleTupleFields.Bold, value);
        }

        public bool Italic
        {
            get => (bool)this.Fields[(int)TextStyleTupleFields.Italic];
            set => this.Set((int)TextStyleTupleFields.Italic, value);
        }

        public bool Strike
        {
            get => (bool)this.Fields[(int)TextStyleTupleFields.Strike];
            set => this.Set((int)TextStyleTupleFields.Strike, value);
        }

        public bool Underline
        {
            get => (bool)this.Fields[(int)TextStyleTupleFields.Underline];
            set => this.Set((int)TextStyleTupleFields.Underline, value);
        }
    }
}