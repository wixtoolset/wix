// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition LockPermissions = new IntermediateSymbolDefinition(
            SymbolDefinitionType.LockPermissions,
            new[]
            {
                new IntermediateFieldDefinition(nameof(LockPermissionsSymbolFields.LockObject), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LockPermissionsSymbolFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LockPermissionsSymbolFields.Domain), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LockPermissionsSymbolFields.User), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LockPermissionsSymbolFields.Permission), IntermediateFieldType.Number),
            },
            typeof(LockPermissionsSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum LockPermissionsSymbolFields
    {
        LockObject,
        Table,
        Domain,
        User,
        Permission,
    }

    /// <summary>
    ///-------------------------------------------------------------------------------------------------
    /// Layout of an Access Mask (from http://technet.microsoft.com/en-us/library/cc783530(WS.10).aspx)
    ///
    ///  -------------------------------------------------------------------------------------------------
    ///  |31|30|29|28|27|26|25|24|23|22|21|20|19|18|17|16|15|14|13|12|11|10|09|08|07|06|05|04|03|02|01|00|
    ///  -------------------------------------------------------------------------------------------------
    ///  |GR|GW|GE|GA| Reserved  |AS|StandardAccessRights|        Object-Specific Access Rights          |
    ///
    ///  Key
    ///  GR = Generic Read
    ///  GW = Generic Write
    ///  GE = Generic Execute
    ///  GA = Generic All
    ///  AS = Right to access SACL
    /// </summary>
    public static class LockPermissionConstants
    {
        /// <summary>
        /// Generic Access Rights (per WinNT.h)
        /// ---------------------
        /// GENERIC_ALL                      (0x10000000L)
        /// GENERIC_EXECUTE                  (0x20000000L)
        /// GENERIC_WRITE                    (0x40000000L)
        /// GENERIC_READ                     (0x80000000L)
        /// </summary>
        public static readonly string[] GenericPermissions = { "GenericAll", "GenericExecute", "GenericWrite", "GenericRead" };

        /// <summary>
        /// Standard Access Rights (per WinNT.h)
        /// ----------------------
        /// DELETE                           (0x00010000L)
        /// READ_CONTROL                     (0x00020000L)
        /// WRITE_DAC                        (0x00040000L)
        /// WRITE_OWNER                      (0x00080000L)
        /// SYNCHRONIZE                      (0x00100000L)
        /// </summary>
        public static readonly string[] StandardPermissions = { "Delete", "ReadPermission", "ChangePermission", "TakeOwnership", "Synchronize" };

        /// <summary>
        /// Object-Specific Access Rights
        /// =============================
        /// Directory Access Rights (per WinNT.h)
        /// -----------------------
        /// FILE_LIST_DIRECTORY       ( 0x0001 )
        /// FILE_ADD_FILE             ( 0x0002 )
        /// FILE_ADD_SUBDIRECTORY     ( 0x0004 )
        /// FILE_READ_EA              ( 0x0008 )
        /// FILE_WRITE_EA             ( 0x0010 )
        /// FILE_TRAVERSE             ( 0x0020 )
        /// FILE_DELETE_CHILD         ( 0x0040 )
        /// FILE_READ_ATTRIBUTES      ( 0x0080 )
        /// FILE_WRITE_ATTRIBUTES     ( 0x0100 )
        /// </summary>
        public static readonly string[] FolderPermissions = { "Read", "CreateFile", "CreateChild", "ReadExtendedAttributes", "WriteExtendedAttributes", "Traverse", "DeleteChild", "ReadAttributes", "WriteAttributes" };

        /// <summary>
        /// Registry Access Rights
        /// ----------------------
        /// </summary>
        public static readonly string[] RegistryPermissions = { "Read", "Write", "CreateSubkeys", "EnumerateSubkeys", "Notify", "CreateLink" };

        /// <summary>
        /// File Access Rights (per WinNT.h)
        /// ------------------
        /// FILE_READ_DATA            ( 0x0001 )
        /// FILE_WRITE_DATA           ( 0x0002 )
        /// FILE_APPEND_DATA          ( 0x0004 )
        /// FILE_READ_EA              ( 0x0008 )
        /// FILE_WRITE_EA             ( 0x0010 )
        /// FILE_EXECUTE              ( 0x0020 )
        /// via mask FILE_ALL_ACCESS  ( 0x0040 )
        /// FILE_READ_ATTRIBUTES      ( 0x0080 )
        /// FILE_WRITE_ATTRIBUTES     ( 0x0100 )
        ///
        /// STANDARD_RIGHTS_REQUIRED  (0x000F0000L)
        /// FILE_ALL_ACCESS           (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1FF)
        /// </summary>
        public static readonly string[] FilePermissions = { "Read", "Write", "Append", "ReadExtendedAttributes", "WriteExtendedAttributes", "Execute", "FileAllRights", "ReadAttributes", "WriteAttributes" };
    }

    public class LockPermissionsSymbol : IntermediateSymbol
    {
        public LockPermissionsSymbol() : base(SymbolDefinitions.LockPermissions, null, null)
        {
        }

        public LockPermissionsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.LockPermissions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[LockPermissionsSymbolFields index] => this.Fields[(int)index];

        public string LockObject
        {
            get => (string)this.Fields[(int)LockPermissionsSymbolFields.LockObject];
            set => this.Set((int)LockPermissionsSymbolFields.LockObject, value);
        }

        public string Table
        {
            get => (string)this.Fields[(int)LockPermissionsSymbolFields.Table];
            set => this.Set((int)LockPermissionsSymbolFields.Table, value);
        }

        public string Domain
        {
            get => (string)this.Fields[(int)LockPermissionsSymbolFields.Domain];
            set => this.Set((int)LockPermissionsSymbolFields.Domain, value);
        }

        public string User
        {
            get => (string)this.Fields[(int)LockPermissionsSymbolFields.User];
            set => this.Set((int)LockPermissionsSymbolFields.User, value);
        }

        public int? Permission
        {
            get => (int?)this.Fields[(int)LockPermissionsSymbolFields.Permission];
            set => this.Set((int)LockPermissionsSymbolFields.Permission, value);
        }
    }
}