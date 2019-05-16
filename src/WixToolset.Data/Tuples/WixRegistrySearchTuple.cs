// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixRegistrySearch = new IntermediateTupleDefinition(
            TupleDefinitionType.WixRegistrySearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixRegistrySearchTupleFields.WixSearch_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRegistrySearchTupleFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixRegistrySearchTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRegistrySearchTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRegistrySearchTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixRegistrySearchTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixRegistrySearchTupleFields
    {
        WixSearch_,
        Root,
        Key,
        Value,
        Attributes,
    }

    public class WixRegistrySearchTuple : IntermediateTuple
    {
        public WixRegistrySearchTuple() : base(TupleDefinitions.WixRegistrySearch, null, null)
        {
        }

        public WixRegistrySearchTuple(SourceLineNumber sourceLineNumber , Identifier id = null) : base(TupleDefinitions.WixRegistrySearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixRegistrySearchTupleFields index] => this.Fields[(int)index];

        public string WixSearch_
        {
            get => (string)this.Fields[(int)WixRegistrySearchTupleFields.WixSearch_];
            set => this.Set((int)WixRegistrySearchTupleFields.WixSearch_, value);
        }

        public int Root
        {
            get => (int)this.Fields[(int)WixRegistrySearchTupleFields.Root];
            set => this.Set((int)WixRegistrySearchTupleFields.Root, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)WixRegistrySearchTupleFields.Key];
            set => this.Set((int)WixRegistrySearchTupleFields.Key, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)WixRegistrySearchTupleFields.Value];
            set => this.Set((int)WixRegistrySearchTupleFields.Value, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixRegistrySearchTupleFields.Attributes];
            set => this.Set((int)WixRegistrySearchTupleFields.Attributes, value);
        }
    }
}