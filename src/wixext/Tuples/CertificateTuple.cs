// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Certificate = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.Certificate.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(CertificateSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CertificateSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CertificateSymbolFields.StoreLocation), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(CertificateSymbolFields.StoreName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CertificateSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(CertificateSymbolFields.BinaryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CertificateSymbolFields.CertificatePath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CertificateSymbolFields.PFXPassword), IntermediateFieldType.String),
            },
            typeof(CertificateSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum CertificateSymbolFields
    {
        ComponentRef,
        Name,
        StoreLocation,
        StoreName,
        Attributes,
        BinaryRef,
        CertificatePath,
        PFXPassword,
    }

    public class CertificateSymbol : IntermediateSymbol
    {
        public CertificateSymbol() : base(IisSymbolDefinitions.Certificate, null, null)
        {
        }

        public CertificateSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.Certificate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CertificateSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)CertificateSymbolFields.ComponentRef].AsString();
            set => this.Set((int)CertificateSymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)CertificateSymbolFields.Name].AsString();
            set => this.Set((int)CertificateSymbolFields.Name, value);
        }

        public int StoreLocation
        {
            get => this.Fields[(int)CertificateSymbolFields.StoreLocation].AsNumber();
            set => this.Set((int)CertificateSymbolFields.StoreLocation, value);
        }

        public string StoreName
        {
            get => this.Fields[(int)CertificateSymbolFields.StoreName].AsString();
            set => this.Set((int)CertificateSymbolFields.StoreName, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)CertificateSymbolFields.Attributes].AsNumber();
            set => this.Set((int)CertificateSymbolFields.Attributes, value);
        }

        public string BinaryRef
        {
            get => this.Fields[(int)CertificateSymbolFields.BinaryRef].AsString();
            set => this.Set((int)CertificateSymbolFields.BinaryRef, value);
        }

        public string CertificatePath
        {
            get => this.Fields[(int)CertificateSymbolFields.CertificatePath].AsString();
            set => this.Set((int)CertificateSymbolFields.CertificatePath, value);
        }

        public string PFXPassword
        {
            get => this.Fields[(int)CertificateSymbolFields.PFXPassword].AsString();
            set => this.Set((int)CertificateSymbolFields.PFXPassword, value);
        }
    }
}