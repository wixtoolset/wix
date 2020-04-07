// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Tuples;

    public static partial class VSTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition HelpNamespace = new IntermediateTupleDefinition(
            VSTupleDefinitionType.HelpNamespace.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpNamespaceTupleFields.NamespaceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpNamespaceTupleFields.CollectionFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpNamespaceTupleFields.Description), IntermediateFieldType.String),
            },
            typeof(HelpNamespaceTuple));
    }
}

namespace WixToolset.VisualStudio.Tuples
{
    using WixToolset.Data;

    public enum HelpNamespaceTupleFields
    {
        NamespaceName,
        CollectionFileRef,
        Description,
    }

    public class HelpNamespaceTuple : IntermediateTuple
    {
        public HelpNamespaceTuple() : base(VSTupleDefinitions.HelpNamespace, null, null)
        {
        }

        public HelpNamespaceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSTupleDefinitions.HelpNamespace, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpNamespaceTupleFields index] => this.Fields[(int)index];

        public string NamespaceName
        {
            get => this.Fields[(int)HelpNamespaceTupleFields.NamespaceName].AsString();
            set => this.Set((int)HelpNamespaceTupleFields.NamespaceName, value);
        }

        public string CollectionFileRef
        {
            get => this.Fields[(int)HelpNamespaceTupleFields.CollectionFileRef].AsString();
            set => this.Set((int)HelpNamespaceTupleFields.CollectionFileRef, value);
        }

        public string Description
        {
            get => this.Fields[(int)HelpNamespaceTupleFields.Description].AsString();
            set => this.Set((int)HelpNamespaceTupleFields.Description, value);
        }
    }
}