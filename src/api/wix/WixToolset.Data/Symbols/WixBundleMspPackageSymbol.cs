// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleMspPackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleMspPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMspPackageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMspPackageSymbolFields.PatchCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMspPackageSymbolFields.Manufacturer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMspPackageSymbolFields.PatchXml), IntermediateFieldType.String),
            },
            typeof(WixBundleMspPackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleMspPackageSymbolFields
    {
        Attributes,
        PatchCode,
        Manufacturer,
        PatchXml,
    }

    [Flags]
    public enum WixBundleMspPackageAttributes
    {
        None = 0x0,
        Slipstream = 0x1,
    }

    public class WixBundleMspPackageSymbol : IntermediateSymbol
    {
        public WixBundleMspPackageSymbol() : base(SymbolDefinitions.WixBundleMspPackage, null, null)
        {
        }

        public WixBundleMspPackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleMspPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMspPackageSymbolFields index] => this.Fields[(int)index];

        public WixBundleMspPackageAttributes Attributes
        {
            get => (WixBundleMspPackageAttributes)(int)this.Fields[(int)WixBundleMspPackageSymbolFields.Attributes];
            set => this.Set((int)WixBundleMspPackageSymbolFields.Attributes, (int)value);
        }

        public string PatchCode
        {
            get => (string)this.Fields[(int)WixBundleMspPackageSymbolFields.PatchCode];
            set => this.Set((int)WixBundleMspPackageSymbolFields.PatchCode, value);
        }

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixBundleMspPackageSymbolFields.Manufacturer];
            set => this.Set((int)WixBundleMspPackageSymbolFields.Manufacturer, value);
        }

        public string PatchXml
        {
            get => (string)this.Fields[(int)WixBundleMspPackageSymbolFields.PatchXml];
            set => this.Set((int)WixBundleMspPackageSymbolFields.PatchXml, value);
        }

        public bool Slipstream
        {
            get { return this.Attributes.HasFlag(WixBundleMspPackageAttributes.Slipstream); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleMspPackageAttributes.Slipstream;
                }
                else
                {
                    this.Attributes &= ~WixBundleMspPackageAttributes.Slipstream;
                }
            }
        }
    }
}
