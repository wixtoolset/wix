// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebSiteCertificates = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebSiteCertificates.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebSiteCertificatesTupleFields.Web_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteCertificatesTupleFields.Certificate_), IntermediateFieldType.String),
            },
            typeof(IIsWebSiteCertificatesTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebSiteCertificatesTupleFields
    {
        Web_,
        Certificate_,
    }

    public class IIsWebSiteCertificatesTuple : IntermediateTuple
    {
        public IIsWebSiteCertificatesTuple() : base(IisTupleDefinitions.IIsWebSiteCertificates, null, null)
        {
        }

        public IIsWebSiteCertificatesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebSiteCertificates, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebSiteCertificatesTupleFields index] => this.Fields[(int)index];

        public string Web_
        {
            get => this.Fields[(int)IIsWebSiteCertificatesTupleFields.Web_].AsString();
            set => this.Set((int)IIsWebSiteCertificatesTupleFields.Web_, value);
        }

        public string Certificate_
        {
            get => this.Fields[(int)IIsWebSiteCertificatesTupleFields.Certificate_].AsString();
            set => this.Set((int)IIsWebSiteCertificatesTupleFields.Certificate_, value);
        }
    }
}