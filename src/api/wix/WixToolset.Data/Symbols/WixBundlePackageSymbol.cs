// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundlePackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundlePackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.PayloadRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.InstallCondition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.Cache), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.CacheId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.Vital), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.PerMachine), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.LogPathVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.RollbackLogPathVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.Size), IntermediateFieldType.LargeNumber),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.InstallSize), IntermediateFieldType.LargeNumber),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.RollbackBoundaryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.RollbackBoundaryBackwardRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageSymbolFields.Win64), IntermediateFieldType.Bool),
            },
            typeof(WixBundlePackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundlePackageSymbolFields
    {
        Type,
        PayloadRef,
        Attributes,
        InstallCondition,
        Cache,
        CacheId,
        Vital,
        PerMachine,
        LogPathVariable,
        RollbackLogPathVariable,
        Size,
        InstallSize,
        Version,
        Language,
        DisplayName,
        Description,
        RollbackBoundaryRef,
        RollbackBoundaryBackwardRef,
        Win64,
    }

    /// <summary>
    /// Types of bundle packages.
    /// </summary>
    public enum WixBundlePackageType
    {
        Exe,
        Msi,
        Msp,
        Msu,
    }

    [Flags]
    public enum WixBundlePackageAttributes
    {
        Permanent = 0x1,
        Visible = 0x2,
        PerMachine = 0x4,
        Win64 = 0x8,
    }

    public class WixBundlePackageSymbol : IntermediateSymbol
    {
        public WixBundlePackageSymbol() : base(SymbolDefinitions.WixBundlePackage, null, null)
        {
        }

        public WixBundlePackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundlePackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePackageSymbolFields index] => this.Fields[(int)index];

        public WixBundlePackageType Type
        {
            get => (WixBundlePackageType)Enum.Parse(typeof(WixBundlePackageType), (string)this.Fields[(int)WixBundlePackageSymbolFields.Type], true);
            set => this.Set((int)WixBundlePackageSymbolFields.Type, value.ToString());
        }

        public string PayloadRef
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.PayloadRef];
            set => this.Set((int)WixBundlePackageSymbolFields.PayloadRef, value);
        }

        public WixBundlePackageAttributes Attributes
        {
            get => (WixBundlePackageAttributes)(int)this.Fields[(int)WixBundlePackageSymbolFields.Attributes];
            set => this.Set((int)WixBundlePackageSymbolFields.Attributes, (int)value);
        }

        public string InstallCondition
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.InstallCondition];
            set => this.Set((int)WixBundlePackageSymbolFields.InstallCondition, value);
        }

        public YesNoAlwaysType Cache
        {
            get => Enum.TryParse((string)this.Fields[(int)WixBundlePackageSymbolFields.Cache], true, out YesNoAlwaysType value) ? value : YesNoAlwaysType.NotSet;
            set => this.Set((int)WixBundlePackageSymbolFields.Cache, value.ToString().ToLowerInvariant());
        }

        public string CacheId
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.CacheId];
            set => this.Set((int)WixBundlePackageSymbolFields.CacheId, value);
        }

        public bool? Vital
        {
            get => (bool?)this.Fields[(int)WixBundlePackageSymbolFields.Vital];
            set => this.Set((int)WixBundlePackageSymbolFields.Vital, value);
        }

        public YesNoDefaultType PerMachine
        {
            get => Enum.TryParse((string)this.Fields[(int)WixBundlePackageSymbolFields.PerMachine], true, out YesNoDefaultType value) ? value : YesNoDefaultType.NotSet;
            set => this.Set((int)WixBundlePackageSymbolFields.PerMachine, value.ToString().ToLowerInvariant());
        }

        public string LogPathVariable
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.LogPathVariable];
            set => this.Set((int)WixBundlePackageSymbolFields.LogPathVariable, value);
        }

        public string RollbackLogPathVariable
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.RollbackLogPathVariable];
            set => this.Set((int)WixBundlePackageSymbolFields.RollbackLogPathVariable, value);
        }

        public long Size
        {
            get => (long)this.Fields[(int)WixBundlePackageSymbolFields.Size];
            set => this.Set((int)WixBundlePackageSymbolFields.Size, value);
        }

        public long? InstallSize
        {
            get => (long?)this.Fields[(int)WixBundlePackageSymbolFields.InstallSize];
            set => this.Set((int)WixBundlePackageSymbolFields.InstallSize, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.Version];
            set => this.Set((int)WixBundlePackageSymbolFields.Version, value);
        }

        public int? Language
        {
            get => (int?)this.Fields[(int)WixBundlePackageSymbolFields.Language];
            set => this.Set((int)WixBundlePackageSymbolFields.Language, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.DisplayName];
            set => this.Set((int)WixBundlePackageSymbolFields.DisplayName, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.Description];
            set => this.Set((int)WixBundlePackageSymbolFields.Description, value);
        }

        public string RollbackBoundaryRef
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.RollbackBoundaryRef];
            set => this.Set((int)WixBundlePackageSymbolFields.RollbackBoundaryRef, value);
        }

        public string RollbackBoundaryBackwardRef
        {
            get => (string)this.Fields[(int)WixBundlePackageSymbolFields.RollbackBoundaryBackwardRef];
            set => this.Set((int)WixBundlePackageSymbolFields.RollbackBoundaryBackwardRef, value);
        }

        public bool Win64
        {
            get => (bool)this.Fields[(int)WixBundlePackageSymbolFields.Win64];
            set => this.Set((int)WixBundlePackageSymbolFields.Win64, value);
        }

        public bool Permanent => (this.Attributes & WixBundlePackageAttributes.Permanent) == WixBundlePackageAttributes.Permanent;
    }
}