// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Symbols;

    public static partial class HttpSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition HttpCertificate = new IntermediateSymbolDefinition(
            HttpSymbolDefinitionType.HttpCertificate.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.Host), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.Port), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.Thumbprint), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.AppId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.Store), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.HandleExisting), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.CertificateType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(HttpCertificateSymbol));
    }
}

namespace WixToolset.Http.Symbols
{
    using WixToolset.Data;

    public enum HttpCertificateSymbolFields
    {
        Host,
        Port,
        Thumbprint,
        AppId,
        Store,
        HandleExisting,
        CertificateType,
        ComponentRef,
    }

    public class HttpCertificateSymbol : IntermediateSymbol
    {
        public HttpCertificateSymbol() : base(HttpSymbolDefinitions.HttpCertificate, null, null)
        {
        }

        public HttpCertificateSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpSymbolDefinitions.HttpCertificate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HttpCertificateSymbolFields index] => this.Fields[(int)index];

        public string Host
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.Host].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.Host, value);
        }

        public string Port
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.Port].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.Port, value);
        }

        public string Thumbprint
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.Thumbprint].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.Thumbprint, value);
        }

        public string AppId
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.AppId].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.AppId, value);
        }

        public string Store
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.Store].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.Store, value);
        }

        public HandleExisting HandleExisting
        {
            get => (HandleExisting)this.Fields[(int)HttpCertificateSymbolFields.HandleExisting].AsNumber();
            set => this.Set((int)HttpCertificateSymbolFields.HandleExisting, (int)value);
        }

        public CertificateType CertificateType
        {
            get => (CertificateType)this.Fields[(int)HttpCertificateSymbolFields.CertificateType].AsNumber();
            set => this.Set((int)HttpCertificateSymbolFields.CertificateType, (int)value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.ComponentRef].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.ComponentRef, value);
        }
    }
}
