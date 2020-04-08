// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusPartitionUser = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusPartitionUser.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserTupleFields.PartitionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserTupleFields.UserRef), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionUserTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusPartitionUserTupleFields
    {
        PartitionRef,
        ComponentRef,
        UserRef,
    }

    public class ComPlusPartitionUserTuple : IntermediateTuple
    {
        public ComPlusPartitionUserTuple() : base(ComPlusTupleDefinitions.ComPlusPartitionUser, null, null)
        {
        }

        public ComPlusPartitionUserTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusPartitionUser, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusPartitionUserTupleFields index] => this.Fields[(int)index];

        public string PartitionRef
        {
            get => this.Fields[(int)ComPlusPartitionUserTupleFields.PartitionRef].AsString();
            set => this.Set((int)ComPlusPartitionUserTupleFields.PartitionRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusPartitionUserTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusPartitionUserTupleFields.ComponentRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)ComPlusPartitionUserTupleFields.UserRef].AsString();
            set => this.Set((int)ComPlusPartitionUserTupleFields.UserRef, value);
        }
    }
}