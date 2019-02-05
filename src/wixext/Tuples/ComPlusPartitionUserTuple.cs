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
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserTupleFields.PartitionUser), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserTupleFields.Partition_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserTupleFields.User_), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionUserTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusPartitionUserTupleFields
    {
        PartitionUser,
        Partition_,
        Component_,
        User_,
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

        public string PartitionUser
        {
            get => this.Fields[(int)ComPlusPartitionUserTupleFields.PartitionUser].AsString();
            set => this.Set((int)ComPlusPartitionUserTupleFields.PartitionUser, value);
        }

        public string Partition_
        {
            get => this.Fields[(int)ComPlusPartitionUserTupleFields.Partition_].AsString();
            set => this.Set((int)ComPlusPartitionUserTupleFields.Partition_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusPartitionUserTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusPartitionUserTupleFields.Component_, value);
        }

        public string User_
        {
            get => this.Fields[(int)ComPlusPartitionUserTupleFields.User_].AsString();
            set => this.Set((int)ComPlusPartitionUserTupleFields.User_, value);
        }
    }
}