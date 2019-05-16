// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition UIText = new IntermediateTupleDefinition(
            TupleDefinitionType.UIText,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UITextTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UITextTupleFields.Text), IntermediateFieldType.String),
            },
            typeof(UITextTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum UITextTupleFields
    {
        Key,
        Text,
    }

    public class UITextTuple : IntermediateTuple
    {
        public UITextTuple() : base(TupleDefinitions.UIText, null, null)
        {
        }

        public UITextTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.UIText, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UITextTupleFields index] => this.Fields[(int)index];

        public string Key
        {
            get => (string)this.Fields[(int)UITextTupleFields.Key];
            set => this.Set((int)UITextTupleFields.Key, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)UITextTupleFields.Text];
            set => this.Set((int)UITextTupleFields.Text, value);
        }
    }
}