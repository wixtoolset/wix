// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixInstanceComponent = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixInstanceComponent,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixInstanceComponentSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(WixInstanceComponentSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixInstanceComponentSymbolFields
    {
        ComponentRef,
    }

    public class WixInstanceComponentSymbol : IntermediateSymbol
    {
        public WixInstanceComponentSymbol() : base(SymbolDefinitions.WixInstanceComponent, null, null)
        {
        }

        public WixInstanceComponentSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixInstanceComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixInstanceComponentSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)WixInstanceComponentSymbolFields.ComponentRef];
            set => this.Set((int)WixInstanceComponentSymbolFields.ComponentRef, value);
        }
    }
}