// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixSearch = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSearchSymbolFields.Variable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSearchSymbolFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSearchSymbolFields.BundleExtensionRef), IntermediateFieldType.String),
            },
            typeof(WixSearchSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixSearchSymbolFields
    {
        Variable,
        Condition,
        BundleExtensionRef,
    }

    public class WixSearchSymbol : IntermediateSymbol
    {
        public WixSearchSymbol() : base(SymbolDefinitions.WixSearch, null, null)
        {
        }

        public WixSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSearchSymbolFields index] => this.Fields[(int)index];

        public string Variable
        {
            get => (string)this.Fields[(int)WixSearchSymbolFields.Variable];
            set => this.Set((int)WixSearchSymbolFields.Variable, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixSearchSymbolFields.Condition];
            set => this.Set((int)WixSearchSymbolFields.Condition, value);
        }

        public string BundleExtensionRef
        {
            get => (string)this.Fields[(int)WixSearchSymbolFields.BundleExtensionRef];
            set => this.Set((int)WixSearchSymbolFields.BundleExtensionRef, value);
        }
    }
}