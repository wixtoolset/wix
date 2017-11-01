// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MoveFile = new IntermediateTupleDefinition(
            TupleDefinitionType.MoveFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.FileKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.SourceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.DestName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.SourceFolder), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.DestFolder), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.Options), IntermediateFieldType.Number),
            },
            typeof(MoveFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MoveFileTupleFields
    {
        FileKey,
        Component_,
        SourceName,
        DestName,
        SourceFolder,
        DestFolder,
        Options,
    }

    public class MoveFileTuple : IntermediateTuple
    {
        public MoveFileTuple() : base(TupleDefinitions.MoveFile, null, null)
        {
        }

        public MoveFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MoveFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MoveFileTupleFields index] => this.Fields[(int)index];

        public string FileKey
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.FileKey]?.Value;
            set => this.Set((int)MoveFileTupleFields.FileKey, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.Component_]?.Value;
            set => this.Set((int)MoveFileTupleFields.Component_, value);
        }

        public string SourceName
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.SourceName]?.Value;
            set => this.Set((int)MoveFileTupleFields.SourceName, value);
        }

        public string DestName
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.DestName]?.Value;
            set => this.Set((int)MoveFileTupleFields.DestName, value);
        }

        public string SourceFolder
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.SourceFolder]?.Value;
            set => this.Set((int)MoveFileTupleFields.SourceFolder, value);
        }

        public string DestFolder
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.DestFolder]?.Value;
            set => this.Set((int)MoveFileTupleFields.DestFolder, value);
        }

        public int Options
        {
            get => (int)this.Fields[(int)MoveFileTupleFields.Options]?.Value;
            set => this.Set((int)MoveFileTupleFields.Options, value);
        }
    }
}