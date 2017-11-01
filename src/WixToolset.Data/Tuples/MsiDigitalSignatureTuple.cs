// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiDigitalSignature = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiDigitalSignature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiDigitalSignatureTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiDigitalSignatureTupleFields.SignObject), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiDigitalSignatureTupleFields.DigitalCertificate_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiDigitalSignatureTupleFields.Hash), IntermediateFieldType.Path),
            },
            typeof(MsiDigitalSignatureTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiDigitalSignatureTupleFields
    {
        Table,
        SignObject,
        DigitalCertificate_,
        Hash,
    }

    public class MsiDigitalSignatureTuple : IntermediateTuple
    {
        public MsiDigitalSignatureTuple() : base(TupleDefinitions.MsiDigitalSignature, null, null)
        {
        }

        public MsiDigitalSignatureTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiDigitalSignature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiDigitalSignatureTupleFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)MsiDigitalSignatureTupleFields.Table]?.Value;
            set => this.Set((int)MsiDigitalSignatureTupleFields.Table, value);
        }

        public string SignObject
        {
            get => (string)this.Fields[(int)MsiDigitalSignatureTupleFields.SignObject]?.Value;
            set => this.Set((int)MsiDigitalSignatureTupleFields.SignObject, value);
        }

        public string DigitalCertificate_
        {
            get => (string)this.Fields[(int)MsiDigitalSignatureTupleFields.DigitalCertificate_]?.Value;
            set => this.Set((int)MsiDigitalSignatureTupleFields.DigitalCertificate_, value);
        }

        public string Hash
        {
            get => (string)this.Fields[(int)MsiDigitalSignatureTupleFields.Hash]?.Value;
            set => this.Set((int)MsiDigitalSignatureTupleFields.Hash, value);
        }
    }
}