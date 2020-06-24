// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Directory = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Directory,
            new[]
            {
                new IntermediateFieldDefinition(nameof(DirectorySymbolFields.ParentDirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectorySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectorySymbolFields.ShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectorySymbolFields.SourceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectorySymbolFields.SourceShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectorySymbolFields.ComponentGuidGenerationSeed), IntermediateFieldType.String),
            },
            typeof(DirectorySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum DirectorySymbolFields
    {
        ParentDirectoryRef,
        Name,
        ShortName,
        SourceName,
        SourceShortName,
        ComponentGuidGenerationSeed,
    }

    public class DirectorySymbol : IntermediateSymbol
    {
        public DirectorySymbol() : base(SymbolDefinitions.Directory, null, null)
        {
        }

        public DirectorySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Directory, sourceLineNumber, id)
        {
        }

        public IntermediateField this[DirectorySymbolFields index] => this.Fields[(int)index];

        public string ParentDirectoryRef
        {
            get => (string)this.Fields[(int)DirectorySymbolFields.ParentDirectoryRef];
            set => this.Set((int)DirectorySymbolFields.ParentDirectoryRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)DirectorySymbolFields.Name];
            set => this.Set((int)DirectorySymbolFields.Name, value);
        }

        public string ShortName
        {
            get => (string)this.Fields[(int)DirectorySymbolFields.ShortName];
            set => this.Set((int)DirectorySymbolFields.ShortName, value);
        }

        public string SourceName
        {
            get => (string)this.Fields[(int)DirectorySymbolFields.SourceName];
            set => this.Set((int)DirectorySymbolFields.SourceName, value);
        }

        public string SourceShortName
        {
            get => (string)this.Fields[(int)DirectorySymbolFields.SourceShortName];
            set => this.Set((int)DirectorySymbolFields.SourceShortName, value);
        }

        public string ComponentGuidGenerationSeed
        {
            get => (string)this.Fields[(int)DirectorySymbolFields.ComponentGuidGenerationSeed];
            set => this.Set((int)DirectorySymbolFields.ComponentGuidGenerationSeed, value);
        }
    }
}
