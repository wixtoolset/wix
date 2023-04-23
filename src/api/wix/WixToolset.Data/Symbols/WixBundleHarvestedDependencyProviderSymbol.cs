// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleHarvestedDependencyProvider = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleHarvestedDependencyProvider,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedDependencyProviderSymbolFields.PackagePayloadRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedDependencyProviderSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedDependencyProviderSymbolFields.ProviderKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedDependencyProviderSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedDependencyProviderSymbolFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedDependencyProviderSymbolFields.ProviderAttributes), IntermediateFieldType.Number),
            },
            typeof(WixBundleHarvestedDependencyProviderSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;
    using WixToolset.Data;

    public enum WixBundleHarvestedDependencyProviderSymbolFields
    {
        PackagePayloadRef,
        Attributes,
        ProviderKey,
        Version,
        DisplayName,
        ProviderAttributes,
    }

    [Flags]
    public enum WixBundleHarvestedDependencyProviderAttributes
    {
        None = 0x0,
    }

    public class WixBundleHarvestedDependencyProviderSymbol : IntermediateSymbol
    {
        public WixBundleHarvestedDependencyProviderSymbol() : base(SymbolDefinitions.WixBundleHarvestedDependencyProvider, null, null)
        {
        }

        public WixBundleHarvestedDependencyProviderSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleHarvestedDependencyProvider, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleHarvestedDependencyProviderSymbolFields index] => this.Fields[(int)index];

        public string PackagePayloadRef
        {
            get => this.Fields[(int)WixBundleHarvestedDependencyProviderSymbolFields.PackagePayloadRef].AsString();
            set => this.Set((int)WixBundleHarvestedDependencyProviderSymbolFields.PackagePayloadRef, value);
        }

        public WixBundleHarvestedDependencyProviderAttributes Attributes
        {
            get => (WixBundleHarvestedDependencyProviderAttributes)this.Fields[(int)WixBundleHarvestedDependencyProviderSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleHarvestedDependencyProviderSymbolFields.Attributes, (int)value);
        }

        public string ProviderKey
        {
            get => this.Fields[(int)WixBundleHarvestedDependencyProviderSymbolFields.ProviderKey].AsString();
            set => this.Set((int)WixBundleHarvestedDependencyProviderSymbolFields.ProviderKey, value);
        }

        public string Version
        {
            get => this.Fields[(int)WixBundleHarvestedDependencyProviderSymbolFields.Version].AsString();
            set => this.Set((int)WixBundleHarvestedDependencyProviderSymbolFields.Version, value);
        }

        public string DisplayName
        {
            get => this.Fields[(int)WixBundleHarvestedDependencyProviderSymbolFields.DisplayName].AsString();
            set => this.Set((int)WixBundleHarvestedDependencyProviderSymbolFields.DisplayName, value);
        }

        public int ProviderAttributes
        {
            get => this.Fields[(int)WixBundleHarvestedDependencyProviderSymbolFields.ProviderAttributes].AsNumber();
            set => this.Set((int)WixBundleHarvestedDependencyProviderSymbolFields.ProviderAttributes, value);
        }
    }
}
