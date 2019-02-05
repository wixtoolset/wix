// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusPartition = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusPartition.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusPartitionTupleFields.Partition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionTupleFields.CustomId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionTupleFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusPartitionTupleFields
    {
        Partition,
        Component_,
        CustomId,
        Name,
    }

    public class ComPlusPartitionTuple : IntermediateTuple
    {
        public ComPlusPartitionTuple() : base(ComPlusTupleDefinitions.ComPlusPartition, null, null)
        {
        }

        public ComPlusPartitionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusPartition, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusPartitionTupleFields index] => this.Fields[(int)index];

        public string Partition
        {
            get => this.Fields[(int)ComPlusPartitionTupleFields.Partition].AsString();
            set => this.Set((int)ComPlusPartitionTupleFields.Partition, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusPartitionTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusPartitionTupleFields.Component_, value);
        }

        public string CustomId
        {
            get => this.Fields[(int)ComPlusPartitionTupleFields.CustomId].AsString();
            set => this.Set((int)ComPlusPartitionTupleFields.CustomId, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusPartitionTupleFields.Name].AsString();
            set => this.Set((int)ComPlusPartitionTupleFields.Name, value);
        }
    }
}