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
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentTupleFields.ComPlusComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentTupleFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(ComPlusRoleForComponentTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusRoleForComponentTupleFields
    {
        ComPlusComponentRef,
        ApplicationRoleRef,
        ComponentRef,
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

        public string ComPlusComponentRef
        {
            get => this.Fields[(int)ComPlusRoleForComponentTupleFields.ComPlusComponentRef].AsString();
            set => this.Set((int)ComPlusRoleForComponentTupleFields.ComPlusComponentRef, value);
        }

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusRoleForComponentTupleFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusRoleForComponentTupleFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusRoleForComponentTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusRoleForComponentTupleFields.ComponentRef, value);
        }
    }
}