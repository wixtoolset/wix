// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Symbols;

    public static partial class VSSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition HelpFilterToNamespace = new IntermediateSymbolDefinition(
            VSSymbolDefinitionType.HelpFilterToNamespace.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpFilterToNamespaceSymbolFields.HelpFilterRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFilterToNamespaceSymbolFields.HelpNamespaceRef), IntermediateFieldType.String),
            },
            typeof(HelpFilterToNamespaceSymbol));
    }
}

namespace WixToolset.VisualStudio.Symbols
{
    using WixToolset.Data;

    public enum HelpFilterToNamespaceSymbolFields
    {
        HelpFilterRef,
        HelpNamespaceRef,
    }

    public class HelpFilterToNamespaceSymbol : IntermediateSymbol
    {
        public HelpFilterToNamespaceSymbol() : base(VSSymbolDefinitions.HelpFilterToNamespace, null, null)
        {
        }

        public HelpFilterToNamespaceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSSymbolDefinitions.HelpFilterToNamespace, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpFilterToNamespaceSymbolFields index] => this.Fields[(int)index];

        public string HelpFilterRef
        {
            get => this.Fields[(int)HelpFilterToNamespaceSymbolFields.HelpFilterRef].AsString();
            set => this.Set((int)HelpFilterToNamespaceSymbolFields.HelpFilterRef, value);
        }

        public string HelpNamespaceRef
        {
            get => this.Fields[(int)HelpFilterToNamespaceSymbolFields.HelpNamespaceRef].AsString();
            set => this.Set((int)HelpFilterToNamespaceSymbolFields.HelpNamespaceRef, value);
        }
    }
}