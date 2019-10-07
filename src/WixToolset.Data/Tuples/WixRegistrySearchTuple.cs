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
    using System;

    public enum WixRegistrySearchTupleFields
    {
        Root,
        Key,
        Value,
        Attributes,
    }

    [Flags]
    public enum WixRegistrySearchAttributes
    {
        Raw = 0x01,
        Compatible = 0x02,
        ExpandEnvironmentVariables = 0x04,
        WantValue = 0x08,
        WantExists = 0x10,
        Win64 = 0x20,
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

        public RegistryRootType Root
        {
            get => (RegistryRootType)this.Fields[(int)WixRegistrySearchTupleFields.Root].AsNumber();
            set => this.Set((int)WixRegistrySearchTupleFields.Root, (int)value);
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

        public WixRegistrySearchAttributes Attributes
        {
            get => (WixRegistrySearchAttributes)this.Fields[(int)WixRegistrySearchTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixRegistrySearchTupleFields.Attributes, (int)value);
        }
    }
}
