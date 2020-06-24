// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixFeatureModules = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixFeatureModules,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFeatureModulesSymbolFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFeatureModulesSymbolFields.WixMergeRef), IntermediateFieldType.String),
            },
            typeof(WixFeatureModulesSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixFeatureModulesSymbolFields
    {
        FeatureRef,
        WixMergeRef,
    }

    public class WixFeatureModulesSymbol : IntermediateSymbol
    {
        public WixFeatureModulesSymbol() : base(SymbolDefinitions.WixFeatureModules, null, null)
        {
        }

        public WixFeatureModulesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixFeatureModules, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFeatureModulesSymbolFields index] => this.Fields[(int)index];

        public string FeatureRef
        {
            get => (string)this.Fields[(int)WixFeatureModulesSymbolFields.FeatureRef];
            set => this.Set((int)WixFeatureModulesSymbolFields.FeatureRef, value);
        }

        public string WixMergeRef
        {
            get => (string)this.Fields[(int)WixFeatureModulesSymbolFields.WixMergeRef];
            set => this.Set((int)WixFeatureModulesSymbolFields.WixMergeRef, value);
        }
    }
}