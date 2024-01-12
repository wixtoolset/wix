// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBootstrapperApplication = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBootstrapperApplication,
            new IntermediateFieldDefinition[]
            {
                new IntermediateFieldDefinition(nameof(WixBootstrapperApplicationSymbolFields.ExePayloadRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBootstrapperApplicationSymbolFields.Secondary), IntermediateFieldType.Number),
            },
            typeof(WixBootstrapperApplicationSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBootstrapperApplicationSymbolFields
    {
        ExePayloadRef,
        Secondary
    }

    public class WixBootstrapperApplicationSymbol : IntermediateSymbol
    {
        public WixBootstrapperApplicationSymbol() : base(SymbolDefinitions.WixBootstrapperApplication, null, null)
        {
        }

        public WixBootstrapperApplicationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBootstrapperApplication, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBootstrapperApplicationSymbolFields index] => this.Fields[(int)index];

        public string ExePayloadRef
        {
            get => (string)this.Fields[(int)WixBootstrapperApplicationSymbolFields.ExePayloadRef];
            set => this.Set((int)WixBootstrapperApplicationSymbolFields.ExePayloadRef, value);
        }

        public bool? Secondary
        {
            get => (bool?)this.Fields[(int)WixBootstrapperApplicationSymbolFields.Secondary];
            set => this.Set((int)WixBootstrapperApplicationSymbolFields.Secondary, value);
        }
    }
}
