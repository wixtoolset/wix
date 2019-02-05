// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusUserInApplicationRole = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusUserInApplicationRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleTupleFields.UserInApplicationRole), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleTupleFields.ApplicationRole_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleTupleFields.User_), IntermediateFieldType.String),
            },
            typeof(ComPlusUserInApplicationRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusUserInApplicationRoleTupleFields
    {
        UserInApplicationRole,
        ApplicationRole_,
        Component_,
        User_,
    }

    public class ComPlusUserInApplicationRoleTuple : IntermediateTuple
    {
        public ComPlusUserInApplicationRoleTuple() : base(ComPlusTupleDefinitions.ComPlusUserInApplicationRole, null, null)
        {
        }

        public ComPlusUserInApplicationRoleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusUserInApplicationRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusUserInApplicationRoleTupleFields index] => this.Fields[(int)index];

        public string UserInApplicationRole
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleTupleFields.UserInApplicationRole].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleTupleFields.UserInApplicationRole, value);
        }

        public string ApplicationRole_
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleTupleFields.ApplicationRole_].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleTupleFields.ApplicationRole_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleTupleFields.Component_, value);
        }

        public string User_
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleTupleFields.User_].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleTupleFields.User_, value);
        }
    }
}