// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleHarvestedMsiPackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleHarvestedMsiPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.ProductName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.ArpComments), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.AllUsers), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.MsiInstallPerUser), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.MsiFastInstall), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.ArpSystemComponent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.Manufacturer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.ProductLanguage), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.ProductVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedMsiPackageSymbolFields.InstallSize), IntermediateFieldType.LargeNumber),
            },
            typeof(WixBundleHarvestedMsiPackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleHarvestedMsiPackageSymbolFields
    {
        Attributes,
        ProductName,
        ArpComments,
        AllUsers,
        MsiInstallPerUser,
        MsiFastInstall,
        ArpSystemComponent,
        ProductCode,
        UpgradeCode,
        Manufacturer,
        ProductLanguage,
        ProductVersion,
        InstallSize,
    }

    [Flags]
    public enum WixBundleHarvestedMsiPackageAttributes
    {
        None = 0x0,
        PerMachine = 0x01,
        Win64 = 0x2,
    }

    public class WixBundleHarvestedMsiPackageSymbol : IntermediateSymbol
    {
        public WixBundleHarvestedMsiPackageSymbol() : base(SymbolDefinitions.WixBundleHarvestedMsiPackage, null, null)
        {
        }

        public WixBundleHarvestedMsiPackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleHarvestedMsiPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleHarvestedMsiPackageSymbolFields index] => this.Fields[(int)index];

        public WixBundleHarvestedMsiPackageAttributes Attributes
        {
            get => (WixBundleHarvestedMsiPackageAttributes)this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.Attributes, (int)value);
        }

        public string ProductName
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.ProductName].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.ProductName, value);
        }

        public string ArpComments
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.ArpComments].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.ArpComments, value);
        }

        public string AllUsers
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.AllUsers].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.AllUsers, value);
        }

        public string MsiInstallPerUser
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.MsiInstallPerUser].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.MsiInstallPerUser, value);
        }

        public string MsiFastInstall
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.MsiFastInstall].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.MsiFastInstall, value);
        }

        public string ArpSystemComponent
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.ArpSystemComponent].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.ArpSystemComponent, value);
        }

        public string ProductCode
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.ProductCode].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.ProductCode, value);
        }

        public string UpgradeCode
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.UpgradeCode].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.UpgradeCode, value);
        }

        public string Manufacturer
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.Manufacturer].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.Manufacturer, value);
        }

        public string ProductLanguage
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.ProductLanguage].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.ProductLanguage, value);
        }

        public string ProductVersion
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.ProductVersion].AsString();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.ProductVersion, value);
        }

        public long InstallSize
        {
            get => this.Fields[(int)WixBundleHarvestedMsiPackageSymbolFields.InstallSize].AsLargeNumber();
            set => this.Set((int)WixBundleHarvestedMsiPackageSymbolFields.InstallSize, value);
        }

        public bool PerMachine
        {
            get { return this.Attributes.HasFlag(WixBundleHarvestedMsiPackageAttributes.PerMachine); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleHarvestedMsiPackageAttributes.PerMachine;
                }
                else
                {
                    this.Attributes &= ~WixBundleHarvestedMsiPackageAttributes.PerMachine;
                }
            }
        }

        public bool Win64
        {
            get { return this.Attributes.HasFlag(WixBundleHarvestedMsiPackageAttributes.Win64); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleHarvestedMsiPackageAttributes.Win64;
                }
                else
                {
                    this.Attributes &= ~WixBundleHarvestedMsiPackageAttributes.Win64;
                }
            }
        }
    }
}
