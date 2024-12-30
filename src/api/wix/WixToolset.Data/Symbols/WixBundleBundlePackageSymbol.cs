// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleBundlePackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleBundlePackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleBundlePackageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleBundlePackageSymbolFields.BundleCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleBundlePackageSymbolFields.EngineVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleBundlePackageSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleBundlePackageSymbolFields.InstallCommand), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleBundlePackageSymbolFields.RepairCommand), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleBundlePackageSymbolFields.UninstallCommand), IntermediateFieldType.String),
            },
            typeof(WixBundleBundlePackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleBundlePackageSymbolFields
    {
        Attributes,
        BundleCode,
        [Obsolete("Use BundleCode instead.")]
        BundleId = BundleCode,
        EngineVersion,
        Version,
        InstallCommand,
        RepairCommand,
        UninstallCommand,
    }

    [Flags]
    public enum WixBundleBundlePackageAttributes
    {
        None = 0,
        SupportsBurnProtocol = 1,
    }

    public class WixBundleBundlePackageSymbol : IntermediateSymbol
    {
        public WixBundleBundlePackageSymbol() : base(SymbolDefinitions.WixBundleBundlePackage, null, null)
        {
        }

        public WixBundleBundlePackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleBundlePackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleBundlePackageSymbolFields index] => this.Fields[(int)index];

        public WixBundleBundlePackageAttributes Attributes
        {
            get => (WixBundleBundlePackageAttributes)(int)this.Fields[(int)WixBundleBundlePackageSymbolFields.Attributes];
            set => this.Set((int)WixBundleBundlePackageSymbolFields.Attributes, (int)value);
        }

        public string BundleCode
        {
            get => (string)this.Fields[(int)WixBundleBundlePackageSymbolFields.BundleCode];
            set => this.Set((int)WixBundleBundlePackageSymbolFields.BundleCode, value);
        }

        [Obsolete("Use BundleCode instead.")]
        public string BundleId
        {
            get => (string)this.Fields[(int)WixBundleBundlePackageSymbolFields.BundleCode];
            set => this.Set((int)WixBundleBundlePackageSymbolFields.BundleCode, value);
        }

        public string EngineVersion
        {
            get => (string)this.Fields[(int)WixBundleBundlePackageSymbolFields.EngineVersion];
            set => this.Set((int)WixBundleBundlePackageSymbolFields.EngineVersion, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixBundleBundlePackageSymbolFields.Version];
            set => this.Set((int)WixBundleBundlePackageSymbolFields.Version, value);
        }

        public string InstallCommand
        {
            get => (string)this.Fields[(int)WixBundleBundlePackageSymbolFields.InstallCommand];
            set => this.Set((int)WixBundleBundlePackageSymbolFields.InstallCommand, value);
        }

        public string RepairCommand
        {
            get => (string)this.Fields[(int)WixBundleBundlePackageSymbolFields.RepairCommand];
            set => this.Set((int)WixBundleBundlePackageSymbolFields.RepairCommand, value);
        }

        public string UninstallCommand
        {
            get => (string)this.Fields[(int)WixBundleBundlePackageSymbolFields.UninstallCommand];
            set => this.Set((int)WixBundleBundlePackageSymbolFields.UninstallCommand, value);
        }

        public bool SupportsBurnProtocol
        {
            get { return this.Attributes.HasFlag(WixBundleBundlePackageAttributes.SupportsBurnProtocol); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleBundlePackageAttributes.SupportsBurnProtocol;
                }
                else
                {
                    this.Attributes &= ~WixBundleBundlePackageAttributes.SupportsBurnProtocol;
                }
            }
        }
    }
}
