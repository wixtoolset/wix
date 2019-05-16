// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixFileSearch = new IntermediateTupleDefinition(
            TupleDefinitionType.WixFileSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.WixSearch_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.MinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.MaxVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.MinSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.MaxSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.MinDate), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.MaxDate), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.Languages), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileSearchTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixFileSearchTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixFileSearchTupleFields
    {
        WixSearch_,
        Path,
        MinVersion,
        MaxVersion,
        MinSize,
        MaxSize,
        MinDate,
        MaxDate,
        Languages,
        Attributes,
    }

    public class WixFileSearchTuple : IntermediateTuple
    {
        public WixFileSearchTuple() : base(TupleDefinitions.WixFileSearch, null, null)
        {
        }

        public WixFileSearchTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixFileSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFileSearchTupleFields index] => this.Fields[(int)index];

        public string WixSearch_
        {
            get => (string)this.Fields[(int)WixFileSearchTupleFields.WixSearch_];
            set => this.Set((int)WixFileSearchTupleFields.WixSearch_, value);
        }

        public string Path
        {
            get => (string)this.Fields[(int)WixFileSearchTupleFields.Path];
            set => this.Set((int)WixFileSearchTupleFields.Path, value);
        }

        public string MinVersion
        {
            get => (string)this.Fields[(int)WixFileSearchTupleFields.MinVersion];
            set => this.Set((int)WixFileSearchTupleFields.MinVersion, value);
        }

        public string MaxVersion
        {
            get => (string)this.Fields[(int)WixFileSearchTupleFields.MaxVersion];
            set => this.Set((int)WixFileSearchTupleFields.MaxVersion, value);
        }

        public int MinSize
        {
            get => (int)this.Fields[(int)WixFileSearchTupleFields.MinSize];
            set => this.Set((int)WixFileSearchTupleFields.MinSize, value);
        }

        public int MaxSize
        {
            get => (int)this.Fields[(int)WixFileSearchTupleFields.MaxSize];
            set => this.Set((int)WixFileSearchTupleFields.MaxSize, value);
        }

        public int MinDate
        {
            get => (int)this.Fields[(int)WixFileSearchTupleFields.MinDate];
            set => this.Set((int)WixFileSearchTupleFields.MinDate, value);
        }

        public int MaxDate
        {
            get => (int)this.Fields[(int)WixFileSearchTupleFields.MaxDate];
            set => this.Set((int)WixFileSearchTupleFields.MaxDate, value);
        }

        public string Languages
        {
            get => (string)this.Fields[(int)WixFileSearchTupleFields.Languages];
            set => this.Set((int)WixFileSearchTupleFields.Languages, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixFileSearchTupleFields.Attributes];
            set => this.Set((int)WixFileSearchTupleFields.Attributes, value);
        }
    }
}