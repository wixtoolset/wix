// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusUserInPartitionRole = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusUserInPartitionRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleTupleFields.PartitionRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleTupleFields.UserRef), IntermediateFieldType.String),
            },
            typeof(ComPlusUserInPartitionRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusUserInPartitionRoleTupleFields
    {
        PartitionRoleRef,
        ComponentRef,
        UserRef,
    }

    public class ComPlusUserInPartitionRoleTuple : IntermediateTuple
    {
        public ComPlusUserInPartitionRoleTuple() : base(ComPlusTupleDefinitions.ComPlusUserInPartitionRole, null, null)
        {
        }

        public ComPlusUserInPartitionRoleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusUserInPartitionRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusUserInPartitionRoleTupleFields index] => this.Fields[(int)index];

        public string PartitionRoleRef
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleTupleFields.PartitionRoleRef].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleTupleFields.PartitionRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleTupleFields.ComponentRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleTupleFields.UserRef].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleTupleFields.UserRef, value);
        }
    }
}