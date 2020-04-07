// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Tuples;

    public static partial class VSTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition HelpFilterToNamespace = new IntermediateTupleDefinition(
            VSTupleDefinitionType.HelpFilterToNamespace.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpFilterToNamespaceTupleFields.HelpFilterRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFilterToNamespaceTupleFields.HelpNamespaceRef), IntermediateFieldType.String),
            },
            typeof(HelpFilterToNamespaceTuple));
    }
}

namespace WixToolset.VisualStudio.Tuples
{
    using WixToolset.Data;

    public enum HelpFilterToNamespaceTupleFields
    {
        HelpFilterRef,
        HelpNamespaceRef,
    }

    public class HelpFilterToNamespaceTuple : IntermediateTuple
    {
        public HelpFilterToNamespaceTuple() : base(VSTupleDefinitions.HelpFilterToNamespace, null, null)
        {
        }

        public HelpFilterToNamespaceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSTupleDefinitions.HelpFilterToNamespace, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpFilterToNamespaceTupleFields index] => this.Fields[(int)index];

        public string HelpFilterRef
        {
            get => this.Fields[(int)HelpFilterToNamespaceTupleFields.HelpFilterRef].AsString();
            set => this.Set((int)HelpFilterToNamespaceTupleFields.HelpFilterRef, value);
        }

        public string HelpNamespaceRef
        {
            get => this.Fields[(int)HelpFilterToNamespaceTupleFields.HelpNamespaceRef].AsString();
            set => this.Set((int)HelpFilterToNamespaceTupleFields.HelpNamespaceRef, value);
        }
    }
}