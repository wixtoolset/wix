// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleHarvestedBundlePackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleHarvestedBundlePackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedBundlePackageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedBundlePackageSymbolFields.BundleId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedBundlePackageSymbolFields.EngineVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedBundlePackageSymbolFields.ManifestNamespace), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedBundlePackageSymbolFields.ProtocolVersion), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedBundlePackageSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedBundlePackageSymbolFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleHarvestedBundlePackageSymbolFields.InstallSize), IntermediateFieldType.LargeNumber),
            },
            typeof(WixBundleHarvestedBundlePackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleHarvestedBundlePackageSymbolFields
    {
        Attributes,
        BundleId,
        EngineVersion,
        ManifestNamespace,
        ProtocolVersion,
        Version,
        DisplayName,
        InstallSize,
    }

    [Flags]
    public enum WixBundleHarvestedBundlePackageAttributes
    {
        None = 0x0,
        PerMachine = 0x1,
        Win64 = 0x2,
    }

    public class WixBundleHarvestedBundlePackageSymbol : IntermediateSymbol
    {
        public WixBundleHarvestedBundlePackageSymbol() : base(SymbolDefinitions.WixBundleHarvestedBundlePackage, null, null)
        {
        }

        public WixBundleHarvestedBundlePackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleHarvestedBundlePackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleHarvestedBundlePackageSymbolFields index] => this.Fields[(int)index];

        public WixBundleHarvestedBundlePackageAttributes Attributes
        {
            get => (WixBundleHarvestedBundlePackageAttributes)this.Fields[(int)WixBundleHarvestedBundlePackageSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleHarvestedBundlePackageSymbolFields.Attributes, (int)value);
        }

        public string BundleId
        {
            get => this.Fields[(int)WixBundleHarvestedBundlePackageSymbolFields.BundleId].AsString();
            set => this.Set((int)WixBundleHarvestedBundlePackageSymbolFields.BundleId, value);
        }

        public string EngineVersion
        {
            get => this.Fields[(int)WixBundleHarvestedBundlePackageSymbolFields.EngineVersion].AsString();
            set => this.Set((int)WixBundleHarvestedBundlePackageSymbolFields.EngineVersion, value);
        }

        public string ManifestNamespace
        {
            get => this.Fields[(int)WixBundleHarvestedBundlePackageSymbolFields.ManifestNamespace].AsString();
            set => this.Set((int)WixBundleHarvestedBundlePackageSymbolFields.ManifestNamespace, value);
        }

        public int ProtocolVersion
        {
            get => this.Fields[(int)WixBundleHarvestedBundlePackageSymbolFields.ProtocolVersion].AsNumber();
            set => this.Set((int)WixBundleHarvestedBundlePackageSymbolFields.ProtocolVersion, value);
        }

        public string Version
        {
            get => this.Fields[(int)WixBundleHarvestedBundlePackageSymbolFields.Version].AsString();
            set => this.Set((int)WixBundleHarvestedBundlePackageSymbolFields.Version, value);
        }

        public string DisplayName
        {
            get => this.Fields[(int)WixBundleHarvestedBundlePackageSymbolFields.DisplayName].AsString();
            set => this.Set((int)WixBundleHarvestedBundlePackageSymbolFields.DisplayName, value);
        }

        public long InstallSize
        {
            get => this.Fields[(int)WixBundleHarvestedBundlePackageSymbolFields.InstallSize].AsLargeNumber();
            set => this.Set((int)WixBundleHarvestedBundlePackageSymbolFields.InstallSize, value);
        }

        public bool PerMachine
        {
            get { return this.Attributes.HasFlag(WixBundleHarvestedBundlePackageAttributes.PerMachine); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleHarvestedBundlePackageAttributes.PerMachine;
                }
                else
                {
                    this.Attributes &= ~WixBundleHarvestedBundlePackageAttributes.PerMachine;
                }
            }
        }

        public bool Win64
        {
            get { return this.Attributes.HasFlag(WixBundleHarvestedBundlePackageAttributes.Win64); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleHarvestedBundlePackageAttributes.Win64;
                }
                else
                {
                    this.Attributes &= ~WixBundleHarvestedBundlePackageAttributes.Win64;
                }
            }
        }
    }
}
