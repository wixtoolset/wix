// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixComponentSearch = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixComponentSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixComponentSearchSymbolFields.Guid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComponentSearchSymbolFields.ProductCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixComponentSearchSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixComponentSearchSymbolFields.Type), IntermediateFieldType.Number),
            },
            typeof(WixComponentSearchSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixComponentSearchSymbolFields
    {
        Guid,
        ProductCode,
        Attributes,
        Type,
    }

    [Flags]
    public enum WixComponentSearchAttributes
    {
        None = 0x0,
    }

    public enum WixComponentSearchType
    {
        KeyPath,
        State,
        WantDirectory,
    }

    public class WixComponentSearchSymbol : IntermediateSymbol
    {
        public WixComponentSearchSymbol() : base(SymbolDefinitions.WixComponentSearch, null, null)
        {
        }

        public WixComponentSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixComponentSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixComponentSearchSymbolFields index] => this.Fields[(int)index];

        public string Guid
        {
            get => (string)this.Fields[(int)WixComponentSearchSymbolFields.Guid];
            set => this.Set((int)WixComponentSearchSymbolFields.Guid, value);
        }

        public string ProductCode
        {
            get => (string)this.Fields[(int)WixComponentSearchSymbolFields.ProductCode];
            set => this.Set((int)WixComponentSearchSymbolFields.ProductCode, value);
        }

        public WixComponentSearchAttributes Attributes
        {
            get => (WixComponentSearchAttributes)this.Fields[(int)WixComponentSearchSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixComponentSearchSymbolFields.Attributes, (int)value);
        }

        public WixComponentSearchType Type
        {
            get => (WixComponentSearchType)this.Fields[(int)WixComponentSearchSymbolFields.Type].AsNumber();
            set => this.Set((int)WixComponentSearchSymbolFields.Type, (int)value);
        }
    }
}
