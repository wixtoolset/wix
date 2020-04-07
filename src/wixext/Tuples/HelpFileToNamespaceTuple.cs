// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Tuples;

    public static partial class VSTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition HelpFileToNamespace = new IntermediateTupleDefinition(
            VSTupleDefinitionType.HelpFileToNamespace.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpFileToNamespaceTupleFields.HelpFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileToNamespaceTupleFields.HelpNamespaceRef), IntermediateFieldType.String),
            },
            typeof(HelpFileToNamespaceTuple));
    }
}

namespace WixToolset.VisualStudio.Tuples
{
    using WixToolset.Data;

    public enum HelpFileToNamespaceTupleFields
    {
        HelpFileRef,
        HelpNamespaceRef,
    }

    public class HelpFileToNamespaceTuple : IntermediateTuple
    {
        public HelpFileToNamespaceTuple() : base(VSTupleDefinitions.HelpFileToNamespace, null, null)
        {
        }

        public HelpFileToNamespaceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSTupleDefinitions.HelpFileToNamespace, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpFileToNamespaceTupleFields index] => this.Fields[(int)index];

        public string HelpFileRef
        {
            get => this.Fields[(int)HelpFileToNamespaceTupleFields.HelpFileRef].AsString();
            set => this.Set((int)HelpFileToNamespaceTupleFields.HelpFileRef, value);
        }

        public string HelpNamespaceRef
        {
            get => this.Fields[(int)HelpFileToNamespaceTupleFields.HelpNamespaceRef].AsString();
            set => this.Set((int)HelpFileToNamespaceTupleFields.HelpNamespaceRef, value);
        }
    }
}