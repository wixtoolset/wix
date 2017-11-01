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
                new IntermediateFieldDefinition(nameof(TextStyleTupleFields.StyleBits), IntermediateFieldType.Number),
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
        StyleBits,
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
            get => (string)this.Fields[(int)TextStyleTupleFields.TextStyle]?.Value;
            set => this.Set((int)TextStyleTupleFields.TextStyle, value);
        }

        public string FaceName
        {
            get => (string)this.Fields[(int)TextStyleTupleFields.FaceName]?.Value;
            set => this.Set((int)TextStyleTupleFields.FaceName, value);
        }

        public int Size
        {
            get => (int)this.Fields[(int)TextStyleTupleFields.Size]?.Value;
            set => this.Set((int)TextStyleTupleFields.Size, value);
        }

        public int Color
        {
            get => (int)this.Fields[(int)TextStyleTupleFields.Color]?.Value;
            set => this.Set((int)TextStyleTupleFields.Color, value);
        }

        public int StyleBits
        {
            get => (int)this.Fields[(int)TextStyleTupleFields.StyleBits]?.Value;
            set => this.Set((int)TextStyleTupleFields.StyleBits, value);
        }
    }
}