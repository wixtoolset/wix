// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition FileSharePermissions = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.FileSharePermissions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileSharePermissionsTupleFields.FileShare_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSharePermissionsTupleFields.User_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSharePermissionsTupleFields.Permissions), IntermediateFieldType.Number),
            },
            typeof(FileSharePermissionsTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum FileSharePermissionsTupleFields
    {
        FileShare_,
        User_,
        Permissions,
    }

    public class FileSharePermissionsTuple : IntermediateTuple
    {
        public FileSharePermissionsTuple() : base(UtilTupleDefinitions.FileSharePermissions, null, null)
        {
        }

        public FileSharePermissionsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.FileSharePermissions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileSharePermissionsTupleFields index] => this.Fields[(int)index];

        public string FileShare_
        {
            get => this.Fields[(int)FileSharePermissionsTupleFields.FileShare_].AsString();
            set => this.Set((int)FileSharePermissionsTupleFields.FileShare_, value);
        }

        public string User_
        {
            get => this.Fields[(int)FileSharePermissionsTupleFields.User_].AsString();
            set => this.Set((int)FileSharePermissionsTupleFields.User_, value);
        }

        public int Permissions
        {
            get => this.Fields[(int)FileSharePermissionsTupleFields.Permissions].AsNumber();
            set => this.Set((int)FileSharePermissionsTupleFields.Permissions, value);
        }
    }
}