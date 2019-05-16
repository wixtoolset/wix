// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition File = new IntermediateTupleDefinition(
            TupleDefinitionType.File,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileTupleFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.ShortFileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.LongFileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.FileSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Language), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.ReadOnly), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Hidden), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.System), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Vital), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Checksum), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Compressed), IntermediateFieldType.Bool),
            },
            typeof(FileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FileTupleFields
    {
        File,
        Component_,
        ShortFileName,
        LongFileName,
        FileSize,
        Version,
        Language,
        ReadOnly,
        Hidden,
        System,
        Vital,
        Checksum,
        Compressed,
    }

    public class FileTuple : IntermediateTuple
    {
        public FileTuple() : base(TupleDefinitions.File, null, null)
        {
        }

        public FileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.File, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileTupleFields index] => this.Fields[(int)index];

        public string File
        {
            get => (string)this.Fields[(int)FileTupleFields.File];
            set => this.Set((int)FileTupleFields.File, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)FileTupleFields.Component_];
            set => this.Set((int)FileTupleFields.Component_, value);
        }

        public string ShortFileName
        {
            get => (string)this.Fields[(int)FileTupleFields.ShortFileName];
            set => this.Set((int)FileTupleFields.ShortFileName, value);
        }

        public string LongFileName
        {
            get => (string)this.Fields[(int)FileTupleFields.LongFileName];
            set => this.Set((int)FileTupleFields.LongFileName, value);
        }

        public int FileSize
        {
            get => (int)this.Fields[(int)FileTupleFields.FileSize];
            set => this.Set((int)FileTupleFields.FileSize, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)FileTupleFields.Version];
            set => this.Set((int)FileTupleFields.Version, value);
        }

        public string Language
        {
            get => (string)this.Fields[(int)FileTupleFields.Language];
            set => this.Set((int)FileTupleFields.Language, value);
        }

        public bool ReadOnly
        {
            get => (bool)this.Fields[(int)FileTupleFields.ReadOnly];
            set => this.Set((int)FileTupleFields.ReadOnly, value);
        }

        public bool Hidden
        {
            get => (bool)this.Fields[(int)FileTupleFields.Hidden];
            set => this.Set((int)FileTupleFields.Hidden, value);
        }

        public bool System
        {
            get => (bool)this.Fields[(int)FileTupleFields.System];
            set => this.Set((int)FileTupleFields.System, value);
        }

        public bool Vital
        {
            get => (bool)this.Fields[(int)FileTupleFields.Vital];
            set => this.Set((int)FileTupleFields.Vital, value);
        }

        public bool Checksum
        {
            get => (bool)this.Fields[(int)FileTupleFields.Checksum];
            set => this.Set((int)FileTupleFields.Checksum, value);
        }

        public bool? Compressed
        {
            get => (bool?)this.Fields[(int)FileTupleFields.Compressed];
            set => this.Set((int)FileTupleFields.Compressed, value);
        }
    }
}
