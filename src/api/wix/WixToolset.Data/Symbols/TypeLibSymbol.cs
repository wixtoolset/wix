// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition TypeLib = new IntermediateSymbolDefinition(
            SymbolDefinitionType.TypeLib,
            new[]
            {
                new IntermediateFieldDefinition(nameof(TypeLibSymbolFields.LibId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibSymbolFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TypeLibSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibSymbolFields.Version), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TypeLibSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibSymbolFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TypeLibSymbolFields.Cost), IntermediateFieldType.Number),
            },
            typeof(TypeLibSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum TypeLibSymbolFields
    {
        LibId,
        Language,
        ComponentRef,
        Version,
        Description,
        DirectoryRef,
        FeatureRef,
        Cost,
    }

    public class TypeLibSymbol : IntermediateSymbol
    {
        public TypeLibSymbol() : base(SymbolDefinitions.TypeLib, null, null)
        {
        }

        public TypeLibSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.TypeLib, sourceLineNumber, id)
        {
        }

        public IntermediateField this[TypeLibSymbolFields index] => this.Fields[(int)index];

        public string LibId
        {
            get => (string)this.Fields[(int)TypeLibSymbolFields.LibId];
            set => this.Set((int)TypeLibSymbolFields.LibId, value);
        }

        public int Language
        {
            get => (int)this.Fields[(int)TypeLibSymbolFields.Language];
            set => this.Set((int)TypeLibSymbolFields.Language, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)TypeLibSymbolFields.ComponentRef];
            set => this.Set((int)TypeLibSymbolFields.ComponentRef, value);
        }

        public int? Version
        {
            get => (int?)this.Fields[(int)TypeLibSymbolFields.Version];
            set => this.Set((int)TypeLibSymbolFields.Version, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)TypeLibSymbolFields.Description];
            set => this.Set((int)TypeLibSymbolFields.Description, value);
        }

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)TypeLibSymbolFields.DirectoryRef];
            set => this.Set((int)TypeLibSymbolFields.DirectoryRef, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)TypeLibSymbolFields.FeatureRef];
            set => this.Set((int)TypeLibSymbolFields.FeatureRef, value);
        }

        public int? Cost
        {
            get => (int?)this.Fields[(int)TypeLibSymbolFields.Cost];
            set => this.Set((int)TypeLibSymbolFields.Cost, value);
        }
    }
}
