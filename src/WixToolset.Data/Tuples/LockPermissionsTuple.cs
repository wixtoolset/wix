// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition LockPermissions = new IntermediateTupleDefinition(
            TupleDefinitionType.LockPermissions,
            new[]
            {
                new IntermediateFieldDefinition(nameof(LockPermissionsTupleFields.LockObject), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LockPermissionsTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LockPermissionsTupleFields.Domain), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LockPermissionsTupleFields.User), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(LockPermissionsTupleFields.Permission), IntermediateFieldType.Number),
            },
            typeof(LockPermissionsTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum LockPermissionsTupleFields
    {
        LockObject,
        Table,
        Domain,
        User,
        Permission,
    }

    public class LockPermissionsTuple : IntermediateTuple
    {
        public LockPermissionsTuple() : base(TupleDefinitions.LockPermissions, null, null)
        {
        }

        public LockPermissionsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.LockPermissions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[LockPermissionsTupleFields index] => this.Fields[(int)index];

        public string LockObject
        {
            get => (string)this.Fields[(int)LockPermissionsTupleFields.LockObject];
            set => this.Set((int)LockPermissionsTupleFields.LockObject, value);
        }

        public string Table
        {
            get => (string)this.Fields[(int)LockPermissionsTupleFields.Table];
            set => this.Set((int)LockPermissionsTupleFields.Table, value);
        }

        public string Domain
        {
            get => (string)this.Fields[(int)LockPermissionsTupleFields.Domain];
            set => this.Set((int)LockPermissionsTupleFields.Domain, value);
        }

        public string User
        {
            get => (string)this.Fields[(int)LockPermissionsTupleFields.User];
            set => this.Set((int)LockPermissionsTupleFields.User, value);
        }

        public int? Permission
        {
            get => (int?)this.Fields[(int)LockPermissionsTupleFields.Permission];
            set => this.Set((int)LockPermissionsTupleFields.Permission, value);
        }
    }
}