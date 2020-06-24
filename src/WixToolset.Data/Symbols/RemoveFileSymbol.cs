// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition RemoveFile = new IntermediateSymbolDefinition(
            SymbolDefinitionType.RemoveFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RemoveFileSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileSymbolFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileSymbolFields.DirProperty), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileSymbolFields.OnInstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(RemoveFileSymbolFields.OnUninstall), IntermediateFieldType.Bool),
            },
            typeof(RemoveFileSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum RemoveFileSymbolFields
    {
        ComponentRef,
        FileName,
        DirProperty,
        OnInstall,
        OnUninstall,
    }

    public class RemoveFileSymbol : IntermediateSymbol
    {
        public RemoveFileSymbol() : base(SymbolDefinitions.RemoveFile, null, null)
        {
        }

        public RemoveFileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.RemoveFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RemoveFileSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)RemoveFileSymbolFields.ComponentRef];
            set => this.Set((int)RemoveFileSymbolFields.ComponentRef, value);
        }

        public string FileName
        {
            get => (string)this.Fields[(int)RemoveFileSymbolFields.FileName];
            set => this.Set((int)RemoveFileSymbolFields.FileName, value);
        }

        public string DirProperty
        {
            get => (string)this.Fields[(int)RemoveFileSymbolFields.DirProperty];
            set => this.Set((int)RemoveFileSymbolFields.DirProperty, value);
        }

        public bool? OnInstall
        {
            get => (bool?)this.Fields[(int)RemoveFileSymbolFields.OnInstall];
            set => this.Set((int)RemoveFileSymbolFields.OnInstall, value);
        }

        public bool? OnUninstall
        {
            get => (bool?)this.Fields[(int)RemoveFileSymbolFields.OnUninstall];
            set => this.Set((int)RemoveFileSymbolFields.OnUninstall, value);
        }
    }
}