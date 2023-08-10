// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Symbols;

    public static partial class HttpSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixHttpCertificate = new IntermediateSymbolDefinition(
            HttpSymbolDefinitionType.WixHttpCertificate.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.StoreLocation), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.StoreName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.BinaryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.CertificatePath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HttpCertificateSymbolFields.PfxPassword), IntermediateFieldType.String),
            },
            typeof(WixHttpCertificateSymbol));
    }
}

namespace WixToolset.Http.Symbols
{
    using WixToolset.Data;
    using WixToolset.Http;

    public enum HttpCertificateSymbolFields
    {
        ComponentRef,
        Name,
        StoreLocation,
        StoreName,
        Attributes,
        BinaryRef,
        CertificatePath,
        PfxPassword,
    }

    public class WixHttpCertificateSymbol : IntermediateSymbol
    {
        public WixHttpCertificateSymbol() : base(HttpSymbolDefinitions.WixHttpCertificate, null, null)
        {
        }

        public WixHttpCertificateSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpSymbolDefinitions.WixHttpCertificate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HttpCertificateSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.ComponentRef].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.Name].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.Name, value);
        }

        public int StoreLocation
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.StoreLocation].AsNumber();
            set => this.Set((int)HttpCertificateSymbolFields.StoreLocation, value);
        }

        public string StoreName
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.StoreName].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.StoreName, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.Attributes].AsNumber();
            set => this.Set((int)HttpCertificateSymbolFields.Attributes, value);
        }

        public string BinaryRef
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.BinaryRef].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.BinaryRef, value);
        }

        public string CertificatePath
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.CertificatePath].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.CertificatePath, value);
        }

        public string PfxPassword
        {
            get => this.Fields[(int)HttpCertificateSymbolFields.PfxPassword].AsString();
            set => this.Set((int)HttpCertificateSymbolFields.PfxPassword, value);
        }
    }
}
