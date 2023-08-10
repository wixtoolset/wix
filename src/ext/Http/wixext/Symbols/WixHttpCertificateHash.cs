// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Symbols;

    public static partial class HttpSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixHttpCertificateHash = new IntermediateSymbolDefinition(
            HttpSymbolDefinitionType.WixHttpCertificateHash.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixHttpCertificateHashSymbolFields.CertificateRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpCertificateHashSymbolFields.Hash), IntermediateFieldType.String),
            },
            typeof(WixHttpCertificateHashSymbol));
    }
}

namespace WixToolset.Http.Symbols
{
    using WixToolset.Data;

    public enum WixHttpCertificateHashSymbolFields
    {
        CertificateRef,
        Hash,
    }

    public class WixHttpCertificateHashSymbol : IntermediateSymbol
    {
        public WixHttpCertificateHashSymbol() : base(HttpSymbolDefinitions.WixHttpCertificateHash, null, null)
        {
        }

        public WixHttpCertificateHashSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpSymbolDefinitions.WixHttpCertificateHash, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixHttpCertificateHashSymbolFields index] => this.Fields[(int)index];

        public string CertificateRef
        {
            get => this.Fields[(int)WixHttpCertificateHashSymbolFields.CertificateRef].AsString();
            set => this.Set((int)WixHttpCertificateHashSymbolFields.CertificateRef, value);
        }

        public string Hash
        {
            get => this.Fields[(int)WixHttpCertificateHashSymbolFields.Hash].AsString();
            set => this.Set((int)WixHttpCertificateHashSymbolFields.Hash, value);
        }
    }
}
