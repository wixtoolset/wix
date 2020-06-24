// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleCustomDataAttribute = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleCustomDataAttribute,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataAttributeSymbolFields.CustomDataRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataAttributeSymbolFields.Name), IntermediateFieldType.String),
            },
            typeof(WixBundleCustomDataAttributeSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleCustomDataAttributeSymbolFields
    {
        CustomDataRef,
        Name,
    }

    public class WixBundleCustomDataAttributeSymbol : IntermediateSymbol
    {
        public WixBundleCustomDataAttributeSymbol() : base(SymbolDefinitions.WixBundleCustomDataAttribute, null, null)
        {
        }

        public WixBundleCustomDataAttributeSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleCustomDataAttribute, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleCustomDataAttributeSymbolFields index] => this.Fields[(int)index];

        public string CustomDataRef
        {
            get => (string)this.Fields[(int)WixBundleCustomDataAttributeSymbolFields.CustomDataRef];
            set => this.Set((int)WixBundleCustomDataAttributeSymbolFields.CustomDataRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleCustomDataAttributeSymbolFields.Name];
            set => this.Set((int)WixBundleCustomDataAttributeSymbolFields.Name, value);
        }
    }
}
