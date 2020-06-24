// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixRegistrySearch = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixRegistrySearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixRegistrySearchSymbolFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixRegistrySearchSymbolFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRegistrySearchSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRegistrySearchSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixRegistrySearchSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixRegistrySearchSymbolFields
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

    public class WixRegistrySearchSymbol : IntermediateSymbol
    {
        public WixRegistrySearchSymbol() : base(SymbolDefinitions.WixRegistrySearch, null, null)
        {
        }

        public WixRegistrySearchSymbol(SourceLineNumber sourceLineNumber , Identifier id = null) : base(SymbolDefinitions.WixRegistrySearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixRegistrySearchSymbolFields index] => this.Fields[(int)index];

        public RegistryRootType Root
        {
            get => (RegistryRootType)this.Fields[(int)WixRegistrySearchSymbolFields.Root].AsNumber();
            set => this.Set((int)WixRegistrySearchSymbolFields.Root, (int)value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)WixRegistrySearchSymbolFields.Key];
            set => this.Set((int)WixRegistrySearchSymbolFields.Key, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)WixRegistrySearchSymbolFields.Value];
            set => this.Set((int)WixRegistrySearchSymbolFields.Value, value);
        }

        public WixRegistrySearchAttributes Attributes
        {
            get => (WixRegistrySearchAttributes)this.Fields[(int)WixRegistrySearchSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixRegistrySearchSymbolFields.Attributes, (int)value);
        }
    }
}
