// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusPartitionProperty = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusPartitionProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusPartitionPropertyTupleFields.Partition_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionPropertyTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionPropertyTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionPropertyTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusPartitionPropertyTupleFields
    {
        Partition_,
        Name,
        Value,
    }

    public class ComPlusPartitionPropertyTuple : IntermediateTuple
    {
        public ComPlusPartitionPropertyTuple() : base(ComPlusTupleDefinitions.ComPlusPartitionProperty, null, null)
        {
        }

        public ComPlusPartitionPropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusPartitionProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusPartitionPropertyTupleFields index] => this.Fields[(int)index];

        public string Partition_
        {
            get => this.Fields[(int)ComPlusPartitionPropertyTupleFields.Partition_].AsString();
            set => this.Set((int)ComPlusPartitionPropertyTupleFields.Partition_, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusPartitionPropertyTupleFields.Name].AsString();
            set => this.Set((int)ComPlusPartitionPropertyTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusPartitionPropertyTupleFields.Value].AsString();
            set => this.Set((int)ComPlusPartitionPropertyTupleFields.Value, value);
        }
    }
}