// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MoveFile = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MoveFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MoveFileSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileSymbolFields.SourceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileSymbolFields.DestName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileSymbolFields.SourceFolder), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileSymbolFields.DestFolder), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileSymbolFields.Delete), IntermediateFieldType.Bool),
            },
            typeof(MoveFileSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MoveFileSymbolFields
    {
        ComponentRef,
        SourceName,
        DestName,
        SourceFolder,
        DestFolder,
        Delete,
    }

    public class MoveFileSymbol : IntermediateSymbol
    {
        public MoveFileSymbol() : base(SymbolDefinitions.MoveFile, null, null)
        {
        }

        public MoveFileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MoveFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MoveFileSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)MoveFileSymbolFields.ComponentRef];
            set => this.Set((int)MoveFileSymbolFields.ComponentRef, value);
        }

        public string SourceName
        {
            get => (string)this.Fields[(int)MoveFileSymbolFields.SourceName];
            set => this.Set((int)MoveFileSymbolFields.SourceName, value);
        }

        public string DestName
        {
            get => (string)this.Fields[(int)MoveFileSymbolFields.DestName];
            set => this.Set((int)MoveFileSymbolFields.DestName, value);
        }

        public string SourceFolder
        {
            get => (string)this.Fields[(int)MoveFileSymbolFields.SourceFolder];
            set => this.Set((int)MoveFileSymbolFields.SourceFolder, value);
        }

        public string DestFolder
        {
            get => (string)this.Fields[(int)MoveFileSymbolFields.DestFolder];
            set => this.Set((int)MoveFileSymbolFields.DestFolder, value);
        }

        public bool Delete
        {
            get => (bool)this.Fields[(int)MoveFileSymbolFields.Delete];
            set => this.Set((int)MoveFileSymbolFields.Delete, value);
        }
    }
}
