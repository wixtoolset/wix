// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleUpdate = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleUpdate,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleUpdateSymbolFields.Location), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleUpdateSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixBundleUpdateSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleUpdateSymbolFields
    {
        Location,
        Attributes,
    }

    public class WixBundleUpdateSymbol : IntermediateSymbol
    {
        public WixBundleUpdateSymbol() : base(SymbolDefinitions.WixBundleUpdate, null, null)
        {
        }

        public WixBundleUpdateSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleUpdate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleUpdateSymbolFields index] => this.Fields[(int)index];

        public string Location
        {
            get => (string)this.Fields[(int)WixBundleUpdateSymbolFields.Location];
            set => this.Set((int)WixBundleUpdateSymbolFields.Location, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixBundleUpdateSymbolFields.Attributes];
            set => this.Set((int)WixBundleUpdateSymbolFields.Attributes, value);
        }
    }
}