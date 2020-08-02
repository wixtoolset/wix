// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleVariable = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleVariable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleVariableSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleVariableSymbolFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleVariableSymbolFields.Hidden), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundleVariableSymbolFields.Persisted), IntermediateFieldType.Bool),
            },
            typeof(WixBundleVariableSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleVariableSymbolFields
    {
        Value,
        Type,
        Hidden,
        Persisted,
    }

    public enum WixBundleVariableType
    {
        Unknown,
        Formatted,
        Numeric,
        String,
        Version,
    }

    public class WixBundleVariableSymbol : IntermediateSymbol
    {
        public WixBundleVariableSymbol() : base(SymbolDefinitions.WixBundleVariable, null, null)
        {
        }

        public WixBundleVariableSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleVariable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleVariableSymbolFields index] => this.Fields[(int)index];

        public string Value
        {
            get => (string)this.Fields[(int)WixBundleVariableSymbolFields.Value];
            set => this.Set((int)WixBundleVariableSymbolFields.Value, value);
        }

        public WixBundleVariableType Type
        {
            get => Enum.TryParse((string)this.Fields[(int)WixBundleVariableSymbolFields.Type], true, out WixBundleVariableType value) ? value : WixBundleVariableType.Unknown;
            set => this.Set((int)WixBundleVariableSymbolFields.Type, value.ToString().ToLowerInvariant());
        }

        public bool Hidden
        {
            get => (bool)this.Fields[(int)WixBundleVariableSymbolFields.Hidden];
            set => this.Set((int)WixBundleVariableSymbolFields.Hidden, value);
        }

        public bool Persisted
        {
            get => (bool)this.Fields[(int)WixBundleVariableSymbolFields.Persisted];
            set => this.Set((int)WixBundleVariableSymbolFields.Persisted, value);
        }
    }
}
