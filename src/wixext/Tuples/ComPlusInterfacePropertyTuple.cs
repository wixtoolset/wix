// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusInterfaceProperty = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusInterfaceProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusInterfacePropertyTupleFields.Interface_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusInterfacePropertyTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusInterfacePropertyTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusInterfacePropertyTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusInterfacePropertyTupleFields
    {
        Interface_,
        Name,
        Value,
    }

    public class ComPlusInterfacePropertyTuple : IntermediateTuple
    {
        public ComPlusInterfacePropertyTuple() : base(ComPlusTupleDefinitions.ComPlusInterfaceProperty, null, null)
        {
        }

        public ComPlusInterfacePropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusInterfaceProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusInterfacePropertyTupleFields index] => this.Fields[(int)index];

        public string Interface_
        {
            get => this.Fields[(int)ComPlusInterfacePropertyTupleFields.Interface_].AsString();
            set => this.Set((int)ComPlusInterfacePropertyTupleFields.Interface_, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusInterfacePropertyTupleFields.Name].AsString();
            set => this.Set((int)ComPlusInterfacePropertyTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusInterfacePropertyTupleFields.Value].AsString();
            set => this.Set((int)ComPlusInterfacePropertyTupleFields.Value, value);
        }
    }
}