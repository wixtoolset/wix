// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiDigitalCertificate = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiDigitalCertificate,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiDigitalCertificateTupleFields.DigitalCertificate), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiDigitalCertificateTupleFields.CertData), IntermediateFieldType.Path),
            },
            typeof(MsiDigitalCertificateTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiDigitalCertificateTupleFields
    {
        DigitalCertificate,
        CertData,
    }

    public class MsiDigitalCertificateTuple : IntermediateTuple
    {
        public MsiDigitalCertificateTuple() : base(TupleDefinitions.MsiDigitalCertificate, null, null)
        {
        }

        public MsiDigitalCertificateTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiDigitalCertificate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiDigitalCertificateTupleFields index] => this.Fields[(int)index];

        public string DigitalCertificate
        {
            get => (string)this.Fields[(int)MsiDigitalCertificateTupleFields.DigitalCertificate]?.Value;
            set => this.Set((int)MsiDigitalCertificateTupleFields.DigitalCertificate, value);
        }

        public string CertData
        {
            get => (string)this.Fields[(int)MsiDigitalCertificateTupleFields.CertData]?.Value;
            set => this.Set((int)MsiDigitalCertificateTupleFields.CertData, value);
        }
    }
}