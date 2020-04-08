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
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceTupleFields.InterfaceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceTupleFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(ComPlusRoleForInterfaceTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusRoleForInterfaceTupleFields
    {
        InterfaceRef,
        ApplicationRoleRef,
        ComponentRef,
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

        public string InterfaceRef
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceTupleFields.InterfaceRef].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceTupleFields.InterfaceRef, value);
        }

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceTupleFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceTupleFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceTupleFields.ComponentRef, value);
        }
    }
}