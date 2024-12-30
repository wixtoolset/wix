// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundlePackageRelatedBundle = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundlePackageRelatedBundle,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePackageRelatedBundleSymbolFields.PackagePayloadRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageRelatedBundleSymbolFields.BundleCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageRelatedBundleSymbolFields.Action), IntermediateFieldType.Number),
            },
            typeof(WixBundlePackageRelatedBundleSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundlePackageRelatedBundleSymbolFields
    {
        PackagePayloadRef,
        BundleCode,
        [Obsolete("Use BundleCode instead.")]
        BundleId = BundleCode,
        Action,
    }

    public class WixBundlePackageRelatedBundleSymbol : IntermediateSymbol
    {
        public WixBundlePackageRelatedBundleSymbol() : base(SymbolDefinitions.WixBundlePackageRelatedBundle, null, null)
        {
        }

        public WixBundlePackageRelatedBundleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundlePackageRelatedBundle, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePackageRelatedBundleSymbolFields index] => this.Fields[(int)index];

        public string PackagePayloadRef
        {
            get => (string)this.Fields[(int)WixBundlePackageRelatedBundleSymbolFields.PackagePayloadRef];
            set => this.Set((int)WixBundlePackageRelatedBundleSymbolFields.PackagePayloadRef, value);
        }

        public string BundleCode
        {
            get => (string)this.Fields[(int)WixBundlePackageRelatedBundleSymbolFields.BundleCode];
            set => this.Set((int)WixBundlePackageRelatedBundleSymbolFields.BundleCode, value);
        }

        [Obsolete("Use BundleCode instead.")]
        public string BundleId
        {
            get => (string)this.Fields[(int)WixBundlePackageRelatedBundleSymbolFields.BundleCode];
            set => this.Set((int)WixBundlePackageRelatedBundleSymbolFields.BundleCode, value);
        }

        public RelatedBundleActionType Action
        {
            get => (RelatedBundleActionType)this.Fields[(int)WixBundlePackageRelatedBundleSymbolFields.Action].AsNumber();
            set => this.Set((int)WixBundlePackageRelatedBundleSymbolFields.Action, (int)value);
        }
    }
}
