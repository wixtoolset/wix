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
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.TextStyle), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.FaceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Size), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.Color), IntermediateFieldType.Number),
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
        TextStyle,
        FaceName,
        Size,
        Color,
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

        public string TextStyle
        {
            get => (string)this.Fields[(int)TextStyleTupleFields.TextStyle];
            set => this.Set((int)TextStyleTupleFields.TextStyle, value);
        }

        public string FaceName
        {
            get => (string)this.Fields[(int)TextStyleTupleFields.FaceName];
            set => this.Set((int)TextStyleTupleFields.FaceName, value);
        }

        public int Size
        {
            get => this.Fields[(int)TextStyleTupleFields.Size].AsNumber();
            set => this.Set((int)TextStyleTupleFields.Size, value);
        }

        public int Color
        {
            get => (int)this.Fields[(int)TextStyleTupleFields.Color].AsNumber();
            set => this.Set((int)TextStyleTupleFields.Color, value);
        }

        public bool Bold
        {
            get => this.Fields[(int)TextStyleTupleFields.Bold].AsBool();
            set => this.Set((int)TextStyleTupleFields.Bold, value);
        }

        public bool Italic
        {
            get => this.Fields[(int)TextStyleTupleFields.Italic].AsBool();
            set => this.Set((int)TextStyleTupleFields.Italic, value);
        }

        public bool Strike
        {
            get => this.Fields[(int)TextStyleTupleFields.Strike].AsBool();
            set => this.Set((int)TextStyleTupleFields.Strike, value);
        }

        public bool Underline
        {
            get => this.Fields[(int)TextStyleTupleFields.Underline].AsBool();
            set => this.Set((int)TextStyleTupleFields.Underline, value);
        }
    }
}