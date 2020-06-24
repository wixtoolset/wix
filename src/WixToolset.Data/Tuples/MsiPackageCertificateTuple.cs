// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiPackageCertificate = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiPackageCertificate,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPackageCertificateSymbolFields.PackageCertificate), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPackageCertificateSymbolFields.DigitalCertificateRef), IntermediateFieldType.String),
            },
            typeof(MsiPackageCertificateSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiPackageCertificateSymbolFields
    {
        PackageCertificate,
        DigitalCertificateRef,
    }

    public class MsiPackageCertificateSymbol : IntermediateSymbol
    {
        public MsiPackageCertificateSymbol() : base(SymbolDefinitions.MsiPackageCertificate, null, null)
        {
        }

        public MsiPackageCertificateSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiPackageCertificate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPackageCertificateSymbolFields index] => this.Fields[(int)index];

        public string PackageCertificate
        {
            get => (string)this.Fields[(int)MsiPackageCertificateSymbolFields.PackageCertificate];
            set => this.Set((int)MsiPackageCertificateSymbolFields.PackageCertificate, value);
        }

        public string DigitalCertificateRef
        {
            get => (string)this.Fields[(int)MsiPackageCertificateSymbolFields.DigitalCertificateRef];
            set => this.Set((int)MsiPackageCertificateSymbolFields.DigitalCertificateRef, value);
        }
    }
}