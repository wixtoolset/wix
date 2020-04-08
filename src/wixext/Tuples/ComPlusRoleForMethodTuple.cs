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
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodTupleFields.MethodRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodTupleFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(ComPlusRoleForMethodTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusRoleForMethodTupleFields
    {
        MethodRef,
        ApplicationRoleRef,
        ComponentRef,
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

        public string MethodRef
        {
            get => this.Fields[(int)ComPlusRoleForMethodTupleFields.MethodRef].AsString();
            set => this.Set((int)ComPlusRoleForMethodTupleFields.MethodRef, value);
        }

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusRoleForMethodTupleFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusRoleForMethodTupleFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusRoleForMethodTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusRoleForMethodTupleFields.ComponentRef, value);
        }
    }
}