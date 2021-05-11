// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ImageFamilies = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ImageFamilies,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ImageFamiliesSymbolFields.Family), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ImageFamiliesSymbolFields.MediaSrcPropName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ImageFamiliesSymbolFields.MediaDiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ImageFamiliesSymbolFields.FileSequenceStart), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ImageFamiliesSymbolFields.DiskPrompt), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ImageFamiliesSymbolFields.VolumeLabel), IntermediateFieldType.String),
            },
            typeof(ImageFamiliesSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ImageFamiliesSymbolFields
    {
        Family,
        MediaSrcPropName,
        MediaDiskId,
        FileSequenceStart,
        DiskPrompt,
        VolumeLabel,
    }

    public class ImageFamiliesSymbol : IntermediateSymbol
    {
        public ImageFamiliesSymbol() : base(SymbolDefinitions.ImageFamilies, null, null)
        {
        }

        public ImageFamiliesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ImageFamilies, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ImageFamiliesSymbolFields index] => this.Fields[(int)index];

        public string Family
        {
            get => (string)this.Fields[(int)ImageFamiliesSymbolFields.Family];
            set => this.Set((int)ImageFamiliesSymbolFields.Family, value);
        }

        public string MediaSrcPropName
        {
            get => (string)this.Fields[(int)ImageFamiliesSymbolFields.MediaSrcPropName];
            set => this.Set((int)ImageFamiliesSymbolFields.MediaSrcPropName, value);
        }

        public int? MediaDiskId
        {
            get => (int?)this.Fields[(int)ImageFamiliesSymbolFields.MediaDiskId];
            set => this.Set((int)ImageFamiliesSymbolFields.MediaDiskId, value);
        }

        public int? FileSequenceStart
        {
            get => (int?)this.Fields[(int)ImageFamiliesSymbolFields.FileSequenceStart];
            set => this.Set((int)ImageFamiliesSymbolFields.FileSequenceStart, value);
        }

        public string DiskPrompt
        {
            get => (string)this.Fields[(int)ImageFamiliesSymbolFields.DiskPrompt];
            set => this.Set((int)ImageFamiliesSymbolFields.DiskPrompt, value);
        }

        public string VolumeLabel
        {
            get => (string)this.Fields[(int)ImageFamiliesSymbolFields.VolumeLabel];
            set => this.Set((int)ImageFamiliesSymbolFields.VolumeLabel, value);
        }
    }
}