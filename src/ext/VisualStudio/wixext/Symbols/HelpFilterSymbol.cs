// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Symbols;

    public static partial class VSSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition HelpFilter = new IntermediateSymbolDefinition(
            VSSymbolDefinitionType.HelpFilter.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpFilterSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFilterSymbolFields.QueryString), IntermediateFieldType.String),
            },
            typeof(HelpFilterSymbol));
    }
}

namespace WixToolset.VisualStudio.Symbols
{
    using WixToolset.Data;

    public enum HelpFilterSymbolFields
    {
        Description,
        QueryString,
    }

    public class HelpFilterSymbol : IntermediateSymbol
    {
        public HelpFilterSymbol() : base(VSSymbolDefinitions.HelpFilter, null, null)
        {
        }

        public HelpFilterSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSSymbolDefinitions.HelpFilter, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpFilterSymbolFields index] => this.Fields[(int)index];

        public string Description
        {
            get => this.Fields[(int)HelpFilterSymbolFields.Description].AsString();
            set => this.Set((int)HelpFilterSymbolFields.Description, value);
        }

        public string QueryString
        {
            get => this.Fields[(int)HelpFilterSymbolFields.QueryString].AsString();
            set => this.Set((int)HelpFilterSymbolFields.QueryString, value);
        }
    }
}