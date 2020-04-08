// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusApplicationProperty = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusApplicationProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusApplicationPropertyTupleFields.ApplicationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationPropertyTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationPropertyTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusApplicationPropertyTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusApplicationPropertyTupleFields
    {
        ApplicationRef,
        Name,
        Value,
    }

    public class ComPlusApplicationPropertyTuple : IntermediateTuple
    {
        public ComPlusApplicationPropertyTuple() : base(ComPlusTupleDefinitions.ComPlusApplicationProperty, null, null)
        {
        }

        public ComPlusApplicationPropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusApplicationProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusApplicationPropertyTupleFields index] => this.Fields[(int)index];

        public string ApplicationRef
        {
            get => this.Fields[(int)ComPlusApplicationPropertyTupleFields.ApplicationRef].AsString();
            set => this.Set((int)ComPlusApplicationPropertyTupleFields.ApplicationRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationPropertyTupleFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationPropertyTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusApplicationPropertyTupleFields.Value].AsString();
            set => this.Set((int)ComPlusApplicationPropertyTupleFields.Value, value);
        }
    }
}