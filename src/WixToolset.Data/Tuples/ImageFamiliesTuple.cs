// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ImageFamilies = new IntermediateTupleDefinition(
            TupleDefinitionType.ImageFamilies,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ImageFamiliesTupleFields.Family), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ImageFamiliesTupleFields.MediaSrcPropName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ImageFamiliesTupleFields.MediaDiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ImageFamiliesTupleFields.FileSequenceStart), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ImageFamiliesTupleFields.DiskPrompt), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ImageFamiliesTupleFields.VolumeLabel), IntermediateFieldType.String),
            },
            typeof(ImageFamiliesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ImageFamiliesTupleFields
    {
        Family,
        MediaSrcPropName,
        MediaDiskId,
        FileSequenceStart,
        DiskPrompt,
        VolumeLabel,
    }

    public class ImageFamiliesTuple : IntermediateTuple
    {
        public ImageFamiliesTuple() : base(TupleDefinitions.ImageFamilies, null, null)
        {
        }

        public ImageFamiliesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ImageFamilies, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ImageFamiliesTupleFields index] => this.Fields[(int)index];

        public string Family
        {
            get => (string)this.Fields[(int)ImageFamiliesTupleFields.Family];
            set => this.Set((int)ImageFamiliesTupleFields.Family, value);
        }

        public string MediaSrcPropName
        {
            get => (string)this.Fields[(int)ImageFamiliesTupleFields.MediaSrcPropName];
            set => this.Set((int)ImageFamiliesTupleFields.MediaSrcPropName, value);
        }

        public int MediaDiskId
        {
            get => (int)this.Fields[(int)ImageFamiliesTupleFields.MediaDiskId];
            set => this.Set((int)ImageFamiliesTupleFields.MediaDiskId, value);
        }

        public int FileSequenceStart
        {
            get => (int)this.Fields[(int)ImageFamiliesTupleFields.FileSequenceStart];
            set => this.Set((int)ImageFamiliesTupleFields.FileSequenceStart, value);
        }

        public string DiskPrompt
        {
            get => (string)this.Fields[(int)ImageFamiliesTupleFields.DiskPrompt];
            set => this.Set((int)ImageFamiliesTupleFields.DiskPrompt, value);
        }

        public string VolumeLabel
        {
            get => (string)this.Fields[(int)ImageFamiliesTupleFields.VolumeLabel];
            set => this.Set((int)ImageFamiliesTupleFields.VolumeLabel, value);
        }
    }
}