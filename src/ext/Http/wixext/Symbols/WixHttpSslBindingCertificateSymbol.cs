// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Symbols;

    public static partial class HttpSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixHttpSslBindingCertificates = new IntermediateSymbolDefinition(
            HttpSymbolDefinitionType.WixHttpSslBindingCertificates.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HttpSslBindingCertificatesSymbolFields.BindingRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpSslBindingCertificatesSymbolFields.CertificateRef), IntermediateFieldType.String),
            },
            typeof(WixHttpSslBindingCertificateSymbol));
    }
}

namespace WixToolset.Http.Symbols
{
    using WixToolset.Data;

    public enum HttpSslBindingCertificatesSymbolFields
    {
        BindingRef,
        CertificateRef,
    }

    public class WixHttpSslBindingCertificateSymbol : IntermediateSymbol
    {
        public WixHttpSslBindingCertificateSymbol() : base(HttpSymbolDefinitions.WixHttpSslBindingCertificates, null, null)
        {
        }

        public WixHttpSslBindingCertificateSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpSymbolDefinitions.WixHttpSslBindingCertificates, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HttpSslBindingCertificatesSymbolFields index] => this.Fields[(int)index];

        public string BindingRef
        {
            get => this.Fields[(int)HttpSslBindingCertificatesSymbolFields.BindingRef].AsString();
            set => this.Set((int)HttpSslBindingCertificatesSymbolFields.BindingRef, value);
        }

        public string CertificateRef
        {
            get => this.Fields[(int)HttpSslBindingCertificatesSymbolFields.CertificateRef].AsString();
            set => this.Set((int)HttpSslBindingCertificatesSymbolFields.CertificateRef, value);
        }
    }
}
