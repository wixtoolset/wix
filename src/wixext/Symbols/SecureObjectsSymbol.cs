// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition SecureObjects = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.SecureObjects.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SecureObjectsSymbolFields.SecureObject), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SecureObjectsSymbolFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SecureObjectsSymbolFields.Domain), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SecureObjectsSymbolFields.User), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SecureObjectsSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SecureObjectsSymbolFields.Permission), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SecureObjectsSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(SecureObjectsSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using System;
    using WixToolset.Data;

    public enum SecureObjectsSymbolFields
    {
        SecureObject,
        Table,
        Domain,
        User,
        Attributes,
        Permission,
        ComponentRef,
    }

    [Flags]
    public enum WixPermissionExAttributes
    {
        None = 0x0,
        Inheritable = 0x01
    }

    public class SecureObjectsSymbol : IntermediateSymbol
    {
        public SecureObjectsSymbol() : base(UtilSymbolDefinitions.SecureObjects, null, null)
        {
        }

        public SecureObjectsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.SecureObjects, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SecureObjectsSymbolFields index] => this.Fields[(int)index];

        public string SecureObject
        {
            get => this.Fields[(int)SecureObjectsSymbolFields.SecureObject].AsString();
            set => this.Set((int)SecureObjectsSymbolFields.SecureObject, value);
        }

        public string Table
        {
            get => this.Fields[(int)SecureObjectsSymbolFields.Table].AsString();
            set => this.Set((int)SecureObjectsSymbolFields.Table, value);
        }

        public string Domain
        {
            get => this.Fields[(int)SecureObjectsSymbolFields.Domain].AsString();
            set => this.Set((int)SecureObjectsSymbolFields.Domain, value);
        }

        public string User
        {
            get => this.Fields[(int)SecureObjectsSymbolFields.User].AsString();
            set => this.Set((int)SecureObjectsSymbolFields.User, value);
        }

        public WixPermissionExAttributes Attributes
        {
            get => (WixPermissionExAttributes)this.Fields[(int)SecureObjectsSymbolFields.Attributes].AsNumber();
            set => this.Set((int)SecureObjectsSymbolFields.Attributes, (int)value);
        }

        public int? Permission
        {
            get => this.Fields[(int)SecureObjectsSymbolFields.Permission].AsNullableNumber();
            set => this.Set((int)SecureObjectsSymbolFields.Permission, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)SecureObjectsSymbolFields.ComponentRef].AsString();
            set => this.Set((int)SecureObjectsSymbolFields.ComponentRef, value);
        }
    }
}