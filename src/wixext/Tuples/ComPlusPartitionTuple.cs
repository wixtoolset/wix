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
                new IntermediateFieldDefinition(nameof(ComPlusPartitionTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionTupleFields.PartitionId), IntermediateFieldType.String),
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
        ComponentRef,
        PartitionId,
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

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusPartitionTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusPartitionTupleFields.ComponentRef, value);
        }

        public string PartitionId
        {
            get => this.Fields[(int)ComPlusPartitionTupleFields.PartitionId].AsString();
            set => this.Set((int)ComPlusPartitionTupleFields.PartitionId, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusPartitionTupleFields.Name].AsString();
            set => this.Set((int)ComPlusPartitionTupleFields.Name, value);
        }
    }
}