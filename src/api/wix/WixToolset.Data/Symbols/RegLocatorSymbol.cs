// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition RegLocator = new IntermediateSymbolDefinition(
            SymbolDefinitionType.RegLocator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RegLocatorSymbolFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegLocatorSymbolFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegLocatorSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegLocatorSymbolFields.Type), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegLocatorSymbolFields.Win64), IntermediateFieldType.Bool),
            },
            typeof(RegLocatorSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum RegLocatorSymbolFields
    {
        Root,
        Key,
        Name,
        Type,
        Win64,
    }

    public enum RegLocatorType
    {
        Directory,
        FileName,
        Raw
    };

    public class RegLocatorSymbol : IntermediateSymbol
    {
        public RegLocatorSymbol() : base(SymbolDefinitions.RegLocator, null, null)
        {
        }

        public RegLocatorSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.RegLocator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RegLocatorSymbolFields index] => this.Fields[(int)index];

        public RegistryRootType Root
        {
            get => (RegistryRootType)this.Fields[(int)RegLocatorSymbolFields.Root].AsNumber();
            set => this.Set((int)RegLocatorSymbolFields.Root, (int)value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)RegLocatorSymbolFields.Key];
            set => this.Set((int)RegLocatorSymbolFields.Key, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)RegLocatorSymbolFields.Name];
            set => this.Set((int)RegLocatorSymbolFields.Name, value);
        }

        public RegLocatorType Type
        {
            get => (RegLocatorType)this.Fields[(int)RegLocatorSymbolFields.Type].AsNumber();
            set => this.Set((int)RegLocatorSymbolFields.Type, (int)value);
        }

        public bool Win64
        {
            get => this.Fields[(int)RegLocatorSymbolFields.Win64].AsBool();
            set => this.Set((int)RegLocatorSymbolFields.Win64, value);
        }
    }
}
