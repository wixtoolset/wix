// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiPatchCertificate = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiPatchCertificate,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchCertificateTupleFields.PatchCertificate), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchCertificateTupleFields.DigitalCertificateRef), IntermediateFieldType.String),
            },
            typeof(MsiPatchCertificateTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiPatchCertificateTupleFields
    {
        PatchCertificate,
        DigitalCertificateRef,
    }

    public class MsiPatchCertificateTuple : IntermediateTuple
    {
        public MsiPatchCertificateTuple() : base(TupleDefinitions.MsiPatchCertificate, null, null)
        {
        }

        public MsiPatchCertificateTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiPatchCertificate, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchCertificateTupleFields index] => this.Fields[(int)index];

        public string PatchCertificate
        {
            get => (string)this.Fields[(int)MsiPatchCertificateTupleFields.PatchCertificate];
            set => this.Set((int)MsiPatchCertificateTupleFields.PatchCertificate, value);
        }

        public string DigitalCertificateRef
        {
            get => (string)this.Fields[(int)MsiPatchCertificateTupleFields.DigitalCertificateRef];
            set => this.Set((int)MsiPatchCertificateTupleFields.DigitalCertificateRef, value);
        }
    }
}