// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusPartitionRole = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusPartitionRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleTupleFields.PartitionRole), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleTupleFields.Partition_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleTupleFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusPartitionRoleTupleFields
    {
        PartitionRole,
        Partition_,
        Component_,
        Name,
    }

    public class ComPlusPartitionRoleTuple : IntermediateTuple
    {
        public ComPlusPartitionRoleTuple() : base(ComPlusTupleDefinitions.ComPlusPartitionRole, null, null)
        {
        }

        public ComPlusPartitionRoleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusPartitionRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusPartitionRoleTupleFields index] => this.Fields[(int)index];

        public string PartitionRole
        {
            get => this.Fields[(int)ComPlusPartitionRoleTupleFields.PartitionRole].AsString();
            set => this.Set((int)ComPlusPartitionRoleTupleFields.PartitionRole, value);
        }

        public string Partition_
        {
            get => this.Fields[(int)ComPlusPartitionRoleTupleFields.Partition_].AsString();
            set => this.Set((int)ComPlusPartitionRoleTupleFields.Partition_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusPartitionRoleTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusPartitionRoleTupleFields.Component_, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusPartitionRoleTupleFields.Name].AsString();
            set => this.Set((int)ComPlusPartitionRoleTupleFields.Name, value);
        }
    }
}