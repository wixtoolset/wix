// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition DuplicateFile = new IntermediateSymbolDefinition(
            SymbolDefinitionType.DuplicateFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(DuplicateFileSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileSymbolFields.DestinationName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileSymbolFields.DestinationShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileSymbolFields.DestinationFolder), IntermediateFieldType.String),
            },
            typeof(DuplicateFileSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum DuplicateFileSymbolFields
    {
        ComponentRef,
        FileRef,
        DestinationName,
        DestinationShortName,
        DestinationFolder,
    }

    public class DuplicateFileSymbol : IntermediateSymbol
    {
        public DuplicateFileSymbol() : base(SymbolDefinitions.DuplicateFile, null, null)
        {
        }

        public DuplicateFileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.DuplicateFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[DuplicateFileSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)DuplicateFileSymbolFields.ComponentRef];
            set => this.Set((int)DuplicateFileSymbolFields.ComponentRef, value);
        }

        public string FileRef
        {
            get => (string)this.Fields[(int)DuplicateFileSymbolFields.FileRef];
            set => this.Set((int)DuplicateFileSymbolFields.FileRef, value);
        }

        public string DestinationName
        {
            get => (string)this.Fields[(int)DuplicateFileSymbolFields.DestinationName];
            set => this.Set((int)DuplicateFileSymbolFields.DestinationName, value);
        }

        public string DestinationShortName
        {
            get => (string)this.Fields[(int)DuplicateFileSymbolFields.DestinationShortName];
            set => this.Set((int)DuplicateFileSymbolFields.DestinationShortName, value);
        }

        public string DestinationFolder
        {
            get => (string)this.Fields[(int)DuplicateFileSymbolFields.DestinationFolder];
            set => this.Set((int)DuplicateFileSymbolFields.DestinationFolder, value);
        }
    }
}