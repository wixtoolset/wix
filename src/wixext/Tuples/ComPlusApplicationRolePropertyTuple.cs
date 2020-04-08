// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusApplicationRoleProperty = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusApplicationRoleProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRolePropertyTupleFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRolePropertyTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRolePropertyTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusApplicationRolePropertyTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusApplicationRolePropertyTupleFields
    {
        ApplicationRoleRef,
        Name,
        Value,
    }

    public class ComPlusApplicationRolePropertyTuple : IntermediateTuple
    {
        public ComPlusApplicationRolePropertyTuple() : base(ComPlusTupleDefinitions.ComPlusApplicationRoleProperty, null, null)
        {
        }

        public ComPlusApplicationRolePropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusApplicationRoleProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusApplicationRolePropertyTupleFields index] => this.Fields[(int)index];

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusApplicationRolePropertyTupleFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusApplicationRolePropertyTupleFields.ApplicationRoleRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationRolePropertyTupleFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationRolePropertyTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusApplicationRolePropertyTupleFields.Value].AsString();
            set => this.Set((int)ComPlusApplicationRolePropertyTupleFields.Value, value);
        }
    }
}