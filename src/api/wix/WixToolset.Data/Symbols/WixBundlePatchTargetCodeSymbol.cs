// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundlePatchTargetCode = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundlePatchTargetCode,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeSymbolFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeSymbolFields.TargetCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixBundlePatchTargetCodeSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundlePatchTargetCodeSymbolFields
    {
        PackageRef,
        TargetCode,
        Attributes,
    }

    [Flags]
    public enum WixBundlePatchTargetCodeAttributes : int
    {
        None = 0,

        /// <summary>
        /// The transform targets a specific ProductCode.
        /// </summary>
        TargetsProductCode = 1,

        /// <summary>
        /// The transform targets a specific UpgradeCode.
        /// </summary>
        TargetsUpgradeCode = 2,
    }

    public class WixBundlePatchTargetCodeSymbol : IntermediateSymbol
    {
        public WixBundlePatchTargetCodeSymbol() : base(SymbolDefinitions.WixBundlePatchTargetCode, null, null)
        {
        }

        public WixBundlePatchTargetCodeSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundlePatchTargetCode, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePatchTargetCodeSymbolFields index] => this.Fields[(int)index];

        public string PackageRef
        {
            get => (string)this.Fields[(int)WixBundlePatchTargetCodeSymbolFields.PackageRef];
            set => this.Set((int)WixBundlePatchTargetCodeSymbolFields.PackageRef, value);
        }

        public string TargetCode
        {
            get => (string)this.Fields[(int)WixBundlePatchTargetCodeSymbolFields.TargetCode];
            set => this.Set((int)WixBundlePatchTargetCodeSymbolFields.TargetCode, value);
        }

        public WixBundlePatchTargetCodeAttributes Attributes
        {
            get => (WixBundlePatchTargetCodeAttributes)this.Fields[(int)WixBundlePatchTargetCodeSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundlePatchTargetCodeSymbolFields.Attributes, (int)value);
        }

        public bool TargetsProductCode => (this.Attributes & WixBundlePatchTargetCodeAttributes.TargetsProductCode) == WixBundlePatchTargetCodeAttributes.TargetsProductCode;

        public bool TargetsUpgradeCode => (this.Attributes & WixBundlePatchTargetCodeAttributes.TargetsUpgradeCode) == WixBundlePatchTargetCodeAttributes.TargetsUpgradeCode;
    }
}
