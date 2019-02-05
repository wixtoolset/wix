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
                new IntermediateFieldDefinition(nameof(ComPlusApplicationTupleFields.Application), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationTupleFields.Partition_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationTupleFields.CustomId), IntermediateFieldType.String),
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
        Application,
        Partition_,
        Component_,
        CustomId,
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

        public string Application
        {
            get => this.Fields[(int)ComPlusApplicationTupleFields.Application].AsString();
            set => this.Set((int)ComPlusApplicationTupleFields.Application, value);
        }

        public string Partition_
        {
            get => this.Fields[(int)ComPlusApplicationTupleFields.Partition_].AsString();
            set => this.Set((int)ComPlusApplicationTupleFields.Partition_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusApplicationTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusApplicationTupleFields.Component_, value);
        }

        public string CustomId
        {
            get => this.Fields[(int)ComPlusApplicationTupleFields.CustomId].AsString();
            set => this.Set((int)ComPlusApplicationTupleFields.CustomId, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationTupleFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationTupleFields.Name, value);
        }
    }
}