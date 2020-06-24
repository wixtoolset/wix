// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IniFile = new IntermediateSymbolDefinition(
            SymbolDefinitionType.IniFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(IniFileSymbolFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileSymbolFields.DirProperty), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileSymbolFields.Section), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileSymbolFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileSymbolFields.Action), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IniFileSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(IniFileSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum IniFileSymbolFields
    {
        FileName,
        DirProperty,
        Section,
        Key,
        Value,
        Action,
        ComponentRef,
    }

    public class IniFileSymbol : IntermediateSymbol
    {
        public IniFileSymbol() : base(SymbolDefinitions.IniFile, null, null)
        {
        }

        public IniFileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.IniFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IniFileSymbolFields index] => this.Fields[(int)index];

        public string FileName
        {
            get => (string)this.Fields[(int)IniFileSymbolFields.FileName];
            set => this.Set((int)IniFileSymbolFields.FileName, value);
        }

        public string DirProperty
        {
            get => (string)this.Fields[(int)IniFileSymbolFields.DirProperty];
            set => this.Set((int)IniFileSymbolFields.DirProperty, value);
        }

        public string Section
        {
            get => (string)this.Fields[(int)IniFileSymbolFields.Section];
            set => this.Set((int)IniFileSymbolFields.Section, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)IniFileSymbolFields.Key];
            set => this.Set((int)IniFileSymbolFields.Key, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)IniFileSymbolFields.Value];
            set => this.Set((int)IniFileSymbolFields.Value, value);
        }

        public InifFileActionType Action
        {
            get => (InifFileActionType)this.Fields[(int)IniFileSymbolFields.Action]?.AsNumber();
            set => this.Set((int)IniFileSymbolFields.Action, (int)value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)IniFileSymbolFields.ComponentRef];
            set => this.Set((int)IniFileSymbolFields.ComponentRef, value);
        }
    }
}