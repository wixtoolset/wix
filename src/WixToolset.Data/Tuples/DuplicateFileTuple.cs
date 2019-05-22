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
                new IntermediateFieldDefinition(nameof(DuplicateFileTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileTupleFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileTupleFields.DestinationName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DuplicateFileTupleFields.DestinationFolder), IntermediateFieldType.String),
            },
            typeof(DuplicateFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum DuplicateFileTupleFields
    {
        ComponentRef,
        FileRef,
        DestinationName,
        DestinationFolder,
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

        public string ComponentRef
        {
            get => (string)this.Fields[(int)DuplicateFileTupleFields.ComponentRef];
            set => this.Set((int)DuplicateFileTupleFields.ComponentRef, value);
        }

        public string FileRef
        {
            get => (string)this.Fields[(int)DuplicateFileTupleFields.FileRef];
            set => this.Set((int)DuplicateFileTupleFields.FileRef, value);
        }

        public string DestinationName
        {
            get => (string)this.Fields[(int)DuplicateFileTupleFields.DestinationName];
            set => this.Set((int)DuplicateFileTupleFields.DestinationName, value);
        }

        public string DestinationFolder
        {
            get => (string)this.Fields[(int)DuplicateFileTupleFields.DestinationFolder];
            set => this.Set((int)DuplicateFileTupleFields.DestinationFolder, value);
        }
    }
}