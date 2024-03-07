// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBootstrapperExtension = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBootstrapperExtension,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBootstrapperExtensionSymbolFields.PayloadRef), IntermediateFieldType.String),
            },
            typeof(WixBootstrapperExtensionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBootstrapperExtensionSymbolFields
    {
        PayloadRef,
    }

    public class WixBootstrapperExtensionSymbol : IntermediateSymbol
    {
        public WixBootstrapperExtensionSymbol() : base(SymbolDefinitions.WixBootstrapperExtension, null, null)
        {
        }

        public WixBootstrapperExtensionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBootstrapperExtension, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBootstrapperExtensionSymbolFields index] => this.Fields[(int)index];

        public string PayloadRef
        {
            get => (string)this.Fields[(int)WixBootstrapperExtensionSymbolFields.PayloadRef];
            set => this.Set((int)WixBootstrapperExtensionSymbolFields.PayloadRef, value);
        }
    }
}
