// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition CreateFolder = new IntermediateSymbolDefinition(
            SymbolDefinitionType.CreateFolder,
            new[]
            {
                new IntermediateFieldDefinition(nameof(CreateFolderSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CreateFolderSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(CreateFolderSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum CreateFolderSymbolFields
    {
        DirectoryRef,
        ComponentRef,
    }

    public class CreateFolderSymbol : IntermediateSymbol
    {
        public CreateFolderSymbol() : base(SymbolDefinitions.CreateFolder, null, null)
        {
        }

        public CreateFolderSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.CreateFolder, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CreateFolderSymbolFields index] => this.Fields[(int)index];

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)CreateFolderSymbolFields.DirectoryRef];
            set => this.Set((int)CreateFolderSymbolFields.DirectoryRef, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)CreateFolderSymbolFields.ComponentRef];
            set => this.Set((int)CreateFolderSymbolFields.ComponentRef, value);
        }
    }
}