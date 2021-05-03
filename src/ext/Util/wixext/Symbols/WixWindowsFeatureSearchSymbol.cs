// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixWindowsFeatureSearch = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.WixWindowsFeatureSearch.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixWindowsFeatureSearchSymbolFields.Type), IntermediateFieldType.String),
            },
            typeof(WixWindowsFeatureSearchSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum WixWindowsFeatureSearchSymbolFields
    {
        Type,
    }

    public class WixWindowsFeatureSearchSymbol : IntermediateSymbol
    {
        public WixWindowsFeatureSearchSymbol() : base(UtilSymbolDefinitions.WixWindowsFeatureSearch, null, null)
        {
        }

        public WixWindowsFeatureSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.WixWindowsFeatureSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixWindowsFeatureSearchSymbolFields index] => this.Fields[(int)index];

        public string Type
        {
            get => this.Fields[(int)WixWindowsFeatureSearchSymbolFields.Type].AsString();
            set => this.Set((int)WixWindowsFeatureSearchSymbolFields.Type, value);
        }
    }
}
