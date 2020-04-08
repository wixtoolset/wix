// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusComponentProperty = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusComponentProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusComponentPropertyTupleFields.ComPlusComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusComponentPropertyTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusComponentPropertyTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusComponentPropertyTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusComponentPropertyTupleFields
    {
        ComPlusComponentRef,
        Name,
        Value,
    }

    public class ComPlusComponentPropertyTuple : IntermediateTuple
    {
        public ComPlusComponentPropertyTuple() : base(ComPlusTupleDefinitions.ComPlusComponentProperty, null, null)
        {
        }

        public ComPlusComponentPropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusComponentProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusComponentPropertyTupleFields index] => this.Fields[(int)index];

        public string ComPlusComponentRef
        {
            get => this.Fields[(int)ComPlusComponentPropertyTupleFields.ComPlusComponentRef].AsString();
            set => this.Set((int)ComPlusComponentPropertyTupleFields.ComPlusComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusComponentPropertyTupleFields.Name].AsString();
            set => this.Set((int)ComPlusComponentPropertyTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusComponentPropertyTupleFields.Value].AsString();
            set => this.Set((int)ComPlusComponentPropertyTupleFields.Value, value);
        }
    }
}