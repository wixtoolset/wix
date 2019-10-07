// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleMsiPackage = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleMsiPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageTupleFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageTupleFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageTupleFields.ProductVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageTupleFields.ProductLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageTupleFields.ProductName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPackageTupleFields.Manufacturer), IntermediateFieldType.String),
            },
            typeof(WixBundleMsiPackageTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundleMsiPackageTupleFields
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
        DisplayInternalUI = 0x1,
        EnableFeatureSelection = 0x4,
        ForcePerMachine = 0x2,
    }

    public class WixBundleMsiPackageTuple : IntermediateTuple
    {
        public WixBundleMsiPackageTuple() : base(TupleDefinitions.WixBundleMsiPackage, null, null)
        {
        }

        public WixBundleMsiPackageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleMsiPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsiPackageTupleFields index] => this.Fields[(int)index];

        public WixBundleMsiPackageAttributes Attributes
        {
            get => (WixBundleMsiPackageAttributes)(int)this.Fields[(int)WixBundleMsiPackageTupleFields.Attributes];
            set => this.Set((int)WixBundleMsiPackageTupleFields.Attributes, (int)value);
        }

        public string ProductCode
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageTupleFields.ProductCode];
            set => this.Set((int)WixBundleMsiPackageTupleFields.ProductCode, value);
        }

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageTupleFields.UpgradeCode];
            set => this.Set((int)WixBundleMsiPackageTupleFields.UpgradeCode, value);
        }

        public string ProductVersion
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageTupleFields.ProductVersion];
            set => this.Set((int)WixBundleMsiPackageTupleFields.ProductVersion, value);
        }

        public int ProductLanguage
        {
            get => (int)this.Fields[(int)WixBundleMsiPackageTupleFields.ProductLanguage];
            set => this.Set((int)WixBundleMsiPackageTupleFields.ProductLanguage, value);
        }

        public string ProductName
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageTupleFields.ProductName];
            set => this.Set((int)WixBundleMsiPackageTupleFields.ProductName, value);
        }

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixBundleMsiPackageTupleFields.Manufacturer];
            set => this.Set((int)WixBundleMsiPackageTupleFields.Manufacturer, value);
        }

        public bool DisplayInternalUI => (this.Attributes & WixBundleMsiPackageAttributes.DisplayInternalUI) == WixBundleMsiPackageAttributes.DisplayInternalUI;

        public bool EnableFeatureSelection => (this.Attributes & WixBundleMsiPackageAttributes.EnableFeatureSelection) == WixBundleMsiPackageAttributes.EnableFeatureSelection;

        public bool ForcePerMachine => (this.Attributes & WixBundleMsiPackageAttributes.ForcePerMachine) == WixBundleMsiPackageAttributes.ForcePerMachine;
    }
}