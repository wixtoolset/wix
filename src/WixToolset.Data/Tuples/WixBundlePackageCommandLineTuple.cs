// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundlePackageCommandLine = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundlePackageCommandLine,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineTupleFields.WixBundlePackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineTupleFields.InstallArgument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineTupleFields.UninstallArgument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineTupleFields.RepairArgument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(WixBundlePackageCommandLineTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundlePackageCommandLineTupleFields
    {
        WixBundlePackageRef,
        InstallArgument,
        UninstallArgument,
        RepairArgument,
        Condition,
    }

    public class WixBundlePackageCommandLineTuple : IntermediateTuple
    {
        public WixBundlePackageCommandLineTuple() : base(TupleDefinitions.WixBundlePackageCommandLine, null, null)
        {
        }

        public WixBundlePackageCommandLineTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundlePackageCommandLine, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePackageCommandLineTupleFields index] => this.Fields[(int)index];

        public string WixBundlePackageRef
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineTupleFields.WixBundlePackageRef];
            set => this.Set((int)WixBundlePackageCommandLineTupleFields.WixBundlePackageRef, value);
        }

        public string InstallArgument
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineTupleFields.InstallArgument];
            set => this.Set((int)WixBundlePackageCommandLineTupleFields.InstallArgument, value);
        }

        public string UninstallArgument
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineTupleFields.UninstallArgument];
            set => this.Set((int)WixBundlePackageCommandLineTupleFields.UninstallArgument, value);
        }

        public string RepairArgument
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineTupleFields.RepairArgument];
            set => this.Set((int)WixBundlePackageCommandLineTupleFields.RepairArgument, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineTupleFields.Condition];
            set => this.Set((int)WixBundlePackageCommandLineTupleFields.Condition, value);
        }
    }
}