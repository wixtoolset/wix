// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebSiteCertificates = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebSiteCertificates.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebSiteCertificatesSymbolFields.WebRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteCertificatesSymbolFields.CertificateRef), IntermediateFieldType.String),
            },
            typeof(IIsWebSiteCertificatesSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebSiteCertificatesSymbolFields
    {
        WebRef,
        CertificateRef,
    }

    public class IIsWebSiteCertificatesSymbol : IntermediateSymbol
    {
        public IIsWebSiteCertificatesSymbol() : base(IisSymbolDefinitions.IIsWebSiteCertificates, null, null)
        {
        }

        public IIsWebSiteCertificatesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebSiteCertificates, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebSiteCertificatesSymbolFields index] => this.Fields[(int)index];

        public string WebRef
        {
            get => this.Fields[(int)IIsWebSiteCertificatesSymbolFields.WebRef].AsString();
            set => this.Set((int)IIsWebSiteCertificatesSymbolFields.WebRef, value);
        }

        public string CertificateRef
        {
            get => this.Fields[(int)IIsWebSiteCertificatesSymbolFields.CertificateRef].AsString();
            set => this.Set((int)IIsWebSiteCertificatesSymbolFields.CertificateRef, value);
        }
    }
}