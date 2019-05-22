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
                new IntermediateFieldDefinition(nameof(FileTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.ShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.FileSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Language), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.ReadOnly), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Hidden), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.System), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Vital), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Checksum), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.Compressed), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFields.FontTitle), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFields.SelfRegCost), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFields.BindPath), IntermediateFieldType.String),
            },
            typeof(FileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FileTupleFields
    {
        ComponentRef,
        ShortName,
        Name,
        FileSize,
        Version,
        Language,
        ReadOnly,
        Hidden,
        System,
        Vital,
        Checksum,
        Compressed,
        FontTitle,
        SelfRegCost,
        BindPath,
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

        public string ComponentRef
        {
            get => (string)this.Fields[(int)FileTupleFields.ComponentRef];
            set => this.Set((int)FileTupleFields.ComponentRef, value);
        }

        public string ShortName
        {
            get => (string)this.Fields[(int)FileTupleFields.ShortName];
            set => this.Set((int)FileTupleFields.ShortName, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)FileTupleFields.Name];
            set => this.Set((int)FileTupleFields.Name, value);
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

        public string FontTitle
        {
            get => (string)this.Fields[(int)FileTupleFields.FontTitle];
            set => this.Set((int)FileTupleFields.FontTitle, value);
        }

        public int? SelfRegCost
        {
            get => (int?)this.Fields[(int)FileTupleFields.SelfRegCost];
            set => this.Set((int)FileTupleFields.SelfRegCost, value);
        }

        public string BindPath
        {
            get => (string)this.Fields[(int)FileTupleFields.BindPath];
            set => this.Set((int)FileTupleFields.BindPath, value);
        }
    }
}
