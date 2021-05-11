// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Symbols;

    public static partial class VSSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition HelpNamespace = new IntermediateSymbolDefinition(
            VSSymbolDefinitionType.HelpNamespace.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpNamespaceSymbolFields.NamespaceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpNamespaceSymbolFields.CollectionFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpNamespaceSymbolFields.Description), IntermediateFieldType.String),
            },
            typeof(HelpNamespaceSymbol));
    }
}

namespace WixToolset.VisualStudio.Symbols
{
    using WixToolset.Data;

    public enum HelpNamespaceSymbolFields
    {
        NamespaceName,
        CollectionFileRef,
        Description,
    }

    public class HelpNamespaceSymbol : IntermediateSymbol
    {
        public HelpNamespaceSymbol() : base(VSSymbolDefinitions.HelpNamespace, null, null)
        {
        }

        public HelpNamespaceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSSymbolDefinitions.HelpNamespace, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpNamespaceSymbolFields index] => this.Fields[(int)index];

        public string NamespaceName
        {
            get => this.Fields[(int)HelpNamespaceSymbolFields.NamespaceName].AsString();
            set => this.Set((int)HelpNamespaceSymbolFields.NamespaceName, value);
        }

        public string CollectionFileRef
        {
            get => this.Fields[(int)HelpNamespaceSymbolFields.CollectionFileRef].AsString();
            set => this.Set((int)HelpNamespaceSymbolFields.CollectionFileRef, value);
        }

        public string Description
        {
            get => this.Fields[(int)HelpNamespaceSymbolFields.Description].AsString();
            set => this.Set((int)HelpNamespaceSymbolFields.Description, value);
        }
    }
}