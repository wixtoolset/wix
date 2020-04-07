// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Tuples;

    public static partial class VSTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition HelpPlugin = new IntermediateTupleDefinition(
            VSTupleDefinitionType.HelpPlugin.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpPluginTupleFields.HelpNamespaceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpPluginTupleFields.ParentHelpNamespaceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpPluginTupleFields.HxTFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpPluginTupleFields.HxAFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpPluginTupleFields.ParentHxTFileRef), IntermediateFieldType.String),
            },
            typeof(HelpPluginTuple));
    }
}

namespace WixToolset.VisualStudio.Tuples
{
    using WixToolset.Data;

    public enum HelpPluginTupleFields
    {
        HelpNamespaceRef,
        ParentHelpNamespaceRef,
        HxTFileRef,
        HxAFileRef,
        ParentHxTFileRef,
    }

    public class HelpPluginTuple : IntermediateTuple
    {
        public HelpPluginTuple() : base(VSTupleDefinitions.HelpPlugin, null, null)
        {
        }

        public HelpPluginTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSTupleDefinitions.HelpPlugin, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpPluginTupleFields index] => this.Fields[(int)index];

        public string HelpNamespaceRef
        {
            get => this.Fields[(int)HelpPluginTupleFields.HelpNamespaceRef].AsString();
            set => this.Set((int)HelpPluginTupleFields.HelpNamespaceRef, value);
        }

        public string ParentHelpNamespaceRef
        {
            get => this.Fields[(int)HelpPluginTupleFields.ParentHelpNamespaceRef].AsString();
            set => this.Set((int)HelpPluginTupleFields.ParentHelpNamespaceRef, value);
        }

        public string HxTFileRef
        {
            get => this.Fields[(int)HelpPluginTupleFields.HxTFileRef].AsString();
            set => this.Set((int)HelpPluginTupleFields.HxTFileRef, value);
        }

        public string HxAFileRef
        {
            get => this.Fields[(int)HelpPluginTupleFields.HxAFileRef].AsString();
            set => this.Set((int)HelpPluginTupleFields.HxAFileRef, value);
        }

        public string ParentHxTFileRef
        {
            get => this.Fields[(int)HelpPluginTupleFields.ParentHxTFileRef].AsString();
            set => this.Set((int)HelpPluginTupleFields.ParentHxTFileRef, value);
        }
    }
}