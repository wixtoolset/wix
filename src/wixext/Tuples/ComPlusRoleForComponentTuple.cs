// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusRoleForComponent = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusRoleForComponent.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentTupleFields.RoleForComponent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentTupleFields.ComPlusComponent_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentTupleFields.ApplicationRole_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(ComPlusRoleForComponentTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusRoleForComponentTupleFields
    {
        RoleForComponent,
        ComPlusComponent_,
        ApplicationRole_,
        Component_,
    }

    public class ComPlusRoleForComponentTuple : IntermediateTuple
    {
        public ComPlusRoleForComponentTuple() : base(ComPlusTupleDefinitions.ComPlusRoleForComponent, null, null)
        {
        }

        public ComPlusRoleForComponentTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusRoleForComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusRoleForComponentTupleFields index] => this.Fields[(int)index];

        public string RoleForComponent
        {
            get => this.Fields[(int)ComPlusRoleForComponentTupleFields.RoleForComponent].AsString();
            set => this.Set((int)ComPlusRoleForComponentTupleFields.RoleForComponent, value);
        }

        public string ComPlusComponent_
        {
            get => this.Fields[(int)ComPlusRoleForComponentTupleFields.ComPlusComponent_].AsString();
            set => this.Set((int)ComPlusRoleForComponentTupleFields.ComPlusComponent_, value);
        }

        public string ApplicationRole_
        {
            get => this.Fields[(int)ComPlusRoleForComponentTupleFields.ApplicationRole_].AsString();
            set => this.Set((int)ComPlusRoleForComponentTupleFields.ApplicationRole_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusRoleForComponentTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusRoleForComponentTupleFields.Component_, value);
        }
    }
}