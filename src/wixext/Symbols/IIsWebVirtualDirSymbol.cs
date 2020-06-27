// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebVirtualDir = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebVirtualDir.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirSymbolFields.WebRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirSymbolFields.Alias), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirSymbolFields.DirPropertiesRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirSymbolFields.ApplicationRef), IntermediateFieldType.String),
            },
            typeof(IIsWebVirtualDirSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebVirtualDirSymbolFields
    {
        ComponentRef,
        WebRef,
        Alias,
        DirectoryRef,
        DirPropertiesRef,
        ApplicationRef,
    }

    public class IIsWebVirtualDirSymbol : IntermediateSymbol
    {
        public IIsWebVirtualDirSymbol() : base(IisSymbolDefinitions.IIsWebVirtualDir, null, null)
        {
        }

        public IIsWebVirtualDirSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebVirtualDir, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebVirtualDirSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)IIsWebVirtualDirSymbolFields.ComponentRef].AsString();
            set => this.Set((int)IIsWebVirtualDirSymbolFields.ComponentRef, value);
        }

        public string WebRef
        {
            get => this.Fields[(int)IIsWebVirtualDirSymbolFields.WebRef].AsString();
            set => this.Set((int)IIsWebVirtualDirSymbolFields.WebRef, value);
        }

        public string Alias
        {
            get => this.Fields[(int)IIsWebVirtualDirSymbolFields.Alias].AsString();
            set => this.Set((int)IIsWebVirtualDirSymbolFields.Alias, value);
        }

        public string DirectoryRef
        {
            get => this.Fields[(int)IIsWebVirtualDirSymbolFields.DirectoryRef].AsString();
            set => this.Set((int)IIsWebVirtualDirSymbolFields.DirectoryRef, value);
        }

        public string DirPropertiesRef
        {
            get => this.Fields[(int)IIsWebVirtualDirSymbolFields.DirPropertiesRef].AsString();
            set => this.Set((int)IIsWebVirtualDirSymbolFields.DirPropertiesRef, value);
        }

        public string ApplicationRef
        {
            get => this.Fields[(int)IIsWebVirtualDirSymbolFields.ApplicationRef].AsString();
            set => this.Set((int)IIsWebVirtualDirSymbolFields.ApplicationRef, value);
        }
    }
}