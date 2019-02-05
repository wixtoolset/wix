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
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleTupleFields.GroupInApplicationRole), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleTupleFields.ApplicationRole_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleTupleFields.Group_), IntermediateFieldType.String),
            },
            typeof(ComPlusGroupInApplicationRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusGroupInApplicationRoleTupleFields
    {
        GroupInApplicationRole,
        ApplicationRole_,
        Component_,
        Group_,
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

        public string GroupInApplicationRole
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleTupleFields.GroupInApplicationRole].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleTupleFields.GroupInApplicationRole, value);
        }

        public string ApplicationRole_
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleTupleFields.ApplicationRole_].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleTupleFields.ApplicationRole_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleTupleFields.Component_, value);
        }

        public string Group_
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleTupleFields.Group_].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleTupleFields.Group_, value);
        }
    }
}