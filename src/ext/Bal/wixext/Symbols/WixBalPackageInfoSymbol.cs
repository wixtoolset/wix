// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplications
{
    using WixToolset.Data;
    using WixToolset.BootstrapperApplications.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBalPackageInfo = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixBalPackageInfo.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalPackageInfoSymbolFields.PackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBalPackageInfoSymbolFields.DisplayInternalUICondition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBalPackageInfoSymbolFields.DisplayFilesInUseDialogCondition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBalPackageInfoSymbolFields.PrimaryPackageType), IntermediateFieldType.Number),
            },
            typeof(WixBalPackageInfoSymbol));
    }
}

namespace WixToolset.BootstrapperApplications.Symbols
{
    using WixToolset.Data;

    public enum WixBalPackageInfoSymbolFields
    {
        PackageId,
        DisplayInternalUICondition,
        DisplayFilesInUseDialogCondition,
        PrimaryPackageType,
    }

    public enum BalPrimaryPackageType
    {
        None,
        Default,
        X86,
        X64,
        ARM64,
    }

    public class WixBalPackageInfoSymbol : IntermediateSymbol
    {
        public WixBalPackageInfoSymbol() : base(BalSymbolDefinitions.WixBalPackageInfo, null, null)
        {
        }

        public WixBalPackageInfoSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixBalPackageInfo, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalPackageInfoSymbolFields index] => this.Fields[(int)index];

        public string PackageId
        {
            get => this.Fields[(int)WixBalPackageInfoSymbolFields.PackageId].AsString();
            set => this.Set((int)WixBalPackageInfoSymbolFields.PackageId, value);
        }

        public string DisplayInternalUICondition
        {
            get => this.Fields[(int)WixBalPackageInfoSymbolFields.DisplayInternalUICondition].AsString();
            set => this.Set((int)WixBalPackageInfoSymbolFields.DisplayInternalUICondition, value);
        }

        public string DisplayFilesInUseDialogCondition
        {
            get => this.Fields[(int)WixBalPackageInfoSymbolFields.DisplayFilesInUseDialogCondition].AsString();
            set => this.Set((int)WixBalPackageInfoSymbolFields.DisplayFilesInUseDialogCondition, value);
        }

        public BalPrimaryPackageType PrimaryPackageType
        {
            get => (BalPrimaryPackageType)this.Fields[(int)WixBalPackageInfoSymbolFields.PrimaryPackageType].AsNumber();
            set => this.Set((int)WixBalPackageInfoSymbolFields.PrimaryPackageType, (int)value);
        }
    }
}
