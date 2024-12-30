// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixRelatedBundle = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixRelatedBundle,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixRelatedBundleSymbolFields.BundleCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRelatedBundleSymbolFields.Action), IntermediateFieldType.Number),
            },
            typeof(WixRelatedBundleSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixRelatedBundleSymbolFields
    {
        BundleCode,
        [Obsolete("Use BundleCode instead.")]
        BundleId = BundleCode,
        Action,
    }

    public enum RelatedBundleActionType
    {
        Detect,
        Upgrade,
        Addon,
        Patch
    }

    public class WixRelatedBundleSymbol : IntermediateSymbol
    {
        public WixRelatedBundleSymbol() : base(SymbolDefinitions.WixRelatedBundle, null, null)
        {
        }

        public WixRelatedBundleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixRelatedBundle, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixRelatedBundleSymbolFields index] => this.Fields[(int)index];

        public string BundleCode
        {
            get => (string)this.Fields[(int)WixRelatedBundleSymbolFields.BundleCode];
            set => this.Set((int)WixRelatedBundleSymbolFields.BundleCode, value);
        }

        [Obsolete("Use BundleCode instead.")]
        public string BundleId
        {
            get => (string)this.Fields[(int)WixRelatedBundleSymbolFields.BundleCode];
            set => this.Set((int)WixRelatedBundleSymbolFields.BundleCode, value);
        }

        public RelatedBundleActionType Action
        {
            get => (RelatedBundleActionType)this.Fields[(int)WixRelatedBundleSymbolFields.Action].AsNumber();
            set => this.Set((int)WixRelatedBundleSymbolFields.Action, (int)value);
        }
    }
}
