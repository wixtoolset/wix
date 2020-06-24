// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleMsuPackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleMsuPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMsuPackageSymbolFields.DetectCondition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsuPackageSymbolFields.MsuKB), IntermediateFieldType.String),
            },
            typeof(WixBundleMsuPackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleMsuPackageSymbolFields
    {
        DetectCondition,
        MsuKB,
    }

    public class WixBundleMsuPackageSymbol : IntermediateSymbol
    {
        public WixBundleMsuPackageSymbol() : base(SymbolDefinitions.WixBundleMsuPackage, null, null)
        {
        }

        public WixBundleMsuPackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleMsuPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsuPackageSymbolFields index] => this.Fields[(int)index];

        public string DetectCondition
        {
            get => (string)this.Fields[(int)WixBundleMsuPackageSymbolFields.DetectCondition];
            set => this.Set((int)WixBundleMsuPackageSymbolFields.DetectCondition, value);
        }

        public string MsuKB
        {
            get => (string)this.Fields[(int)WixBundleMsuPackageSymbolFields.MsuKB];
            set => this.Set((int)WixBundleMsuPackageSymbolFields.MsuKB, value);
        }
    }
}