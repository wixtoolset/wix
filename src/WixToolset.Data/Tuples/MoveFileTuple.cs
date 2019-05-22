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
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.SourceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.DestName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.SourceFolder), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.DestFolder), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MoveFileTupleFields.Delete), IntermediateFieldType.Bool),
            },
            typeof(MoveFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MoveFileTupleFields
    {
        ComponentRef,
        SourceName,
        DestName,
        SourceFolder,
        DestFolder,
        Delete,
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

        public string ComponentRef
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.ComponentRef];
            set => this.Set((int)MoveFileTupleFields.ComponentRef, value);
        }

        public string SourceName
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.SourceName];
            set => this.Set((int)MoveFileTupleFields.SourceName, value);
        }

        public string DestName
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.DestName];
            set => this.Set((int)MoveFileTupleFields.DestName, value);
        }

        public string SourceFolder
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.SourceFolder];
            set => this.Set((int)MoveFileTupleFields.SourceFolder, value);
        }

        public string DestFolder
        {
            get => (string)this.Fields[(int)MoveFileTupleFields.DestFolder];
            set => this.Set((int)MoveFileTupleFields.DestFolder, value);
        }

        public bool Delete
        {
            get => (bool)this.Fields[(int)MoveFileTupleFields.Delete];
            set => this.Set((int)MoveFileTupleFields.Delete, value);
        }
    }
}
