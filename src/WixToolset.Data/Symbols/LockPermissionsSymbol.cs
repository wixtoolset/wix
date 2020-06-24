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