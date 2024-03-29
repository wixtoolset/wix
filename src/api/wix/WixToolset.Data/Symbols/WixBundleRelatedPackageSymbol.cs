// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleRelatedPackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleRelatedPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageSymbolFields.PackagePayloadRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageSymbolFields.RelatedId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageSymbolFields.MinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageSymbolFields.MaxVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageSymbolFields.Languages), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRelatedPackageSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixBundleRelatedPackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleRelatedPackageSymbolFields
    {
        PackagePayloadRef,
        RelatedId,
        MinVersion,
        MaxVersion,
        Languages,
        Attributes,
    }

    [Flags]
    public enum WixBundleRelatedPackageAttributes
    {
        None = 0x0,
        OnlyDetect = 0x1,
        MinInclusive = 0x2,
        MaxInclusive = 0x4,
        LangInclusive = 0x8,
    }

    public class WixBundleRelatedPackageSymbol : IntermediateSymbol
    {
        public WixBundleRelatedPackageSymbol() : base(SymbolDefinitions.WixBundleRelatedPackage, null, null)
        {
        }

        public WixBundleRelatedPackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleRelatedPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleRelatedPackageSymbolFields index] => this.Fields[(int)index];

        public string PackagePayloadRef
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageSymbolFields.PackagePayloadRef];
            set => this.Set((int)WixBundleRelatedPackageSymbolFields.PackagePayloadRef, value);
        }

        public string RelatedId
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageSymbolFields.RelatedId];
            set => this.Set((int)WixBundleRelatedPackageSymbolFields.RelatedId, value);
        }

        public string MinVersion
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageSymbolFields.MinVersion];
            set => this.Set((int)WixBundleRelatedPackageSymbolFields.MinVersion, value);
        }

        public string MaxVersion
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageSymbolFields.MaxVersion];
            set => this.Set((int)WixBundleRelatedPackageSymbolFields.MaxVersion, value);
        }

        public string Languages
        {
            get => (string)this.Fields[(int)WixBundleRelatedPackageSymbolFields.Languages];
            set => this.Set((int)WixBundleRelatedPackageSymbolFields.Languages, value);
        }

        public WixBundleRelatedPackageAttributes Attributes
        {
            get => (WixBundleRelatedPackageAttributes)this.Fields[(int)WixBundleRelatedPackageSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleRelatedPackageSymbolFields.Attributes, (int)value);
        }

        public bool MinInclusive
        {
            get { return this.Attributes.HasFlag(WixBundleRelatedPackageAttributes.MinInclusive); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleRelatedPackageAttributes.MinInclusive;
                }
                else
                {
                    this.Attributes &= ~WixBundleRelatedPackageAttributes.MinInclusive;
                }
            }
        }

        public bool MaxInclusive
        {
            get { return this.Attributes.HasFlag(WixBundleRelatedPackageAttributes.MaxInclusive); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleRelatedPackageAttributes.MaxInclusive;
                }
                else
                {
                    this.Attributes &= ~WixBundleRelatedPackageAttributes.MaxInclusive;
                }
            }
        }

        public bool OnlyDetect
        {
            get { return this.Attributes.HasFlag(WixBundleRelatedPackageAttributes.OnlyDetect); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleRelatedPackageAttributes.OnlyDetect;
                }
                else
                {
                    this.Attributes &= ~WixBundleRelatedPackageAttributes.OnlyDetect;
                }
            }
        }

        public bool LangInclusive
        {
            get { return this.Attributes.HasFlag(WixBundleRelatedPackageAttributes.LangInclusive); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleRelatedPackageAttributes.LangInclusive;
                }
                else
                {
                    this.Attributes &= ~WixBundleRelatedPackageAttributes.LangInclusive;
                }
            }
        }
    }
}
