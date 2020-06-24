// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixVariable = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixVariable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixVariableSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixVariableSymbolFields.Overridable), IntermediateFieldType.Bool),
            },
            typeof(WixVariableSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixVariableSymbolFields
    {
        Value,
        Overridable,
    }

    public class WixVariableSymbol : IntermediateSymbol
    {
        public WixVariableSymbol() : base(SymbolDefinitions.WixVariable, null, null)
        {
        }

        public WixVariableSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixVariable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixVariableSymbolFields index] => this.Fields[(int)index];

        public string Value
        {
            get => (string)this.Fields[(int)WixVariableSymbolFields.Value];
            set => this.Set((int)WixVariableSymbolFields.Value, value);
        }

        public bool Overridable
        {
            get => (bool)this.Fields[(int)WixVariableSymbolFields.Overridable];
            set => this.Set((int)WixVariableSymbolFields.Overridable, value);
        }
    }
}
