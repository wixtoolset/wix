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
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRoleTupleFields.ApplicationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRoleTupleFields.ComponentRef), IntermediateFieldType.String),
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
        ApplicationRef,
        ComponentRef,
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

        public string ApplicationRef
        {
            get => this.Fields[(int)ComPlusApplicationRoleTupleFields.ApplicationRef].AsString();
            set => this.Set((int)ComPlusApplicationRoleTupleFields.ApplicationRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusApplicationRoleTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusApplicationRoleTupleFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationRoleTupleFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationRoleTupleFields.Name, value);
        }
    }
}