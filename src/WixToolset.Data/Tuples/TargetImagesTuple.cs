// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition TargetImages = new IntermediateSymbolDefinition(
            SymbolDefinitionType.TargetImages,
            new[]
            {
                new IntermediateFieldDefinition(nameof(TargetImagesSymbolFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesSymbolFields.MsiPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesSymbolFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesSymbolFields.Upgraded), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesSymbolFields.Order), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TargetImagesSymbolFields.ProductValidateFlags), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesSymbolFields.IgnoreMissingSrcFiles), IntermediateFieldType.Bool),
            },
            typeof(TargetImagesSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum TargetImagesSymbolFields
    {
        Target,
        MsiPath,
        SymbolPaths,
        Upgraded,
        Order,
        ProductValidateFlags,
        IgnoreMissingSrcFiles,
    }

    public class TargetImagesSymbol : IntermediateSymbol
    {
        public TargetImagesSymbol() : base(SymbolDefinitions.TargetImages, null, null)
        {
        }

        public TargetImagesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.TargetImages, sourceLineNumber, id)
        {
        }

        public IntermediateField this[TargetImagesSymbolFields index] => this.Fields[(int)index];

        public string Target
        {
            get => (string)this.Fields[(int)TargetImagesSymbolFields.Target];
            set => this.Set((int)TargetImagesSymbolFields.Target, value);
        }

        public string MsiPath
        {
            get => (string)this.Fields[(int)TargetImagesSymbolFields.MsiPath];
            set => this.Set((int)TargetImagesSymbolFields.MsiPath, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)TargetImagesSymbolFields.SymbolPaths];
            set => this.Set((int)TargetImagesSymbolFields.SymbolPaths, value);
        }

        public string Upgraded
        {
            get => (string)this.Fields[(int)TargetImagesSymbolFields.Upgraded];
            set => this.Set((int)TargetImagesSymbolFields.Upgraded, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)TargetImagesSymbolFields.Order];
            set => this.Set((int)TargetImagesSymbolFields.Order, value);
        }

        public string ProductValidateFlags
        {
            get => (string)this.Fields[(int)TargetImagesSymbolFields.ProductValidateFlags];
            set => this.Set((int)TargetImagesSymbolFields.ProductValidateFlags, value);
        }

        public bool IgnoreMissingSrcFiles
        {
            get => (bool)this.Fields[(int)TargetImagesSymbolFields.IgnoreMissingSrcFiles];
            set => this.Set((int)TargetImagesSymbolFields.IgnoreMissingSrcFiles, value);
        }
    }
}