// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixMediaTemplate = new IntermediateTupleDefinition(
            TupleDefinitionType.WixMediaTemplate,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixMediaTemplateTupleFields.CabinetTemplate), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateTupleFields.CompressionLevel), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateTupleFields.DiskPrompt), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateTupleFields.VolumeLabel), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateTupleFields.MaximumUncompressedMediaSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMediaTemplateTupleFields.MaximumCabinetSizeForLargeFileSplitting), IntermediateFieldType.Number),
            },
            typeof(WixMediaTemplateTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixMediaTemplateTupleFields
    {
        CabinetTemplate,
        CompressionLevel,
        DiskPrompt,
        VolumeLabel,
        MaximumUncompressedMediaSize,
        MaximumCabinetSizeForLargeFileSplitting,
    }

    public class WixMediaTemplateTuple : IntermediateTuple
    {
        public WixMediaTemplateTuple() : base(TupleDefinitions.WixMediaTemplate, null, null)
        {
        }

        public WixMediaTemplateTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixMediaTemplate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixMediaTemplateTupleFields index] => this.Fields[(int)index];

        public string CabinetTemplate
        {
            get => (string)this.Fields[(int)WixMediaTemplateTupleFields.CabinetTemplate]?.Value;
            set => this.Set((int)WixMediaTemplateTupleFields.CabinetTemplate, value);
        }

        public CompressionLevel CompressionLevel
        {
            get => (CompressionLevel)Enum.Parse(typeof(CompressionLevel), (string)this.Fields[(int)WixMediaTupleFields.CompressionLevel]?.Value, true);
            set => this.Set((int)WixMediaTupleFields.CompressionLevel, value.ToString());
        }

        public string DiskPrompt
        {
            get => (string)this.Fields[(int)WixMediaTemplateTupleFields.DiskPrompt]?.Value;
            set => this.Set((int)WixMediaTemplateTupleFields.DiskPrompt, value);
        }

        public string VolumeLabel
        {
            get => (string)this.Fields[(int)WixMediaTemplateTupleFields.VolumeLabel]?.Value;
            set => this.Set((int)WixMediaTemplateTupleFields.VolumeLabel, value);
        }

        public int MaximumUncompressedMediaSize
        {
            get => (int)this.Fields[(int)WixMediaTemplateTupleFields.MaximumUncompressedMediaSize]?.Value;
            set => this.Set((int)WixMediaTemplateTupleFields.MaximumUncompressedMediaSize, value);
        }

        public int MaximumCabinetSizeForLargeFileSplitting
        {
            get => (int)this.Fields[(int)WixMediaTemplateTupleFields.MaximumCabinetSizeForLargeFileSplitting]?.Value;
            set => this.Set((int)WixMediaTemplateTupleFields.MaximumCabinetSizeForLargeFileSplitting, value);
        }
    }
}