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
                new IntermediateFieldDefinition(nameof(WixBundleVariableSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleVariableSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleVariableSymbolFields.Type), IntermediateFieldType.String),
            },
            typeof(WixBundleVariableSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleVariableSymbolFields
    {
        Attributes,
        Value,
        Type,
    }

    [Flags]
    public enum WixBundleVariableAttributes
    {
        None = 0x0,
        Hidden = 0x1,
        Persisted = 0x2,
        BuiltIn = 0x4,
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

        public WixBundleVariableAttributes Attributes
        {
            get => (WixBundleVariableAttributes)this.Fields[(int)WixBundleVariableSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleVariableSymbolFields.Attributes, (int)value);
        }

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
            get { return this.Attributes.HasFlag(WixBundleVariableAttributes.Hidden); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleVariableAttributes.Hidden;
                }
                else
                {
                    this.Attributes &= ~WixBundleVariableAttributes.Hidden;
                }
            }
        }

        public bool Persisted
        {
            get { return this.Attributes.HasFlag(WixBundleVariableAttributes.Persisted); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleVariableAttributes.Persisted;
                }
                else
                {
                    this.Attributes &= ~WixBundleVariableAttributes.Persisted;
                }
            }
        }

        public bool BuiltIn
        {
            get { return this.Attributes.HasFlag(WixBundleVariableAttributes.BuiltIn); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleVariableAttributes.BuiltIn;
                }
                else
                {
                    this.Attributes &= ~WixBundleVariableAttributes.BuiltIn;
                }
            }
        }
    }
}
