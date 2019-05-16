// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Properties = new IntermediateTupleDefinition(
            TupleDefinitionType.Properties,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PropertiesTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PropertiesTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(PropertiesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum PropertiesTupleFields
    {
        Name,
        Value,
    }

    public class PropertiesTuple : IntermediateTuple
    {
        public PropertiesTuple() : base(TupleDefinitions.Properties, null, null)
        {
        }

        public PropertiesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Properties, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PropertiesTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)PropertiesTupleFields.Name];
            set => this.Set((int)PropertiesTupleFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)PropertiesTupleFields.Value];
            set => this.Set((int)PropertiesTupleFields.Value, value);
        }
    }
}