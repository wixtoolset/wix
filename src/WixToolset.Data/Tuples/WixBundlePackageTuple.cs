// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundlePackage = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundlePackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.PayloadRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.InstallCondition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.Cache), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.CacheId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.Vital), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.PerMachine), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.LogPathVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.RollbackLogPathVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.Size), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.InstallSize), IntermediateFieldType.LargeNumber),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.RollbackBoundaryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.RollbackBoundaryBackwardRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageTupleFields.Win64), IntermediateFieldType.Bool),
            },
            typeof(WixBundlePackageTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundlePackageTupleFields
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

    public class WixBundlePackageTuple : IntermediateTuple
    {
        public WixBundlePackageTuple() : base(TupleDefinitions.WixBundlePackage, null, null)
        {
        }

        public WixBundlePackageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundlePackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePackageTupleFields index] => this.Fields[(int)index];

        public WixBundlePackageType Type
        {
            get => (WixBundlePackageType)Enum.Parse(typeof(WixBundlePackageType), (string)this.Fields[(int)WixBundlePackageTupleFields.Type], true);
            set => this.Set((int)WixBundlePackageTupleFields.Type, value.ToString());
        }

        public string PayloadRef
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.PayloadRef];
            set => this.Set((int)WixBundlePackageTupleFields.PayloadRef, value);
        }

        public WixBundlePackageAttributes Attributes
        {
            get => (WixBundlePackageAttributes)(int)this.Fields[(int)WixBundlePackageTupleFields.Attributes];
            set => this.Set((int)WixBundlePackageTupleFields.Attributes, (int)value);
        }

        public string InstallCondition
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.InstallCondition];
            set => this.Set((int)WixBundlePackageTupleFields.InstallCondition, value);
        }

        public YesNoAlwaysType Cache
        {
            get => Enum.TryParse((string)this.Fields[(int)WixBundlePackageTupleFields.Cache], true, out YesNoAlwaysType value) ? value : YesNoAlwaysType.NotSet;
            set => this.Set((int)WixBundlePackageTupleFields.Cache, value.ToString().ToLowerInvariant());
        }

        public string CacheId
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.CacheId];
            set => this.Set((int)WixBundlePackageTupleFields.CacheId, value);
        }

        public bool? Vital
        {
            get => (bool?)this.Fields[(int)WixBundlePackageTupleFields.Vital];
            set => this.Set((int)WixBundlePackageTupleFields.Vital, value);
        }

        public YesNoDefaultType PerMachine
        {
            get => Enum.TryParse((string)this.Fields[(int)WixBundlePackageTupleFields.PerMachine], true, out YesNoDefaultType value) ? value : YesNoDefaultType.NotSet;
            set => this.Set((int)WixBundlePackageTupleFields.PerMachine, value.ToString().ToLowerInvariant());
        }

        public string LogPathVariable
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.LogPathVariable];
            set => this.Set((int)WixBundlePackageTupleFields.LogPathVariable, value);
        }

        public string RollbackLogPathVariable
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.RollbackLogPathVariable];
            set => this.Set((int)WixBundlePackageTupleFields.RollbackLogPathVariable, value);
        }

        public int Size
        {
            get => (int)this.Fields[(int)WixBundlePackageTupleFields.Size];
            set => this.Set((int)WixBundlePackageTupleFields.Size, value);
        }

        public long? InstallSize
        {
            get => (long?)this.Fields[(int)WixBundlePackageTupleFields.InstallSize];
            set => this.Set((int)WixBundlePackageTupleFields.InstallSize, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.Version];
            set => this.Set((int)WixBundlePackageTupleFields.Version, value);
        }

        public int Language
        {
            get => (int)this.Fields[(int)WixBundlePackageTupleFields.Language];
            set => this.Set((int)WixBundlePackageTupleFields.Language, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.DisplayName];
            set => this.Set((int)WixBundlePackageTupleFields.DisplayName, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.Description];
            set => this.Set((int)WixBundlePackageTupleFields.Description, value);
        }

        public string RollbackBoundaryRef
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.RollbackBoundaryRef];
            set => this.Set((int)WixBundlePackageTupleFields.RollbackBoundaryRef, value);
        }

        public string RollbackBoundaryBackwardRef
        {
            get => (string)this.Fields[(int)WixBundlePackageTupleFields.RollbackBoundaryBackwardRef];
            set => this.Set((int)WixBundlePackageTupleFields.RollbackBoundaryBackwardRef, value);
        }

        public bool Win64
        {
            get => (bool)this.Fields[(int)WixBundlePackageTupleFields.Win64];
            set => this.Set((int)WixBundlePackageTupleFields.Win64, value);
        }

        public bool Permanent => (this.Attributes & WixBundlePackageAttributes.Permanent) == WixBundlePackageAttributes.Permanent;
    }
}