// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPatchTarget = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixPatchTarget,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchTargetSymbolFields.ProductCode), IntermediateFieldType.String),
            },
            typeof(WixPatchTargetSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixPatchTargetSymbolFields
    {
        ProductCode,
    }

    public class WixPatchTargetSymbol : IntermediateSymbol
    {
        public WixPatchTargetSymbol() : base(SymbolDefinitions.WixPatchTarget, null, null)
        {
        }

        public WixPatchTargetSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixPatchTarget, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchTargetSymbolFields index] => this.Fields[(int)index];

        public string ProductCode
        {
            get => (string)this.Fields[(int)WixPatchTargetSymbolFields.ProductCode];
            set => this.Set((int)WixPatchTargetSymbolFields.ProductCode, value);
        }
    }
}