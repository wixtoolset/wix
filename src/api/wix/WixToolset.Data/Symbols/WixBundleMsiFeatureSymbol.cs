// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleMsiFeature = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleMsiFeature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.Size), IntermediateFieldType.LargeNumber),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.Title), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.Display), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.Level), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.Directory), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiFeatureSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixBundleMsiFeatureSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleMsiFeatureSymbolFields
    {
        PackageRef,
        Name,
        Size,
        Parent,
        Title,
        Description,
        Display,
        Level,
        Directory,
        Attributes,
    }

    public class WixBundleMsiFeatureSymbol : IntermediateSymbol
    {
        public WixBundleMsiFeatureSymbol() : base(SymbolDefinitions.WixBundleMsiFeature, null, null)
        {
        }

        public WixBundleMsiFeatureSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleMsiFeature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsiFeatureSymbolFields index] => this.Fields[(int)index];

        public string PackageRef
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureSymbolFields.PackageRef];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.PackageRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureSymbolFields.Name];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.Name, value);
        }

        public long Size
        {
            get => (long)this.Fields[(int)WixBundleMsiFeatureSymbolFields.Size];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.Size, value);
        }

        public string Parent
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureSymbolFields.Parent];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.Parent, value);
        }

        public string Title
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureSymbolFields.Title];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.Title, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureSymbolFields.Description];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.Description, value);
        }

        public int Display
        {
            get => (int)this.Fields[(int)WixBundleMsiFeatureSymbolFields.Display];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.Display, value);
        }

        public int Level
        {
            get => (int)this.Fields[(int)WixBundleMsiFeatureSymbolFields.Level];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.Level, value);
        }

        public string Directory
        {
            get => (string)this.Fields[(int)WixBundleMsiFeatureSymbolFields.Directory];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.Directory, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixBundleMsiFeatureSymbolFields.Attributes];
            set => this.Set((int)WixBundleMsiFeatureSymbolFields.Attributes, value);
        }
    }
}
