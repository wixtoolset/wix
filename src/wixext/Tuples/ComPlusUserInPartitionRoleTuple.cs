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
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleTupleFields.UserInPartitionRole), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleTupleFields.PartitionRole_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleTupleFields.User_), IntermediateFieldType.String),
            },
            typeof(ComPlusUserInPartitionRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusUserInPartitionRoleTupleFields
    {
        UserInPartitionRole,
        PartitionRole_,
        Component_,
        User_,
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

        public string UserInPartitionRole
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleTupleFields.UserInPartitionRole].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleTupleFields.UserInPartitionRole, value);
        }

        public string PartitionRole_
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleTupleFields.PartitionRole_].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleTupleFields.PartitionRole_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleTupleFields.Component_, value);
        }

        public string User_
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleTupleFields.User_].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleTupleFields.User_, value);
        }
    }
}