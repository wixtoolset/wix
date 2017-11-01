// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComponentOriginal = new IntermediateTupleDefinition(TupleDefinitionType.Component, new[]
        {
            new IntermediateFieldDefinition("Guid", IntermediateFieldType.String),
            new IntermediateFieldDefinition("Directory", IntermediateFieldType.String),
            new IntermediateFieldDefinition("Condition", IntermediateFieldType.String),
            new IntermediateFieldDefinition("KeyPath", IntermediateFieldType.String),
            new IntermediateFieldDefinition("LocalOnly", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("SourceOnly", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("Optional", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("RegistryKeyPath", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("SharedDllRefCount", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("Permanent", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("OdbcDataSource", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("Transitive", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("NeverOverwrite", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("x64", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("DisableRegistryReflection", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("UnisntallOnSupersedence", IntermediateFieldType.Bool),
            new IntermediateFieldDefinition("Shared", IntermediateFieldType.Bool),
        }, typeof(ComponentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public class ComponentTupleOriginal : IntermediateTuple
    {
        public ComponentTupleOriginal(IntermediateTupleDefinition definition) : base(definition, null, null)
        {
            if (definition != TupleDefinitions.ComponentOriginal) throw new ArgumentException(nameof(definition));
        }

        public string Guid
        {
            get => (string)this[0]?.Value;
            set => this.Set(0, value);
        }

        public string Directory
        {
            get => (string)this[1]?.Value;
            set => this.Set(1, value);
        }
    }
}
