// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebDirProperties = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebDirProperties.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.Access), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.Authorization), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.AnonymousUserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.IIsControlledPassword), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.LogVisits), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.Index), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.DefaultDoc), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.AspDetailedError), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.HttpExpires), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.CacheControlMaxAge), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.CacheControlCustom), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.NoCustomError), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.AccessSSLFlags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebDirPropertiesSymbolFields.AuthenticationProviders), IntermediateFieldType.String),
            },
            typeof(IIsWebDirPropertiesSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebDirPropertiesSymbolFields
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

    public class IIsWebDirPropertiesSymbol : IntermediateSymbol
    {
        public IIsWebDirPropertiesSymbol() : base(IisSymbolDefinitions.IIsWebDirProperties, null, null)
        {
        }

        public IIsWebDirPropertiesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebDirProperties, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebDirPropertiesSymbolFields index] => this.Fields[(int)index];

        public int? Access
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.Access].AsNullableNumber();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.Access, value);
        }

        public int? Authorization
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.Authorization].AsNullableNumber();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.Authorization, value);
        }

        public string AnonymousUserRef
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.AnonymousUserRef].AsString();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.AnonymousUserRef, value);
        }

        public int? IIsControlledPassword
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.IIsControlledPassword].AsNullableNumber();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.IIsControlledPassword, value);
        }

        public int? LogVisits
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.LogVisits].AsNullableNumber();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.LogVisits, value);
        }

        public int? Index
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.Index].AsNullableNumber();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.Index, value);
        }

        public string DefaultDoc
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.DefaultDoc].AsString();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.DefaultDoc, value);
        }

        public int? AspDetailedError
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.AspDetailedError].AsNullableNumber();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.AspDetailedError, value);
        }

        public string HttpExpires
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.HttpExpires].AsString();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.HttpExpires, value);
        }

        public int? CacheControlMaxAge
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.CacheControlMaxAge].AsNullableNumber();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.CacheControlMaxAge, value);
        }

        public string CacheControlCustom
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.CacheControlCustom].AsString();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.CacheControlCustom, value);
        }

        public int? NoCustomError
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.NoCustomError].AsNullableNumber();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.NoCustomError, value);
        }

        public int? AccessSSLFlags
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.AccessSSLFlags].AsNullableNumber();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.AccessSSLFlags, value);
        }

        public string AuthenticationProviders
        {
            get => this.Fields[(int)IIsWebDirPropertiesSymbolFields.AuthenticationProviders].AsString();
            set => this.Set((int)IIsWebDirPropertiesSymbolFields.AuthenticationProviders, value);
        }
    }
}