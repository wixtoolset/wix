// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiPatchCertificate = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiPatchCertificate,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchCertificateSymbolFields.PatchCertificate), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchCertificateSymbolFields.DigitalCertificateRef), IntermediateFieldType.String),
            },
            typeof(MsiPatchCertificateSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiPatchCertificateSymbolFields
    {
        PatchCertificate,
        DigitalCertificateRef,
    }

    public class MsiPatchCertificateSymbol : IntermediateSymbol
    {
        public MsiPatchCertificateSymbol() : base(SymbolDefinitions.MsiPatchCertificate, null, null)
        {
        }

        public MsiPatchCertificateSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiPatchCertificate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchCertificateSymbolFields index] => this.Fields[(int)index];

        public string PatchCertificate
        {
            get => (string)this.Fields[(int)MsiPatchCertificateSymbolFields.PatchCertificate];
            set => this.Set((int)MsiPatchCertificateSymbolFields.PatchCertificate, value);
        }

        public string DigitalCertificateRef
        {
            get => (string)this.Fields[(int)MsiPatchCertificateSymbolFields.DigitalCertificateRef];
            set => this.Set((int)MsiPatchCertificateSymbolFields.DigitalCertificateRef, value);
        }
    }
}