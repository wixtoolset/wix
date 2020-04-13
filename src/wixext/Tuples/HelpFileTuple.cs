// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Tuples;

    public static partial class VSTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition HelpFile = new IntermediateTupleDefinition(
            VSTupleDefinitionType.HelpFile.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpFileTupleFields.HelpFileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileTupleFields.LangID), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(HelpFileTupleFields.HxSFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileTupleFields.HxIFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileTupleFields.HxQFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileTupleFields.HxRFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileTupleFields.SamplesFileRef), IntermediateFieldType.String),
            },
            typeof(HelpFileTuple));
    }
}

namespace WixToolset.VisualStudio.Tuples
{
    using WixToolset.Data;

    public enum HelpFileTupleFields
    {
        HelpFileName,
        LangID,
        HxSFileRef,
        HxIFileRef,
        HxQFileRef,
        HxRFileRef,
        SamplesFileRef,
    }

    public class HelpFileTuple : IntermediateTuple
    {
        public HelpFileTuple() : base(VSTupleDefinitions.HelpFile, null, null)
        {
        }

        public HelpFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSTupleDefinitions.HelpFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpFileTupleFields index] => this.Fields[(int)index];

        public string HelpFileName
        {
            get => this.Fields[(int)HelpFileTupleFields.HelpFileName].AsString();
            set => this.Set((int)HelpFileTupleFields.HelpFileName, value);
        }

        public int? LangID
        {
            get => this.Fields[(int)HelpFileTupleFields.LangID].AsNullableNumber();
            set => this.Set((int)HelpFileTupleFields.LangID, value);
        }

        public string HxSFileRef
        {
            get => this.Fields[(int)HelpFileTupleFields.HxSFileRef].AsString();
            set => this.Set((int)HelpFileTupleFields.HxSFileRef, value);
        }

        public string HxIFileRef
        {
            get => this.Fields[(int)HelpFileTupleFields.HxIFileRef].AsString();
            set => this.Set((int)HelpFileTupleFields.HxIFileRef, value);
        }

        public string HxQFileRef
        {
            get => this.Fields[(int)HelpFileTupleFields.HxQFileRef].AsString();
            set => this.Set((int)HelpFileTupleFields.HxQFileRef, value);
        }

        public string HxRFileRef
        {
            get => this.Fields[(int)HelpFileTupleFields.HxRFileRef].AsString();
            set => this.Set((int)HelpFileTupleFields.HxRFileRef, value);
        }

        public string SamplesFileRef
        {
            get => this.Fields[(int)HelpFileTupleFields.SamplesFileRef].AsString();
            set => this.Set((int)HelpFileTupleFields.SamplesFileRef, value);
        }
    }
}