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
                new IntermediateFieldDefinition(nameof(WixBundlePackageRelatedBundleSymbolFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageRelatedBundleSymbolFields.BundleId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageRelatedBundleSymbolFields.Action), IntermediateFieldType.Number),
            },
            typeof(WixBundlePackageRelatedBundleSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundlePackageRelatedBundleSymbolFields
    {
        PackageRef,
        BundleId,
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

        public string PackageRef
        {
            get => (string)this.Fields[(int)WixBundlePackageRelatedBundleSymbolFields.PackageRef];
            set => this.Set((int)WixBundlePackageRelatedBundleSymbolFields.PackageRef, value);
        }

        public string BundleId
        {
            get => (string)this.Fields[(int)WixBundlePackageRelatedBundleSymbolFields.BundleId];
            set => this.Set((int)WixBundlePackageRelatedBundleSymbolFields.BundleId, value);
        }

        public RelatedBundleActionType Action
        {
            get => (RelatedBundleActionType)this.Fields[(int)WixBundlePackageRelatedBundleSymbolFields.Action].AsNumber();
            set => this.Set((int)WixBundlePackageRelatedBundleSymbolFields.Action, (int)value);
        }
    }
}
