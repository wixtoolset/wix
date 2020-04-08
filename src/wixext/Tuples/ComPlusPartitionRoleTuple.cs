// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusPartitionRole = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusPartitionRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleTupleFields.PartitionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleTupleFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionRoleTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusPartitionRoleTupleFields
    {
        PartitionRef,
        ComponentRef,
        Name,
    }

    public class ComPlusPartitionRoleTuple : IntermediateTuple
    {
        public ComPlusPartitionRoleTuple() : base(ComPlusTupleDefinitions.ComPlusPartitionRole, null, null)
        {
        }

        public ComPlusPartitionRoleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusPartitionRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusPartitionRoleTupleFields index] => this.Fields[(int)index];

        public string PartitionRef
        {
            get => this.Fields[(int)ComPlusPartitionRoleTupleFields.PartitionRef].AsString();
            set => this.Set((int)ComPlusPartitionRoleTupleFields.PartitionRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusPartitionRoleTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusPartitionRoleTupleFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusPartitionRoleTupleFields.Name].AsString();
            set => this.Set((int)ComPlusPartitionRoleTupleFields.Name, value);
        }
    }
}