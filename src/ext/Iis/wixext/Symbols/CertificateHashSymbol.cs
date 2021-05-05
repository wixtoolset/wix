// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition CertificateHash = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.CertificateHash.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(CertificateHashSymbolFields.CertificateRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CertificateHashSymbolFields.Hash), IntermediateFieldType.String),
            },
            typeof(CertificateHashSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum CertificateHashSymbolFields
    {
        CertificateRef,
        Hash,
    }

    public class CertificateHashSymbol : IntermediateSymbol
    {
        public CertificateHashSymbol() : base(IisSymbolDefinitions.CertificateHash, null, null)
        {
        }

        public CertificateHashSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.CertificateHash, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CertificateHashSymbolFields index] => this.Fields[(int)index];

        public string CertificateRef
        {
            get => this.Fields[(int)CertificateHashSymbolFields.CertificateRef].AsString();
            set => this.Set((int)CertificateHashSymbolFields.CertificateRef, value);
        }

        public string Hash
        {
            get => this.Fields[(int)CertificateHashSymbolFields.Hash].AsString();
            set => this.Set((int)CertificateHashSymbolFields.Hash, value);
        }
    }
}