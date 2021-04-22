// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiDigitalSignature = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiDigitalSignature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiDigitalSignatureSymbolFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiDigitalSignatureSymbolFields.SignObject), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiDigitalSignatureSymbolFields.DigitalCertificateRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiDigitalSignatureSymbolFields.Hash), IntermediateFieldType.Path),
            },
            typeof(MsiDigitalSignatureSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiDigitalSignatureSymbolFields
    {
        Table,
        SignObject,
        DigitalCertificateRef,
        Hash,
    }

    public class MsiDigitalSignatureSymbol : IntermediateSymbol
    {
        public MsiDigitalSignatureSymbol() : base(SymbolDefinitions.MsiDigitalSignature, null, null)
        {
        }

        public MsiDigitalSignatureSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiDigitalSignature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiDigitalSignatureSymbolFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)MsiDigitalSignatureSymbolFields.Table];
            set => this.Set((int)MsiDigitalSignatureSymbolFields.Table, value);
        }

        public string SignObject
        {
            get => (string)this.Fields[(int)MsiDigitalSignatureSymbolFields.SignObject];
            set => this.Set((int)MsiDigitalSignatureSymbolFields.SignObject, value);
        }

        public string DigitalCertificateRef
        {
            get => (string)this.Fields[(int)MsiDigitalSignatureSymbolFields.DigitalCertificateRef];
            set => this.Set((int)MsiDigitalSignatureSymbolFields.DigitalCertificateRef, value);
        }

        public string Hash
        {
            get => (string)this.Fields[(int)MsiDigitalSignatureSymbolFields.Hash];
            set => this.Set((int)MsiDigitalSignatureSymbolFields.Hash, value);
        }
    }
}