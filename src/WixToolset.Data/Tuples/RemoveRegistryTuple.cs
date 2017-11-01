// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition RemoveRegistry = new IntermediateTupleDefinition(
            TupleDefinitionType.RemoveRegistry,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.RemoveRegistry), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(RemoveRegistryTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RemoveRegistryTupleFields
    {
        RemoveRegistry,
        Root,
        Key,
        Name,
        Component_,
    }

    public class RemoveRegistryTuple : IntermediateTuple
    {
        public RemoveRegistryTuple() : base(TupleDefinitions.RemoveRegistry, null, null)
        {
        }

        public RemoveRegistryTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.RemoveRegistry, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RemoveRegistryTupleFields index] => this.Fields[(int)index];

        public string RemoveRegistry
        {
            get => (string)this.Fields[(int)RemoveRegistryTupleFields.RemoveRegistry]?.Value;
            set => this.Set((int)RemoveRegistryTupleFields.RemoveRegistry, value);
        }

        public int Root
        {
            get => (int)this.Fields[(int)RemoveRegistryTupleFields.Root]?.Value;
            set => this.Set((int)RemoveRegistryTupleFields.Root, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)RemoveRegistryTupleFields.Key]?.Value;
            set => this.Set((int)RemoveRegistryTupleFields.Key, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)RemoveRegistryTupleFields.Name]?.Value;
            set => this.Set((int)RemoveRegistryTupleFields.Name, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)RemoveRegistryTupleFields.Component_]?.Value;
            set => this.Set((int)RemoveRegistryTupleFields.Component_, value);
        }
    }
}