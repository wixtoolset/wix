// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebDirProperties = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebDirProperties.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.Access), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.Authorization), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.AnonymousUserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.IIsControlledPassword), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.LogVisits), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.Index), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.DefaultDoc), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.AspDetailedError), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.HttpExpires), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.CacheControlMaxAge), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.CacheControlCustom), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.NoCustomError), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.AccessSSLFlags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesTupleFields.AuthenticationProviders), IntermediateFieldType.String),
            },
            typeof(IIsWebDirPropertiesTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebDirPropertiesTupleFields
    {
        Access,
        Authorization,
        AnonymousUserRef,
        IIsControlledPassword,
        LogVisits,
        Index,
        DefaultDoc,
        AspDetailedError,
        HttpExpires,
        CacheControlMaxAge,
        CacheControlCustom,
        NoCustomError,
        AccessSSLFlags,
        AuthenticationProviders,
    }

    public class IIsWebDirPropertiesTuple : IntermediateTuple
    {
        public IIsWebDirPropertiesTuple() : base(IisTupleDefinitions.IIsWebDirProperties, null, null)
        {
        }

        public IIsWebDirPropertiesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebDirProperties, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebDirPropertiesTupleFields index] => this.Fields[(int)index];

        public int Access
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.Access].AsNumber();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.Access, value);
        }

        public int Authorization
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.Authorization].AsNumber();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.Authorization, value);
        }

        public string AnonymousUserRef
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.AnonymousUserRef].AsString();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.AnonymousUserRef, value);
        }

        public int IIsControlledPassword
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.IIsControlledPassword].AsNumber();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.IIsControlledPassword, value);
        }

        public int LogVisits
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.LogVisits].AsNumber();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.LogVisits, value);
        }

        public int Index
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.Index].AsNumber();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.Index, value);
        }

        public string DefaultDoc
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.DefaultDoc].AsString();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.DefaultDoc, value);
        }

        public int AspDetailedError
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.AspDetailedError].AsNumber();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.AspDetailedError, value);
        }

        public string HttpExpires
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.HttpExpires].AsString();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.HttpExpires, value);
        }

        public int CacheControlMaxAge
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.CacheControlMaxAge].AsNumber();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.CacheControlMaxAge, value);
        }

        public string CacheControlCustom
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.CacheControlCustom].AsString();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.CacheControlCustom, value);
        }

        public int NoCustomError
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.NoCustomError].AsNumber();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.NoCustomError, value);
        }

        public int AccessSSLFlags
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.AccessSSLFlags].AsNumber();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.AccessSSLFlags, value);
        }

        public string AuthenticationProviders
        {
            get => this.Fields[(int)IIsWebDirPropertiesTupleFields.AuthenticationProviders].AsString();
            set => this.Set((int)IIsWebDirPropertiesTupleFields.AuthenticationProviders, value);
        }
    }
}