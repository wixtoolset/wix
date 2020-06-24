// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundlePackageCommandLine = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundlePackageCommandLine,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineSymbolFields.WixBundlePackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineSymbolFields.InstallArgument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineSymbolFields.UninstallArgument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineSymbolFields.RepairArgument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageCommandLineSymbolFields.Condition), IntermediateFieldType.String),
            },
            typeof(WixBundlePackageCommandLineSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundlePackageCommandLineSymbolFields
    {
        WixBundlePackageRef,
        InstallArgument,
        UninstallArgument,
        RepairArgument,
        Condition,
    }

    public class WixBundlePackageCommandLineSymbol : IntermediateSymbol
    {
        public WixBundlePackageCommandLineSymbol() : base(SymbolDefinitions.WixBundlePackageCommandLine, null, null)
        {
        }

        public WixBundlePackageCommandLineSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundlePackageCommandLine, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePackageCommandLineSymbolFields index] => this.Fields[(int)index];

        public string WixBundlePackageRef
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineSymbolFields.WixBundlePackageRef];
            set => this.Set((int)WixBundlePackageCommandLineSymbolFields.WixBundlePackageRef, value);
        }

        public string InstallArgument
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineSymbolFields.InstallArgument];
            set => this.Set((int)WixBundlePackageCommandLineSymbolFields.InstallArgument, value);
        }

        public string UninstallArgument
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineSymbolFields.UninstallArgument];
            set => this.Set((int)WixBundlePackageCommandLineSymbolFields.UninstallArgument, value);
        }

        public string RepairArgument
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineSymbolFields.RepairArgument];
            set => this.Set((int)WixBundlePackageCommandLineSymbolFields.RepairArgument, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixBundlePackageCommandLineSymbolFields.Condition];
            set => this.Set((int)WixBundlePackageCommandLineSymbolFields.Condition, value);
        }
    }
}