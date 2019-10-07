// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleExePackage = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleExePackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleExePackageTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageTupleFields.DetectCondition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageTupleFields.InstallCommand), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageTupleFields.RepairCommand), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageTupleFields.UninstallCommand), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageTupleFields.ExeProtocol), IntermediateFieldType.String),
            },
            typeof(WixBundleExePackageTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundleExePackageTupleFields
    {
        Attributes,
        DetectCondition,
        InstallCommand,
        RepairCommand,
        UninstallCommand,
        ExeProtocol,
    }

    [Flags]
    public enum WixBundleExePackageAttributes
    {
        None = 0,
    }

    public class WixBundleExePackageTuple : IntermediateTuple
    {
        public WixBundleExePackageTuple() : base(TupleDefinitions.WixBundleExePackage, null, null)
        {
        }

        public WixBundleExePackageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleExePackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleExePackageTupleFields index] => this.Fields[(int)index];

        public WixBundleExePackageAttributes Attributes
        {
            get => (WixBundleExePackageAttributes)(int)this.Fields[(int)WixBundleExePackageTupleFields.Attributes];
            set => this.Set((int)WixBundleExePackageTupleFields.Attributes, (int)value);
        }

        public string DetectCondition
        {
            get => (string)this.Fields[(int)WixBundleExePackageTupleFields.DetectCondition];
            set => this.Set((int)WixBundleExePackageTupleFields.DetectCondition, value);
        }

        public string InstallCommand
        {
            get => (string)this.Fields[(int)WixBundleExePackageTupleFields.InstallCommand];
            set => this.Set((int)WixBundleExePackageTupleFields.InstallCommand, value);
        }

        public string RepairCommand
        {
            get => (string)this.Fields[(int)WixBundleExePackageTupleFields.RepairCommand];
            set => this.Set((int)WixBundleExePackageTupleFields.RepairCommand, value);
        }

        public string UninstallCommand
        {
            get => (string)this.Fields[(int)WixBundleExePackageTupleFields.UninstallCommand];
            set => this.Set((int)WixBundleExePackageTupleFields.UninstallCommand, value);
        }

        public string ExeProtocol
        {
            get => (string)this.Fields[(int)WixBundleExePackageTupleFields.ExeProtocol];
            set => this.Set((int)WixBundleExePackageTupleFields.ExeProtocol, value);
        }

        public bool Repairable => !String.IsNullOrEmpty(this.RepairCommand);
    }
}
