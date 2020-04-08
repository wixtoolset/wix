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
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleTupleFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleTupleFields.UserRef), IntermediateFieldType.String),
            },
            typeof(ComPlusUserInApplicationRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusUserInApplicationRoleTupleFields
    {
        ApplicationRoleRef,
        ComponentRef,
        UserRef,
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

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleTupleFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleTupleFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleTupleFields.ComponentRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleTupleFields.UserRef].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleTupleFields.UserRef, value);
        }
    }
}