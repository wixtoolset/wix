// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Tuples;

    public static partial class VSTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition HelpFilter = new IntermediateTupleDefinition(
            VSTupleDefinitionType.HelpFilter.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpFilterTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFilterTupleFields.QueryString), IntermediateFieldType.String),
            },
            typeof(HelpFilterTuple));
    }
}

namespace WixToolset.VisualStudio.Tuples
{
    using WixToolset.Data;

    public enum HelpFilterTupleFields
    {
        Description,
        QueryString,
    }

    public class HelpFilterTuple : IntermediateTuple
    {
        public HelpFilterTuple() : base(VSTupleDefinitions.HelpFilter, null, null)
        {
        }

        public HelpFilterTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSTupleDefinitions.HelpFilter, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpFilterTupleFields index] => this.Fields[(int)index];

        public string Description
        {
            get => this.Fields[(int)HelpFilterTupleFields.Description].AsString();
            set => this.Set((int)HelpFilterTupleFields.Description, value);
        }

        public string QueryString
        {
            get => this.Fields[(int)HelpFilterTupleFields.QueryString].AsString();
            set => this.Set((int)HelpFilterTupleFields.QueryString, value);
        }
    }
}