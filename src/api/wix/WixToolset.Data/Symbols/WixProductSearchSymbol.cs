// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixProductSearch = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixProductSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixProductSearchSymbolFields.Guid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixProductSearchSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixProductSearchSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixProductSearchSymbolFields
    {
        Guid,
        Attributes,
    }

    [Flags]
    public enum WixProductSearchAttributes
    {
        Version = 0x1,
        Language = 0x2,
        State = 0x4,
        Assignment = 0x8,
        UpgradeCode = 0x10,
    }

    public class WixProductSearchSymbol : IntermediateSymbol
    {
        public WixProductSearchSymbol() : base(SymbolDefinitions.WixProductSearch, null, null)
        {
        }

        public WixProductSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixProductSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixProductSearchSymbolFields index] => this.Fields[(int)index];

        public string Guid
        {
            get => (string)this.Fields[(int)WixProductSearchSymbolFields.Guid];
            set => this.Set((int)WixProductSearchSymbolFields.Guid, value);
        }

        public WixProductSearchAttributes Attributes
        {
            get => (WixProductSearchAttributes)this.Fields[(int)WixProductSearchSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixProductSearchSymbolFields.Attributes, (int)value);
        }
    }
}
