// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusRoleForInterface = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusRoleForInterface.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceTupleFields.RoleForInterface), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceTupleFields.Interface_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceTupleFields.ApplicationRole_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(ComPlusRoleForInterfaceTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusRoleForInterfaceTupleFields
    {
        RoleForInterface,
        Interface_,
        ApplicationRole_,
        Component_,
    }

    public class ComPlusRoleForInterfaceTuple : IntermediateTuple
    {
        public ComPlusRoleForInterfaceTuple() : base(ComPlusTupleDefinitions.ComPlusRoleForInterface, null, null)
        {
        }

        public ComPlusRoleForInterfaceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusRoleForInterface, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusRoleForInterfaceTupleFields index] => this.Fields[(int)index];

        public string RoleForInterface
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceTupleFields.RoleForInterface].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceTupleFields.RoleForInterface, value);
        }

        public string Interface_
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceTupleFields.Interface_].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceTupleFields.Interface_, value);
        }

        public string ApplicationRole_
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceTupleFields.ApplicationRole_].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceTupleFields.ApplicationRole_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceTupleFields.Component_, value);
        }
    }
}