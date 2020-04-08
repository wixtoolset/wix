// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusApplication = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusApplication.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusApplicationTupleFields.PartitionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationTupleFields.ApplicationId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationTupleFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusApplicationTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusApplicationTupleFields
    {
        PartitionRef,
        ComponentRef,
        ApplicationId,
        Name,
    }

    public class ComPlusApplicationTuple : IntermediateTuple
    {
        public ComPlusApplicationTuple() : base(ComPlusTupleDefinitions.ComPlusApplication, null, null)
        {
        }

        public ComPlusApplicationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusApplication, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusApplicationTupleFields index] => this.Fields[(int)index];

        public string PartitionRef
        {
            get => this.Fields[(int)ComPlusApplicationTupleFields.PartitionRef].AsString();
            set => this.Set((int)ComPlusApplicationTupleFields.PartitionRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusApplicationTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusApplicationTupleFields.ComponentRef, value);
        }

        public string ApplicationId
        {
            get => this.Fields[(int)ComPlusApplicationTupleFields.ApplicationId].AsString();
            set => this.Set((int)ComPlusApplicationTupleFields.ApplicationId, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationTupleFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationTupleFields.Name, value);
        }
    }
}