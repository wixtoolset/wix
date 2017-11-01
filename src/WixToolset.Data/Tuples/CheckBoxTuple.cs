// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition CheckBox = new IntermediateTupleDefinition(
            TupleDefinitionType.CheckBox,
            new[]
            {
                new IntermediateFieldDefinition(nameof(CheckBoxTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CheckBoxTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(CheckBoxTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum CheckBoxTupleFields
    {
        Property,
        Value,
    }

    public class CheckBoxTuple : IntermediateTuple
    {
        public CheckBoxTuple() : base(TupleDefinitions.CheckBox, null, null)
        {
        }

        public CheckBoxTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.CheckBox, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CheckBoxTupleFields index] => this.Fields[(int)index];

        public string Property
        {
            get => (string)this.Fields[(int)CheckBoxTupleFields.Property]?.Value;
            set => this.Set((int)CheckBoxTupleFields.Property, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)CheckBoxTupleFields.Value]?.Value;
            set => this.Set((int)CheckBoxTupleFields.Value, value);
        }
    }
}