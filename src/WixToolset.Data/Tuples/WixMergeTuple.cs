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
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.SourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.FileAttributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.ConfigurationData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMergeTupleFields.FeatureRef), IntermediateFieldType.String),
            },
            typeof(WixMergeTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixMergeTupleFields
    {
        Language,
        DirectoryRef,
        SourceFile,
        DiskId,
        FileAttributes,
        ConfigurationData,
        FeatureRef,
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

        public int Language
        {
            get => (int)this.Fields[(int)WixMergeTupleFields.Language];
            set => this.Set((int)WixMergeTupleFields.Language, value);
        }

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)WixMergeTupleFields.DirectoryRef];
            set => this.Set((int)WixMergeTupleFields.DirectoryRef, value);
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

        public FileTupleAttributes FileAttributes
        {
            get => (FileTupleAttributes)this.Fields[(int)WixMergeTupleFields.FileAttributes].AsNumber();
            set => this.Set((int)WixMergeTupleFields.FileAttributes, (int)value);
        }

        public string ConfigurationData
        {
            get => (string)this.Fields[(int)WixMergeTupleFields.ConfigurationData];
            set => this.Set((int)WixMergeTupleFields.ConfigurationData, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)WixMergeTupleFields.FeatureRef];
            set => this.Set((int)WixMergeTupleFields.FeatureRef, value);
        }
    }
}
