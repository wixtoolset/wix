// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Registry = new IntermediateTupleDefinition(
            TupleDefinitionType.Registry,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Registry), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(RegistryTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RegistryTupleFields
    {
        Registry,
        Root,
        Key,
        Name,
        Value,
        Component_,
    }

    public class RegistryTuple : IntermediateTuple
    {
        public RegistryTuple() : base(TupleDefinitions.Registry, null, null)
        {
        }

        public RegistryTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Registry, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RegistryTupleFields index] => this.Fields[(int)index];

        public string Registry
        {
            get => (string)this.Fields[(int)RegistryTupleFields.Registry]?.Value;
            set => this.Set((int)RegistryTupleFields.Registry, value);
        }

        public int Root
        {
            get => (int)this.Fields[(int)RegistryTupleFields.Root]?.Value;
            set => this.Set((int)RegistryTupleFields.Root, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)RegistryTupleFields.Key]?.Value;
            set => this.Set((int)RegistryTupleFields.Key, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)RegistryTupleFields.Name]?.Value;
            set => this.Set((int)RegistryTupleFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)RegistryTupleFields.Value]?.Value;
            set => this.Set((int)RegistryTupleFields.Value, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)RegistryTupleFields.Component_]?.Value;
            set => this.Set((int)RegistryTupleFields.Component_, value);
        }
    }
}