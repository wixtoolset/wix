// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleMsiPackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleMsiPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageSymbolFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageSymbolFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageSymbolFields.ProductVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageSymbolFields.ProductLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageSymbolFields.ProductName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageSymbolFields.Manufacturer), IntermediateFieldType.String),
            },
            typeof(WixBundleMsiPackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleMsiPackageSymbolFields
    {
        Attributes,
        ProductCode,
        UpgradeCode,
        ProductVersion,
        ProductLanguage,
        ProductName,
        Manufacturer,
    }

    [Flags]
    public enum WixBundleMsiPackageAttributes
    {
        None = 0x0,
        EnableFeatureSelection = 0x1,
        ForcePerMachine = 0x2,
    }

    public class WixBundleMsiPackageSymbol : IntermediateSymbol
    {
        public WixBundleMsiPackageSymbol() : base(SymbolDefinitions.WixBundleMsiPackage, null, null)
        {
        }

        public WixBundleMsiPackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleMsiPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsiPackageSymbolFields index] => this.Fields[(int)index];

        public WixBundleMsiPackageAttributes Attributes
        {
            get => (WixBundleMsiPackageAttributes)(int)this.Fields[(int)WixBundleMsiPackageSymbolFields.Attributes];
            set => this.Set((int)WixBundleMsiPackageSymbolFields.Attributes, (int)value);
        }

        public string ProductCode
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageSymbolFields.ProductCode];
            set => this.Set((int)WixBundleMsiPackageSymbolFields.ProductCode, value);
        }

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageSymbolFields.UpgradeCode];
            set => this.Set((int)WixBundleMsiPackageSymbolFields.UpgradeCode, value);
        }

        public string ProductVersion
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageSymbolFields.ProductVersion];
            set => this.Set((int)WixBundleMsiPackageSymbolFields.ProductVersion, value);
        }

        public int ProductLanguage
        {
            get => (int)this.Fields[(int)WixBundleMsiPackageSymbolFields.ProductLanguage];
            set => this.Set((int)WixBundleMsiPackageSymbolFields.ProductLanguage, value);
        }

        public string ProductName
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageSymbolFields.ProductName];
            set => this.Set((int)WixBundleMsiPackageSymbolFields.ProductName, value);
        }

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageSymbolFields.Manufacturer];
            set => this.Set((int)WixBundleMsiPackageSymbolFields.Manufacturer, value);
        }

        public bool EnableFeatureSelection
        {
            get { return this.Attributes.HasFlag(WixBundleMsiPackageAttributes.EnableFeatureSelection); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleMsiPackageAttributes.EnableFeatureSelection;
                }
                else
                {
                    this.Attributes &= ~WixBundleMsiPackageAttributes.EnableFeatureSelection;
                }
            }
        }

        public bool ForcePerMachine
        {
            get { return this.Attributes.HasFlag(WixBundleMsiPackageAttributes.ForcePerMachine); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleMsiPackageAttributes.ForcePerMachine;
                }
                else
                {
                    this.Attributes &= ~WixBundleMsiPackageAttributes.ForcePerMachine;
                }
            }
        }
    }
}
