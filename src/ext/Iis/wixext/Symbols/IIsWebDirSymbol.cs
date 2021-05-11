// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebDir = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebDir.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebDirSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirSymbolFields.WebRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirSymbolFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirSymbolFields.DirPropertiesRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirSymbolFields.ApplicationRef), IntermediateFieldType.String),
            },
            typeof(IIsWebDirSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebDirSymbolFields
    {
        ComponentRef,
        WebRef,
        Path,
        DirPropertiesRef,
        ApplicationRef,
    }

    public class IIsWebDirSymbol : IntermediateSymbol
    {
        public IIsWebDirSymbol() : base(IisSymbolDefinitions.IIsWebDir, null, null)
        {
        }

        public IIsWebDirSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebDir, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebDirSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)IIsWebDirSymbolFields.ComponentRef].AsString();
            set => this.Set((int)IIsWebDirSymbolFields.ComponentRef, value);
        }

        public string WebRef
        {
            get => this.Fields[(int)IIsWebDirSymbolFields.WebRef].AsString();
            set => this.Set((int)IIsWebDirSymbolFields.WebRef, value);
        }

        public string Path
        {
            get => this.Fields[(int)IIsWebDirSymbolFields.Path].AsString();
            set => this.Set((int)IIsWebDirSymbolFields.Path, value);
        }

        public string DirPropertiesRef
        {
            get => this.Fields[(int)IIsWebDirSymbolFields.DirPropertiesRef].AsString();
            set => this.Set((int)IIsWebDirSymbolFields.DirPropertiesRef, value);
        }

        public string ApplicationRef
        {
            get => this.Fields[(int)IIsWebDirSymbolFields.ApplicationRef].AsString();
            set => this.Set((int)IIsWebDirSymbolFields.ApplicationRef, value);
        }
    }
}