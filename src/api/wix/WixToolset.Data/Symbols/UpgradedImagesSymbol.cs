// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition UpgradedImages = new IntermediateSymbolDefinition(
            SymbolDefinitionType.UpgradedImages,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UpgradedImagesSymbolFields.Upgraded), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedImagesSymbolFields.MsiPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedImagesSymbolFields.PatchMsiPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedImagesSymbolFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedImagesSymbolFields.Family), IntermediateFieldType.String),
            },
            typeof(UpgradedImagesSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum UpgradedImagesSymbolFields
    {
        Upgraded,
        MsiPath,
        PatchMsiPath,
        SymbolPaths,
        Family,
    }

    public class UpgradedImagesSymbol : IntermediateSymbol
    {
        public UpgradedImagesSymbol() : base(SymbolDefinitions.UpgradedImages, null, null)
        {
        }

        public UpgradedImagesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.UpgradedImages, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UpgradedImagesSymbolFields index] => this.Fields[(int)index];

        public string Upgraded
        {
            get => (string)this.Fields[(int)UpgradedImagesSymbolFields.Upgraded];
            set => this.Set((int)UpgradedImagesSymbolFields.Upgraded, value);
        }

        public string MsiPath
        {
            get => (string)this.Fields[(int)UpgradedImagesSymbolFields.MsiPath];
            set => this.Set((int)UpgradedImagesSymbolFields.MsiPath, value);
        }

        public string PatchMsiPath
        {
            get => (string)this.Fields[(int)UpgradedImagesSymbolFields.PatchMsiPath];
            set => this.Set((int)UpgradedImagesSymbolFields.PatchMsiPath, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)UpgradedImagesSymbolFields.SymbolPaths];
            set => this.Set((int)UpgradedImagesSymbolFields.SymbolPaths, value);
        }

        public string Family
        {
            get => (string)this.Fields[(int)UpgradedImagesSymbolFields.Family];
            set => this.Set((int)UpgradedImagesSymbolFields.Family, value);
        }
    }
}