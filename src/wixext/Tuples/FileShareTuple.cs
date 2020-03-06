// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition FileShare = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.FileShare.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.ShareName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.DirectoryRef), IntermediateFieldType.String),
            },
            typeof(FileShareTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum FileShareTupleFields
    {
        ShareName,
        ComponentRef,
        Description,
        DirectoryRef,
    }

    public class FileShareTuple : IntermediateTuple
    {
        public FileShareTuple() : base(UtilTupleDefinitions.FileShare, null, null)
        {
        }

        public FileShareTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.FileShare, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileShareTupleFields index] => this.Fields[(int)index];

        public string ShareName
        {
            get => this.Fields[(int)FileShareTupleFields.ShareName].AsString();
            set => this.Set((int)FileShareTupleFields.ShareName, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)FileShareTupleFields.ComponentRef].AsString();
            set => this.Set((int)FileShareTupleFields.ComponentRef, value);
        }

        public string Description
        {
            get => this.Fields[(int)FileShareTupleFields.Description].AsString();
            set => this.Set((int)FileShareTupleFields.Description, value);
        }

        public string DirectoryRef
        {
            get => this.Fields[(int)FileShareTupleFields.DirectoryRef].AsString();
            set => this.Set((int)FileShareTupleFields.DirectoryRef, value);
        }
    }
}