// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition SecureObjects = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.SecureObjects.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SecureObjectsTupleFields.SecureObject), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SecureObjectsTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SecureObjectsTupleFields.Domain), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SecureObjectsTupleFields.User), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SecureObjectsTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SecureObjectsTupleFields.Permission), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SecureObjectsTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(SecureObjectsTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum SecureObjectsTupleFields
    {
        SecureObject,
        Table,
        Domain,
        User,
        Attributes,
        Permission,
        ComponentRef,
    }

    public class SecureObjectsTuple : IntermediateTuple
    {
        public SecureObjectsTuple() : base(UtilTupleDefinitions.SecureObjects, null, null)
        {
        }

        public SecureObjectsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.SecureObjects, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SecureObjectsTupleFields index] => this.Fields[(int)index];

        public string SecureObject
        {
            get => this.Fields[(int)SecureObjectsTupleFields.SecureObject].AsString();
            set => this.Set((int)SecureObjectsTupleFields.SecureObject, value);
        }

        public string Table
        {
            get => this.Fields[(int)SecureObjectsTupleFields.Table].AsString();
            set => this.Set((int)SecureObjectsTupleFields.Table, value);
        }

        public string Domain
        {
            get => this.Fields[(int)SecureObjectsTupleFields.Domain].AsString();
            set => this.Set((int)SecureObjectsTupleFields.Domain, value);
        }

        public string User
        {
            get => this.Fields[(int)SecureObjectsTupleFields.User].AsString();
            set => this.Set((int)SecureObjectsTupleFields.User, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)SecureObjectsTupleFields.Attributes].AsNumber();
            set => this.Set((int)SecureObjectsTupleFields.Attributes, value);
        }

        public int? Permission
        {
            get => this.Fields[(int)SecureObjectsTupleFields.Permission].AsNullableNumber();
            set => this.Set((int)SecureObjectsTupleFields.Permission, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)SecureObjectsTupleFields.ComponentRef].AsString();
            set => this.Set((int)SecureObjectsTupleFields.ComponentRef, value);
        }
    }
}