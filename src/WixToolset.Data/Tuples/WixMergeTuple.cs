// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixMerge = new IntermediateTupleDefinition(
            TupleDefinitionType.WixMerge,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.WixMerge), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.SourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.FileCompression), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.ConfigurationData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.Feature_), IntermediateFieldType.String),
            },
            typeof(WixMergeTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixMergeTupleFields
    {
        WixMerge,
        Language,
        Directory_,
        SourceFile,
        DiskId,
        FileCompression,
        ConfigurationData,
        Feature_,
    }

    public class WixMergeTuple : IntermediateTuple
    {
        public WixMergeTuple() : base(TupleDefinitions.WixMerge, null, null)
        {
        }

        public WixMergeTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixMerge, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixMergeTupleFields index] => this.Fields[(int)index];

        public string WixMerge
        {
            get => (string)this.Fields[(int)WixMergeTupleFields.WixMerge];
            set => this.Set((int)WixMergeTupleFields.WixMerge, value);
        }

        public int Language
        {
            get => (int)this.Fields[(int)WixMergeTupleFields.Language];
            set => this.Set((int)WixMergeTupleFields.Language, value);
        }

        public string Directory_
        {
            get => (string)this.Fields[(int)WixMergeTupleFields.Directory_];
            set => this.Set((int)WixMergeTupleFields.Directory_, value);
        }

        public string SourceFile
        {
            get => (string)this.Fields[(int)WixMergeTupleFields.SourceFile];
            set => this.Set((int)WixMergeTupleFields.SourceFile, value);
        }

        public int DiskId
        {
            get => (int)this.Fields[(int)WixMergeTupleFields.DiskId];
            set => this.Set((int)WixMergeTupleFields.DiskId, value);
        }

        public bool? FileCompression
        {
            get => (bool?)this.Fields[(int)WixMergeTupleFields.FileCompression];
            set => this.Set((int)WixMergeTupleFields.FileCompression, value);
        }

        public string ConfigurationData
        {
            get => (string)this.Fields[(int)WixMergeTupleFields.ConfigurationData];
            set => this.Set((int)WixMergeTupleFields.ConfigurationData, value);
        }

        public string Feature_
        {
            get => (string)this.Fields[(int)WixMergeTupleFields.Feature_];
            set => this.Set((int)WixMergeTupleFields.Feature_, value);
        }
    }
}