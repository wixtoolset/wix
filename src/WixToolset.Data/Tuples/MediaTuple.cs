// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Media = new IntermediateTupleDefinition(
            TupleDefinitionType.Media,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MediaTupleFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MediaTupleFields.LastSequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MediaTupleFields.DiskPrompt), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MediaTupleFields.Cabinet), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MediaTupleFields.VolumeLabel), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MediaTupleFields.Source), IntermediateFieldType.String),
            },
            typeof(MediaTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MediaTupleFields
    {
        DiskId,
        LastSequence,
        DiskPrompt,
        Cabinet,
        VolumeLabel,
        Source,
    }

    public class MediaTuple : IntermediateTuple
    {
        public MediaTuple() : base(TupleDefinitions.Media, null, null)
        {
        }

        public MediaTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Media, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MediaTupleFields index] => this.Fields[(int)index];

        public int DiskId
        {
            get => (int)this.Fields[(int)MediaTupleFields.DiskId]?.Value;
            set => this.Set((int)MediaTupleFields.DiskId, value);
        }

        public int LastSequence
        {
            get => (int)this.Fields[(int)MediaTupleFields.LastSequence]?.Value;
            set => this.Set((int)MediaTupleFields.LastSequence, value);
        }

        public string DiskPrompt
        {
            get => (string)this.Fields[(int)MediaTupleFields.DiskPrompt]?.Value;
            set => this.Set((int)MediaTupleFields.DiskPrompt, value);
        }

        public string Cabinet
        {
            get => (string)this.Fields[(int)MediaTupleFields.Cabinet]?.Value;
            set => this.Set((int)MediaTupleFields.Cabinet, value);
        }

        public string VolumeLabel
        {
            get => (string)this.Fields[(int)MediaTupleFields.VolumeLabel]?.Value;
            set => this.Set((int)MediaTupleFields.VolumeLabel, value);
        }

        public string Source
        {
            get => (string)this.Fields[(int)MediaTupleFields.Source]?.Value;
            set => this.Set((int)MediaTupleFields.Source, value);
        }
    }
}