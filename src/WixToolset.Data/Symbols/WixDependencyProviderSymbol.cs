// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixDependencyProvider = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixDependencyProvider,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDependencyProviderSymbolFields.ParentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderSymbolFields.ProviderKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderSymbolFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixDependencyProviderSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;
    using WixToolset.Data;

    public enum WixDependencyProviderSymbolFields
    {
        ParentRef,
        ProviderKey,
        Version,
        DisplayName,
        Attributes,
    }

    [Flags]
    public enum WixDependencyProviderAttributes
    {
        ProvidesAttributesBundle = 0x10000,
        ProvidesAttributesImported = 0x20000
    }

    public class WixDependencyProviderSymbol : IntermediateSymbol
    {
        public WixDependencyProviderSymbol() : base(SymbolDefinitions.WixDependencyProvider, null, null)
        {
        }

        public WixDependencyProviderSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixDependencyProvider, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDependencyProviderSymbolFields index] => this.Fields[(int)index];

        public string ParentRef
        {
            get => this.Fields[(int)WixDependencyProviderSymbolFields.ParentRef].AsString();
            set => this.Set((int)WixDependencyProviderSymbolFields.ParentRef, value);
        }

        public string ProviderKey
        {
            get => this.Fields[(int)WixDependencyProviderSymbolFields.ProviderKey].AsString();
            set => this.Set((int)WixDependencyProviderSymbolFields.ProviderKey, value);
        }

        public string Version
        {
            get => this.Fields[(int)WixDependencyProviderSymbolFields.Version].AsString();
            set => this.Set((int)WixDependencyProviderSymbolFields.Version, value);
        }

        public string DisplayName
        {
            get => this.Fields[(int)WixDependencyProviderSymbolFields.DisplayName].AsString();
            set => this.Set((int)WixDependencyProviderSymbolFields.DisplayName, value);
        }

        public WixDependencyProviderAttributes Attributes
        {
            get => (WixDependencyProviderAttributes)(int)this.Fields[(int)WixDependencyProviderSymbolFields.Attributes];
            set => this.Set((int)WixDependencyProviderSymbolFields.Attributes, (int)value);
        }

        public bool Bundle => (this.Attributes & WixDependencyProviderAttributes.ProvidesAttributesBundle) == WixDependencyProviderAttributes.ProvidesAttributesBundle;

        public bool Imported => (this.Attributes & WixDependencyProviderAttributes.ProvidesAttributesImported) == WixDependencyProviderAttributes.ProvidesAttributesImported;
    }
}
