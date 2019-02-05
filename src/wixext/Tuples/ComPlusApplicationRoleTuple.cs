// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusApplicationRole = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusApplicationRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRoleTupleFields.ApplicationRole), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRoleTupleFields.Application_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRoleTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRoleTupleFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusApplicationRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusApplicationRoleTupleFields
    {
        ApplicationRole,
        Application_,
        Component_,
        Name,
    }

    public class ComPlusApplicationRoleTuple : IntermediateTuple
    {
        public ComPlusApplicationRoleTuple() : base(ComPlusTupleDefinitions.ComPlusApplicationRole, null, null)
        {
        }

        public ComPlusApplicationRoleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusApplicationRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusApplicationRoleTupleFields index] => this.Fields[(int)index];

        public string ApplicationRole
        {
            get => this.Fields[(int)ComPlusApplicationRoleTupleFields.ApplicationRole].AsString();
            set => this.Set((int)ComPlusApplicationRoleTupleFields.ApplicationRole, value);
        }

        public string Application_
        {
            get => this.Fields[(int)ComPlusApplicationRoleTupleFields.Application_].AsString();
            set => this.Set((int)ComPlusApplicationRoleTupleFields.Application_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusApplicationRoleTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusApplicationRoleTupleFields.Component_, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationRoleTupleFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationRoleTupleFields.Name, value);
        }
    }
}