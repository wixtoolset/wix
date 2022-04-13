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
                new IntermediateFieldDefinition(nameof(WixBundlePatchTargetCodeSymbolFields.Type), IntermediateFieldType.Number),
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
        Type,
    }

    [Flags]
    public enum WixBundlePatchTargetCodeAttributes : int
    {
        None = 0,
    }

    public enum WixBundlePatchTargetCodeType
    {
        /// <summary>
        /// The transform has no specific target.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The transform targets a specific ProductCode.
        /// </summary>
        ProductCode,

        /// <summary>
        /// The transform targets a specific UpgradeCode.
        /// </summary>
        UpgradeCode,
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

        public WixBundlePatchTargetCodeType Type
        {
            get => (WixBundlePatchTargetCodeType)this.Fields[(int)WixBundlePatchTargetCodeSymbolFields.Type].AsNumber();
            set => this.Set((int)WixBundlePatchTargetCodeSymbolFields.Type, (int)value);
        }
    }
}
