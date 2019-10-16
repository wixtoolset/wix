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
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.FileShare), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.ShareName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.User_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileShareTupleFields.Permissions), IntermediateFieldType.Number),
            },
            typeof(FileShareTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum FileShareTupleFields
    {
        FileShare,
        ShareName,
        Component_,
        Description,
        Directory_,
        User_,
        Permissions,
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

        public string FileShare
        {
            get => this.Fields[(int)FileShareTupleFields.FileShare].AsString();
            set => this.Set((int)FileShareTupleFields.FileShare, value);
        }

        public string ShareName
        {
            get => this.Fields[(int)FileShareTupleFields.ShareName].AsString();
            set => this.Set((int)FileShareTupleFields.ShareName, value);
        }

        public string Component_
        {
            get => this.Fields[(int)FileShareTupleFields.Component_].AsString();
            set => this.Set((int)FileShareTupleFields.Component_, value);
        }

        public string Description
        {
            get => this.Fields[(int)FileShareTupleFields.Description].AsString();
            set => this.Set((int)FileShareTupleFields.Description, value);
        }

        public string Directory_
        {
            get => this.Fields[(int)FileShareTupleFields.Directory_].AsString();
            set => this.Set((int)FileShareTupleFields.Directory_, value);
        }

        public string User_
        {
            get => this.Fields[(int)FileShareTupleFields.User_].AsString();
            set => this.Set((int)FileShareTupleFields.User_, value);
        }

        public int? Permissions
        {
            get => this.Fields[(int)FileShareTupleFields.Permissions].AsNullableNumber();
            set => this.Set((int)FileShareTupleFields.Permissions, value);
        }
    }
}