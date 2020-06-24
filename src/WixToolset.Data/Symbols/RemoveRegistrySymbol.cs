// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition RemoveRegistry = new IntermediateSymbolDefinition(
            SymbolDefinitionType.RemoveRegistry,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RemoveRegistrySymbolFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RemoveRegistrySymbolFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveRegistrySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveRegistrySymbolFields.Action), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RemoveRegistrySymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(RemoveRegistrySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum RemoveRegistrySymbolFields
    {
        Root,
        Key,
        Name,
        Action,
        ComponentRef,
    }

    public enum RemoveRegistryActionType
    {
        RemoveOnInstall,
        RemoveOnUninstall
    };

    public class RemoveRegistrySymbol : IntermediateSymbol
    {
        public RemoveRegistrySymbol() : base(SymbolDefinitions.RemoveRegistry, null, null)
        {
        }

        public RemoveRegistrySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.RemoveRegistry, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RemoveRegistrySymbolFields index] => this.Fields[(int)index];

        public RegistryRootType Root
        {
            get => (RegistryRootType)this.Fields[(int)RemoveRegistrySymbolFields.Root].AsNumber();
            set => this.Set((int)RemoveRegistrySymbolFields.Root, (int)value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)RemoveRegistrySymbolFields.Key];
            set => this.Set((int)RemoveRegistrySymbolFields.Key, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)RemoveRegistrySymbolFields.Name];
            set => this.Set((int)RemoveRegistrySymbolFields.Name, value);
        }

        public RemoveRegistryActionType Action
        {
            get => (RemoveRegistryActionType)this.Fields[(int)RemoveRegistrySymbolFields.Action].AsNumber();
            set => this.Set((int)RemoveRegistrySymbolFields.Action, (int)value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)RemoveRegistrySymbolFields.ComponentRef];
            set => this.Set((int)RemoveRegistrySymbolFields.ComponentRef, value);
        }
    }
}