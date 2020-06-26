// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition FileShare = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.FileShare.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileShareSymbolFields.ShareName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareSymbolFields.DirectoryRef), IntermediateFieldType.String),
            },
            typeof(FileShareSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum FileShareSymbolFields
    {
        ShareName,
        ComponentRef,
        Description,
        DirectoryRef,
    }

    public class FileShareSymbol : IntermediateSymbol
    {
        public FileShareSymbol() : base(UtilSymbolDefinitions.FileShare, null, null)
        {
        }

        public FileShareSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.FileShare, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileShareSymbolFields index] => this.Fields[(int)index];

        public string ShareName
        {
            get => this.Fields[(int)FileShareSymbolFields.ShareName].AsString();
            set => this.Set((int)FileShareSymbolFields.ShareName, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)FileShareSymbolFields.ComponentRef].AsString();
            set => this.Set((int)FileShareSymbolFields.ComponentRef, value);
        }

        public string Description
        {
            get => this.Fields[(int)FileShareSymbolFields.Description].AsString();
            set => this.Set((int)FileShareSymbolFields.Description, value);
        }

        public string DirectoryRef
        {
            get => this.Fields[(int)FileShareSymbolFields.DirectoryRef].AsString();
            set => this.Set((int)FileShareSymbolFields.DirectoryRef, value);
        }
    }
}