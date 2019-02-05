// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusRoleForMethod = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusRoleForMethod.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodTupleFields.RoleForMethod), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodTupleFields.Method_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodTupleFields.ApplicationRole_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(ComPlusRoleForMethodTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusRoleForMethodTupleFields
    {
        RoleForMethod,
        Method_,
        ApplicationRole_,
        Component_,
    }

    public class ComPlusRoleForMethodTuple : IntermediateTuple
    {
        public ComPlusRoleForMethodTuple() : base(ComPlusTupleDefinitions.ComPlusRoleForMethod, null, null)
        {
        }

        public ComPlusRoleForMethodTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusRoleForMethod, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusRoleForMethodTupleFields index] => this.Fields[(int)index];

        public string RoleForMethod
        {
            get => this.Fields[(int)ComPlusRoleForMethodTupleFields.RoleForMethod].AsString();
            set => this.Set((int)ComPlusRoleForMethodTupleFields.RoleForMethod, value);
        }

        public string Method_
        {
            get => this.Fields[(int)ComPlusRoleForMethodTupleFields.Method_].AsString();
            set => this.Set((int)ComPlusRoleForMethodTupleFields.Method_, value);
        }

        public string ApplicationRole_
        {
            get => this.Fields[(int)ComPlusRoleForMethodTupleFields.ApplicationRole_].AsString();
            set => this.Set((int)ComPlusRoleForMethodTupleFields.ApplicationRole_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusRoleForMethodTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusRoleForMethodTupleFields.Component_, value);
        }
    }
}