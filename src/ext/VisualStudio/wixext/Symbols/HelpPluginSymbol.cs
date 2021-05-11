// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Symbols;

    public static partial class VSSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition HelpPlugin = new IntermediateSymbolDefinition(
            VSSymbolDefinitionType.HelpPlugin.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpPluginSymbolFields.HelpNamespaceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpPluginSymbolFields.ParentHelpNamespaceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpPluginSymbolFields.HxTFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpPluginSymbolFields.HxAFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpPluginSymbolFields.ParentHxTFileRef), IntermediateFieldType.String),
            },
            typeof(HelpPluginSymbol));
    }
}

namespace WixToolset.VisualStudio.Symbols
{
    using WixToolset.Data;

    public enum HelpPluginSymbolFields
    {
        HelpNamespaceRef,
        ParentHelpNamespaceRef,
        HxTFileRef,
        HxAFileRef,
        ParentHxTFileRef,
    }

    public class HelpPluginSymbol : IntermediateSymbol
    {
        public HelpPluginSymbol() : base(VSSymbolDefinitions.HelpPlugin, null, null)
        {
        }

        public HelpPluginSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSSymbolDefinitions.HelpPlugin, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpPluginSymbolFields index] => this.Fields[(int)index];

        public string HelpNamespaceRef
        {
            get => this.Fields[(int)HelpPluginSymbolFields.HelpNamespaceRef].AsString();
            set => this.Set((int)HelpPluginSymbolFields.HelpNamespaceRef, value);
        }

        public string ParentHelpNamespaceRef
        {
            get => this.Fields[(int)HelpPluginSymbolFields.ParentHelpNamespaceRef].AsString();
            set => this.Set((int)HelpPluginSymbolFields.ParentHelpNamespaceRef, value);
        }

        public string HxTFileRef
        {
            get => this.Fields[(int)HelpPluginSymbolFields.HxTFileRef].AsString();
            set => this.Set((int)HelpPluginSymbolFields.HxTFileRef, value);
        }

        public string HxAFileRef
        {
            get => this.Fields[(int)HelpPluginSymbolFields.HxAFileRef].AsString();
            set => this.Set((int)HelpPluginSymbolFields.HxAFileRef, value);
        }

        public string ParentHxTFileRef
        {
            get => this.Fields[(int)HelpPluginSymbolFields.ParentHxTFileRef].AsString();
            set => this.Set((int)HelpPluginSymbolFields.ParentHxTFileRef, value);
        }
    }
}