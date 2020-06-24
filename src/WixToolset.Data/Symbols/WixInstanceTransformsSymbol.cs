// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixInstanceTransforms = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixInstanceTransforms,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixInstanceTransformsSymbolFields.PropertyId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInstanceTransformsSymbolFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInstanceTransformsSymbolFields.ProductName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInstanceTransformsSymbolFields.UpgradeCode), IntermediateFieldType.String),
            },
            typeof(WixInstanceTransformsSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixInstanceTransformsSymbolFields
    {
        PropertyId,
        ProductCode,
        ProductName,
        UpgradeCode,
    }

    public class WixInstanceTransformsSymbol : IntermediateSymbol
    {
        public WixInstanceTransformsSymbol() : base(SymbolDefinitions.WixInstanceTransforms, null, null)
        {
        }

        public WixInstanceTransformsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixInstanceTransforms, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixInstanceTransformsSymbolFields index] => this.Fields[(int)index];

        public string PropertyId
        {
            get => (string)this.Fields[(int)WixInstanceTransformsSymbolFields.PropertyId];
            set => this.Set((int)WixInstanceTransformsSymbolFields.PropertyId, value);
        }

        public string ProductCode
        {
            get => (string)this.Fields[(int)WixInstanceTransformsSymbolFields.ProductCode];
            set => this.Set((int)WixInstanceTransformsSymbolFields.ProductCode, value);
        }

        public string ProductName
        {
            get => (string)this.Fields[(int)WixInstanceTransformsSymbolFields.ProductName];
            set => this.Set((int)WixInstanceTransformsSymbolFields.ProductName, value);
        }

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)WixInstanceTransformsSymbolFields.UpgradeCode];
            set => this.Set((int)WixInstanceTransformsSymbolFields.UpgradeCode, value);
        }
    }
}
