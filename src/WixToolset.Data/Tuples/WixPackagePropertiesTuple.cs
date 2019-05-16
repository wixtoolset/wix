// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixPackageProperties = new IntermediateTupleDefinition(
            TupleDefinitionType.WixPackageProperties,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.Package), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.Vital), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.DownloadSize), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.PackageSize), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.InstalledSize), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.PackageType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.Permanent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.LogPathVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.RollbackLogPathVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.Compressed), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.DisplayInternalUI), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.InstallCondition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackagePropertiesTupleFields.Cache), IntermediateFieldType.String),
            },
            typeof(WixPackagePropertiesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPackagePropertiesTupleFields
    {
        Package,
        Vital,
        DisplayName,
        Description,
        DownloadSize,
        PackageSize,
        InstalledSize,
        PackageType,
        Permanent,
        LogPathVariable,
        RollbackLogPathVariable,
        Compressed,
        DisplayInternalUI,
        ProductCode,
        UpgradeCode,
        Version,
        InstallCondition,
        Cache,
    }

    public class WixPackagePropertiesTuple : IntermediateTuple
    {
        public WixPackagePropertiesTuple() : base(TupleDefinitions.WixPackageProperties, null, null)
        {
        }

        public WixPackagePropertiesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixPackageProperties, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPackagePropertiesTupleFields index] => this.Fields[(int)index];

        public string Package
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.Package];
            set => this.Set((int)WixPackagePropertiesTupleFields.Package, value);
        }

        public string Vital
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.Vital];
            set => this.Set((int)WixPackagePropertiesTupleFields.Vital, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.DisplayName];
            set => this.Set((int)WixPackagePropertiesTupleFields.DisplayName, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.Description];
            set => this.Set((int)WixPackagePropertiesTupleFields.Description, value);
        }

        public string DownloadSize
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.DownloadSize];
            set => this.Set((int)WixPackagePropertiesTupleFields.DownloadSize, value);
        }

        public string PackageSize
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.PackageSize];
            set => this.Set((int)WixPackagePropertiesTupleFields.PackageSize, value);
        }

        public string InstalledSize
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.InstalledSize];
            set => this.Set((int)WixPackagePropertiesTupleFields.InstalledSize, value);
        }

        public string PackageType
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.PackageType];
            set => this.Set((int)WixPackagePropertiesTupleFields.PackageType, value);
        }

        public string Permanent
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.Permanent];
            set => this.Set((int)WixPackagePropertiesTupleFields.Permanent, value);
        }

        public string LogPathVariable
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.LogPathVariable];
            set => this.Set((int)WixPackagePropertiesTupleFields.LogPathVariable, value);
        }

        public string RollbackLogPathVariable
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.RollbackLogPathVariable];
            set => this.Set((int)WixPackagePropertiesTupleFields.RollbackLogPathVariable, value);
        }

        public string Compressed
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.Compressed];
            set => this.Set((int)WixPackagePropertiesTupleFields.Compressed, value);
        }

        public string DisplayInternalUI
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.DisplayInternalUI];
            set => this.Set((int)WixPackagePropertiesTupleFields.DisplayInternalUI, value);
        }

        public string ProductCode
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.ProductCode];
            set => this.Set((int)WixPackagePropertiesTupleFields.ProductCode, value);
        }

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.UpgradeCode];
            set => this.Set((int)WixPackagePropertiesTupleFields.UpgradeCode, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.Version];
            set => this.Set((int)WixPackagePropertiesTupleFields.Version, value);
        }

        public string InstallCondition
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.InstallCondition];
            set => this.Set((int)WixPackagePropertiesTupleFields.InstallCondition, value);
        }

        public string Cache
        {
            get => (string)this.Fields[(int)WixPackagePropertiesTupleFields.Cache];
            set => this.Set((int)WixPackagePropertiesTupleFields.Cache, value);
        }
    }
}