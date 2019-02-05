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
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleTupleFields.GroupInPartitionRole), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleTupleFields.PartitionRole_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleTupleFields.Group_), IntermediateFieldType.String),
            },
            typeof(ComPlusGroupInPartitionRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusGroupInPartitionRoleTupleFields
    {
        GroupInPartitionRole,
        PartitionRole_,
        Component_,
        Group_,
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

        public string GroupInPartitionRole
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleTupleFields.GroupInPartitionRole].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleTupleFields.GroupInPartitionRole, value);
        }

        public string PartitionRole_
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleTupleFields.PartitionRole_].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleTupleFields.PartitionRole_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleTupleFields.Component_, value);
        }

        public string Group_
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleTupleFields.Group_].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleTupleFields.Group_, value);
        }
    }
}