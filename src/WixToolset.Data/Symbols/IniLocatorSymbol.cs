// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IniLocator = new IntermediateSymbolDefinition(
            SymbolDefinitionType.IniLocator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(IniLocatorSymbolFields.SignatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniLocatorSymbolFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniLocatorSymbolFields.Section), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniLocatorSymbolFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniLocatorSymbolFields.Field), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IniLocatorSymbolFields.Type), IntermediateFieldType.Number),
            },
            typeof(IniLocatorSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum IniLocatorSymbolFields
    {
        SignatureRef,
        FileName,
        Section,
        Key,
        Field,
        Type,
    }

    public class IniLocatorSymbol : IntermediateSymbol
    {
        public IniLocatorSymbol() : base(SymbolDefinitions.IniLocator, null, null)
        {
        }

        public IniLocatorSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.IniLocator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IniLocatorSymbolFields index] => this.Fields[(int)index];

        public string SignatureRef
        {
            get => (string)this.Fields[(int)IniLocatorSymbolFields.SignatureRef];
            set => this.Set((int)IniLocatorSymbolFields.SignatureRef, value);
        }

        public string FileName
        {
            get => (string)this.Fields[(int)IniLocatorSymbolFields.FileName];
            set => this.Set((int)IniLocatorSymbolFields.FileName, value);
        }

        public string Section
        {
            get => (string)this.Fields[(int)IniLocatorSymbolFields.Section];
            set => this.Set((int)IniLocatorSymbolFields.Section, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)IniLocatorSymbolFields.Key];
            set => this.Set((int)IniLocatorSymbolFields.Key, value);
        }

        public int? Field
        {
            get => (int?)this.Fields[(int)IniLocatorSymbolFields.Field];
            set => this.Set((int)IniLocatorSymbolFields.Field, value);
        }

        public int? Type
        {
            get => (int?)this.Fields[(int)IniLocatorSymbolFields.Type];
            set => this.Set((int)IniLocatorSymbolFields.Type, value);
        }
    }
}