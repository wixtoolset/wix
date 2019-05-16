// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixMedia = new IntermediateTupleDefinition(
            TupleDefinitionType.WixMedia,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixMediaTupleFields.DiskId_), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixMediaTupleFields.CompressionLevel), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMediaTupleFields.Layout), IntermediateFieldType.String),
            },
            typeof(WixMediaTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixMediaTupleFields
    {
        DiskId_,
        CompressionLevel,
        Layout,
    }

    public class WixMediaTuple : IntermediateTuple
    {
        public WixMediaTuple() : base(TupleDefinitions.WixMedia, null, null)
        {
        }

        public WixMediaTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixMedia, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixMediaTupleFields index] => this.Fields[(int)index];

        public int DiskId_
        {
            get => (int)this.Fields[(int)WixMediaTupleFields.DiskId_];
            set => this.Set((int)WixMediaTupleFields.DiskId_, value);
        }

        public CompressionLevel? CompressionLevel
        {
            get => Enum.TryParse((string)this.Fields[(int)WixMediaTupleFields.CompressionLevel], true, out CompressionLevel value) ? value : (CompressionLevel?)null;
            set => this.Set((int)WixMediaTupleFields.CompressionLevel, value?.ToString());
        }

        public string Layout
        {
            get => (string)this.Fields[(int)WixMediaTupleFields.Layout];
            set => this.Set((int)WixMediaTupleFields.Layout, value);
        }
    }
}