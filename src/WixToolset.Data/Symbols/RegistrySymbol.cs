// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Registry = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Registry,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RegistrySymbolFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegistrySymbolFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistrySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistrySymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistrySymbolFields.ValueType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegistrySymbolFields.ValueAction), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegistrySymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(RegistrySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum RegistrySymbolFields
    {
        Root,
        Key,
        Name,
        Value,
        ValueType,
        ValueAction,
        ComponentRef,
    }

    public enum RegistryValueType
    {
        String,
        Binary,
        Expandable,
        Integer,
        MultiString,
    }

    public enum RegistryValueActionType
    {
        Write,
        Append,
        Prepend,
    }

    public class RegistrySymbol : IntermediateSymbol
    {
        public RegistrySymbol() : base(SymbolDefinitions.Registry, null, null)
        {
        }

        public RegistrySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Registry, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RegistrySymbolFields index] => this.Fields[(int)index];

        public RegistryRootType Root
        {
            get => (RegistryRootType)this.Fields[(int)RegistrySymbolFields.Root].AsNumber();
            set => this.Set((int)RegistrySymbolFields.Root, (int)value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)RegistrySymbolFields.Key];
            set => this.Set((int)RegistrySymbolFields.Key, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)RegistrySymbolFields.Name];
            set => this.Set((int)RegistrySymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)RegistrySymbolFields.Value].AsString();
            set => this.Set((int)RegistrySymbolFields.Value, value);
        }

        public RegistryValueType ValueType
        {
            get => (RegistryValueType)this.Fields[(int)RegistrySymbolFields.ValueType].AsNumber();
            set => this.Set((int)RegistrySymbolFields.ValueType, (int)value);
        }

        public RegistryValueActionType ValueAction
        {
            get => (RegistryValueActionType)this.Fields[(int)RegistrySymbolFields.ValueAction].AsNumber();
            set => this.Set((int)RegistrySymbolFields.ValueAction, (int)value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)RegistrySymbolFields.ComponentRef];
            set => this.Set((int)RegistrySymbolFields.ComponentRef, value);
        }
    }
}