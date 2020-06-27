// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Symbols;

    public static partial class VSSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition HelpFileToNamespace = new IntermediateSymbolDefinition(
            VSSymbolDefinitionType.HelpFileToNamespace.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpFileToNamespaceSymbolFields.HelpFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileToNamespaceSymbolFields.HelpNamespaceRef), IntermediateFieldType.String),
            },
            typeof(HelpFileToNamespaceSymbol));
    }
}

namespace WixToolset.VisualStudio.Symbols
{
    using WixToolset.Data;

    public enum HelpFileToNamespaceSymbolFields
    {
        HelpFileRef,
        HelpNamespaceRef,
    }

    public class HelpFileToNamespaceSymbol : IntermediateSymbol
    {
        public HelpFileToNamespaceSymbol() : base(VSSymbolDefinitions.HelpFileToNamespace, null, null)
        {
        }

        public HelpFileToNamespaceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSSymbolDefinitions.HelpFileToNamespace, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpFileToNamespaceSymbolFields index] => this.Fields[(int)index];

        public string HelpFileRef
        {
            get => this.Fields[(int)HelpFileToNamespaceSymbolFields.HelpFileRef].AsString();
            set => this.Set((int)HelpFileToNamespaceSymbolFields.HelpFileRef, value);
        }

        public string HelpNamespaceRef
        {
            get => this.Fields[(int)HelpFileToNamespaceSymbolFields.HelpNamespaceRef].AsString();
            set => this.Set((int)HelpFileToNamespaceSymbolFields.HelpNamespaceRef, value);
        }
    }
}