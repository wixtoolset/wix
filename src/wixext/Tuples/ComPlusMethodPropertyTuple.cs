// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusMethodProperty = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusMethodProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusMethodPropertyTupleFields.Method_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusMethodPropertyTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusMethodPropertyTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusMethodPropertyTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusMethodPropertyTupleFields
    {
        Method_,
        Name,
        Value,
    }

    public class ComPlusMethodPropertyTuple : IntermediateTuple
    {
        public ComPlusMethodPropertyTuple() : base(ComPlusTupleDefinitions.ComPlusMethodProperty, null, null)
        {
        }

        public ComPlusMethodPropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusMethodProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusMethodPropertyTupleFields index] => this.Fields[(int)index];

        public string Method_
        {
            get => this.Fields[(int)ComPlusMethodPropertyTupleFields.Method_].AsString();
            set => this.Set((int)ComPlusMethodPropertyTupleFields.Method_, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusMethodPropertyTupleFields.Name].AsString();
            set => this.Set((int)ComPlusMethodPropertyTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusMethodPropertyTupleFields.Value].AsString();
            set => this.Set((int)ComPlusMethodPropertyTupleFields.Value, value);
        }
    }
}