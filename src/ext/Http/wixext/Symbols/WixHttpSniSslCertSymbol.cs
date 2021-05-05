// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Symbols;

    public static partial class HttpSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixHttpSniSslCert = new IntermediateSymbolDefinition(
            HttpSymbolDefinitionType.WixHttpSniSslCert.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixHttpSniSslCertSymbolFields.Host), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSniSslCertSymbolFields.Port), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSniSslCertSymbolFields.Thumbprint), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSniSslCertSymbolFields.AppId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSniSslCertSymbolFields.Store), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpSniSslCertSymbolFields.HandleExisting), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixHttpSniSslCertSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(WixHttpSniSslCertSymbol));
    }
}

namespace WixToolset.Http.Symbols
{
    using WixToolset.Data;

    public enum WixHttpSniSslCertSymbolFields
    {
        Host,
        Port,
        Thumbprint,
        AppId,
        Store,
        HandleExisting,
        ComponentRef,
    }

    public class WixHttpSniSslCertSymbol : IntermediateSymbol
    {
        public WixHttpSniSslCertSymbol() : base(HttpSymbolDefinitions.WixHttpSniSslCert, null, null)
        {
        }

        public WixHttpSniSslCertSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpSymbolDefinitions.WixHttpSniSslCert, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixHttpSniSslCertSymbolFields index] => this.Fields[(int)index];

        public string Host
        {
            get => this.Fields[(int)WixHttpSniSslCertSymbolFields.Host].AsString();
            set => this.Set((int)WixHttpSniSslCertSymbolFields.Host, value);
        }

        public string Port
        {
            get => this.Fields[(int)WixHttpSniSslCertSymbolFields.Port].AsString();
            set => this.Set((int)WixHttpSniSslCertSymbolFields.Port, value);
        }

        public string Thumbprint
        {
            get => this.Fields[(int)WixHttpSniSslCertSymbolFields.Thumbprint].AsString();
            set => this.Set((int)WixHttpSniSslCertSymbolFields.Thumbprint, value);
        }

        public string AppId
        {
            get => this.Fields[(int)WixHttpSniSslCertSymbolFields.AppId].AsString();
            set => this.Set((int)WixHttpSniSslCertSymbolFields.AppId, value);
        }

        public string Store
        {
            get => this.Fields[(int)WixHttpSniSslCertSymbolFields.Store].AsString();
            set => this.Set((int)WixHttpSniSslCertSymbolFields.Store, value);
        }

        public HandleExisting HandleExisting
        {
            get => (HandleExisting)this.Fields[(int)WixHttpSniSslCertSymbolFields.HandleExisting].AsNumber();
            set => this.Set((int)WixHttpSniSslCertSymbolFields.HandleExisting, (int)value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)WixHttpSniSslCertSymbolFields.ComponentRef].AsString();
            set => this.Set((int)WixHttpSniSslCertSymbolFields.ComponentRef, value);
        }
    }
}
