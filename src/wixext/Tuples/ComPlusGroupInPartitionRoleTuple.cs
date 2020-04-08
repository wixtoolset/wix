// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusGroupInPartitionRole = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusGroupInPartitionRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleTupleFields.PartitionRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleTupleFields.GroupRef), IntermediateFieldType.String),
            },
            typeof(ComPlusGroupInPartitionRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusGroupInPartitionRoleTupleFields
    {
        PartitionRoleRef,
        ComponentRef,
        GroupRef,
    }

    public class ComPlusGroupInPartitionRoleTuple : IntermediateTuple
    {
        public ComPlusGroupInPartitionRoleTuple() : base(ComPlusTupleDefinitions.ComPlusGroupInPartitionRole, null, null)
        {
        }

        public ComPlusGroupInPartitionRoleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusGroupInPartitionRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusGroupInPartitionRoleTupleFields index] => this.Fields[(int)index];

        public string PartitionRoleRef
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleTupleFields.PartitionRoleRef].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleTupleFields.PartitionRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleTupleFields.ComponentRef, value);
        }

        public string GroupRef
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleTupleFields.GroupRef].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleTupleFields.GroupRef, value);
        }
    }
}