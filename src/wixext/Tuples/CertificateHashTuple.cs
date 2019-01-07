// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition CertificateHash = new IntermediateTupleDefinition(
            IisTupleDefinitionType.CertificateHash.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(CertificateHashTupleFields.Certificate_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CertificateHashTupleFields.Hash), IntermediateFieldType.String),
            },
            typeof(CertificateHashTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum CertificateHashTupleFields
    {
        Certificate_,
        Hash,
    }

    public class CertificateHashTuple : IntermediateTuple
    {
        public CertificateHashTuple() : base(IisTupleDefinitions.CertificateHash, null, null)
        {
        }

        public CertificateHashTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.CertificateHash, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CertificateHashTupleFields index] => this.Fields[(int)index];

        public string Certificate_
        {
            get => this.Fields[(int)CertificateHashTupleFields.Certificate_].AsString();
            set => this.Set((int)CertificateHashTupleFields.Certificate_, value);
        }

        public string Hash
        {
            get => this.Fields[(int)CertificateHashTupleFields.Hash].AsString();
            set => this.Set((int)CertificateHashTupleFields.Hash, value);
        }
    }
}