// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiDigitalCertificate = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiDigitalCertificate,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiDigitalCertificateSymbolFields.CertData), IntermediateFieldType.Path),
            },
            typeof(MsiDigitalCertificateSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiDigitalCertificateSymbolFields
    {
        CertData,
    }

    public class MsiDigitalCertificateSymbol : IntermediateSymbol
    {
        public MsiDigitalCertificateSymbol() : base(SymbolDefinitions.MsiDigitalCertificate, null, null)
        {
        }

        public MsiDigitalCertificateSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiDigitalCertificate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiDigitalCertificateSymbolFields index] => this.Fields[(int)index];

        public string CertData
        {
            get => (string)this.Fields[(int)MsiDigitalCertificateSymbolFields.CertData];
            set => this.Set((int)MsiDigitalCertificateSymbolFields.CertData, value);
        }
    }
}