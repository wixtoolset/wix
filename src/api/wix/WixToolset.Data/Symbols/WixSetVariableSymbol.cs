// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixSetVariable = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixSetVariable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSetVariableSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSetVariableSymbolFields.Type), IntermediateFieldType.String),
            },
            typeof(WixSetVariableSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixSetVariableSymbolFields
    {
        Value,
        Type,
    }

    public class WixSetVariableSymbol : IntermediateSymbol
    {
        public WixSetVariableSymbol() : base(SymbolDefinitions.WixSetVariable, null, null)
        {
        }

        public WixSetVariableSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixSetVariable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSetVariableSymbolFields index] => this.Fields[(int)index];

        public string Value
        {
            get => (string)this.Fields[(int)WixSetVariableSymbolFields.Value];
            set => this.Set((int)WixSetVariableSymbolFields.Value, value);
        }

        public WixBundleVariableType Type
        {
            get => Enum.TryParse((string)this.Fields[(int)WixSetVariableSymbolFields.Type], true, out WixBundleVariableType value) ? value : WixBundleVariableType.Unknown;
            set => this.Set((int)WixSetVariableSymbolFields.Type, value.ToString().ToLowerInvariant());
        }
    }
}
