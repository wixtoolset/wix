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
                new IntermediateFieldDefinition(nameof(WixRegistrySearchSymbolFields.Type), IntermediateFieldType.Number),
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
        Type,
    }

    [Flags]
    public enum WixRegistrySearchAttributes
    {
        None = 0x0,
        ExpandEnvironmentVariables = 0x01,
        Win64 = 0x2,
    }

    public enum WixRegistrySearchType
    {
        Value,
        Exists,
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

        public WixRegistrySearchType Type
        {
            get => (WixRegistrySearchType)this.Fields[(int)WixRegistrySearchSymbolFields.Type].AsNumber();
            set => this.Set((int)WixRegistrySearchSymbolFields.Type, (int)value);
        }

        public bool ExpandEnvironmentVariables
        {
            get { return this.Attributes.HasFlag(WixRegistrySearchAttributes.ExpandEnvironmentVariables); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixRegistrySearchAttributes.ExpandEnvironmentVariables;
                }
                else
                {
                    this.Attributes &= ~WixRegistrySearchAttributes.ExpandEnvironmentVariables;
                }
            }
        }

        public bool Win64
        {
            get { return this.Attributes.HasFlag(WixRegistrySearchAttributes.Win64); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixRegistrySearchAttributes.Win64;
                }
                else
                {
                    this.Attributes &= ~WixRegistrySearchAttributes.Win64;
                }
            }
        }
    }
}
