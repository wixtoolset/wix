// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition DuplicateFile = new IntermediateTupleDefinition(
            TupleDefinitionType.DuplicateFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(DuplicateFileTupleFields.FileKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileTupleFields.DestName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileTupleFields.DestFolder), IntermediateFieldType.String),
            },
            typeof(DuplicateFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum DuplicateFileTupleFields
    {
        FileKey,
        Component_,
        File_,
        DestName,
        DestFolder,
    }

    public class DuplicateFileTuple : IntermediateTuple
    {
        public DuplicateFileTuple() : base(TupleDefinitions.DuplicateFile, null, null)
        {
        }

        public DuplicateFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.DuplicateFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[DuplicateFileTupleFields index] => this.Fields[(int)index];

        public string FileKey
        {
            get => (string)this.Fields[(int)DuplicateFileTupleFields.FileKey]?.Value;
            set => this.Set((int)DuplicateFileTupleFields.FileKey, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)DuplicateFileTupleFields.Component_]?.Value;
            set => this.Set((int)DuplicateFileTupleFields.Component_, value);
        }

        public string File_
        {
            get => (string)this.Fields[(int)DuplicateFileTupleFields.File_]?.Value;
            set => this.Set((int)DuplicateFileTupleFields.File_, value);
        }

        public string DestName
        {
            get => (string)this.Fields[(int)DuplicateFileTupleFields.DestName]?.Value;
            set => this.Set((int)DuplicateFileTupleFields.DestName, value);
        }

        public string DestFolder
        {
            get => (string)this.Fields[(int)DuplicateFileTupleFields.DestFolder]?.Value;
            set => this.Set((int)DuplicateFileTupleFields.DestFolder, value);
        }
    }
}