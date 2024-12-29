// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.PackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.Language), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.Manufacturer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.Codepage), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.Scope), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPackageSymbolFields.UpgradeStrategy), IntermediateFieldType.Number),
            },
            typeof(WixPackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixPackageSymbolFields
    {
        PackageId,
        UpgradeCode,
        Name,
        Language,
        Version,
        Manufacturer,
        Attributes,
        Codepage,
        Scope,
        UpgradeStrategy,
    }

    [Flags]
    public enum WixPackageAttributes
    {
        None = 0x0,
    }

    public enum WixPackageScope
    {
        PerMachine,
        PerUser,
        PerUserOrMachine,
    }

    public enum WixPackageUpgradeStrategy
    {
        None,
        MajorUpgrade,
    }

    public class WixPackageSymbol : IntermediateSymbol
    {
        public WixPackageSymbol() : base(SymbolDefinitions.WixPackage, null, null)
        {
        }

        public WixPackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPackageSymbolFields index] => this.Fields[(int)index];

        public string PackageId
        {
            get => (string)this.Fields[(int)WixPackageSymbolFields.PackageId];
            set => this.Set((int)WixPackageSymbolFields.PackageId, value);
        }

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)WixPackageSymbolFields.UpgradeCode];
            set => this.Set((int)WixPackageSymbolFields.UpgradeCode, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixPackageSymbolFields.Name];
            set => this.Set((int)WixPackageSymbolFields.Name, value);
        }

        public string Language
        {
            get => (string)this.Fields[(int)WixPackageSymbolFields.Language];
            set => this.Set((int)WixPackageSymbolFields.Language, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixPackageSymbolFields.Version];
            set => this.Set((int)WixPackageSymbolFields.Version, value);
        }

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixPackageSymbolFields.Manufacturer];
            set => this.Set((int)WixPackageSymbolFields.Manufacturer, value);
        }

        public WixPackageAttributes Attributes
        {
            get => (WixPackageAttributes)this.Fields[(int)WixPackageSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixPackageSymbolFields.Attributes, (int)value);
        }

        public string Codepage
        {
            get => (string)this.Fields[(int)WixPackageSymbolFields.Codepage];
            set => this.Set((int)WixPackageSymbolFields.Codepage, value);
        }

        public WixPackageScope Scope
        {
            get => (WixPackageScope)this.Fields[(int)WixPackageSymbolFields.Scope].AsNumber();
            set => this.Set((int)WixPackageSymbolFields.Scope, (int)value);
        }

        public WixPackageUpgradeStrategy UpgradeStrategy
        {
            get => (WixPackageUpgradeStrategy)this.Fields[(int)WixPackageSymbolFields.UpgradeStrategy].AsNumber();
            set => this.Set((int)WixPackageSymbolFields.UpgradeStrategy, (int)value);
        }
    }
}
