// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition FileSharePermissions = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.FileSharePermissions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileSharePermissionsSymbolFields.FileShareRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSharePermissionsSymbolFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSharePermissionsSymbolFields.Permissions), IntermediateFieldType.Number),
            },
            typeof(FileSharePermissionsSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum FileSharePermissionsSymbolFields
    {
        FileShareRef,
        UserRef,
        Permissions,
    }

    public class FileSharePermissionsSymbol : IntermediateSymbol
    {
        public FileSharePermissionsSymbol() : base(UtilSymbolDefinitions.FileSharePermissions, null, null)
        {
        }

        public FileSharePermissionsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.FileSharePermissions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileSharePermissionsSymbolFields index] => this.Fields[(int)index];

        public string FileShareRef
        {
            get => this.Fields[(int)FileSharePermissionsSymbolFields.FileShareRef].AsString();
            set => this.Set((int)FileSharePermissionsSymbolFields.FileShareRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)FileSharePermissionsSymbolFields.UserRef].AsString();
            set => this.Set((int)FileSharePermissionsSymbolFields.UserRef, value);
        }

        public int Permissions
        {
            get => this.Fields[(int)FileSharePermissionsSymbolFields.Permissions].AsNumber();
            set => this.Set((int)FileSharePermissionsSymbolFields.Permissions, value);
        }
    }
}