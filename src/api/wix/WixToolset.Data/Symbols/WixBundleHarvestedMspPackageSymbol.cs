// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleHarvestedMspPackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleHarvestedMspPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMspPackageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMspPackageSymbolFields.PatchCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMspPackageSymbolFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMspPackageSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMspPackageSymbolFields.ManufacturerName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMspPackageSymbolFields.PatchXml), IntermediateFieldType.String),
            },
            typeof(WixBundleHarvestedMspPackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleHarvestedMspPackageSymbolFields
    {
        Attributes,
        PatchCode,
        DisplayName,
        Description,
        ManufacturerName,
        PatchXml,
    }

    [Flags]
    public enum WixBundleHarvestedMspPackageAttributes
    {
        None = 0x0,
    }

    public class WixBundleHarvestedMspPackageSymbol : IntermediateSymbol
    {
        public WixBundleHarvestedMspPackageSymbol() : base(SymbolDefinitions.WixBundleHarvestedMspPackage, null, null)
        {
        }

        public WixBundleHarvestedMspPackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleHarvestedMspPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleHarvestedMspPackageSymbolFields index] => this.Fields[(int)index];

        public WixBundleHarvestedMspPackageAttributes Attributes
        {
            get => (WixBundleHarvestedMspPackageAttributes)this.Fields[(int)WixBundleHarvestedMspPackageSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleHarvestedMspPackageSymbolFields.Attributes, (int)value);
        }

        public string PatchCode
        {
            get => this.Fields[(int)WixBundleHarvestedMspPackageSymbolFields.PatchCode].AsString();
            set => this.Set((int)WixBundleHarvestedMspPackageSymbolFields.PatchCode, value);
        }

        public string DisplayName
        {
            get => this.Fields[(int)WixBundleHarvestedMspPackageSymbolFields.DisplayName].AsString();
            set => this.Set((int)WixBundleHarvestedMspPackageSymbolFields.DisplayName, value);
        }

        public string Description
        {
            get => this.Fields[(int)WixBundleHarvestedMspPackageSymbolFields.Description].AsString();
            set => this.Set((int)WixBundleHarvestedMspPackageSymbolFields.Description, value);
        }

        public string ManufacturerName
        {
            get => this.Fields[(int)WixBundleHarvestedMspPackageSymbolFields.ManufacturerName].AsString();
            set => this.Set((int)WixBundleHarvestedMspPackageSymbolFields.ManufacturerName, value);
        }

        public string PatchXml
        {
            get => this.Fields[(int)WixBundleHarvestedMspPackageSymbolFields.PatchXml].AsString();
            set => this.Set((int)WixBundleHarvestedMspPackageSymbolFields.PatchXml, value);
        }
    }
}
