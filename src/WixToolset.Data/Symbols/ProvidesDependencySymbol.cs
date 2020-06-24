// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ProvidesDependency = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ProvidesDependency,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ProvidesDependencySymbolFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProvidesDependencySymbolFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProvidesDependencySymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProvidesDependencySymbolFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProvidesDependencySymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ProvidesDependencySymbolFields.Imported), IntermediateFieldType.Bool),
            },
            typeof(ProvidesDependencySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ProvidesDependencySymbolFields
    {
        PackageRef,
        Key,
        Version,
        DisplayName,
        Attributes,
        Imported,
    }

    public class ProvidesDependencySymbol : IntermediateSymbol
    {
        public ProvidesDependencySymbol() : base(SymbolDefinitions.ProvidesDependency, null, null)
        {
        }

        public ProvidesDependencySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ProvidesDependency, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ProvidesDependencySymbolFields index] => this.Fields[(int)index];

        public string PackageRef
        {
            get => (string)this.Fields[(int)ProvidesDependencySymbolFields.PackageRef];
            set => this.Set((int)ProvidesDependencySymbolFields.PackageRef, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)ProvidesDependencySymbolFields.Key];
            set => this.Set((int)ProvidesDependencySymbolFields.Key, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)ProvidesDependencySymbolFields.Version];
            set => this.Set((int)ProvidesDependencySymbolFields.Version, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)ProvidesDependencySymbolFields.DisplayName];
            set => this.Set((int)ProvidesDependencySymbolFields.DisplayName, value);
        }

        public int? Attributes
        {
            get => (int?)this.Fields[(int)ProvidesDependencySymbolFields.Attributes];
            set => this.Set((int)ProvidesDependencySymbolFields.Attributes, value);
        }

        public bool Imported
        {
            get => (bool)this.Fields[(int)ProvidesDependencySymbolFields.Imported];
            set => this.Set((int)ProvidesDependencySymbolFields.Imported, value);
        }
    }
}
