// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusGroupInApplicationRole = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusGroupInApplicationRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleTupleFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleTupleFields.GroupRef), IntermediateFieldType.String),
            },
            typeof(ComPlusGroupInApplicationRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusGroupInApplicationRoleTupleFields
    {
        ApplicationRoleRef,
        ComponentRef,
        GroupRef,
    }

    public class ComPlusGroupInApplicationRoleTuple : IntermediateTuple
    {
        public ComPlusGroupInApplicationRoleTuple() : base(ComPlusTupleDefinitions.ComPlusGroupInApplicationRole, null, null)
        {
        }

        public ComPlusGroupInApplicationRoleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusGroupInApplicationRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusGroupInApplicationRoleTupleFields index] => this.Fields[(int)index];

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleTupleFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleTupleFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleTupleFields.ComponentRef, value);
        }

        public string GroupRef
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleTupleFields.GroupRef].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleTupleFields.GroupRef, value);
        }
    }
}